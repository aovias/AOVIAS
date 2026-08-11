# Entity Models and DTOs Documentation

## Overview
This document provides a comprehensive overview of all entity models, DTOs, and foreign key associations in **OrganizationService** and **ComplianceService**.

---

## Base Entity Classes

### BaseEntity
**Location:** `Shared.Infrastructure.Domain.BaseEntity`
- `Id` (Guid) - Primary Key
- `CreatedAt` (DateTime)
- `RowVersion` (byte[]) - Concurrency token (Timestamp)

### PlatformEntity
**Location:** `Shared.Infrastructure.Domain.PlatformEntity`  
**Inherits:** `BaseEntity`
- All properties from `BaseEntity`
- `PlatformId` (string, max 10) - Unique business identifier

---

## OrganizationService Entities

### 1. Organization
**Location:** `Shared.Infrastructure.Domain.Organization`  
**Inherits:** `PlatformEntity`

**Properties:**
- `Id` (Guid) - PK
- `PlatformId` (string) - Unique, indexed
- `Name` (string) - Required
- `Type` (string) - Required
- `Address` (string) - Required
- `CreatedAt` (DateTime)
- `RowVersion` (byte[]) - Concurrency token

**Foreign Keys:** None

**Indexes:**
- Unique index on `PlatformId`

---

### 2. OrganizationMember
**Location:** `Shared.Infrastructure.Domain.OrganizationMember`  
**Inherits:** `BaseEntity`

**Properties:**
- `Id` (Guid) - PK
- `OrganizationId` (Guid) - FK → Organization.Id
- `UserId` (Guid) - References user in IdentityService
- `Role` (string) - Required
- `CreatedAt` (DateTime)
- `RowVersion` (byte[]) - Concurrency token

**Foreign Keys:**
- `OrganizationId` → `Organization.Id` (Many-to-One)

**Indexes:**
- Index on `OrganizationId`

---

## ComplianceService Entities

### 1. ComplianceFramework
**Location:** `ComplianceService.Domain.ComplianceFramework`  
**Inherits:** `PlatformEntity`

**Properties:**
- `Id` (Guid) - PK
- `PlatformId` (string) - Unique business identifier
- `Name` (string) - Required (e.g., "NABH")
- `Authority` (string) - Required (e.g., "QCI")
- `Version` (string) - Required (e.g., "Entry Level", "2024")
- `IsActive` (bool)
- `CreatedAt` (DateTime)
- `RowVersion` (byte[])

**Foreign Keys:** None

**Indexes:**
- Unique composite index on (`Name`, `Version`)

---

### 2. ComplianceChapter
**Location:** `ComplianceService.Domain.ComplianceChapter`  
**Inherits:** `PlatformEntity`

**Properties:**
- `Id` (Guid) - PK
- `PlatformId` (string)
- `FrameworkId` (Guid) - FK → ComplianceFramework.Id
- `Framework` (ComplianceFramework) - Navigation property
- `Code` (string) - Required (e.g., "PRE", "ICC", "HRM")
- `Name` (string) - Required (e.g., "Patient Rights", "Infection Control")
- `Sequence` (int)
- `Clauses` (ICollection<ComplianceClause>) - Navigation property
- `CreatedAt` (DateTime)
- `RowVersion` (byte[])

**Foreign Keys:**
- `FrameworkId` → `ComplianceFramework.Id` (Many-to-One, Cascade Delete)

**Indexes:**
- Index on `FrameworkId`

---

### 3. ComplianceClause
**Location:** `ComplianceService.Domain.ComplianceClause`  
**Inherits:** `PlatformEntity`

**Properties:**
- `Id` (Guid) - PK
- `PlatformId` (string)
- `ChapterId` (Guid) - FK → ComplianceChapter.Id
- `Chapter` (ComplianceChapter) - Navigation property
- `ClauseCode` (string) - Required (e.g., "PRE.1", "ICC.3")
- `Description` (string)
- `Severity` (string) - e.g., "Critical", "Major", "Minor"
- `IsMandatory` (bool)
- `Requirements` (ICollection<ComplianceRequirement>) - Navigation property
- `CreatedAt` (DateTime)
- `RowVersion` (byte[])

**Foreign Keys:**
- `ChapterId` → `ComplianceChapter.Id` (Many-to-One, Restrict Delete)

**Indexes:**
- Unique composite index on (`ChapterId`, `ClauseCode`)

---

### 4. ComplianceRequirement
**Location:** `ComplianceService.Domain.ComplianceRequirement`  
**Inherits:** `PlatformEntity`

**Properties:**
- `Id` (Guid) - PK
- `PlatformId` (string)
- `ClauseId` (Guid) - FK → ComplianceClause.Id
- `Clause` (ComplianceClause) - Navigation property
- `RequirementText` (string) - Required
- `Department` (string) - e.g., "ICU", "OPD", "Pharmacy"
- `ResponsibleRole` (string) - e.g., "Nurse", "MO", "Admin"
- `Frequency` (string) - e.g., "Daily", "Monthly", "Event"
- `EvidenceType` (string) - e.g., "Photo", "Log", "Document"
- `IsActive` (bool)
- `CreatedAt` (DateTime)
- `RowVersion` (byte[])

**Foreign Keys:**
- `ClauseId` → `ComplianceClause.Id` (Many-to-One, Restrict Delete)

**Indexes:**
- Index on `ClauseId`

---

### 5. ComplianceProgram
**Location:** `ComplianceService.Domain.ComplianceProgram`  
**Inherits:** `PlatformEntity`

**Properties:**
- `Id` (Guid) - PK
- `PlatformId` (string) - Unique, indexed
- `OrganizationId` (Guid) - References Organization.Id (cross-service)
- `ComplianceFrameworkId` (Guid) - FK → ComplianceFramework.Id
- `ComplianceFramework` (ComplianceFramework) - Navigation property
- `Name` (string) - Required
- `Explainability` (ExplainabilityDto?) - JSON column
- `CreatedAt` (DateTime)
- `RowVersion` (byte[])

**Foreign Keys:**
- `ComplianceFrameworkId` → `ComplianceFramework.Id` (Many-to-One, Restrict Delete)
- `OrganizationId` → `Organization.Id` (Cross-service reference, no FK constraint)

**Indexes:**
- Unique index on `PlatformId`
- Index on `ComplianceFrameworkId`
- Index on `OrganizationId`

---

### 6. Regulation
**Location:** `ComplianceService.Domain.Regulation`  
**Inherits:** `PlatformEntity`

**Properties:**
- `Id` (Guid) - PK
- `PlatformId` (string) - Required
- `OrganizationId` (Guid) - References Organization.Id (cross-service)
- `ComplianceFrameworkId` (Guid) - FK → ComplianceFramework.Id
- `Status` (string) - Required
- `StartDate` (DateTime)
- `TargetDate` (DateTime?)
- `ValidUntil` (DateTime?)
- `CreatedAt` (DateTime)
- `RowVersion` (byte[])

**Foreign Keys:**
- `ComplianceFrameworkId` → `ComplianceFramework.Id` (Many-to-One, Restrict Delete)
- `OrganizationId` → `Organization.Id` (Cross-service reference, no FK constraint)

**Indexes:**
- Unique composite index on (`OrganizationId`, `ComplianceFrameworkId`)
- Index on `ComplianceFrameworkId`
- Index on `OrganizationId`

---

### 7. Control
**Location:** `ComplianceService.Domain.Control`  
**Inherits:** `BaseEntity`

**Properties:**
- `Id` (Guid) - PK
- `ProgramId` (Guid) - FK → ComplianceProgram.Id
- `ComplianceRequirementId` (Guid?) - FK → ComplianceRequirement.Id (nullable)
- `Code` (string) - Required (e.g., "NABH-01")
- `Description` (string) - Required
- `Phase` (string) - Default: "Readiness", max 50 chars
- `Explainability` (ExplainabilityDto?) - JSON column
- `CreatedAt` (DateTime)
- `RowVersion` (byte[])

**Foreign Keys:**
- `ProgramId` → `ComplianceProgram.Id` (Many-to-One, Restrict Delete)
- `ComplianceRequirementId` → `ComplianceRequirement.Id` (Many-to-One, nullable, Restrict Delete)

**Indexes:**
- Unique composite index on (`ProgramId`, `Code`)
- Index on `ProgramId`
- Index on `ComplianceRequirementId`

---

### 8. ComplianceTask
**Location:** `ComplianceService.Domain.ComplianceTask`  
**Inherits:** `BaseEntity`

**Properties:**
- `Id` (Guid) - PK
- `OrganizationId` (Guid) - References Organization.Id (cross-service)
- `RegulationId` (Guid) - FK → Regulation.Id
- `RequirementId` (Guid) - FK → ComplianceRequirement.Id
- `ControlId` (Guid?) - FK → Control.Id (nullable)
- `AssignedToUserId` (string) - Required, references user in IdentityService
- `DueDate` (DateTime)
- `Status` (string) - Required
- `Title` (string?)
- `MaturityState` (string?)
- `Explainability` (ExplainabilityDto?) - JSON column
- `CreatedAt` (DateTime)
- `RowVersion` (byte[])

**Foreign Keys:**
- `RegulationId` → `Regulation.Id` (Many-to-One, Restrict Delete)
- `RequirementId` → `ComplianceRequirement.Id` (Many-to-One, Restrict Delete)
- `ControlId` → `Control.Id` (Many-to-One, nullable, Restrict Delete)
- `OrganizationId` → `Organization.Id` (Cross-service reference, no FK constraint)

**Indexes:**
- Index on `OrganizationId`
- Index on `RegulationId`
- Index on `RequirementId`
- Index on `ControlId`

---

### 9. Evidence
**Location:** `ComplianceService.Domain.Evidence`  
**Inherits:** `BaseEntity`

**Properties:**
- `Id` (Guid) - PK
- `TaskId` (Guid) - FK → ComplianceTask.Id
- `Task` (ComplianceTask) - Navigation property
- `EvidenceType` (string) - Required
- `FileUrl` (string) - Required
- `UploadedOn` (DateTime)
- `Metadata` (string) - JSON format
- `CreatedAt` (DateTime)
- `RowVersion` (byte[])

**Foreign Keys:**
- `TaskId` → `ComplianceTask.Id` (Many-to-One, Cascade Delete)

**Indexes:**
- Index on `TaskId`

---

## DTOs (Data Transfer Objects)

### OrganizationService DTOs

#### CreateOrganizationDto
**Location:** `Shared.Infrastructure.DTO.CreateOrganizationDto`

**Properties:**
- `Name` (string) - Required
- `Type` (string) - Required
- `Address` (string) - Required

---

#### UpdateOrganizationDto
**Location:** `Shared.Infrastructure.DTO.UpdateOrganizationDto`

**Properties:**
- `Name` (string?)
- `Type` (string?)
- `Address` (string?)

---

#### AddMemberDto
**Location:** `Shared.Infrastructure.DTO.AddMemberDto`

**Properties:**
- `UserId` (Guid) - Required
- `Role` (string) - Required

---

### ComplianceService DTOs

#### CreateRegulationDto
**Location:** `ComplianceService.Application.DTOs.CreateRegulationDto`

**Properties:**
- `OrganizationId` (Guid) - Required
- `Name` (string) - Required
- `Authority` (string) - Required (e.g., "NABH", "ISO")
- `Version` (string) - Required

---

#### EnrollRegulationRequest
**Location:** `ComplianceService.Application.DTOs.EnrollRegulationRequest`

**Properties:**
- `OrganizationId` (Guid) - Required
- `ComplianceFrameworkId` (Guid) - Required
- `TargetDate` (DateTime?) - Optional

---

#### ExplanationRewriteRequest
**Location:** `ComplianceService.Application.DTOs.ExplanationRewriteRequest`

**Properties:**
- `OriginalExplanationText` (string) - Required, the original explanation to rewrite
- `AudienceType` (ExplanationAudienceType) - Target audience (Admin, ComplianceOfficer, Auditor)
- `Tone` (ExplanationTone) - Desired tone (Neutral, Supportive, Formal)

---

## Foreign Key Relationships Summary

### OrganizationService
```
Organization (1) ──< (Many) OrganizationMember
```

### ComplianceService
```
ComplianceFramework (1) ──< (Many) ComplianceChapter
ComplianceFramework (1) ──< (Many) ComplianceProgram
ComplianceFramework (1) ──< (Many) Regulation

ComplianceChapter (1) ──< (Many) ComplianceClause

ComplianceClause (1) ──< (Many) ComplianceRequirement

ComplianceProgram (1) ──< (Many) Control

ComplianceRequirement (1) ──< (Many) Control (optional)

Regulation (1) ──< (Many) ComplianceTask

ComplianceRequirement (1) ──< (Many) ComplianceTask

Control (1) ──< (Many) ComplianceTask (optional)

ComplianceTask (1) ──< (Many) Evidence
```

### Cross-Service References (No FK Constraints)
- `ComplianceProgram.OrganizationId` → `Organization.Id`
- `Regulation.OrganizationId` → `Organization.Id`
- `ComplianceTask.OrganizationId` → `Organization.Id`
- `OrganizationMember.UserId` → `IdentityService.AppUser.Id`
- `ComplianceTask.AssignedToUserId` → `IdentityService.AppUser.Id`

---

## Entity Hierarchy

### PlatformEntity Hierarchy
- `Organization`
- `ComplianceFramework`
- `ComplianceChapter`
- `ComplianceClause`
- `ComplianceRequirement`
- `ComplianceProgram`
- `Regulation`

### BaseEntity Hierarchy
- `OrganizationMember`
- `Control`
- `ComplianceTask`
- `Evidence`

---

## Delete Behaviors

- **Cascade Delete:**
  - `ComplianceChapter` → `ComplianceFramework`
  - `Evidence` → `ComplianceTask`

- **Restrict Delete:**
  - `OrganizationMember` → `Organization`
  - `ComplianceClause` → `ComplianceChapter`
  - `ComplianceRequirement` → `ComplianceClause`
  - `ComplianceProgram` → `ComplianceFramework`
  - `Regulation` → `ComplianceFramework`
  - `Control` → `ComplianceProgram`
  - `Control` → `ComplianceRequirement`
  - `ComplianceTask` → `Regulation`
  - `ComplianceTask` → `ComplianceRequirement`
  - `ComplianceTask` → `Control`

---

## Notes

1. **Cross-Service References:** `OrganizationId` fields in ComplianceService entities reference Organization entities but don't have database foreign key constraints due to microservices architecture.

2. **User References:** `UserId` and `AssignedToUserId` reference IdentityService but don't have FK constraints.

3. **Unique Constraints:**
   - `Organization.PlatformId` - Unique
   - `ComplianceFramework` (Name, Version) - Unique composite
   - `ComplianceClause` (ChapterId, ClauseCode) - Unique composite
   - `Control` (ProgramId, Code) - Unique composite
   - `Regulation` (OrganizationId, ComplianceFrameworkId) - Unique composite
   - `ComplianceProgram.PlatformId` - Unique

4. **Concurrency Control:** All entities use `RowVersion` (byte[]) for optimistic concurrency control.

5. **Explainability:** `ComplianceProgram`, `Control`, and `ComplianceTask` support explainability metadata stored as JSON.
