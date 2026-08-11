# Compliance Platform - Architectural Documentation

**Generated:** February 2025  
**Project:** Compliance Platform v1.0  
**Architecture:** Microservices (.NET 10.0)

---

## Table of Contents

1. [Solution Overview](#1-solution-overview)
2. [Service-Level Details](#2-service-level-details)
3. [Messaging Architecture](#3-messaging-architecture)
4. [Authentication & Authorization](#4-authentication--authorization)
5. [Compliance Module Deep Dive](#5-compliance-module-deep-dive)
6. [Cross-Service Communication](#6-cross-service-communication)
7. [Current Pain Points](#7-current-pain-points)
8. [Architecture Diagram Summary](#8-architecture-diagram-summary)

---

## 1️⃣ Solution Overview

### Microservices

| Service | Purpose | Technology Stack | Database | Deployment |
|---------|---------|------------------|----------|------------|
| **Gateway** | API Gateway, request routing, aggregation | .NET 10.0, YARP Reverse Proxy, MediatR, FluentValidation | None | Docker Compose (dev) |
| **IdentityService** | Authentication & Authorization | .NET 10.0, Duende IdentityServer 7.4.4, ASP.NET Core Identity, JWT | SQL Server (`Identity`) | Docker Compose (dev) |
| **ComplianceService** | Compliance programs, tasks, scoring, snapshots | .NET 10.0, EF Core 10.0.1, Hangfire | SQL Server (`Compliance`) | Docker Compose (dev) |
| **AuditService** | Audit sessions, findings, reports | .NET 10.0, EF Core 10.0.1 | SQL Server (`Audit`) | Docker Compose (dev) |
| **OrganizationService** | Organizations, hospitals, members | .NET 10.0, EF Core 10.0.1 | SQL Server (`Organization`) | Docker Compose (dev) |
| **AIOrchestratorService** | AI workflow orchestration, intent detection | .NET 10.0, EF Core 10.0.1 | SQL Server (if any) | Docker Compose (dev) |

### Shared Libraries

- **Shared.Infrastructure**: Common DTOs, middleware, domain base classes, email service, platform ID generator
- **Compliance.MasterData**: Master data seeding for compliance frameworks, chapters, clauses

### Technology Stack Summary

- **.NET Version**: 10.0 (all services)
- **Database**: SQL Server 2022 (separate database per service)
- **ORM**: Entity Framework Core 10.0.1
- **Authentication**: JWT Bearer + Duende IdentityServer
- **API Gateway**: YARP (Yet Another Reverse Proxy) 2.2.0
- **CQRS**: MediatR 12.4.1 (Gateway only)
- **Validation**: FluentValidation 11.9.2
- **Background Jobs**: Hangfire 1.8.17 (ComplianceService only)
- **Messaging**: None (synchronous HTTP only)
- **Caching**: None
- **Deployment**: Docker Compose for development, production deployment not configured

---

## 2️⃣ Service-Level Details

### Gateway

**Responsibility**: API Gateway, request routing, authentication, rate limiting, CQRS orchestration

**Database**: None

**Public API Endpoints**:

- **DashboardController** (`/api/v1/dashboard`)
  - `GET /summary` - Dashboard summary (aggregates data from multiple services)
  - `GET /program-overview-snapshot?hospitalId={id}` - Compliance snapshot for hospital

- **IdentityController** (`/api/v1/identity`)
  - `POST /login` - User login
  - `POST /refresh` - Refresh JWT token
  - `POST /register` - User registration

- **AIController** (`/api/v1/ai`)
  - `POST /explain-task` - AI-powered task explanation

- **TenantController** (`/api/v1/tenant`)
  - `GET /{tenantId}/organization-detail` - Organization hierarchy

**Internal Domain Models**: None (uses DTOs from backend services)

**Events Published**: None

**Events Consumed**: None

**Background Jobs**: None

**External Dependencies**:
- Calls IdentityService (login, register, refresh token)
- Calls ComplianceService (snapshots, dashboard data)
- Calls OrganizationService (organization details)
- Calls AIOrchestratorService (AI workflows)

**Communication Pattern**: Synchronous HTTP via HttpClient with `IProxyService` wrapper

---

### IdentityService

**Responsibility**: User authentication, JWT token generation, OAuth2/OIDC provider

**Database**: SQL Server (`Identity`)

**DbContext Structure** (`IdentityDbContext`):
- `AppUser` (extends `IdentityUser<Guid>`)
- `IdentityRole<Guid>`
- `PlatformSequence` (shared entity)

**Public API Endpoints**:

- **AuthController** (`/api/v1/auth`)
  - Endpoints for authentication (exact routes not fully visible in codebase)

- **TokenController** (`/api/token`)
  - Token generation endpoints

- **InternalUserController** (`/api/internal/users`)
  - Internal user management

**Internal Domain Models**:
- `AppUser` - User entity with `PlatformId`

**Events Published**: None

**Events Consumed**: None

**Background Jobs**: None

**External Dependencies**: None (standalone service)

**Special Features**:
- Duende IdentityServer for OAuth2/OIDC
- ASP.NET Core Identity for user management
- Email service integration (ConsoleEmailService placeholder)
- JWT token generation with custom claims (`organization_id`, `hospital_id`, `scope`)

---

### ComplianceService

**Responsibility**: Compliance programs, tasks, scoring, snapshots, risk management, audit simulation

**Database**: SQL Server (`Compliance`)

**DbContext Structure** (`ComplianceDbContext`):
- `ComplianceFramework` - Master data (NABH, JCI, etc.)
- `ComplianceChapter` - Framework chapters
- `ComplianceClause` - Chapter clauses
- `ComplianceRequirement` - Clause requirements
- `Regulation` - Hospital's enrolled regulations
- `ComplianceProgram` - Hospital compliance programs
- `Control` - Program controls
- `ComplianceTask` - Task assignments
- `Evidence` - Task evidence attachments
- `Risk` - Risk entities (linked to Clause or Task)
- `ComplianceSnapshot` - Calculated compliance scores
- `PlatformSequence` - Shared entity

**Public API Endpoints**:

- **ProgramsController** (`/api/v1/compliance/programs`)
  - `POST /` - Create compliance program

- **ControlsController** (`/api/v1/compliance`)
  - `POST /programs/{programId}/controls` - Load controls for program

- **TasksController** (`/api/v1/compliance`)
  - `POST /programs/{programId}/tasks` - Assign tasks to program

- **EvidenceController** (`/api/v1/compliance`)
  - Evidence management endpoints

- **RegulationsController** (`/api/regulations`)
  - `POST /enroll` - Enroll hospital in regulation

- **SnapshotsController** (`/api/v1/compliance/snapshots`)
  - `GET /hospital/{hospitalId}` - Get compliance snapshot

**Internal Domain Models**:
- `ComplianceProgram`, `Control`, `ComplianceTask`, `Evidence`
- `Regulation`, `ComplianceFramework`, `ComplianceChapter`, `ComplianceClause`, `ComplianceRequirement`
- `Risk`, `ComplianceSnapshot`
- `AuditSimulationResult`, `HighRiskChapter`
- Enums: `ComplianceSeverity`, `TaskCriticalityLevel`, `RiskStatus`

**Events Published**: None

**Events Consumed**: None

**Background Jobs**:
- `ComplianceSnapshotJob` - Recurring job (daily at 2 AM UTC) to recalculate all hospital compliance scores
- Hangfire Dashboard at `/hangfire` (secured with ComplianceScope policy)

**External Dependencies**:
- Uses `Compliance.MasterData` for framework seeding
- No direct calls to other services

**Scoring Logic**:
- `ComplianceScoringService` calculates weighted compliance scores
- Chapter-level scoring with weights
- Task completion status tracking
- Risk penalty calculation:
  - Task penalties: Critical (-5%), High (-2%)
  - Risk penalties: Critical (-5%), Major (-3%), Minor (-1%)
  - Total penalty capped at 40%
  - Avoids double-counting (risks linked to tasks)
- Audit simulation via `AuditSimulationService`:
  - MajorNC, MinorNC, Observation, OFI counts
  - High-risk chapter identification (<50% completion)

---

### AuditService

**Responsibility**: Audit sessions, findings, corrective actions, audit reports

**Database**: SQL Server (`Audit`)

**DbContext Structure** (`AuditDbContext`):
- `AuditSession` - Audit sessions
- `AuditFinding` - Audit findings
- `CorrectiveAction` - Corrective actions
- `AuditReport` - Audit reports
- `PlatformSequence` - Shared entity

**Public API Endpoints**:

- **AuditController** (`/api/v1/audit`)
  - Audit management endpoints (exact routes not fully visible)

**Internal Domain Models**:
- `AuditSession`, `AuditFinding`, `CorrectiveAction`, `AuditReport`
- Enum: `AuditNonConformanceType` (MajorNC, MinorNC, Observation, OFI)

**Events Published**: None

**Events Consumed**: None

**Background Jobs**: None

**External Dependencies**:
- References ComplianceService project (for domain models)
- No HTTP calls to other services

---

### OrganizationService

**Responsibility**: Organization management, hospitals, organization members

**Database**: SQL Server (`Organization`)

**DbContext Structure** (`OrganizationDbContext`):
- `Organization` - Organizations/tenants
- `Hospital` - Hospitals (belongs to organization)
- `OrganizationMember` - Organization members
- `PlatformSequence` - Shared entity

**Public API Endpoints**:

- **OrganizationsController** (`/api/v1/organizations`)
  - Organization CRUD endpoints

- **InternalOrganizationController** (`/api/internal/organizations`)
  - Internal organization management

**Internal Domain Models**:
- `Organization`, `Hospital`, `OrganizationMember` (from `Shared.Infrastructure.Domain`)

**Events Published**: None

**Events Consumed**: None

**Background Jobs**: None

**External Dependencies**: None (standalone service)

---

### AIOrchestratorService

**Responsibility**: AI workflow orchestration, intent detection, LLM integration

**Database**: SQL Server (if any, not fully visible)

**DbContext Structure**: Not fully visible in codebase

**Public API Endpoints**:

- **AiController** (`/api/v1/ai`)
  - AI workflow endpoints (exact routes not fully visible)

**Internal Domain Models**:
- Workflow engine components (`IWorkflowExecutor`, `IWorkflowRegistry`)
- `StartComplianceWorkflow` - Example workflow

**Events Published**: None

**Events Consumed**: None

**Background Jobs**: None

**External Dependencies**:
- Calls ComplianceService via HttpClient (`InternalClient`)
- Calls AuditService via `AuditClient`
- Forwards authentication headers via `ForwardAuthHeaderHandler`

---

## 3️⃣ Messaging Architecture

**Message Broker**: None

**Event Publishing**: None

**Pattern Used**: Synchronous HTTP only (no async messaging)

**Outbox Pattern**: Not implemented

**Retry Strategy**: None (no message broker)

**Dead-Letter Strategy**: None

**Note**: The system uses synchronous HTTP calls between services. No message queue (RabbitMQ, Azure Service Bus, Kafka) is configured. All inter-service communication is request-response via HttpClient.

---

## 4️⃣ Authentication & Authorization

**Identity Provider**: Duende IdentityServer (IdentityService)

**Token Structure**: JWT Bearer tokens

**JWT Claims Example**:
```json
{
  "sub": "9ead6aa7-9bbb-4dd1-8289-65bdf257af04",
  "email": "user@example.com",
  "role": "GroupAdmin",
  "organization_id": "02dd651e-489c-442a-a94b-9d4ca999284c",
  "hospital_id": "e198747b-096d-4c19-a341-16be566c8da4",
  "scope": "compliance.api organization.api",
  "exp": 1711167157,
  "iss": "https://localhost:7135",
  "aud": "https://localhost:7135"
}
```

**Token Validation**: All services validate JWT tokens using `JwtBearer` authentication scheme

**Scope Configuration**:
- `compliance.api` - ComplianceService access
- `organization.api` - OrganizationService access
- `audit.api` - AuditService access (referenced but not fully configured)
- `ai.api` - AIOrchestratorService access (referenced but not fully configured)

**Policy Configuration**:
- `ComplianceScope` - Requires `compliance.api` scope
- `OrganizationScope` - Requires `organization.api` scope
- `AuditScope` - Requires `audit.api` scope
- `AiScope` - Requires `ai.api` scope
- `AdminOnly` - Admin-only access
- `TenantScope` - Tenant-level access

**Token Generation**: IdentityService generates JWT tokens with custom claims (`organization_id`, `hospital_id`, `scope`)

**Token Forwarding**: Gateway forwards `Authorization` header to backend services via YARP reverse proxy

---

## 5️⃣ Compliance Module Deep Dive

### Compliance Entities

**Hierarchy**:
```
ComplianceFramework (e.g., NABH, JCI)
  └── ComplianceChapter (e.g., HRM, Quality)
      └── ComplianceClause (e.g., HRM-01)
          └── ComplianceRequirement (specific requirement)
              └── ComplianceTask (assigned to hospital)
```

**Key Entities**:

1. **ComplianceFramework**: Master data (NABH, JCI, etc.)
   - `Id`, `Name`, `Version`

2. **ComplianceChapter**: Framework chapters
   - `Id`, `FrameworkId`, `Code`, `Name`, `Weight` (decimal 5,2)

3. **ComplianceClause**: Chapter clauses
   - `Id`, `ChapterId`, `ClauseCode`, `Title`, `Description`, `Severity` (enum: Critical, Major, Minor), `IsMandatory`, `Weight` (decimal 5,2)

4. **ComplianceRequirement**: Clause requirements
   - `Id`, `ClauseId`, `Description`

5. **Regulation**: Hospital's enrolled regulation
   - `Id`, `HospitalId`, `ComplianceFrameworkId`, `PlatformId`, `Status`, `StartDate`, `TargetDate`, `ValidUntil`

6. **ComplianceProgram**: Hospital compliance program
   - `Id`, `HospitalId`, `ComplianceFrameworkId`, `PlatformId`, `Name`, `Explainability` (JSON)

7. **ComplianceTask**: Task assignments
   - `Id`, `HospitalId`, `RegulationId`, `RequirementId`, `ControlId`, `AssignedToUserId`, `DueDate`, `Status`, `Criticality` (enum: Critical, High, Medium, Low), `Weight` (decimal 5,2)

8. **Risk**: Risk entities
   - `Id`, `HospitalId`, `ClauseId` OR `TaskId` (mutually exclusive), `Title`, `Description`, `Severity`, `Likelihood` (1-5), `Impact` (1-5), `RiskScore` (computed: Likelihood * Impact), `Status` (enum: Open, Mitigated, Closed)

9. **ComplianceSnapshot**: Calculated compliance scores
   - `Id`, `HospitalId`, `OverallScore` (decimal 5,2), `AuditConfidenceScore` (decimal 5,2), `CriticalCount`, `MajorCount`, `MinorCount`, `ObservationCount`, `OFICount`, `MajorNCCount`, `MinorNCCount`, `ChapterBreakdownJson` (nvarchar(max)), `HighRiskChaptersJson` (nvarchar(max)), `CalculatedAt`

### Current DB Schema

**Tables**:
- `ComplianceFrameworks` - Master frameworks
- `ComplianceChapters` - Framework chapters
- `ComplianceClauses` - Chapter clauses
- `ComplianceRequirements` - Clause requirements
- `Regulations` - Hospital regulations
- `CompliancePrograms` - Hospital programs
- `Controls` - Program controls
- `ComplianceTasks` - Task assignments
- `Evidences` - Task evidence
- `Risks` - Risk entities
- `ComplianceSnapshots` - Calculated snapshots
- `PlatformSequences` - ID generation sequences

**Constraints**:
- `CK_Risk_ClauseOrTaskRequired` - Risk must have either ClauseId OR TaskId (not both, not neither)
- `CK_Risk_LikelihoodRange` - Likelihood must be 1-5
- `CK_Risk_ImpactRange` - Impact must be 1-5
- Unique constraints on `(HospitalId, ComplianceFrameworkId)` for Regulations
- Unique constraint on `HospitalId` for ComplianceSnapshots

### Status Tracking Logic

**Task Status**: String-based (e.g., "Completed", "Done", "Closed", "Verified")
- Completion check: `CompletedStatuses` HashSet contains status (case-insensitive)

**Risk Status**: Enum (`Open`, `Mitigated`, `Closed`)
- Only `Open` risks contribute to penalty calculation

**Regulation Status**: String-based (exact values not fully visible)

### Scoring Logic

**Overall Score Calculation**:
1. Load all tasks for hospital
2. Group by chapter
3. Calculate chapter scores:
   - Task completion percentage
   - Weighted by task weights
   - Apply risk penalties
4. Aggregate chapter scores:
   - Weighted by chapter weights
   - Apply global risk penalty
5. Cap penalty at 40%
6. Ensure score never goes below 0

**Risk Penalty Calculation**:
- Task penalties: Critical (-5%), High (-2%)
- Risk penalties: Critical (-5%), Major (-3%), Minor (-1%)
- Avoid double-counting: If risk is linked to task, only count risk penalty
- Total penalty capped at 40%

**Audit Simulation**:
- `AuditSimulationService` simulates audit findings:
  - Mandatory clause with no tasks → MajorNC
  - Mandatory clause with incomplete tasks → MajorNC
  - Open Risk with Critical severity → MajorNC
  - Open Risk with Major severity → MinorNC
  - Chapter completion < 50% → Flag as High Risk
- Results stored in `ComplianceSnapshot`: `MajorNCCount`, `MinorNCCount`, `ObservationCount`, `OFICount`, `HighRiskChaptersJson`

### Snapshot Tables

**ComplianceSnapshots Table**:
- One snapshot per hospital (unique constraint on `HospitalId`)
- Stores calculated scores, counts, and JSON breakdowns
- `CalculatedAt` timestamp for historical tracking
- Background job recalculates daily at 2 AM UTC

### Hospital-Framework Mapping

**Mapping Flow**:
1. Hospital enrolls in `Regulation` (links `HospitalId` to `ComplianceFrameworkId`)
2. `ComplianceProgram` created (links `HospitalId` to `ComplianceFrameworkId`)
3. `ComplianceTask` assigned (links `HospitalId` to `RequirementId` → `ClauseId` → `ChapterId` → `FrameworkId`)
4. Scoring aggregates by chapter within framework

**Master Data**: `ComplianceFramework`, `ComplianceChapter`, `ComplianceClause`, `ComplianceRequirement` are seeded via `Compliance.MasterData` project

---

## 6️⃣ Cross-Service Communication

### Communication Map

| From Service | To Service | Method | Purpose |
|--------------|------------|--------|---------|
| Gateway | IdentityService | HTTP (YARP) | Login, register, refresh token |
| Gateway | ComplianceService | HTTP (YARP) | Get snapshots, dashboard data |
| Gateway | OrganizationService | HTTP (HttpClient) | Get organization details |
| Gateway | AIOrchestratorService | HTTP (YARP) | AI workflows |
| AIOrchestratorService | ComplianceService | HTTP (HttpClient) | Compliance data for workflows |
| AIOrchestratorService | AuditService | HTTP (HttpClient) | Audit data for workflows |

### Sync vs Async Communication

**All Communication is Synchronous**:
- No message broker
- No event-driven architecture
- All inter-service calls are HTTP request-response
- Gateway uses YARP reverse proxy for routing
- Gateway uses HttpClient for direct calls (OrganizationService)
- AIOrchestratorService uses HttpClient for backend calls

### API Gateway

**Present**: Yes (Gateway service using YARP)

**Routing Configuration**:
- `/api/identity/{**catch-all}` → IdentityService (http://localhost:7135)
- `/api/compliance/{**catch-all}` → ComplianceService (http://localhost:7087)
- `/api/ai/{**catch-all}` → AIOrchestratorService (http://localhost:7097)

**Gateway Features**:
- JWT authentication validation
- Rate limiting
- CORS configuration
- Request forwarding with authentication headers
- MediatR for CQRS pattern
- FluentValidation for request validation

### Service Discovery

**Mechanism**: Configuration-based (appsettings.json)

**No Dynamic Service Discovery**: Services are configured with static URLs in `appsettings.Development.json`:
```json
{
  "Services": {
    "ComplianceService": { "BaseUrl": "https://localhost:7087" },
    "IdentityService": { "BaseUrl": "https://localhost:7135" },
    "AIService": { "BaseUrl": "http://localhost:5003" }
  }
}
```

---

## 7️⃣ Current Pain Points

### Known Technical Debt

1. **Swagger UI Disabled**: Swashbuckle.AspNetCore not compatible with .NET 10
   - OpenAPI JSON still available at `/openapi/v1.json`
   - No Swagger UI for API documentation

2. **Email Service Placeholder**: `ConsoleEmailService` is a console placeholder
   - Needs real SMTP implementation for production

3. **No Message Broker**: All communication is synchronous HTTP
   - No async event-driven architecture
   - No retry/backoff for failed calls
   - No dead-letter queue

4. **No Caching**: No Redis or in-memory caching
   - All data fetched from database on every request

5. **Static Service Discovery**: Services configured with static URLs
   - No dynamic service discovery (Consul, Eureka, etc.)
   - Hard to scale horizontally

### Incomplete Modules

1. **AuditService**: UnitOfWork integration incomplete (mentioned in PROJECT_STATUS_REPORT.md)

2. **AIOrchestratorService**: 
   - Intent detection is rule-based (needs LLM integration)
   - Workflow state persistence not implemented
   - Limited workflows (only `StartComplianceWorkflow` example)

3. **ComplianceService**:
   - Control loading from regulation templates not fully implemented
   - Task assignment logic has placeholder code
   - Evidence upload functionality not implemented

4. **IdentityService**:
   - `audit.api` and `ai.api` scopes referenced but not fully configured

### Known Scalability Concerns

1. **Database Connections**: Each service has its own database
   - No connection pooling configuration visible
   - Potential connection exhaustion under load

2. **Synchronous Calls**: Gateway makes sequential calls to backend services
   - Dashboard summary may be slow (multiple HTTP calls)
   - No parallel request handling

3. **No Load Balancing**: Services configured with single destination
   - Cannot scale horizontally without manual configuration

4. **Hangfire Single Instance**: ComplianceService uses Hangfire for background jobs
   - Single instance may become bottleneck
   - No distributed job processing

### Known Bugs

1. **Route Ambiguity (Fixed)**: `SnapshotsController.Example.cs` caused `AmbiguousMatchException`
   - Fixed by deleting example file

2. **SSL Certificate Validation**: Development environment bypasses SSL validation
   - May cause issues in production if not configured properly

3. **Protocol Mismatch**: Gateway configuration had `http` vs `https` mismatch
   - Fixed in `appsettings.Development.json`

---

## 8️⃣ Architecture Diagram Summary (Text-Based)

```
┌─────────────────────────────────────────────────────────────────┐
│                         Angular Frontend                        │
│                      (http://localhost:4200)                   │
└────────────────────────────┬────────────────────────────────────┘
                             │ HTTPS
                             ▼
┌─────────────────────────────────────────────────────────────────┐
│                         Gateway Service                         │
│                    (https://localhost:7216)                    │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  YARP Reverse Proxy                                      │  │
│  │  - /api/identity/* → IdentityService                     │  │
│  │  - /api/compliance/* → ComplianceService                │  │
│  │  - /api/ai/* → AIOrchestratorService                    │  │
│  └──────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  MediatR (CQRS)                                           │  │
│  │  - Query Handlers                                         │  │
│  │  - Command Handlers                                       │  │
│  └──────────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  Authentication & Authorization                           │  │
│  │  - JWT Validation                                         │  │
│  │  - Rate Limiting                                          │  │
│  │  - CORS                                                   │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────┬──────────────────┬────────────────────────────┘
                  │                  │
        ┌─────────┴─────────┐       │
        │                   │       │
        ▼                   ▼       ▼
┌──────────────┐   ┌──────────────┐   ┌──────────────────────┐
│IdentityService│   │ComplianceService│ │AIOrchestratorService│
│(Port 7135)    │   │(Port 7087)   │   │(Port 7097)           │
│               │   │              │   │                      │
│ - JWT Tokens  │   │ - Programs   │   │ - Workflows          │
│ - OAuth2/OIDC│   │ - Tasks      │   │ - Intent Detection  │
│ - Users       │   │ - Scoring   │   │ - LLM Integration    │
│               │   │ - Snapshots │   │                      │
│               │   │ - Risks     │   │                      │
│               │   │ - Hangfire  │   │                      │
└───────┬───────┘   └──────┬───────┘   └──────┬───────────────┘
        │                  │                  │
        │                  │                  │
        ▼                  ▼                  ▼
┌──────────────┐   ┌──────────────┐   ┌──────────────┐
│  SQL Server  │   │  SQL Server  │   │  SQL Server │
│  (Identity)  │   │  (Compliance)│   │  (if any)   │
└──────────────┘   └──────────────┘   └──────────────┘

        ┌──────────────────────────────────────┐
        │                                      │
        ▼                                      ▼
┌──────────────┐                      ┌──────────────┐
│AuditService  │                      │Organization  │
│(Port 5003)   │                      │Service      │
│              │                      │(Port 5004)   │
│ - Sessions   │                      │              │
│ - Findings   │                      │ - Orgs       │
│ - Reports    │                      │ - Hospitals  │
│              │                      │ - Members   │
└──────┬───────┘                      └──────┬───────┘
       │                                     │
       ▼                                     ▼
┌──────────────┐                      ┌──────────────┐
│  SQL Server  │                      │  SQL Server │
│  (Audit)     │                      │  (Organization)│
└──────────────┘                      └──────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    Shared Infrastructure                     │
│  - ApiResponse<T>, ApiError                                 │
│  - GlobalExceptionMiddleware                                 │
│  - ValidationExceptionMiddleware                             │
│  - ControllerExtensions                                      │
│  - PlatformIdGenerator                                       │
│  - EmailService (ConsoleEmailService placeholder)            │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    Compliance.MasterData                     │
│  - Framework Seeding                                         │
│  - Chapter/Clause/Requirement Data                          │
└─────────────────────────────────────────────────────────────┘
```

### Communication Flow Example: Dashboard Summary

```
1. Angular → Gateway: GET /api/v1/dashboard/summary
2. Gateway validates JWT token
3. Gateway extracts hospital_id from token
4. Gateway → ComplianceService: GET /api/v1/compliance/snapshots/hospital/{id}
5. Gateway → OrganizationService: GET /api/v1/organizations/{id} (via HttpClient)
6. Gateway aggregates responses
7. Gateway → Angular: Aggregated dashboard summary
```

### Communication Flow Example: AI Explain Task

```
1. Angular → Gateway: POST /api/v1/ai/explain-task
2. Gateway validates JWT token
3. Gateway → AIOrchestratorService: POST /api/v1/ai/explain-task (via YARP)
4. AIOrchestratorService → ComplianceService: GET /api/v1/compliance/tasks/{id} (via HttpClient)
5. AIOrchestratorService processes with LLM
6. AIOrchestratorService → Gateway: Response
7. Gateway → Angular: AI explanation
```

---

## Summary

The Compliance Platform is a microservices-based system built with .NET 10.0. It uses synchronous HTTP communication, JWT authentication, and separate databases per service. The architecture is functional but lacks async messaging, caching, and dynamic service discovery. The ComplianceService is the most feature-complete, with scoring, snapshots, and background jobs. The Gateway provides routing and aggregation, while other services handle identity, organizations, audits, and AI orchestration.

---

**Document Version**: 1.0  
**Last Updated**: February 2025  
**Maintained By**: Architecture Team
