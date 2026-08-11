$ErrorActionPreference = 'Stop'

$gw = 'http://localhost:5212'
$id = 'https://localhost:7135'
$corr = 'd0-http-' + [guid]::NewGuid().ToString('N')
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

Write-Output "step:token-a"
$tokAResp = Invoke-RestMethod -Method Post -Uri "$id/internal/token/generate" -ContentType 'application/json' -Body (@{
    userId = '40bfde8b-fb22-4aa7-98aa-e667bb386c3c'
    email = 'd0httpa@example.com'
    role = 'ORG_ADMIN'
    organizationId = '11111111-1111-1111-1111-111111111111'
    hospitalId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
} | ConvertTo-Json) -TimeoutSec 20

Write-Output "step:token-b"
$tokBResp = Invoke-RestMethod -Method Post -Uri "$id/internal/token/generate" -ContentType 'application/json' -Body (@{
    userId = 'bbcbfdd4-3506-4f19-8fba-4a0bf4268108'
    email = 'd0httpb@example.com'
    role = 'ORG_ADMIN'
    organizationId = '22222222-2222-2222-2222-222222222222'
    hospitalId = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
} | ConvertTo-Json) -TimeoutSec 20

$tokenA = 'Bearer ' + $tokAResp.token
$tokenB = 'Bearer ' + $tokBResp.token

$startBody = @{
    goal = 'AuditPreparationWorkflow'
    aiRequest = @{
        prompt = 'Prepare audit plan'
        useCase = 5
        hospitalId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
        contextVersionId = 'ctx-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-v1'
        promptTemplateId = 'TaskRecommendation'
        promptTemplateVersion = '1.0'
    }
} | ConvertTo-Json -Depth 6

$assistBody = @{
    prompt = 'Give me recommendation'
    useCase = 5
    hospitalId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'
    contextVersionId = 'ctx-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa-v1'
    promptTemplateId = 'TaskRecommendation'
    promptTemplateVersion = '1.0'
} | ConvertTo-Json -Depth 6

$hA = @{ Authorization = $tokenA; 'X-Correlation-Id' = $corr }
$hB = @{ Authorization = $tokenB; 'X-Correlation-Id' = $corr }

$start1 = Invoke-WebRequest -Method Post -Uri "$gw/api/v1/ai/workflows/start" -Headers $hA -ContentType 'application/json' -Body $startBody -TimeoutSec 40
Write-Output "step:start1"
$start2 = Invoke-WebRequest -Method Post -Uri "$gw/api/v1/ai/workflows/start" -Headers $hA -ContentType 'application/json' -Body $startBody -TimeoutSec 40
Write-Output "step:start2"
$exec1 = ($start1.Content | ConvertFrom-Json).executionId
$exec2 = ($start2.Content | ConvertFrom-Json).executionId

$assist = Invoke-WebRequest -Method Post -Uri "$gw/api/v1/ai/assist" -Headers $hA -ContentType 'application/json' -Body $assistBody -TimeoutSec 40
Write-Output "step:assist"
$assistJson = $assist.Content | ConvertFrom-Json

$approveResp = Invoke-WebRequest -Method Post -Uri "$gw/api/v1/ai/workflows/$exec1/approve-step" -Headers $hA -ContentType 'application/json' -Body '{"reason":"approved-http-matrix"}' -TimeoutSec 40
Write-Output "step:approve"
$rejectResp = Invoke-WebRequest -Method Post -Uri "$gw/api/v1/ai/workflows/$exec2/reject-step" -Headers $hA -ContentType 'application/json' -Body '{"reason":"reject-http-matrix"}' -TimeoutSec 40
Write-Output "step:reject"

$crossTenant = 'blocked'
try {
    Invoke-WebRequest -Method Post -Uri "$gw/api/v1/ai/workflows/$exec1/approve-step" -Headers $hB -ContentType 'application/json' -Body '{"reason":"cross-tenant-attempt"}' -ErrorAction Stop -TimeoutSec 40 | Out-Null
    $crossTenant = 'allowed'
}
catch {
    $crossTenant = 'blocked'
}

$startTimeout = Invoke-WebRequest -Method Post -Uri "$gw/api/v1/ai/workflows/start" -Headers $hA -ContentType 'application/json' -Body $startBody -TimeoutSec 40
Write-Output "step:start-timeout"
$execTimeout = ($startTimeout.Content | ConvertFrom-Json).executionId
Start-Sleep -Seconds 2
$timeoutApprove = Invoke-WebRequest -Method Post -Uri "$gw/api/v1/ai/workflows/$execTimeout/approve-step" -Headers $hA -ContentType 'application/json' -Body '{"reason":"late-approval"}' -TimeoutSec 40
Write-Output "step:timeout-approve"

$get1 = Invoke-WebRequest -Method Get -Uri "$gw/api/v1/ai/workflows/$exec1" -Headers $hA -TimeoutSec 40
$get2 = Invoke-WebRequest -Method Get -Uri "$gw/api/v1/ai/workflows/$exec2" -Headers $hA -TimeoutSec 40
$getTimeout = Invoke-WebRequest -Method Get -Uri "$gw/api/v1/ai/workflows/$execTimeout" -Headers $hA -TimeoutSec 40

$result = [ordered]@{
    correlationId = $corr
    executions = @{
        first = $exec1
        retry = $exec2
        timeout = $execTimeout
    }
    idempotencyRetryDifferentExecution = ($exec1 -ne $exec2)
    http = @{
        start1 = $start1.StatusCode
        start2 = $start2.StatusCode
        assist = $assist.StatusCode
        approve = $approveResp.StatusCode
        reject = $rejectResp.StatusCode
        timeoutApprove = $timeoutApprove.StatusCode
    }
    crossTenantApproval = $crossTenant
    statuses = @{
        first = (($get1.Content | ConvertFrom-Json).status)
        retry = (($get2.Content | ConvertFrom-Json).status)
        timeout = (($getTimeout.Content | ConvertFrom-Json).status)
    }
    assistSerialization = @{
        hasConfidence = ($null -ne $assistJson.confidenceScore)
        hasReasoning = ($assistJson.reasoning.Count -gt 0)
        hasSources = ($assistJson.sourceReferences.Count -gt 0)
    }
}

$result | ConvertTo-Json -Depth 10
