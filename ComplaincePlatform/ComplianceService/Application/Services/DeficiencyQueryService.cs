using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

/// <summary>
/// Read-only projection service for ComplianceDeficiency data.
/// All queries bypass the global tenant filter (internal use only).
/// </summary>
public class DeficiencyQueryService : IDeficiencyQueryService
{
    private readonly ComplianceDbContext _db;

    public DeficiencyQueryService(ComplianceDbContext db)
    {
        _db = db;
    }

    public async Task<DeficiencyListDto> GetListAsync(
        Guid hospitalId,
        Guid frameworkId,
        string? status,
        string? severity,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.ComplianceDeficiencies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(d => d.HospitalId == hospitalId && d.FrameworkId == frameworkId);

        if (!string.IsNullOrWhiteSpace(status)
         && Enum.TryParse<DeficiencyStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            query = query.Where(d => d.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(severity)
         && Enum.TryParse<DeficiencySeverity>(severity, ignoreCase: true, out var parsedSeverity))
        {
            query = query.Where(d => d.Severity == parsedSeverity);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(d => (int)d.Severity)
            .ThenByDescending(d => d.DetectedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => MapToDto(d))
            .ToListAsync(ct);

        return new DeficiencyListDto
        {
            Items      = items,
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize,
        };
    }

    public async Task<DeficiencyDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var d = await _db.ComplianceDeficiencies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return d == null ? null : MapToDto(d);
    }

    public async Task<DeficiencySummaryDto> GetSummaryAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken ct = default)
    {
        var counts = await _db.ComplianceDeficiencies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(d => d.HospitalId == hospitalId && d.FrameworkId == frameworkId)
            .GroupBy(d => new { Severity = (int)d.Severity, IsClosed = d.Status == DeficiencyStatus.Closed })
            .Select(g => new { g.Key.Severity, g.Key.IsClosed, Count = g.Count() })
            .ToListAsync(ct);

        var summary = new DeficiencySummaryDto();

        foreach (var row in counts)
        {
            if (!row.IsClosed)
            {
                summary.OpenCount += row.Count;
            }

            switch ((DeficiencySeverity)row.Severity)
            {
                case DeficiencySeverity.Critical: summary.Critical += row.Count; break;
                case DeficiencySeverity.Major:    summary.Major    += row.Count; break;
                case DeficiencySeverity.Moderate: summary.Moderate += row.Count; break;
                case DeficiencySeverity.Minor:    summary.Minor    += row.Count; break;
            }
            summary.Total += row.Count;
        }

        return summary;
    }

    public async Task<IReadOnlyList<DeficiencyByStageDto>> GetByStageAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken ct = default)
    {
        var rows = await _db.ComplianceDeficiencies
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(d => d.HospitalId == hospitalId
                     && d.FrameworkId == frameworkId
                     && d.Status != DeficiencyStatus.Closed)
            .GroupBy(d => new { Stage = (int)d.MaturityLevel, Severity = (int)d.Severity })
            .Select(g => new { g.Key.Stage, g.Key.Severity, Count = g.Count() })
            .ToListAsync(ct);

        var stageMap = new Dictionary<int, DeficiencyByStageDto>();

        foreach (var row in rows)
        {
            if (!stageMap.TryGetValue(row.Stage, out var dto))
            {
                var classification = (ObjectiveElementClassification)row.Stage;
                dto = new DeficiencyByStageDto
                {
                    Stage = classification.ToString(),
                    Label = classification switch
                    {
                        ObjectiveElementClassification.Core       => "Core",
                        ObjectiveElementClassification.Commitment => "Commitment",
                        ObjectiveElementClassification.Achievement => "Achievement",
                        ObjectiveElementClassification.Excellence => "Excellence",
                        _ => classification.ToString()
                    }
                };
                stageMap[row.Stage] = dto;
            }

            switch ((DeficiencySeverity)row.Severity)
            {
                case DeficiencySeverity.Critical: dto.Critical += row.Count; break;
                case DeficiencySeverity.Major:    dto.Major    += row.Count; break;
                case DeficiencySeverity.Moderate: dto.Moderate += row.Count; break;
                case DeficiencySeverity.Minor:    dto.Minor    += row.Count; break;
            }
            dto.Total += row.Count;
        }

        return stageMap.Values
            .OrderBy(d => Enum.TryParse<ObjectiveElementClassification>(d.Stage, out var lvl) ? (int)lvl : 99)
            .ToList();
    }

    private static DeficiencyDto MapToDto(ComplianceDeficiency d) => new()
    {
        Id                    = d.Id,
        OrganizationId        = d.OrganizationId,
        HospitalId            = d.HospitalId,
        FrameworkId           = d.FrameworkId,
        ObjectiveElementId    = d.ObjectiveElementId,
        ComplianceObligationId = d.ComplianceObligationId,
        DeficiencyCode        = d.DeficiencyCode,
        Title                 = d.Title,
        Description           = d.Description,
        DeficiencyType        = d.DeficiencyType.ToString(),
        Severity              = d.Severity.ToString(),
        Status                = d.Status.ToString(),
        MaturityLevel         = d.MaturityLevel.ToString(),
        RiskCategory          = d.RiskCategory,
        AssignedTo            = d.AssignedTo,
        DueDate               = d.DueDate,
        Source                = d.Source,
        DetectedDate          = d.DetectedDate,
        ClosedDate            = d.ClosedDate,
        LastEvaluatedDate     = d.LastEvaluatedDate,
        CreatedAt             = d.CreatedAt,
        UpdatedAt             = d.UpdatedAt,
    };
}
