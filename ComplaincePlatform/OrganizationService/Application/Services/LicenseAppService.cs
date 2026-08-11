using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrganizationService.Domain.Entities;
using OrganizationService.Infrastructure.Persistence;

namespace OrganizationService.Application.Services;

public sealed class LicenseStatusDto
{
    public Guid LicenseKeyId { get; set; }
    public Guid OrganizationId { get; set; }
    public string KeyCode { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    /// <summary>Mission alias for Active (Activated / Operational).</summary>
    public string StatusAlias { get; set; } = string.Empty;
    public int MaxSeats { get; set; }
    public int UsedSeats { get; set; }
    public int GracePeriodDays { get; set; }
    public int ExtensionCount { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? GraceEndsAtUtc { get; set; }
    public IReadOnlyList<string> Entitlements { get; set; } = Array.Empty<string>();
    public IReadOnlyList<EntitlementEvaluation> EntitlementDetails { get; set; } = Array.Empty<EntitlementEvaluation>();
    public bool IsOperational { get; set; }
    public string? Message { get; set; }
    public Guid? SubscriptionPlanId { get; set; }
    public Guid? OrganizationSubscriptionId { get; set; }
}

public sealed class LicensingSummaryDto
{
    public Guid OrganizationId { get; set; }
    public LicenseStatusDto? License { get; set; }
    public string OrganizationCommercialStatus { get; set; } = "Unknown";
    public string? PlanCode { get; set; }
    public string? PlanName { get; set; }
    public string? PlanLifecycleStatus { get; set; }
    public Guid? CatalogSubscriptionId { get; set; }
    public bool CatalogLinked { get; set; }
    public IReadOnlyList<EntitlementEvaluation> Entitlements { get; set; } = Array.Empty<EntitlementEvaluation>();
}

public sealed class CatalogMigrationResultDto
{
    public int LicensesLinked { get; set; }
    public int CatalogRowsCreated { get; set; }
    public int DriftDetected { get; set; }
    public IReadOnlyList<string> Notes { get; set; } = Array.Empty<string>();
}

public sealed class LicenseAppService
{
    private static readonly string[] DefaultEntitlements =
    [
        "core.compliance", "core.documents", "core.capa", "ai.assist", "ai.write",
        "governance.risk", "ace.insights", "frameworks"
    ];

    private readonly OrganizationDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LicenseAppService> _logger;
    private readonly EntitlementEngine _entitlements;

    public LicenseAppService(
        OrganizationDbContext db,
        IConfiguration configuration,
        ILogger<LicenseAppService> logger,
        EntitlementEngine entitlements)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
        _entitlements = entitlements;
    }

    public async Task<LicenseKey> IssueAsync(
        Guid organizationId,
        Guid? planId,
        LicenseType type,
        int maxSeats,
        int? validDays,
        int graceDays,
        IEnumerable<string>? entitlements,
        string? customerName,
        CancellationToken cancellationToken = default)
    {
        if (type is LicenseType.Trial or LicenseType.Subscription)
        {
            var existing = await FindOperationalCommercialKeyAsync(organizationId, cancellationToken);
            if (existing != null)
            {
                throw new InvalidOperationException(
                    "Organization already has an operational Trial or Subscription license.");
            }
        }

        var now = DateTime.UtcNow;
        var days = validDays ?? (type == LicenseType.Trial ? 30 : 365);
        var ents = entitlements?.ToList() ?? DefaultEntitlements.ToList();

        var key = new LicenseKey
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            SubscriptionPlanId = planId,
            KeyCode = GenerateKeyCode(),
            LicenseType = type,
            Status = LicenseStatus.Issued,
            MaxSeats = Math.Max(1, maxSeats),
            UsedSeats = 0,
            GracePeriodDays = Math.Max(0, graceDays),
            IssuedAtUtc = now,
            ExpiresAtUtc = now.AddDays(days),
            EntitlementsJson = JsonSerializer.Serialize(ents),
            CustomerName = customerName,
            CreatedAtUtc = now
        };

        _db.LicenseKeys.Add(key);
        await _db.SaveChangesAsync(cancellationToken);

        if (_configuration.GetValue("Licensing:SyncCatalogFromLicense", true))
        {
            await SyncCatalogFromLicenseAsync(key, cancellationToken);
        }

        _entitlements.InvalidateCache();
        _logger.LogInformation("Issued license {Key} for org {Org}", key.KeyCode, organizationId);
        return key;
    }

    /// <summary>
    /// Idempotent trial bootstrap: reuse an operational Trial/Subscription key when present; otherwise issue Trial.
    /// </summary>
    public async Task<(LicenseKey Key, bool Reused)> GetOrIssueTrialAsync(
        Guid organizationId,
        int maxSeats,
        int? validDays,
        int graceDays,
        IEnumerable<string>? entitlements,
        string? customerName,
        Guid? planId = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await FindOperationalCommercialKeyAsync(organizationId, cancellationToken);
        if (existing != null)
        {
            RefreshStatus(existing);
            await _db.SaveChangesAsync(cancellationToken);
            return (existing, true);
        }

        var key = await IssueAsync(
            organizationId,
            planId,
            LicenseType.Trial,
            maxSeats,
            validDays,
            graceDays,
            entitlements,
            customerName,
            cancellationToken);
        return (key, false);
    }

    public async Task<LicenseKey?> FindOperationalCommercialKeyAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var keys = await _db.LicenseKeys
            .Where(x => x.OrganizationId == organizationId
                        && x.Status != LicenseStatus.Revoked
                        && x.Status != LicenseStatus.Expired
                        && x.Status != LicenseStatus.Suspended
                        && x.Status != LicenseStatus.Archived
                        && (x.LicenseType == LicenseType.Trial
                            || x.LicenseType == LicenseType.Subscription
                            || x.LicenseType == LicenseType.Enterprise
                            || x.LicenseType == LicenseType.Evaluation
                            || x.LicenseType == LicenseType.Developer))
            .OrderByDescending(x => x.ActivatedAtUtc ?? x.IssuedAtUtc)
            .ToListAsync(cancellationToken);

        foreach (var key in keys)
        {
            RefreshStatus(key);
            if (IsOperational(key) && key.LicenseType is LicenseType.Trial
                    or LicenseType.Subscription
                    or LicenseType.Enterprise
                    or LicenseType.Evaluation
                    or LicenseType.Developer)
            {
                return key;
            }
        }

        return null;
    }

    public async Task RevokeAsync(Guid licenseKeyId, CancellationToken cancellationToken = default)
    {
        var key = await _db.LicenseKeys.FirstOrDefaultAsync(x => x.Id == licenseKeyId, cancellationToken)
                  ?? throw new InvalidOperationException("License key not found.");
        key.Status = LicenseStatus.Revoked;
        key.UpdatedAtUtc = DateTime.UtcNow;

        var activations = await _db.LicenseActivations
            .Where(x => x.LicenseKeyId == licenseKeyId && x.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var a in activations)
        {
            a.IsActive = false;
            a.DeactivatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogWarning("Revoked license {KeyId} for org {Org}", licenseKeyId, key.OrganizationId);
    }

    public async Task ReleaseSeatAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var seats = await _db.LicenseSeatAssignments
            .Where(x => x.OrganizationId == organizationId && x.UserId == userId && x.IsActive)
            .ToListAsync(cancellationToken);
        if (seats.Count == 0)
        {
            return;
        }

        foreach (var seat in seats)
        {
            seat.IsActive = false;
            seat.ReleasedAtUtc = DateTime.UtcNow;
            var key = await _db.LicenseKeys.FirstOrDefaultAsync(x => x.Id == seat.LicenseKeyId, cancellationToken);
            if (key != null && key.UsedSeats > 0)
            {
                key.UsedSeats -= 1;
                key.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<LicenseStatusDto> ActivateOnlineAsync(
        string keyCode,
        Guid organizationId,
        string machineFingerprint,
        string? activatedBy,
        CancellationToken cancellationToken = default)
    {
        var key = await _db.LicenseKeys.FirstOrDefaultAsync(
            x => x.KeyCode == keyCode && x.OrganizationId == organizationId,
            cancellationToken)
            ?? throw new InvalidOperationException("License key not found for organization.");

        EnsureNotRevoked(key);
        var now = DateTime.UtcNow;

        // Idempotent: already active → skip re-activation bookkeeping unless fingerprint missing.
        if (key.Status == LicenseStatus.Active && key.ActivatedAtUtc.HasValue)
        {
            var hasFp = await _db.LicenseActivations.AnyAsync(
                x => x.LicenseKeyId == key.Id && x.MachineFingerprint == machineFingerprint && x.IsActive,
                cancellationToken);
            if (hasFp)
            {
                return ToStatus(key);
            }
        }

        key.Status = LicenseStatus.Active;
        key.ActivatedAtUtc ??= now;
        key.UpdatedAtUtc = now;
        if (key.ExpiresAtUtc.HasValue && key.GracePeriodDays > 0)
        {
            key.GraceEndsAtUtc = key.ExpiresAtUtc.Value.AddDays(key.GracePeriodDays);
        }

        // Deactivate prior activations for this fingerprint
        var prior = await _db.LicenseActivations
            .Where(x => x.LicenseKeyId == key.Id && x.MachineFingerprint == machineFingerprint && x.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var p in prior)
        {
            p.IsActive = false;
            p.DeactivatedAtUtc = now;
        }

        _db.LicenseActivations.Add(new LicenseActivation
        {
            Id = Guid.NewGuid(),
            LicenseKeyId = key.Id,
            OrganizationId = organizationId,
            MachineFingerprint = machineFingerprint,
            ActivationMode = "Online",
            ActivatedAtUtc = now,
            IsActive = true,
            ActivatedBy = activatedBy
        });

        await _db.SaveChangesAsync(cancellationToken);
        return ToStatus(key);
    }

    public async Task<LicenseStatusDto> ActivateOfflineAsync(
        string keyCode,
        Guid organizationId,
        string machineFingerprint,
        string offlinePayload,
        string? activatedBy,
        CancellationToken cancellationToken = default)
    {
        var signature = SignPayload($"{keyCode}|{organizationId:N}|{machineFingerprint}|{offlinePayload}");
        var key = await _db.LicenseKeys.FirstOrDefaultAsync(
            x => x.KeyCode == keyCode && x.OrganizationId == organizationId,
            cancellationToken)
            ?? throw new InvalidOperationException("License key not found for organization.");

        EnsureNotRevoked(key);
        var now = DateTime.UtcNow;
        key.Status = LicenseStatus.Active;
        key.LicenseType = LicenseType.Offline;
        key.ActivatedAtUtc ??= now;
        key.UpdatedAtUtc = now;

        _db.LicenseActivations.Add(new LicenseActivation
        {
            Id = Guid.NewGuid(),
            LicenseKeyId = key.Id,
            OrganizationId = organizationId,
            MachineFingerprint = machineFingerprint,
            ActivationMode = "Offline",
            OfflinePayload = offlinePayload,
            Signature = signature,
            ActivatedAtUtc = now,
            IsActive = true,
            ActivatedBy = activatedBy
        });

        await _db.SaveChangesAsync(cancellationToken);
        return ToStatus(key);
    }

    public async Task<LicenseStatusDto> ValidateAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var key = await GetActiveKeyAsync(organizationId, cancellationToken);
        if (key == null)
        {
            return new LicenseStatusDto
            {
                OrganizationId = organizationId,
                Status = LicenseStatus.Expired.ToString(),
                IsOperational = false,
                Message = "No active license."
            };
        }

        RefreshStatus(key);
        await _db.SaveChangesAsync(cancellationToken);
        return ToStatus(key);
    }

    public async Task<LicenseHeartbeat> HeartbeatAsync(
        Guid organizationId,
        string machineFingerprint,
        string? productVersion,
        int seatUsage,
        CancellationToken cancellationToken = default)
    {
        var key = await GetActiveKeyAsync(organizationId, cancellationToken)
                  ?? throw new InvalidOperationException("No active license for heartbeat.");

        RefreshStatus(key);
        key.UsedSeats = Math.Max(0, seatUsage);
        key.UpdatedAtUtc = DateTime.UtcNow;

        var beat = new LicenseHeartbeat
        {
            Id = Guid.NewGuid(),
            LicenseKeyId = key.Id,
            OrganizationId = organizationId,
            MachineFingerprint = machineFingerprint,
            ReceivedAtUtc = DateTime.UtcNow,
            ProductVersion = productVersion,
            StatusSnapshot = key.Status.ToString(),
            SeatUsage = seatUsage
        };
        _db.LicenseHeartbeats.Add(beat);
        await _db.SaveChangesAsync(cancellationToken);
        return beat;
    }

    public async Task<LicenseSeatAssignment> AssignSeatAsync(
        Guid organizationId,
        Guid userId,
        string? email,
        CancellationToken cancellationToken = default)
    {
        var key = await GetActiveKeyAsync(organizationId, cancellationToken)
                  ?? throw new InvalidOperationException("No active license.");

        RefreshStatus(key);
        if (!IsOperational(key))
        {
            throw new InvalidOperationException("License is not operational.");
        }

        var existing = await _db.LicenseSeatAssignments.FirstOrDefaultAsync(
            x => x.LicenseKeyId == key.Id && x.UserId == userId && x.IsActive,
            cancellationToken);
        if (existing != null)
        {
            return existing;
        }

        if (key.UsedSeats >= key.MaxSeats)
        {
            throw new InvalidOperationException("Seat limit reached.");
        }

        var seat = new LicenseSeatAssignment
        {
            Id = Guid.NewGuid(),
            LicenseKeyId = key.Id,
            OrganizationId = organizationId,
            UserId = userId,
            UserEmail = email,
            AssignedAtUtc = DateTime.UtcNow,
            IsActive = true
        };
        key.UsedSeats += 1;
        key.UpdatedAtUtc = DateTime.UtcNow;
        _db.LicenseSeatAssignments.Add(seat);
        await _db.SaveChangesAsync(cancellationToken);
        return seat;
    }

    public async Task<bool> HasEntitlementAsync(Guid organizationId, string featureCode, CancellationToken cancellationToken = default)
    {
        var eval = await EvaluateEntitlementAsync(organizationId, featureCode, cancellationToken);
        return eval.Allowed;
    }

    public async Task<EntitlementEvaluation> EvaluateEntitlementAsync(
        Guid organizationId,
        string featureCode,
        CancellationToken cancellationToken = default)
    {
        var key = await GetActiveKeyAsync(organizationId, cancellationToken);
        if (key == null)
        {
            return new EntitlementEvaluation
            {
                Code = featureCode,
                Allowed = false,
                Decision = FeatureAccessDecision.Disabled,
                Message = "No license found."
            };
        }

        RefreshStatus(key);
        await _db.SaveChangesAsync(cancellationToken);
        return _entitlements.Evaluate(key.EntitlementsJson, featureCode, IsOperational(key));
    }

    public async Task<IReadOnlyList<EntitlementEvaluation>> ListEntitlementsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var key = await GetActiveKeyAsync(organizationId, cancellationToken);
        if (key == null)
        {
            return Array.Empty<EntitlementEvaluation>();
        }

        RefreshStatus(key);
        return _entitlements.EvaluateAll(key.EntitlementsJson, IsOperational(key));
    }

    public async Task<LicensingSummaryDto> GetLicensingSummaryAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var key = await GetActiveKeyAsync(organizationId, cancellationToken);
        LicenseStatusDto? licenseDto = null;
        if (key != null)
        {
            RefreshStatus(key);
            await _db.SaveChangesAsync(cancellationToken);
            licenseDto = ToStatus(key);
        }

        string? planCode = null;
        string? planName = null;
        string? planLifecycle = null;
        Guid? catalogId = key?.OrganizationSubscriptionId;
        var planId = key?.SubscriptionPlanId;

        if (catalogId == null || catalogId == Guid.Empty)
        {
            var catalog = await _db.OrganizationSubscriptions.AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.IsActive)
                .OrderByDescending(x => x.StartsAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (catalog != null)
            {
                catalogId = catalog.Id;
                planId ??= catalog.SubscriptionPlanId;
            }
        }

        if (planId is { } pid && pid != Guid.Empty)
        {
            var plan = await _db.SubscriptionPlans.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == pid, cancellationToken);
            if (plan != null)
            {
                planCode = plan.Code;
                planName = plan.Name;
                planLifecycle = string.IsNullOrWhiteSpace(plan.LifecycleStatus)
                    ? (plan.IsActive ? "Published" : "Retired")
                    : plan.LifecycleStatus;
            }
        }

        return new LicensingSummaryDto
        {
            OrganizationId = organizationId,
            License = licenseDto,
            OrganizationCommercialStatus = EntitlementEngine.MapOrganizationDisplay(key).ToString(),
            PlanCode = planCode,
            PlanName = planName,
            PlanLifecycleStatus = planLifecycle,
            CatalogSubscriptionId = catalogId,
            CatalogLinked = key?.OrganizationSubscriptionId != null,
            Entitlements = licenseDto?.EntitlementDetails ?? Array.Empty<EntitlementEvaluation>()
        };
    }

    /// <summary>Best-effort catalogue sync from LicenseKey (metadata only; License remains SSOT).</summary>
    public async Task SyncCatalogFromLicenseAsync(LicenseKey key, CancellationToken cancellationToken = default)
    {
        try
        {
            var planId = key.SubscriptionPlanId
                         ?? _configuration.GetValue<Guid?>("Licensing:DefaultCatalogPlanId")
                         ?? _configuration.GetValue<Guid?>("TrialEnablement:DefaultTrialPlanId");

            if (planId is null || planId == Guid.Empty)
            {
                var fallback = await _db.SubscriptionPlans
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Code)
                    .Select(p => (Guid?)p.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                planId = fallback;
            }

            if (planId is null || planId == Guid.Empty)
            {
                _logger.LogDebug("Catalog sync skipped for {Key}: no SubscriptionPlan available", key.KeyCode);
                return;
            }

            var plan = await _db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == planId, cancellationToken);
            if (plan == null)
            {
                return;
            }

            key.SubscriptionPlanId ??= plan.Id;

            if (key.OrganizationSubscriptionId is { } existingId && existingId != Guid.Empty)
            {
                var existing = await _db.OrganizationSubscriptions
                    .FirstOrDefaultAsync(x => x.Id == existingId, cancellationToken);
                if (existing != null)
                {
                    existing.SubscriptionPlanId = plan.Id;
                    existing.IsTrial = key.LicenseType == LicenseType.Trial;
                    existing.IsActive = IsOperational(key);
                    existing.EndsAt = key.ExpiresAtUtc;
                    await _db.SaveChangesAsync(cancellationToken);
                    return;
                }
            }

            var active = await _db.OrganizationSubscriptions
                .Where(x => x.OrganizationId == key.OrganizationId && x.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var sub in active)
            {
                sub.IsActive = false;
                sub.EndsAt = DateTime.UtcNow;
            }

            var row = new Shared.Infrastructure.Domain.OrganizationSubscription
            {
                Id = Guid.NewGuid(),
                OrganizationId = key.OrganizationId,
                SubscriptionPlanId = plan.Id,
                StartsAt = key.ActivatedAtUtc ?? key.IssuedAtUtc,
                EndsAt = key.ExpiresAtUtc,
                IsTrial = key.LicenseType == LicenseType.Trial,
                IsActive = IsOperational(key),
                CurrentUserCount = key.UsedSeats,
                CurrentFrameworkCount = 0,
                CreatedAt = DateTime.UtcNow
            };
            _db.OrganizationSubscriptions.Add(row);
            key.OrganizationSubscriptionId = row.Id;
            key.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Synced catalogue subscription {SubId} from license {Key}", row.Id, key.KeyCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Catalog sync from license {Key} failed (best-effort)", key.KeyCode);
        }
    }

    public async Task<CatalogMigrationResultDto> RunCatalogBackfillAsync(CancellationToken cancellationToken = default)
    {
        var notes = new List<string>();
        var linked = 0;
        var created = 0;
        var drift = 0;

        var keys = await _db.LicenseKeys
            .Where(x => x.Status != LicenseStatus.Revoked && x.Status != LicenseStatus.Archived)
            .OrderByDescending(x => x.IssuedAtUtc)
            .ToListAsync(cancellationToken);

        foreach (var key in keys)
        {
            RefreshStatus(key);
            if (key.OrganizationSubscriptionId == null)
            {
                var before = await _db.OrganizationSubscriptions.CountAsync(
                    x => x.OrganizationId == key.OrganizationId, cancellationToken);
                await SyncCatalogFromLicenseAsync(key, cancellationToken);
                if (key.OrganizationSubscriptionId != null)
                {
                    linked++;
                    var after = await _db.OrganizationSubscriptions.CountAsync(
                        x => x.OrganizationId == key.OrganizationId, cancellationToken);
                    if (after > before)
                    {
                        created++;
                    }
                }
            }
            else
            {
                var sub = await _db.OrganizationSubscriptions.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == key.OrganizationSubscriptionId, cancellationToken);
                if (sub == null)
                {
                    drift++;
                    notes.Add($"Orphan link on license {key.Id}");
                    key.OrganizationSubscriptionId = null;
                    await SyncCatalogFromLicenseAsync(key, cancellationToken);
                }
                else if (key.SubscriptionPlanId.HasValue && sub.SubscriptionPlanId != key.SubscriptionPlanId)
                {
                    drift++;
                    notes.Add($"Plan drift org {key.OrganizationId}: license vs catalogue");
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new CatalogMigrationResultDto
        {
            LicensesLinked = linked,
            CatalogRowsCreated = created,
            DriftDetected = drift,
            Notes = notes.Take(50).ToList()
        };
    }

    public async Task<LicenseKey> SuspendAsync(Guid licenseKeyId, CancellationToken cancellationToken = default)
    {
        var key = await _db.LicenseKeys.FirstOrDefaultAsync(x => x.Id == licenseKeyId, cancellationToken)
                  ?? throw new InvalidOperationException("License key not found.");
        key.Status = LicenseStatus.Suspended;
        key.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        _entitlements.InvalidateCache();
        return key;
    }

    public async Task<LicenseKey> ArchiveAsync(Guid licenseKeyId, CancellationToken cancellationToken = default)
    {
        var key = await _db.LicenseKeys.FirstOrDefaultAsync(x => x.Id == licenseKeyId, cancellationToken)
                  ?? throw new InvalidOperationException("License key not found.");
        key.Status = LicenseStatus.Archived;
        key.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        _entitlements.InvalidateCache();
        return key;
    }

    public async Task<IReadOnlyList<LicenseActivation>> GetActivationHistoryAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _db.LicenseActivations.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .OrderByDescending(x => x.ActivatedAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<object> GetUsageAnalyticsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var key = await GetActiveKeyAsync(organizationId, cancellationToken);
        var heartbeats = await _db.LicenseHeartbeats.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .OrderByDescending(x => x.ReceivedAtUtc)
            .Take(50)
            .Select(x => new { x.ReceivedAtUtc, x.SeatUsage, x.ProductVersion, x.StatusSnapshot })
            .ToListAsync(cancellationToken);

        return new
        {
            License = key == null ? null : ToStatus(key),
            RecentHeartbeats = heartbeats,
            ActiveSeats = key?.UsedSeats ?? 0,
            MaxSeats = key?.MaxSeats ?? 0
        };
    }

    /// <summary>CRP Phase 4 — extend trial expiry within policy caps.</summary>
    public async Task<LicenseKey> ExtendTrialAsync(
        Guid organizationId,
        int extensionDays,
        int maxExtensions,
        CancellationToken cancellationToken = default)
    {
        var key = await RequireTrialKeyAsync(organizationId, cancellationToken);
        if (key.ExtensionCount >= maxExtensions)
        {
            throw new InvalidOperationException($"Trial extension limit reached ({maxExtensions}).");
        }

        var days = Math.Max(1, extensionDays);
        key.ExpiresAtUtc = (key.ExpiresAtUtc ?? DateTime.UtcNow).AddDays(days);
        if (key.GraceEndsAtUtc.HasValue)
        {
            key.GraceEndsAtUtc = key.ExpiresAtUtc.Value.AddDays(key.GracePeriodDays);
        }

        key.ExtensionCount += 1;
        key.UpdatedAtUtc = DateTime.UtcNow;
        if (key.Status is LicenseStatus.Expired or LicenseStatus.Grace)
        {
            key.Status = LicenseStatus.Active;
            key.GraceEndsAtUtc = null;
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Extended trial {Key} for org {Org} by {Days}d (count={Count})",
            key.KeyCode, organizationId, days, key.ExtensionCount);
        return key;
    }

    public async Task<LicenseKey> ShortenTrialAsync(
        Guid organizationId,
        int days,
        CancellationToken cancellationToken = default)
    {
        var key = await RequireTrialKeyAsync(organizationId, cancellationToken);
        var reduce = Math.Max(1, days);
        var current = key.ExpiresAtUtc ?? DateTime.UtcNow.AddDays(1);
        var next = current.AddDays(-reduce);
        if (next <= DateTime.UtcNow)
        {
            next = DateTime.UtcNow.AddHours(1);
        }

        key.ExpiresAtUtc = next;
        key.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return key;
    }

    public async Task<LicenseKey> ExpireTrialAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var key = await RequireTrialKeyAsync(organizationId, cancellationToken);
        key.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        key.GraceEndsAtUtc = DateTime.UtcNow.AddMinutes(-1);
        key.Status = LicenseStatus.Expired;
        key.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return key;
    }

    public async Task<LicenseKey> RestartTrialAsync(
        Guid organizationId,
        int validDays,
        int maxSeats,
        CancellationToken cancellationToken = default)
    {
        var existing = await FindOperationalCommercialKeyAsync(organizationId, cancellationToken);
        if (existing != null)
        {
            existing.Status = LicenseStatus.Revoked;
            existing.UpdatedAtUtc = DateTime.UtcNow;
            var activations = await _db.LicenseActivations
                .Where(a => a.LicenseKeyId == existing.Id && a.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var a in activations)
            {
                a.IsActive = false;
                a.DeactivatedAtUtc = DateTime.UtcNow;
            }
        }

        return await IssueAsync(
            organizationId,
            null,
            LicenseType.Trial,
            maxSeats,
            validDays,
            7,
            null,
            null,
            cancellationToken);
    }

    public async Task<LicenseKey> ConvertTrialToSubscriptionAsync(
        Guid organizationId,
        Guid? planId,
        int maxSeats,
        int validDays,
        CancellationToken cancellationToken = default)
    {
        var trial = await RequireTrialKeyAsync(organizationId, cancellationToken);
        trial.Status = LicenseStatus.Revoked;
        trial.UpdatedAtUtc = DateTime.UtcNow;
        var activations = await _db.LicenseActivations
            .Where(a => a.LicenseKeyId == trial.Id && a.IsActive)
            .ToListAsync(cancellationToken);
        foreach (var a in activations)
        {
            a.IsActive = false;
            a.DeactivatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var sub = await IssueAsync(
            organizationId,
            planId,
            LicenseType.Subscription,
            maxSeats <= 0 ? trial.MaxSeats : maxSeats,
            validDays <= 0 ? 365 : validDays,
            trial.GracePeriodDays,
            DeserializeEntitlements(trial.EntitlementsJson),
            trial.CustomerName,
            cancellationToken);

        await ActivateOnlineAsync(sub.KeyCode, organizationId, Environment.MachineName, "commercial-admin", cancellationToken);
        return await _db.LicenseKeys.FirstAsync(x => x.Id == sub.Id, cancellationToken);
    }

    public async Task<LicenseKey> SetMaxSeatsAsync(Guid licenseKeyId, int maxSeats, CancellationToken cancellationToken = default)
    {
        var key = await _db.LicenseKeys.FirstOrDefaultAsync(x => x.Id == licenseKeyId, cancellationToken)
                  ?? throw new InvalidOperationException("License not found.");
        EnsureNotRevoked(key);
        if (maxSeats < key.UsedSeats)
        {
            throw new InvalidOperationException("Max seats cannot be below used seats.");
        }

        key.MaxSeats = maxSeats;
        key.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return key;
    }

    public async Task<(IReadOnlyList<LicenseStatusDto> Items, int Total)> SearchAsync(
        string? query,
        string? licenseType,
        string? status,
        Guid? organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var q = _db.LicenseKeys.AsNoTracking().AsQueryable();
        if (organizationId is { } oid && oid != Guid.Empty)
        {
            q = q.Where(x => x.OrganizationId == oid);
        }

        if (!string.IsNullOrWhiteSpace(licenseType) && Enum.TryParse<LicenseType>(licenseType, true, out var lt))
        {
            q = q.Where(x => x.LicenseType == lt);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<LicenseStatus>(status, true, out var st))
        {
            q = q.Where(x => x.Status == st);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(x => x.KeyCode.Contains(term) || (x.CustomerName != null && x.CustomerName.Contains(term)));
        }

        var total = await q.CountAsync(cancellationToken);
        var keys = await q.OrderByDescending(x => x.IssuedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (keys.Select(ToStatus).ToList(), total);
    }

    private async Task<LicenseKey> RequireTrialKeyAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var key = await _db.LicenseKeys
            .Where(x => x.OrganizationId == organizationId && x.LicenseType == LicenseType.Trial && x.Status != LicenseStatus.Revoked)
            .OrderByDescending(x => x.ActivatedAtUtc ?? x.IssuedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (key == null)
        {
            throw new InvalidOperationException("No active trial license found for organization.");
        }

        return key;
    }

    private async Task<LicenseKey?> GetActiveKeyAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        return await _db.LicenseKeys
            .Where(x => x.OrganizationId == organizationId
                        && x.Status != LicenseStatus.Revoked
                        && x.Status != LicenseStatus.Archived)
            .OrderByDescending(x => x.ActivatedAtUtc ?? x.IssuedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static void EnsureNotRevoked(LicenseKey key)
    {
        if (key.Status is LicenseStatus.Revoked or LicenseStatus.Archived)
        {
            throw new InvalidOperationException("License has been revoked or archived.");
        }
    }

    private static void RefreshStatus(LicenseKey key)
    {
        var now = DateTime.UtcNow;
        if (key.Status is LicenseStatus.Revoked or LicenseStatus.Suspended or LicenseStatus.Archived)
        {
            return;
        }

        if (key.ExpiresAtUtc.HasValue && now > key.ExpiresAtUtc.Value)
        {
            if (key.GraceEndsAtUtc.HasValue && now <= key.GraceEndsAtUtc.Value)
            {
                key.Status = LicenseStatus.Grace;
            }
            else if (key.GracePeriodDays > 0 && !key.GraceEndsAtUtc.HasValue)
            {
                key.GraceEndsAtUtc = key.ExpiresAtUtc.Value.AddDays(key.GracePeriodDays);
                key.Status = now <= key.GraceEndsAtUtc.Value ? LicenseStatus.Grace : LicenseStatus.Expired;
            }
            else
            {
                key.Status = LicenseStatus.Expired;
            }
        }
        else if (key.ActivatedAtUtc.HasValue)
        {
            key.Status = LicenseStatus.Active;
        }
    }

    private static bool IsOperational(LicenseKey key)
        => key.Status is LicenseStatus.Active or LicenseStatus.Grace or LicenseStatus.Issued;

    private LicenseStatusDto ToStatus(LicenseKey key)
    {
        RefreshStatus(key);
        var operational = IsOperational(key);
        var defs = _entitlements.Parse(key.EntitlementsJson);
        var details = _entitlements.EvaluateAll(key.EntitlementsJson, operational);
        var codes = defs.Select(d => d.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return new LicenseStatusDto
        {
            LicenseKeyId = key.Id,
            OrganizationId = key.OrganizationId,
            KeyCode = MaskKey(key.KeyCode),
            LicenseType = key.LicenseType.ToString(),
            Status = key.Status.ToString(),
            StatusAlias = key.Status == LicenseStatus.Active ? "Operational" : key.Status.ToString(),
            MaxSeats = key.MaxSeats,
            UsedSeats = key.UsedSeats,
            GracePeriodDays = key.GracePeriodDays,
            ExtensionCount = key.ExtensionCount,
            ExpiresAtUtc = key.ExpiresAtUtc,
            GraceEndsAtUtc = key.GraceEndsAtUtc,
            Entitlements = codes,
            EntitlementDetails = details,
            IsOperational = operational,
            SubscriptionPlanId = key.SubscriptionPlanId,
            OrganizationSubscriptionId = key.OrganizationSubscriptionId,
            Message = key.Status switch
            {
                LicenseStatus.Grace => "License in grace period — renew soon.",
                LicenseStatus.Expired => "License expired.",
                LicenseStatus.Revoked => "License revoked.",
                LicenseStatus.Suspended => "License suspended.",
                LicenseStatus.Archived => "License archived.",
                LicenseStatus.Issued => "License issued — activate to go live.",
                _ => "License operational (Active)."
            }
        };
    }

    private IReadOnlyList<string> DeserializeEntitlements(string json)
        => _entitlements.Parse(json).Select(d => d.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static string MaskKey(string key)
    {
        if (string.IsNullOrEmpty(key) || key.Length < 8)
        {
            return "****";
        }

        return key[..4] + "-****-****-" + key[^4..];
    }

    private static string GenerateKeyCode()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        var hex = Convert.ToHexString(bytes);
        return $"AOVI-{hex[..4]}-{hex[4..8]}-{hex[8..12]}-{hex[12..16]}";
    }

    private string SignPayload(string payload)
    {
        var secret = _configuration["Licensing:SigningKey"] ?? "AOVIAS-DEV-LICENSE-SIGNING-KEY-CHANGE-ME";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }
}
