<#
.SYNOPSIS
  Read-only completion check / report helper for an agent worktree.
.DESCRIPTION
  Reports branch, status, diff summary, and recent commits.
  Optional -BuildStatus / -TestStatus are recorded only (not executed).
  Never commits or merges.
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory = $true)]
  [ValidateSet('cursor', 'claude')]
  [string]$Agent,

  [string]$MainPath = 'V:\DevelopmentRepo\AOVIAS',
  [string]$CursorPath = 'V:\DevelopmentRepo\AOVIAS.Cursor',
  [string]$ClaudePath = 'V:\DevelopmentRepo\AOVIAS.Claude',
  [string]$BuildStatus = 'NOT_SUPPLIED',
  [string]$TestStatus = 'NOT_SUPPLIED'
)

$ErrorActionPreference = 'Stop'
$failed = $false

$map = @{
  cursor = $CursorPath
  claude = $ClaudePath
}
$path = $map[$Agent]

Write-Host ('=== Agent Completion Check (' + $Agent + ') - read-only ===')

if (-not (Test-Path -LiteralPath $path)) {
  Write-Host ('[FAIL] Worktree missing: ' + $path)
  exit 1
}

$branch = git -C $path branch --show-current
$head = git -C $path rev-parse HEAD
$porc = @(git -C $path status --porcelain)
$statusShort = (git -C $path status --short --branch | Out-String).TrimEnd()
$diffStat = (git -C $path diff --stat | Out-String).TrimEnd()
$cachedStat = (git -C $path diff --cached --stat | Out-String).TrimEnd()
$log = (git -C $path log --oneline -5 | Out-String).TrimEnd()
$ahead = (git -C $path log --oneline main..HEAD 2>$null | Out-String).TrimEnd()

$wtLabel = if ($porc.Count -eq 0) { 'CLEAN' } else { 'DIRTY (' + $porc.Count + ')' }
Write-Host ('Worktree: ' + $path)
Write-Host ('Branch:   ' + $branch)
Write-Host ('HEAD:     ' + $head)
Write-Host ('Status:   ' + $wtLabel)
Write-Host ('BuildStatus (supplied): ' + $BuildStatus)
Write-Host ('TestStatus (supplied):  ' + $TestStatus)
Write-Host '--- status --short --branch ---'
Write-Host $statusShort
Write-Host '--- diff --stat (unstaged) ---'
if ([string]::IsNullOrWhiteSpace($diffStat)) { Write-Host '(none)' } else { Write-Host $diffStat }
Write-Host '--- diff --cached --stat ---'
if ([string]::IsNullOrWhiteSpace($cachedStat)) { Write-Host '(none)' } else { Write-Host $cachedStat }
Write-Host '--- log -5 ---'
Write-Host $log
Write-Host '--- commits not in main (main..HEAD) ---'
if ([string]::IsNullOrWhiteSpace($ahead)) { Write-Host '(none or unavailable)' } else { Write-Host $ahead }

if ([string]::IsNullOrWhiteSpace($branch)) {
  Write-Host '[FAIL] No current branch'
  $failed = $true
}
if ($porc.Count -ne 0) {
  Write-Host '[WARN] Working tree dirty - do not claim completion until committed/clean'
  $failed = $true
}

$null = git -C $path rev-parse --verify HEAD
if ($LASTEXITCODE -ne 0) {
  Write-Host '[FAIL] No commit at HEAD'
  $failed = $true
}

Write-Host ''
Write-Host 'Suggested completion fields:'
Write-Host ('Agent: ' + $Agent)
Write-Host ('Worktree: ' + $path)
Write-Host ('Branch: ' + $branch)
Write-Host ('Commit SHA: ' + $head)
Write-Host ('Build: ' + $BuildStatus)
Write-Host ('Tests: ' + $TestStatus)

if ($failed) {
  Write-Host 'RESULT: FAIL'
  exit 1
}
Write-Host 'RESULT: PASS'
exit 0
