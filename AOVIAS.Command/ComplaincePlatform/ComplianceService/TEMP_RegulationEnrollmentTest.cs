// ============================================================================
// TEMPORARY TEST CODE - DELETE AFTER VERIFICATION
// ============================================================================
// This file contains temporary integration tests to verify:
// 1. Regulation is created successfully
// 2. Duplicate enrollment throws InvalidOperationException
// 
// To run: Uncomment the test code block in Program.cs
// To remove: Delete this file and remove the test code from Program.cs
// ============================================================================

using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService;

public static class TempRegulationEnrollmentTest
{
    public static async Task RunTestAsync(IServiceProvider serviceProvider)
    {
        Console.WriteLine("\n==========================================");
        Console.WriteLine("TEMPORARY TEST: Regulation Enrollment");
        Console.WriteLine("==========================================\n");

        var service = serviceProvider.GetRequiredService<IRegulationService>();
        var dbContext = serviceProvider.GetRequiredService<ComplianceDbContext>();

        // Test data
        var orgId = Guid.NewGuid();
        var frameworkId = Guid.NewGuid();
        var targetDate = DateTime.UtcNow.AddDays(90);

        var request = new EnrollRegulationRequest
        {
            HospitalId = orgId, // Using orgId as hospitalId for test
            ComplianceFrameworkId = frameworkId,
            TargetDate = targetDate
        };

        try
        {
            // Test 1: Successful enrollment
            Console.WriteLine("Test 1: Creating regulation...");
            var regulationId = await service.EnrollAsync(request, CancellationToken.None);
            Console.WriteLine($"✓ PASS: Regulation created successfully");
            Console.WriteLine($"  RegulationId: {regulationId}");
            Console.WriteLine($"  HospitalId: {orgId}");
            Console.WriteLine($"  ComplianceFrameworkId: {frameworkId}");

            // Verify in database
            var created = await dbContext.Regulations
                .FirstOrDefaultAsync(r => r.Id == regulationId);
            
            if (created != null)
            {
                Console.WriteLine($"✓ PASS: Regulation found in database");
                Console.WriteLine($"  PlatformId: {created.PlatformId}");
                Console.WriteLine($"  Status: {created.Status}");
                Console.WriteLine($"  StartDate: {created.StartDate}");
                Console.WriteLine($"  TargetDate: {created.TargetDate}");
            }
            else
            {
                Console.WriteLine("✗ FAIL: Regulation not found in database");
            }

            // Test 2: Duplicate enrollment should throw exception
            Console.WriteLine("\nTest 2: Attempting duplicate enrollment...");
            try
            {
                await service.EnrollAsync(request, CancellationToken.None);
                Console.WriteLine("✗ FAIL: Expected InvalidOperationException was not thrown");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"✓ PASS: InvalidOperationException thrown as expected");
                Console.WriteLine($"  Message: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ FAIL: Unexpected exception type: {ex.GetType().Name}");
                Console.WriteLine($"  Message: {ex.Message}");
            }

            // Cleanup
            Console.WriteLine("\nCleaning up test data...");
            if (created != null)
            {
                dbContext.Regulations.Remove(created);
                await dbContext.SaveChangesAsync();
                Console.WriteLine("✓ Test data cleaned up");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ FAIL: Unexpected error during test execution");
            Console.WriteLine($"  Type: {ex.GetType().Name}");
            Console.WriteLine($"  Message: {ex.Message}");
            Console.WriteLine($"  StackTrace: {ex.StackTrace}");
        }

        Console.WriteLine("\n==========================================\n");
    }
}
