# Manual Verification Checklist - Data Persistence & UnitOfWork

## Prerequisites
- [ ] SQL Server is running (or Docker container `sqlserver-dev` is up)
- [ ] ComplianceService is configured with database connection
- [ ] AuditService is configured with database connection
- [ ] Both services can connect to their respective databases
- [ ] JWT token with appropriate scopes (`compliance.api`, `audit.api`) is available

---

## 1. Compliance Data Persistence Across Service Restarts

### 1.1 ComplianceProgram Persistence
- [ ] **Create a ComplianceProgram**
  - Call: `POST /api/v1.0/compliance/programs`
  - Body: `{ "Regulation": "NABH", "OrganizationId": "<guid>" }`
  - Note the returned `ProgramId` from response

- [ ] **Verify Program Exists Before Restart**
  - Call: `GET /api/v1.0/compliance/programs/{programId}`
  - Verify: Returns program with correct data
  - Note: `PlatformId`, `Name`, `OrganizationId` values

- [ ] **Restart ComplianceService**
  - Stop the service completely
  - Wait 5 seconds
  - Start the service again
  - Verify service starts successfully

- [ ] **Verify Program Exists After Restart**
  - Call: `GET /api/v1.0/compliance/programs/{programId}` (same ID)
  - Verify: Returns same program with identical data
  - Check: `PlatformId`, `Name`, `OrganizationId` match pre-restart values

### 1.2 Control Persistence
- [ ] **Create Controls for a Program**
  - Call: `POST /api/v1.0/compliance/programs/{programId}/controls`
  - Body: `{ "Regulation": "NABH" }`
  - Note: Response should indicate controls were created

- [ ] **Verify Controls Before Restart**
  - Call: `GET /api/v1.0/compliance/programs/{programId}/controls`
  - Verify: Returns list of controls
  - Count the number of controls returned

- [ ] **Restart ComplianceService**

- [ ] **Verify Controls After Restart**
  - Call: `GET /api/v1.0/compliance/programs/{programId}/controls` (same programId)
  - Verify: Returns same number of controls
  - Verify: Control `Code`, `Description`, `ProgramId` match pre-restart

### 1.3 ComplianceTask Persistence
- [ ] **Create a ComplianceTask** (if task creation logic is enabled)
  - Call: `POST /api/v1.0/compliance/programs/{programId}/tasks`
  - Body: `{ "OrganizationId": "<guid>" }`
  - Note: If tasks are created, note the task IDs

- [ ] **Verify Tasks Before Restart** (if applicable)
  - Call: `GET /api/v1.0/compliance/programs/{programId}/tasks`
  - Verify: Returns list of tasks
  - Note: Task count and key properties

- [ ] **Restart ComplianceService**

- [ ] **Verify Tasks After Restart** (if applicable)
  - Call: `GET /api/v1.0/compliance/programs/{programId}/tasks` (same programId)
  - Verify: Returns same tasks with identical data

### 1.4 Task Status Update Persistence
- [ ] **Update Task Status**
  - Call: `PUT /api/v1.0/compliance/tasks/{taskId}/status`
  - Body: `{ "Status": "InProgress" }`
  - Note: Original status value

- [ ] **Verify Status Update Before Restart**
  - Call: `GET /api/v1.0/compliance/programs/{programId}/tasks`
  - Find the updated task
  - Verify: Status is "InProgress"

- [ ] **Restart ComplianceService**

- [ ] **Verify Status Persists After Restart**
  - Call: `GET /api/v1.0/compliance/programs/{programId}/tasks`
  - Find the same task
  - Verify: Status is still "InProgress" (not reverted)

---

## 2. Audit Data Persistence Across Service Restarts

### 2.1 AuditSession Persistence
- [ ] **Create an AuditSession**
  - Call: `POST /api/v1.0/audit/generate`
  - Body: `{ "OrganizationId": "<guid>", "ComplianceProgramId": "<guid>" }`
  - Note the returned `Id` and `PlatformId` from response

- [ ] **Verify Session Exists Before Restart**
  - Call: `GET /api/v1.0/audit/{sessionId}`
  - Verify: Returns session with correct data
  - Note: `PlatformId`, `Status`, `OrganizationId`, `ComplianceProgramId` values

- [ ] **Restart AuditService**
  - Stop the service completely
  - Wait 5 seconds
  - Start the service again
  - Verify service starts successfully

- [ ] **Verify Session Exists After Restart**
  - Call: `GET /api/v1.0/audit/{sessionId}` (same ID)
  - Verify: Returns same session with identical data
  - Check: `PlatformId`, `Status`, `OrganizationId`, `ComplianceProgramId` match pre-restart values

### 2.2 AuditSession List Persistence
- [ ] **Create Multiple AuditSessions**
  - Call: `POST /api/v1.0/audit/generate` 2-3 times with same OrganizationId
  - Note: All session IDs created

- [ ] **Verify Sessions List Before Restart**
  - Call: `GET /api/v1.0/audit/organizations/{organizationId}`
  - Verify: Returns all created sessions
  - Count the number of sessions

- [ ] **Restart AuditService**

- [ ] **Verify Sessions List After Restart**
  - Call: `GET /api/v1.0/audit/organizations/{organizationId}` (same organizationId)
  - Verify: Returns same number of sessions
  - Verify: All previously noted session IDs are present
  - Verify: Session data matches pre-restart values

---

## 3. No Endpoint Depends on In-Memory State

### 3.1 Verify No Static Dictionaries in Controllers
- [ ] **Code Review: ProgramsController**
  - Open: `ComplianceService/Controllers/V1/ProgramsController.cs`
  - Verify: No `static readonly Dictionary` fields
  - Verify: All operations use `_repository` and `_unitOfWork`

- [ ] **Code Review: ControlsController**
  - Open: `ComplianceService/Controllers/V1/ControlsController.cs`
  - Verify: No `static readonly Dictionary` fields
  - Verify: All operations use `_repository` and `_unitOfWork`

- [ ] **Code Review: TasksController**
  - Open: `ComplianceService/Controllers/V1/TasksController.cs`
  - Verify: No `static readonly Dictionary` fields
  - Verify: All operations use `_repository` and `_unitOfWork`

- [ ] **Code Review: AuditController**
  - Open: `AuditService/Controllers/V1/AuditController.cs`
  - Verify: No `static readonly Dictionary` fields
  - Verify: All operations use `_service` (which uses repository)

### 3.2 Verify Data Survives Multiple Requests (No In-Memory State)
- [ ] **Create Entity in Request 1**
  - Call: `POST /api/v1.0/compliance/programs` (Request 1)
  - Note: `ProgramId` from response

- [ ] **Verify Entity in Request 2** (New HTTP request, same service instance)
  - Wait 2 seconds
  - Call: `GET /api/v1.0/compliance/programs/{programId}` (Request 2)
  - Verify: Entity exists and data is correct

- [ ] **Verify Entity in Request 3** (New HTTP request, same service instance)
  - Wait 2 seconds
  - Call: `GET /api/v1.0/compliance/programs/{programId}` (Request 3)
  - Verify: Entity still exists with same data

- [ ] **Repeat for AuditService**
  - Create audit session in Request 1
  - Retrieve in Request 2 (new HTTP request)
  - Retrieve in Request 3 (new HTTP request)
  - Verify: Data persists across requests

### 3.3 Verify Concurrent Requests Don't Share State
- [ ] **Create Multiple Entities Concurrently**
  - Simultaneously call `POST /api/v1.0/compliance/programs` 3 times
  - Use different OrganizationIds or Regulations
  - Note: All 3 ProgramIds returned

- [ ] **Verify Each Entity is Independent**
  - Call: `GET /api/v1.0/compliance/programs/{programId1}`
  - Call: `GET /api/v1.0/compliance/programs/{programId2}`
  - Call: `GET /api/v1.0/compliance/programs/{programId3}`
  - Verify: Each returns correct, independent data
  - Verify: No data mixing or corruption

---

## 4. UnitOfWork SaveChanges is Always Executed

### 4.1 Verify SaveChangesAsync is Called in Controllers
- [ ] **Code Review: ProgramsController.Create**
  - Open: `ComplianceService/Controllers/V1/ProgramsController.cs`
  - Verify: `await _unitOfWork.SaveChangesAsync(ct)` is called after `AddAsync`
  - Verify: No early return before `SaveChangesAsync`

- [ ] **Code Review: ControlsController.Load**
  - Open: `ComplianceService/Controllers/V1/ControlsController.cs`
  - Verify: `await _unitOfWork.SaveChangesAsync(ct)` is called after all `AddAsync` calls
  - Verify: No early return before `SaveChangesAsync`

- [ ] **Code Review: TasksController.Assign**
  - Open: `ComplianceService/Controllers/V1/TasksController.cs`
  - Verify: `await _unitOfWork.SaveChangesAsync(ct)` is called after all `AddAsync` calls
  - Verify: No early return before `SaveChangesAsync`

- [ ] **Code Review: TasksController.UpdateStatus**
  - Open: `ComplianceService/Controllers/V1/TasksController.cs`
  - Verify: `await _unitOfWork.SaveChangesAsync(ct)` is called after `Update`
  - Verify: No early return before `SaveChangesAsync`

- [ ] **Code Review: AuditAppService.GenerateAuditAsync**
  - Open: `AuditService/Application/Services/AuditAppService.cs`
  - Verify: `await _unitOfWork.SaveChangesAsync(ct)` is called after `AddAsync`
  - Verify: No early return before `SaveChangesAsync`

### 4.2 Verify Repositories Don't Call SaveChangesAsync
- [ ] **Code Review: All Repository Implementations**
  - Open: `ComplianceService/Infrastructure/Repositories/ComplianceProgramRepository.cs`
  - Verify: No `SaveChangesAsync` calls
  - Open: `ComplianceService/Infrastructure/Repositories/ControlRepository.cs`
  - Verify: No `SaveChangesAsync` calls
  - Open: `ComplianceService/Infrastructure/Repositories/ComplianceTaskRepository.cs`
  - Verify: No `SaveChangesAsync` calls
  - Open: `AuditService/Infrastructure/Repositories/AuditRepository.cs`
  - Verify: No `SaveChangesAsync` calls

### 4.3 Verify SaveChangesAsync is Actually Executed (Logging Verification)
- [ ] **Enable Detailed Logging**
  - Set log level to `Information` or `Debug` in `appsettings.Development.json`
  - Restart services

- [ ] **Create ComplianceProgram and Check Logs**
  - Call: `POST /api/v1.0/compliance/programs`
  - Check logs for: `"Successfully saved {SavedCount} entity/entities to database"`
  - Verify: `SavedCount > 0`

- [ ] **Create AuditSession and Check Logs**
  - Call: `POST /api/v1.0/audit/generate`
  - Check logs for: `"Successfully saved {SavedCount} entity/entities to database"`
  - Check logs for: `"Persisted audit session {SessionId} to database. Saved {SavedCount} entity/entities"`
  - Verify: `SavedCount > 0`

- [ ] **Update Task Status and Check Logs**
  - Call: `PUT /api/v1.0/compliance/tasks/{taskId}/status`
  - Check logs for: `"Successfully saved {SavedCount} entity/entities to database"`
  - Verify: `SavedCount > 0`

### 4.4 Verify Data is Actually Persisted (Database Verification)
- [ ] **Create Entity and Verify in Database**
  - Call: `POST /api/v1.0/compliance/programs`
  - Note: `ProgramId` from response
  - Connect to SQL Server database directly
  - Query: `SELECT * FROM CompliancePrograms WHERE Id = '<programId>'`
  - Verify: Row exists with correct data

- [ ] **Create AuditSession and Verify in Database**
  - Call: `POST /api/v1.0/audit/generate`
  - Note: `Id` from response
  - Connect to SQL Server database directly
  - Query: `SELECT * FROM AuditSessions WHERE Id = '<sessionId>'`
  - Verify: Row exists with correct data

- [ ] **Update Task and Verify in Database**
  - Call: `PUT /api/v1.0/compliance/tasks/{taskId}/status` with new status
  - Connect to SQL Server database directly
  - Query: `SELECT * FROM ComplianceTasks WHERE Id = '<taskId>'`
  - Verify: `Status` column matches the updated value

---

## 5. Integration Verification

### 5.1 End-to-End Flow: Create → Restart → Retrieve
- [ ] **Complete Flow Test - ComplianceProgram**
  1. Create program: `POST /api/v1.0/compliance/programs`
  2. Note ProgramId
  3. Verify exists: `GET /api/v1.0/compliance/programs/{programId}`
  4. **Restart ComplianceService**
  5. Verify still exists: `GET /api/v1.0/compliance/programs/{programId}`
  6. Verify data unchanged

- [ ] **Complete Flow Test - AuditSession**
  1. Create session: `POST /api/v1.0/audit/generate`
  2. Note SessionId
  3. Verify exists: `GET /api/v1.0/audit/{sessionId}`
  4. **Restart AuditService**
  5. Verify still exists: `GET /api/v1.0/audit/{sessionId}`
  6. Verify data unchanged

### 5.2 Verify PlatformSequence Persistence
- [ ] **Create Multiple Programs and Verify Sequential PlatformIds**
  - Call: `POST /api/v1.0/compliance/programs` (Program 1)
  - Note: `PlatformId` (e.g., "PROG000001")
  - **Restart ComplianceService**
  - Call: `POST /api/v1.0/compliance/programs` (Program 2)
  - Note: `PlatformId` (should be "PROG000002" or higher)
  - Verify: PlatformId sequence continues correctly after restart

- [ ] **Create Multiple AuditSessions and Verify Sequential PlatformIds**
  - Call: `POST /api/v1.0/audit/generate` (Session 1)
  - Note: `PlatformId` (e.g., "AUD000001")
  - **Restart AuditService**
  - Call: `POST /api/v1.0/audit/generate` (Session 2)
  - Note: `PlatformId` (should be "AUD000002" or higher)
  - Verify: PlatformId sequence continues correctly after restart

---

## 6. Error Scenarios (Negative Testing)

### 6.1 Verify NotFound Responses
- [ ] **Get Non-Existent ComplianceProgram**
  - Call: `GET /api/v1.0/compliance/programs/{nonExistentGuid}`
  - Verify: Returns 404 Not Found

- [ ] **Get Non-Existent AuditSession**
  - Call: `GET /api/v1.0/audit/{nonExistentGuid}`
  - Verify: Returns 404 Not Found

- [ ] **Update Non-Existent Task**
  - Call: `PUT /api/v1.0/compliance/tasks/{nonExistentGuid}/status`
  - Verify: Returns 404 Not Found

### 6.2 Verify Empty Results
- [ ] **Get Controls for Program with No Controls**
  - Create a new program
  - Call: `GET /api/v1.0/compliance/programs/{programId}/controls`
  - Verify: Returns empty array `[]` (not null, not error)

- [ ] **Get Tasks for Program with No Tasks**
  - Create a new program
  - Call: `GET /api/v1.0/compliance/programs/{programId}/tasks`
  - Verify: Returns empty array `[]` (not null, not error)

- [ ] **Get AuditSessions for Organization with No Sessions**
  - Call: `GET /api/v1.0/audit/organizations/{organizationIdWithNoSessions}`
  - Verify: Returns empty array `[]` (not null, not error)

---

## Verification Summary

### Success Criteria
- ✅ All created entities persist after service restart
- ✅ All updated entities persist changes after service restart
- ✅ No static dictionaries found in controller code
- ✅ All create/update operations call `SaveChangesAsync`
- ✅ Repositories do not call `SaveChangesAsync` directly
- ✅ Logs confirm `SaveChangesAsync` execution
- ✅ Database queries confirm data persistence
- ✅ PlatformId sequences continue correctly after restart

### Failure Indicators
- ❌ Entity not found after service restart
- ❌ Entity data changed after service restart
- ❌ Static dictionary found in controller
- ❌ No `SaveChangesAsync` call in controller/service
- ❌ Repository calls `SaveChangesAsync` directly
- ❌ No log entry for `SaveChangesAsync`
- ❌ Database query returns no rows
- ❌ PlatformId sequence resets after restart

---

## Notes
- Use a REST client (Postman, Insomnia, curl) or HTTP file for testing
- Keep a log of all created entity IDs for verification
- Test with different OrganizationIds to avoid conflicts
- Verify database connection strings are correct before testing
- Check service logs for any errors during operations
