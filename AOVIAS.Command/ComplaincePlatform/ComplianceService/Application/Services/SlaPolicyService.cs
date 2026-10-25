using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

public sealed class SlaPolicyService : ISlaPolicyService
{
    private readonly ComplianceDbContext _db;
    private readonly IUnitOfWork _unitOfWork;

    public SlaPolicyService(ComplianceDbContext db, IUnitOfWork unitOfWork)
    {
        _db = db;
        _unitOfWork = unitOfWork;
    }

    public async Task EnsureDefaultPoliciesAsync(Guid organizationId, Guid? hospitalId = null, CancellationToken ct = default)
    {
        if (await _db.SlaPolicies.AnyAsync(p => p.OrganizationId == organizationId, ct))
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var template in SlaPolicyDefaults.Templates)
        {
            var escalationPolicy = new EscalationPolicy
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                HospitalId = hospitalId,
                Name = $"{template.Name} Escalation",
                Severity = template.Severity,
                Status = SlaPolicyStatus.Active,
                CreatedOn = now,
                CreatedAt = now,
                RowVersion = new byte[8],
            };

            foreach (var level in template.EscalationLevels)
            {
                escalationPolicy.Levels.Add(new EscalationLevel
                {
                    Id = Guid.NewGuid(),
                    EscalationPolicyId = escalationPolicy.Id,
                    Level = level.Level,
                    TargetType = level.TargetType,
                    TargetDesignation = level.TargetDesignation,
                    TargetLabel = level.TargetLabel,
                    TriggerAtSlaPercent = level.TriggerAtSlaPercent,
                    CreatedAt = now,
                    RowVersion = new byte[8],
                });
            }

            await _db.EscalationPolicies.AddAsync(escalationPolicy, ct);

            await _db.SlaPolicies.AddAsync(new SlaPolicy
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                HospitalId = hospitalId,
                Name = template.Name,
                Severity = template.Severity,
                TargetResolutionHours = template.TargetResolutionHours,
                WarningThresholdPercent = template.WarningThresholdPercent,
                EscalationPolicyId = escalationPolicy.Id,
                Status = SlaPolicyStatus.Active,
                CreatedOn = now,
                CreatedAt = now,
                RowVersion = new byte[8],
            }, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SlaPolicyDto>> ListPoliciesAsync(
        Guid organizationId,
        Guid? hospitalId = null,
        CancellationToken ct = default)
    {
        await EnsureDefaultPoliciesAsync(organizationId, hospitalId, ct);

        var query = _db.SlaPolicies.AsNoTracking()
            .Where(p => p.OrganizationId == organizationId && p.Status == SlaPolicyStatus.Active);

        if (hospitalId.HasValue)
        {
            query = query.Where(p => p.HospitalId == null || p.HospitalId == hospitalId);
        }

        return await query
            .OrderBy(p => p.Severity)
            .Select(p => new SlaPolicyDto
            {
                Id = p.Id,
                OrganizationId = p.OrganizationId,
                HospitalId = p.HospitalId,
                Name = p.Name,
                Severity = p.Severity.ToString(),
                LifecycleStage = p.LifecycleStage != null ? p.LifecycleStage.ToString() : null,
                TargetResolutionHours = p.TargetResolutionHours,
                WarningThresholdPercent = p.WarningThresholdPercent,
                EscalationPolicyId = p.EscalationPolicyId,
                Status = p.Status.ToString(),
                CreatedOn = p.CreatedOn,
            })
            .ToListAsync(ct);
    }

    public async Task<SlaPolicyDto> CreatePolicyAsync(CreateSlaPolicyRequestDto request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var policy = new SlaPolicy
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            HospitalId = request.HospitalId,
            Name = request.Name,
            Severity = request.Severity,
            LifecycleStage = request.LifecycleStage,
            TargetResolutionHours = request.TargetResolutionHours,
            WarningThresholdPercent = request.WarningThresholdPercent,
            EscalationPolicyId = request.EscalationPolicyId,
            Status = SlaPolicyStatus.Active,
            CreatedOn = now,
            CreatedAt = now,
            RowVersion = new byte[8],
        };

        await _db.SlaPolicies.AddAsync(policy, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return new SlaPolicyDto
        {
            Id = policy.Id,
            OrganizationId = policy.OrganizationId,
            HospitalId = policy.HospitalId,
            Name = policy.Name,
            Severity = policy.Severity.ToString(),
            LifecycleStage = policy.LifecycleStage?.ToString(),
            TargetResolutionHours = policy.TargetResolutionHours,
            WarningThresholdPercent = policy.WarningThresholdPercent,
            EscalationPolicyId = policy.EscalationPolicyId,
            Status = policy.Status.ToString(),
            CreatedOn = policy.CreatedOn,
        };
    }

    public async Task<SlaPolicy?> ResolvePolicyForDeficiencyAsync(
        ComplianceDeficiency deficiency,
        CancellationToken ct = default)
    {
        await EnsureDefaultPoliciesAsync(deficiency.OrganizationId, deficiency.HospitalId, ct);

        var policies = await _db.SlaPolicies
            .AsNoTracking()
            .Where(p => p.OrganizationId == deficiency.OrganizationId
                        && p.Status == SlaPolicyStatus.Active
                        && p.Severity == deficiency.Severity
                        && (p.HospitalId == null || p.HospitalId == deficiency.HospitalId))
            .OrderByDescending(p => p.HospitalId)
            .ThenByDescending(p => p.LifecycleStage == deficiency.MaturityLevel)
            .ToListAsync(ct);

        return policies.FirstOrDefault();
    }
}
