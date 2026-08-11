<#
.SYNOPSIS
  Restore a single SQL Server database from a .bak file produced by backup-databases.ps1.

.PARAMETER Server
  SQL Server host.

.PARAMETER Database
  Target database name.

.PARAMETER BackupFile
  Full path to .bak (or folder; picks newest matching Database_*.bak).

.PARAMETER Replace
  If set, uses WITH REPLACE.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Database,
    [string]$BackupFile = "",
    [string]$Server = $(if ($env:SQL_SERVER) { $env:SQL_SERVER } else { "localhost,1433" }),
    [string]$SqlUser = "sa",
    [string]$SqlPassword = $(if ($env:SA_PASSWORD) { $env:SA_PASSWORD } else { "" }),
    [string]$BackupRoot = "",
    [switch]$Replace
)

$ErrorActionPreference = "Stop"
$platformRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
if (-not $BackupRoot) {
    $BackupRoot = Join-Path $platformRoot "Gateway\App_Data\backups"
}

if (-not $BackupFile) {
    $matches = Get-ChildItem -Path $BackupRoot -Filter "${Database}_*.bak" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending
    if (-not $matches) {
        throw "No .bak found for database '$Database' under $BackupRoot. Pass -BackupFile explicitly."
    }
    $BackupFile = $matches[0].FullName
    Write-Host "Using newest backup: $BackupFile"
}

if (-not (Test-Path $BackupFile)) {
    throw "Backup file not found: $BackupFile"
}

function Find-SqlCmd {
    $cmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $paths = @(
        "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe",
        "${env:ProgramFiles}\Microsoft SQL Server\Client SDK\ODBC\180\Tools\Binn\sqlcmd.exe"
    )
    foreach ($p in $paths) { if (Test-Path $p) { return $p } }
    return $null
}

$sqlcmd = Find-SqlCmd
if (-not $sqlcmd) {
    Write-Warning @"
sqlcmd not found. Documented restore for $Database from $BackupFile:

  ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
  RESTORE DATABASE [$Database] FROM DISK = N'$BackupFile' WITH $(if ($Replace) { 'REPLACE, ' } else { '' })RECOVERY;
  ALTER DATABASE [$Database] SET MULTI_USER;

Install sqlcmd and re-run this script to execute automatically.
"@
    exit 2
}

if (-not $SqlPassword) {
    throw "SA_PASSWORD / -SqlPassword is required."
}

$with = if ($Replace) { "REPLACE, RECOVERY" } else { "RECOVERY" }
$tsql = @"
IF DB_ID(N'$Database') IS NOT NULL
BEGIN
  ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
END
RESTORE DATABASE [$Database] FROM DISK = N'$($BackupFile.Replace("'", "''"))' WITH $with;
IF DB_ID(N'$Database') IS NOT NULL
BEGIN
  ALTER DATABASE [$Database] SET MULTI_USER;
END
"@

Write-Host "Restoring $Database from $BackupFile on $Server ..."
& $sqlcmd -S $Server -U $SqlUser -P $SqlPassword -C -Q $tsql
if ($LASTEXITCODE -ne 0) {
    throw "Restore failed with exit code $LASTEXITCODE"
}
Write-Host "Restore completed for $Database."
