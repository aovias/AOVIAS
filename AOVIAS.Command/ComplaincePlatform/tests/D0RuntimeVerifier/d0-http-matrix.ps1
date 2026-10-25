$ErrorActionPreference = 'Stop'

$gw = 'http://localhost:5212'
$id = 'http://localhost:5062'
$corr = 'd0-http-' + [guid]::NewGuid().ToString('N')

$orgA = [guid]::NewGuid()
$hospA = [guid]::NewGuid()
$orgB = [guid]::NewGuid()
$hospB = [guid]::NewGuid()

$emailA = 'd0a_' + ([guid]::NewGuid().ToString('N').Substring(0, 8)) + '@example.com'
$emailB = 'd0b_' + ([guid]::NewGuid().ToString('N').Substring(0, 8)) + '@example.com'
$pwd = 'P@ssw0rd!1'

$headers = @{ 'X-Correlation-Id' = $corr }

Invoke-RestMethod -Method Post -Uri "$id/internal/users/register" -Headers $headers -ContentType 'application/json' -Body (@{ email = $emailA; password = $pwd; role = 'admin' } | ConvertTo-Json)
Invoke-RestMethod -Method Post -Uri "$id/internal/users/register" -Headers $headers -ContentType 'application/json' -Body (@{ email = $emailB; password = $pwd; role = 'admin' } | ConvertTo-Json)

$userA = Invoke-RestMethod -Method Post -Uri "$id/api/v1/auth/login" -Headers $headers -ContentType 'application/json' -Body (@{ email = $emailA; password = $pwd } | ConvertTo-Json)
$userB = Invoke-RestMethod -Method Post -Uri "$id/api/v1/auth/login" -Headers $headers -ContentType 'application/json' -Body (@{ email = $emailB; password = $pwd } | ConvertTo-Json)
$subA = $userA.data.userId
$subB = $userB.data.userId

$genA = Invoke-RestMethod -Method Post -Uri "$id/internal/token/generate" -Headers $headers -ContentType 'application/json' -Body (@{ userId = $subA; email = $emailA; organizationId = $orgA; hospitalId = $hospA; role = 'admin' } | ConvertTo-Json)
$genB = Invoke-RestMethod -Method Post -Uri "$id/internal/token/generate" -Headers $headers -ContentType 'application/json' -Body (@{ userId = $subB; email = $emailB; organizationId = $orgB; hospitalId = $hospB; role = 'admin' } | ConvertTo-Json)
$bearerA = 'Bearer ' + $genA.token
$bearerB = 'Bearer ' + $genB.token

$startBody = @{
    goal = 'AuditPreparationWorkflow'
    aiRequest = @{
        prompt = 'Prepare audit plan'
        useCase = 5
        hospitalId = $hospA
        contextVersionId = ('ctx-' + $hospA.ToString('N') + '-v1')
        promptTemplateId = 'TaskRecommendation'
        promptTemplateVersion = '1.0'
    }
} | ConvertTo-Json -Depth 6

$hA = @{ Authorization = $bearerA; 'X-Correlation-Id' = $corr }
$hB = @{ Authorization = $bearerB; 'X-Correlation-Id' = $corr }

$start1 = Invoke-WebRequest -Method Post -Uri "$gw/api/v1/ai/workflows/start" -Headers $hA -ContentType 'application/json' -Body $startBody
$start2 = Invoke-WebRequest -Method Post -Uri "$gw/api/v1/ai/workflows/start" -Headers $hA -ContentType 'application/json' -Body $startBody
$exec1 = ($start1.Content | ConvertFrom-Json).executionId
$exec2 = ($start2.Content | ConvertFrom-Json).executionId

$assistBody = @{
    prompt = 'Give me recommendation'
    useCase = 5
    hospitalId = $hospA
    contextVersionId = ('ctx-' + $hospA.ToString('N') + '-v1')
    promptTemplateId = 'TaskRecommendation'
    promptTemplateVersion = '1.0'
} | ConvertTo-Json
$assist = Invoke-WebRequest -Method Post -Uri "$gw/api/v1/ai/assist" -Headers $hA -ContentType 'application/json' -Body $assistBody
$assistJson = $assist.Content | ConvertFrom-Json

$approveBody = @{ reason = 'approved-http-matrix' } | ConvertTo-Json
$rejectBody = @{ reason = 'reject-http-matrix' } | ConvertTo-Json

$approveResp = Invoke-WebRequest -Method Post -Uri "$gw/api/v1/ai/workflows/$exec1/approve-step" -Headers $hA -ContentType 'application/json' -Body $approveBody
$rejectResp = Invoke-WebRequest -Method Post -Uri "$gw/api/v1/ai/workflows/$exec2/reject-step" -Headers $hA -ContentType 'application/json' -Body $rejectBody

$crossTenantStatus = 'not-run'
try {
    Invoke-WebRequest -Method Post -Uri "$gw/api/v1/ai/workflows/$exec1/approve-step" -Headers $hB -ContentType 'application/json' -Body $approveBody -ErrorAction Stop | Out-Null
    $crossTenantStatus = 'allowed'
}
catch {
    $crossTenantStatus = 'blocked'
}

$startTimeoutBody = @{
    goal = 'AuditPreparationWorkflow'
    aiRequest = @{
        prompt = 'Timeout flow'
        useCase = 5
        hospitalId = $hospA
        contextVersionId = ('ctx-' + $hospA.ToString('N') + '-v1')
        promptTemplateId = 'TaskRecommendation'
        promptTemplateVersion = '1.0'
    }
} | ConvertTo-Json -Depth 6
$startTimeout = Invoke-WebRequest -Method Post -Uri "$gw/api/v1/ai/workflows/start" -Headers $hA -ContentType 'application/json' -Body $startTimeoutBody
$execTimeout = ($startTimeout.Content | ConvertFrom-Json).executionId
Start-Sleep -Seconds 2
$timeoutApprove = Invoke-WebRequest -Method Post -Uri "$gw/api/v1/ai/workflows/$execTimeout/approve-step" -Headers $hA -ContentType 'application/json' -Body $approveBody

$get1 = Invoke-WebRequest -Method Get -Uri "$gw/api/v1/ai/workflows/$exec1" -Headers $hA
$get2 = Invoke-WebRequest -Method Get -Uri "$gw/api/v1/ai/workflows/$exec2" -Headers $hA
$getTimeout = Invoke-WebRequest -Method Get -Uri "$gw/api/v1/ai/workflows/$execTimeout" -Headers $hA

$result = [ordered]@{
    correlationId = $corr
    executions = @{
        first = $exec1
        retry = $exec2
        timeout = $execTimeout
    }
    idempotencyRetryDifferentExecution = ($exec1 -ne $exec2)
    approveStatusCode = $approveResp.StatusCode
    rejectStatusCode = $rejectResp.StatusCode
    timeoutApproveStatusCode = $timeoutApprove.StatusCode
    crossTenantApproval = $crossTenantStatus
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
