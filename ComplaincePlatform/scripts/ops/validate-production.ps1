<#
.SYNOPSIS
  Phase 6/7 production validation checklist for AOVIAS Command (PRAMAN).

.DESCRIPTION
  Prints a structured validation checklist for commercial pilot readiness:
  fresh install, upgrade, rollback, backup/restore, license activation,
  tenant isolation, config validation, and DR smoke.

  Marks each step as AUTO (script can run/verify) or MANUAL (operator drill).
  Optional -RunAutomated executes feasible local checks (unit tests + file presence).
  Optional -RunLoad best-effort runs k6 load scripts when k6 is installed.

.PARAMETER RunAutomated
  When set, runs automated checks that do not require a live production stack.

.PARAMETER SkipTests
  Skip dotnet test when -RunAutomated is set.

.PARAMETER RunLoad
  When set, best-effort runs k6 scripts under tests/load (auth-login + dashboard)
  if k6 is on PATH. Missing k6 or script failure is soft-skipped (non-fatal).

.PARAMETER LoadBaseUrl
  Gateway base URL for -RunLoad (default http://localhost:5000). Also honors env BASE_URL.

.EXAMPLE
  .\scripts\ops\validate-production.ps1
  .\scripts\ops\validate-production.ps1 -RunAutomated
  .\scripts\ops\validate-production.ps1 -RunLoad
  .\scripts\ops\validate-production.ps1 -RunAutomated -RunLoad
#>
[CmdletBinding()]
param(
    [switch]$RunAutomated,
    [switch]$SkipTests,
    [switch]$RunLoad,
    [string]$LoadBaseUrl = ""
)

$ErrorActionPreference = "Stop"
# scripts/ops -> platform root (ComplaincePlatform)
$platformRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
if (-not (Test-Path (Join-Path $platformRoot "OrganizationService"))) {
    throw "Could not resolve ComplaincePlatform root from $PSScriptRoot"
}

$script:PassCount = 0
$script:FailCount = 0
$script:SkipCount = 0

function Write-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host ("=" * 72) -ForegroundColor Cyan
    Write-Host (" {0}" -f $Title) -ForegroundColor Cyan
    Write-Host ("=" * 72) -ForegroundColor Cyan
}

function Write-Step {
    param(
        [Parameter(Mandatory)][ValidateSet("AUTO", "MANUAL")][string]$Mode,
        [Parameter(Mandatory)][string]$Id,
        [Parameter(Mandatory)][string]$Description,
        [string]$HowTo = ""
    )
    $tag = if ($Mode -eq "AUTO") { "AUTO  " } else { "MANUAL" }
    $color = if ($Mode -eq "AUTO") { "Green" } else { "Yellow" }
    Write-Host ("  [{0}] {1}  {2}" -f $tag, $Id, $Description) -ForegroundColor $color
    if ($HowTo) {
        Write-Host ("           -> {0}" -f $HowTo) -ForegroundColor DarkGray
    }
}

function Assert-PathExists {
    param([string]$RelativePath, [string]$Label)
    $full = Join-Path $platformRoot $RelativePath
    if (Test-Path $full) {
        Write-Host ("  OK  {0} ({1})" -f $Label, $RelativePath) -ForegroundColor Green
        $script:PassCount++
        return $true
    }
    Write-Host ("  FAIL {0} missing: {1}" -f $Label, $RelativePath) -ForegroundColor Red
    $script:FailCount++
    return $false
}

Write-Host ""
Write-Host "AOVIAS Command - Phase 6/7 Production Validation Checklist" -ForegroundColor White
Write-Host ("Platform root: {0}" -f $platformRoot)
$modeBits = @("checklist")
if ($RunAutomated) { $modeBits += "automated checks" }
if ($RunLoad) { $modeBits += "k6 load (best-effort)" }
Write-Host ("Mode: {0}" -f ($modeBits -join " + "))
if (-not $RunAutomated -and -not $RunLoad) {
    Write-Host "Pass -RunAutomated and/or -RunLoad to execute AUTO / load steps." -ForegroundColor DarkGray
}
Write-Host ""

# -----------------------------------------------------------------------------
Write-Section "1. Fresh install"
Write-Step -Mode MANUAL -Id "INST-01" -Description "Provision SQL Server and blob/file storage for target environment"
Write-Step -Mode MANUAL -Id "INST-02" -Description "Copy .env / secrets from vault; never commit production secrets"
Write-Step -Mode MANUAL -Id "INST-03" -Description "Run Setup Wizard or compose install profile through DB/admin/org/license steps" -HowTo "Docs: docs/commercial-readiness/phase-6/03-installation.md (when present)"
Write-Step -Mode AUTO -Id "INST-04" -Description "Confirm docker-compose.dev.yml / deploy scripts exist in repo"
Write-Step -Mode MANUAL -Id "INST-05" -Description "Bring up stack; verify Gateway /health and /readiness for core services"
Write-Step -Mode MANUAL -Id "INST-06" -Description "First admin login succeeds; Installation Completed lock prevents re-wizard"

# -----------------------------------------------------------------------------
Write-Section "2. Upgrade"
Write-Step -Mode MANUAL -Id "UPG-01" -Description "Record current package/version and migration watermark"
Write-Step -Mode MANUAL -Id "UPG-02" -Description "Run preflight compatibility check (scripts/ops/upgrade.ps1 when present)"
Write-Step -Mode MANUAL -Id "UPG-03" -Description "Take pre-upgrade backup (see section 4)"
Write-Step -Mode MANUAL -Id "UPG-04" -Description "Apply new packages and EF migrations across Identity, Organization, satellites"
Write-Step -Mode MANUAL -Id "UPG-05" -Description "Post-upgrade health smoke: login, dashboard, one OE evidence read"
Write-Step -Mode AUTO -Id "UPG-06" -Description "Confirm upgrade/runbook docs present when Sprint 4 landed" -HowTo "docs/commercial-readiness/phase-6/05-upgrade-platform.md"

# -----------------------------------------------------------------------------
Write-Section "3. Rollback"
Write-Step -Mode MANUAL -Id "RB-01" -Description "Stop new package set; restore DB backup from UPG-03"
Write-Step -Mode MANUAL -Id "RB-02" -Description "Pin previous package versions / compose image tags"
Write-Step -Mode MANUAL -Id "RB-03" -Description "Restart stack; confirm app version and data integrity"
Write-Step -Mode MANUAL -Id "RB-04" -Description "Document zero data-loss constraints for the drill"

# -----------------------------------------------------------------------------
Write-Section "4. Backup / restore"
Write-Step -Mode MANUAL -Id "BAK-01" -Description "Full SQL backup of Identity, Organization, Compliance, and remaining DBs"
Write-Step -Mode MANUAL -Id "BAK-02" -Description "Snapshot config/.env redacted inventory and evidence blob prefix"
Write-Step -Mode MANUAL -Id "BAK-03" -Description "Restore into clean SQL instance per foundation restore order"
Write-Step -Mode MANUAL -Id "BAK-04" -Description "Validate login, license status, and one journey read after restore"
Write-Step -Mode AUTO -Id "BAK-05" -Description "Confirm backup strategy doc exists" -HowTo "docs/commercial-readiness/foundation/11-backup-recovery.md"

# -----------------------------------------------------------------------------
Write-Section "5. License activation"
Write-Step -Mode MANUAL -Id "LIC-01" -Description "Issue trial/subscription key via Platform Admin or POST /api/v1/licenses/issue"
Write-Step -Mode MANUAL -Id "LIC-02" -Description "Activate online with machine fingerprint and verify Active status"
Write-Step -Mode MANUAL -Id "LIC-03" -Description "Optional: offline activation with signed payload"
Write-Step -Mode MANUAL -Id "LIC-04" -Description "Heartbeat and seat assign; entitlement check for a gated feature"
Write-Step -Mode MANUAL -Id "LIC-05" -Description "Grace drill: expire key, Grace operational, then Expired soft-gate"
Write-Step -Mode AUTO -Id "LIC-06" -Description "Unit tests: LicenseAppService issue/activate/grace (OrganizationService.Tests)"

# -----------------------------------------------------------------------------
Write-Section "6. Tenant isolation"
Write-Step -Mode MANUAL -Id "TEN-01" -Description "Create two orgs; obtain JWTs for each"
Write-Step -Mode MANUAL -Id "TEN-02" -Description "Call org-scoped API with Org A token and Org B route id; expect 403"
Write-Step -Mode MANUAL -Id "TEN-03" -Description "SUPER_ADMIN can cross orgs; hospital-scoped data stays isolated"
Write-Step -Mode AUTO -Id "TEN-04" -Description "Unit tests: OrganizationAccessHandler match/mismatch/super-admin"

# -----------------------------------------------------------------------------
Write-Section "7. Config validation"
Write-Step -Mode MANUAL -Id "CFG-01" -Description "Validate required env: ConnectionStrings, Jwt, InternalApi, Licensing SigningKey"
Write-Step -Mode MANUAL -Id "CFG-02" -Description "Confirm Production profile has empty/placeholder secrets in base appsettings"
Write-Step -Mode MANUAL -Id "CFG-03" -Description "SMTP/storage/AI endpoints reachable or explicitly disabled for pilot"
Write-Step -Mode AUTO -Id "CFG-04" -Description "Confirm .env.example / deployment.metadata.json catalog present"

# -----------------------------------------------------------------------------
Write-Section "8. DR smoke"
Write-Step -Mode MANUAL -Id "DR-01" -Description "Simulate primary SQL loss; restore last good backup to standby"
Write-Step -Mode MANUAL -Id "DR-02" -Description "Repoint connection strings; start Gateway last after satellites healthy"
Write-Step -Mode MANUAL -Id "DR-03" -Description "Smoke: login, license validate, dashboard, evidence read"
Write-Step -Mode MANUAL -Id "DR-04" -Description "Record RPO/RTO achieved vs pilot targets (24h RPO or better)"
Write-Step -Mode AUTO -Id "DR-05" -Description "Confirm DR notes in foundation and phase-6 validation docs"

# -----------------------------------------------------------------------------
Write-Section "9. Enterprise load / security (Phase 7)"
Write-Step -Mode AUTO -Id "LOAD-01" -Description "k6 load scripts present under tests/load" -HowTo "auth-login.js, dashboard.js, search.js, soak.js"
Write-Step -Mode MANUAL -Id "LOAD-02" -Description "Run k6 dashboard/search against demo Gateway with TOKEN" -HowTo "tests/load/README.md or -RunLoad"
Write-Step -Mode MANUAL -Id "CHAOS-01" -Description "Chaos drills: Redis down, AIOrchestrator kill, SQL restart" -HowTo "tests/load/chaos-checklist.md"
Write-Step -Mode AUTO -Id "SEC-01" -Description "Security header scan script" -HowTo "scripts/ops/scan-security-headers.ps1"
Write-Step -Mode MANUAL -Id "SEC-02" -Description "OWASP ASVS lite checklist ticks" -HowTo "tests/security/owasp-asvs-lite-checklist.md"

Write-Host ""
Write-Host "Legend: AUTO = script/repo can verify locally; MANUAL = operator drill on a real stack." -ForegroundColor DarkGray
Write-Host "Honest deferrals: payment gateway, full K8s production, Playwright E2E (see phase-6 completion report)." -ForegroundColor DarkGray
Write-Host "Phase 7 deferrals: multi-region HA, paid pen-test, full blob cutover (see phase-7 completion report)." -ForegroundColor DarkGray

if (-not $RunAutomated -and -not $RunLoad) {
    Write-Host ""
    Write-Host "Checklist printed. Re-run with -RunAutomated and/or -RunLoad." -ForegroundColor Cyan
    exit 0
}

if ($RunAutomated) {
    # -----------------------------------------------------------------------------
    Write-Section "Automated checks (-RunAutomated)"

    [void](Assert-PathExists "docker-compose.dev.yml" "Dev compose file")
    [void](Assert-PathExists "docs\commercial-readiness\foundation\11-backup-recovery.md" "Backup strategy doc")
    [void](Assert-PathExists "docs\commercial-readiness\phase-6\01-licensing-platform.md" "Licensing doc")
    [void](Assert-PathExists "docs\commercial-readiness\phase-6\09-production-validation.md" "Production validation doc")
    [void](Assert-PathExists "docs\commercial-readiness\phase-6\10-phase-6-completion-report.md" "Phase 6 completion report")
    [void](Assert-PathExists "OrganizationService\Application\Services\LicenseAppService.cs" "LicenseAppService")
    [void](Assert-PathExists "Gateway\Authorization\OrganizationAccessHandler.cs" "Tenant OrgAccess handler")
    [void](Assert-PathExists "Gateway\deployment.metadata.json" "Gateway deployment metadata")
    [void](Assert-PathExists "tests\load\auth-login.js" "k6 auth-login script")
    [void](Assert-PathExists "tests\load\dashboard.js" "k6 dashboard script")
    [void](Assert-PathExists "tests\load\search.js" "k6 search script")
    [void](Assert-PathExists "tests\load\soak.js" "k6 soak script")
    [void](Assert-PathExists "tests\load\chaos-checklist.md" "Chaos checklist")
    [void](Assert-PathExists "tests\security\owasp-asvs-lite-checklist.md" "ASVS lite checklist")
    [void](Assert-PathExists "scripts\ops\scan-security-headers.ps1" "Security header scan")
    [void](Assert-PathExists "Gateway\Middleware\SecurityHeadersMiddleware.cs" "SecurityHeadersMiddleware")

    $envExampleCandidates = @(
        ".env.example",
        "docker\.env.example"
    )
    $envFound = $false
    foreach ($c in $envExampleCandidates) {
        $full = Join-Path $platformRoot $c
        if (Test-Path $full) {
            Write-Host ("  OK  Env example found ({0})" -f $full) -ForegroundColor Green
            $script:PassCount++
            $envFound = $true
            break
        }
    }
    if (-not $envFound) {
        Write-Host "  SKIP No .env.example located (non-blocking for unit validation)" -ForegroundColor Yellow
        $script:SkipCount++
    }

    $phase6Docs = @(
        @{ Rel = "docs\commercial-readiness\phase-6\02-multi-tenant-platform.md"; Required = $false },
        @{ Rel = "docs\commercial-readiness\phase-6\03-installation.md"; Required = $false },
        @{ Rel = "docs\commercial-readiness\phase-6\04-configuration-platform.md"; Required = $false },
        @{ Rel = "docs\commercial-readiness\phase-6\05-upgrade-platform.md"; Required = $false },
        @{ Rel = "docs\commercial-readiness\phase-6\06-backup-recovery.md"; Required = $false },
        @{ Rel = "docs\commercial-readiness\phase-6\07-commercial-operations.md"; Required = $false },
        @{ Rel = "docs\commercial-readiness\phase-6\08-deployment-automation.md"; Required = $false }
    )
    Write-Host ""
    Write-Host "  Phase-6 docs 02-08 presence (informational):" -ForegroundColor DarkCyan
    foreach ($doc in $phase6Docs) {
        $full = Join-Path $platformRoot $doc.Rel
        if (Test-Path $full) {
            Write-Host ("    PRESENT  {0}" -f $doc.Rel) -ForegroundColor Green
        }
        else {
            Write-Host ("    MISSING  {0}  (Sprint 2-5 may still be landing)" -f $doc.Rel) -ForegroundColor Yellow
            $script:SkipCount++
        }
    }

    $phase7Docs = @(
        "docs\commercial-readiness\phase-7\01-enterprise-security.md",
        "docs\commercial-readiness\phase-7\02-performance.md",
        "docs\commercial-readiness\phase-7\03-scalability.md",
        "docs\commercial-readiness\phase-7\04-observability.md",
        "docs\commercial-readiness\phase-7\05-reliability.md",
        "docs\commercial-readiness\phase-7\06-enterprise-operations.md",
        "docs\commercial-readiness\phase-7\07-enterprise-testing.md",
        "docs\commercial-readiness\phase-7\08-phase-7-completion-report.md"
    )
    Write-Host ""
    Write-Host "  Phase-7 docs 01-08 presence (informational):" -ForegroundColor DarkCyan
    foreach ($rel in $phase7Docs) {
        $full = Join-Path $platformRoot $rel
        if (Test-Path $full) {
            Write-Host ("    PRESENT  {0}" -f $rel) -ForegroundColor Green
        }
        else {
            Write-Host ("    MISSING  {0}" -f $rel) -ForegroundColor Yellow
            $script:SkipCount++
        }
    }

    if (-not $SkipTests) {
        Write-Host ""
        Write-Host "  Running focused unit tests..." -ForegroundColor DarkCyan
        Push-Location $platformRoot
        try {
            $orgTests = Join-Path $platformRoot "OrganizationService.Tests\OrganizationService.Tests.csproj"
            $gwTests = Join-Path $platformRoot "tests\Gateway.UnitTests\Gateway.UnitTests.csproj"
            $outOrg = Join-Path $platformRoot "_build_verify_phase6\validate_org"
            $outGw = Join-Path $platformRoot "_build_verify_phase6\validate_gw"

            if (Test-Path $orgTests) {
                & dotnet test $orgTests --filter "FullyQualifiedName~LicenseAppService" -o $outOrg --verbosity quiet
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "  OK  OrganizationService.Tests (LicenseAppService filter)" -ForegroundColor Green
                    $script:PassCount++
                }
                else {
                    Write-Host "  FAIL OrganizationService.Tests (LicenseAppService filter)" -ForegroundColor Red
                    $script:FailCount++
                }
            }
            if (Test-Path $gwTests) {
                & dotnet test $gwTests --filter "FullyQualifiedName~OrganizationAccessHandler" -o $outGw --verbosity quiet
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "  OK  Gateway.UnitTests (OrganizationAccessHandler filter)" -ForegroundColor Green
                    $script:PassCount++
                }
                else {
                    Write-Host "  FAIL Gateway.UnitTests (OrganizationAccessHandler filter)" -ForegroundColor Red
                    $script:FailCount++
                }
            }
        }
        finally {
            Pop-Location
        }
    }
    else {
        Write-Host "  SKIP unit tests (-SkipTests)" -ForegroundColor Yellow
        $script:SkipCount++
    }

    Write-Host ""
    Write-Host ("Automated summary: {0} pass, {1} fail, {2} skip/missing-optional" -f $script:PassCount, $script:FailCount, $script:SkipCount)
}

if ($RunLoad) {
    # -----------------------------------------------------------------------------
    Write-Section "Load checks (-RunLoad, best-effort)"

    $k6Cmd = Get-Command k6 -ErrorAction SilentlyContinue
    if (-not $k6Cmd) {
        Write-Host "  SKIP k6 not installed / not on PATH (install from https://k6.io)" -ForegroundColor Yellow
        $script:SkipCount++
    }
    else {
        $base = $LoadBaseUrl
        if ([string]::IsNullOrWhiteSpace($base)) {
            $base = if ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5000" }
        }
        $env:BASE_URL = $base.TrimEnd("/")
        Write-Host ("  Using BASE_URL={0}" -f $env:BASE_URL) -ForegroundColor DarkCyan
        if ($env:TOKEN) {
            Write-Host "  TOKEN is set (authenticated dashboard paths enabled)" -ForegroundColor DarkGray
        }
        else {
            Write-Host "  TOKEN unset - dashboard.js will smoke /health only for auth paths" -ForegroundColor DarkGray
        }

        $scripts = @(
            "tests\load\auth-login.js",
            "tests\load\dashboard.js"
        )
        Push-Location $platformRoot
        try {
            foreach ($rel in $scripts) {
                $full = Join-Path $platformRoot $rel
                if (-not (Test-Path $full)) {
                    Write-Host ("  SKIP missing {0}" -f $rel) -ForegroundColor Yellow
                    $script:SkipCount++
                    continue
                }
                Write-Host ("  Running k6 {0} ..." -f $rel) -ForegroundColor DarkCyan
                & k6 run $full
                if ($LASTEXITCODE -eq 0) {
                    Write-Host ("  OK  k6 {0}" -f $rel) -ForegroundColor Green
                    $script:PassCount++
                }
                else {
                    # Best-effort: do not fail the whole validation run if Gateway is down / thresholds miss.
                    Write-Host ("  SOFT-FAIL k6 {0} exit={1} (non-fatal for -RunLoad)" -f $rel, $LASTEXITCODE) -ForegroundColor Yellow
                    $script:SkipCount++
                }
            }
        }
        finally {
            Pop-Location
        }
    }

    $scanScript = Join-Path $platformRoot "scripts\ops\scan-security-headers.ps1"
    if (Test-Path $scanScript) {
        Write-Host ""
        Write-Host "  Running security header scan (soft-fail if Gateway down)..." -ForegroundColor DarkCyan
        $scanBase = if (-not [string]::IsNullOrWhiteSpace($LoadBaseUrl)) { $LoadBaseUrl } elseif ($env:BASE_URL) { $env:BASE_URL } else { "http://localhost:5000" }
        & $scanScript -BaseUrl $scanBase
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  OK  scan-security-headers.ps1" -ForegroundColor Green
            $script:PassCount++
        }
        else {
            Write-Host "  FAIL scan-security-headers.ps1 (Gateway up but headers missing)" -ForegroundColor Red
            $script:FailCount++
        }
    }
}

Write-Host ""
Write-Host ("Validation summary: {0} pass, {1} fail, {2} skip/soft" -f $script:PassCount, $script:FailCount, $script:SkipCount)
if ($script:FailCount -gt 0) {
    exit 1
}
exit 0
