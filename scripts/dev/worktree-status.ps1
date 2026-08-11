<#
.SYNOPSIS
  Read-only health report for AOVIAS main + Cursor + Claude worktrees.
.DESCRIPTION
  Never checks out, resets, cleans, fetches, pulls, merges, commits,
  deletes branches, modifies files, or changes Git configuration.
#>
[CmdletBinding()]
param(
  [string]$MainPath = 'V:\DevelopmentRepo\AOVIAS',
  [string]$CursorPath = 'V:\DevelopmentRepo\AOVIAS.Cursor',
  [string]$ClaudePath = 'V:\DevelopmentRepo\AOVIAS.Claude'
)

$ErrorActionPreference = 'Stop'
$failed = $false

function Get-SafeState([string]$Path, [string]$Label) {
  if (-not (Test-Path -LiteralPath $Path)) {
    Write-Host ('[FAIL] ' + $Label + ' path missing: ' + $Path)
    return $null
  }
  try {
    $branch = git -C $Path branch --show-current 2>$null
    $head = git -C $Path rev-parse HEAD 2>$null
    $porc = @(git -C $Path status --porcelain 2>$null)
    $gitDir = git -C $Path rev-parse --git-dir 2>$null
    $common = git -C $Path rev-parse --git-common-dir 2>$null
    $detached = ((git -C $Path rev-parse --abbrev-ref HEAD 2>$null) -eq 'HEAD')
    return [pscustomobject]@{
      Label = $Label
      Path = $Path
      Branch = $branch
      Head = $head
      DirtyCount = $porc.Count
      Clean = ($porc.Count -eq 0)
      Detached = $detached
      GitDir = $gitDir
      CommonDir = $common
    }
  } catch {
    Write-Host ('[FAIL] ' + $Label + ' git query failed: ' + $_.Exception.Message)
    return $null
  }
}

function Resolve-Full([string]$Base, [string]$P) {
  if ([string]::IsNullOrWhiteSpace($P)) { return $null }
  if ([IO.Path]::IsPathRooted($P)) { return [IO.Path]::GetFullPath($P) }
  return [IO.Path]::GetFullPath((Join-Path $Base $P))
}

function Format-CleanLabel($State) {
  if ($State.Clean) { return 'CLEAN' }
  return ('DIRTY (' + $State.DirtyCount + ')')
}

$main = Get-SafeState $MainPath 'Main'
$cursor = Get-SafeState $CursorPath 'Cursor'
$claude = Get-SafeState $ClaudePath 'Claude'
if (-not $main -or -not $cursor -or -not $claude) { $failed = $true }

$remotes = @()
$worktreeCount = 0
$commonDirResolved = $null

if ($main) {
  $remotes = @(git -C $MainPath remote 2>$null)
  $wtLines = @(git -C $MainPath worktree list --porcelain 2>$null | Where-Object { $_ -like 'worktree *' })
  $worktreeCount = $wtLines.Count
  $commonDirResolved = Resolve-Full $MainPath $main.CommonDir
}

Write-Host '=== AOVIAS Worktree Status (read-only) ==='
Write-Host ''
Write-Host '--- Repository ---'
Write-Host ('Repository: ' + $MainPath)
Write-Host ('Worktree count: ' + $worktreeCount)
if ($remotes.Count -eq 0) {
  Write-Host 'Remote status: NOT CONFIGURED'
} else {
  Write-Host ('Remote status: ' + ($remotes -join ', '))
}

Write-Host ''
Write-Host '--- Main ---'
if ($main) {
  Write-Host ('Main branch: ' + $main.Branch)
  Write-Host ('Main HEAD:   ' + $main.Head)
  Write-Host ('Main status: ' + (Format-CleanLabel $main))
} else {
  Write-Host 'Main: UNAVAILABLE'
}

Write-Host ''
Write-Host '--- Cursor ---'
if ($cursor) {
  Write-Host ('Cursor worktree: ' + $cursor.Path)
  Write-Host ('Cursor branch:   ' + $cursor.Branch)
  Write-Host ('Cursor HEAD:     ' + $cursor.Head)
  Write-Host ('Cursor status:   ' + (Format-CleanLabel $cursor))
} else {
  Write-Host 'Cursor: UNAVAILABLE'
}

Write-Host ''
Write-Host '--- Claude ---'
if ($claude) {
  Write-Host ('Claude worktree: ' + $claude.Path)
  Write-Host ('Claude branch:   ' + $claude.Branch)
  Write-Host ('Claude HEAD:     ' + $claude.Head)
  Write-Host ('Claude status:   ' + (Format-CleanLabel $claude))
} else {
  Write-Host 'Claude: UNAVAILABLE'
}

Write-Host ''
Write-Host '--- Common Git directory ---'
if ($main -and $cursor -and $claude) {
  $mc = $commonDirResolved
  $cc = Resolve-Full $CursorPath $cursor.CommonDir
  $lc = Resolve-Full $ClaudePath $claude.CommonDir
  Write-Host ('Common Git directory: ' + $mc)
  if ($mc -ne $cc -or $mc -ne $lc) {
    Write-Host '[FAIL] git-common-dir mismatch across worktrees'
    Write-Host ('  main:   ' + $mc)
    Write-Host ('  cursor: ' + $cc)
    Write-Host ('  claude: ' + $lc)
    $failed = $true
  } else {
    Write-Host '[OK] Shared common Git directory'
  }
} else {
  Write-Host 'Common Git directory: UNAVAILABLE'
}

if ($main -and $main.Detached) { Write-Host '[FAIL] Main detached'; $failed = $true }
if ($cursor -and $cursor.Detached) { Write-Host '[FAIL] Cursor detached'; $failed = $true }
if ($claude -and $claude.Detached) { Write-Host '[FAIL] Claude detached'; $failed = $true }

Write-Host ''
if ($failed) {
  Write-Host 'RESULT: FAIL'
  exit 1
}
Write-Host 'RESULT: PASS'
exit 0
