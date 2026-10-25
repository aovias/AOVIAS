# RabbitMQ Connection Test Script
# Tests the RabbitMQ connection via the API endpoint

$baseUrl = "https://localhost:7087"
$endpoint = "$baseUrl/api/v1/test/rabbitmq"

Write-Host "Testing RabbitMQ Connection..." -ForegroundColor Cyan
Write-Host "Endpoint: $endpoint" -ForegroundColor Gray
Write-Host ""

try {
    # Skip SSL certificate validation for localhost (development only)
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
    }
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12

    $response = Invoke-RestMethod -Uri $endpoint -Method Get -ContentType "application/json" -ErrorAction Stop

    Write-Host "=== RabbitMQ Connection Test Results ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Status: " -NoNewline
    if ($response.Success) {
        Write-Host "✅ PASSED" -ForegroundColor Green
    } else {
        Write-Host "❌ FAILED" -ForegroundColor Red
    }
    Write-Host "Timestamp: $($response.Timestamp)" -ForegroundColor Gray
    Write-Host "Message: $($response.Message)" -ForegroundColor $(if ($response.Success) { "Green" } else { "Red" })
    Write-Host ""
    Write-Host "Connection Details:" -ForegroundColor Cyan
    Write-Host "  - Connected: $($response.IsConnected)" -ForegroundColor $(if ($response.IsConnected) { "Green" } else { "Red" })
    Write-Host "  - Connection State: $($response.ConnectionState)" -ForegroundColor Gray
    Write-Host "  - Can Create Channel: $($response.CanCreateChannel)" -ForegroundColor $(if ($response.CanCreateChannel) { "Green" } else { "Red" })
    Write-Host ""
    Write-Host "Exchange Status:" -ForegroundColor Cyan
    Write-Host "  - Exchange Exists: $($response.ExchangeExists)" -ForegroundColor $(if ($response.ExchangeExists) { "Green" } else { "Red" })
    if ($response.ExchangeError) {
        Write-Host "  - Error: $($response.ExchangeError)" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "Queue Status:" -ForegroundColor Cyan
    foreach ($queue in $response.QueuesStatus.PSObject.Properties) {
        $status = $queue.Value
        $color = if ($status -eq "Exists") { "Green" } else { "Red" }
        Write-Host "  - $($queue.Name): $status" -ForegroundColor $color
    }
    Write-Host ""
    Write-Host "Publish Test:" -ForegroundColor Cyan
    Write-Host "  - Can Publish: $($response.CanPublish)" -ForegroundColor $(if ($response.CanPublish) { "Green" } else { "Red" })
    if ($response.PublishError) {
        Write-Host "  - Error: $($response.PublishError)" -ForegroundColor Red
    }
    Write-Host ""

    if ($response.Error) {
        Write-Host "Error Details:" -ForegroundColor Red
        Write-Host "  $($response.Error)" -ForegroundColor Red
        Write-Host ""
    }

    if ($response.Success) {
        Write-Host "✅ All tests passed! RabbitMQ is ready." -ForegroundColor Green
        exit 0
    } else {
        Write-Host "❌ Some tests failed. Check the details above." -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "❌ Error calling test endpoint: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Make sure:" -ForegroundColor Yellow
    Write-Host "  1. The ComplianceService is running" -ForegroundColor Yellow
    Write-Host "  2. The endpoint URL is correct: $endpoint" -ForegroundColor Yellow
    Write-Host "  3. RabbitMQ Docker container is running" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}
