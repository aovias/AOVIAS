using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

public sealed class RiskRegisterAppService : IRiskRegisterAppService
{
    private readonly ComplianceDbContext _db;

    public RiskRegisterAppService(ComplianceDbContext db) => _db = db;

    public async Task<RiskListDto> ListAsync(
        Guid organizationId,
        Guid? hospitalId,
        string? status,
        string? category,
        string? search,
        Guid? ownerUserId,
        Guid? departmentId,
        bool? reviewDueOnly,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Risks.AsNoTracking().Where(r => r.TenantId == organizationId);
        if (hospitalId.HasValue && hospitalId.Value != Guid.Empty)
            query = query.Where(r => r.HospitalId == hospitalId.Value);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RiskStatus>(status, true, out var st))
            query = query.Where(r => r.Status == st);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(r => r.Category != null && r.Category == category);

        if (ownerUserId.HasValue)
            query = query.Where(r => r.OwnerUserId == ownerUserId);

        if (departmentId.HasValue)
            query = query.Where(r => r.DepartmentId == departmentId);

        if (reviewDueOnly == true)
        {
            var now = DateTime.UtcNow;
            query = query.Where(r => r.Status != RiskStatus.Closed
                                     && r.ReviewDueDate != null
                                     && r.ReviewDueDate <= now);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim();
            query = query.Where(r => r.Title.Contains(q)
                                     || (r.Description != null && r.Description.Contains(q))
                                     || (r.Category != null && r.Category.Contains(q))
                                     || (r.OwnerName != null && r.OwnerName.Contains(q)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.Likelihood * r.Impact)
            .ThenByDescending(r => r.LastUpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new RiskListDto
        {
            Items = items.Select(ToSummary).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<RiskDetailDto?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken ct = default)
    {
        var risk = await _db.Risks.AsNoTracking()
            .FirstOrDefaultAsync(r => r.TenantId == organizationId && r.Id == id, ct);
        return risk == null ? null : ToDetail(risk);
    }

    public async Task<RiskDetailDto> CreateAsync(
        Guid organizationId,
        Guid hospitalId,
        Guid userId,
        CreateRiskRequestDto request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        Risk.ValidateScoreDimensions(request.Likelihood, request.Impact);
        Risk.ValidateResidualDimensions(request.ResidualLikelihood, request.ResidualImpact);

        var now = DateTime.UtcNow;
        var status = request.Status ?? RiskStatus.Identified;
        if (status == RiskStatus.Closed)
            throw new ArgumentException("Cannot create a risk already Closed.");

        var risk = new Risk
        {
            Id = Guid.NewGuid(),
            TenantId = organizationId,
            HospitalId = hospitalId,
            Title = request.Title.Trim(),
            Description = request.Description,
            Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
            Severity = request.Severity,
            Likelihood = request.Likelihood,
            Impact = request.Impact,
            ResidualLikelihood = request.ResidualLikelihood,
            ResidualImpact = request.ResidualImpact,
            OwnerUserId = request.OwnerUserId,
            OwnerName = request.OwnerName,
            MitigationPlan = request.MitigationPlan,
            ReviewDueDate = request.ReviewDueDate,
            DepartmentId = request.DepartmentId,
            ObjectiveElementId = request.ObjectiveElementId,
            TaskId = request.TaskId,
            FindingId = request.FindingId,
            IncidentId = request.IncidentId,
            DeficiencyId = request.DeficiencyId,
            Status = status,
            IdentifiedAt = now,
            LastUpdatedAt = now,
            CreatedAt = now,
            RowVersion = new byte[8],
        };

        _db.Risks.Add(risk);
        await _db.SaveChangesAsync(ct);
        return ToDetail(risk);
    }

    public async Task<RiskDetailDto> UpdateAsync(
        Guid organizationId,
        Guid id,
        Guid userId,
        UpdateRiskRequestDto request,
        CancellationToken ct = default)
    {
        var risk = await _db.Risks.FirstOrDefaultAsync(r => r.TenantId == organizationId && r.Id == id, ct)
                   ?? throw new InvalidOperationException("Risk not found.");

        risk.EnsureMutable();

        if (request.Title != null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Title cannot be empty.");
            risk.Title = request.Title.Trim();
        }

        if (request.Description != null) risk.Description = request.Description;
        if (request.Category != null)
            risk.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        if (request.Severity.HasValue) risk.Severity = request.Severity.Value;

        var likelihood = request.Likelihood ?? risk.Likelihood;
        var impact = request.Impact ?? risk.Impact;
        Risk.ValidateScoreDimensions(likelihood, impact);
        risk.Likelihood = likelihood;
        risk.Impact = impact;

        if (request.ResidualLikelihood.HasValue || request.ResidualImpact.HasValue)
        {
            var rl = request.ResidualLikelihood ?? risk.ResidualLikelihood;
            var ri = request.ResidualImpact ?? risk.ResidualImpact;
            Risk.ValidateResidualDimensions(rl, ri);
            risk.ResidualLikelihood = rl;
            risk.ResidualImpact = ri;
        }

        if (request.OwnerUserId.HasValue) risk.OwnerUserId = request.OwnerUserId;
        if (request.OwnerName != null) risk.OwnerName = request.OwnerName;
        if (request.MitigationPlan != null) risk.MitigationPlan = request.MitigationPlan;
        if (request.ReviewDueDate.HasValue) risk.ReviewDueDate = request.ReviewDueDate;
        if (request.DepartmentId.HasValue) risk.DepartmentId = request.DepartmentId;
        if (request.ObjectiveElementId.HasValue) risk.ObjectiveElementId = request.ObjectiveElementId;
        if (request.TaskId.HasValue) risk.TaskId = request.TaskId;
        if (request.FindingId.HasValue) risk.FindingId = request.FindingId;
        if (request.IncidentId.HasValue) risk.IncidentId = request.IncidentId;
        if (request.DeficiencyId.HasValue) risk.DeficiencyId = request.DeficiencyId;

        if (request.Status.HasValue)
            risk.TransitionTo(request.Status.Value);

        risk.LastUpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ToDetail(risk);
    }

    public async Task<RiskMatrixDto> GetMatrixAsync(
        Guid organizationId,
        Guid? hospitalId,
        bool useResidual = false,
        CancellationToken ct = default)
    {
        var query = _db.Risks.AsNoTracking()
            .Where(r => r.TenantId == organizationId && r.Status != RiskStatus.Closed);
        if (hospitalId.HasValue && hospitalId.Value != Guid.Empty)
            query = query.Where(r => r.HospitalId == hospitalId.Value);

        var risks = await query.ToListAsync(ct);
        var now = DateTime.UtcNow;

        var cells = new List<RiskMatrixCellDto>();
        for (var likelihood = 1; likelihood <= 5; likelihood++)
        {
            for (var impact = 1; impact <= 5; impact++)
            {
                var cellRisks = risks.Where(r =>
                {
                    if (useResidual && r.ResidualLikelihood.HasValue && r.ResidualImpact.HasValue)
                        return r.ResidualLikelihood == likelihood && r.ResidualImpact == impact;
                    return r.Likelihood == likelihood && r.Impact == impact;
                }).ToList();

                var score = likelihood * impact;
                cells.Add(new RiskMatrixCellDto
                {
                    Likelihood = likelihood,
                    Impact = impact,
                    Score = score,
                    Count = cellRisks.Count,
                    Band = ScoreBand(score),
                    Risks = cellRisks.Select(r => new RiskMatrixRiskItemDto
                    {
                        Id = r.Id,
                        Title = r.Title,
                        Status = r.Status.ToString(),
                        RiskScore = useResidual ? (r.ResidualRiskScore ?? r.RiskScore) : r.RiskScore,
                    }).Take(10).ToList(),
                });
            }
        }

        return new RiskMatrixDto
        {
            Cells = cells,
            TotalActiveRisks = risks.Count,
            HighScoreCount = risks.Count(r => (useResidual ? (r.ResidualRiskScore ?? r.RiskScore) : r.RiskScore) >= 15),
            ReviewDueCount = risks.Count(r => r.ReviewDueDate != null && r.ReviewDueDate <= now),
        };
    }

    private static string ScoreBand(int score) => score switch
    {
        >= 15 => "Critical",
        >= 10 => "High",
        >= 5 => "Medium",
        _ => "Low",
    };

    private static RiskSummaryDto ToSummary(Risk r) => new()
    {
        Id = r.Id,
        Title = r.Title,
        Category = r.Category,
        Status = r.Status.ToString(),
        Severity = r.Severity.ToString(),
        Likelihood = r.Likelihood,
        Impact = r.Impact,
        RiskScore = r.RiskScore,
        ResidualLikelihood = r.ResidualLikelihood,
        ResidualImpact = r.ResidualImpact,
        ResidualRiskScore = r.ResidualRiskScore,
        OwnerUserId = r.OwnerUserId,
        OwnerName = r.OwnerName,
        DepartmentId = r.DepartmentId,
        ReviewDueDate = r.ReviewDueDate,
        IdentifiedAt = r.IdentifiedAt,
        LastUpdatedAt = r.LastUpdatedAt,
    };

    private static RiskDetailDto ToDetail(Risk r) => new()
    {
        Id = r.Id,
        TenantId = r.TenantId,
        HospitalId = r.HospitalId,
        Title = r.Title,
        Description = r.Description,
        Category = r.Category,
        Status = r.Status.ToString(),
        Severity = r.Severity.ToString(),
        Likelihood = r.Likelihood,
        Impact = r.Impact,
        RiskScore = r.RiskScore,
        ResidualLikelihood = r.ResidualLikelihood,
        ResidualImpact = r.ResidualImpact,
        ResidualRiskScore = r.ResidualRiskScore,
        OwnerUserId = r.OwnerUserId,
        OwnerName = r.OwnerName,
        MitigationPlan = r.MitigationPlan,
        DepartmentId = r.DepartmentId,
        ReviewDueDate = r.ReviewDueDate,
        ObjectiveElementId = r.ObjectiveElementId,
        TaskId = r.TaskId,
        FindingId = r.FindingId,
        IncidentId = r.IncidentId,
        DeficiencyId = r.DeficiencyId,
        IdentifiedAt = r.IdentifiedAt,
        ClosedAt = r.ClosedAt,
        LastUpdatedAt = r.LastUpdatedAt,
    };
}
