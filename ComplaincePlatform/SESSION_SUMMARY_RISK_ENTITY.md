# Session Summary: Risk Entity Creation and Configuration

## Overview
This session focused on creating a comprehensive Risk domain model for the ComplianceService, along with audit simulation enhancements.

## 1. Risk Entity Creation

### Entity: `Risk` (`ComplianceService/Domain/Risk.cs`)
- **Base Class**: `BaseEntity` (includes Id, RowVersion, CreatedAt)
- **Key Fields**:
  - `HospitalId` (Guid, required)
  - `ClauseId` (Guid?, nullable) - FK to ComplianceClause
  - `TaskId` (Guid?, nullable) - FK to ComplianceTask
  - `Title` (string, required, max 250)
  - `Description` (string, optional)
  - `Severity` (ComplianceSeverity enum)
  - `Likelihood` (int, 1-5 scale)
  - `Impact` (int, 1-5 scale)
  - `RiskScore` (int, computed = Likelihood × Impact, **not stored**)
  - `Status` (RiskStatus enum, default: Open)
  - `IdentifiedAt` (DateTime, required)
  - `ClosedAt` (DateTime?, nullable)

### Enum: `RiskStatus` (`ComplianceService/Domain/Enums/RiskStatus.cs`)
- `Open = 1` - Risk is active and requires monitoring
- `Mitigated = 2` - Risk addressed with mitigation measures
- `Closed = 3` - Risk fully resolved

## 2. Entity Framework Configuration

### Database Configuration (`ComplianceDbContext.cs`)
- **Table**: `Risks`
- **Indexes**:
  - `HospitalId` - for query performance
  - `Status` - for filtering by status
  - `ClauseId` - for clause relationships
  - `TaskId` - for task relationships
- **Foreign Keys**:
  - `ComplianceClause` (Restrict delete)
  - `ComplianceTask` (Restrict delete)
- **Check Constraints**:
  - `CK_Risk_ClauseOrTaskRequired` - Either ClauseId or TaskId must be set (not both null)
  - `CK_Risk_LikelihoodRange` - Likelihood must be 1-5
  - `CK_Risk_ImpactRange` - Impact must be 1-5
- **Computed Property**: `RiskScore` is ignored in database (calculated in entity getter)

### Migration
- **Migration**: `20260215111832_AddRiskEntity.cs`
- **Status**: ✅ Applied successfully
- **Changes**: Created Risks table with all constraints and indexes

## 3. Audit Simulation Enhancements

### AuditSimulationResult (`ComplianceService/Domain/AuditSimulationResult.cs`)
- **Properties**:
  - `MajorNCCount`, `MinorNCCount`, `ObservationCount`, `OFICount`
  - `HighRiskChapters` (List<HighRiskChapter>)
  - `TotalNonConformances` (computed)

### AuditSimulationService (`ComplianceService/Application/Services/AuditSimulationService.cs`)
- **Rules Implemented**:
  1. Mandatory clause with no tasks → MajorNC
  2. Mandatory clause with incomplete tasks → MajorNC
  3. Open Risk with Critical severity → MajorNC
  4. Open Risk with Major severity → MinorNC
  5. Chapter completion < 50% → Flag as High Risk

### NonConformanceMapper (`ComplianceService/Application/Utilities/NonConformanceMapper.cs`)
- Maps risks to non-conformance types
- Uses `Shared.Infrastructure.Domain.Enums.NonConformanceType`
- Rules:
  - Critical + Open → MajorNC
  - Major + Open → MinorNC
  - Other combinations → null

## 4. Key Design Decisions

1. **Computed RiskScore**: Calculated as `Likelihood × Impact` in entity getter, not stored in database
2. **Mutual Exclusivity**: Either ClauseId or TaskId must be set (enforced by check constraint)
3. **Isolated Logic**: Audit simulation service is separate from scoring engine
4. **No Circular Dependencies**: NonConformanceMapper placed in ComplianceService to avoid dependency issues

## 5. Files Created/Modified

### Created:
- `ComplianceService/Domain/Risk.cs`
- `ComplianceService/Domain/Enums/RiskStatus.cs`
- `ComplianceService/Domain/AuditSimulationResult.cs`
- `ComplianceService/Application/Services/AuditSimulationService.cs`
- `ComplianceService/Application/Utilities/NonConformanceMapper.cs`
- `ComplianceService/Migrations/20260215111832_AddRiskEntity.cs`

### Modified:
- `ComplianceService/Infrastructure/Persistence/ComplianceDbContext.cs` - Added Risk DbSet and configuration
- `AuditService/AuditService.csproj` - Added ComplianceService reference (for NonConformanceMapper initially, later moved)

## 6. Status

✅ **Complete**: Risk entity created, configured, and migrated  
✅ **Complete**: Audit simulation service with all rules implemented  
✅ **Complete**: Build successful, no errors  
⚠️ **Note**: Logic is isolated from scoring engine as requested
