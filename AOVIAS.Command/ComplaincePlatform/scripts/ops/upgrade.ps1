<#
.SYNOPSIS
  AOVIAS platform upgrade helper: version detect → backup → migrate notes → health checks.

.PARAMETER TargetVersion
  Optional label recorded in the upgrade log (e.g. 1.2.0).

.PARAMETER SkipBackup
  Skip database backup step.

.PARAMETER Databases
  Databases to back up before upgrade.

.PARAMETER HealthUrls
  URLs to probe after upgrade (defaults include Gateway + core service swagger/health).
#>
[CmdletBinding()]
param(
    [string]$TargetVersion = "",
    [switch]$SkipBackup,
    [string[]]$Databases = @("Identity", "Organization", "Compliance"),
    [string[]]$HealthUrls = @(
        "http://localhost:5212/api/v1/ops/backup/status",
        "https://localhost:7135/swagger/index.html",
        "https://localhost:7181/swagger/index.html",
        "https://localhost:7087/swagger/index.html"
    ),
    [string]$ComposeFile = ""
)

$ErrorActionPreference = "Stop"
$opsDir = $PSScriptRoot
$platformRoot = (Resolve-Path (Join-Path $opsDir "..\..")).Path
$versionFile = Join-Path $platformRoot "VERSION"
$logDir = Join-Path $platformRoot "Gateway\App_Data\upgrades"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

function Get-PlatformVersion {
    if (Test-Path $versionFile) {
        return (Get-Content $versionFile -Raw).Trim()
    }
    $csproj = Join-Path $platformRoot "Gateway\Gateway.csproj"
    if (Test-Path $csproj) {
        $xml = [xml](Get-Content $csproj)
        $v = $xml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
        if ($v) { return $v }
    }
    return "unknown"
}

$fromVersion = Get-PlatformVersion
if (-not $TargetVersion) { $TargetVersion = $fromVersion }
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logPath = Join-Path $logDir "upgrade-$stamp.log"

function Write-Log([string]$msg) {
    $line = "$(Get-Date -Format o) $msg"
    Add-Content -Path $logPath -Value $line
    Write-Host $line
}

Write-Log "=== AOVIAS upgrade start ==="
Write-Log "FromVersion=$fromVersion TargetVersion=$TargetVersion"

# 1) Validate environment
Write-Log "Step 1: Validate-Environment"
& (Join-Path $opsDir "Validate-Environment.ps1")
if ($LASTEXITCODE -ne 0) {
    Write-Log "Environment validation failed (exit $LASTEXITCODE). Aborting."
    exit $LASTEXITCODE
}

# 2) Backup
if (-not $SkipBackup) {
    Write-Log "Step 2: Backup databases ($($Databases -join ', '))"
    & (Join-Path $opsDir "backup-databases.ps1") -Databases $Databases
    if ($LASTEXITCODE -ne 0) {
        Write-Log "Backup failed. Aborting upgrade."
        exit $LASTEXITCODE
    }
} else {
    Write-Log "Step 2: Backup skipped (-SkipBackup)"
}

# 3) Migrate note (EF migrations run on service startup in Development; document for prod)
Write-Log "Step 3: Migration notes"
Write-Log @"
Apply EF migrations per service after deploy (production):
  cd IdentityService;        dotnet ef database update
  cd OrganizationService;    dotnet ef database update
  cd ComplianceService;     dotnet ef database update
Or rely on Development auto-migrate / Hangfire startup hooks already configured.
Compose deploy (optional): docker compose -f docker-compose.dev.yml -f docker-compose.commercial.yml up -d --build
"@

if ($ComposeFile -and (Test-Path $ComposeFile)) {
    Write-Log "Step 3b: docker compose -f $ComposeFile up -d --build"
    docker compose -f $ComposeFile up -d --build
    if ($LASTEXITCODE -ne 0) {
        Write-Log "Compose upgrade failed. Consider rollback.ps1"
        exit $LASTEXITCODE
    }
}

# 4) Record version
if ($TargetVersion -and $TargetVersion -ne "unknown") {
    Set-Content -Path $versionFile -Value $TargetVersion -Encoding UTF8
    Write-Log "VERSION file updated to $TargetVersion"
}

# 5) Health checks
Write-Log "Step 4: Health checks"
$healthFail = $false
foreach ($url in $HealthUrls) {
    try {
        # Skip TLS validation for local HTTPS swagger probes
        if ($url -like "https://*") {
            try {
                add-type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAll : ICertificatePolicy {
  public bool CheckValidationResult(ServicePoint s, X509Certificate c, WebRequest r, int p) { return true; }
}
"@ -ErrorAction SilentlyContinue
                [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAll
            } catch { }
        }
        $params = @{ Uri = $url; UseBasicParsing = $true; TimeoutSec = 15; ErrorAction = "Stop" }
        # PowerShell 7+
        if ((Get-Command Invoke-WebRequest).Parameters.ContainsKey("SkipCertificateCheck")) {
            $params.SkipCertificateCheck = $true
        }
        $resp = Invoke-WebRequest @params
        Write-Log "Health OK $url -> $($resp.StatusCode)"
    } catch {
        Write-Log "Health WARN $url : $($_.Exception.Message)"
        # Non-fatal for swagger pages that may redirect; ops status is preferred signal
        if ($url -like "*/ops/backup/status") { $healthFail = $true }
    }
}

Write-Log "=== AOVIAS upgrade finished. Log: $logPath ==="
if ($healthFail) { exit 3 }
exit 0
