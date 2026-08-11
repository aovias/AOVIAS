# PRAMAN Event-Driven Compliance - Implementation Roadmap

**Blueprint Version:** 1.0  
**Analysis Date:** January 2025  
**Current Status:** Phase 1 Foundation Complete, Phase 2 Integration Complete

---

## 📊 Implementation Status Overview

| Category | Implemented | Remaining | Total | Completion % |
|----------|------------|-----------|-------|--------------|
| **Core Domain** | ✅ Complete | - | 100% | 100% |
| **Outbox Pattern** | ✅ Complete | - | 100% | 100% |
| **Snapshot Infrastructure** | ✅ Complete | - | 100% | 100% |
| **RabbitMQ Integration** | ✅ Complete | - | 100% | 100% |
| **Message Consumers** | ✅ Complete | - | 100% | 100% |
| **API Endpoints** | 🟡 Partial | 8+ endpoints | ~30% | 30% |
| **Journey State Machine** | 🟡 Basic | Framework policies | ~40% | 40% |
| **Evidence Management** | 🟡 Entity Only | Azure Blob Storage | ~30% | 30% |
| **Report Generation** | ❌ Not Started | All reports | 0% | 0% |
| **Renewal Engine** | ❌ Not Started | Complete engine | 0% | 0% |
| **AI Gap Detection** | ❌ Not Started | ML integration | 0% | 0% |

**Overall Completion:** ~65%

---

## ✅ What Has Been Implemented

### 1. Core Domain Model ✅
- ✅ **HospitalComplianceAggregate** - Aggregate root with (HospitalId + FrameworkId) scope
- ✅ **JourneyState Enum** - 8 states: NotStarted, Enrolled, InProgress, AwaitingReview, UnderReview, Approved, Rejected, Completed
- ✅ **TransitionJourneyState()** - Basic domain method for state transitions
- ✅ **Domain Events** - ComplianceUpdatedDomainEvent with proper serialization
- ✅ **Evidence Entity** - Enhanced with HospitalId, FrameworkId, RequirementId, Hash, UploadedBy

### 2. Outbox Pattern ✅
- ✅ **OutboxMessage Entity** - All fields per blueprint (Id, AggregateId, OccurredOn, Type, Payload, ProcessedOn, RetryCount, Error)
- ✅ **OutboxDomainEventDispatcher** - Serializes events and inserts into Outbox
- ✅ **OutboxRelayWorker** - Hangfire job polls every 5 seconds, publishes to RabbitMQ
- ✅ **Atomic Transactions** - State change + Outbox insert in single transaction
- ✅ **Indexes** - IX_Outbox_ProcessedOn, IX_Outbox_AggregateId

### 3. Snapshot Infrastructure ✅
- ✅ **FrameworkComplianceSnapshot** (ComplianceSnapshot) - Level 2 snapshot with FrameworkId, JourneyState, CriticalRiskCount
- ✅ **FrameworkChapterSnapshot** - Chapter-level breakdown with scores, task counts, risk penalties
- ✅ **OrganizationComplianceOverviewSnapshot** - Level 1 snapshot with weighted averages, worst framework, renewals
- ✅ **Database Migrations** - All tables created with proper indexes and constraints
- ✅ **Unique Constraints** - (HospitalId, FrameworkId) for framework snapshots, (HospitalId) for organization snapshot

### 4. RabbitMQ Integration ✅
- ✅ **Connection Factory** - IRabbitMQConnectionFactory with connection pooling
- ✅ **Setup Service** - Automatic exchange and queue declaration on startup
- ✅ **Exchanges** - praman.events (topic), praman.dlx (direct)
- ✅ **Queues** - compliance.outbox.relay, compliance.snapshot.framework, compliance.snapshot.chapter, compliance.snapshot.organization, compliance.dlq
- ✅ **Dead-Letter Queue** - Configured with retry mechanism
- ✅ **Connection Testing** - Test endpoint and automatic startup test

### 5. Message Consumers ✅
- ✅ **RabbitMQConsumerBase** - Base class with error handling, retry, DLQ routing
- ✅ **FrameworkSnapshotConsumer** - Updates FrameworkComplianceSnapshot
- ✅ **ChapterSnapshotConsumer** - Updates FrameworkChapterSnapshot for all chapters
- ✅ **OrganizationSnapshotConsumer** - Aggregates across frameworks for Level 1 snapshot
- ✅ **Service Registration** - All consumers registered as hosted services

### 6. Scoring Service ✅
- ✅ **CalculateFrameworkScoreAsync** - Framework-specific scoring method
- ✅ **CalculateScoreFromTasksAsync** - Shared scoring logic
- ✅ **Chapter Breakdown** - Detailed chapter-level scores

### 7. Transaction Handling ✅
- ✅ **Execution Strategy** - All transactions wrapped in CreateExecutionStrategy()
- ✅ **Retry Compatibility** - Fixed all transaction-related errors

---

## 🚧 What Is Remaining

### Phase 2: API Endpoints & Business Logic

#### 2.1 Framework Management APIs ❌
**Blueprint Requirements:**
- `GET /api/frameworks` - Get available frameworks
- `GET /api/hospitals/{hospitalId}/frameworks` - Get active frameworks for hospital
- `POST /api/hospitals/{hospitalId}/frameworks/{frameworkId}/enroll` - Enroll hospital in framework

**Current Status:**
- ❌ No dedicated Framework controller
- 🟡 `POST /api/regulations/enroll` exists but doesn't match blueprint structure
- ❌ Missing framework listing endpoints

**Dependencies:**
- None (can start immediately)

**Estimated Effort:** 2-3 days

---

#### 2.2 Journey APIs ❌
**Blueprint Requirements:**
- `POST /api/frameworks/{frameworkId}/requirements/{requirementId}/evidence` - Submit evidence
- `PATCH /api/frameworks/{frameworkId}/tasks/{taskId}` - Update task status
- `POST /api/frameworks/{frameworkId}/journey/transition` - Transition journey state

**Current Status:**
- 🟡 EvidenceController exists but may not match blueprint structure
- 🟡 TasksController exists but may not match blueprint structure
- ❌ No journey transition endpoint

**Dependencies:**
- Journey Policy implementation (2.3) for state transition validation

**Estimated Effort:** 3-4 days

---

#### 2.3 Journey Policy (Framework-Specific State Machines) ❌
**Blueprint Requirements:**
- Framework-specific state transition rules
- Validation for each framework type (NABH, CEA, PCPNDT, etc.)
- Role-based permissions for transitions

**Current Status:**
- ✅ JourneyState enum exists
- ✅ TransitionJourneyState() method exists
- ❌ No IJourneyPolicy interface
- ❌ No framework-specific policies
- ❌ No transition validation rules

**Dependencies:**
- Business requirements for each framework's state machine
- Role/permission system (if not already available)

**Estimated Effort:** 5-7 days

**Business Decisions Needed:**
1. What are the valid state transitions for NABH?
2. What are the valid state transitions for CEA?
3. What are the valid state transitions for PCPNDT?
4. Who can trigger each transition? (Role-based rules)
5. Are there framework-specific states beyond the common enum?

---

#### 2.4 Dashboard APIs 🟡
**Blueprint Requirements:**
- `GET /api/dashboard/framework/{frameworkId}` - Level 2 dashboard (Framework + Chapter snapshots)
- `GET /api/dashboard/organization` - Level 1 dashboard (Organization snapshot + framework summary)

**Current Status:**
- 🟡 `GET /api/v1/compliance/snapshots/hospital/{hospitalId}` exists but:
  - Doesn't match blueprint structure
  - Returns single snapshot (not framework-specific)
  - Missing chapter breakdown endpoint
  - Missing organization-level endpoint

**Dependencies:**
- None (snapshots are already being populated by consumers)

**Estimated Effort:** 2-3 days

---

### Phase 3: Evidence & File Management

#### 3.1 Azure Blob Storage Integration ❌
**Blueprint Requirements:**
- Path structure: `/tenant/{hospitalId}/framework/{frameworkId}/requirement/{requirementId}/file.pdf`
- File upload service
- File download service
- Hash calculation (SHA-256) for integrity

**Current Status:**
- ✅ Evidence entity has Hash, HospitalId, FrameworkId, RequirementId fields
- ❌ No Azure Blob Storage integration
- ❌ File upload/download not implemented
- ❌ Hash calculation not implemented

**Dependencies:**
- Azure Blob Storage account setup
- Azure.Storage.Blobs NuGet package
- Connection string configuration

**Estimated Effort:** 4-5 days

**Infrastructure Setup Required:**
1. Create Azure Storage Account
2. Get connection string
3. Configure in appsettings.json
4. Set up container structure

---

### Phase 4: Report Generation

#### 4.1 Report Generator Service ❌
**Blueprint Requirements:**
- Self Assessment Report
- Audit Ready Summary
- NC Register (Non-Conformance Register)
- Renewal Compliance Status
- Generate from snapshot tables only (no live aggregate queries)
- PDF/Excel output
- Versioned storage

**Current Status:**
- ❌ No report generation implemented
- ❌ No PDF library integrated
- ❌ No report templates

**Dependencies:**
- PDF generation library (QuestPDF, iTextSharp, or similar)
- Report templates (design required)
- Storage for generated reports (Azure Blob or file system)

**Estimated Effort:** 7-10 days

**Decisions Needed:**
1. Which PDF library? (QuestPDF recommended for .NET)
2. Report template format (HTML templates, Razor views, or code-based)
3. Storage location for generated reports
4. Report versioning strategy
5. Report access control

---

### Phase 5: Renewal Engine

#### 5.1 Renewal Management ❌
**Blueprint Requirements:**
- Renewal period calculation per framework
- Renewal alerts (days before deadline)
- Renewal scoring impact logic
- Scheduled background jobs

**Current Status:**
- ❌ No renewal engine
- ❌ No renewal tracking
- 🟡 OrganizationSnapshot has UpcomingRenewals field (JSON) but not populated

**Dependencies:**
- Business rules for renewal periods per framework
- Hangfire (already available)
- Framework metadata (renewal period per framework)

**Estimated Effort:** 5-7 days

**Business Decisions Needed:**
1. What is the renewal period for each framework?
2. How many days before renewal should alerts be sent?
3. Does score reset on renewal or accumulate?
4. What happens to journey state on renewal?

---

### Phase 6: Advanced Features

#### 6.1 AI-Assisted Gap Detection ❌
**Blueprint Requirements:**
- Identify compliance gaps
- ML/AI integration
- Gap analysis reports

**Current Status:**
- ❌ Not implemented
- 🟡 AIOrchestratorService exists in solution (may be used for this)

**Dependencies:**
- AI service integration (Azure OpenAI, custom ML model)
- Training data for gap detection
- Gap detection algorithm/ML model

**Estimated Effort:** 10-15 days (depends on AI approach)

---

## 📋 Stage-Wise Implementation Plan

### **STAGE 1: API Endpoints (No Dependencies)** ⚡ START HERE

**Priority:** HIGH  
**Dependencies:** None  
**Estimated Duration:** 5-7 days

#### Tasks:
1. **Framework Management APIs** (2-3 days)
   - Create `FrameworksController`
   - `GET /api/frameworks` - List all available frameworks
   - `GET /api/hospitals/{hospitalId}/frameworks` - Get hospital's enrolled frameworks
   - `POST /api/hospitals/{hospitalId}/frameworks/{frameworkId}/enroll` - Enrollment logic
   - Service layer: `IFrameworkService` with enrollment validation

2. **Dashboard APIs** (2-3 days)
   - Update `SnapshotsController` or create new `DashboardController`
   - `GET /api/dashboard/framework/{frameworkId}` - Return FrameworkComplianceSnapshot + FrameworkChapterSnapshots
   - `GET /api/dashboard/organization` - Return OrganizationComplianceOverviewSnapshot + framework summaries
   - DTOs for dashboard responses

3. **Task & Evidence APIs** (1 day)
   - Update existing controllers to match blueprint structure
   - Ensure endpoints match: `/api/frameworks/{frameworkId}/tasks/{taskId}`
   - Ensure endpoints match: `/api/frameworks/{frameworkId}/requirements/{requirementId}/evidence`

**Why Start Here:**
- No external dependencies
- Provides immediate API coverage
- Enables frontend development
- Uses existing snapshot infrastructure

---

### **STAGE 2: Journey State Machine (Depends on Business Rules)** 

**Priority:** HIGH  
**Dependencies:** Business requirements for framework state machines  
**Estimated Duration:** 5-7 days

#### Tasks:
1. **Journey Policy Interface** (1 day)
   - Create `IJourneyPolicy` interface
   - Define transition validation contract
   - Create base implementation

2. **Framework-Specific Policies** (3-4 days)
   - `NabhJourneyPolicy` - NABH-specific state machine
   - `CeaJourneyPolicy` - CEA-specific state machine
   - `PcpndtJourneyPolicy` - PCPNDT-specific state machine
   - `DefaultJourneyPolicy` - Generic policy for other frameworks

3. **Journey Transition Endpoint** (1 day)
   - `POST /api/frameworks/{frameworkId}/journey/transition`
   - Validate transition using appropriate policy
   - Update aggregate and emit event

4. **Role-Based Permissions** (1-2 days)
   - Integrate with existing auth system
   - Define permission requirements per transition
   - Validate permissions in controller/service

**Dependencies to Resolve:**
- **Business Requirements:** Need state machine definitions for each framework
- **Role System:** Verify existing role/permission system supports this

**Blockers:**
- Cannot proceed without business rules for state transitions

---

### **STAGE 3: Azure Blob Storage (Infrastructure Dependency)**

**Priority:** MEDIUM  
**Dependencies:** Azure Storage Account setup  
**Estimated Duration:** 4-5 days

#### Tasks:
1. **Azure Setup** (0.5 day)
   - Create Azure Storage Account
   - Get connection string
   - Configure in appsettings.json

2. **Blob Storage Service** (2 days)
   - Create `IBlobStorageService` interface
   - Implement `AzureBlobStorageService`
   - Path structure: `/tenant/{hospitalId}/framework/{frameworkId}/requirement/{requirementId}/`
   - File upload with hash calculation (SHA-256)
   - File download with SAS tokens (optional)

3. **Evidence Upload Integration** (1.5 days)
   - Update EvidenceController to use blob storage
   - Calculate file hash on upload
   - Store blob URL in Evidence.FileUrl
   - Update Evidence entity with hash

4. **Testing** (1 day)
   - Test file upload/download
   - Test hash calculation
   - Test path structure
   - Test multi-tenant isolation

**Dependencies to Resolve:**
- Azure Storage Account creation
- Connection string configuration
- NuGet package: `Azure.Storage.Blobs`

**Blockers:**
- Requires Azure account and credentials

---

### **STAGE 4: Report Generation (Library Dependency)**

**Priority:** MEDIUM  
**Dependencies:** PDF library selection, report templates  
**Estimated Duration:** 7-10 days

#### Tasks:
1. **PDF Library Integration** (1 day)
   - Select library (QuestPDF recommended)
   - Add NuGet package
   - Create base report service

2. **Report Templates** (3-4 days)
   - Design Self Assessment Report template
   - Design Audit Ready Summary template
   - Design NC Register template
   - Design Renewal Compliance Status template
   - Create template models/DTOs

3. **Report Service Implementation** (2-3 days)
   - `IReportService` interface
   - Implement each report type
   - Load data from snapshot tables only
   - Generate PDF/Excel output

4. **Report Storage** (1 day)
   - Store generated reports (Azure Blob or file system)
   - Version reports
   - Track report generation events

5. **Report API Endpoints** (1 day)
   - `POST /api/reports/self-assessment`
   - `POST /api/reports/audit-ready`
   - `POST /api/reports/nc-register`
   - `POST /api/reports/renewal-status`
   - `GET /api/reports/{reportId}/download`

**Dependencies to Resolve:**
- PDF library selection (QuestPDF vs iTextSharp vs others)
- Report template design
- Storage location decision

**Blockers:**
- Requires template design decisions
- May depend on Stage 3 (Azure Blob) for report storage

---

### **STAGE 5: Renewal Engine (Business Logic Dependency)**

**Priority:** MEDIUM  
**Dependencies:** Business rules for renewal periods  
**Estimated Duration:** 5-7 days

#### Tasks:
1. **Renewal Configuration** (1 day)
   - Add renewal period to Framework entity or configuration
   - Define renewal calculation logic

2. **Renewal Calculation Service** (2 days)
   - Calculate renewal dates per framework
   - Calculate days until renewal
   - Identify upcoming renewals

3. **Renewal Alert System** (1 day)
   - Background job to check renewals
   - Alert generation (email/notification)
   - Integration with notification system

4. **Renewal Impact on Scoring** (2 days)
   - Define renewal scoring logic
   - Update scoring service if needed
   - Handle score reset vs accumulation

5. **Renewal API Endpoints** (1 day)
   - `GET /api/frameworks/{frameworkId}/renewal-status`
   - `GET /api/hospitals/{hospitalId}/upcoming-renewals`

**Dependencies to Resolve:**
- Business rules: Renewal period per framework
- Business rules: Alert timing (how many days before?)
- Business rules: Score impact on renewal

**Blockers:**
- Requires business decisions on renewal logic

---

### **STAGE 6: AI Gap Detection (Advanced Feature)**

**Priority:** LOW  
**Dependencies:** AI service integration, ML model/training  
**Estimated Duration:** 10-15 days

#### Tasks:
1. **AI Service Integration** (3-4 days)
   - Integrate with Azure OpenAI or custom ML service
   - Create gap detection service interface
   - Implement gap analysis algorithm

2. **Gap Detection Logic** (4-5 days)
   - Analyze compliance gaps
   - Identify missing evidence
   - Identify incomplete tasks
   - Generate gap reports

3. **Gap Detection API** (2-3 days)
   - `POST /api/frameworks/{frameworkId}/gap-analysis`
   - Return gap analysis results
   - Integration with report generation

**Dependencies to Resolve:**
- AI service selection (Azure OpenAI vs custom ML)
- Training data or prompt engineering
- Gap detection algorithm design

**Blockers:**
- Requires AI/ML expertise
- May require training data collection

---

## 🔗 Dependency Graph

```
STAGE 1 (API Endpoints)
  └─ No dependencies ✅ START HERE

STAGE 2 (Journey State Machine)
  └─ Depends on: Business rules for state machines
  └─ Can start in parallel with Stage 1 (different endpoints)

STAGE 3 (Azure Blob Storage)
  └─ Depends on: Azure account setup
  └─ Independent of other stages

STAGE 4 (Report Generation)
  └─ Depends on: PDF library, templates
  └─ Can use Stage 3 for report storage (optional)

STAGE 5 (Renewal Engine)
  └─ Depends on: Business rules for renewal periods
  └─ Independent of other stages

STAGE 6 (AI Gap Detection)
  └─ Depends on: AI service, ML model
  └─ Can be done last (advanced feature)
```

---

## 📝 Recommended Execution Order

### **Immediate (Week 1-2):**
1. ✅ **STAGE 1: API Endpoints** - No blockers, immediate value
2. 🔄 **STAGE 2: Journey State Machine** - Start if business rules are available

### **Short-term (Week 3-4):**
3. ✅ **STAGE 3: Azure Blob Storage** - If Azure account is ready
4. 🔄 **STAGE 4: Report Generation** - Start template design in parallel

### **Medium-term (Week 5-6):**
5. ✅ **STAGE 4: Report Generation** - Complete implementation
6. 🔄 **STAGE 5: Renewal Engine** - If business rules are defined

### **Long-term (Week 7+):**
7. ✅ **STAGE 6: AI Gap Detection** - Advanced feature, can be deferred

---

## 🎯 Quick Wins (Can Start Immediately)

1. **Framework Management APIs** - 2-3 days, no dependencies
2. **Dashboard APIs** - 2-3 days, snapshots already exist
3. **Task/Evidence API Updates** - 1 day, align with blueprint structure

**Total Quick Wins:** 5-7 days of development

---

## ⚠️ Blockers & Dependencies Summary

### **Infrastructure Dependencies:**
- ✅ RabbitMQ - **RESOLVED** (Docker setup complete)
- ❌ Azure Blob Storage - **PENDING** (needs account setup)
- ✅ Hangfire - **RESOLVED** (already configured)
- ✅ Database - **RESOLVED** (migrations applied)

### **Business Logic Dependencies:**
- ❌ Framework state machine rules - **PENDING** (needs business input)
- ❌ Renewal period definitions - **PENDING** (needs business input)
- ❌ Role-based permission rules - **PENDING** (verify existing system)

### **Technical Dependencies:**
- ❌ PDF library selection - **PENDING** (decision needed)
- ❌ Report template design - **PENDING** (design needed)
- ❌ AI service selection - **PENDING** (for Stage 6)

---

## 📊 Completion Estimates

| Stage | Duration | Dependencies | Can Start? |
|-------|----------|--------------|------------|
| **Stage 1: APIs** | 5-7 days | None | ✅ YES |
| **Stage 2: Journey** | 5-7 days | Business rules | ⚠️ PARTIAL |
| **Stage 3: Azure Blob** | 4-5 days | Azure account | ⚠️ PENDING |
| **Stage 4: Reports** | 7-10 days | PDF library, templates | ⚠️ PARTIAL |
| **Stage 5: Renewal** | 5-7 days | Business rules | ⚠️ PENDING |
| **Stage 6: AI** | 10-15 days | AI service | ❌ DEFER |

**Total Remaining:** 36-51 days (excluding Stage 6)

---

## 🚀 Next Steps (Action Items)

### **This Week:**
1. ✅ Start **Stage 1: API Endpoints** (Framework Management + Dashboard APIs)
2. 📋 Gather business requirements for **Stage 2: Journey State Machine**
3. 🔧 Set up Azure Storage Account for **Stage 3**

### **Next Week:**
4. ✅ Complete **Stage 1** APIs
5. 🔄 Start **Stage 2** if business rules are ready
6. 🔄 Start **Stage 3** if Azure is ready

### **Ongoing:**
7. 📋 Design report templates for **Stage 4**
8. 📋 Define renewal business rules for **Stage 5**
9. 📋 Evaluate PDF libraries for **Stage 4**

---

## 📚 Reference: Blueprint Requirements Checklist

### Section 2: Domain Model ✅
- [x] HospitalComplianceAggregate with (HospitalId + FrameworkId) scope
- [x] JourneyState enum
- [x] Evidence entity with multi-tenant fields
- [x] Framework, Requirement, Task entities

### Section 3: Outbox Pattern ✅
- [x] OutboxMessages table with all fields
- [x] Atomic transaction with state changes
- [x] Outbox relay worker
- [x] Retry mechanism

### Section 4: Snapshot Design ✅
- [x] FrameworkComplianceSnapshot (Level 2)
- [x] FrameworkChapterSnapshot (Level 2)
- [x] OrganizationComplianceOverviewSnapshot (Level 1)
- [x] Proper indexes and constraints

### Section 5: API Specification 🟡
- [ ] Framework Management APIs (3 endpoints)
- [ ] Journey APIs (3 endpoints)
- [ ] Dashboard APIs (2 endpoints) - Partially implemented

### Section 6: Event Flow ✅
- [x] Command Handler Pattern
- [x] Outbox Relay Worker
- [x] Projection Handlers (Consumers)

### Section 7: Feature Implementation 🟡
- [ ] Evidence & File Upload (Azure Blob) - Entity ready, storage pending
- [ ] Report Generator - Not started
- [ ] Journey Policy - Basic implementation, policies pending
- [ ] Renewal Engine - Not started

---

## 📋 Detailed Task Breakdown

### STAGE 1: API Endpoints - Detailed Tasks

#### Task 1.1: Framework Management Controller
**Files to Create:**
- `ComplianceService/Controllers/V1/FrameworksController.cs`
- `ComplianceService/Application/Contracts/IFrameworkService.cs`
- `ComplianceService/Application/Services/FrameworkService.cs`
- `ComplianceService/Application/DTOs/FrameworkDto.cs`
- `ComplianceService/Application/DTOs/EnrollFrameworkRequest.cs`

**Endpoints:**
```csharp
[HttpGet("frameworks")]
public async Task<IActionResult> GetAvailableFrameworks()

[HttpGet("hospitals/{hospitalId}/frameworks")]
public async Task<IActionResult> GetHospitalFrameworks(Guid hospitalId)

[HttpPost("hospitals/{hospitalId}/frameworks/{frameworkId}/enroll")]
public async Task<IActionResult> EnrollInFramework(Guid hospitalId, Guid frameworkId)
```

**Business Logic:**
- Enrollment validation (can hospital enroll in multiple frameworks?)
- Framework version compatibility checks
- Initial journey state setup (Enrolled)
- Create HospitalComplianceAggregate on enrollment

---

#### Task 1.2: Dashboard Controller
**Files to Create/Update:**
- `ComplianceService/Controllers/V1/DashboardController.cs` (new)
- `ComplianceService/Application/DTOs/FrameworkDashboardDto.cs`
- `ComplianceService/Application/DTOs/OrganizationDashboardDto.cs`
- Update `ComplianceService/Application/Contracts/ISnapshotService.cs`

**Endpoints:**
```csharp
[HttpGet("dashboard/framework/{frameworkId}")]
public async Task<IActionResult> GetFrameworkDashboard(Guid frameworkId)

[HttpGet("dashboard/organization")]
public async Task<IActionResult> GetOrganizationDashboard()
```

**Response Structure:**
- Framework Dashboard: FrameworkComplianceSnapshot + List<FrameworkChapterSnapshot>
- Organization Dashboard: OrganizationComplianceOverviewSnapshot + List<FrameworkSummary>

---

#### Task 1.3: Update Existing Controllers
**Files to Update:**
- `ComplianceService/Controllers/V1/EvidenceController.cs`
- `ComplianceService/Controllers/V1/TasksController.cs`

**Changes:**
- Update routes to match blueprint: `/api/frameworks/{frameworkId}/...`
- Ensure proper framework context in all operations
- Add framework validation

---

### STAGE 2: Journey State Machine - Detailed Tasks

#### Task 2.1: Journey Policy Interface
**Files to Create:**
- `ComplianceService/Domain/Policies/IJourneyPolicy.cs`
- `ComplianceService/Domain/Policies/BaseJourneyPolicy.cs`

**Interface:**
```csharp
public interface IJourneyPolicy
{
    bool CanTransition(JourneyState from, JourneyState to, Guid frameworkId);
    IEnumerable<JourneyState> GetValidTransitions(JourneyState current, Guid frameworkId);
    bool RequiresRole(JourneyState from, JourneyState to, string role);
    string GetPolicyName();
}
```

---

#### Task 2.2: Framework-Specific Policies
**Files to Create:**
- `ComplianceService/Domain/Policies/NabhJourneyPolicy.cs`
- `ComplianceService/Domain/Policies/CeaJourneyPolicy.cs`
- `ComplianceService/Domain/Policies/PcpndtJourneyPolicy.cs`
- `ComplianceService/Domain/Policies/DefaultJourneyPolicy.cs`

**Policy Factory:**
- `ComplianceService/Domain/Policies/JourneyPolicyFactory.cs` - Selects policy based on framework type

---

#### Task 2.3: Journey Transition Endpoint
**Files to Create:**
- `ComplianceService/Controllers/V1/JourneyController.cs`
- `ComplianceService/Application/DTOs/TransitionJourneyRequest.cs`

**Endpoint:**
```csharp
[HttpPost("frameworks/{frameworkId}/journey/transition")]
public async Task<IActionResult> TransitionJourneyState(
    Guid frameworkId, 
    [FromBody] TransitionJourneyRequest request)
```

**Validation:**
- Load appropriate policy
- Validate transition is allowed
- Check role permissions
- Update aggregate
- Emit domain event

---

### STAGE 3: Azure Blob Storage - Detailed Tasks

#### Task 3.1: Blob Storage Service
**Files to Create:**
- `ComplianceService/Infrastructure/Storage/IBlobStorageService.cs`
- `ComplianceService/Infrastructure/Storage/AzureBlobStorageService.cs`
- `ComplianceService/Infrastructure/Storage/BlobStorageOptions.cs`

**Configuration:**
```json
"BlobStorage": {
  "ConnectionString": "...",
  "ContainerName": "compliance-evidence",
  "PathTemplate": "tenant/{hospitalId}/framework/{frameworkId}/requirement/{requirementId}"
}
```

---

#### Task 3.2: Evidence Upload Integration
**Files to Update:**
- `ComplianceService/Controllers/V1/EvidenceController.cs`
- `ComplianceService/Application/Services/EvidenceService.cs` (new)

**Changes:**
- Accept file upload (multipart/form-data)
- Calculate SHA-256 hash
- Upload to Azure Blob
- Store blob URL and hash in Evidence entity
- Update path structure

---

### STAGE 4: Report Generation - Detailed Tasks

#### Task 4.1: PDF Library Integration
**NuGet Package:**
- QuestPDF (recommended) or iTextSharp

**Files to Create:**
- `ComplianceService/Application/Reports/IReportService.cs`
- `ComplianceService/Application/Reports/ReportService.cs`
- `ComplianceService/Application/Reports/Templates/` (folder)

---

#### Task 4.2: Report Templates
**Files to Create:**
- `ComplianceService/Application/Reports/Templates/SelfAssessmentReportTemplate.cs`
- `ComplianceService/Application/Reports/Templates/AuditReadySummaryTemplate.cs`
- `ComplianceService/Application/Reports/Templates/NcRegisterTemplate.cs`
- `ComplianceService/Application/Reports/Templates/RenewalStatusTemplate.cs`

**Data Sources:**
- All reports read from snapshot tables only
- No live aggregate queries

---

### STAGE 5: Renewal Engine - Detailed Tasks

#### Task 5.1: Renewal Configuration
**Files to Update:**
- `ComplianceService/Domain/ComplianceFramework.cs` - Add RenewalPeriodDays property
- Or create `ComplianceService/Application/Configuration/RenewalConfiguration.cs`

---

#### Task 5.2: Renewal Service
**Files to Create:**
- `ComplianceService/Application/Services/IRenewalService.cs`
- `ComplianceService/Application/Services/RenewalService.cs`
- `ComplianceService/Application/BackgroundJobs/RenewalAlertJob.cs`

**Background Job:**
- Scheduled daily to check upcoming renewals
- Generate alerts for renewals within X days
- Update OrganizationSnapshot.UpcomingRenewals

---

## 🎯 Success Criteria

### Stage 1 Complete When:
- [ ] All 8 API endpoints implemented and tested
- [ ] Endpoints match blueprint structure
- [ ] Integration tests pass
- [ ] API documentation updated

### Stage 2 Complete When:
- [ ] Journey policies implemented for all framework types
- [ ] State transitions validated
- [ ] Role-based permissions enforced
- [ ] Journey transition endpoint functional

### Stage 3 Complete When:
- [ ] Files upload to Azure Blob Storage
- [ ] Hash calculation working
- [ ] File download functional
- [ ] Multi-tenant path structure verified

### Stage 4 Complete When:
- [ ] All 4 report types generate successfully
- [ ] Reports read from snapshots only
- [ ] PDF output correct
- [ ] Report storage and versioning working

### Stage 5 Complete When:
- [ ] Renewal dates calculated correctly
- [ ] Alerts generated on schedule
- [ ] OrganizationSnapshot.UpcomingRenewals populated
- [ ] Renewal API endpoints functional

---

## 📝 Notes & Considerations

1. **API Versioning:** Current APIs use `/api/v1/` prefix. Blueprint doesn't specify versioning. Consider alignment.

2. **Authentication:** All endpoints should use existing auth system. Verify role/permission system supports journey transitions.

3. **Error Handling:** Implement consistent error responses across all APIs.

4. **Validation:** Add FluentValidation or similar for all request DTOs.

5. **Testing:** Each stage should include:
   - Unit tests for services
   - Integration tests for APIs
   - End-to-end tests for critical flows

6. **Documentation:** Update API documentation (Swagger/OpenAPI) as endpoints are added.

7. **Performance:** Dashboard APIs should be fast (reading from snapshots). Monitor query performance.

8. **Monitoring:** Add logging and metrics for all new endpoints and services.

---

**Report Generated:** January 2025  
**Status:** Ready for Stage 1 Implementation  
**Recommendation:** Start with API Endpoints (Stage 1) as it has no dependencies and provides immediate value.
