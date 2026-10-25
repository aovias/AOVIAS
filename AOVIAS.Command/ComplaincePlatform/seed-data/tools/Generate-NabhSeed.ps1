#Requires -Version 5.1
<#
.SYNOPSIS
  Generates idempotent SQL Server seed scripts for NABH Hospital 6th Edition from CSV extracts.
.NOTES
  Run from repo root: pwsh ./seed-data/tools/Generate-NabhSeed.ps1
#>
$ErrorActionPreference = 'Stop'
$Root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$Src = Join-Path $Root 'seed-data/source'
$OutDir = Join-Path $Root 'seed-data/sql'
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

function Get-DeterministicGuid([string]$seed) {
    $md5 = [System.Security.Cryptography.MD5]::Create()
    $bytes = $md5.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($seed))
    [byte[]]$b = $bytes[0..15]
    $b[6] = ($b[6] -band 0x0F) -bor 0x40
    $b[8] = ($b[8] -band 0x3F) -bor 0x80
    return [Guid][byte[]]$b
}

function SqlString([string]$s) {
    if ($null -eq $s) { return "N''" }
    return "N'" + ($s -replace "'", "''") + "'"
}

function Trunc([string]$s, [int]$max) {
    if ($null -eq $s) { return '' }
    if ($s.Length -le $max) { return $s }
    return $s.Substring(0, $max)
}

$map = Get-Content (Join-Path $Root 'seed-data/config/framework-mapping.json') -Raw | ConvertFrom-Json
$targetFw = [Guid]::Parse($map.targetFrameworkId)
$sourceFw = [Guid]::Parse($map.sourceFrameworkIdInHierarchyCsv)
$scoringId = [Guid]::Parse($map.scoringConfigId)

$kiTpl = Get-Content (Join-Path $Root 'seed-data/config/knowledge-item-templates.json') -Raw | ConvertFrom-Json

function Expand-KnowledgeItemTemplate([string]$text, $oeRow, $stRow, $chRow) {
    if ([string]::IsNullOrEmpty($text)) { return '' }
    $owner = if ($oeRow.OwnerRole) { $oeRow.OwnerRole.Trim() } else { 'designated process owner' }
    $dept = if ($oeRow.ApplicableDepartments) { $oeRow.ApplicableDepartments.Trim() } else { 'relevant clinical and support departments' }
    $scope = if ($oeRow.ApplicableScope) { $oeRow.ApplicableScope.Trim() } else { 'as stated in the standard' }
    $ev = if ($oeRow.EvidenceType) { $oeRow.EvidenceType.Trim() } else { 'Document' }
    $chCode = if ($oeRow.ChapterCode) { $oeRow.ChapterCode.Trim().ToUpperInvariant() } else { 'GENERAL' }
    $code = if ($oeRow.Code) { $oeRow.Code.Trim() } else { '' }
    $title = if ($oeRow.Title) { $oeRow.Title.Trim() } else { '' }
    $stdCode = if ($stRow -and $stRow.Code) { $stRow.Code.Trim().TrimEnd('.') } else { '' }
    $stdTitle = if ($stRow -and $stRow.Title) { $stRow.Title.Trim() } else { '' }
    $chTitle = if ($chRow -and $chRow.Title) { $chRow.Title.Trim() } else { $chCode }
    $out = $text
    $out = $out.Replace('{OwnerRole}', $owner)
    $out = $out.Replace('{ApplicableDepartments}', $dept)
    $out = $out.Replace('{ApplicableScope}', $scope)
    $out = $out.Replace('{EvidenceType}', $ev)
    $out = $out.Replace('{ChapterCode}', $chCode)
    $out = $out.Replace('{ChapterTitle}', $chTitle)
    $out = $out.Replace('{Code}', $code)
    $out = $out.Replace('{Title}', $title)
    $out = $out.Replace('{StandardCode}', $stdCode)
    $out = $out.Replace('{StandardTitle}', $stdTitle)
    return $out
}

function Get-EvidenceRequiredBody($kiTpl, $oeRow, $stRow, $chRow) {
    $evKey = if ($oeRow.EvidenceType) { $oeRow.EvidenceType.Trim() } else { 'Document' }
    $ht = $kiTpl.evidenceRequiredByEvidenceType
    $specific = $ht.$evKey
    if ([string]::IsNullOrEmpty($specific)) { $specific = $ht.default }
    if ([string]::IsNullOrEmpty($specific)) { $specific = $ht.Document }
    $intro = Expand-KnowledgeItemTemplate $kiTpl.evidenceRequiredIntroTemplate $oeRow $stRow $chRow
    return ($intro + $specific + $kiTpl.evidenceRequiredClosing)
}

function Get-RiskLevel($oeRow, $defaults) {
    try {
        $w = [decimal]::Parse($oeRow.ItemWeight, [System.Globalization.CultureInfo]::InvariantCulture)
    } catch {
        return $defaults.defaultRiskLevel
    }
    if ($w -ge 2) { return 'High' }
    if ($w -gt 0) { return 'Medium' }
    return 'Low'
}

function Get-ImpactLevel($oeRow, $kiTpl) {
    try {
        $w = [decimal]::Parse($oeRow.ItemWeight, [System.Globalization.CultureInfo]::InvariantCulture)
    } catch {
        return $kiTpl.impactLevelByWeight.zero
    }
    if ($w -ge 2) { return $kiTpl.impactLevelByWeight.high }
    if ($w -gt 0) { return $kiTpl.impactLevelByWeight.low }
    return $kiTpl.impactLevelByWeight.zero
}

$hdrFw = @('Id','Name','Code','Version','IsActive','RowVersion','ModifiedAt','ExternalCode')
$hdrCh = @('Id','FrameworkId','Code','Title','Sequence','Weight','RowVersion','ModifiedAt','ExternalCode')
$hdrSt = @('Id','ChapterId','Code','Title','Sequence','Weight','RowVersion','ModifiedAt','ExternalCode')
$hdrOe = @('Id','StandardId','Code','Title','Description','F5','F6','F7','F8','F9','ChapterCode','ApplicableDepartments','OwnerRole','ApplicableScope','EvidenceType','ItemWeight','RowVersion','ModifiedAt','ExternalCode')

$frameworks = Import-Csv (Join-Path $Src 'MasterFrameworks.csv') -Header $hdrFw
$chapters = Import-Csv (Join-Path $Src 'Masterchapters.csv') -Header $hdrCh
$standards = Import-Csv (Join-Path $Src 'Standards.csv') -Header $hdrSt
$oes = Import-Csv (Join-Path $Src 'ObjectiveElements.csv') -Header $hdrOe

$fwRow = $frameworks | Where-Object { $_.Version -eq '6th Edition' } | Select-Object -First 1
if (-not $fwRow) { throw '6th Edition framework row not found in MasterFrameworks.csv' }

$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine('SET NOCOUNT ON;')
[void]$sb.AppendLine('SET XACT_ABORT ON;')
[void]$sb.AppendLine('BEGIN TRY')
[void]$sb.AppendLine('BEGIN TRAN')
[void]$sb.AppendLine('')

# --- Frameworks ---
$fid = [Guid]::Parse($fwRow.Id)
[void]$sb.AppendLine("IF NOT EXISTS (SELECT 1 FROM dbo.Frameworks WHERE Id = '$($fid)')")
[void]$sb.AppendLine('INSERT INTO dbo.Frameworks (Id, Code, Name, Version, Description, IsActive) VALUES (')
[void]$sb.AppendLine("  '$($fid)', $(SqlString $fwRow.Code), $(SqlString $fwRow.Name), $(SqlString $fwRow.Version), $(SqlString $fwRow.Name), $([int]$fwRow.IsActive));")
[void]$sb.AppendLine('')

# --- ScoringConfigs ---
[void]$sb.AppendLine("IF NOT EXISTS (SELECT 1 FROM dbo.ScoringConfigs WHERE FrameworkId = '$($targetFw)')")
[void]$sb.AppendLine('INSERT INTO dbo.ScoringConfigs (Id, FrameworkId, FullyCompliantScore, PartialScore, NonCompliantScore, AggregationMethod, ScaleType) VALUES (')
[void]$sb.AppendLine("  '$($scoringId)', '$($targetFw)', 5, 3, 1, N'WeightedAverage', N'NABH5Point');")
[void]$sb.AppendLine('')

# --- FrameworkClassifications (chapters) ---
foreach ($ch in $chapters) {
    $cid = [Guid]::Parse($ch.Id)
    $w = [decimal]$ch.Weight
    if ($w -eq 0) { $w = 1.0 }
    $nm = Trunc $ch.Title 450
    [void]$sb.AppendLine("IF NOT EXISTS (SELECT 1 FROM dbo.FrameworkClassifications WHERE Id = '$($cid)')")
    [void]$sb.AppendLine('INSERT INTO dbo.FrameworkClassifications (Id, FrameworkId, Name, Weight, IsMandatory) VALUES (')
    [void]$sb.AppendLine("  '$($cid)', '$($targetFw)', $(SqlString $nm), $([string]$w -replace ',','.'), 1);")
}
[void]$sb.AppendLine('')

# --- ComplianceNodes: chapters ---
$seq = 0
foreach ($ch in $chapters) {
    $seq++
    $cid = [Guid]::Parse($ch.Id)
    $code = Trunc $ch.Code 128
    $title = Trunc $ch.Title 512
    [void]$sb.AppendLine("IF NOT EXISTS (SELECT 1 FROM dbo.ComplianceNodes WHERE Id = '$($cid)')")
    [void]$sb.AppendLine('INSERT INTO dbo.ComplianceNodes (Id, FrameworkId, ParentId, Level, Code, Title, Description, NodeType, Sequence) VALUES (')
    [void]$sb.AppendLine("  '$($cid)', '$($targetFw)', NULL, 1, $(SqlString $code), $(SqlString $title), $(SqlString $title), N'Chapter', $([int][Math]::Truncate([double]$ch.Sequence)));")
}
[void]$sb.AppendLine('')

# --- ComplianceNodes: standards ---
foreach ($st in $standards) {
    $sid = [Guid]::Parse($st.Id)
    $chapterRef = [Guid]::Parse($st.ChapterId)
    $code = Trunc ($st.Code.TrimEnd('.')) 128
    $title = Trunc $st.Title 512
    [void]$sb.AppendLine("IF NOT EXISTS (SELECT 1 FROM dbo.ComplianceNodes WHERE Id = '$($sid)')")
    [void]$sb.AppendLine('INSERT INTO dbo.ComplianceNodes (Id, FrameworkId, ParentId, Level, Code, Title, Description, NodeType, Sequence) VALUES (')
    [void]$sb.AppendLine("  '$($sid)', '$($targetFw)', '$($chapterRef)', 2, $(SqlString $code), $(SqlString $title), $(SqlString $title), N'Standard', $([int][Math]::Truncate([double]$st.Sequence)));")
}
[void]$sb.AppendLine('')

# --- ComplianceNodes: objective elements ---
$oeSeq = 0
foreach ($oe in $oes) {
    $oeSeq++
    $oid = [Guid]::Parse($oe.Id)
    $spid = [Guid]::Parse($oe.StandardId)
    $code = Trunc $oe.Code 128
    $title = Trunc $oe.Title 512
    $desc = $oe.Description
    [void]$sb.AppendLine("IF NOT EXISTS (SELECT 1 FROM dbo.ComplianceNodes WHERE Id = '$($oid)')")
    [void]$sb.AppendLine('INSERT INTO dbo.ComplianceNodes (Id, FrameworkId, ParentId, Level, Code, Title, Description, NodeType, Sequence) VALUES (')
    [void]$sb.AppendLine("  '$($oid)', '$($targetFw)', '$($spid)', 3, $(SqlString $code), $(SqlString $title), $(SqlString $desc), N'ObjectiveElement', $oeSeq);")
}
[void]$sb.AppendLine('')

# --- RequirementDetails + RequirementScores (per standard) ---
$chapterCodeById = @{}
foreach ($ch in $chapters) { $chapterCodeById[[Guid]::Parse($ch.Id).ToString()] = $ch.Code }

foreach ($st in $standards) {
    $sid = [Guid]::Parse($st.Id)
    $chId = [Guid]::Parse($st.ChapterId).ToString()
    $cls = $chapterCodeById[$chId]
    if (-not $cls) { $cls = 'UNK' }
    $intent = Trunc $st.Title 4000
    $mand = 0
    if ([decimal]$st.Weight -ge 2) { $mand = 1 }
    $rdId = Get-DeterministicGuid "RD:$($sid)"
    $rsId = Get-DeterministicGuid "RS:$($sid)"
    $classId = [Guid]::Parse($st.ChapterId)
    $wt = [decimal]$st.Weight
    [void]$sb.AppendLine("IF NOT EXISTS (SELECT 1 FROM dbo.RequirementDetails WHERE NodeId = '$($sid)')")
    [void]$sb.AppendLine('INSERT INTO dbo.RequirementDetails (Id, NodeId, Intent, MeasurableElements, Classification, IsMandatory, ApplicableDepartments, RequiresDocumentation) VALUES (')
    [void]$sb.AppendLine("  '$($rdId)', '$($sid)', $(SqlString $intent), N'', $(SqlString $cls), $mand, N'', 0);")
    [void]$sb.AppendLine("IF NOT EXISTS (SELECT 1 FROM dbo.RequirementScores WHERE NodeId = '$($sid)')")
    [void]$sb.AppendLine('INSERT INTO dbo.RequirementScores (Id, NodeId, MaxScore, Weight, ClassificationId) VALUES (')
    [void]$sb.AppendLine("  '$($rsId)', '$($sid)', 5, $([string]$wt -replace ',','.'), '$($classId)');")
}
[void]$sb.AppendLine('')

# Lookups for per-OE KnowledgeItem text (chapter + parent standard titles)
$stdById = @{}
foreach ($s in $standards) { $stdById[$s.Id] = $s }
$chById = @{}
foreach ($c in $chapters) { $chById[$c.Id] = $c }

# --- KnowledgeItems (per OE): differentiated using OE + parent standard + chapter (see knowledge-item-templates.json)
foreach ($oe in $oes) {
    $oid = [Guid]::Parse($oe.Id)
    $kid = Get-DeterministicGuid "KI:$($oid)"
    $stRow = $stdById[$oe.StandardId]
    $chRow = $null
    if ($stRow) { $chRow = $chById[$stRow.ChapterId] }

    $expl = Trunc ("$($oe.Code.Trim()): $($oe.Title.Trim()). $($oe.Description)".Trim()) 4000
    $why = Trunc (Expand-KnowledgeItemTemplate $kiTpl.whyItMattersTemplate $oe $stRow $chRow) 4000
    $how = Trunc (Expand-KnowledgeItemTemplate $kiTpl.howToComplyTemplate $oe $stRow $chRow) 4000
    $evidence = Trunc (Get-EvidenceRequiredBody $kiTpl $oe $stRow $chRow) 4000
    $audit = Trunc (Expand-KnowledgeItemTemplate $kiTpl.auditChecklistTemplate $oe $stRow $chRow) 4000
    $risk = Trunc (Get-RiskLevel $oe $kiTpl) 64
    $impact = Trunc (Get-ImpactLevel $oe $kiTpl) 64
    $tags = Trunc (Expand-KnowledgeItemTemplate $kiTpl.tagsTemplate $oe $stRow $chRow) 4000
    [void]$sb.AppendLine("IF NOT EXISTS (SELECT 1 FROM dbo.KnowledgeItems WHERE NodeId = '$($oid)')")
    [void]$sb.AppendLine('INSERT INTO dbo.KnowledgeItems (Id, NodeId, Explanation, WhyItMatters, HowToComply, EvidenceRequired, AuditChecklist, RiskLevel, ImpactLevel, Tags, Version, IsActive) VALUES (')
    [void]$sb.AppendLine("  '$($kid)', '$($oid)', $(SqlString $expl), $(SqlString $why), $(SqlString $how), $(SqlString $evidence), $(SqlString $audit), $(SqlString $risk), $(SqlString $impact), $(SqlString $tags), 1, 1);")
}

[void]$sb.AppendLine('COMMIT TRAN')
[void]$sb.AppendLine('END TRY')
[void]$sb.AppendLine('BEGIN CATCH')
[void]$sb.AppendLine('  IF @@TRANCOUNT > 0 ROLLBACK TRAN;')
[void]$sb.AppendLine('  THROW;')
[void]$sb.AppendLine('END CATCH')

$outFile = Join-Path $OutDir 'seed-nabh-hospital-6e.sql'
[System.IO.File]::WriteAllText($outFile, $sb.ToString(), [System.Text.UTF8Encoding]::new($false))
Write-Host "Wrote $outFile"
