<#
.SYNOPSIS
  Validate commercial / upgrade environment variables and optional SQL connectivity.

.DESCRIPTION
  Checks required env vars for compose/deploy and optionally probes SQL with sqlcmd.
  Exit 0 = ok, 1 = missing required vars, 2 = connection failure.
#>
[CmdletBinding()]
param(
    [switch]$RequireSql,
    [string]$Server = $(if ($env:SQL_SERVER) { $env:SQL_SERVER } else { "localhost,1433" }),
    [string]$SqlUser = "sa",
    [string]$SqlPassword = $(if ($env:SA_PASSWORD) { $env:SA_PASSWORD } else { "" }),
    [string[]]$HealthUrls = @()
)

$ErrorActionPreference = "Continue"
$failures = @()

$required = @(
    @{ Name = "SA_PASSWORD"; Value = $env:SA_PASSWORD }
)

$recommended = @(
    @{ Name = "ASPNETCORE_CERT_PASSWORD"; Value = $env:ASPNETCORE_CERT_PASSWORD },
    @{ Name = "SQL_SERVER"; Value = $env:SQL_SERVER },
    @{ Name = "GATEWAY_PUBLIC_URL"; Value = $env:GATEWAY_PUBLIC_URL },
    @{ Name = "JwtSettings__SecretKey"; Value = $env:JwtSettings__SecretKey },
    @{ Name = "InternalApi__ApiKey"; Value = $env:InternalApi__ApiKey }
)

Write-Host "=== AOVIAS environment validation ==="

foreach ($r in $required) {
    if ([string]::IsNullOrWhiteSpace($r.Value)) {
        $failures += "Missing required env: $($r.Name)"
        Write-Host "[FAIL] $($r.Name)"
    } else {
        Write-Host "[OK]   $($r.Name) (set)"
    }
}

foreach ($r in $recommended) {
    if ([string]::IsNullOrWhiteSpace($r.Value)) {
        Write-Host "[WARN] $($r.Name) not set"
    } else {
        Write-Host "[OK]   $($r.Name) (set)"
    }
}

if ($RequireSql) {
    $pwd = if ($SqlPassword) { $SqlPassword } else { $env:SA_PASSWORD }
    $sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
    if (-not $sqlcmd) {
        $failures += "sqlcmd not found but -RequireSql was specified"
        Write-Host "[FAIL] sqlcmd not found"
    } elseif (-not $pwd) {
        $failures += "SQL password empty"
        Write-Host "[FAIL] SQL password empty"
    } else {
        & sqlcmd -S $Server -U $SqlUser -P $pwd -C -Q "SELECT 1" -b | Out-Null
        if ($LASTEXITCODE -ne 0) {
            $failures += "SQL connection failed to $Server"
            Write-Host "[FAIL] SQL connection to $Server"
        } else {
            Write-Host "[OK]   SQL connection to $Server"
        }
    }
}

foreach ($url in $HealthUrls) {
    try {
        $resp = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10
        if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 400) {
            Write-Host "[OK]   Health $url -> $($resp.StatusCode)"
        } else {
            $failures += "Health check unexpected status $($resp.StatusCode) for $url"
            Write-Host "[FAIL] Health $url -> $($resp.StatusCode)"
        }
    } catch {
        $failures += "Health check failed for $url : $($_.Exception.Message)"
        Write-Host "[FAIL] Health $url"
    }
}

if ($failures.Count -gt 0) {
    Write-Host "`nValidation FAILED:"
    $failures | ForEach-Object { Write-Host " - $_" }
    exit 1
}

Write-Host "`nValidation passed."
exit 0
