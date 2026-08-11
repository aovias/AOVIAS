<#
.SYNOPSIS
  Backup one or more SQL Server databases used by AOVIAS / PRAMAN.

.DESCRIPTION
  Uses sqlcmd when available. Writes .bak files under -BackupRoot and updates
  Gateway App_Data/backups/manifest.json (or -ManifestPath) with last backup metadata.

.PARAMETER Server
  SQL Server host (default: localhost,1433 or $env:SQL_SERVER).

.PARAMETER Databases
  Database names to back up (default: Identity, Organization, Compliance).

.PARAMETER BackupRoot
  Folder for .bak files (default: <repo>/Gateway/App_Data/backups).

.PARAMETER SqlUser / SqlPassword
  SQL auth credentials (defaults: sa / $env:SA_PASSWORD).
#>
[CmdletBinding()]
param(
    [string]$Server = $(if ($env:SQL_SERVER) { $env:SQL_SERVER } else { "localhost,1433" }),
    [string[]]$Databases = @("Identity", "Organization", "Compliance"),
    [string]$BackupRoot = "",
    [string]$ManifestPath = "",
    [string]$SqlUser = "sa",
    [string]$SqlPassword = $(if ($env:SA_PASSWORD) { $env:SA_PASSWORD } else { "" }),
    [switch]$WhatIfOnly
)

$ErrorActionPreference = "Stop"
# scripts/ops → ComplaincePlatform root
$platformRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
if (-not (Test-Path (Join-Path $platformRoot "Gateway"))) {
    throw "Expected Gateway under platform root: $platformRoot"
}

if (-not $BackupRoot) {
    $BackupRoot = Join-Path $platformRoot "Gateway\App_Data\backups"
}
if (-not $ManifestPath) {
    $ManifestPath = Join-Path $BackupRoot "manifest.json"
}

New-Item -ItemType Directory -Force -Path $BackupRoot | Out-Null

function Find-SqlCmd {
    $candidates = @(
        "sqlcmd",
        "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\180\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles(x86)}\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe"
    )
    foreach ($c in $candidates) {
        if ($c -eq "sqlcmd") {
            $cmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
            if ($cmd) { return $cmd.Source }
        } elseif (Test-Path $c) {
            return $c
        }
    }
    return $null
}

$sqlcmd = Find-SqlCmd
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$entries = @()

Write-Host "AOVIAS backup — server=$Server databases=$($Databases -join ',')"

if (-not $sqlcmd) {
    Write-Warning @"
sqlcmd was not found on PATH.
Placeholder mode: creating stub backup markers only.
Install SQL Server command-line tools, or run from a host with sqlcmd, then re-run this script.
Documented backup SQL (per database):
  BACKUP DATABASE [DbName] TO DISK = N'$BackupRoot\DbName_$stamp.bak' WITH INIT, COMPRESSION;
"@
}

foreach ($db in $Databases) {
    $bakName = "${db}_${stamp}.bak"
    $bakPath = Join-Path $BackupRoot $bakName
    $ok = $false
    $note = ""

    if ($WhatIfOnly) {
        Write-Host "[WhatIf] Would backup $db -> $bakPath"
        $note = "whatif"
        $ok = $true
    } elseif ($sqlcmd) {
        if (-not $SqlPassword) {
            throw "SA_PASSWORD / -SqlPassword is required when sqlcmd is available."
        }
        $tsql = "BACKUP DATABASE [$db] TO DISK = N'$($bakPath.Replace("'", "''"))' WITH INIT, COPY_ONLY, STATS = 10;"
        & $sqlcmd -S $Server -U $SqlUser -P $SqlPassword -C -Q $tsql
        if ($LASTEXITCODE -eq 0 -and (Test-Path $bakPath)) {
            $ok = $true
            $note = "sqlcmd"
            Write-Host "Backed up $db -> $bakPath"
        } else {
            $note = "sqlcmd-failed-exit-$LASTEXITCODE"
            Write-Warning "Backup failed for $db (exit $LASTEXITCODE). Writing marker."
            "PLACEHOLDER_FAILED $(Get-Date -Format o) $db" | Set-Content -Path ($bakPath + ".failed.txt") -Encoding UTF8
        }
    } else {
        "PLACEHOLDER $(Get-Date -Format o) database=$db server=$Server" | Set-Content -Path ($bakPath + ".placeholder.txt") -Encoding UTF8
        $note = "placeholder-no-sqlcmd"
        $ok = $true
        Write-Host "Placeholder recorded for $db"
    }

    $entries += [pscustomobject]@{
        database   = $db
        fileName   = $bakName
        path       = $bakPath
        success    = $ok
        method     = $note
        takenAtUtc = (Get-Date).ToUniversalTime().ToString("o")
        sizeBytes  = if (Test-Path $bakPath) { (Get-Item $bakPath).Length } else { 0 }
    }
}

$manifest = [ordered]@{
    schemaVersion   = 1
    lastBackupAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    server          = $Server
    backupRoot      = $BackupRoot
    databases       = $entries
}

$manifest | ConvertTo-Json -Depth 6 | Set-Content -Path $ManifestPath -Encoding UTF8
Write-Host "Manifest written: $ManifestPath"
exit 0
