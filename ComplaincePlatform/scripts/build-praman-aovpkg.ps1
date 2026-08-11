# Builds PRAMAN .aovpkg workloads using the AOVIAS Package Builder.
param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$packagesRoot = Join-Path $repoRoot "aovias-config\storage\local\packages\praman"
$publishRoot = Join-Path $repoRoot "ComplaincePlatform\.publish"

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
    $projectPath = Join-Path $repoRoot "ComplaincePlatform\$($service.Project)"
    $outputPath = Join-Path $publishRoot $service.ServiceId
    if (Test-Path $outputPath) { Remove-Item $outputPath -Recurse -Force }
    dotnet publish $projectPath -c $Configuration -o $outputPath
    if ($LASTEXITCODE -ne 0) { throw "Publish failed for $($service.ServiceId)" }
}

Write-Host "PRAMAN publish complete. Use Deployment Center to build .aovpkg via AOVIAS Package Builder for each folder under $publishRoot."
