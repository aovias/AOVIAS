# Phase 0 Verification Guide

## Objective
Verify that ComplianceService data persists across service restarts and no in-memory collections remain.

## Prerequisites
1. ComplianceService is running
2. Database is accessible (SQL Server)
3. API endpoints are accessible (may need authentication token)

## Verification Steps

### Step 1: Create a ComplianceProgram

**Request:**
```bash
POST https://localhost:7001/api/v1/compliance/programs
Content-Type: application/json
Authorization: Bearer <token-if-required>

{
  "Regulation": "GDPR",
  "OrganizationId": "00000000-0000-0000-0000-000000000001"
}
```

**Expected Response:**
```json
{
  "ProgramId": "<guid>"
}
```

**Save the ProgramId for next steps.**

---

### Step 2: Load Controls

**Request:**
```bash
POST https://localhost:7001/api/v1/compliance/programs/{programId}/controls
Content-Type: application/json
Authorization: Bearer <token-if-required>

{
  "Regulation": "GDPR"
}
```

**Expected Response:**
```json
{
  "Count": 2
}
```

---

### Step 3: Assign Tasks

**Request:**
```bash
POST https://localhost:7001/api/v1/compliance/programs/{programId}/tasks
Content-Type: application/json
Authorization: Bearer <token-if-required>

{
  "OrganizationId": "00000000-0000-0000-0000-000000000001"
}
```

**Expected Response:**
```json
{
  "Count": 0
}
```

*(Note: Task creation is placeholder, so count will be 0)*

---

### Step 4: Verify Data Before Restart

**Get Program:**
```bash
GET https://localhost:7001/api/v1/compliance/programs/{programId}
Authorization: Bearer <token-if-required>
```

**Get Controls:**
```bash
GET https://localhost:7001/api/v1/compliance/programs/{programId}/controls
Authorization: Bearer <token-if-required>
```

**Record the results:**
- Program Name: ___________
- Controls Count: ___________

---

### Step 5: Restart ComplianceService

**Option A: Docker Compose**
```bash
docker-compose -f docker-compose.dev.yml restart compliance-service-dev
```

**Option B: Manual Restart**
- Stop the ComplianceService process
- Start it again

**Wait for service to fully start** (check logs for "Now listening on...")

---

### Step 6: Verify Data After Restart

**Repeat Step 4 requests with the same ProgramId.**

**Compare Results:**
- [ ] Program Name matches
- [ ] Controls Count matches
- [ ] All data is present

---

### Step 7: Verify Database Directly

**Connect to SQL Server:**
```bash
# Using SQL Server Management Studio or sqlcmd
sqlcmd -S localhost\SQLEXPRESS -d Compliance -E
```

**Or using Docker:**
```bash
docker exec -it sqlserver-dev /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P StrongPassword@123 -d Compliance -C
```

**Run queries:**
```sql
-- Check CompliancePrograms
SELECT COUNT(*) AS ProgramCount FROM CompliancePrograms;
SELECT TOP 5 Id, PlatformId, Name, CreatedAt FROM CompliancePrograms ORDER BY CreatedAt DESC;

-- Check Controls
SELECT COUNT(*) AS ControlCount FROM Controls;
SELECT TOP 5 c.Id, c.ProgramId, c.Code, c.Description, p.Name AS ProgramName
FROM Controls c
LEFT JOIN CompliancePrograms p ON c.ProgramId = p.Id
ORDER BY c.CreatedAt DESC;

-- Check ComplianceTasks
SELECT COUNT(*) AS TaskCount FROM ComplianceTasks;

-- Check Evidence
SELECT COUNT(*) AS EvidenceCount FROM Evidence;
```

**Expected Results:**
- [ ] At least 1 ComplianceProgram exists
- [ ] At least 2 Controls exist (from Step 2)
- [ ] Controls are linked to the Program (ProgramId matches)
- [ ] No orphaned records

---

## Verification Checklist

### Code Verification
- [x] No `static Dictionary` found in Controllers
- [x] No `_storage` fields found
- [x] All controllers use Repository + UnitOfWork pattern

### Persistence Verification
- [ ] Program created successfully
- [ ] Controls loaded successfully
- [ ] Data persists after service restart
- [ ] Database contains expected records
- [ ] No data loss observed

### Architecture Verification
- [ ] All entities use EF Core persistence
- [ ] UnitOfWork.SaveChangesAsync() is called
- [ ] No in-memory state dependencies

---

## Troubleshooting

### Issue: "Connection refused" or "Cannot connect"
- Verify ComplianceService is running
- Check port (default: 5002 for Docker, 7001 for local)
- Verify firewall settings

### Issue: "401 Unauthorized"
- Check if authentication is required
- Obtain valid JWT token from IdentityService
- Add `Authorization: Bearer <token>` header

### Issue: "404 Not Found"
- Verify ProgramId is correct
- Check if program was created successfully
- Verify database connection

### Issue: "No data after restart"
- Check database connection string in appsettings.json
- Verify migrations were applied
- Check service logs for errors
- Verify UnitOfWork.SaveChangesAsync() is being called

---

## Success Criteria

✅ **Phase 0 is complete when:**
1. All data persists after service restart
2. No static in-memory collections exist
3. Database queries return expected data
4. All CRUD operations work correctly

---

## Next Steps

After Phase 0 verification:
- Proceed to Phase 1: Enhanced CRUD operations
- Add validation and business rules
- Implement file upload for Evidence
- Add pagination and filtering
