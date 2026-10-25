<#
.SYNOPSIS
  Deploy AOVIAS commercial stack via existing docker-compose plane.

.DESCRIPTION
  Validates environment, then brings up docker-compose.dev.yml + docker-compose.commercial.yml.
#>
[CmdletBinding()]
param(
    [switch]$Build,
    [switch]$Detach,
    [string]$ProjectName = "aovias"
)

$ErrorActionPreference = "Stop"
$platformRoot = Split-Path $PSScriptRoot -Parent
Set-Location $platformRoot

# Load .env into process env for validation (compose also reads .env natively)
$envFile = Join-Path $platformRoot ".env"
if (Test-Path $envFile) {
    Get-Content $envFile | ForEach-Object {
        if ($_ -match '^\s*([^#=]+)=(.*)$') {
            $name = $matches[1].Trim()
            $val = $matches[2].Trim().Trim('"').Trim("'")
            if (-not [Environment]::GetEnvironmentVariable($name, "Process")) {
                [Environment]::SetEnvironmentVariable($name, $val, "Process")
            }
        }
    }
}

$opsValidate = Join-Path $PSScriptRoot "ops\Validate-Environment.ps1"
if (Test-Path $opsValidate) {
    & $opsValidate
    if ($LASTEXITCODE -ne 0) {
        throw "Environment validation failed. Set SA_PASSWORD in .env and retry."
    }
} elseif (-not $env:SA_PASSWORD) {
    throw "SA_PASSWORD is required (export or set in .env)."
}

$composeArgs = @(
    "-f", "docker-compose.dev.yml",
    "-f", "docker-compose.commercial.yml",
    "-p", $ProjectName,
    "up"
)
if ($Detach) { $composeArgs += "-d" }
if ($Build) { $composeArgs += "--build" }

Write-Host "Deploying AOVIAS: docker compose $($composeArgs -join ' ')"
docker compose @composeArgs
if ($LASTEXITCODE -ne 0) {
    throw "docker compose failed with exit $LASTEXITCODE"
}

Write-Host @"

Deploy started.
  Gateway:  http://localhost:5000  (https://localhost:7216)
  Identity: https://localhost:7135
  Org:      https://localhost:7181
  Compliance: https://localhost:7087
  AI:       https://localhost:7097

Ops:
  .\scripts\ops\Validate-Environment.ps1
  .\scripts\ops\backup-databases.ps1
  .\scripts\ops\upgrade.ps1
"@
