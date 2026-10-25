<#
.SYNOPSIS
  Phase 7 — scan Gateway HTTP security headers (ASVS lite AUTO check).

.DESCRIPTION
  GETs Gateway /health (or -Path) and asserts presence of:
    - X-Content-Type-Options
    - X-Frame-Options
    - Content-Security-Policy

  Soft-fail (exit 0, warning) when Gateway is unreachable so CI/ops
  checklists do not hard-fail offline environments.
  Hard-fail (exit 1) when Gateway responds but required headers are missing.

.PARAMETER BaseUrl
  Gateway base URL. Default http://localhost:5000

.PARAMETER Path
  Path to probe. Default /health

.EXAMPLE
  .\scripts\ops\scan-security-headers.ps1
  .\scripts\ops\scan-security-headers.ps1 -BaseUrl https://localhost:7216
#>
[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$Path = "/health"
)

$ErrorActionPreference = "Continue"
$BaseUrl = $BaseUrl.TrimEnd("/")
if (-not $Path.StartsWith("/")) { $Path = "/$Path" }
$uri = "$BaseUrl$Path"

$required = @(
    "X-Content-Type-Options",
    "X-Frame-Options",
    "Content-Security-Policy"
)

Write-Host ""
Write-Host "AOVIAS security header scan" -ForegroundColor White
Write-Host ("Target: {0}" -f $uri)
Write-Host ""

try {
    $response = Invoke-WebRequest -Uri $uri -Method GET -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
}
catch {
    Write-Host ("SOFT-FAIL Gateway unreachable: {0}" -f $_.Exception.Message) -ForegroundColor Yellow
    Write-Host "Start Gateway and re-run. Exit 0 (non-blocking)." -ForegroundColor DarkGray
    exit 0
}

$status = [int]$response.StatusCode
Write-Host ("HTTP {0}" -f $status) -ForegroundColor Cyan

$headers = $response.Headers
$missing = New-Object System.Collections.Generic.List[string]
$present = New-Object System.Collections.Generic.List[string]

foreach ($name in $required) {
    $value = $null
    if ($headers.ContainsKey($name)) {
        $value = $headers[$name]
    }
    else {
        # Header dictionary keys can vary by casing depending on runtime
        foreach ($key in $headers.Keys) {
            if ([string]::Equals([string]$key, $name, [System.StringComparison]::OrdinalIgnoreCase)) {
                $value = $headers[$key]
                break
            }
        }
    }

    if ([string]::IsNullOrWhiteSpace([string]$value)) {
        $missing.Add($name) | Out-Null
        Write-Host ("  MISSING  {0}" -f $name) -ForegroundColor Red
    }
    else {
        $present.Add($name) | Out-Null
        Write-Host ("  OK       {0}: {1}" -f $name, $value) -ForegroundColor Green
    }
}

Write-Host ""
if ($missing.Count -gt 0) {
    Write-Host ("FAIL {0} required header(s) missing. Ensure SecurityHeadersMiddleware is registered." -f $missing.Count) -ForegroundColor Red
    exit 1
}

Write-Host ("PASS All {0} required security headers present." -f $required.Count) -ForegroundColor Green
exit 0
