<#
.SYNOPSIS
    INF-01 Phase 4 -- EF Core migration integrity gate.

.DESCRIPTION
    Fails the build when the migration system drifts. Three checks, each
    targeting a failure this repository has actually experienced:

      1. Migration attributes   -- every migration file must carry [Migration].
                                  Two files (Phase36, H1_4) lacked it and were
                                  silently invisible to EF Core: never applied,
                                  never reported pending. A fresh database
                                  simply never gained DashboardProjections.

      2. Snapshot drift         -- generating a migration must produce zero
                                  operations. 16 hand-written migrations left
                                  ComplianceDbContextModelSnapshot.cs stale, so
                                  every new migration tried to recreate objects
                                  that already existed.

      3. Fresh database apply   -- all migrations must build a database from
                                  nothing. This is the only check that proves a
                                  deployment to a new environment will work.

.NOTES
    Run from CI on every pull request touching ComplianceService.
    Exit code 0 = pass, 1 = fail.
#>
[CmdletBinding()]
param(
    [string] $ProjectPath = "$PSScriptRoot\..\..\ComplianceService\ComplianceService.csproj",
    [string] $MigrationsDir = "$PSScriptRoot\..\..\ComplianceService\Migrations",
    [string] $SqlServer = 'localhost\SQLEXPRESS',
    # Disposable database for check 3. Dropped and recreated on every run.
    [string] $FreshDbName = 'Compliance_CiFreshTest'
)

# 'Continue', not 'Stop': dotnet writes NuGet warnings to stderr, and under
# 'Stop' PowerShell 5.1 promotes those to terminating errors even when the
# command succeeded. Failures are detected explicitly via  below.
$ErrorActionPreference = 'Continue'
$failures = @()

function Write-Check($name) { Write-Host "`n[CHECK] $name" -ForegroundColor Cyan }
function Fail($msg) { Write-Host "  FAIL: $msg" -ForegroundColor Red; $script:failures += $msg }
function Pass($msg) { Write-Host "  PASS: $msg" -ForegroundColor Green }

# -- 1. Every migration is visible to EF Core ---------------------------------
Write-Check 'Migration attributes'
# EF-generated migrations carry [Migration] in their .Designer.cs; hand-written
# ones must declare it in the migration file itself. A file with neither is
# invisible to EF Core - it will never be applied and never reported pending.
$invisible = @()
Get-ChildItem $MigrationsDir -Filter '*.cs' |
    Where-Object { $_.Name -notmatch 'Designer|ModelSnapshot' } |
    ForEach-Object {
        $designer = Join-Path $_.DirectoryName ($_.BaseName + '.Designer.cs')
        $hasAttr = Select-String -Path $_.FullName -Pattern '\[Migration\(' -Quiet
        if (-not $hasAttr -and -not (Test-Path $designer)) {
            $invisible += $_.Name
        }
    }
if ($invisible.Count) {
    Fail "Migration(s) missing the [Migration] attribute -- EF Core cannot see them: $($invisible -join ', ')"
} else {
    Pass 'All migrations carry [Migration].'
}

# -- 2. Model snapshot matches the entity model -------------------------------
Write-Check 'Snapshot drift'
$probe = "CiDriftProbe$(Get-Date -Format 'yyyyMMddHHmmss')"
$probeFiles = @()
try {
    dotnet ef migrations add $probe --project $ProjectPath | Out-Null
    $probeFiles = Get-ChildItem $MigrationsDir -Filter "*$probe*"
    $body = $probeFiles | Where-Object { $_.Name -notmatch 'Designer' } | Select-Object -First 1
    if (-not $body) {
        Fail 'Probe migration was not generated; cannot assess drift.'
    } else {
        $ops = (Select-String -Path $body.FullName -Pattern 'migrationBuilder\.').Count
        if ($ops -gt 0) {
            Fail "Snapshot drift: a generated migration contains $ops operation(s). Expected 0. Review $($body.Name) before merging."
        } else {
            Pass 'Generated migration is empty -- snapshot matches the model.'
        }
    }
} finally {
    # Delete the probe files directly. `ef migrations remove` reverts the
    # snapshot using the previous migration's Designer file, and corrupts it
    # when that file is absent -- which is how INF-01 lost the snapshot once.
    $probeFiles | Remove-Item -Force -ErrorAction SilentlyContinue
}

# -- 3. A fresh database can be built from source control alone ---------------
Write-Check 'Fresh database apply'
try {
    $drop = "IF DB_ID('$FreshDbName') IS NOT NULL BEGIN ALTER DATABASE [$FreshDbName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$FreshDbName]; END; CREATE DATABASE [$FreshDbName];"
    sqlcmd -S $SqlServer -E -Q $drop | Out-Null

    $conn = "Server=$SqlServer;Database=$FreshDbName;Trusted_Connection=True;TrustServerCertificate=True"
    $output = dotnet ef database update --project $ProjectPath --connection $conn
    if ($LASTEXITCODE -ne 0) {
        Fail "Migrations failed against a fresh database:`n$($output | Select-Object -Last 15 | Out-String)"
    } else {
        Pass 'All migrations applied to a fresh database.'
    }
} finally {
    sqlcmd -S $SqlServer -E -Q "IF DB_ID('$FreshDbName') IS NOT NULL BEGIN ALTER DATABASE [$FreshDbName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$FreshDbName]; END;" | Out-Null
}

# -- Result -------------------------------------------------------------------
Write-Host ''
if ($failures.Count) {
    Write-Host "MIGRATION VALIDATION FAILED ($($failures.Count) issue(s))" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}
Write-Host 'MIGRATION VALIDATION PASSED' -ForegroundColor Green
exit 0
