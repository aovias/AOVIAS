# Sample Snapshot Data Seeder

This directory contains scripts to insert sample data for the `program-overview-snapshot` endpoint.

## Hospital Information
- **Hospital ID**: `e198747b-096d-4c19-a341-16be566c8da4`
- **Hospital Name**: Kanchan Bai Hospital
- **Organization**: Sitaram and sons pvt ltd

## Quick Start

### Option 1: SQL Script (Recommended - Fastest)

1. Open SQL Server Management Studio (SSMS) or your preferred SQL client
2. Connect to your database (default: `Compliance`)
3. Open `InsertSampleSnapshotData.sql`
4. Update the database name if needed (line 6: `USE [Compliance]`)
5. Execute the script

The script will:
- Check if a snapshot already exists for the hospital
- Update existing snapshot or insert a new one
- Display verification query results

### Option 2: C# Seeder (For Development)

1. Register the seeder in `Program.cs`:

```csharp
// Add to service registration
builder.Services.AddScoped<SeedSampleSnapshotData>();

// Add after master data seeding (in Development section)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<SeedSampleSnapshotData>();
        var hospitalId = Guid.Parse("e198747b-096d-4c19-a341-16be566c8da4");
        await seeder.SeedAsync(hospitalId);
    }
}
```

2. Run the ComplianceService application

## Sample Data Details

The sample snapshot includes:

- **OverallScore**: 78.5%
- **AuditConfidenceScore**: 82.0%
- **CriticalCount**: 2
- **MajorCount**: 5
- **MinorCount**: 12
- **ObservationCount**: 3
- **OFICount**: 1
- **MajorNCCount**: 2
- **MinorNCCount**: 3
- **ChapterBreakdownJson**: 4 sample chapters with scores
- **HighRiskChaptersJson**: 1 high-risk chapter (HRM at 45% completion)

## Verification

After seeding, test the endpoint:

```bash
GET https://localhost:7087/api/v1/compliance/snapshots/hospital/e198747b-096d-4c19-a341-16be566c8da4
```

Or via Gateway:

```bash
GET https://localhost:7216/api/v1/dashboard/program-overview-snapshot?hospitalId=e198747b-096d-4c19-a341-16be566c8da4
```

## Files

- `InsertSampleSnapshotData.sql` - SQL script for direct database insertion
- `SeedSampleSnapshotData.cs` - C# seeder class (optional, for programmatic seeding)
- `RunSampleSnapshotSeeder.ps1` - PowerShell helper script with instructions
