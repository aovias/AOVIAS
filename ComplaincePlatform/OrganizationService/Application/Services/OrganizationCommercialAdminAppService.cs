using Microsoft.EntityFrameworkCore;
using OrganizationService.Domain;
using OrganizationService.Infrastructure.Persistence;
using Shared.Infrastructure.Domain;

namespace OrganizationService.Application.Services;

public sealed class OrganizationAdminListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PlatformId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int HospitalCount { get; set; }
    public int MemberCount { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public sealed class OrganizationCommercialAdminAppService
{
    private readonly OrganizationDbContext _db;
    private readonly ILogger<OrganizationCommercialAdminAppService> _logger;

    public OrganizationCommercialAdminAppService(
        OrganizationDbContext db,
        ILogger<OrganizationCommercialAdminAppService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<OrganizationAdminListItemDto> Items, int Total)> SearchAsync(
        string? query,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = _db.Organizations.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            q = q.Where(o => o.Status == status);
        }
        else
        {
            q = q.Where(o => o.Status != OrganizationStatuses.Deleted);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(o =>
                o.Name.Contains(term) ||
                (o.PlatformId != null && o.PlatformId.Contains(term)) ||
                (o.OrganizationCode != null && o.OrganizationCode.Contains(term)));
        }

        var total = await q.CountAsync(cancellationToken);
        var pageItems = await q.OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrganizationAdminListItemDto
            {
                Id = o.Id,
                Name = o.Name,
                PlatformId = o.PlatformId,
                Status = o.Status,
                Type = o.Type,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var ids = pageItems.Select(x => x.Id).ToList();
        var hospitalCounts = await _db.Hospitals.AsNoTracking()
            .Where(h => ids.Contains(h.OrganizationId))
            .GroupBy(h => h.OrganizationId)
            .Select(g => new { OrgId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OrgId, x => x.Count, cancellationToken);
        var memberCounts = await _db.Members.AsNoTracking()
            .Where(m => ids.Contains(m.OrganizationId) && !m.IsDeleted)
            .GroupBy(m => m.OrganizationId)
            .Select(g => new { OrgId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OrgId, x => x.Count, cancellationToken);

        foreach (var item in pageItems)
        {
            item.HospitalCount = hospitalCounts.GetValueOrDefault(item.Id);
            item.MemberCount = memberCounts.GetValueOrDefault(item.Id);
        }

        return (pageItems, total);
    }

    public async Task<Organization?> GetAsync(Guid organizationId, CancellationToken cancellationToken = default)
        => await _db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken);

    public async Task<Organization> SetStatusAsync(
        Guid organizationId,
        string targetStatus,
        CancellationToken cancellationToken = default)
    {
        if (!OrganizationStatuses.All.Contains(targetStatus))
        {
            throw new InvalidOperationException($"Unknown organization status '{targetStatus}'.");
        }

        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken)
                  ?? throw new InvalidOperationException("Organization not found.");

        var from = org.Status;
        ValidateTransition(from, targetStatus);
        org.Status = targetStatus;
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Organization {Org} status {From} -> {To}", organizationId, from, targetStatus);
        return org;
    }

    public async Task PermanentDeleteAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, cancellationToken)
                  ?? throw new InvalidOperationException("Organization not found.");
        if (!string.Equals(org.Status, OrganizationStatuses.Deleted, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(org.Status, OrganizationStatuses.Archived, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Permanent delete requires Archived or soft-Deleted status.");
        }

        _db.Organizations.Remove(org);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogWarning("Organization {Org} permanently deleted", organizationId);
    }

    private static void ValidateTransition(string from, string to)
    {
        from = string.IsNullOrWhiteSpace(from) ? OrganizationStatuses.Active : from;
        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var ok = false;
        if (string.Equals(to, OrganizationStatuses.Active, StringComparison.OrdinalIgnoreCase)
            && (Eq(from, OrganizationStatuses.Suspended) || Eq(from, OrganizationStatuses.Archived) || Eq(from, OrganizationStatuses.Deleted)))
        {
            ok = true;
        }
        else if (string.Equals(to, OrganizationStatuses.Suspended, StringComparison.OrdinalIgnoreCase)
                 && Eq(from, OrganizationStatuses.Active))
        {
            ok = true;
        }
        else if (string.Equals(to, OrganizationStatuses.Archived, StringComparison.OrdinalIgnoreCase)
                 && (Eq(from, OrganizationStatuses.Active) || Eq(from, OrganizationStatuses.Suspended)))
        {
            ok = true;
        }
        else if (string.Equals(to, OrganizationStatuses.Deleted, StringComparison.OrdinalIgnoreCase)
                 && (Eq(from, OrganizationStatuses.Active) || Eq(from, OrganizationStatuses.Suspended) || Eq(from, OrganizationStatuses.Archived)))
        {
            ok = true;
        }

        if (!ok)
        {
            throw new InvalidOperationException($"Cannot transition organization from '{from}' to '{to}'.");
        }

        static bool Eq(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
