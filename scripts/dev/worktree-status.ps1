<#
.SYNOPSIS
  Read-only health report for AOVIAS main + Cursor + Claude worktrees.
.DESCRIPTION
  Never checks out, resets, cleans, fetches, pulls, or modifies files/branches.
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
if ($main) { $remotes = @(git -C $MainPath remote 2>$null) }

Write-Host '=== AOVIAS Worktree Status (read-only) ==='
Write-Host ('Repository: ' + $MainPath)
if ($main) {
  Write-Host ('Main branch: ' + $main.Branch)
  Write-Host ('Main status: ' + (Format-CleanLabel $main))
  Write-Host ('Main HEAD:   ' + $main.Head)
}
if ($cursor) {
  Write-Host ('Cursor worktree: ' + $cursor.Path)
  Write-Host ('Cursor branch:   ' + $cursor.Branch)
  Write-Host ('Cursor status:   ' + (Format-CleanLabel $cursor))
  Write-Host ('Cursor HEAD:     ' + $cursor.Head)
}
if ($claude) {
  Write-Host ('Claude worktree: ' + $claude.Path)
  Write-Host ('Claude branch:   ' + $claude.Branch)
  Write-Host ('Claude status:   ' + (Format-CleanLabel $claude))
  Write-Host ('Claude HEAD:     ' + $claude.Head)
}

if ($main -and $cursor -and $claude) {
  $mc = Resolve-Full $MainPath $main.CommonDir
  $cc = Resolve-Full $CursorPath $cursor.CommonDir
  $lc = Resolve-Full $ClaudePath $claude.CommonDir
  Write-Host ('Common Git directory (main):   ' + $mc)
  Write-Host ('Common Git directory (cursor): ' + $cc)
  Write-Host ('Common Git directory (claude): ' + $lc)
  if ($mc -ne $cc -or $mc -ne $lc) {
    Write-Host '[FAIL] git-common-dir mismatch'
    $failed = $true
  } else {
    Write-Host '[OK] Shared common Git directory'
  }
}

if ($remotes.Count -eq 0) {
  Write-Host 'Remote status: NOT CONFIGURED'
} else {
  Write-Host ('Remote status: ' + ($remotes -join ', '))
}

if ($main -and $main.Detached) { Write-Host '[FAIL] Main detached'; $failed = $true }
if ($cursor -and $cursor.Detached) { Write-Host '[FAIL] Cursor detached'; $failed = $true }
if ($claude -and $claude.Detached) { Write-Host '[FAIL] Claude detached'; $failed = $true }

if ($failed) {
  Write-Host 'RESULT: FAIL'
  exit 1
}
Write-Host 'RESULT: PASS'
exit 0
