using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace DemoDataSeeder;

internal static class SeederProgram
{
    private static readonly Guid OrganizationId = Guid.Parse("2759FAD4-C895-4A5F-9E49-6DED5D378098");
    private static readonly Guid HospitalId = Guid.Parse("D9F5554A-BF83-4262-B386-A31587B0BA06");
    private const string DefaultEmail = "ekta.gupta@gmail.com";
    private static readonly string[] DatabaseNames = ["Identity", "Organization", "Person", "Workforce", "Professional", "Governance", "AccessGovernance", "Compliance"];
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static async Task<int> RunAsync(string[] args)
    {
        var command = args.FirstOrDefault()?.ToLowerInvariant();
        if (command is not ("dry-run" or "seed" or "verify"))
        {
            Console.Error.WriteLine("Usage: dotnet run --project DemoDataSeeder -- <dry-run|seed|verify>");
            return 2;
        }

        var settings = Settings.FromEnvironment();
        var report = new SeedReport(command, settings.Email, OrganizationId, HospitalId);
        try
        {
            var context = await ResolveContextAsync(settings, report);
            if (command == "dry-run")
                await DryRunAsync(settings, context, report);
            else if (command == "seed")
                await SeedAsync(settings, context, report);
            else
                await VerifyAsync(settings, context, report);

            await WriteReportAsync(report);
            PrintReport(report);
            return report.Errors.Count == 0 ? 0 : 1;
        }
        catch (Exception ex)
        {
            report.Errors.Add(ex.Message);
            await WriteReportAsync(report);
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static async Task<TargetContext> ResolveContextAsync(Settings settings, SeedReport report)
    {
        await using var identity = await SchemaDb.OpenAsync(settings.Connection("Identity"));
        var userTable = await identity.FirstExistingTableAsync("AspNetUsers");
        var user = await identity.QuerySingleAsync(
            $"SELECT TOP (1) * FROM {Q(userTable)} WHERE LOWER([Email])=@email OR [NormalizedEmail]=@normalized",
            new("@email", settings.Email.ToLowerInvariant()), new("@normalized", settings.Email.ToUpperInvariant()));
        if (user is null)
            throw new InvalidOperationException($"Existing Identity account '{settings.Email}' was not found.");

        var userId = AsGuid(user, "Id");
        var identityPersonId = AsNullableGuid(user, "PersonId");

        await using var organization = await SchemaDb.OpenAsync(settings.Connection("Organization"));
        var orgExists = await organization.ExistsAsync("Organizations", new() { ["Id"] = OrganizationId });
        var hospitalExists = await organization.ExistsAsync("Hospitals", new() { ["Id"] = HospitalId, ["OrganizationId"] = OrganizationId });
        if (!orgExists || !hospitalExists)
            throw new InvalidOperationException("The requested organization/hospital pair does not exist in Organization.");

        var member = await organization.QuerySingleAsync(
            "SELECT TOP (1) * FROM [OrganizationMember] WHERE [OrganizationId]=@org AND ([UserId]=@user OR LOWER([Email])=@email) ORDER BY [IsPrimary] DESC",
            new("@org", OrganizationId), new("@user", userId), new("@email", settings.Email.ToLowerInvariant()),
            tolerateMissingTable: true)
            ?? await organization.QuerySingleAsync(
                "SELECT TOP (1) * FROM [OrganizationMembers] WHERE [OrganizationId]=@org AND ([UserId]=@user OR LOWER([Email])=@email) ORDER BY [IsPrimary] DESC",
                new("@org", OrganizationId), new("@user", userId), new("@email", settings.Email.ToLowerInvariant()),
                tolerateMissingTable: true)
            ?? await organization.QuerySingleAsync(
                "SELECT TOP (1) * FROM [Members] WHERE [OrganizationId]=@org AND ([UserId]=@user OR LOWER([Email])=@email) ORDER BY [IsPrimary] DESC",
                new("@org", OrganizationId), new("@user", userId), new("@email", settings.Email.ToLowerInvariant()),
                tolerateMissingTable: true);

        report.Context["UserId"] = userId.ToString();
        report.Context["OrganizationMemberId"] = member is null ? "(not found; optional link omitted)" : AsGuid(member, "Id").ToString();
        report.Context["IdentityPersonId"] = identityPersonId?.ToString() ?? "(not linked)";
        return new(userId, member is null ? null : AsGuid(member, "Id"), identityPersonId);
    }

    private static async Task DryRunAsync(Settings settings, TargetContext context, SeedReport report)
    {
        foreach (var database in DatabaseNames)
        {
            await using var db = await SchemaDb.OpenAsync(settings.Connection(database));
            var tables = await db.GetTableNamesAsync();
            report.Databases[database] = $"{tables.Count} tables reachable";
        }
        await using (var directory = await SchemaDb.OpenAsync(settings.Connection("EnterpriseDirectory")))
            report.Databases["EnterpriseDirectory"] = $"{(await directory.GetTableNamesAsync()).Count} tables reachable";

        await using var compliance = await SchemaDb.OpenAsync(settings.Connection("Compliance"));
        var oeCount = await compliance.ScalarAsync<int>("SELECT COUNT(*) FROM [ObjectiveElements]");
        report.Checks["ObjectiveElementsAvailable"] = oeCount.ToString();
        report.Checks["ExactScoreDistributionPossible"] = (oeCount >= 100).ToString();
        report.Checks["PlannedPeople"] = "72";
        report.Checks["PlannedDepartments"] = "16";
        report.Checks["PlannedOeDistribution"] = "25% implemented / 42% in progress / 33% pending";
        if (oeCount < 100)
            report.Errors.Add("At least 100 ObjectiveElements are required for the exact 25/42/33 distribution.");
    }

    private static async Task SeedAsync(Settings settings, TargetContext context, SeedReport report)
    {
        await SeedOrganizationAsync(settings, report);
        var people = await SeedPeopleAsync(settings, context, report);
        var workforce = await SeedWorkforceAsync(settings, context, people, report);
        await SeedIdentityAndMembershipsAsync(settings, context, people, workforce, report);
        await SeedAccessGovernanceAsync(settings, context, people, workforce, report);
        await SeedProfessionalAsync(settings, workforce, people, report);
        await SeedGovernanceAsync(settings, context, workforce, report);
        await SeedEnterpriseDirectoryAsync(settings, people, workforce, report);
        await SeedComplianceAsync(settings, context, workforce, report);
        await VerifyAsync(settings, context, report);
    }

    private static async Task SeedOrganizationAsync(Settings settings, SeedReport report)
    {
        await using var db = await SchemaDb.OpenAsync(settings.Connection("Organization"));
        await using var tx = await db.BeginAsync();
        var now = DateTime.UtcNow;

        await db.UpsertAsync("Organizations", new() { ["Id"] = OrganizationId }, new()
        {
            ["Name"] = "AOVIAS Multispeciality Hospital",
            ["Type"] = "Independent Multispeciality Hospital",
            ["Address"] = "18, Arogya Marg, Saket, New Delhi, Delhi 110017",
            ["Status"] = "Active", ["IsDeleted"] = false, ["EffectiveFrom"] = now.AddYears(-4)
        }, report);
        await db.UpsertAsync("Hospitals", new() { ["Id"] = HospitalId }, new()
        {
            ["OrganizationId"] = OrganizationId, ["Code"] = "AMH-DEL", ["Name"] = "AOVIAS Multispeciality Hospital",
            ["Address"] = "18, Arogya Marg, Saket, New Delhi, Delhi 110017",
            ["Type"] = "Small Multi-speciality Hospital", ["CountryCode"] = "IN", ["StateCode"] = "DL",
            ["BedCapacity"] = 25, ["BedCapacityRange"] = "1-50", ["IsOnboarded"] = true, ["WizardVersion"] = 1,
            ["NABHLevel"] = "Full Accreditation - Preparation", ["Status"] = "Active", ["IsDeleted"] = false
        }, report);
        await db.TryUpsertAsync("OrganizationSettings", new() { ["OrganizationId"] = OrganizationId }, new()
        {
            ["TimeZoneId"] = "India Standard Time", ["LanguageCode"] = "en-IN",
            ["WorkingDaysJson"] = "[\"Monday\",\"Tuesday\",\"Wednesday\",\"Thursday\",\"Friday\",\"Saturday\"]",
            ["BrandingAssetsJson"] = "{}", ["BrandingConfigJson"] = "{\"primaryColor\":\"#146C94\"}"
        }, report, "hospital profile settings");

        foreach (var (code, name, category) in DepartmentData())
        {
            var id = D($"department:{code}");
            await db.UpsertAsync("MasterDepartments", new() { ["Code"] = code }, new()
            {
                ["Id"] = id, ["Name"] = name, ["Category"] = category, ["Description"] = $"{name} services",
                ["DisplayOrder"] = Array.IndexOf(DepartmentData(), (code, name, category)) + 1, ["IsActive"] = true, ["CreatedAt"] = now
            }, report);
            var actualId = await db.SelectGuidAsync("MasterDepartments", "Id", new() { ["Code"] = code });
            await db.UpsertAsync("HospitalMasterDepartments",
                new() { ["HospitalId"] = HospitalId, ["MasterDepartmentId"] = actualId },
                new() { ["IsActive"] = true, ["CreatedAt"] = now }, report);

            await db.UpsertAsync("Departments", new() { ["OrganizationId"] = OrganizationId, ["Code"] = code }, new()
            {
                ["Id"] = D($"organization-department:{code}"), ["HospitalId"] = HospitalId, ["Name"] = name,
                ["Category"] = category, ["Description"] = $"{name} department of AOVIAS Multispeciality Hospital",
                ["CostCenter"] = $"CC-{code}", ["ServiceLine"] = category, ["Status"] = "Active",
                ["IsActive"] = true, ["IsDeleted"] = false, ["EffectiveFrom"] = now.AddYears(-3), ["CreatedAt"] = now
            }, report);
        }

        var unitTypes = await db.QueryAsync("""
            SELECT v.[Id], v.[Code]
            FROM [OrganizationLookupValues] v
            JOIN [OrganizationLookupCategories] c ON c.[Id]=v.[CategoryId]
            WHERE c.[Code]='UnitType' AND v.[Code] IN ('Campus','Building','Wing')
            """, tolerateMissingTable: true);
        if (unitTypes.Count > 0)
        {
            Guid UnitType(string code) => AsGuid(unitTypes.First(x => AsString(x, "Code").Equals(code, StringComparison.OrdinalIgnoreCase)), "Id");
            var campusId = D("org-unit:campus");
            var buildingId = D("org-unit:clinical-block");
            await db.TryUpsertAsync("OrganizationalUnits", new() { ["Id"] = campusId }, new()
            {
                ["OrganizationId"] = OrganizationId, ["HospitalId"] = HospitalId, ["UnitTypeValueId"] = UnitType("Campus"),
                ["Code"] = "SAKET-CAMPUS", ["Name"] = "Saket Hospital Campus", ["Status"] = "Active",
                ["EffectiveFrom"] = now.AddYears(-4), ["IsDeleted"] = false, ["CreatedAt"] = now
            }, report, "organizational units");
            await db.TryUpsertAsync("OrganizationalUnits", new() { ["Id"] = buildingId }, new()
            {
                ["OrganizationId"] = OrganizationId, ["HospitalId"] = HospitalId, ["ParentUnitId"] = campusId,
                ["UnitTypeValueId"] = UnitType("Building"), ["Code"] = "CLINICAL-BLOCK",
                ["Name"] = "Clinical and Diagnostic Block", ["Status"] = "Active",
                ["EffectiveFrom"] = now.AddYears(-4), ["IsDeleted"] = false, ["CreatedAt"] = now
            }, report, "organizational units");
            foreach (var (code, name) in new[]
                     {
                         ("GROUND", "Ground Floor - Emergency, OPD and Diagnostics"),
                         ("FIRST", "First Floor - Inpatient Wards and ICU"),
                         ("SECOND", "Second Floor - Operation Theatre and Administration")
                     })
                await db.TryUpsertAsync("OrganizationalUnits", new() { ["Id"] = D($"org-unit:{code}") }, new()
                {
                    ["OrganizationId"] = OrganizationId, ["HospitalId"] = HospitalId, ["ParentUnitId"] = buildingId,
                    ["UnitTypeValueId"] = UnitType("Wing"), ["Code"] = code, ["Name"] = name,
                    ["Status"] = "Active", ["EffectiveFrom"] = now.AddYears(-4), ["IsDeleted"] = false, ["CreatedAt"] = now
                }, report, "organizational units");
        }
        await tx.CommitAsync();
    }

    private static async Task<List<PersonSeed>> SeedPeopleAsync(Settings settings, TargetContext context, SeedReport report)
    {
        var people = BuildPeople(context.IdentityPersonId);
        await using var db = await SchemaDb.OpenAsync(settings.Connection("Person"));
        await using var tx = await db.BeginAsync();
        var now = DateTime.UtcNow;
        foreach (var person in people)
        {
            await db.UpsertAsync("Persons", new() { ["Id"] = person.Id }, new()
            {
                ["PlatformId"] = $"PER-{person.Ordinal:0000}", ["Status"] = 0, ["SearchName"] = person.Name.ToUpperInvariant(),
                ["LegalFirstName"] = person.FirstName, ["LegalLastName"] = person.LastName, ["DisplayName"] = person.Name,
                ["PreferredName"] = person.FirstName, ["ShortName"] = person.FirstName, ["Initials"] = $"{person.FirstName[0]}{person.LastName[0]}",
                ["DateOfBirth"] = new DateTime(1965 + person.Ordinal % 29, person.Ordinal % 12 + 1, person.Ordinal % 27 + 1),
                ["TimeZone"] = "India Standard Time", ["IsDeleted"] = false, ["CreatedByUserId"] = context.UserId, ["CreatedAt"] = now
            }, report);
            await db.UpsertAsync("PersonContacts", new() { ["Id"] = D($"contact:{person.Ordinal}") }, new()
            {
                ["PersonId"] = person.Id, ["ContactKind"] = "Email", ["Value"] = person.Email,
                ["NormalizedValue"] = person.Email.ToUpperInvariant(), ["IsPrimary"] = true, ["IsDeleted"] = false, ["CreatedAt"] = now
            }, report);
            await db.TryUpsertAsync("PersonAddresses", new() { ["Id"] = D($"address:{person.Ordinal}") }, new()
            {
                ["PersonId"] = person.Id, ["Line1"] = $"{20 + person.Ordinal}, Healthcare Enclave", ["City"] = person.City,
                ["State"] = person.State, ["PostalCode"] = $"{110000 + person.Ordinal:000000}", ["CountryCode"] = "IN",
                ["IsPrimary"] = true, ["IsDeleted"] = false, ["CreatedAt"] = now
            }, report, "person addresses");
        }
        await tx.CommitAsync();
        return people;
    }

    private static async Task<WorkforceSeed> SeedWorkforceAsync(Settings settings, TargetContext context, List<PersonSeed> people, SeedReport report)
    {
        await using var orgDb = await SchemaDb.OpenAsync(settings.Connection("Organization"));
        var departmentIds = new Dictionary<string, Guid>();
        foreach (var department in DepartmentData())
            departmentIds[department.Code] = await orgDb.SelectGuidAsync("MasterDepartments", "Id", new() { ["Code"] = department.Code });

        await using var db = await SchemaDb.OpenAsync(settings.Connection("Workforce"));
        await using var tx = await db.BeginAsync();
        var now = DateTime.UtcNow;
        var positions = new List<Guid>();
        for (var i = 0; i < DepartmentData().Length; i++)
        {
            var department = DepartmentData()[i];
            var id = D($"position:{department.Code}");
            await db.UpsertAsync("Positions", new() { ["OrganizationId"] = OrganizationId, ["PositionCode"] = $"POS-{department.Code}" }, new()
            {
                ["Id"] = id, ["HospitalId"] = HospitalId, ["PositionName"] = PositionName(i), ["Description"] = $"{department.Name} role",
                ["IsClinical"] = department.Category == "Clinical", ["IsAdministrative"] = department.Category != "Clinical",
                ["IsLeadership"] = i < 3, ["ApprovalLevel"] = i < 3 ? 3 : 1, ["IsActive"] = true, ["IsDeleted"] = false, ["CreatedAt"] = now
            }, report);
            positions.Add(await db.SelectGuidAsync("Positions", "Id", new() { ["OrganizationId"] = OrganizationId, ["PositionCode"] = $"POS-{department.Code}" }));
        }

        var employments = new List<Guid>();
        for (var i = 0; i < people.Count; i++)
        {
            var person = people[i];
            var employmentId = D($"employment:{person.Ordinal}");
            employments.Add(employmentId);
            await db.UpsertAsync("Employments", new() { ["Id"] = employmentId }, new()
            {
                ["EmploymentNumber"] = $"AOV-{person.Ordinal:0000}", ["PersonId"] = person.Id, ["OrganizationId"] = OrganizationId,
                ["HospitalId"] = HospitalId, ["UserId"] = person.Ordinal == 1 ? context.UserId : null,
                ["OrganizationMemberId"] = person.Ordinal == 1 ? context.OrganizationMemberId : null,
                ["PersonSearchName"] = person.Name.ToUpperInvariant(), ["JoiningDate"] = JoiningDateFor(i, now),
                ["ConfirmationDate"] = JoiningDateFor(i, now) <= now.AddMonths(-6) ? JoiningDateFor(i, now).AddMonths(6) : null,
                ["NoticePeriodDays"] = 30, ["IsDeleted"] = false,
                ["CreatedByUserId"] = context.UserId, ["CreatedAt"] = now
            }, report);
            var departmentIndex = EmployeeDepartmentIndex(i);
            await db.UpsertAsync("PositionAssignments", new() { ["Id"] = D($"position-assignment:{person.Ordinal}") }, new()
            {
                ["EmploymentId"] = employmentId, ["PositionId"] = positions[departmentIndex], ["EffectiveFrom"] = now.AddYears(-2),
                ["IsPrimary"] = true, ["IsDeleted"] = false, ["CreatedAt"] = now
            }, report);
            await db.UpsertAsync("DepartmentAssignments", new() { ["Id"] = D($"department-assignment:{person.Ordinal}") }, new()
            {
                ["EmploymentId"] = employmentId, ["MasterDepartmentId"] = departmentIds[DepartmentData()[departmentIndex].Code],
                ["EffectiveFrom"] = now.AddYears(-2), ["IsPrimary"] = true, ["AllocationPercentage"] = 100m,
                ["IsTemporary"] = false, ["IsDeleted"] = false, ["CreatedAt"] = now
            }, report);
            if (i > 0)
            {
                var managerIndex = DepartmentHeadIndex(departmentIndex);
                var manager = employments[managerIndex == i ? 0 : managerIndex];
                if (manager == employmentId) manager = employments[0];
                await db.TryUpsertAsync("ReportingRelationships", new() { ["Id"] = D($"reporting:{person.Ordinal}") }, new()
                {
                    ["EmploymentId"] = employmentId, ["ManagerEmploymentId"] = manager, ["EffectiveFrom"] = now.AddYears(-2),
                    ["IsDeleted"] = false, ["CreatedAt"] = now
                }, report, "reporting relationships");
            }
        }
        await tx.CommitAsync();
        return new(employments, positions, departmentIds);
    }

    private static async Task SeedIdentityAndMembershipsAsync(
        Settings settings,
        TargetContext context,
        List<PersonSeed> people,
        WorkforceSeed workforce,
        SeedReport report)
    {
        var now = DateTime.UtcNow;
        var userIds = people.Select(p => p.Ordinal == 1 ? context.UserId : D($"user:{p.Ordinal}")).ToList();

        await using (var identity = await SchemaDb.OpenAsync(settings.Connection("Identity")))
        await using (var tx = await identity.BeginAsync())
        {
            for (var i = 0; i < people.Count; i++)
            {
                var person = people[i];
                var userId = userIds[i];
                await identity.UpsertAsync("AspNetUsers", new() { ["Id"] = userId }, new()
                {
                    ["PlatformId"] = $"USR{person.Ordinal:00000000}", ["UserName"] = person.Email,
                    ["NormalizedUserName"] = person.Email.ToUpperInvariant(), ["Email"] = person.Email,
                    ["NormalizedEmail"] = person.Email.ToUpperInvariant(), ["EmailConfirmed"] = true,
                    ["SecurityStamp"] = D($"security-stamp:{person.Ordinal}").ToString("N"),
                    ["ConcurrencyStamp"] = D($"concurrency-stamp:{person.Ordinal}").ToString("N"),
                    ["PhoneNumber"] = $"+9198{person.Ordinal:00000000}", ["PhoneNumberConfirmed"] = true,
                    ["TwoFactorEnabled"] = false, ["LockoutEnabled"] = true, ["AccessFailedCount"] = 0,
                    ["CreatedAt"] = JoiningDateFor(i, now), ["ForcePasswordChange"] = false,
                    ["IsDeleted"] = false, ["Status"] = 0, ["TokenVersion"] = 0, ["PersonId"] = person.Id
                }, report);
            }
            await tx.CommitAsync();
        }

        await using var organization = await SchemaDb.OpenAsync(settings.Connection("Organization"));
        await using var orgTx = await organization.BeginAsync();
        var membershipTypeRows = await organization.QueryAsync("""
            SELECT v.[Id], v.[Code]
            FROM [OrganizationLookupValues] v
            JOIN [OrganizationLookupCategories] c ON c.[Id]=v.[CategoryId]
            WHERE c.[Code]='MembershipType'
            """, tolerateMissingTable: true);
        var organizationDepartments = new Dictionary<string, Guid>();
        foreach (var department in DepartmentData())
            organizationDepartments[department.Code] = await organization.SelectGuidAsync(
                "Departments", "Id", new() { ["OrganizationId"] = OrganizationId, ["Code"] = department.Code });

        for (var i = 0; i < people.Count; i++)
        {
            var person = people[i];
            var departmentIndex = EmployeeDepartmentIndex(i);
            var departmentCode = DepartmentData()[departmentIndex].Code;
            var memberId = person.Ordinal == 1 && context.OrganizationMemberId.HasValue
                ? context.OrganizationMemberId.Value
                : D($"organization-member:{person.Ordinal}");
            var membershipCode = i >= 70 ? "Vendor" : i is 30 or 31 ? "Consultant" : "Employee";
            var membershipTypeId = membershipTypeRows
                .FirstOrDefault(x => AsString(x, "Code").Equals(membershipCode, StringComparison.OrdinalIgnoreCase)) is { } typeRow
                ? AsGuid(typeRow, "Id")
                : (Guid?)null;
            var managerIndex = i == 0 ? -1 : DepartmentHeadIndex(departmentIndex);
            var role = i == 0 ? "ORG_ADMIN" : IsDepartmentHead(i) ? "DEPARTMENT_HEAD" : i is 69 ? "COMPLIANCE_VIEWER" : "USER";

            await organization.UpsertAsync("Members", new() { ["Id"] = memberId }, new()
            {
                ["OrganizationId"] = OrganizationId, ["UserId"] = userIds[i],
                ["DepartmentId"] = organizationDepartments[departmentCode], ["Role"] = role,
                ["FirstName"] = person.FirstName, ["LastName"] = person.LastName, ["Email"] = person.Email,
                ["PhoneNumber"] = $"+9198{person.Ordinal:00000000}", ["Designation"] = EmployeePositionName(i),
                ["ReportsToUserId"] = managerIndex >= 0 ? userIds[managerIndex] : null, ["PersonId"] = person.Id,
                ["EmploymentId"] = workforce.EmploymentIds[i], ["HospitalId"] = HospitalId,
                ["MembershipTypeValueId"] = membershipTypeId, ["EffectiveFrom"] = JoiningDateFor(i, now),
                ["IsDeleted"] = false, ["IsPrimary"] = true, ["MembershipStatus"] = "Active", ["CreatedAt"] = JoiningDateFor(i, now)
            }, report);
        }

        foreach (var departmentIndex in Enumerable.Range(0, DepartmentData().Length))
        {
            var code = DepartmentData()[departmentIndex].Code;
            await organization.UpsertAsync("Departments",
                new() { ["OrganizationId"] = OrganizationId, ["Code"] = code },
                new() { ["ManagerEmploymentId"] = workforce.EmploymentIds[DepartmentHeadIndex(departmentIndex)], ["UpdatedAt"] = now },
                report);
        }
        await orgTx.CommitAsync();
    }

    private static async Task SeedAccessGovernanceAsync(
        Settings settings,
        TargetContext context,
        List<PersonSeed> people,
        WorkforceSeed workforce,
        SeedReport report)
    {
        await using var db = await SchemaDb.OpenAsync(settings.Connection("AccessGovernance"));
        await using var tx = await db.BeginAsync();
        var roles = await db.QueryAsync(
            "SELECT [Id],[RoleCode] FROM [RoleDefinitions] WHERE [RoleCode] IN ('ORG_ADMIN','COMPLIANCE_OFFICER','MANAGER','USER') AND [IsDeleted]=0");
        Guid Role(string code)
        {
            var row = roles.FirstOrDefault(x => AsString(x, "RoleCode").Equals(code, StringComparison.OrdinalIgnoreCase))
                ?? roles.FirstOrDefault(x => AsString(x, "RoleCode").Equals("COMPLIANCE_OFFICER", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("AccessGovernance has no assignable role definitions.");
            return AsGuid(row, "Id");
        }

        var now = DateTime.UtcNow;
        for (var i = 0; i < people.Count; i++)
        {
            var roleCode = i == 0 ? "ORG_ADMIN" : "COMPLIANCE_OFFICER";
            var userId = i == 0 ? context.UserId : D($"user:{i + 1}");
            await db.UpsertAsync("RoleAssignments", new() { ["Id"] = D($"role-assignment:{i + 1}") }, new()
            {
                ["RoleDefinitionId"] = Role(roleCode), ["UserId"] = userId,
                ["OrganizationId"] = OrganizationId, ["HospitalId"] = HospitalId,
                ["DepartmentId"] = i == 0 ? null : D($"organization-department:{DepartmentData()[EmployeeDepartmentIndex(i)].Code}"),
                ["EffectiveFrom"] = JoiningDateFor(i, now), ["IsRevoked"] = false,
                ["AssignedByUserId"] = context.UserId, ["AssignedAt"] = JoiningDateFor(i, now),
                ["EmploymentId"] = workforce.EmploymentIds[i]
            }, report);
        }
        await tx.CommitAsync();
    }

    private static async Task SeedEnterpriseDirectoryAsync(
        Settings settings,
        List<PersonSeed> people,
        WorkforceSeed workforce,
        SeedReport report)
    {
        await using var db = await SchemaDb.OpenAsync(settings.Connection("EnterpriseDirectory"));
        await using var tx = await db.BeginAsync();
        var now = DateTime.UtcNow;
        for (var i = 0; i < people.Count; i++)
        {
            var person = people[i];
            var departmentIndex = EmployeeDepartmentIndex(i);
            var department = DepartmentData()[departmentIndex];
            var position = EmployeePositionName(i);
            var managerIndex = i == 0 ? -1 : DepartmentHeadIndex(departmentIndex);
            var managerName = managerIndex >= 0 ? people[managerIndex].Name : null;
            var category = ProfessionalCategory(i);
            var tags = new[] { department.Code, category, position, i % 2 == 0 ? "Day Shift" : "Rotational Shift" };
            await db.UpsertAsync("DirectoryIndex", new() { ["Id"] = D($"directory-index:{person.Ordinal}") }, new()
            {
                ["PersonId"] = person.Id, ["EmploymentId"] = workforce.EmploymentIds[i], ["OrganizationId"] = OrganizationId,
                ["HospitalId"] = HospitalId, ["DepartmentId"] = D($"organization-department:{department.Code}"),
                ["DisplayName"] = person.Name, ["PreferredName"] = person.FirstName, ["PlatformId"] = $"PER-{person.Ordinal:0000}",
                ["EmployeeNumber"] = $"AOV-{person.Ordinal:0000}", ["ProfessionalNumber"] = $"PRF-{person.Ordinal:0000}",
                ["EmailNormalized"] = person.Email.ToUpperInvariant(), ["PhoneNormalized"] = $"+9198{person.Ordinal:00000000}",
                ["DepartmentName"] = department.Name, ["HospitalName"] = "AOVIAS Multispeciality Hospital",
                ["Designation"] = position, ["PositionTitle"] = position, ["EmploymentStatus"] = "Active",
                ["EmploymentType"] = i is 30 or 31 ? "Visiting Consultant" : i >= 70 ? "Outsourced" : "Permanent",
                ["ProfessionalCategory"] = category, ["ManagerName"] = managerName,
                ["SearchBlob"] = $"{person.Name} {person.Email} AOV-{person.Ordinal:0000} {department.Name} {position} {category} AOVIAS Multispeciality Hospital".ToUpperInvariant(),
                ["PhoneticKey"] = $"{person.LastName.ToUpperInvariant()}-{person.FirstName[0]}",
                ["SkillsJson"] = JsonSerializer.Serialize(new[] { position, "Patient Safety", "Quality Improvement" }),
                ["CertificationsJson"] = JsonSerializer.Serialize(new[] { "Fire Safety", "Hand Hygiene", i % 3 == 0 ? "CPR" : "NABH Awareness" }),
                ["CommitteesJson"] = JsonSerializer.Serialize(i < 24 ? new[] { "Quality and Patient Safety Committee" } : Array.Empty<string>()),
                ["ResponsibilitiesJson"] = JsonSerializer.Serialize(new[] { $"{department.Name} service delivery", "Compliance task ownership" }),
                ["TagsJson"] = JsonSerializer.Serialize(tags), ["VisibilityFlags"] = "Organization,Hospital",
                ["LastIndexedUtc"] = now, ["IsActive"] = true
            }, report);
        }

        for (var i = 0; i < DepartmentData().Length; i++)
        {
            var department = DepartmentData()[i];
            await db.TryUpsertAsync("DirectoryFacets", new() { ["Id"] = D($"directory-facet:department:{department.Code}") }, new()
            {
                ["OrganizationId"] = OrganizationId, ["FacetType"] = "Department",
                ["FacetValue"] = department.Name, ["Count"] = DepartmentEnds[i] - DepartmentStarts[i], ["AsOfUtc"] = now
            }, report, "directory facets");
        }
        await db.TryUpsertAsync("DirectorySnapshots", new() { ["Id"] = D("directory-snapshot") }, new()
        {
            ["Version"] = 1, ["RowCount"] = people.Count, ["AsOfUtc"] = now, ["DurationMs"] = 0L
        }, report, "directory snapshot");
        await tx.CommitAsync();
    }

    private static async Task SeedProfessionalAsync(Settings settings, WorkforceSeed workforce, List<PersonSeed> people, SeedReport report)
    {
        await using var db = await SchemaDb.OpenAsync(settings.Connection("Professional"));
        await using var tx = await db.BeginAsync();
        var now = DateTime.UtcNow;
        var lookups = await db.QueryAsync("""
            SELECT c.[Code] AS [CategoryCode], v.[Code], v.[Id]
            FROM [ProfessionalLookupValues] v
            JOIN [ProfessionalLookupCategories] c ON c.[Id]=v.[CategoryId]
            """, tolerateMissingTable: true);
        Guid? Lookup(string category, string code)
        {
            var row = lookups.FirstOrDefault(x =>
                AsString(x, "CategoryCode").Equals(category, StringComparison.OrdinalIgnoreCase) &&
                AsString(x, "Code").Equals(code, StringComparison.OrdinalIgnoreCase));
            return row is null ? null : AsGuid(row, "Id");
        }
        var trainingTopics = new[]
        {
            "Hand Hygiene", "Fire Safety", "NABH Awareness", "Biomedical Waste Management",
            "Patient Safety", "Cardiopulmonary Resuscitation", "Medication Safety"
        };
        for (var i = 0; i < people.Count; i++)
        {
            var person = people[i];
            var profileId = D($"professional:{person.Ordinal}");
            var position = EmployeePositionName(i);
            var category = ProfessionalCategory(i);
            await db.UpsertAsync("ProfessionalProfiles", new() { ["Id"] = profileId }, new()
            {
                ["ProfessionalNumber"] = $"PRF-{person.Ordinal:0000}", ["EmploymentId"] = workforce.EmploymentIds[i],
                ["PersonId"] = person.Id, ["PersonSearchName"] = person.Name.ToUpperInvariant(),
                ["ProfessionalCategoryValueId"] = Lookup("ProfessionalCategory", category),
                ["Biography"] = $"{position} contributing to safe, patient-centred care and hospital quality improvement.",
                ["YearsOfExperience"] = 3 + i % 24, ["PracticeStartDate"] = now.AddYears(-(3 + i % 24)),
                ["OrganizationId"] = OrganizationId, ["HospitalId"] = HospitalId, ["IsDeleted"] = false, ["CreatedAt"] = now
            }, report);

            var authorityCode = RegistrationAuthority(category);
            if (authorityCode is not null)
            {
                var expired = i % 17 == 0 && i > 0;
                var expiringSoon = !expired && i % 11 == 0;
                var renewed = !expired && !expiringSoon && i % 7 == 0;
                var authorityPrefix = authorityCode switch
                {
                    "MCI" => "DMC", "NMC" => "DNC", "PCI" => "DPC", "LAB" => "DMLT", _ => "DPMC"
                };
                var registrationId = D($"registration:{person.Ordinal}");
                var expiry = expired ? now.AddDays(-45 - i) : expiringSoon ? now.AddDays(35 + i) : now.AddYears(2 + i % 3);
                await db.TryUpsertAsync("ProfessionalRegistrations", new() { ["Id"] = registrationId }, new()
                {
                    ["ProfessionalProfileId"] = profileId, ["RegistrationAuthorityValueId"] = Lookup("RegistrationAuthority", authorityCode),
                    ["RegistrationNumber"] = $"{authorityPrefix}-{201000 + person.Ordinal}", ["IssueDate"] = now.AddYears(-5 - i % 6),
                    ["ExpiryDate"] = expiry, ["Status"] = expired ? "Expired" : "Active",
                    ["VerificationStatus"] = renewed ? "Recently Renewed" : expiringSoon ? "Renewal Due" : expired ? "Expired" : "Verified",
                    ["IsDeleted"] = false, ["CreatedAt"] = now
                }, report, "professional registrations");
                if (renewed)
                    await db.TryUpsertAsync("RenewalHistory", new() { ["Id"] = D($"renewal:{person.Ordinal}") }, new()
                    {
                        ["ProfessionalRegistrationId"] = registrationId, ["RenewedAt"] = now.AddMonths(-2),
                        ["NewExpiryDate"] = expiry, ["Notes"] = "Registration renewed after credential verification."
                    }, report, "professional renewal history");
            }

            for (var topicIndex = 0; topicIndex < trainingTopics.Length; topicIndex++)
            {
                var completed = (i * trainingTopics.Length + topicIndex) % 100 < 78;
                await db.TryUpsertAsync("TrainingRecords", new() { ["Id"] = D($"training:{person.Ordinal}:{topicIndex}") }, new()
                {
                    ["ProfessionalProfileId"] = profileId, ["TrainingTypeValueId"] = Lookup("TrainingType", "MANDATORY"),
                    ["CourseName"] = trainingTopics[topicIndex], ["Provider"] = "AOVIAS Multispeciality Hospital Education Unit",
                    ["Hours"] = topicIndex == 5 ? 6m : 3m,
                    ["CompletionDate"] = completed ? now.AddMonths(-(1 + (i + topicIndex) % 10)) : null,
                    ["CertificateUrl"] = completed ? $"local://training-certificates/AOV-{person.Ordinal:0000}-{topicIndex + 1}.pdf" : null,
                    ["IsMandatory"] = true, ["RenewalRequired"] = true,
                    ["AssessmentScore"] = completed ? 72m + (i + topicIndex) % 27 : null,
                    ["IsDeleted"] = false, ["CreatedAt"] = now.AddMonths(-11)
                }, report, "training records");
            }
            await db.TryUpsertAsync("Qualifications", new() { ["Id"] = D($"qualification:{person.Ordinal}") }, new()
            {
                ["ProfessionalProfileId"] = profileId, ["QualificationTypeValueId"] = Lookup("QualificationType", "DEGREE"),
                ["Title"] = QualificationFor(category, position),
                ["Institution"] = i % 3 == 0 ? "All India Institute of Medical Sciences, New Delhi" :
                    i % 3 == 1 ? "Delhi University Faculty of Medical Sciences" : "Jamia Hamdard University",
                ["University"] = "Recognised Indian University", ["CountryCode"] = "IN", ["CompletionDate"] = now.AddYears(-(5 + i % 20)),
                ["VerificationStatus"] = "Verified", ["IsDeleted"] = false, ["CreatedAt"] = now
            }, report, "professional qualifications");
        }
        await tx.CommitAsync();
    }

    private static async Task SeedGovernanceAsync(Settings settings, TargetContext context, WorkforceSeed workforce, SeedReport report)
    {
        await using var db = await SchemaDb.OpenAsync(settings.Connection("Governance"));
        await using var tx = await db.BeginAsync();
        var now = DateTime.UtcNow;
        var lookupId = await EnsureGovernanceLookupAsync(db, report);
        var committees = new[]
        {
            ("QSC", "Quality and Patient Safety Committee"), ("ICC", "Infection Control Committee"),
            ("MEC", "Medical Executive Committee"), ("PTC", "Pharmacy and Therapeutics Committee"),
            ("BMW", "Biomedical Waste Management Committee"), ("FSC", "Fire and Life Safety Committee")
        };
        for (var i = 0; i < committees.Length; i++)
        {
            var committeeId = D($"committee:{committees[i].Item1}");
            await db.UpsertAsync("Committees", new() { ["CommitteeCode"] = committees[i].Item1 }, new()
            {
                ["Id"] = committeeId, ["CommitteeName"] = committees[i].Item2, ["CommitteeTypeValueId"] = lookupId,
                ["Charter"] = $"Oversight charter for {committees[i].Item2}.", ["OrganizationId"] = OrganizationId,
                ["HospitalId"] = HospitalId, ["ChairpersonAssigneeId"] = workforce.EmploymentIds[i],
                ["SecretaryAssigneeId"] = workforce.EmploymentIds[i + 4], ["TermStart"] = now.Date.AddMonths(-6),
                ["TermEnd"] = now.Date.AddMonths(18), ["Status"] = 0, ["IsDeleted"] = false, ["CreatedAt"] = now
            }, report);
            committeeId = await db.SelectGuidAsync("Committees", "Id", new() { ["CommitteeCode"] = committees[i].Item1 });
            for (var m = 0; m < 6; m++)
                await db.TryUpsertAsync("CommitteeMemberships", new() { ["Id"] = D($"committee-member:{i}:{m}") }, new()
                {
                    ["CommitteeId"] = committeeId, ["MemberAssigneeId"] = workforce.EmploymentIds[i * 6 + m],
                    ["CommitteeRoleValueId"] = lookupId, ["TermStart"] = now.Date.AddMonths(-6),
                    ["TermEnd"] = now.Date.AddMonths(18), ["VotingRights"] = true, ["Status"] = 0
                }, report, "committee memberships");

            for (var meetingIndex = 0; meetingIndex < 4; meetingIndex++)
            {
                var meetingId = D($"committee-meeting:{i}:{meetingIndex}");
                var scheduledAt = now.Date.AddMonths(-(meetingIndex + 1)).AddDays(5 + i);
                await db.TryUpsertAsync("CommitteeMeetings", new() { ["Id"] = meetingId }, new()
                {
                    ["CommitteeId"] = committeeId, ["ScheduledAt"] = scheduledAt, ["MeetingTypeValueId"] = lookupId,
                    ["Agenda"] = $"Review {committees[i].Item2} indicators, incidents, open actions and upcoming assurance activities.",
                    ["Status"] = 2, ["Minutes"] = "The committee reviewed trends, assigned owners and confirmed target dates for open actions.",
                    ["AttendanceJson"] = JsonSerializer.Serialize(Enumerable.Range(0, 6).Select(m => new
                    {
                        employmentId = workforce.EmploymentIds[i * 6 + m], present = m != 5
                    })),
                    ["CompletedAt"] = scheduledAt.AddHours(1)
                }, report, "committee meetings");
                await db.TryUpsertAsync("CommitteeDecisions", new() { ["Id"] = D($"committee-decision:{i}:{meetingIndex}") }, new()
                {
                    ["CommitteeMeetingId"] = meetingId, ["DecisionTypeValueId"] = lookupId,
                    ["Summary"] = $"Complete the priority action agreed during the {committees[i].Item2} review.",
                    ["GovernedResourceType"] = "ComplianceAction", ["OwnerAssigneeId"] = workforce.EmploymentIds[(i * 6 + meetingIndex) % workforce.EmploymentIds.Count],
                    ["ImplementationStatus"] = meetingIndex == 3 ? "In Progress" : "Completed", ["RecordedAt"] = scheduledAt.AddMinutes(55)
                }, report, "committee decisions");
            }
        }
        await tx.CommitAsync();
    }

    private static async Task<Guid> EnsureGovernanceLookupAsync(SchemaDb db, SeedReport report)
    {
        var categoryId = D("governance-lookup:COMMITTEE");
        var valueId = D("governance-lookup-value:STANDING");
        await db.TryUpsertAsync("LookupCategories", new() { ["Code"] = "COMMITTEE_GOVERNANCE_TYPE" }, new()
        {
            ["Id"] = categoryId, ["Name"] = "Committee Governance Type"
        }, report, "governance lookup category");
        categoryId = await db.SelectGuidAsync("LookupCategories", "Id", new() { ["Code"] = "COMMITTEE_GOVERNANCE_TYPE" });
        await db.TryUpsertAsync("LookupValues", new() { ["CategoryId"] = categoryId, ["Code"] = "STANDING" }, new()
        {
            ["Id"] = valueId, ["Name"] = "Standing Committee", ["SortOrder"] = 1
        }, report, "governance lookup value");
        return await db.SelectGuidAsync("LookupValues", "Id", new() { ["CategoryId"] = categoryId, ["Code"] = "STANDING" });
    }

    private static async Task SeedComplianceAsync(Settings settings, TargetContext context, WorkforceSeed workforce, SeedReport report)
    {
        await using var db = await SchemaDb.OpenAsync(settings.Connection("Compliance"));
        await using var tx = await db.BeginAsync();
        var now = DateTime.UtcNow;
        var existingJourney = await db.QuerySingleAsync("""
            SELECT TOP (1) j.*
            FROM [ComplianceJourneys] j
            JOIN [ComplianceFrameworks] f ON f.[Id]=j.[FrameworkId]
            WHERE j.[OrganizationId]=@org AND j.[HospitalId]=@hospital AND f.[Name] LIKE '%NABH%'
            ORDER BY CASE WHEN j.[Status] IN (0,1,3) THEN 0 ELSE 1 END, j.[CreatedAt]
            """, new SqlParameter("@org", OrganizationId), new SqlParameter("@hospital", HospitalId));
        var framework = existingJourney is null
            ? await db.QuerySingleAsync("SELECT TOP (1) * FROM [ComplianceFrameworks] WHERE [Name] LIKE '%NABH%' ORDER BY CASE WHEN [PlatformId]='CP00000001' THEN 0 ELSE 1 END, [Name]")
            : await db.QuerySingleAsync("SELECT TOP (1) * FROM [ComplianceFrameworks] WHERE [Id]=@id", new SqlParameter("@id", AsGuid(existingJourney, "FrameworkId")));
        if (framework is null) throw new InvalidOperationException("Compliance has no NABH framework master data.");
        var frameworkId = AsGuid(framework, "Id");
        var oeRows = await db.QueryAsync(
            """
            SELECT oe.[Id], oe.[ComplianceStandardId], oe.[Code], oe.[Title], oe.[Description],
                   s.[Name] AS [StandardName], s.[Code] AS [StandardCode],
                   c.[Code] AS [ChapterCode], c.[Name] AS [ChapterName]
            FROM [ObjectiveElements] oe
            JOIN [ComplianceStandards] s ON s.[Id]=oe.[ComplianceStandardId]
            JOIN [ComplianceChapters] c ON c.[Id]=s.[ChapterId]
            WHERE c.[FrameworkId]=@framework
            ORDER BY c.[Sequence], s.[Sequence], oe.[Code]
            """, false, new SqlParameter("@framework", frameworkId));
        if (oeRows.Count < 100) throw new InvalidOperationException("Compliance requires at least 100 ObjectiveElements for 25/42/33 demo distribution.");

        var journeyId = existingJourney is null ? D("compliance-journey") : AsGuid(existingJourney, "Id");
        await db.UpsertAsync("ComplianceJourneys", new() { ["Id"] = journeyId }, new()
        {
            ["TenantId"] = OrganizationId, ["OrganizationId"] = OrganizationId, ["HospitalId"] = HospitalId,
            ["FrameworkId"] = frameworkId, ["Status"] = 0, ["OperationalMode"] = 1, ["EnrollmentDate"] = now.AddMonths(-9),
            ["ValidityPeriodInMonths"] = 36, ["MinimumRequiredScore"] = 70m, ["CurrentReadinessScore"] = 25m,
            ["RiskScore"] = 42m, ["IsPublicCertificateVisible"] = false, ["IsLocked"] = false,
            ["CreatedAt"] = now.AddMonths(-8), ["UpdatedAt"] = now
        }, report);

        var completedCount = (int)Math.Round(oeRows.Count * .25m);
        var progressCount = (int)Math.Round(oeRows.Count * .42m);
        var existingTasks = (await db.QueryAsync(
                "SELECT * FROM [ComplianceTasks] WHERE [HospitalId]=@hospital",
                false, new SqlParameter("@hospital", HospitalId)))
            .Where(x => AsNullableGuid(x, "ObjectiveElementId").HasValue)
            .GroupBy(x => AsGuid(x, "ObjectiveElementId"))
            .ToDictionary(x => x.Key, x => x.First());
        var taskIds = new List<Guid>();
        for (var i = 0; i < oeRows.Count; i++)
        {
            var oeId = AsGuid(oeRows[i], "Id");
            var standardId = AsGuid(oeRows[i], "ComplianceStandardId");
            var departmentIndex = i % DepartmentData().Length;
            var department = DepartmentData()[departmentIndex];
            var objectiveTitle = ObjectiveTitle(oeRows[i]);
            var objectiveDescription = $"{CleanText(AsString(oeRows[i], "StandardName"))} Evidence must demonstrate sustained implementation, staff awareness and periodic monitoring.";
            await db.UpsertAsync("ObjectiveElements", new() { ["Id"] = oeId }, new()
            {
                ["Title"] = objectiveTitle, ["Description"] = objectiveDescription,
                ["Department"] = department.Name, ["ResponsibleRole"] = PositionName(departmentIndex),
                ["Frequency"] = i % 4 == 0 ? "Monthly" : i % 4 == 1 ? "Quarterly" : i % 4 == 2 ? "Half-yearly" : "Annual",
                ["EvidenceCategory"] = EvidenceCategory(i), ["Tags"] = $"NABH;{AsString(oeRows[i], "ChapterCode")};{department.Code}"
            }, report);
            var score = i < completedCount ? 5 : i < completedCount + progressCount ? 2 : 1;
            await db.UpsertAsync("ObjectiveElementScores", new() { ["Id"] = D($"oe-score:{journeyId}:{oeId}") }, new()
            {
                ["ObjectiveElementId"] = oeId, ["JourneyId"] = journeyId, ["AssessorId"] = context.UserId,
                ["Score"] = score, ["Rationale"] = score == 5 ? "Implemented and supported by current verified evidence." :
                    score == 2 ? "Implementation is in progress with assigned actions and partial evidence." :
                    "Implementation has not started; ownership and target dates are assigned.",
                ["EvidenceSampleCompliancePercent"] = score == 5 ? 92m : score == 2 ? 38m : 0m,
                ["IsDocumentationInadequate"] = score == 1, ["DowngradeApplied"] = false, ["IsApplicable"] = true,
                ["AssessedAt"] = now.AddDays(-(i % 180)), ["CreatedAt"] = now.AddMonths(-8)
            }, report);

            var taskId = existingTasks.TryGetValue(oeId, out var existingTask)
                ? AsGuid(existingTask, "Id")
                : D($"compliance-task:{oeId}");
            taskIds.Add(taskId);
            var taskStatus = i < completedCount ? 4 : i < completedCount + progressCount ? 1 : 0;
            var dueDate = taskStatus == 4
                ? now.AddDays(-(7 + i % 150))
                : i % 9 == 0 ? now.AddDays(-(3 + i % 45)) : now.AddDays(7 + i % 75);
            await db.UpsertAsync("ComplianceTasks", new() { ["Id"] = taskId }, new()
            {
                ["HospitalId"] = HospitalId, ["ComplianceStandardId"] = standardId, ["ObjectiveElementId"] = oeId,
                ["DepartmentId"] = D($"organization-department:{department.Code}"), ["DepartmentName"] = department.Name,
                ["AssignedToUserId"] = (i % 72 == 0 ? context.UserId : D($"user:{i % 72 + 1}")).ToString(),
                ["DueDate"] = dueDate, ["Status"] = taskStatus, ["Title"] = $"{AsString(oeRows[i], "Code")} — {objectiveTitle}",
                ["Description"] = $"Implement and verify: {CleanText(AsString(oeRows[i], "StandardName"))}",
                ["Criticality"] = 1 + i % 4, ["Priority"] = 1 + i % 4, ["Weight"] = 1m, ["CreatedAt"] = now.AddMonths(-8)
            }, report);
        }

        var artifacts = await CreateEvidenceArtifactsAsync();
        for (var i = 0; i < 420; i++)
        {
            var oe = oeRows[i % oeRows.Count];
            var oeId = AsGuid(oe, "Id");
            var taskId = taskIds[i % taskIds.Count];
            var department = DepartmentData()[i % DepartmentData().Length];
            var artifact = artifacts[i % artifacts.Count];
            var evidenceId = D($"evidence:{i}");
            var uploadedOn = now.AddDays(-(8 + i % 330));
            var verified = i % 5 != 0;
            await db.UpsertAsync("Evidences", new() { ["Id"] = evidenceId }, new()
            {
                ["TaskId"] = taskId, ["HospitalId"] = HospitalId, ["FrameworkId"] = frameworkId,
                ["EvidenceType"] = EvidenceCategory(i), ["ArtifactType"] = i % 3, ["LifecycleStatus"] = verified ? 2 : 1,
                ["Title"] = EvidenceTitle(i, department.Name, uploadedOn),
                ["Description"] = $"Hospital assurance record for {department.Name}, reviewed against {AsString(oe, "Code")}.",
                ["Source"] = "Hospital Quality Record", ["Department"] = department.Name,
                ["Tags"] = $"NABH,{department.Code},{EvidenceCategory(i)}",
                ["CurrentVersion"] = 1, ["FileUrl"] = artifact.Url, ["Hash"] = artifact.Hash,
                ["UploadedBy"] = (i % 72 == 0 ? context.UserId : D($"user:{i % 72 + 1}")).ToString(),
                ["UploadedOn"] = uploadedOn, ["ExpiresAt"] = i % 13 == 0 ? now.AddDays(45 + i % 60) : null,
                ["Metadata"] = JsonSerializer.Serialize(new { period = uploadedOn.ToString("yyyy-MM"), source = "Quality Department", confidential = false }),
                ["CreatedAt"] = uploadedOn
            }, report);
            await db.TryUpsertAsync("EvidenceVersions", new() { ["Id"] = D($"evidence-version:{i}") }, new()
            {
                ["EvidenceId"] = evidenceId, ["VersionNumber"] = 1, ["FileUrl"] = artifact.Url,
                ["Hash"] = artifact.Hash, ["CreatedBy"] = context.UserId, ["CreatedAt"] = uploadedOn
            }, report, "evidence versions");
            await db.TryUpsertAsync("ObjectiveElementEvidences", new() { ["Id"] = D($"oe-evidence:{i}") }, new()
            {
                ["ObjectiveElementId"] = oeId, ["EvidenceId"] = evidenceId, ["LinkedBy"] = context.UserId,
                ["LinkedAt"] = uploadedOn, ["VerifiedBy"] = verified ? context.UserId : null,
                ["VerifiedAt"] = verified ? uploadedOn.AddDays(2) : null, ["VerificationStatus"] = verified ? 1 : 4,
                ["Remarks"] = verified ? "Evidence verified for relevance, validity and traceability." : "Awaiting quality team review.",
                ["CreatedAt"] = uploadedOn
            }, report, "objective element evidence links");
        }

        var findingIds = new List<Guid>();
        for (var i = 0; i < 24; i++)
        {
            var id = D($"finding:{i}");
            findingIds.Add(id);
            var oeIndex = completedCount + progressCount + i;
            var departmentIndex = i % DepartmentData().Length;
            await db.TryUpsertAsync("Findings", new() { ["Id"] = id }, new()
            {
                ["TenantId"] = OrganizationId, ["Scope"] = 1, ["ComplianceJourneyId"] = journeyId,
                ["ObjectiveElementId"] = AsGuid(oeRows[oeIndex % oeRows.Count], "Id"), ["TaskId"] = taskIds[oeIndex % taskIds.Count],
                ["ComplianceStatus"] = 1, ["IsMandatory"] = true, ["RiskLevel"] = i % 3,
                ["CreatedByUserId"] = context.UserId, ["CreatedByType"] = 0, ["ConfidenceScore"] = .9m,
                ["Title"] = FindingTitle(i), ["Description"] = FindingDescription(i),
                ["ClauseReference"] = AsString(oeRows[oeIndex % oeRows.Count], "Code"), ["Severity"] = i < 4 ? 2 : 1,
                ["Status"] = i < 12 ? 4 : i % 3, ["Department"] = 1 + departmentIndex % 4, ["RaisedBy"] = 0,
                ["DateRaised"] = now.AddDays(-20 - i * 5), ["AssignedToUserId"] = D($"user:{DepartmentHeadIndex(departmentIndex) + 1}"),
                ["DueDate"] = i < 12 ? now.AddDays(-5 - i) : now.AddDays(i % 5 == 0 ? -7 : 18 + i),
                ["IsResolved"] = i < 12, ["ResolvedAt"] = i < 12 ? now.AddDays(-2 - i) : null,
                ["LastUpdatedAt"] = now, ["EvidenceCount"] = 1 + i % 3, ["FindingNumber"] = $"FND-2026-{i + 1:000}",
                ["FindingKind"] = "Finding", ["CreatedAt"] = now
            }, report, "findings");
        }

        for (var i = 0; i < 17; i++)
            await db.TryUpsertAsync("Capas", new() { ["Id"] = D($"capa:{i}") }, new()
            {
                ["TenantId"] = OrganizationId, ["OrganizationId"] = OrganizationId, ["HospitalId"] = HospitalId,
                ["FrameworkId"] = frameworkId, ["ObjectiveElementId"] = AsGuid(oeRows[(completedCount + progressCount + i) % oeRows.Count], "Id"),
                ["SourceFindingId"] = findingIds[i % findingIds.Count], ["Title"] = CapaTitle(i),
                ["Description"] = "Corrective and preventive action arising from internal assurance review.",
                ["RootCause"] = RootCause(i), ["CorrectiveAction"] = CorrectiveAction(i),
                ["PreventiveAction"] = PreventiveAction(i), ["OwnerUserId"] = D($"user:{i % 72 + 1}"),
                ["OwnerName"] = BuildPeople(null)[i % 72].Name,
                ["DepartmentId"] = D($"organization-department:{DepartmentData()[i % DepartmentData().Length].Code}"),
                ["DueDate"] = i < 11 ? now.AddDays(-(5 + i)) : i < 15 ? now.AddDays(15 + i) : now.AddDays(-10 - i),
                ["Status"] = i < 11 ? 4 : i < 15 ? 1 : 0, ["ApprovedBy"] = i < 11 ? context.UserId : null,
                ["ApprovedAt"] = i < 11 ? now.AddDays(-20 - i) : null, ["CreatedAt"] = now.AddMonths(-5)
            }, report, "CAPA");

        for (var i = 0; i < 15; i++)
            await db.TryUpsertAsync("Risks", new() { ["Id"] = D($"risk:{i}") }, new()
            {
                ["HospitalId"] = HospitalId, ["Title"] = RiskTitle(i), ["Description"] = RiskDescription(i), ["Severity"] = i % 4,
                ["Likelihood"] = 2 + i % 4, ["Impact"] = 2 + (i + 1) % 4, ["Status"] = i % 3,
                ["IdentifiedAt"] = now.AddDays(-45 - i * 6), ["ObjectiveElementId"] = AsGuid(oeRows[(completedCount + i) % oeRows.Count], "Id"),
                ["TaskId"] = taskIds[(completedCount + i) % taskIds.Count], ["FindingId"] = i < findingIds.Count ? findingIds[i] : null,
                ["CreatedAt"] = now.AddMonths(-7)
            }, report, "risks");

        var auditScopes = new[] { "Infection Control", "Pharmacy", "Operation Theatre", "Intensive Care Unit", "Housekeeping", "Medication Management", "Patient Identification", "Laboratory", "Radiology", "Fire and Life Safety", "Biomedical Waste", "Human Resources", "Nursing", "Emergency", "Documentation Control", "Quality Governance" };
        for (var i = 0; i < auditScopes.Length; i++)
        {
            var completed = i < 12;
            var auditDate = completed ? now.AddMonths(-(1 + i % 8)).AddDays(i) : now.AddDays(10 + i * 8);
            await db.TryUpsertAsync("ScheduledAudits", new() { ["Id"] = D($"scheduled-audit:{i}") }, new()
            {
                ["TenantId"] = OrganizationId, ["OrganizationId"] = OrganizationId, ["HospitalId"] = HospitalId,
                ["FrameworkId"] = frameworkId, ["ComplianceJourneyId"] = journeyId, ["Title"] = $"{auditScopes[i]} Internal Audit",
                ["ScheduledDate"] = auditDate, ["Status"] = completed ? 2 : i % 2,
                ["AuditorName"] = i % 2 == 0 ? "Dr. Meera Iyer" : "Ananya Nair", ["Result"] = completed ? "Completed with follow-up actions" : null,
                ["CreatedAt"] = now.AddMonths(-9)
            }, report, "scheduled audits");
            var sessionId = D($"audit-session:{i}");
            await db.TryUpsertAsync("AuditSessions", new() { ["Id"] = sessionId }, new()
            {
                ["TenantId"] = OrganizationId, ["HospitalId"] = HospitalId, ["FrameworkId"] = frameworkId,
                ["SessionNumber"] = $"IA-2026-{i + 1:000}", ["AuditType"] = 0, ["Scope"] = auditScopes[i],
                ["LeadAuditorUserId"] = context.UserId, ["LeadAuditorName"] = i % 2 == 0 ? "Dr. Meera Iyer" : "Ananya Nair",
                ["AuditorTeamJson"] = "[{\"name\":\"Quality Executive\",\"role\":\"Auditor\"},{\"name\":\"Department Representative\",\"role\":\"Observer\"}]",
                ["PlannedStartDate"] = auditDate, ["PlannedEndDate"] = auditDate.AddDays(1),
                ["ActualStartDate"] = completed ? auditDate : null, ["ActualEndDate"] = completed ? auditDate.AddDays(1) : null,
                ["Status"] = completed ? 5 : i % 2, ["OverallResult"] = completed ? 2 : 0,
                ["Summary"] = completed ? $"{auditScopes[i]} controls were reviewed; findings and recommendations were assigned." : $"Planned review of {auditScopes[i]} controls and evidence.",
                ["CreatedByUserId"] = context.UserId, ["LastUpdatedAt"] = now, ["CreatedAt"] = now.AddMonths(-9)
            }, report, "audit sessions");
            for (var observationIndex = 0; observationIndex < 2; observationIndex++)
                await db.TryUpsertAsync("AuditObservations", new() { ["Id"] = D($"audit-observation:{i}:{observationIndex}") }, new()
                {
                    ["TenantId"] = OrganizationId, ["AuditSessionId"] = sessionId,
                    ["ObservationNumber"] = $"OBS-{i + 1:00}-{observationIndex + 1}",
                    ["Type"] = observationIndex, ["Title"] = observationIndex == 0 ? "Evidence traceability requires reinforcement" : "Staff awareness was inconsistent across shifts",
                    ["Description"] = observationIndex == 0 ? "A sample of records did not include complete review and approval traceability." : "Night-shift staff could not consistently describe the latest approved procedure.",
                    ["ObjectiveElementId"] = AsGuid(oeRows[(i * 2 + observationIndex) % oeRows.Count], "Id"),
                    ["Department"] = 1 + i % 4, ["CreatedByUserId"] = context.UserId, ["CreatedByName"] = "Quality Audit Team",
                    ["LastUpdatedAt"] = now, ["CreatedAt"] = auditDate
                }, report, "audit observations");
        }

        await SeedDocumentsAsync(db, context, frameworkId, oeRows, now, report);
        await SeedIncidentsAsync(db, context, now, report);
        await SeedIndicatorsAsync(db, context, oeRows, now, report);
        await SeedDashboardAsync(db, frameworkId, journeyId, oeRows.Count, completedCount, progressCount, now, report);
        await tx.CommitAsync();
    }

    private static async Task SeedDocumentsAsync(SchemaDb db, TargetContext context, Guid frameworkId, List<Dictionary<string, object?>> oes, DateTime now, SeedReport report)
    {
        var placeholder = await db.QuerySingleAsync(
            """
            SELECT TOP (1) * FROM [ControlledDocuments]
            WHERE [TenantId]=@tenant AND [HospitalId]=@hospital
              AND ([DocumentNumber]='POL-2026-001' OR LOWER([Title]) IN ('test','demo','sample'))
            ORDER BY CASE WHEN [DocumentNumber]='POL-2026-001' THEN 0 ELSE 1 END
            """,
            new SqlParameter("@tenant", OrganizationId), new SqlParameter("@hospital", HospitalId));
        const int totalDocuments = 194;
        for (var i = 0; i < totalDocuments; i++)
        {
            var documentId = i == 0 && placeholder is not null ? AsGuid(placeholder, "Id") : D($"document:{i}");
            var versionId = D($"document-version:{i}");
            var department = DepartmentData()[(i / DocumentSubjects.Length + i) % DepartmentData().Length];
            var subject = DocumentSubjects[i % DocumentSubjects.Length];
            var (documentType, typeName, prefix, schema) = DocumentTypeInfo(i);
            var effective = i < 185;
            var owner = BuildPeople(null)[DepartmentHeadIndex(Array.IndexOf(DepartmentData(), department))].Name;
            var effectiveDate = now.AddMonths(-(2 + i % 10));
            var nextReviewDate = effective ? now.AddMonths(2 + i % 11) : now.AddDays(-(3 + i % 75));
            await db.TryUpsertAsync("ControlledDocuments", new() { ["Id"] = documentId }, new()
            {
                ["TenantId"] = OrganizationId, ["HospitalId"] = HospitalId, ["DocumentNumber"] = $"{prefix}-2026-{i + 1:000}",
                ["Title"] = $"{subject} {typeName} — {department.Name}", ["Description"] = $"Controlled {typeName.ToLowerInvariant()} governing {subject.ToLowerInvariant()} in {department.Name}.",
                ["DocumentType"] = documentType, ["Status"] = effective ? 4 : 5, ["Department"] = department.Name,
                ["OwnerUserId"] = D($"user:{DepartmentHeadIndex(Array.IndexOf(DepartmentData(), department)) + 1}"), ["OwnerName"] = owner,
                ["ReviewerUserId"] = context.UserId, ["ReviewerName"] = "Quality and Accreditation Head",
                ["ApproverUserId"] = context.UserId, ["ApproverName"] = "Hospital Administrator",
                ["CurrentVersionId"] = versionId, ["CurrentVersionLabel"] = i % 9 == 0 ? "2.0" : "1.0", ["EffectiveDate"] = effectiveDate,
                ["ReviewDate"] = effectiveDate.AddDays(-10), ["NextReviewDate"] = nextReviewDate, ["ReviewFrequencyMonths"] = 12,
                ["FrameworkId"] = frameworkId, ["MappedFramework"] = "NABH", ["MappedObjectiveElementId"] = AsGuid(oes[i], "Id"),
                ["MappedObjectiveElement"] = AsString(oes[i], "Code"), ["IsArchived"] = false, ["CreatedByUserId"] = context.UserId,
                ["CreatedByName"] = owner, ["LastUpdatedAt"] = now, ["CreatedAt"] = now.AddMonths(-11)
            }, report, "controlled documents");
            await db.TryUpsertAsync("DocumentVersions", new() { ["Id"] = versionId }, new()
            {
                ["ControlledDocumentId"] = documentId, ["VersionNumber"] = i % 9 == 0 ? 2 : 1,
                ["VersionLabel"] = i % 9 == 0 ? "2.0" : "1.0", ["AuthoringSchemaKey"] = schema,
                ["Content"] = JsonSerializer.Serialize(new
                {
                    type = "doc",
                    title = $"{subject} {typeName}",
                    sections = new[]
                    {
                        new { heading = "Purpose", text = $"To standardise {subject.ToLowerInvariant()} and protect patients, staff and visitors." },
                        new { heading = "Scope", text = $"Applies to all personnel working in or supporting {department.Name}." },
                        new { heading = "Responsibilities", text = $"{owner} owns implementation; the Quality Department monitors compliance." },
                        new { heading = "Monitoring", text = "Compliance is reviewed through audit, training, incident trends and committee oversight." }
                    }
                }),
                ["ChangeSummary"] = i % 9 == 0 ? "Annual review updated responsibilities, monitoring indicators and escalation timelines." : "Initial approved issue",
                ["StatusAtCreation"] = effective ? 4 : 5, ["IsCurrent"] = true, ["IsSuperseded"] = false,
                ["CreatedByUserId"] = context.UserId, ["CreatedByName"] = owner, ["CreatedAt"] = effectiveDate
            }, report, "document versions");
            await db.TryUpsertAsync("DocumentReviews", new() { ["Id"] = D($"document-review:{i}") }, new()
            {
                ["ControlledDocumentId"] = documentId, ["DocumentVersionId"] = versionId,
                ["ScheduledDate"] = effectiveDate.AddDays(-14), ["CompletedDate"] = effective ? effectiveDate.AddDays(-7) : null,
                ["ReviewerUserId"] = context.UserId, ["ReviewerName"] = "Quality and Accreditation Head",
                ["Outcome"] = effective ? "Approved" : "Review Due",
                ["Notes"] = effective ? "Reviewed for clinical applicability, current regulation and NABH alignment." : "Scheduled review is awaiting the document owner.",
                ["CreatedAt"] = effectiveDate.AddDays(-14)
            }, report, "document reviews");
            await db.TryUpsertAsync("DocumentApprovals", new() { ["Id"] = D($"document-approval:{i}") }, new()
            {
                ["DocumentVersionId"] = versionId, ["ApprovalOrder"] = 1, ["ApproverRole"] = "Hospital Administrator",
                ["ApproverUserId"] = context.UserId, ["ApproverName"] = "Ekta Gupta", ["Status"] = effective ? 1 : 0,
                ["DecidedAt"] = effective ? effectiveDate.AddDays(-3) : null,
                ["Comments"] = effective ? "Approved for hospital-wide implementation." : "Approval pending completion of scheduled review.",
                ["CreatedAt"] = effectiveDate.AddDays(-5)
            }, report, "document approvals");
            var acknowledged = i % 5 != 0;
            await db.TryUpsertAsync("DocumentAcknowledgements", new() { ["Id"] = D($"document-acknowledgement:{i}") }, new()
            {
                ["ControlledDocumentId"] = documentId, ["DocumentVersionId"] = versionId, ["AssigneeType"] = 1,
                ["AssigneeKey"] = department.Code, ["AssigneeName"] = department.Name,
                ["AssigneeUserId"] = D($"user:{DepartmentHeadIndex(Array.IndexOf(DepartmentData(), department)) + 1}"),
                ["ReadAt"] = acknowledged ? effectiveDate.AddDays(2) : null, ["AcknowledgedAt"] = acknowledged ? effectiveDate.AddDays(3) : null,
                ["ReminderCount"] = acknowledged ? 0 : 2, ["LastReminderAt"] = acknowledged ? null : now.AddDays(-3),
                ["CreatedAt"] = effectiveDate
            }, report, "document acknowledgements");
        }
    }

    private static async Task SeedIncidentsAsync(SchemaDb db, TargetContext context, DateTime now, SeedReport report)
    {
        var existing = await db.QuerySingleAsync(
            """
            SELECT TOP (1) * FROM [Incidents]
            WHERE [TenantId]=@tenant AND [HospitalId]=@hospital
              AND ([IncidentNumber]='INC-2026-00001' OR LOWER([Title]) IN ('wrong medication','test','demo','sample'))
            ORDER BY CASE WHEN [IncidentNumber]='INC-2026-00001' THEN 0 ELSE 1 END, [CreatedAt]
            """,
            new SqlParameter("@tenant", OrganizationId), new SqlParameter("@hospital", HospitalId));
        var incidentTitles = new[]
        {
            "Look-alike medication selected before administration", "Assisted patient fall near inpatient washroom",
            "Needlestick injury during blood sample disposal", "Near miss during patient identification",
            "Infusion pump battery failure during transfer", "Delayed communication of critical potassium result",
            "Incomplete allergy documentation at admission", "Specimen label mismatch detected before processing",
            "Oxygen cylinder found below minimum reserve", "Wheelchair brake failure identified during safety round",
            "High-alert medicine double-check omitted", "Minor burn from hot water dispenser",
            "Temporary loss of access to electronic records", "Incorrect diet tray intercepted before service",
            "Sharps container exceeded safe fill line", "Patient transfer handover omitted fall-risk status",
            "Fire door obstructed by supply trolley", "Radiology report acknowledgement delayed"
        };
        for (var i = 0; i < incidentTitles.Length; i++)
        {
            var incidentId = i == 0 && existing is not null ? AsGuid(existing, "Id") : D($"incident:{i}");
            var closed = i < 15;
            var incidentDate = now.AddDays(-(4 + i * 9));
            var owner = BuildPeople(null)[(i + 14) % 72];
            await db.TryUpsertAsync("Incidents", new() { ["Id"] = incidentId }, new()
            {
                ["TenantId"] = OrganizationId, ["HospitalId"] = HospitalId, ["IncidentNumber"] = $"INC-2026-{i + 1:00000}",
                ["Title"] = incidentTitles[i],
                ["Description"] = $"The event was reported through the hospital safety system, immediate controls were applied and no patient-identifiable information is retained in this training environment.",
                ["IncidentType"] = i % 3,
                ["Severity"] = i % 4, ["Priority"] = i % 3, ["Category"] = i % 8, ["Location"] = DepartmentData()[i % DepartmentData().Length].Name,
                ["Department"] = 1 + i % 4, ["ReporterUserId"] = D($"user:{i % 72 + 1}"), ["ReporterName"] = BuildPeople(null)[i % 72].Name,
                ["IsAnonymous"] = false, ["PatientInvolved"] = i % 4 != 2,
                ["StaffInvolvedJson"] = JsonSerializer.Serialize(new[] { new { name = BuildPeople(null)[(i + 1) % 72].Name, role = EmployeePositionName((i + 1) % 72) } }),
                ["IncidentDateTime"] = incidentDate, ["ReportedAt"] = incidentDate.AddMinutes(35),
                ["Status"] = closed ? 6 : 3, ["InvestigationStatus"] = closed ? 4 : 2,
                ["RcaRequired"] = i % 3 == 0, ["OwnerUserId"] = D($"user:{owner.Ordinal}"), ["OwnerName"] = owner.Name,
                ["RelatedDocumentationJson"] = "[\"Incident Reporting Procedure\",\"Patient Safety Policy\"]",
                ["ClosureSummary"] = closed ? "Investigation completed, action effectiveness reviewed and the incident closed by the Patient Safety Committee." : null,
                ["ClosedAt"] = closed ? incidentDate.AddDays(14 + i % 20) : null,
                ["LastUpdatedAt"] = now, ["CreatedAt"] = incidentDate
            }, report, "incidents");
            var existingInvestigation = await db.QuerySingleAsync(
                "SELECT TOP (1) [Id] FROM [IncidentInvestigations] WHERE [IncidentId]=@incident",
                new SqlParameter("@incident", incidentId));
            var investigationId = existingInvestigation is null ? D($"investigation:{i}") : AsGuid(existingInvestigation, "Id");
            await db.TryUpsertAsync("IncidentInvestigations", new() { ["Id"] = investigationId }, new()
            {
                ["IncidentId"] = incidentId, ["Status"] = closed ? 4 : 2,
                ["IncidentSummary"] = $"Multidisciplinary review of: {incidentTitles[i]}.",
                ["Recommendations"] = "Standardise the verification step, brief affected staff and monitor compliance for three months.",
                ["ContributingFactorsJson"] = "[\"workload\",\"handoff\",\"environment\"]",
                ["PeopleFactorsJson"] = "[]", ["EnvironmentFactorsJson"] = "[]", ["EquipmentFactorsJson"] = "[]",
                ["ProcessFactorsJson"] = "[\"verification\"]", ["CommunicationFactorsJson"] = "[\"handoff\"]",
                ["TimelineJson"] = JsonSerializer.Serialize(new[] { new { at = incidentDate, action = "Event reported" }, new { at = incidentDate.AddDays(1), action = "Investigation opened" } }),
                ["ReviewersJson"] = "[\"Patient Safety Officer\",\"Department Head\",\"Quality Manager\"]",
                ["LastUpdatedAt"] = now, ["CreatedAt"] = incidentDate.AddDays(1)
            }, report, "incident investigations");
            if (i % 3 == 0)
            {
                var existingRca = await db.QuerySingleAsync(
                    "SELECT TOP (1) [Id] FROM [IncidentRcas] WHERE [IncidentId]=@incident",
                    new SqlParameter("@incident", incidentId));
                var rcaId = existingRca is null ? D($"incident-rca:{i}") : AsGuid(existingRca, "Id");
                await db.TryUpsertAsync("IncidentRcas", new() { ["Id"] = rcaId }, new()
                {
                    ["IncidentId"] = incidentId, ["Method"] = 0,
                    ["RootCauseSummary"] = "The workflow did not consistently protect the required independent verification step during peak workload.",
                    ["FiveWhysJson"] = "[\"Required check missed\",\"Staff member interrupted\",\"No protected verification point\",\"Workflow relied on memory\",\"Process design had not been reviewed after service growth\"]",
                    ["FishboneJson"] = "{}", ["HumanFactorsJson"] = "[]", ["ProcessFactorsJson"] = "[\"workflow\"]",
                    ["EnvironmentalFactorsJson"] = "[]", ["EquipmentFactorsJson"] = "[]", ["OrganizationFactorsJson"] = "[\"standardisation\"]",
                    ["LastUpdatedAt"] = now, ["CreatedAt"] = incidentDate.AddDays(3)
                }, report, "incident RCA");
            }
        }
    }

    private static async Task SeedIndicatorsAsync(SchemaDb db, TargetContext context, List<Dictionary<string, object?>> oes, DateTime now, SeedReport report)
    {
        var indicators = new[] { ("CLABSI", "Central line infection rate", 1.1m, 1.0m), ("FALL", "Patient falls per 1,000 bed days", 1.8m, 1.5m), ("MEDERR", "Medication error rate", .7m, .5m), ("HH", "Hand hygiene compliance", 92m, 95m), ("TAT", "Critical result communication within target", 88m, 95m) };
        for (var i = 0; i < indicators.Length; i++)
        {
            var id = D($"indicator:{indicators[i].Item1}");
            await db.TryUpsertAsync("ClinicalIndicators", new() { ["Id"] = id }, new()
            {
                ["TenantId"] = OrganizationId, ["HospitalId"] = HospitalId, ["IndicatorCode"] = indicators[i].Item1,
                ["Name"] = indicators[i].Item2, ["Description"] = $"Monthly quality indicator: {indicators[i].Item2}.",
                ["Category"] = i % 6, ["Department"] = 1 + i % 4, ["MeasurementUnit"] = 0, ["CalculationMethod"] = 0,
                ["NumeratorDefinition"] = "Qualifying events", ["DenominatorDefinition"] = "Eligible opportunities",
                ["FormulaExpression"] = "(Numerator / Denominator) * 100", ["CurrentNumerator"] = indicators[i].Item3,
                ["CurrentDenominator"] = 100m, ["CurrentValue"] = indicators[i].Item3, ["TargetValue"] = indicators[i].Item4,
                ["GreenThreshold"] = indicators[i].Item4, ["YellowThreshold"] = indicators[i].Item4 * .9m,
                ["RedThreshold"] = indicators[i].Item4 * .75m, ["CriticalThreshold"] = indicators[i].Item4 * .5m,
                ["LowerIsBetter"] = i < 3, ["Trend"] = i % 4, ["Frequency"] = 2, ["OwnerUserId"] = context.UserId,
                ["OwnerName"] = DefaultEmail, ["FrameworkMapping"] = 0, ["FrameworkReference"] = AsString(oes[i], "Code"),
                ["ObjectiveElementId"] = AsGuid(oes[i], "Id"), ["Status"] = 0, ["RiskLevel"] = i % 3,
                ["ThresholdStatus"] = i % 4, ["DataSource"] = "Hospital Quality Indicator Register", ["LastCalculatedAt"] = now,
                ["Comments"] = "Validated monthly value reviewed by the Quality and Patient Safety Committee.", ["RelatedDocumentationJson"] = "[]",
                ["RelatedIncidentCategoriesJson"] = "[]", ["LastUpdatedAt"] = now, ["CreatedAt"] = now
            }, report, "clinical indicators");
            for (var month = 0; month < 6; month++)
                await db.TryUpsertAsync("IndicatorMeasurements", new() { ["Id"] = D($"indicator-measure:{i}:{month}") }, new()
                {
                    ["IndicatorId"] = id, ["PeriodStart"] = new DateTime(now.Year, now.Month, 1).AddMonths(-month),
                    ["PeriodEnd"] = new DateTime(now.Year, now.Month, 1).AddMonths(1 - month).AddDays(-1),
                    ["Frequency"] = 2, ["Value"] = indicators[i].Item3 + (month - 2) * .1m,
                    ["Source"] = "Hospital Quality Indicator Register", ["Notes"] = "Monthly value verified by the indicator owner.",
                    ["CalculatedAt"] = now, ["CreatedAt"] = now
                }, report, "indicator measurements");
        }
    }

    private static async Task SeedDashboardAsync(
        SchemaDb db,
        Guid frameworkId,
        Guid journeyId,
        int totalObjectiveElements,
        int completedObjectiveElements,
        int inProgressObjectiveElements,
        DateTime now,
        SeedReport report)
    {
        var pendingObjectiveElements = totalObjectiveElements - completedObjectiveElements - inProgressObjectiveElements;
        var overallScore = Math.Round(completedObjectiveElements * 100m / totalObjectiveElements, 1);
        var chapterBreakdown = DepartmentData().Select((d, i) => new
        {
            chapterCode = d.Code,
            chapterName = d.Name,
            completionPercentage = 14 + i * 2 % 24,
            implemented = completedObjectiveElements / DepartmentData().Length,
            inProgress = inProgressObjectiveElements / DepartmentData().Length
        });
        await db.TryUpsertAsync("ComplianceSnapshots", new() { ["Id"] = D("compliance-snapshot") }, new()
        {
            ["TenantId"] = OrganizationId, ["HospitalId"] = HospitalId, ["FrameworkId"] = frameworkId,
            ["ComplianceJourneyId"] = journeyId, ["OverallScore"] = overallScore, ["AuditConfidenceScore"] = 61m,
            ["CriticalCount"] = 3, ["MajorCount"] = 7, ["MinorCount"] = 9,
            ["ChapterBreakdownJson"] = JsonSerializer.Serialize(chapterBreakdown),
            ["MajorNCCount"] = 4, ["MinorNCCount"] = 8, ["ObservationCount"] = 12, ["OFICount"] = 6,
            ["HighRiskChaptersJson"] = JsonSerializer.Serialize(new[]
            {
                new { chapterCode = "HRM", chapterName = "Human Resource Management", completionPercentage = 18, totalObjectiveElements = 42, completedObjectiveElements = 8 },
                new { chapterCode = "FMS", chapterName = "Facility Management and Safety", completionPercentage = 21, totalObjectiveElements = 38, completedObjectiveElements = 8 }
            }),
            ["CriticalRiskCount"] = 3, ["JourneyState"] = 1, ["CalculatedAt"] = now, ["CreatedAt"] = now
        }, report, "compliance snapshot");
        await db.TryUpsertAsync("AnalyticsDailySnapshots", new() { ["Id"] = D("analytics-snapshot") }, new()
        {
            ["OrganizationId"] = OrganizationId, ["HospitalId"] = HospitalId, ["FrameworkId"] = frameworkId,
            ["SnapshotDate"] = now.Date, ["ReadinessScore"] = overallScore, ["ComplianceMaturityIndex"] = 28m,
            ["OpenDeficiencies"] = 12, ["CriticalDeficiencies"] = 3, ["CapaOpen"] = 6,
            ["CapaEffective"] = 11, ["CapaIneffective"] = 0, ["EscalationsOpen"] = 4,
            ["SlaBreached"] = 2, ["StageUnlockedCount"] = 1, ["RecurringIssues"] = 3,
            ["EvidencePendingVerification"] = 84, ["TaskBacklog"] = inProgressObjectiveElements + pendingObjectiveElements,
            ["CreatedAt"] = now
        }, report, "analytics snapshot");
        await db.TryUpsertAsync("DashboardProjections", new() { ["Id"] = D("dashboard-projection") }, new()
        {
            ["OrganizationId"] = OrganizationId, ["HospitalId"] = HospitalId, ["FrameworkId"] = frameworkId,
            ["ProjectionSchemaVersion"] = 1, ["DataVersion"] = 1,
            ["OverallComplianceScore"] = overallScore, ["AuditConfidenceScore"] = 61m,
            ["ActiveJourneysCount"] = 1, ["TotalJourneysCount"] = 1,
            ["PendingTasksCount"] = inProgressObjectiveElements + pendingObjectiveElements,
            ["OverdueTasksCount"] = 47, ["OpenAlertsCount"] = 6,
            ["PayloadJson"] = JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                summary = new
                {
                    totalHospitals = 1, onboardedHospitals = 1, totalJourneys = 1, activeJourneys = 1,
                    upcomingExpiries = 0, overdueTasks = 42, completionRate = overallScore,
                    avgComplianceScore = overallScore, frameworksActive = 1
                },
                frameworks = new[]
                {
                    new
                    {
                        frameworkCode = "CP00000001", frameworkName = "NABH Full Accreditation (Hospital)",
                        activeHospitals = 1, journeys = 1, avgScore = overallScore,
                        statusCounts = new Dictionary<string, int> { ["InProgress"] = 1 },
                        upcomingMilestones = Array.Empty<object>(), recentActivity = Array.Empty<object>()
                    }
                },
                hospitals = new[]
                {
                    new
                    {
                        hospitalId = HospitalId.ToString(), name = "AOVIAS Multispeciality Hospital",
                        code = "AOVIAS-HOSP-001", isOnboarded = true, nabhLevel = "Full Accreditation",
                        openTasks = inProgressObjectiveElements + pendingObjectiveElements, overdueTasks = 42,
                        lastUpdated = now.ToString("O"),
                        journeys = new[]
                        {
                            new
                            {
                                journeyId = journeyId.ToString(), frameworkCode = "CP00000001",
                                frameworkName = "NABH Full Accreditation (Hospital)",
                                startDate = now.AddMonths(-8).ToString("O"), expiryDate = now.AddMonths(10).ToString("O"),
                                status = "InProgress", score = overallScore, progressPercent = overallScore
                            }
                        }
                    }
                },
                tasksSummary = new
                {
                    total = totalObjectiveElements,
                    open = inProgressObjectiveElements + pendingObjectiveElements,
                    overdue = 42,
                    dueSoon = 34,
                    byPriority = new Dictionary<string, int>
                    {
                        ["Critical"] = 126, ["High"] = 126, ["Medium"] = 126, ["Low"] = 125
                    },
                    byType = new Dictionary<string, int>
                    {
                        ["Pending"] = pendingObjectiveElements,
                        ["InProgress"] = inProgressObjectiveElements,
                        ["Completed"] = completedObjectiveElements
                    },
                    byFramework = new[]
                    {
                        new
                        {
                            frameworkCode = "CP00000001",
                            open = inProgressObjectiveElements + pendingObjectiveElements,
                            overdue = 42
                        }
                    }
                },
                alerts = Enumerable.Range(0, 6).Select(i => new
                {
                    alertId = D($"dashboard-alert:{i}").ToString(),
                    type = "ComplianceGap",
                    severity = i < 2 ? "High" : "Medium",
                    message = $"{DepartmentData()[i].Name} compliance gap requires owner review.",
                    entityType = "ObjectiveElement",
                    entityId = D($"objective-alert:{i}").ToString(),
                    actor = "Quality and Accreditation Team",
                    createdAt = now.AddDays(-i - 1).ToString("O"),
                    acknowledged = false
                }).ToArray(),
                scoreTrends = new[]
                {
                    new { date = now.AddMonths(-2).ToString("yyyy-MM-dd"), avgScore = 18m, frameworks = new Dictionary<string, decimal> { ["CP00000001"] = 18m } },
                    new { date = now.AddMonths(-1).ToString("yyyy-MM-dd"), avgScore = 22m, frameworks = new Dictionary<string, decimal> { ["CP00000001"] = 22m } },
                    new { date = now.ToString("yyyy-MM-dd"), avgScore = overallScore, frameworks = new Dictionary<string, decimal> { ["CP00000001"] = overallScore } }
                },
                milestones = Array.Empty<object>(),
                recentActivity = Array.Empty<object>(),
                journeyStatusCounts = new Dictionary<string, int> { ["InProgress"] = 1 },
                widgetAggregates = new Dictionary<string, object>()
            }),
            ["IsComplete"] = true, ["IsCorrupted"] = false, ["LastUpdatedAt"] = now, ["CreatedAt"] = now
        }, report, "dashboard projection");
    }

    private static readonly string[] DocumentSubjects =
    [
        "Infection Prevention and Control", "Fire and Life Safety", "Biomedical Waste Management",
        "Medication Management", "Patient Admission", "Patient Discharge", "Hand Hygiene",
        "Operation Theatre Cleaning", "Blood Sample Collection", "Patient Identification",
        "Medication Administration", "Incident Reporting", "Informed Consent", "Clinical Risk Assessment",
        "Internal Quality Audit", "Emergency Triage", "Pain Assessment", "Fall Prevention",
        "Needlestick Injury Management", "High-alert Medication", "Antimicrobial Stewardship",
        "Critical Result Communication", "Crash Cart Readiness", "Patient Transfer and Referral",
        "Medical Record Completion", "Radiation Safety", "Laboratory Quality Control",
        "Nursing Handover", "Vendor Qualification", "Staff Credential Verification",
        "Utility Failure Response", "Information Security and Downtime"
    ];

    private static (int Type, string Name, string Prefix, string Schema) DocumentTypeInfo(int index) =>
        (index % 10) switch
        {
            0 => (0, "Policy", "POL", "policy"),
            1 => (1, "SOP", "SOP", "sop"),
            2 => (7, "Form", "FRM", "form"),
            3 => (6, "Checklist", "CHK", "form"),
            4 => (4, "Protocol", "PRO", "sop"),
            5 => (5, "Procedure", "PRC", "sop"),
            6 => (3, "Guideline", "GDL", "policy"),
            7 => (2, "Manual", "MAN", "policy"),
            8 => (8, "Template", "TMP", "form"),
            _ => (9, "Committee Charter", "COM", "committee-minutes")
        };

    private static string CleanText(string value) =>
        value.Replace("?", "fi", StringComparison.Ordinal).Replace("  ", " ", StringComparison.Ordinal).Trim();

    private static string ObjectiveTitle(Dictionary<string, object?> oe)
    {
        var standard = CleanText(AsString(oe, "StandardName")).TrimEnd('.');
        var code = AsString(oe, "Code");
        var suffix = code.Split('.').LastOrDefault()?.ToLowerInvariant();
        var action = suffix switch
        {
            "a" => "Scope, ownership and requirements are defined",
            "b" => "Approved processes are implemented",
            "c" => "Staff are trained and competent",
            "d" => "Records are maintained and traceable",
            "e" => "Performance is monitored and reviewed",
            "f" => "Corrective improvement is sustained",
            "g" => "Effectiveness is evaluated by leadership",
            _ => "Implementation is demonstrated through current evidence"
        };
        return $"{action}: {standard}";
    }

    private static string EvidenceCategory(int index) => (index % 8) switch
    {
        0 => "Policy and Procedure", 1 => "Audit Report", 2 => "Training Record", 3 => "Meeting Record",
        4 => "Monitoring Data", 5 => "Drill Record", 6 => "Calibration Record", _ => "Photographic Evidence"
    };

    private static string EvidenceTitle(int index, string department, DateTime date)
    {
        var titles = new[]
        {
            "Monthly Hand Hygiene Audit Report", "Fire Drill Attendance and Debrief",
            "Biomedical Waste Segregation Round", "Medication Storage Temperature Review",
            "Patient Identification Compliance Audit", "Staff Safety Training Attendance",
            "Emergency Trolley Readiness Checklist", "Equipment Calibration Certificate Register",
            "Internal Audit Closure Evidence", "Committee Meeting Minutes and Action Log",
            "Clinical Indicator Validation Sheet", "Department Safety Round Photograph"
        };
        return $"{titles[index % titles.Length]} — {department} — {date:MMM yyyy}";
    }

    private static string FindingTitle(int index)
    {
        var titles = new[]
        {
            "Hand hygiene observation records incomplete on night shift",
            "High-alert medicine labels not consistently applied",
            "Operation theatre cleaning checklist missing supervisor sign-off",
            "Critical laboratory result escalation time exceeded",
            "Fire exit inspection record not available for one week",
            "Biomedical waste trolley disinfection log incomplete",
            "Crash cart seal verification not documented",
            "Patient identity check audit below hospital target",
            "Nursing handover tool not used for two sampled transfers",
            "Professional registration renewal tracker not updated",
            "Radiology consent form version inconsistent",
            "Housekeeping chemical dilution record unavailable"
        };
        return titles[index % titles.Length];
    }

    private static string FindingDescription(int index) =>
        $"Internal audit sampling identified a repeatable control gap. The department accepted the finding, nominated an owner and recorded recommendation {index + 1} in the action tracker.";

    private static string CapaTitle(int index)
    {
        var titles = new[]
        {
            "Standardise high-alert medication identification", "Reinforce patient identification at every handoff",
            "Introduce shift-wise hand hygiene review", "Close gaps in critical result escalation",
            "Strengthen operation theatre cleaning verification", "Improve biomedical waste traceability",
            "Restore fire safety inspection discipline", "Automate professional registration reminders",
            "Improve crash cart readiness assurance", "Standardise nursing transfer handover",
            "Correct calibration certificate tracking", "Strengthen specimen labelling controls",
            "Improve adverse event learning feedback", "Update radiology consent control",
            "Reinforce housekeeping chemical dilution checks", "Close overdue document reviews",
            "Improve contractor safety induction"
        };
        return titles[index % titles.Length];
    }

    private static string RootCause(int index) => (index % 4) switch
    {
        0 => "Ownership was understood informally but was not defined in the controlled procedure.",
        1 => "The checklist did not include an independent verification step.",
        2 => "Training coverage did not include all shifts and outsourced personnel.",
        _ => "Monitoring data was collected but exceptions were not escalated at the agreed frequency."
    };

    private static string CorrectiveAction(int index) => (index % 3) switch
    {
        0 => "Revise the procedure, brief staff and verify implementation through focused audit sampling.",
        1 => "Correct affected records, complete department training and document supervisor verification.",
        _ => "Assign a named owner, clear the backlog and submit closure evidence to the Quality Department."
    };

    private static string PreventiveAction(int index) => (index % 3) switch
    {
        0 => "Add the control to the monthly department scorecard and committee review agenda.",
        1 => "Configure advance reminders and escalate missed checks to the department head.",
        _ => "Include the control in induction, annual refresher training and quarterly internal audits."
    };

    private static string RiskTitle(int index)
    {
        var titles = new[]
        {
            "Medication selection error involving look-alike packaging", "Patient fall during unassisted transfer",
            "Failure to communicate a critical diagnostic result", "Infection transmission due to missed hand hygiene",
            "Interruption of oxygen supply", "Fire evacuation route obstruction", "Cybersecurity incident affecting clinical records",
            "Biomedical waste segregation failure", "Delayed response to equipment breakdown",
            "Unavailability of appropriately credentialed clinical staff", "Cold-chain excursion in pharmacy",
            "Specimen misidentification", "Radiation exposure control failure", "Utility power interruption",
            "Breakdown in emergency referral communication"
        };
        return titles[index % titles.Length];
    }

    private static string RiskDescription(int index) =>
        $"Risk assessment {index + 1} includes existing controls, a named mitigation action and quarterly residual-risk review by the responsible department.";

    private static async Task<List<EvidenceArtifact>> CreateEvidenceArtifactsAsync()
    {
        var root = FindPlatformRoot();
        var artifactDirectory = Path.Combine(root, "ComplianceService", "App_Data", "demo-evidence");
        Directory.CreateDirectory(artifactDirectory);
        var artifacts = new List<EvidenceArtifact>();
        var titles = new[]
        {
            "Fire Drill Evacuation Map", "Hand Hygiene Observation Summary", "Training Attendance Summary",
            "Internal Audit Closure Record", "Calibration Certificate Register", "Committee Action Review"
        };
        for (var i = 0; i < titles.Length; i++)
        {
            var svgName = $"hospital-assurance-{i + 1}.svg";
            var svg = $"""
                <svg xmlns="http://www.w3.org/2000/svg" width="1200" height="800" viewBox="0 0 1200 800">
                  <rect width="1200" height="800" fill="#f7fafc"/>
                  <rect x="60" y="60" width="1080" height="680" rx="24" fill="white" stroke="#146c94" stroke-width="4"/>
                  <text x="100" y="145" font-family="Arial" font-size="42" fill="#12344d">AOVIAS Multispeciality Hospital</text>
                  <text x="100" y="210" font-family="Arial" font-size="34" fill="#146c94">{titles[i]}</text>
                  <text x="100" y="285" font-family="Arial" font-size="24" fill="#334e68">Quality assurance record • Reviewed Jul 2026</text>
                  <line x1="100" y1="330" x2="1100" y2="330" stroke="#d9e2ec" stroke-width="3"/>
                  <text x="100" y="400" font-family="Arial" font-size="26" fill="#334e68">Owner: Quality and Patient Safety Department</text>
                  <text x="100" y="460" font-family="Arial" font-size="26" fill="#334e68">Status: Verified</text>
                  <text x="100" y="520" font-family="Arial" font-size="26" fill="#334e68">Review cycle: Monthly / Quarterly as applicable</text>
                  <text x="100" y="620" font-family="Arial" font-size="22" fill="#627d98">No patient-identifiable information is included.</text>
                </svg>
                """;
            var svgBytes = Encoding.UTF8.GetBytes(svg);
            await File.WriteAllBytesAsync(Path.Combine(artifactDirectory, svgName), svgBytes);
            artifacts.Add(new($"local://App_Data/demo-evidence/{svgName}", Convert.ToHexString(SHA256.HashData(svgBytes))));

            var pdfName = $"hospital-assurance-{i + 1}.pdf";
            var pdfBytes = BuildSimplePdf(titles[i],
                "AOVIAS Multispeciality Hospital",
                "Quality and Patient Safety Department",
                "Reviewed and approved assurance record",
                "No patient-identifiable information is included.");
            await File.WriteAllBytesAsync(Path.Combine(artifactDirectory, pdfName), pdfBytes);
            artifacts.Add(new($"local://App_Data/demo-evidence/{pdfName}", Convert.ToHexString(SHA256.HashData(pdfBytes))));
        }
        return artifacts;
    }

    private static string FindPlatformRoot()
    {
        var candidates = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
        foreach (var candidate in candidates)
        {
            var directory = new DirectoryInfo(candidate);
            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "ComplianceService")))
                    return directory.FullName;
                directory = directory.Parent;
            }
        }
        throw new DirectoryNotFoundException("Could not locate the ComplaincePlatform directory.");
    }

    private static byte[] BuildSimplePdf(string title, params string[] lines)
    {
        static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        var text = new StringBuilder($"BT /F1 18 Tf 72 750 Td ({Escape(title)}) Tj /F1 11 Tf");
        foreach (var line in lines) text.Append($" 0 -34 Td ({Escape(line)}) Tj");
        text.Append(" ET");
        var stream = text.ToString();
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream"
        };
        var builder = new StringBuilder("%PDF-1.4\n");
        var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }
        var xref = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) builder.Append($"{offset:0000000000} 00000 n \n");
        builder.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(builder.ToString());
    }

    private static async Task VerifyAsync(Settings settings, TargetContext context, SeedReport report)
    {
        var checks = new (string Db, string Table, string? Predicate, string Label)[]
        {
            ("Identity", "AspNetUsers", "[PlatformId] LIKE 'USR%'", "Directory users"),
            ("Organization", "Departments", $"[OrganizationId]='{OrganizationId}' AND [HospitalId]='{HospitalId}' AND [IsDeleted]=0", "Hospital departments"),
            ("Organization", "Members", $"[OrganizationId]='{OrganizationId}' AND [HospitalId]='{HospitalId}' AND [MembershipStatus]='Active' AND [IsDeleted]=0", "Active members"),
            ("Organization", "OrganizationalUnits", $"[OrganizationId]='{OrganizationId}' AND [HospitalId]='{HospitalId}' AND [IsDeleted]=0", "Locations and units"),
            ("Person", "Persons", "[PlatformId] LIKE 'PER-%'", "People"),
            ("Workforce", "Employments", $"[OrganizationId]='{OrganizationId}' AND [HospitalId]='{HospitalId}'", "Employments"),
            ("Professional", "ProfessionalProfiles", $"[OrganizationId]='{OrganizationId}' AND [HospitalId]='{HospitalId}'", "Professional profiles"),
            ("Professional", "ProfessionalRegistrations", "[RegistrationNumber] LIKE 'D%-%'", "Professional registrations"),
            ("Professional", "TrainingRecords", "[CourseName] IN ('Hand Hygiene','Fire Safety','NABH Awareness','Biomedical Waste Management','Patient Safety','Cardiopulmonary Resuscitation','Medication Safety')", "Training records"),
            ("AccessGovernance", "RoleAssignments", $"[OrganizationId]='{OrganizationId}' AND [HospitalId]='{HospitalId}' AND [IsRevoked]=0", "Access role assignments"),
            ("Governance", "Committees", $"[OrganizationId]='{OrganizationId}' AND [HospitalId]='{HospitalId}'", "Committees"),
            ("Governance", "CommitteeMeetings", "[Agenda] LIKE 'Review % indicators%'", "Committee meetings"),
            ("EnterpriseDirectory", "DirectoryIndex", $"[OrganizationId]='{OrganizationId}' AND [HospitalId]='{HospitalId}' AND [IsActive]=1", "Directory index"),
            ("Compliance", "ObjectiveElementScores", $"[JourneyId] IN (SELECT [Id] FROM [ComplianceJourneys] WHERE [OrganizationId]='{OrganizationId}' AND [HospitalId]='{HospitalId}')", "OE scores"),
            ("Compliance", "ComplianceTasks", $"[HospitalId]='{HospitalId}'", "Compliance tasks"),
            ("Compliance", "Evidences", $"[HospitalId]='{HospitalId}'", "Evidence"),
            ("Compliance", "ObjectiveElementEvidences", $"[EvidenceId] IN (SELECT [Id] FROM [Evidences] WHERE [HospitalId]='{HospitalId}')", "OE evidence links"),
            ("Compliance", "Findings", $"[TenantId]='{OrganizationId}' AND [FindingNumber] LIKE 'FND-2026-%'", "Findings"),
            ("Compliance", "Capas", $"[HospitalId]='{HospitalId}'", "CAPA"),
            ("Compliance", "Risks", $"[HospitalId]='{HospitalId}'", "Risks"),
            ("Compliance", "ScheduledAudits", $"[HospitalId]='{HospitalId}'", "Scheduled audits"),
            ("Compliance", "AuditSessions", $"[TenantId]='{OrganizationId}' AND [HospitalId]='{HospitalId}'", "Audit sessions"),
            ("Compliance", "ControlledDocuments", $"[TenantId]='{OrganizationId}' AND [HospitalId]='{HospitalId}'", "Documents"),
            ("Compliance", "Incidents", $"[TenantId]='{OrganizationId}' AND [HospitalId]='{HospitalId}'", "Incidents"),
            ("Compliance", "ClinicalIndicators", $"[TenantId]='{OrganizationId}'", "Clinical indicators")
        };
        foreach (var group in checks.GroupBy(x => x.Db))
        {
            await using var db = await SchemaDb.OpenAsync(settings.Connection(group.Key));
            foreach (var check in group)
            {
                if (!await db.TableExistsAsync(check.Table))
                {
                    report.Unsupported.Add($"{check.Label}: table {check.Db}.{check.Table} is absent.");
                    continue;
                }
                var count = await db.ScalarAsync<int>($"SELECT COUNT(*) FROM {Q(check.Table)} WHERE {check.Predicate}");
                report.Verification[check.Label] = count;
            }
        }

        var minimums = new Dictionary<string, int>
        {
            ["Directory users"] = 72, ["Hospital departments"] = 16, ["Active members"] = 72,
            ["Locations and units"] = 5, ["People"] = 72, ["Employments"] = 72,
            ["Professional profiles"] = 72, ["Professional registrations"] = 35, ["Training records"] = 504, ["Access role assignments"] = 72,
            ["Committees"] = 6, ["Committee meetings"] = 24, ["Directory index"] = 72, ["OE scores"] = 100,
            ["Compliance tasks"] = 100, ["Evidence"] = 420, ["OE evidence links"] = 420,
            ["Findings"] = 24, ["CAPA"] = 17, ["Risks"] = 15, ["Scheduled audits"] = 16,
            ["Audit sessions"] = 16, ["Documents"] = 194, ["Incidents"] = 18, ["Clinical indicators"] = 5
        };
        foreach (var expected in minimums)
        {
            if (!report.Verification.TryGetValue(expected.Key, out var count) || count < expected.Value)
                report.Errors.Add($"{expected.Key}: expected at least {expected.Value}, found {count}.");
        }
        report.Checks["TargetUserId"] = context.UserId.ToString();
        report.Checks["SeedDetected"] = report.Errors.Count == 0 ? "yes" : "partial";
    }

    private static (string Code, string Name, string Category)[] DepartmentData() =>
    [
        ("ADMIN", "Administration", "Administrative"), ("ER", "Emergency", "Clinical"),
        ("OPD", "Outpatient Services", "Clinical"), ("IPD", "Inpatient Services", "Clinical"),
        ("ICU", "Intensive Care Unit", "Clinical"), ("OT", "Operation Theatre", "Clinical"),
        ("PHARM", "Pharmacy", "Clinical Support"), ("LAB", "Laboratory", "Clinical Support"),
        ("RAD", "Radiology", "Clinical Support"), ("NURS", "Nursing", "Clinical"),
        ("HK", "Housekeeping", "Support"), ("BME", "Biomedical Engineering", "Support"),
        ("MAINT", "Maintenance", "Support"), ("HR", "Human Resources", "Administrative"),
        ("FIN", "Finance and Billing", "Administrative"), ("QUALITY", "Quality and Patient Safety", "Quality")
    ];

    private static string PositionName(int index) => new[]
    {
        "Hospital Administrator", "Emergency Physician", "Consultant Physician", "Resident Medical Officer",
        "Intensivist", "Consultant Surgeon", "Clinical Pharmacist", "Laboratory Technologist",
        "Radiology Technologist", "Nursing Officer", "Housekeeping Supervisor", "Biomedical Engineer",
        "Maintenance Engineer", "Human Resources Executive", "Billing Executive", "Quality Manager"
    }[index];

    private static readonly int[] DepartmentStarts = [0, 4, 9, 17, 23, 29, 35, 39, 44, 48, 60, 64, 66, 68, 70, 71];
    private static readonly int[] DepartmentEnds = [4, 9, 17, 23, 29, 35, 39, 44, 48, 60, 64, 66, 68, 70, 71, 72];

    private static int EmployeeDepartmentIndex(int employeeIndex)
    {
        for (var i = 0; i < DepartmentEnds.Length; i++)
            if (employeeIndex < DepartmentEnds[i]) return i;
        return DepartmentEnds.Length - 1;
    }

    private static int DepartmentHeadIndex(int departmentIndex) => DepartmentStarts[departmentIndex];
    private static bool IsDepartmentHead(int employeeIndex) =>
        DepartmentStarts.Contains(employeeIndex);
    private static DateTime JoiningDateFor(int employeeIndex, DateTime now) =>
        employeeIndex % 9 == 0
            ? now.Date.AddMonths(-(1 + employeeIndex % 6))
            : now.Date.AddYears(-(1 + employeeIndex % 12)).AddMonths(-(employeeIndex % 11));

    private static string EmployeePositionName(int employeeIndex)
    {
        var department = EmployeeDepartmentIndex(employeeIndex);
        if (IsDepartmentHead(employeeIndex))
            return department switch
            {
                0 => "Hospital Administrator",
                9 => "Nursing Superintendent",
                10 => "Housekeeping Supervisor",
                11 => "Biomedical Engineering In-charge",
                12 => "Maintenance In-charge",
                13 => "Human Resources Manager",
                14 => "Finance Manager",
                15 => "Quality and Accreditation Head",
                _ => $"Head - {DepartmentData()[department].Name}"
            };
        return department switch
        {
            0 => employeeIndex % 3 == 0 ? "Human Resources Executive" : employeeIndex % 3 == 1 ? "Billing Executive" : "Reception Executive",
            1 => employeeIndex % 2 == 0 ? "Emergency Medical Officer" : "Emergency Staff Nurse",
            2 => (employeeIndex % 5) switch
            {
                0 => "Consultant Physician", 1 => "Consultant Paediatrician", 2 => "Consultant Gynaecologist",
                3 => "Consultant Orthopaedic Surgeon", _ => "Medical Officer"
            },
            3 => employeeIndex % 2 == 0 ? "Resident Medical Officer" : "IPD Staff Nurse",
            4 => employeeIndex % 2 == 0 ? "Intensivist" : "ICU Staff Nurse",
            5 => employeeIndex % 2 == 0 ? "Consultant Surgeon" : "Operation Theatre Technician",
            6 => "Clinical Pharmacist",
            7 => "Laboratory Technician",
            8 => "Radiology Technician",
            9 => "Staff Nurse",
            10 => "Housekeeping Attendant",
            11 => "Biomedical Engineer",
            12 => employeeIndex % 2 == 0 ? "Electrician" : "Maintenance Technician",
            13 => "Human Resources Executive",
            14 => "Billing Executive",
            15 => employeeIndex % 2 == 0 ? "Quality Executive" : "NABH Coordinator",
            _ => PositionName(department)
        };
    }

    private static string ProfessionalCategory(int employeeIndex)
    {
        var position = EmployeePositionName(employeeIndex);
        if (position.Contains("Nurse", StringComparison.OrdinalIgnoreCase) ||
            position.Contains("Nursing", StringComparison.OrdinalIgnoreCase)) return "NURSE";
        if (position.Contains("Pharmacist", StringComparison.OrdinalIgnoreCase)) return "PHARMACIST";
        if (position.Contains("Radiology", StringComparison.OrdinalIgnoreCase)) return "RADIOGRAPHER";
        if (position.Contains("Technician", StringComparison.OrdinalIgnoreCase) ||
            position.Contains("Technologist", StringComparison.OrdinalIgnoreCase)) return "TECHNICIAN";
        if (position.Contains("Biomedical", StringComparison.OrdinalIgnoreCase)) return "BME";
        if (position.Contains("Quality", StringComparison.OrdinalIgnoreCase) ||
            position.Contains("NABH", StringComparison.OrdinalIgnoreCase)) return "QUALITY";
        if (position.Contains("Physician", StringComparison.OrdinalIgnoreCase) ||
            position.Contains("Medical Officer", StringComparison.OrdinalIgnoreCase) ||
            position.Contains("Surgeon", StringComparison.OrdinalIgnoreCase) ||
            position.Contains("Consultant", StringComparison.OrdinalIgnoreCase) ||
            position.Contains("Intensivist", StringComparison.OrdinalIgnoreCase)) return "DOCTOR";
        return "ADMIN";
    }

    private static string? RegistrationAuthority(string category) => category switch
    {
        "DOCTOR" => "MCI", "NURSE" => "NMC", "PHARMACIST" => "PCI",
        "RADIOGRAPHER" => "PARAMEDICAL", "TECHNICIAN" => "LAB", "BME" => "PARAMEDICAL",
        _ => null
    };

    private static string QualificationFor(string category, string position) => category switch
    {
        "DOCTOR" when position.Contains("Surgeon", StringComparison.OrdinalIgnoreCase) => "MBBS, MS",
        "DOCTOR" when position.Contains("Gynaecologist", StringComparison.OrdinalIgnoreCase) => "MBBS, MD (Obstetrics and Gynaecology)",
        "DOCTOR" when position.Contains("Paediatrician", StringComparison.OrdinalIgnoreCase) => "MBBS, MD (Paediatrics)",
        "DOCTOR" => "MBBS, MD",
        "NURSE" => "BSc Nursing",
        "PHARMACIST" => "Bachelor of Pharmacy",
        "RADIOGRAPHER" => "BSc Medical Radiology and Imaging Technology",
        "TECHNICIAN" => "BSc Medical Laboratory Technology",
        "BME" => "BE Biomedical Engineering",
        "QUALITY" => "Master of Hospital Administration",
        _ => "Bachelor of Hospital Administration"
    };

    private static List<PersonSeed> BuildPeople(Guid? linkedPersonId)
    {
        string[] first = ["Ekta", "Aarav", "Vivaan", "Aditya", "Arjun", "Sai", "Reyansh", "Ayaan", "Krishna", "Ishaan", "Ananya", "Diya", "Myra", "Sara", "Aadhya", "Pari", "Riya", "Meera"];
        string[] last = ["Gupta", "Sharma", "Patel", "Singh", "Kumar", "Nair", "Reddy", "Iyer", "Joshi", "Das", "Mehta", "Kulkarni"];
        string[] cities = ["New Delhi", "Mumbai", "Bengaluru", "Pune", "Jaipur", "Lucknow", "Chandigarh", "Ahmedabad"];
        string[] states = ["Delhi", "Maharashtra", "Karnataka", "Maharashtra", "Rajasthan", "Uttar Pradesh", "Chandigarh", "Gujarat"];
        var result = new List<PersonSeed>();
        for (var i = 0; i < 72; i++)
        {
            var firstName = first[i % first.Length];
            var lastName = last[(i % last.Length + i / first.Length * 3) % last.Length];
            var email = i == 0 ? DefaultEmail : $"{firstName}.{lastName}.{i + 1}@aoviasmultispeciality.in".ToLowerInvariant();
            result.Add(new(i + 1, i == 0 && linkedPersonId.HasValue ? linkedPersonId.Value : D($"person:{i + 1}"),
                firstName, lastName, $"{firstName} {lastName}", email, cities[i % cities.Length], states[i % states.Length]));
        }
        return result;
    }

    internal static Guid D(string name)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"AOVIAS-DEMO-SEEDER::{name}"))[..16];
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }

    private static Guid AsGuid(Dictionary<string, object?> row, string name) =>
        row.TryGetValue(name, out var value) && value is Guid guid ? guid : Guid.Parse(Convert.ToString(value)!);
    private static Guid? AsNullableGuid(Dictionary<string, object?> row, string name) =>
        !row.TryGetValue(name, out var value) || value is null or DBNull ? null : value is Guid guid ? guid : Guid.Parse(Convert.ToString(value)!);
    private static string AsString(Dictionary<string, object?> row, string name) =>
        row.TryGetValue(name, out var value) ? Convert.ToString(value) ?? string.Empty : string.Empty;
    internal static string Q(string identifier) => $"[{identifier.Replace("]", "]]")}]";

    private static async Task WriteReportAsync(SeedReport report)
    {
        report.CompletedAtUtc = DateTime.UtcNow;
        var path = Path.Combine(AppContext.BaseDirectory, "demo-seed-manifest.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report, JsonOptions));
        Console.WriteLine($"Manifest: {path}");
    }

    private static void PrintReport(SeedReport report)
    {
        Console.WriteLine($"{report.Command} completed. Inserted: {report.Inserted}, updated: {report.Updated}, unchanged: {report.Unchanged}.");
        foreach (var check in report.Verification) Console.WriteLine($"  {check.Key}: {check.Value}");
        foreach (var item in report.Unsupported.Distinct()) Console.WriteLine($"  Unsupported: {item}");
        foreach (var error in report.Errors) Console.Error.WriteLine($"  Error: {error}");
    }

    private sealed record Settings(string Server, string Email, IReadOnlyDictionary<string, string> Connections)
    {
        public static Settings FromEnvironment()
        {
            var server = Environment.GetEnvironmentVariable("AOVIAS_SQL_SERVER") ?? @"localhost\SQLEXPRESS";
            var email = Environment.GetEnvironmentVariable("AOVIAS_SEED_EMAIL") ?? DefaultEmail;
            var connections = DatabaseNames.ToDictionary(
                db => db,
                db => Environment.GetEnvironmentVariable($"AOVIAS_{db.ToUpperInvariant()}_CONNECTION")
                    ?? $"Server={server};Database={db};Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Application Name=AOVIAS DemoDataSeeder");
            connections["EnterpriseDirectory"] =
                Environment.GetEnvironmentVariable("AOVIAS_ENTERPRISEDIRECTORY_CONNECTION")
                ?? @"Server=(localdb)\mssqllocaldb;Database=PramanEnterpriseDirectory;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Application Name=AOVIAS DemoDataSeeder";
            return new(server, email, connections);
        }
        public string Connection(string database) => Connections[database];
    }

    private sealed record TargetContext(Guid UserId, Guid? OrganizationMemberId, Guid? IdentityPersonId);
    private sealed record PersonSeed(int Ordinal, Guid Id, string FirstName, string LastName, string Name, string Email, string City, string State);
    private sealed record WorkforceSeed(List<Guid> EmploymentIds, List<Guid> PositionIds, Dictionary<string, Guid> DepartmentIds);
    private sealed record EvidenceArtifact(string Url, string Hash);
}

internal sealed class SeedReport(string command, string email, Guid organizationId, Guid hospitalId)
{
    public string Command { get; } = command;
    public string Email { get; } = email;
    public Guid OrganizationId { get; } = organizationId;
    public Guid HospitalId { get; } = hospitalId;
    public DateTime StartedAtUtc { get; } = DateTime.UtcNow;
    public DateTime CompletedAtUtc { get; set; }
    public int Inserted { get; set; }
    public int Updated { get; set; }
    public int Unchanged { get; set; }
    public Dictionary<string, string> Context { get; } = [];
    public Dictionary<string, string> Databases { get; } = [];
    public Dictionary<string, string> Checks { get; } = [];
    public Dictionary<string, int> Verification { get; } = [];
    public List<string> Unsupported { get; } = [];
    public List<string> Errors { get; } = [];
}

internal sealed class SchemaDb : IAsyncDisposable
{
    private readonly SqlConnection _connection;
    private SqlTransaction? _transaction;
    private readonly Dictionary<string, Dictionary<string, ColumnInfo>> _columnCache = new(StringComparer.OrdinalIgnoreCase);

    private SchemaDb(SqlConnection connection) => _connection = connection;

    public static async Task<SchemaDb> OpenAsync(string connectionString)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        return new(connection);
    }

    public async Task<SqlTransaction> BeginAsync()
    {
        _transaction = (SqlTransaction)await _connection.BeginTransactionAsync();
        return _transaction;
    }

    public async Task<List<string>> GetTableNamesAsync()
    {
        var rows = await QueryAsync("SELECT [name] FROM sys.tables ORDER BY [name]");
        return rows.Select(x => Convert.ToString(x["name"])!).ToList();
    }

    public async Task<string> FirstExistingTableAsync(params string[] candidates)
    {
        foreach (var table in candidates)
            if (await TableExistsAsync(table)) return table;
        throw new InvalidOperationException($"None of the expected tables exist: {string.Join(", ", candidates)}.");
    }

    public async Task<bool> TableExistsAsync(string table)
    {
        using var command = Command("SELECT CASE WHEN OBJECT_ID(@name, 'U') IS NULL THEN 0 ELSE 1 END");
        command.Parameters.AddWithValue("@name", table);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    public async Task<bool> ExistsAsync(string table, Dictionary<string, object?> keys)
    {
        if (!await TableExistsAsync(table)) return false;
        var (where, parameters) = BuildWhere(keys, "k");
        using var command = Command($"SELECT CASE WHEN EXISTS(SELECT 1 FROM {SeederProgram.Q(table)} WHERE {where}) THEN 1 ELSE 0 END");
        command.Parameters.AddRange(parameters);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
    }

    public async Task<Guid> SelectGuidAsync(string table, string column, Dictionary<string, object?> keys)
    {
        var (where, parameters) = BuildWhere(keys, "s");
        using var command = Command($"SELECT TOP (1) {SeederProgram.Q(column)} FROM {SeederProgram.Q(table)} WHERE {where}");
        command.Parameters.AddRange(parameters);
        var result = await command.ExecuteScalarAsync();
        if (result is null or DBNull) throw new InvalidOperationException($"{table} lookup did not return {column}.");
        return result is Guid guid ? guid : Guid.Parse(Convert.ToString(result)!);
    }

    public async Task UpsertAsync(string table, Dictionary<string, object?> keys, Dictionary<string, object?> values, SeedReport report)
    {
        if (!await TableExistsAsync(table)) throw new InvalidOperationException($"Required table '{table}' does not exist.");
        var columns = await ColumnsAsync(table);
        var usableKeys = keys.Where(x => columns.ContainsKey(x.Key)).ToDictionary();
        if (usableKeys.Count != keys.Count || usableKeys.Count == 0)
            throw new InvalidOperationException($"{table}: one or more key columns are absent.");
        var usableValues = values.Where(x => columns.TryGetValue(x.Key, out var c) && !c.IsComputed && !c.IsIdentity && !c.IsRowVersion)
            .ToDictionary(x => x.Key, x => x.Value);
        await ExecuteUpsertAsync(table, usableKeys, usableValues, report);
    }

    public async Task TryUpsertAsync(string table, Dictionary<string, object?> keys, Dictionary<string, object?> values, SeedReport report, string feature)
    {
        try { await UpsertAsync(table, keys, values, report); }
        catch (Exception ex)
        {
            report.Unsupported.Add($"{feature}: {ex.Message}");
        }
    }

    private async Task ExecuteUpsertAsync(string table, Dictionary<string, object?> keys, Dictionary<string, object?> values, SeedReport report)
    {
        var exists = await ExistsAsync(table, keys);
        var combined = keys.Concat(values.Where(v => !keys.ContainsKey(v.Key))).ToDictionary(x => x.Key, x => x.Value);
        if (!exists)
        {
            var names = combined.Keys.ToList();
            using var insert = Command($"INSERT INTO {SeederProgram.Q(table)} ({string.Join(",", names.Select(SeederProgram.Q))}) VALUES ({string.Join(",", names.Select((_, i) => $"@p{i}"))})");
            AddParameters(insert, combined.Values);
            await insert.ExecuteNonQueryAsync();
            report.Inserted++;
            return;
        }
        if (values.Count == 0) { report.Unchanged++; return; }
        var (where, keyParams) = BuildWhere(keys, "k");
        var mutable = values
            .Where(v => !keys.ContainsKey(v.Key) && !v.Key.Equals("Id", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (mutable.Count == 0) { report.Unchanged++; return; }
        using var update = Command($"UPDATE {SeederProgram.Q(table)} SET {string.Join(",", mutable.Select((v, i) => $"{SeederProgram.Q(v.Key)}=@v{i}"))} WHERE {where}");
        for (var i = 0; i < mutable.Count; i++) update.Parameters.Add(new SqlParameter($"@v{i}", mutable[i].Value ?? DBNull.Value));
        update.Parameters.AddRange(keyParams);
        report.Updated += await update.ExecuteNonQueryAsync();
    }

    private async Task<Dictionary<string, ColumnInfo>> ColumnsAsync(string table)
    {
        if (_columnCache.TryGetValue(table, out var cached)) return cached;
        using var command = Command("""
            SELECT c.[name], c.[is_nullable], c.[is_identity], c.[is_computed], t.[name] AS [type_name],
                   CASE WHEN dc.[object_id] IS NULL THEN 0 ELSE 1 END AS [has_default]
            FROM sys.columns c
            JOIN sys.types t ON c.user_type_id=t.user_type_id
            LEFT JOIN sys.default_constraints dc ON c.default_object_id=dc.object_id
            WHERE c.object_id=OBJECT_ID(@table)
            """);
        command.Parameters.AddWithValue("@table", table);
        using var reader = await command.ExecuteReaderAsync();
        var result = new Dictionary<string, ColumnInfo>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync())
        {
            var type = reader.GetString(4);
            result[reader.GetString(0)] = new(reader.GetBoolean(1), reader.GetBoolean(2), reader.GetBoolean(3), type is "timestamp" or "rowversion", reader.GetInt32(5) == 1);
        }
        _columnCache[table] = result;
        return result;
    }

    public async Task<Dictionary<string, object?>?> QuerySingleAsync(string sql, params SqlParameter[] parameters) =>
        (await QueryAsync(sql, false, parameters)).FirstOrDefault();

    public async Task<Dictionary<string, object?>?> QuerySingleAsync(string sql, SqlParameter p1, SqlParameter p2, SqlParameter p3, bool tolerateMissingTable) =>
        (await QueryAsync(sql, tolerateMissingTable, p1, p2, p3)).FirstOrDefault();

    public async Task<List<Dictionary<string, object?>>> QueryAsync(string sql, bool tolerateMissingTable = false, params SqlParameter[] parameters)
    {
        try
        {
            using var command = Command(sql);
            command.Parameters.AddRange(parameters);
            using var reader = await command.ExecuteReaderAsync();
            var rows = new List<Dictionary<string, object?>>();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++) row[reader.GetName(i)] = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                rows.Add(row);
            }
            return rows;
        }
        catch (SqlException ex) when (tolerateMissingTable && ex.Number == 208) { return []; }
    }

    public async Task<T> ScalarAsync<T>(string sql)
    {
        using var command = Command(sql);
        var value = await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Scalar query returned null.");
        return (T)Convert.ChangeType(value, typeof(T))!;
    }

    private SqlCommand Command(string sql) => new(sql, _connection, _transaction);

    private static (string Sql, SqlParameter[] Parameters) BuildWhere(Dictionary<string, object?> values, string prefix)
    {
        var parameters = values.Select((x, i) => new SqlParameter($"@{prefix}{i}", x.Value ?? DBNull.Value)).ToArray();
        var clauses = values.Select((x, i) => x.Value is null ? $"{SeederProgram.Q(x.Key)} IS NULL" : $"{SeederProgram.Q(x.Key)}=@{prefix}{i}");
        return (string.Join(" AND ", clauses), parameters);
    }

    private static void AddParameters(SqlCommand command, IEnumerable<object?> values)
    {
        var index = 0;
        foreach (var value in values) command.Parameters.Add(new SqlParameter($"@p{index++}", value ?? DBNull.Value));
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null) await _transaction.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private sealed record ColumnInfo(bool IsNullable, bool IsIdentity, bool IsComputed, bool IsRowVersion, bool HasDefault);
}
