# Phase 3D Gate B — validate, backup, migrate. Throws and rolls back on mismatch.
# Target: localhost\SQLEXPRESS / Compliance (Trusted_Connection). No secrets.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if (-not (Test-Path (Join-Path $PSScriptRoot 'phase3d-twin-mapping.csv'))) {
    $root = 'V:\DevelopmentRepo\AOVIAS.Cursor'
}
$scriptDir = Join-Path $root 'scripts\dev'
$mapPath = Join-Path $scriptDir 'phase3d-twin-mapping.csv'
$missPath = Join-Path $scriptDir 'phase3d-missing-hospital-obligation-ids.csv'
$evidenceDir = Join-Path $scriptDir 'phase3d-gateb-evidence'
New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null

$map = Import-Csv $mapPath
$miss = Import-Csv $missPath
if ($map.Count -ne 1006) { throw "Twin mapping row count $($map.Count) != 1006" }
if ($miss.Count -ne 650) { throw "Missing-hospital row count $($miss.Count) != 650" }
$identical = @($map | Where-Object { $_.StateClass -eq 'IDENTICAL' })
$diverged = @($map | Where-Object { $_.StateClass -eq 'DIVERGED' })
if ($identical.Count -ne 965) { throw "IDENTICAL count $($identical.Count) != 965" }
if ($diverged.Count -ne 41) { throw "DIVERGED count $($diverged.Count) != 41" }

function GuidSql([string]$g) { return "'" + ([guid]$g).ToString() + "'" }

$cs = 'Server=localhost\SQLEXPRESS;Database=Compliance;Trusted_Connection=True;TrustServerCertificate=True'
$script:tran = $null
$conn = New-Object System.Data.SqlClient.SqlConnection $cs
$conn.Open()

function Bind-Tran($cmd) {
    if ($null -ne $script:tran) { $cmd.Transaction = $script:tran }
}

function Exec-Scalar([string]$sql) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandTimeout = 180
    Bind-Tran $cmd
    $cmd.CommandText = $sql
    return $cmd.ExecuteScalar()
}

function Exec-NonQuery([string]$sql) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandTimeout = 180
    Bind-Tran $cmd
    $cmd.CommandText = $sql
    return $cmd.ExecuteNonQuery()
}

function Exec-ReaderText([string]$sql) {
    $cmd = $conn.CreateCommand()
    $cmd.CommandTimeout = 180
    Bind-Tran $cmd
    $cmd.CommandText = $sql
    $r = $cmd.ExecuteReader()
    $rows = New-Object System.Collections.Generic.List[string]
    while ($r.Read()) {
        $parts = @()
        for ($i = 0; $i -lt $r.FieldCount; $i++) { $parts += [string]$r.GetValue($i) }
        $rows.Add(($parts -join '|'))
    }
    $r.Close()
    return $rows
}

Write-Host '=== PREFLIGHT COUNTS ==='
$total = [int](Exec-Scalar 'SELECT COUNT(*) FROM dbo.OrganizationComplianceObligations')
$dup = [int](Exec-Scalar @'
SELECT COUNT(*) FROM (
  SELECT HospitalId, FrameworkId, ObjectiveElementId
  FROM dbo.OrganizationComplianceObligations
  GROUP BY HospitalId, FrameworkId, ObjectiveElementId
  HAVING COUNT(*) > 1
) x
'@)
$defs = [int](Exec-Scalar 'SELECT COUNT(*) FROM dbo.ComplianceDeficiencies')
$tasks = [int](Exec-Scalar 'SELECT COUNT(*) FROM dbo.ComplianceTasks')
$ev = [int](Exec-Scalar 'SELECT COUNT(*) FROM dbo.Evidences')
$find = [int](Exec-Scalar 'SELECT COUNT(*) FROM dbo.Findings')
$capa = [int](Exec-Scalar 'SELECT COUNT(*) FROM dbo.Capas')
$sla = [int](Exec-Scalar 'SELECT COUNT(*) FROM dbo.SlaPolicies')
$mat = [int](Exec-Scalar 'SELECT COUNT(*) FROM dbo.OrganizationMaturityStageStates')
$stamp = (Get-Date).ToUniversalTime().ToString('o')
@"
MigrationTimestampUtc=$stamp
TotalObligations=$total
DuplicateBusinessKeys=$dup
Deficiencies=$defs
Tasks=$tasks
Evidences=$ev
Findings=$find
Capas=$capa
SlaPolicies=$sla
MaturityStates=$mat
"@ | Set-Content (Join-Path $evidenceDir 'pre-migration-counts.txt')

if ($total -ne 2662 -or $dup -ne 1006 -or $defs -ne 177 -or $tasks -ne 1534 -or $ev -ne 473 -or $find -ne 35 -or $capa -ne 17 -or $sla -ne 8 -or $mat -ne 4) {
    throw "Pre-migration counts differ from Gate A baseline. See phase3d-gateb-evidence/pre-migration-counts.txt"
}

# Load mapping into #Map on this connection (temp table lasts for the session).
$createMap = @'
IF OBJECT_ID('tempdb..#Phase3DMap') IS NOT NULL DROP TABLE #Phase3DMap;
IF OBJECT_ID('tempdb..#Phase3DMiss') IS NOT NULL DROP TABLE #Phase3DMiss;
CREATE TABLE #Phase3DMap (
  TwinObligationId uniqueidentifier NOT NULL PRIMARY KEY,
  CanonicalObligationId uniqueidentifier NOT NULL,
  HospitalId uniqueidentifier NOT NULL,
  FrameworkId uniqueidentifier NOT NULL,
  ObjectiveElementId uniqueidentifier NOT NULL,
  StateClass nvarchar(20) NOT NULL
);
CREATE TABLE #Phase3DMiss (
  ObligationId uniqueidentifier NOT NULL PRIMARY KEY,
  HospitalId uniqueidentifier NOT NULL
);
'@
$cmd = $conn.CreateCommand(); $cmd.CommandText = $createMap; [void]$cmd.ExecuteNonQuery()

$batch = New-Object System.Text.StringBuilder
$n = 0
foreach ($row in $map) {
    [void]$batch.AppendLine(("INSERT INTO #Phase3DMap VALUES ({0},{1},{2},{3},{4},'{5}');" -f `
        (GuidSql $row.TwinObligationId), (GuidSql $row.CanonicalObligationId), (GuidSql $row.HospitalId), `
        (GuidSql $row.FrameworkId), (GuidSql $row.ObjectiveElementId), $row.StateClass))
    $n++
    if ($n % 100 -eq 0) {
        $cmd = $conn.CreateCommand(); $cmd.CommandTimeout = 180; $cmd.CommandText = $batch.ToString(); [void]$cmd.ExecuteNonQuery()
        $batch = New-Object System.Text.StringBuilder
    }
}
if ($batch.Length -gt 0) {
    $cmd = $conn.CreateCommand(); $cmd.CommandTimeout = 180; $cmd.CommandText = $batch.ToString(); [void]$cmd.ExecuteNonQuery()
}

$batch = New-Object System.Text.StringBuilder
$n = 0
foreach ($row in $miss) {
    [void]$batch.AppendLine(("INSERT INTO #Phase3DMiss VALUES ({0},{1});" -f (GuidSql $row.ObligationId), (GuidSql $row.HospitalId)))
    $n++
    if ($n % 100 -eq 0) {
        $cmd = $conn.CreateCommand(); $cmd.CommandTimeout = 180; $cmd.CommandText = $batch.ToString(); [void]$cmd.ExecuteNonQuery()
        $batch = New-Object System.Text.StringBuilder
    }
}
if ($batch.Length -gt 0) {
    $cmd = $conn.CreateCommand(); $cmd.CommandTimeout = 180; $cmd.CommandText = $batch.ToString(); [void]$cmd.ExecuteNonQuery()
}

$mapLive = [int](Exec-Scalar 'SELECT COUNT(*) FROM #Phase3DMap')
$missLive = [int](Exec-Scalar 'SELECT COUNT(*) FROM #Phase3DMiss')
if ($mapLive -ne 1006 -or $missLive -ne 650) { throw "Temp table load failed map=$mapLive miss=$missLive" }

$twinGone = [int](Exec-Scalar @'
SELECT COUNT(*) FROM #Phase3DMap m
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrganizationComplianceObligations o WHERE o.Id = m.TwinObligationId)
'@)
$canGone = [int](Exec-Scalar @'
SELECT COUNT(*) FROM #Phase3DMap m
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrganizationComplianceObligations o WHERE o.Id = m.CanonicalObligationId)
'@)
if ($twinGone -ne 0 -or $canGone -ne 0) { throw "Mapping IDs missing in DB twinGone=$twinGone canGone=$canGone" }

$keyMismatch = [int](Exec-Scalar @'
SELECT COUNT(*)
FROM #Phase3DMap m
JOIN dbo.OrganizationComplianceObligations t ON t.Id = m.TwinObligationId
JOIN dbo.OrganizationComplianceObligations c ON c.Id = m.CanonicalObligationId
WHERE t.HospitalId <> m.HospitalId OR t.FrameworkId <> m.FrameworkId OR t.ObjectiveElementId <> m.ObjectiveElementId
   OR c.HospitalId <> m.HospitalId OR c.FrameworkId <> m.FrameworkId OR c.ObjectiveElementId <> m.ObjectiveElementId
   OR t.HospitalId <> c.HospitalId OR t.FrameworkId <> c.FrameworkId OR t.ObjectiveElementId <> c.ObjectiveElementId
   OR t.OrganizationId <> t.HospitalId
   OR NOT EXISTS (
        SELECT 1 FROM Organization.dbo.Hospitals h
        WHERE h.Id = c.HospitalId AND h.OrganizationId = c.OrganizationId AND h.IsDeleted = 0)
'@)
if ($keyMismatch -ne 0) { throw "Twin/canonical key or tenant mismatch count=$keyMismatch" }

$identStatusDrift = [int](Exec-Scalar @'
SELECT COUNT(*)
FROM #Phase3DMap m
JOIN dbo.OrganizationComplianceObligations t ON t.Id = m.TwinObligationId
JOIN dbo.OrganizationComplianceObligations c ON c.Id = m.CanonicalObligationId
WHERE m.StateClass = N'IDENTICAL'
  AND (t.ImplementationStatus <> c.ImplementationStatus
    OR t.AssessmentStatus <> c.AssessmentStatus
    OR t.JourneyStatus <> c.JourneyStatus
    OR t.EvidenceStatus <> c.EvidenceStatus
    OR t.VerificationStatus <> c.VerificationStatus)
'@)
if ($identStatusDrift -ne 0) { throw "IDENTICAL pairs drifted: $identStatusDrift" }

$divCount = [int](Exec-Scalar "SELECT COUNT(*) FROM #Phase3DMap WHERE StateClass = N'DIVERGED'")
if ($divCount -ne 41) { throw "Live DIVERGED mapping count $divCount != 41" }

$twinDefs = [int](Exec-Scalar @'
SELECT COUNT(*) FROM dbo.ComplianceDeficiencies d
JOIN #Phase3DMap m ON m.TwinObligationId = d.ComplianceObligationId
'@)
$identDefs = [int](Exec-Scalar @'
SELECT COUNT(*) FROM dbo.ComplianceDeficiencies d
JOIN #Phase3DMap m ON m.TwinObligationId = d.ComplianceObligationId
WHERE m.StateClass = N'IDENTICAL'
'@)
$divTwinDefs = [int](Exec-Scalar @'
SELECT COUNT(*) FROM dbo.ComplianceDeficiencies d
JOIN #Phase3DMap m ON m.TwinObligationId = d.ComplianceObligationId
WHERE m.StateClass = N'DIVERGED'
'@)
if ($twinDefs -ne 88) { throw "Twin-backed deficiencies expected 88, got twinDefs=$twinDefs" }
if (($identDefs + $divTwinDefs) -ne 88) { throw "Twin-def split $identDefs+$divTwinDefs != 88" }
# Gate A classified IDENTICAL by five status fields only. Live data: $identDefs of the approved 88 twin-backed
# deficiencies sit on IDENTICAL-classified twins (parallel trees; same remap policy as DIVERGED).
"IdentTwinDefs=$identDefs`nDivergedTwinDefs=$divTwinDefs`nNote=IDENTICAL class is status-equal; 88 remaps still apply to every twin-backed deficiency" |
    Set-Content (Join-Path $evidenceDir 'twin-deficiency-class-split.txt')

$missExist = [int](Exec-Scalar @'
SELECT COUNT(*) FROM #Phase3DMiss m
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrganizationComplianceObligations o WHERE o.Id = m.ObligationId AND o.HospitalId = m.HospitalId)
'@)
if ($missExist -ne 0) { throw "650 ID list no longer matches DB: $missExist" }

$missInOrg = [int](Exec-Scalar @'
SELECT COUNT(DISTINCT m.HospitalId)
FROM #Phase3DMiss m
WHERE EXISTS (SELECT 1 FROM Organization.dbo.Hospitals h WHERE h.Id = m.HospitalId)
'@)
if ($missInOrg -ne 0) { throw "Missing-hospital IDs now exist in Organization.Hospitals: $missInOrg" }

$hospitalTables = Exec-ReaderText @'
SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.COLUMNS
WHERE COLUMN_NAME = 'HospitalId' AND TABLE_NAME <> 'OrganizationComplianceObligations'
'@
$unexpected = 0
foreach ($line in $hospitalTables) {
    $parts = $line.Split('|')
    $schema = $parts[0]; $table = $parts[1]
    $cnt = [int](Exec-Scalar @"
SELECT COUNT(*) FROM [$schema].[$table] t
WHERE t.HospitalId IN (SELECT DISTINCT HospitalId FROM #Phase3DMiss)
"@)
    if ($cnt -ne 0) {
        Write-Host "UNEXPECTED refs in $schema.$table : $cnt"
        $unexpected += $cnt
    }
}
if ($unexpected -ne 0) { throw "650 hospitals have $unexpected unexpected HospitalId references" }

$missDef = [int](Exec-Scalar @'
SELECT COUNT(*) FROM dbo.ComplianceDeficiencies d
WHERE d.ComplianceObligationId IN (SELECT ObligationId FROM #Phase3DMiss)
'@)
if ($missDef -ne 0) { throw "650 obligations have deficiencies: $missDef" }

Write-Host 'Preflight mapping/650 checks PASSED'

$conn.Close()

$bak = 'C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\Compliance_Phase3D_GateB_20260831.bak'
Write-Host "Backing up Compliance to $bak"
sqlcmd -S 'localhost\SQLEXPRESS' -E -Q "BACKUP DATABASE [Compliance] TO DISK = N'$bak' WITH COPY_ONLY, INIT, STATS=10" | Write-Host
if ($LASTEXITCODE -ne 0) { throw 'BACKUP DATABASE failed' }
"BackupPath=$bak`nBackupUtc=$stamp" | Set-Content (Join-Path $evidenceDir 'backup.txt')

# Re-open and reload temp tables inside the migration transaction connection.
$conn = New-Object System.Data.SqlClient.SqlConnection $cs
$conn.Open()
$cmd = $conn.CreateCommand(); $cmd.CommandText = $createMap; [void]$cmd.ExecuteNonQuery()
$batch = New-Object System.Text.StringBuilder
$n = 0
foreach ($row in $map) {
    [void]$batch.AppendLine(("INSERT INTO #Phase3DMap VALUES ({0},{1},{2},{3},{4},'{5}');" -f `
        (GuidSql $row.TwinObligationId), (GuidSql $row.CanonicalObligationId), (GuidSql $row.HospitalId), `
        (GuidSql $row.FrameworkId), (GuidSql $row.ObjectiveElementId), $row.StateClass))
    $n++
    if ($n % 100 -eq 0) {
        $cmd = $conn.CreateCommand(); $cmd.CommandTimeout = 180; $cmd.CommandText = $batch.ToString(); [void]$cmd.ExecuteNonQuery()
        $batch = New-Object System.Text.StringBuilder
    }
}
if ($batch.Length -gt 0) {
    $cmd = $conn.CreateCommand(); $cmd.CommandTimeout = 180; $cmd.CommandText = $batch.ToString(); [void]$cmd.ExecuteNonQuery()
}
$batch = New-Object System.Text.StringBuilder
$n = 0
foreach ($row in $miss) {
    [void]$batch.AppendLine(("INSERT INTO #Phase3DMiss VALUES ({0},{1});" -f (GuidSql $row.ObligationId), (GuidSql $row.HospitalId)))
    $n++
    if ($n % 100 -eq 0) {
        $cmd = $conn.CreateCommand(); $cmd.CommandTimeout = 180; $cmd.CommandText = $batch.ToString(); [void]$cmd.ExecuteNonQuery()
        $batch = New-Object System.Text.StringBuilder
    }
}
if ($batch.Length -gt 0) {
    $cmd = $conn.CreateCommand(); $cmd.CommandTimeout = 180; $cmd.CommandText = $batch.ToString(); [void]$cmd.ExecuteNonQuery()
}

$script:tran = $conn.BeginTransaction()
try {
    Write-Host '=== MIGRATE BEGIN TRAN ==='
    Exec-NonQuery @'
IF OBJECT_ID('tempdb..#DefRemap') IS NOT NULL DROP TABLE #DefRemap;
CREATE TABLE #DefRemap (
  DeficiencyId uniqueidentifier NOT NULL,
  OldObligationId uniqueidentifier NOT NULL,
  NewObligationId uniqueidentifier NOT NULL
);
UPDATE d
SET d.ComplianceObligationId = m.CanonicalObligationId
OUTPUT deleted.Id, deleted.ComplianceObligationId, inserted.ComplianceObligationId
INTO #DefRemap (DeficiencyId, OldObligationId, NewObligationId)
FROM dbo.ComplianceDeficiencies d
INNER JOIN #Phase3DMap m ON m.TwinObligationId = d.ComplianceObligationId;
'@ | Out-Null

    $remapCount = [int](Exec-Scalar 'SELECT COUNT(*) FROM #DefRemap')
    if ($remapCount -ne 88) { throw "Expected 88 deficiency remaps, got $remapCount" }

    Exec-NonQuery @'
IF OBJECT_ID('tempdb..#OrgFix') IS NOT NULL DROP TABLE #OrgFix;
CREATE TABLE #OrgFix (
  DeficiencyId uniqueidentifier NOT NULL,
  OldOrganizationId uniqueidentifier NOT NULL,
  NewOrganizationId uniqueidentifier NOT NULL
);
UPDATE d
SET d.OrganizationId = h.OrganizationId
OUTPUT deleted.Id, deleted.OrganizationId, inserted.OrganizationId
INTO #OrgFix (DeficiencyId, OldOrganizationId, NewOrganizationId)
FROM dbo.ComplianceDeficiencies d
INNER JOIN #DefRemap r ON r.DeficiencyId = d.Id
INNER JOIN Organization.dbo.Hospitals h ON h.Id = d.HospitalId AND h.IsDeleted = 0
WHERE h.OrganizationId <> d.OrganizationId;
'@ | Out-Null

    $orgFix = [int](Exec-Scalar 'SELECT COUNT(*) FROM #OrgFix')
    if ($orgFix -ne 88) { throw "Expected 88 deficiency OrganizationId repairs, got $orgFix" }

    $stillBad = [int](Exec-Scalar @'
SELECT COUNT(*) FROM #DefRemap r
JOIN dbo.ComplianceDeficiencies d ON d.Id = r.DeficiencyId
WHERE NOT EXISTS (
  SELECT 1 FROM Organization.dbo.Hospitals h
  WHERE h.Id = d.HospitalId AND h.OrganizationId = d.OrganizationId AND h.IsDeleted = 0)
'@)
    if ($stillBad -ne 0) { throw "Deficiency tenant mismatch after repair: $stillBad" }

    $delTwins = Exec-NonQuery 'DELETE FROM dbo.OrganizationComplianceObligations WHERE Id IN (SELECT TwinObligationId FROM #Phase3DMap);'
    if ($delTwins -ne 1006) { throw "Expected delete 1006 twin obligations (965 identical + 41 identity-only twins after remap), got $delTwins" }

    $delMiss = Exec-NonQuery 'DELETE FROM dbo.OrganizationComplianceObligations WHERE Id IN (SELECT ObligationId FROM #Phase3DMiss);'
    if ($delMiss -ne 650) { throw "Expected delete 650 missing-hospital obligations, got $delMiss" }

    $left = [int](Exec-Scalar 'SELECT COUNT(*) FROM dbo.OrganizationComplianceObligations')
    if ($left -ne 1006) { throw "Expected 1006 obligations remaining, got $left" }

    $dupAfter = [int](Exec-Scalar @'
SELECT COUNT(*) FROM (
  SELECT HospitalId, FrameworkId, ObjectiveElementId
  FROM dbo.OrganizationComplianceObligations
  GROUP BY HospitalId, FrameworkId, ObjectiveElementId
  HAVING COUNT(*) > 1
) x
'@)
    if ($dupAfter -ne 0) { throw "Duplicate canonical keys remain: $dupAfter" }

    $orgEqHosp = [int](Exec-Scalar 'SELECT COUNT(*) FROM dbo.OrganizationComplianceObligations WHERE OrganizationId = HospitalId')
    if ($orgEqHosp -ne 0) { throw "Surviving OrganizationId=HospitalId: $orgEqHosp" }

    $mismatch = [int](Exec-Scalar @'
SELECT COUNT(*) FROM dbo.OrganizationComplianceObligations o
WHERE NOT EXISTS (
  SELECT 1 FROM Organization.dbo.Hospitals h
  WHERE h.Id = o.HospitalId AND h.OrganizationId = o.OrganizationId AND h.IsDeleted = 0)
'@)
    if ($mismatch -ne 0) { throw "Hospital/Organization mismatches: $mismatch" }

    $orphanDef = [int](Exec-Scalar @'
SELECT COUNT(*) FROM dbo.ComplianceDeficiencies d
WHERE NOT EXISTS (SELECT 1 FROM dbo.OrganizationComplianceObligations o WHERE o.Id = d.ComplianceObligationId)
'@)
    if ($orphanDef -ne 0) { throw "Orphan deficiencies: $orphanDef" }

    $defAfter = [int](Exec-Scalar 'SELECT COUNT(*) FROM dbo.ComplianceDeficiencies')
    $taskAfter = [int](Exec-Scalar 'SELECT COUNT(*) FROM dbo.ComplianceTasks')
    $evAfter = [int](Exec-Scalar 'SELECT COUNT(*) FROM dbo.Evidences')
    $findAfter = [int](Exec-Scalar 'SELECT COUNT(*) FROM dbo.Findings')
    $capaAfter = [int](Exec-Scalar 'SELECT COUNT(*) FROM dbo.Capas')
    $slaAfter = [int](Exec-Scalar 'SELECT COUNT(*) FROM dbo.SlaPolicies')
    $matAfter = [int](Exec-Scalar 'SELECT COUNT(*) FROM dbo.OrganizationMaturityStageStates')
    if ($defAfter -ne 177 -or $taskAfter -ne 1534 -or $evAfter -ne 473 -or $findAfter -ne 35 -or $capaAfter -ne 17 -or $slaAfter -ne 8 -or $matAfter -ne 4) {
        throw "Conservation failed defs=$defAfter tasks=$taskAfter ev=$evAfter find=$findAfter capa=$capaAfter sla=$slaAfter mat=$matAfter"
    }

    Exec-NonQuery 'DROP INDEX UX_OrganizationComplianceObligations_OrgOE ON dbo.OrganizationComplianceObligations;' | Out-Null
    Exec-NonQuery @'
CREATE UNIQUE INDEX UX_OrganizationComplianceObligations_HospitalFrameworkOE
ON dbo.OrganizationComplianceObligations (HospitalId, FrameworkId, ObjectiveElementId);
'@ | Out-Null

    $remapRows = Exec-ReaderText 'SELECT CONVERT(nvarchar(36), DeficiencyId), CONVERT(nvarchar(36), OldObligationId), CONVERT(nvarchar(36), NewObligationId) FROM #DefRemap'
    "DeficiencyId,OldObligationId,NewObligationId,Action,Reason" | Set-Content (Join-Path $evidenceDir 'deficiency-remap.csv')
    foreach ($line in $remapRows) {
        $p = $line.Split('|')
        Add-Content (Join-Path $evidenceDir 'deficiency-remap.csv') ("{0},{1},{2},remap_fk,approved_keep_both_trees" -f $p[0], $p[1], $p[2])
    }

    $script:tran.Commit()
    $script:tran = $null
    Write-Host '=== MIGRATE COMMITTED ==='

    @"
PostObligations=$left
DuplicateKeys=$dupAfter
OrgEqHospital=$orgEqHosp
HospitalOrgMismatch=$mismatch
OrphanDeficiencies=$orphanDef
DeficiencyRemaps=$remapCount
OrganizationIdRepairs=$orgFix
TwinsDeleted=$delTwins
MissingHospitalDeleted=$delMiss
Deficiencies=$defAfter
Tasks=$taskAfter
Evidences=$evAfter
Findings=$findAfter
Capas=$capaAfter
SlaPolicies=$slaAfter
MaturityStates=$matAfter
Index=UX_OrganizationComplianceObligations_HospitalFrameworkOE
"@ | Set-Content (Join-Path $evidenceDir 'post-migration-counts.txt')
}
catch {
    if ($null -ne $script:tran -and $null -ne $script:tran.Connection) {
        $script:tran.Rollback()
        Write-Host '=== ROLLED BACK ==='
        $script:tran = $null
    }
    if ($conn.State -eq 'Open') { $conn.Close() }
    throw
}
$conn.Close()
Write-Host 'Gate B SQL migration finished.'
