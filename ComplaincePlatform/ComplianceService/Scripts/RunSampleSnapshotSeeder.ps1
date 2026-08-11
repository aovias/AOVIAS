# ============================================================================
# PowerShell script to run the sample snapshot seeder
# ============================================================================
# This script can be run from the ComplianceService project directory
# ============================================================================

param(
    [Parameter(Mandatory=$false)]
    [string]$HospitalId = "e198747b-096d-4c19-a341-16be566c8da4"
)

Write-Host "Seeding sample snapshot data for hospital: $HospitalId" -ForegroundColor Cyan

# Option 1: Run SQL script directly
Write-Host "`nOption 1: Run SQL Script" -ForegroundColor Yellow
Write-Host "Execute the SQL script: ComplianceService\Scripts\InsertSampleSnapshotData.sql" -ForegroundColor Gray
Write-Host "Make sure to update the database name in the script if needed." -ForegroundColor Gray

# Option 2: Use dotnet ef database update (if you create a migration)
Write-Host "`nOption 2: Create a Migration" -ForegroundColor Yellow
Write-Host "You can create a migration that seeds this data, then run:" -ForegroundColor Gray
Write-Host "  dotnet ef migrations add SeedSampleSnapshotData --project ComplianceService" -ForegroundColor Gray
Write-Host "  dotnet ef database update --project ComplianceService" -ForegroundColor Gray

# Option 3: Run via C# code (recommended for development)
Write-Host "`nOption 3: Add to Program.cs (Development Only)" -ForegroundColor Yellow
Write-Host "Add this code to Program.cs in the Development section:" -ForegroundColor Gray
Write-Host @"
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<SeedSampleSnapshotData>();
        var hospitalId = Guid.Parse("$HospitalId");
        await seeder.SeedAsync(hospitalId);
    }
}
"@ -ForegroundColor DarkGray

Write-Host "`nPress any key to continue..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
