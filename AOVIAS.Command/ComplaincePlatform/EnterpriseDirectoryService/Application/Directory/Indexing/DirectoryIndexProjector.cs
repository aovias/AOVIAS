using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using EnterpriseDirectoryService.Application.Directory.Utilities;
using EnterpriseDirectoryService.Application.Contracts;
using EnterpriseDirectoryService.Application.Directory.Configuration;
using EnterpriseDirectoryService.Application.Directory.Platform;
using EnterpriseDirectoryService.Infrastructure.Clients;
using EnterpriseDirectoryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseDirectoryService.Application.Directory.Indexing;

public sealed class DirectoryIndexProjector(
    EnterpriseDirectoryDbContext db,
    IPersonSourceClient personClient,
    IWorkforceSourceClient workforceClient,
    IProfessionalSourceClient professionalClient,
    IDirectoryOutboxWriter outbox,
    ILogger<DirectoryIndexProjector> logger) : IDirectoryIndexProjector
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<int> RebuildOrganizationAsync(Guid organizationId, string correlationId, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var persons = await personClient.ListPersonsAsync(organizationId, ct);
        var employments = await workforceClient.ListEmploymentsAsync(organizationId, ct);
        var profiles = await professionalClient.ListProfilesAsync(organizationId, ct);
        var profileByPerson = profiles.ToDictionary(p => p.PersonId);

        var existing = await db.DirectoryIndexes.Where(x => x.OrganizationId == organizationId).ToListAsync(ct);
        db.DirectoryIndexes.RemoveRange(existing);

        var rows = new List<Domain.ReadModels.DirectoryIndex>();
        foreach (var employment in employments)
        {
            var person = persons.FirstOrDefault(p => p.Id == employment.PersonId);
            profileByPerson.TryGetValue(employment.PersonId, out var profile);
            rows.Add(BuildIndexRow(person, employment, profile));
        }

        foreach (var person in persons.Where(p => employments.All(e => e.PersonId != p.Id)))
        {
            profileByPerson.TryGetValue(person.Id, out var profile);
            rows.Add(BuildIndexRow(person, null, profile));
        }

        db.DirectoryIndexes.AddRange(rows);
        await RebuildFacetsAsync(organizationId, rows, ct);
        db.DirectorySnapshots.Add(new Domain.ReadModels.DirectorySnapshot
        {
            Id = Guid.NewGuid(),
            Version = (await db.DirectorySnapshots.MaxAsync(s => (int?)s.Version, ct) ?? 0) + 1,
            RowCount = rows.Count,
            AsOfUtc = DateTime.UtcNow,
            CorrelationId = correlationId,
            DurationMs = sw.ElapsedMilliseconds
        });
        await db.SaveChangesAsync(ct);
        await outbox.EnqueueAsync("SearchIndexRebuilt", organizationId, new { organizationId, rowCount = rows.Count }, ct);
        logger.LogInformation("Rebuilt directory index for org {OrgId}: {Count} rows in {Ms}ms", organizationId, rows.Count, sw.ElapsedMilliseconds);
        return rows.Count;
    }

    public async Task HandlePersonUpdatedAsync(Guid personId, string payload, CancellationToken ct = default)
    {
        var orgId = TryReadOrgId(payload);
        if (orgId == Guid.Empty) return;
        var person = await personClient.GetPersonAsync(personId, ct);
        if (person is null) return;
        var employments = (await workforceClient.ListEmploymentsAsync(orgId, ct)).Where(e => e.PersonId == personId).ToList();
        var profile = (await professionalClient.ListProfilesAsync(orgId, ct)).FirstOrDefault(p => p.PersonId == personId);
        await UpsertRowsAsync(person, employments, profile, ct);
    }

    public async Task HandleEmploymentEventAsync(string eventType, Guid employmentId, string payload, CancellationToken ct = default)
    {
        if (eventType.Equals("EmploymentTerminated", StringComparison.OrdinalIgnoreCase))
        {
            var row = await db.DirectoryIndexes.FirstOrDefaultAsync(x => x.EmploymentId == employmentId, ct);
            if (row != null) { row.IsActive = false; await db.SaveChangesAsync(ct); }
            return;
        }
        var orgId = TryReadOrgId(payload);
        if (orgId == Guid.Empty) return;
        var employment = (await workforceClient.ListEmploymentsAsync(orgId, ct)).FirstOrDefault(e => e.Id == employmentId);
        if (employment is null) return;
        var person = await personClient.GetPersonAsync(employment.PersonId, ct);
        var profile = (await professionalClient.ListProfilesAsync(orgId, ct)).FirstOrDefault(p => p.PersonId == employment.PersonId);
        await UpsertRowsAsync(person, [employment], profile, ct);
    }

    public async Task HandleProfessionalEventAsync(Guid profileId, string payload, CancellationToken ct = default)
    {
        var orgId = TryReadOrgId(payload);
        if (orgId == Guid.Empty) return;
        var profile = (await professionalClient.ListProfilesAsync(orgId, ct)).FirstOrDefault(p => p.Id == profileId);
        if (profile is null) return;
        var person = await personClient.GetPersonAsync(profile.PersonId, ct);
        var employments = (await workforceClient.ListEmploymentsAsync(orgId, ct)).Where(e => e.PersonId == profile.PersonId).ToList();
        await UpsertRowsAsync(person, employments, profile, ct);
    }

    private async Task UpsertRowsAsync(SourcePersonDto? person, IReadOnlyList<SourceEmploymentDto> employments, SourceProfessionalProfileDto? profile, CancellationToken ct)
    {
        if (person is null) return;
        if (employments.Count == 0)
        {
            await UpsertSingleRowAsync(person, null, profile, ct);
            return;
        }
        foreach (var employment in employments)
            await UpsertSingleRowAsync(person, employment, profile, ct);
    }

    private async Task UpsertSingleRowAsync(SourcePersonDto person, SourceEmploymentDto? employment, SourceProfessionalProfileDto? profile, CancellationToken ct)
    {
        var row = BuildIndexRow(person, employment, profile);
        var existing = await db.DirectoryIndexes.FirstOrDefaultAsync(x =>
            x.PersonId == row.PersonId && x.EmploymentId == row.EmploymentId, ct);
        if (existing is null) db.DirectoryIndexes.Add(row);
        else
        {
            existing.DisplayName = row.DisplayName;
            existing.SearchBlob = row.SearchBlob;
            existing.PhoneticKey = row.PhoneticKey;
            existing.LastIndexedUtc = row.LastIndexedUtc;
            existing.IsActive = row.IsActive;
            existing.DepartmentName = row.DepartmentName;
            existing.HospitalName = row.HospitalName;
            existing.SkillsJson = row.SkillsJson;
            existing.CertificationsJson = row.CertificationsJson;
        }
        await db.SaveChangesAsync(ct);
        await outbox.EnqueueAsync("DirectoryIndexUpdated", row.PersonId, new { row.PersonId, row.EmploymentId }, ct);
    }

    private static Domain.ReadModels.DirectoryIndex BuildIndexRow(SourcePersonDto? person, SourceEmploymentDto? employment, SourceProfessionalProfileDto? profile)
    {
        var displayName = person?.PreferredName ?? person?.LegalName ?? "Unknown";
        var skills = profile?.Skills.Select(s => s.SkillName).ToList() ?? [];
        var certs = profile?.Certifications.Select(c => c.Name).Concat(profile?.Licenses.Select(l => l.Name) ?? []).ToList() ?? [];
        var blobParts = new List<string?>
        {
            displayName, person?.LegalName, person?.PreferredName, person?.PlatformId,
            employment?.EmployeeNumber, profile?.ProfessionalNumber,
            person?.Email, person?.Phone,
            employment?.DepartmentName, employment?.HospitalName, employment?.Designation,
            employment?.PositionTitle, employment?.ManagerName, employment?.EmploymentStatus,
            profile?.Category
        };
        blobParts.AddRange(skills);
        blobParts.AddRange(certs);
        var searchBlob = string.Join(' ', blobParts.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!.Trim().ToLowerInvariant()));

        return new Domain.ReadModels.DirectoryIndex
        {
            Id = Guid.NewGuid(),
            PersonId = person?.Id ?? employment?.PersonId ?? Guid.Empty,
            EmploymentId = employment?.Id,
            OrganizationId = employment?.OrganizationId ?? person?.OrganizationId ?? profile?.OrganizationId ?? Guid.Empty,
            HospitalId = employment?.HospitalId,
            DepartmentId = employment?.DepartmentId,
            DisplayName = displayName,
            PreferredName = person?.PreferredName,
            PlatformId = person?.PlatformId,
            EmployeeNumber = employment?.EmployeeNumber,
            ProfessionalNumber = profile?.ProfessionalNumber,
            EmailNormalized = Normalize(person?.Email),
            PhoneNormalized = Normalize(person?.Phone),
            DepartmentName = employment?.DepartmentName,
            HospitalName = employment?.HospitalName,
            Designation = employment?.Designation,
            PositionTitle = employment?.PositionTitle,
            EmploymentStatus = employment?.EmploymentStatus,
            EmploymentType = employment?.EmploymentType,
            ProfessionalCategory = profile?.Category,
            ManagerName = employment?.ManagerName,
            SearchBlob = searchBlob,
            PhoneticKey = PhoneticHelper.ComputeKey(displayName),
            SkillsJson = JsonSerializer.Serialize(skills),
            CertificationsJson = JsonSerializer.Serialize(certs),
            LastIndexedUtc = DateTime.UtcNow,
            IsActive = employment?.IsActive ?? true
        };
    }

    private async Task RebuildFacetsAsync(Guid organizationId, List<Domain.ReadModels.DirectoryIndex> rows, CancellationToken ct)
    {
        var old = await db.DirectoryFacets.Where(f => f.OrganizationId == organizationId).ToListAsync(ct);
        db.DirectoryFacets.RemoveRange(old);
        var asOf = DateTime.UtcNow;
        void AddFacet(string type, IEnumerable<string?> values) =>
            db.DirectoryFacets.AddRange(values.Where(v => !string.IsNullOrWhiteSpace(v))
                .GroupBy(v => v!)
                .Select(g => new Domain.ReadModels.DirectoryFacet
                {
                    Id = Guid.NewGuid(), OrganizationId = organizationId, FacetType = type,
                    FacetValue = g.Key, Count = g.Count(), AsOfUtc = asOf
                }));
        AddFacet("department", rows.Select(r => r.DepartmentName));
        AddFacet("hospital", rows.Select(r => r.HospitalName));
        AddFacet("status", rows.Select(r => r.EmploymentStatus));
        AddFacet("category", rows.Select(r => r.ProfessionalCategory));
    }

    private static Guid TryReadOrgId(string payload)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("organizationId", out var el) && Guid.TryParse(el.GetString(), out var id))
                return id;
        }
        catch { /* ignore */ }
        return Guid.Empty;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
}
