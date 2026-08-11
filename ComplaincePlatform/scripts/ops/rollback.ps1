<#
.SYNOPSIS
  Rollback AOVIAS databases from a backup folder (or manifest snapshot).

.PARAMETER BackupFolder
  Folder containing .bak files (default: Gateway/App_Data/backups).

.PARAMETER Stamp
  Optional timestamp suffix filter (yyyyMMdd-HHmmss) to restore a specific set.

.PARAMETER Databases
  Databases to restore.
#>
[CmdletBinding()]
param(
    [string]$BackupFolder = "",
    [string]$Stamp = "",
    [string[]]$Databases = @("Identity", "Organization", "Compliance"),
    [string]$Server = $(if ($env:SQL_SERVER) { $env:SQL_SERVER } else { "localhost,1433" }),
    [string]$SqlPassword = $(if ($env:SA_PASSWORD) { $env:SA_PASSWORD } else { "" }),
    [switch]$Confirm
)

$ErrorActionPreference = "Stop"
$opsDir = $PSScriptRoot
$platformRoot = (Resolve-Path (Join-Path $opsDir "..\..")).Path
if (-not $BackupFolder) {
    $BackupFolder = Join-Path $platformRoot "Gateway\App_Data\backups"
}

if (-not (Test-Path $BackupFolder)) {
    throw "Backup folder not found: $BackupFolder"
}

if (-not $Confirm) {
    Write-Host "Dry run. Re-run with -Confirm to execute restores."
}

foreach ($db in $Databases) {
    $pattern = if ($Stamp) { "${db}_${Stamp}.bak" } else { "${db}_*.bak" }
    $file = Get-ChildItem -Path $BackupFolder -Filter $pattern -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $file) {
        Write-Warning "No backup matching $pattern in $BackupFolder"
        continue
    }

    Write-Host "Rollback candidate: $db <- $($file.FullName)"
    if ($Confirm) {
        & (Join-Path $opsDir "restore-database.ps1") `
            -Database $db `
            -BackupFile $file.FullName `
            -Server $Server `
            -SqlPassword $SqlPassword `
            -Replace
        if ($LASTEXITCODE -ne 0) {
            throw "Restore failed for $db"
        }
    }
}

$manifestPath = Join-Path $BackupFolder "manifest.json"
if (Test-Path $manifestPath) {
    Write-Host "Last backup manifest:"
    Get-Content $manifestPath -Raw
}

Write-Host "Rollback procedure complete (confirm=$Confirm)."
if (-not $Confirm) { exit 0 }
exit 0
