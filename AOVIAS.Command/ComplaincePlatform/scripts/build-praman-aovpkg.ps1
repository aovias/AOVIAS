# Builds PRAMAN .aovpkg workloads using the AOVIAS Package Builder.
param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$platformDir = Split-Path $PSScriptRoot -Parent
$repoRoot = Split-Path (Split-Path $platformDir -Parent) -Parent
$infraConfigRoot = Join-Path $repoRoot "AOVIAS.Infrastructure\aovias-config"
$legacyConfigRoot = Join-Path $repoRoot "aovias-config"
if (-not (Test-Path $infraConfigRoot) -and -not (Test-Path $legacyConfigRoot)) {
    throw "Could not locate AOVIAS configuration directory under repository root '$repoRoot'. Expected '$infraConfigRoot' or '$legacyConfigRoot'."
}
if (-not (Test-Path $platformDir)) {
    throw "Could not locate ComplaincePlatform platform directory at '$platformDir'."
}
$configRoot = if (Test-Path $infraConfigRoot) { $infraConfigRoot } elseif (Test-Path $legacyConfigRoot) { $legacyConfigRoot } else { $infraConfigRoot }
$packagesRoot = Join-Path $configRoot "storage\local\packages\praman"
$publishRoot = Join-Path $platformDir ".publish"

$services = @(
    @{ Project = "Gateway\Gateway.csproj"; ServiceId = "praman-gateway"; DisplayName = "PRAMAN Gateway"; PackageId = "PKG-PRAMAN-GATEWAY" },
    @{ Project = "IdentityService\IdentityService.csproj"; ServiceId = "praman-identity"; DisplayName = "PRAMAN Identity"; PackageId = "PKG-PRAMAN-IDENTITY" },
    @{ Project = "OrganizationService\OrganizationService.csproj"; ServiceId = "praman-organization"; DisplayName = "PRAMAN Organization"; PackageId = "PKG-PRAMAN-ORGANIZATION" },
    @{ Project = "ComplianceService\ComplianceService.csproj"; ServiceId = "praman-compliance"; DisplayName = "PRAMAN Compliance"; PackageId = "PKG-PRAMAN-COMPLIANCE" },
    @{ Project = "AuditService\AuditService.csproj"; ServiceId = "praman-audit"; DisplayName = "PRAMAN Audit"; PackageId = "PKG-PRAMAN-AUDIT" },
    @{ Project = "KnowledgeBase\KnowledgeBase.csproj"; ServiceId = "praman-knowledgebase"; DisplayName = "PRAMAN Knowledge Base"; PackageId = "PKG-PRAMAN-KNOWLEDGEBASE" },
    @{ Project = "AIOrchestratorService\AIOrchestratorService.csproj"; ServiceId = "praman-ai-orchestrator"; DisplayName = "PRAMAN AI Orchestrator"; PackageId = "PKG-PRAMAN-AI" }
)

Write-Host "Publishing PRAMAN services..."
foreach ($service in $services) {
    $projectPath = Join-Path $platformDir $service.Project
    $outputPath = Join-Path $publishRoot $service.ServiceId
    if (Test-Path $outputPath) { Remove-Item $outputPath -Recurse -Force }
    dotnet publish $projectPath -c $Configuration -o $outputPath
    if ($LASTEXITCODE -ne 0) { throw "Publish failed for $($service.ServiceId)" }
}

Write-Host "PRAMAN publish complete. Use Deployment Center to build .aovpkg via AOVIAS Package Builder for each folder under $publishRoot."
