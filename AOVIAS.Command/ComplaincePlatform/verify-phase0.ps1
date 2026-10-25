# Phase 0 Verification Script
# Tests: Create Program -> Load Controls -> Assign Tasks -> Restart -> Verify Persistence

$ErrorActionPreference = "Stop"

Write-Host "=== Phase 0 Verification: ComplianceService Persistence ===" -ForegroundColor Cyan
Write-Host ""

# Configuration
$baseUrl = "https://localhost:7001"  # Adjust if needed
$orgId = [guid]::NewGuid()
$regulation = "GDPR"

# Skip SSL certificate validation for local testing
if (-not ([System.Management.Automation.PSTypeName]'TrustAllCertsPolicy').Type) {
    Add-Type @"
        using System.Net;
        using System.Security.Cryptography.X509Certificates;
        public class TrustAllCertsPolicy : ICertificatePolicy {
            public bool CheckValidationResult(
                ServicePoint srvPoint, X509Certificate certificate,
                WebRequest request, int certificateProblem) {
                return true;
            }
        }
"@
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
}

function Invoke-ApiPost {
    param(
        [string]$Url,
        [object]$Body
    )
    
    $jsonBody = $Body | ConvertTo-Json
    Write-Host "POST $Url" -ForegroundColor Yellow
    Write-Host "Body: $jsonBody" -ForegroundColor Gray
    
    try {
        $response = Invoke-RestMethod -Uri $Url -Method Post -Body $jsonBody -ContentType "application/json" -Headers @{
            "Authorization" = "Bearer test-token"  # Adjust if auth is required
        }
        return $response
    }
    catch {
        Write-Host "Error: $_" -ForegroundColor Red
        Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Red
        throw
    }
}

function Invoke-ApiGet {
    param(
        [string]$Url
    )
    
    Write-Host "GET $Url" -ForegroundColor Yellow
    
    try {
        $response = Invoke-RestMethod -Uri $Url -Method Get -Headers @{
            "Authorization" = "Bearer test-token"  # Adjust if auth is required
        }
        return $response
    }
    catch {
        Write-Host "Error: $_" -ForegroundColor Red
        throw
    }
}

# Step 1: Create ComplianceProgram
Write-Host "Step 1: Creating ComplianceProgram..." -ForegroundColor Green
$createProgramBody = @{
    Regulation = $regulation
    OrganizationId = $orgId
}
$programResponse = Invoke-ApiPost -Url "$baseUrl/api/v1/compliance/programs" -Body $createProgramBody
$programId = $programResponse.ProgramId
Write-Host "Created Program ID: $programId" -ForegroundColor Green
Write-Host ""

# Step 2: Load Controls
Write-Host "Step 2: Loading Controls..." -ForegroundColor Green
$loadControlsBody = @{
    Regulation = $regulation
}
$controlsResponse = Invoke-ApiPost -Url "$baseUrl/api/v1/compliance/programs/$programId/controls" -Body $loadControlsBody
Write-Host "Loaded Controls Count: $($controlsResponse.Count)" -ForegroundColor Green
Write-Host ""

# Step 3: Assign Tasks
Write-Host "Step 3: Assigning Tasks..." -ForegroundColor Green
$assignTasksBody = @{
    OrganizationId = $orgId
}
$tasksResponse = Invoke-ApiPost -Url "$baseUrl/api/v1/compliance/programs/$programId/tasks" -Body $assignTasksBody
Write-Host "Assigned Tasks Count: $($tasksResponse.Count)" -ForegroundColor Green
Write-Host ""

# Step 4: Verify data before restart
Write-Host "Step 4: Verifying data before restart..." -ForegroundColor Green
$programBefore = Invoke-ApiGet -Url "$baseUrl/api/v1/compliance/programs/$programId"
$controlsBefore = Invoke-ApiGet -Url "$baseUrl/api/v1/compliance/programs/$programId/controls"
Write-Host "Program Name: $($programBefore.Name)" -ForegroundColor Green
Write-Host "Controls Count: $($controlsBefore.Count)" -ForegroundColor Green
Write-Host ""

# Step 5: Restart ComplianceService
Write-Host "Step 5: Restarting ComplianceService..." -ForegroundColor Yellow
Write-Host "Please restart the ComplianceService manually, then press Enter to continue..."
Read-Host

# Step 6: Verify data after restart
Write-Host "Step 6: Verifying data after restart..." -ForegroundColor Green
$programAfter = Invoke-ApiGet -Url "$baseUrl/api/v1/compliance/programs/$programId"
$controlsAfter = Invoke-ApiGet -Url "$baseUrl/api/v1/compliance/programs/$programId/controls"
Write-Host "Program Name: $($programAfter.Name)" -ForegroundColor Green
Write-Host "Controls Count: $($controlsAfter.Count)" -ForegroundColor Green
Write-Host ""

# Verification Results
Write-Host "=== Verification Results ===" -ForegroundColor Cyan
$programPersisted = $programBefore.Id -eq $programAfter.Id
$controlsPersisted = $controlsBefore.Count -eq $controlsAfter.Count

if ($programPersisted) {
    Write-Host "✓ Program persisted correctly" -ForegroundColor Green
} else {
    Write-Host "✗ Program data lost after restart" -ForegroundColor Red
}

if ($controlsPersisted) {
    Write-Host "✓ Controls persisted correctly" -ForegroundColor Green
} else {
    Write-Host "✗ Controls data lost after restart" -ForegroundColor Red
}

Write-Host ""
Write-Host "Program ID: $programId" -ForegroundColor Cyan
Write-Host "Organization ID: $orgId" -ForegroundColor Cyan
Write-Host ""
Write-Host "=== Verification Complete ===" -ForegroundColor Cyan
