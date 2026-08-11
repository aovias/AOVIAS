<#
.SYNOPSIS
  Read-only preflight for Cursor or Claude agent worktree.
.DESCRIPTION
  Verifies worktree mapping, branch, cleanliness, git validity, main existence,
  and baseline ancestry. Never modifies repository state.
#>
[CmdletBinding()]
param(
  [Parameter(Mandatory = $true)]
  [ValidateSet('cursor', 'claude')]
  [string]$Agent,

  [string]$MainPath = 'V:\DevelopmentRepo\AOVIAS',
  [string]$CursorPath = 'V:\DevelopmentRepo\AOVIAS.Cursor',
  [string]$ClaudePath = 'V:\DevelopmentRepo\AOVIAS.Claude',
  [string]$BaselineTag = 'aovias-baseline-2026-08-11'
)

$ErrorActionPreference = 'Stop'
$failures = New-Object System.Collections.Generic.List[string]

function Fail([string]$Message) {
  [void]$failures.Add($Message)
  Write-Host ('[FAIL] ' + $Message)
}

$map = @{
  cursor = @{ Path = $CursorPath; ActiveBranch = 'agent/cursor/active'; Prefix = 'agent/cursor/' }
  claude = @{ Path = $ClaudePath; ActiveBranch = 'agent/claude/active'; Prefix = 'agent/claude/' }
}
$cfg = $map[$Agent]
$path = $cfg.Path

Write-Host ('=== Agent Preflight (' + $Agent + ') - read-only ===')
Write-Host ('Expected worktree: ' + $path)

if (-not (Test-Path -LiteralPath $MainPath)) { Fail ('Main path missing: ' + $MainPath) }
if (-not (Test-Path -LiteralPath $path)) { Fail ('Agent worktree missing: ' + $path) }

if ($failures.Count -gt 0) { Write-Host 'RESULT: FAIL'; exit 1 }

$null = git -C $path rev-parse --git-dir 2>$null
if ($LASTEXITCODE -ne 0) { Fail 'Agent path is not a valid Git worktree/repo' }
$null = git -C $MainPath rev-parse --git-dir 2>$null
if ($LASTEXITCODE -ne 0) { Fail 'Main path is not a valid Git repository' }

$mainRef = git -C $MainPath rev-parse --verify main 2>$null
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($mainRef)) { Fail 'Branch main does not exist' }

$branch = git -C $path branch --show-current
$head = git -C $path rev-parse HEAD
$porc = @(git -C $path status --porcelain)
$abbrev = git -C $path rev-parse --abbrev-ref HEAD

$wtLabel = if ($porc.Count -eq 0) { 'CLEAN' } else { 'DIRTY (' + $porc.Count + ')' }
Write-Host ('Current branch: ' + $branch)
Write-Host ('HEAD: ' + $head)
Write-Host ('Working tree: ' + $wtLabel)

if ($abbrev -eq 'HEAD' -or [string]::IsNullOrWhiteSpace($branch)) { Fail 'Worktree is detached or has no current branch' }
if (-not $branch.StartsWith($cfg.Prefix)) { Fail ('Branch ' + $branch + ' is not under ' + $cfg.Prefix) }
if ($porc.Count -ne 0) { Fail 'Working tree is not clean' }

$common = git -C $path rev-parse --git-common-dir
if (-not [IO.Path]::IsPathRooted($common)) { $common = Join-Path $path $common }
$common = [IO.Path]::GetFullPath($common)
$indexLock = Join-Path $common 'index.lock'
if (Test-Path -LiteralPath $indexLock) { Fail ('Unexpected index.lock present: ' + $indexLock) }

$tag = git -C $MainPath rev-parse --verify $BaselineTag 2>$null
if ($LASTEXITCODE -ne 0) {
  Fail ('Baseline tag missing: ' + $BaselineTag)
} else {
  git -C $path merge-base --is-ancestor $tag HEAD 2>$null
  if ($LASTEXITCODE -ne 0) { Fail ('HEAD is not a descendant of baseline tag ' + $BaselineTag) }
  else { Write-Host ('[OK] Baseline ancestry (' + $BaselineTag + ')') }
}

$mainTip = git -C $MainPath rev-parse main
Write-Host ('main tip: ' + $mainTip)
Write-Host ('Active branch name (home): ' + $cfg.ActiveBranch)

if ($failures.Count -gt 0) {
  Write-Host ('RESULT: FAIL (' + $failures.Count + ' issues)')
  exit 1
}

Write-Host 'RESULT: PASS'
exit 0
