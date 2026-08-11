using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OrganizationService.Application.Contracts;
using OrganizationService.Configuration;
using OrganizationService.Domain;
using OrganizationService.Domain.Entities;
using OrganizationService.Domain.Enums;
using OrganizationService.Infrastructure.Persistence;
using Shared.Communication;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.DTO;
using Shared.Infrastructure.Email;

namespace OrganizationService.Application.Services;

public class OrganizationTimelineProjector(OrganizationDbContext db) : IOrganizationTimelineService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task AppendAsync(string eventType, string summary, Guid? organizationId, string? resourceType, Guid? resourceId, Guid? actorUserId, object? payload = null, CancellationToken ct = default)
    {
        db.OrganizationTimeline.Add(new OrganizationTimelineEntry
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Summary = summary,
            OrganizationId = organizationId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            ActorUserId = actorUserId,
            OccurredAt = DateTime.UtcNow,
            PayloadJson = payload == null ? "{}" : JsonSerializer.Serialize(payload, JsonOptions)
        });
        await db.SaveChangesAsync(ct);
    }
}

public class MembershipAppService(OrganizationDbContext db, IOrganizationTimelineService timeline, LicenseAppService licenses) : IMembershipAppService
{
    public async Task<MembershipDto> CreateAsync(CreateMembershipRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        ValidateRole(request.Role);
        var effectiveFrom = request.EffectiveFrom ?? DateTime.UtcNow;
        if (request.EffectiveTo.HasValue && request.EffectiveTo <= effectiveFrom)
            throw new InvalidOperationException("EffectiveTo must be after EffectiveFrom.");

        if (request.IsPrimary)
            await EnsureNoPrimaryConflictAsync(request.UserId, request.OrganizationId, effectiveFrom, request.EffectiveTo, null, ct);

        var status = MembershipStatuses.All.Contains(request.MembershipStatus) ? request.MembershipStatus : MembershipStatuses.Active;
        var member = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            UserId = request.UserId,
            PersonId = request.PersonId,
            HospitalId = request.HospitalId,
            EmploymentId = request.EmploymentId,
            MembershipTypeValueId = request.MembershipTypeValueId,
            MembershipStatus = status,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsPrimary = request.IsPrimary,
            DepartmentId = request.DepartmentId,
            Designation = request.Designation,
            Role = RoleLegacyMap.ToCanonical(request.Role),
            Email = request.Email,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        };

        db.Members.Add(member);
        db.MembershipHistories.Add(new OrganizationMembershipHistory
        {
            Id = Guid.NewGuid(),
            MembershipId = member.Id,
            FromStatus = MembershipStatuses.Draft,
            ToStatus = status,
            Reason = "Created",
            ActorUserId = actorUserId,
            EffectiveFrom = effectiveFrom,
            RecordedAt = DateTime.UtcNow
        });
        db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = member.Id, OccurredOn = DateTime.UtcNow, Type = "MembershipCreated", Payload = JsonSerializer.Serialize(new { member.Id }) });
        await db.SaveChangesAsync(ct);
        await timeline.AppendAsync("MembershipCreated", $"Membership for user {member.UserId}", member.OrganizationId, "Membership", member.Id, actorUserId, ct: ct);
        return Map(member);
    }

    public async Task<MembershipDto?> UpdateAsync(Guid id, UpdateMembershipRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var member = await db.Members.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, ct);
        if (member == null) return null;

        var priorStatus = member.MembershipStatus;
        if (request.Role != null) { ValidateRole(request.Role); member.Role = RoleLegacyMap.ToCanonical(request.Role); }
        if (request.MembershipStatus != null && MembershipStatuses.All.Contains(request.MembershipStatus))
            member.MembershipStatus = request.MembershipStatus;
        if (request.IsPrimary == true)
            await EnsureNoPrimaryConflictAsync(member.UserId, member.OrganizationId, member.EffectiveFrom, member.EffectiveTo, member.Id, ct);
        if (request.IsPrimary.HasValue) member.IsPrimary = request.IsPrimary.Value;
        if (request.EffectiveTo.HasValue) member.EffectiveTo = request.EffectiveTo;
        if (request.HospitalId.HasValue) member.HospitalId = request.HospitalId;
        if (request.DepartmentId.HasValue) member.DepartmentId = request.DepartmentId;
        if (request.Designation != null) member.Designation = request.Designation;
        if (request.FirstName != null) member.FirstName = string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName.Trim();
        if (request.LastName != null) member.LastName = string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim();
        if (request.Email != null) member.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        if (request.PhoneNumber != null) member.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        if (priorStatus != member.MembershipStatus)
        {
            db.MembershipHistories.Add(new OrganizationMembershipHistory
            {
                Id = Guid.NewGuid(),
                MembershipId = member.Id,
                FromStatus = priorStatus,
                ToStatus = member.MembershipStatus,
                ActorUserId = actorUserId,
                EffectiveFrom = DateTime.UtcNow,
                RecordedAt = DateTime.UtcNow
            });

            if (member.MembershipStatus is MembershipStatuses.Suspended or MembershipStatuses.Archived or MembershipStatuses.Expired)
            {
                try
                {
                    await licenses.ReleaseSeatAsync(member.OrganizationId, member.UserId, ct);
                    await timeline.AppendAsync(
                        "SeatReleased",
                        member.Email ?? member.UserId.ToString(),
                        member.OrganizationId,
                        "Membership",
                        member.Id,
                        actorUserId,
                        ct: ct);
                }
                catch
                {
                    // best-effort seat release
                }
            }
        }

        await db.SaveChangesAsync(ct);
        return Map(member);
    }

    public async Task<PagedResultDto<MembershipDto>> ListAsync(
        Guid? organizationId,
        Guid? userId,
        int page,
        int pageSize,
        string? search = null,
        string? role = null,
        Guid? departmentId = null,
        string? membershipStatus = null,
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var q = db.Members.AsNoTracking().Where(m => !m.IsDeleted);
        if (!includeInactive)
            q = q.Where(m => m.MembershipStatus == MembershipStatuses.Active && m.EffectiveFrom <= now && (m.EffectiveTo == null || m.EffectiveTo > now));
        if (organizationId.HasValue) q = q.Where(m => m.OrganizationId == organizationId);
        if (userId.HasValue) q = q.Where(m => m.UserId == userId);
        if (!string.IsNullOrWhiteSpace(role)) q = q.Where(m => m.Role == RoleLegacyMap.ToCanonical(role));
        if (departmentId.HasValue) q = q.Where(m => m.DepartmentId == departmentId);
        if (!string.IsNullOrWhiteSpace(membershipStatus) && MembershipStatuses.All.Contains(membershipStatus))
            q = q.Where(m => m.MembershipStatus == membershipStatus);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(m =>
                (m.FirstName != null && m.FirstName.Contains(term)) ||
                (m.LastName != null && m.LastName.Contains(term)) ||
                (m.Email != null && m.Email.Contains(term)) ||
                (m.Designation != null && m.Designation.Contains(term)));
        }

        var total = await q.CountAsync(ct);
        var items = await (
            from m in q.OrderByDescending(m => m.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            join md in db.MasterDepartments on m.DepartmentId equals md.Id into mdJoin
            from md in mdJoin.DefaultIfEmpty()
            select Map(m, md != null ? md.Name : null)).ToListAsync(ct);
        return new PagedResultDto<MembershipDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<IReadOnlyList<MembershipHistoryDto>> GetHistoryAsync(Guid membershipId, CancellationToken ct = default)
        => await db.MembershipHistories.Where(h => h.MembershipId == membershipId).OrderByDescending(h => h.RecordedAt)
            .Select(h => new MembershipHistoryDto { Id = h.Id, MembershipId = h.MembershipId, FromStatus = h.FromStatus, ToStatus = h.ToStatus, Reason = h.Reason, RecordedAt = h.RecordedAt }).ToListAsync(ct);

    public async Task<MembershipDto> TransferAsync(TransferMembershipRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var member = await db.Members.FirstOrDefaultAsync(m => m.Id == request.MembershipId && !m.IsDeleted, ct)
            ?? throw new InvalidOperationException("Membership not found.");
        member.MembershipStatus = MembershipStatuses.Archived;
        member.EffectiveTo = DateTime.UtcNow;
        db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = member.Id, OccurredOn = DateTime.UtcNow, Type = "MembershipTransferred", Payload = "{}" });
        await db.SaveChangesAsync(ct);

        return await CreateAsync(new CreateMembershipRequest
        {
            OrganizationId = request.TargetOrganizationId,
            UserId = member.UserId,
            PersonId = member.PersonId,
            HospitalId = request.TargetHospitalId,
            EmploymentId = member.EmploymentId,
            Role = member.Role,
            IsPrimary = member.IsPrimary,
            DepartmentId = member.DepartmentId,
            Designation = member.Designation,
            Email = member.Email
        }, actorUserId, ct);
    }

    private async Task EnsureNoPrimaryConflictAsync(Guid userId, Guid organizationId, DateTime from, DateTime? to, Guid? excludeId, CancellationToken ct)
    {
        var q = db.Members.Where(m => m.UserId == userId && m.OrganizationId == organizationId && !m.IsDeleted && m.IsPrimary && m.MembershipStatus == MembershipStatuses.Active);
        if (excludeId.HasValue) q = q.Where(m => m.Id != excludeId);
        var overlap = await q.AnyAsync(m =>
            m.EffectiveFrom <= (to ?? DateTime.MaxValue) && (m.EffectiveTo == null || m.EffectiveTo > from), ct);
        if (overlap) throw new InvalidOperationException("Primary membership already exists for this organization.");
    }

    private static void ValidateRole(string role)
    {
        if (!ApplicationRoles.IsAssignableRole(role))
            throw new InvalidOperationException($"Role '{role}' is not assignable.");
    }

    private static MembershipDto Map(OrganizationMember m, string? departmentName = null) => new()
    {
        Id = m.Id, OrganizationId = m.OrganizationId, UserId = m.UserId, PersonId = m.PersonId,
        HospitalId = m.HospitalId, EmploymentId = m.EmploymentId, Role = m.Role,
        MembershipStatus = m.MembershipStatus, IsPrimary = m.IsPrimary,
        EffectiveFrom = m.EffectiveFrom, EffectiveTo = m.EffectiveTo,
        DepartmentId = m.DepartmentId, DepartmentName = departmentName, Designation = m.Designation,
        FirstName = m.FirstName, LastName = m.LastName, Email = m.Email,
        PhoneNumber = m.PhoneNumber, ProfilePicUrl = m.ProfilePicUrl, CreatedAt = m.CreatedAt
    };
}

public class InvitationAppService(
    OrganizationDbContext db,
    IMembershipAppService membership,
    IOrganizationTimelineService timeline,
    ICommunicationClient communicationClient,
    IEmailService email,
    IOptions<WorkforceOnboardingOptions> options,
    ILogger<InvitationAppService> logger) : IInvitationAppService
{
    private readonly WorkforceOnboardingOptions _opts = options.Value;

    public async Task<InvitationDto> CreateAsync(CreateInvitationRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var normalized = request.Email.Trim().ToUpperInvariant();
        if (await db.OrganizationMemberInvitations.AnyAsync(
                i => i.OrganizationId == request.OrganizationId
                     && i.NormalizedEmail == normalized
                     && i.Status == InvitationStatus.Pending, ct))
        {
            throw new InvalidOperationException("Pending invitation already exists for this email.");
        }

        var (rawToken, hash) = GenerateToken();
        var expiryDays = Math.Clamp(_opts.InvitationExpiryDays, 1, 90);
        var invite = new OrganizationMemberInvitation
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            HospitalId = request.HospitalId,
            DepartmentId = request.DepartmentId,
            Role = RoleLegacyMap.ToCanonical(request.Role),
            Email = request.Email.Trim(),
            NormalizedEmail = normalized,
            TokenHash = hash,
            ExpiresAt = request.ExpiresAt ?? DateTime.UtcNow.AddDays(expiryDays),
            Status = InvitationStatus.Pending,
            Designation = request.Designation?.Trim(),
            EmploymentType = string.IsNullOrWhiteSpace(request.EmploymentType)
                ? _opts.DefaultEmploymentType
                : request.EmploymentType.Trim(),
            ReportsToUserId = request.ReportsToUserId,
            ManagerEmploymentId = request.ManagerEmploymentId,
            RequireSeat = request.RequireSeat ?? _opts.RequireSeatDefault,
            PositionId = request.PositionId,
            InvitedByUserId = actorUserId == Guid.Empty ? null : actorUserId,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        };
        db.OrganizationMemberInvitations.Add(invite);
        await db.SaveChangesAsync(ct);
        await timeline.AppendAsync("InvitationCreated", invite.Email, request.OrganizationId, "Invitation", invite.Id, actorUserId, ct: ct);
        await SendInvitationEmailAsync(invite, rawToken, ct);
        await timeline.AppendAsync("InvitationSent", invite.Email, request.OrganizationId, "Invitation", invite.Id, actorUserId, ct: ct);
        return Map(invite);
    }

    public async Task<IReadOnlyList<InvitationDto>> BulkCreateAsync(
        BulkCreateInvitationsRequest request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        var results = new List<InvitationDto>();
        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Email))
            {
                continue;
            }

            var merged = new CreateInvitationRequest
            {
                OrganizationId = request.OrganizationId,
                HospitalId = item.HospitalId == Guid.Empty ? request.HospitalId : item.HospitalId,
                DepartmentId = item.DepartmentId,
                Role = item.Role,
                Email = item.Email,
                ExpiresAt = item.ExpiresAt,
                Designation = item.Designation,
                EmploymentType = item.EmploymentType,
                ReportsToUserId = item.ReportsToUserId,
                ManagerEmploymentId = item.ManagerEmploymentId,
                RequireSeat = item.RequireSeat,
                PositionId = item.PositionId
            };
            try
            {
                results.Add(await CreateAsync(merged, actorUserId, ct));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Bulk invite skipped for {Email}", item.Email);
            }
        }

        return results;
    }

    public async Task<PagedResultDto<InvitationDto>> ListAsync(
        Guid organizationId,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var q = db.OrganizationMemberInvitations.AsNoTracking()
            .Where(i => i.OrganizationId == organizationId);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InvitationStatus>(status, true, out var st))
        {
            q = q.Where(i => i.Status == st);
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        foreach (var i in items)
        {
            ExpireIfNeeded(i);
        }

        return new PagedResultDto<InvitationDto>
        {
            Items = items.Select(Map).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<InvitationDto?> GetAsync(Guid invitationId, CancellationToken ct = default)
    {
        var invite = await db.OrganizationMemberInvitations.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invitationId, ct);
        return invite == null ? null : Map(invite);
    }

    public async Task<InvitationRedeemPreviewDto?> PreviewByTokenAsync(string rawToken, CancellationToken ct = default)
    {
        var invite = await FindByRawTokenAsync(rawToken, ct);
        if (invite == null)
        {
            return null;
        }

        ExpireIfNeeded(invite);
        if (invite.Status != InvitationStatus.Pending)
        {
            await db.SaveChangesAsync(ct);
        }

        var orgName = await db.Organizations.AsNoTracking()
            .Where(o => o.Id == invite.OrganizationId)
            .Select(o => o.Name)
            .FirstOrDefaultAsync(ct);

        return new InvitationRedeemPreviewDto
        {
            InvitationId = invite.Id,
            Email = invite.Email,
            OrganizationId = invite.OrganizationId,
            OrganizationName = orgName,
            HospitalId = invite.HospitalId,
            Role = invite.Role,
            Designation = invite.Designation,
            Status = invite.Status.ToString(),
            ExpiresAt = invite.ExpiresAt,
            RequireSeat = invite.RequireSeat,
            PipelineStep = invite.PipelineStep
        };
    }

    public async Task<InvitationDto?> GetByTokenAsync(string rawToken, CancellationToken ct = default)
    {
        var invite = await FindByRawTokenAsync(rawToken, ct);
        if (invite == null)
        {
            return null;
        }

        ExpireIfNeeded(invite);
        await db.SaveChangesAsync(ct);
        return Map(invite);
    }

    public async Task<InvitationDto> ResendAsync(Guid invitationId, Guid actorUserId, CancellationToken ct = default)
    {
        var invite = await db.OrganizationMemberInvitations.FirstOrDefaultAsync(i => i.Id == invitationId, ct)
                     ?? throw new InvalidOperationException("Invitation not found.");
        if (invite.Status is InvitationStatus.Accepted or InvitationStatus.Revoked or InvitationStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot resend a closed invitation.");
        }

        var (rawToken, hash) = GenerateToken();
        invite.TokenHash = hash;
        invite.Status = InvitationStatus.Pending;
        invite.ExpiresAt = DateTime.UtcNow.AddDays(Math.Clamp(_opts.InvitationExpiryDays, 1, 90));
        invite.PipelineError = null;
        invite.CancelledAt = null;
        await db.SaveChangesAsync(ct);
        await SendInvitationEmailAsync(invite, rawToken, ct);
        await timeline.AppendAsync("InvitationSent", invite.Email, invite.OrganizationId, "Invitation", invite.Id, actorUserId, ct: ct);
        return Map(invite);
    }

    public async Task CancelAsync(Guid invitationId, Guid actorUserId, CancellationToken ct = default)
    {
        var invite = await db.OrganizationMemberInvitations.FirstOrDefaultAsync(i => i.Id == invitationId, ct)
                     ?? throw new InvalidOperationException("Invitation not found.");
        if (invite.Status == InvitationStatus.Accepted)
        {
            throw new InvalidOperationException("Cannot cancel an accepted invitation.");
        }

        invite.Status = InvitationStatus.Cancelled;
        invite.CancelledAt = DateTime.UtcNow;
        invite.TokenHash = null;
        await db.SaveChangesAsync(ct);
        await timeline.AppendAsync("InvitationCancelled", invite.Email, invite.OrganizationId, "Invitation", invite.Id, actorUserId, ct: ct);
    }

    public async Task<MembershipDto> AcceptAsync(Guid invitationId, Guid userId, CancellationToken ct = default)
    {
        var invite = await db.OrganizationMemberInvitations.FirstOrDefaultAsync(i => i.Id == invitationId, ct)
                     ?? throw new InvalidOperationException("Invitation not found.");
        ExpireIfNeeded(invite);
        if (invite.Status != InvitationStatus.Pending)
        {
            throw new InvalidOperationException("Invitation is not pending.");
        }

        if (invite.MembershipId is { } existingMemberId && existingMemberId != Guid.Empty)
        {
            var existing = await db.Members.AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == existingMemberId, ct);
            if (existing != null)
            {
                return MapMembership(existing);
            }
        }

        invite.PipelineStep = "MembershipCreate";
        invite.AcceptedUserId = userId;
        await db.SaveChangesAsync(ct);

        MembershipDto member;
        try
        {
            member = await membership.CreateAsync(new CreateMembershipRequest
            {
                OrganizationId = invite.OrganizationId,
                UserId = userId,
                PersonId = invite.PersonId,
                HospitalId = invite.HospitalId,
                DepartmentId = invite.DepartmentId,
                Role = invite.Role,
                Email = invite.Email,
                Designation = invite.Designation,
                MembershipStatus = MembershipStatuses.Active,
                EmploymentId = invite.EmploymentId
            }, userId, ct);
        }
        catch (Exception ex)
        {
            invite.PipelineError = ex.Message;
            await db.SaveChangesAsync(ct);
            throw;
        }

        invite.MembershipId = member.Id;
        invite.PipelineStep = "MembershipCreated";
        invite.PipelineError = null;
        await db.SaveChangesAsync(ct);
        await timeline.AppendAsync("MembershipCreated", invite.Email, invite.OrganizationId, "Membership", member.Id, userId, ct: ct);
        return member;
    }

    public async Task RevokeAsync(Guid invitationId, Guid actorUserId, CancellationToken ct = default)
    {
        var invite = await db.OrganizationMemberInvitations.FirstOrDefaultAsync(i => i.Id == invitationId, ct)
                     ?? throw new InvalidOperationException("Invitation not found.");
        invite.Status = InvitationStatus.Revoked;
        invite.TokenHash = null;
        await db.SaveChangesAsync(ct);
        await timeline.AppendAsync("InvitationRevoked", invite.Email, invite.OrganizationId, "Invitation", invite.Id, actorUserId, ct: ct);
    }

    public async Task<InvitationDto> UpdatePipelineAsync(
        Guid invitationId,
        InvitationPipelineUpdateRequest request,
        CancellationToken ct = default)
    {
        var invite = await db.OrganizationMemberInvitations.FirstOrDefaultAsync(i => i.Id == invitationId, ct)
                     ?? throw new InvalidOperationException("Invitation not found.");

        if (request.PipelineStep != null)
        {
            invite.PipelineStep = request.PipelineStep;
        }

        if (request.PipelineError != null)
        {
            invite.PipelineError = request.PipelineError;
        }

        if (request.MembershipId.HasValue)
        {
            invite.MembershipId = request.MembershipId;
        }

        if (request.EmploymentId.HasValue)
        {
            invite.EmploymentId = request.EmploymentId;
        }

        if (request.PersonId.HasValue)
        {
            invite.PersonId = request.PersonId;
        }

        if (request.SeatAssigned.HasValue)
        {
            invite.SeatAssigned = request.SeatAssigned.Value;
        }

        if (request.AcceptedUserId.HasValue)
        {
            invite.AcceptedUserId = request.AcceptedUserId;
        }

        if (request.MarkAccepted)
        {
            invite.Status = InvitationStatus.Accepted;
            invite.AcceptedAt = DateTime.UtcNow;
            invite.TokenHash = null;
            invite.PipelineStep = "EmployeeActivated";
            invite.PipelineError = null;
            await timeline.AppendAsync(
                "InvitationAccepted",
                invite.Email,
                invite.OrganizationId,
                "Invitation",
                invite.Id,
                invite.AcceptedUserId,
                ct: ct);
            await timeline.AppendAsync(
                "EmployeeActivated",
                invite.Email,
                invite.OrganizationId,
                "Invitation",
                invite.Id,
                invite.AcceptedUserId,
                ct: ct);
        }

        await db.SaveChangesAsync(ct);
        return Map(invite);
    }

    public async Task LinkEmploymentAsync(
        Guid membershipId,
        Guid employmentId,
        Guid? personId,
        CancellationToken ct = default)
    {
        var member = await db.Members.FirstOrDefaultAsync(m => m.Id == membershipId, ct)
                     ?? throw new InvalidOperationException("Membership not found.");
        member.EmploymentId = employmentId;
        if (personId.HasValue)
        {
            member.PersonId = personId;
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task SendInvitationEmailAsync(
        OrganizationMemberInvitation invite,
        string rawToken,
        CancellationToken ct)
    {
        var orgName = await db.Organizations.AsNoTracking()
            .Where(o => o.Id == invite.OrganizationId)
            .Select(o => o.Name)
            .FirstOrDefaultAsync(ct) ?? "your organization";
        var baseUrl = _opts.PublicAppBaseUrl.TrimEnd('/');
        var path = string.IsNullOrWhiteSpace(_opts.AcceptPath) ? "/invitations/accept" : _opts.AcceptPath;
        var inviteUrl = $"{baseUrl}{path}?token={Uri.EscapeDataString(rawToken)}";
        var (subject, html) = IdentityEmailTemplates.Invitation(inviteUrl, orgName);
        try
        {
            await email.SendAsync(invite.Email, subject, html);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Direct invitation email failed for {Email}; trying Communication", invite.Email);
        }

        try
        {
            await communicationClient.SendAsync(new CommunicationRequestDto
            {
                ModuleCode = "Organization",
                EventType = "InvitationCreated",
                RecipientEmail = invite.Email,
                TemplateCode = "member-invitation",
                Variables = new Dictionary<string, string>
                {
                    ["email"] = invite.Email,
                    ["invitationId"] = invite.Id.ToString(),
                    ["inviteUrl"] = inviteUrl,
                    ["organizationName"] = orgName,
                    ["message"] = $"You have been invited to join {orgName}."
                },
                DedupeKey = $"InvitationCreated:{invite.Id}:{invite.ExpiresAt:O}"
            }, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Communication invitation notify failed for {Id}", invite.Id);
        }
    }

    private async Task<OrganizationMemberInvitation?> FindByRawTokenAsync(string rawToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var hash = HashToken(rawToken.Trim());
        return await db.OrganizationMemberInvitations
            .FirstOrDefaultAsync(i => i.TokenHash == hash, ct);
    }

    private static void ExpireIfNeeded(OrganizationMemberInvitation invite)
    {
        if (invite.Status == InvitationStatus.Pending && invite.ExpiresAt < DateTime.UtcNow)
        {
            invite.Status = InvitationStatus.Expired;
            invite.TokenHash = null;
        }
    }

    private static (string Raw, string Hash) GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (raw, HashToken(raw));
    }

    private static string HashToken(string raw)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash);
    }

    private static InvitationDto Map(OrganizationMemberInvitation i) => new()
    {
        Id = i.Id,
        OrganizationId = i.OrganizationId,
        HospitalId = i.HospitalId,
        DepartmentId = i.DepartmentId,
        Email = i.Email,
        Role = i.Role,
        Status = i.Status.ToString(),
        ExpiresAt = i.ExpiresAt,
        Designation = i.Designation,
        EmploymentType = i.EmploymentType,
        RequireSeat = i.RequireSeat,
        PositionId = i.PositionId,
        ReportsToUserId = i.ReportsToUserId,
        ManagerEmploymentId = i.ManagerEmploymentId,
        PipelineStep = i.PipelineStep,
        PipelineError = i.PipelineError,
        MembershipId = i.MembershipId,
        EmploymentId = i.EmploymentId,
        PersonId = i.PersonId,
        SeatAssigned = i.SeatAssigned,
        AcceptedAt = i.AcceptedAt,
        AcceptedUserId = i.AcceptedUserId,
        CreatedAt = i.CreatedAt
    };

    private static MembershipDto MapMembership(OrganizationMember m) => new()
    {
        Id = m.Id,
        OrganizationId = m.OrganizationId,
        UserId = m.UserId,
        PersonId = m.PersonId,
        HospitalId = m.HospitalId,
        EmploymentId = m.EmploymentId,
        Role = m.Role,
        MembershipStatus = m.MembershipStatus,
        IsPrimary = m.IsPrimary,
        EffectiveFrom = m.EffectiveFrom,
        EffectiveTo = m.EffectiveTo,
        DepartmentId = m.DepartmentId,
        Designation = m.Designation,
        FirstName = m.FirstName,
        LastName = m.LastName,
        Email = m.Email,
        PhoneNumber = m.PhoneNumber,
        ProfilePicUrl = m.ProfilePicUrl,
        CreatedAt = m.CreatedAt
    };
}

public class OrganizationalUnitAppService(OrganizationDbContext db, IOrganizationTimelineService timeline) : IOrganizationalUnitAppService
{
    public async Task<OrganizationalUnitDto> CreateAsync(CreateOrganizationalUnitRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var typeMap = await LoadUnitTypeMapAsync(ct);
        if (!typeMap.TryGetValue(request.UnitTypeValueId, out var childType))
            throw new InvalidOperationException("Unit type not found.");

        string? parentTypeCode = null;
        if (request.ParentUnitId.HasValue)
        {
            var parent = await db.OrganizationalUnits.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.ParentUnitId && u.OrganizationId == request.OrganizationId && !u.IsDeleted, ct)
                ?? throw new InvalidOperationException("Parent organizational unit not found.");
            if (!typeMap.TryGetValue(parent.UnitTypeValueId, out var parentType))
                throw new InvalidOperationException("Parent unit type not found.");
            parentTypeCode = parentType.Code;
        }

        OrganizationalUnitHierarchyPolicy.EnsureAllowedParentChild(parentTypeCode, childType.Code);

        var unit = new OrganizationalUnit
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            HospitalId = request.HospitalId,
            ParentUnitId = request.ParentUnitId,
            UnitTypeValueId = request.UnitTypeValueId,
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            Status = "Active",
            EffectiveFrom = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        db.OrganizationalUnits.Add(unit);
        db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = unit.Id, OccurredOn = DateTime.UtcNow, Type = "OrganizationHierarchyChanged", Payload = "{}" });
        await db.SaveChangesAsync(ct);
        await timeline.AppendAsync("OrganizationHierarchyChanged", $"Created {childType.Code} '{unit.Name}'", request.OrganizationId, "OrganizationalUnit", unit.Id, actorUserId, ct: ct);
        return MapDto(unit, childType, Array.Empty<OrganizationalUnitDto>());
    }

    public async Task<OrganizationalUnitDto> UpdateAsync(Guid id, UpdateOrganizationalUnitRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var unit = await db.OrganizationalUnits.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct)
            ?? throw new InvalidOperationException("Organizational unit not found.");
        var typeMap = await LoadUnitTypeMapAsync(ct);

        if (request.UnitTypeValueId.HasValue && request.UnitTypeValueId.Value != unit.UnitTypeValueId)
        {
            if (!typeMap.TryGetValue(request.UnitTypeValueId.Value, out var newType))
                throw new InvalidOperationException("Unit type not found.");
            string? parentTypeCode = null;
            if (unit.ParentUnitId.HasValue)
            {
                var parent = await db.OrganizationalUnits.AsNoTracking()
                    .FirstAsync(u => u.Id == unit.ParentUnitId && !u.IsDeleted, ct);
                parentTypeCode = typeMap[parent.UnitTypeValueId].Code;
            }
            OrganizationalUnitHierarchyPolicy.EnsureAllowedParentChild(parentTypeCode, newType.Code);
            unit.UnitTypeValueId = request.UnitTypeValueId.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Code)) unit.Code = request.Code.Trim();
        if (!string.IsNullOrWhiteSpace(request.Name)) unit.Name = request.Name.Trim();
        if (request.HospitalId.HasValue) unit.HospitalId = request.HospitalId;

        await db.SaveChangesAsync(ct);
        await timeline.AppendAsync("OrganizationHierarchyChanged", $"Updated '{unit.Name}'", unit.OrganizationId, "OrganizationalUnit", unit.Id, actorUserId, ct: ct);
        return MapDto(unit, typeMap[unit.UnitTypeValueId], Array.Empty<OrganizationalUnitDto>());
    }

    public async Task<OrganizationalUnitDto> MoveAsync(Guid id, MoveOrganizationalUnitRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var unit = await db.OrganizationalUnits.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct)
            ?? throw new InvalidOperationException("Organizational unit not found.");
        var typeMap = await LoadUnitTypeMapAsync(ct);
        var childType = typeMap[unit.UnitTypeValueId];

        string? parentTypeCode = null;
        if (request.NewParentUnitId.HasValue)
        {
            var parent = await db.OrganizationalUnits.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.NewParentUnitId && u.OrganizationId == unit.OrganizationId && !u.IsDeleted, ct)
                ?? throw new InvalidOperationException("New parent organizational unit not found.");
            parentTypeCode = typeMap[parent.UnitTypeValueId].Code;
        }

        OrganizationalUnitHierarchyPolicy.EnsureAllowedParentChild(parentTypeCode, childType.Code);

        var parents = await db.OrganizationalUnits
            .Where(u => u.OrganizationId == unit.OrganizationId && !u.IsDeleted)
            .ToDictionaryAsync(u => u.Id, u => u.ParentUnitId, ct);
        HierarchyValidator.EnsureNoCircularParent(parents, unit.Id, request.NewParentUnitId);

        unit.ParentUnitId = request.NewParentUnitId;
        await db.SaveChangesAsync(ct);
        await timeline.AppendAsync("OrganizationHierarchyChanged", $"Moved '{unit.Name}'", unit.OrganizationId, "OrganizationalUnit", unit.Id, actorUserId, ct: ct);
        return MapDto(unit, childType, Array.Empty<OrganizationalUnitDto>());
    }

    public async Task<OrganizationalUnitDto> SetStatusAsync(Guid id, string status, Guid actorUserId, CancellationToken ct = default)
    {
        var normalized = status?.Trim() ?? string.Empty;
        if (!string.Equals(normalized, "Active", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(normalized, "Inactive", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Status must be Active or Inactive.");

        var unit = await db.OrganizationalUnits.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct)
            ?? throw new InvalidOperationException("Organizational unit not found.");
        unit.Status = string.Equals(normalized, "Active", StringComparison.OrdinalIgnoreCase) ? "Active" : "Inactive";
        unit.EffectiveTo = unit.Status == "Inactive" ? DateTime.UtcNow : null;
        if (unit.Status == "Active") unit.EffectiveFrom ??= DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await timeline.AppendAsync("OrganizationHierarchyChanged", $"Set status {unit.Status} on '{unit.Name}'", unit.OrganizationId, "OrganizationalUnit", unit.Id, actorUserId, ct: ct);
        var typeMap = await LoadUnitTypeMapAsync(ct);
        return MapDto(unit, typeMap[unit.UnitTypeValueId], Array.Empty<OrganizationalUnitDto>());
    }

    public async Task SoftDeleteAsync(Guid id, Guid actorUserId, CancellationToken ct = default)
    {
        var unit = await db.OrganizationalUnits.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, ct)
            ?? throw new InvalidOperationException("Organizational unit not found.");
        var hasChildren = await db.OrganizationalUnits.AnyAsync(u => u.ParentUnitId == id && !u.IsDeleted, ct);
        if (hasChildren)
            throw new InvalidOperationException("Cannot delete a unit that still has children. Move or delete children first.");

        unit.IsDeleted = true;
        unit.Status = "Inactive";
        unit.EffectiveTo = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await timeline.AppendAsync("OrganizationHierarchyChanged", $"Deleted '{unit.Name}'", unit.OrganizationId, "OrganizationalUnit", unit.Id, actorUserId, ct: ct);
    }

    public async Task<IReadOnlyList<OrganizationalUnitDto>> GetTreeAsync(Guid organizationId, Guid? hospitalId, bool includeInactive = false, CancellationToken ct = default)
    {
        var q = db.OrganizationalUnits.AsNoTracking().Where(u => u.OrganizationId == organizationId && !u.IsDeleted);
        if (hospitalId.HasValue) q = q.Where(u => u.HospitalId == null || u.HospitalId == hospitalId);
        if (!includeInactive) q = q.Where(u => u.Status == "Active");
        var units = await q.ToListAsync(ct);
        var typeMap = await LoadUnitTypeMapAsync(ct);
        return BuildUnitTree(units, null, typeMap);
    }

    public async Task<IReadOnlyList<OrganizationalUnitTypeDto>> GetUnitTypesAsync(CancellationToken ct = default)
    {
        var category = await db.LookupCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Code == "UnitType", ct)
            ?? throw new InvalidOperationException("UnitType lookup category is not seeded.");
        return await db.LookupValues.AsNoTracking()
            .Where(v => v.CategoryId == category.Id)
            .OrderBy(v => v.SortOrder)
            .Select(v => new OrganizationalUnitTypeDto { Id = v.Id, Code = v.Code, Name = v.Name, SortOrder = v.SortOrder })
            .ToListAsync(ct);
    }

    private async Task<Dictionary<Guid, OrganizationLookupValue>> LoadUnitTypeMapAsync(CancellationToken ct)
    {
        var categoryId = await db.LookupCategories.AsNoTracking()
            .Where(c => c.Code == "UnitType")
            .Select(c => c.Id)
            .FirstOrDefaultAsync(ct);
        if (categoryId == Guid.Empty)
            throw new InvalidOperationException("UnitType lookup category is not seeded.");
        return await db.LookupValues.AsNoTracking()
            .Where(v => v.CategoryId == categoryId)
            .ToDictionaryAsync(v => v.Id, ct);
    }

    private static List<OrganizationalUnitDto> BuildUnitTree(
        List<OrganizationalUnit> units,
        Guid? parentId,
        IReadOnlyDictionary<Guid, OrganizationLookupValue> typeMap)
        => units.Where(u => u.ParentUnitId == parentId)
            .OrderBy(u => u.Name)
            .Select(u =>
            {
                typeMap.TryGetValue(u.UnitTypeValueId, out var type);
                return MapDto(u, type, BuildUnitTree(units, u.Id, typeMap));
            }).ToList();

    private static OrganizationalUnitDto MapDto(
        OrganizationalUnit u,
        OrganizationLookupValue? type,
        IReadOnlyList<OrganizationalUnitDto> children)
        => new()
        {
            Id = u.Id,
            OrganizationId = u.OrganizationId,
            HospitalId = u.HospitalId,
            Code = u.Code,
            Name = u.Name,
            ParentUnitId = u.ParentUnitId,
            UnitTypeValueId = u.UnitTypeValueId,
            UnitTypeCode = type?.Code ?? string.Empty,
            UnitTypeName = type?.Name ?? string.Empty,
            Status = u.Status,
            Children = children
        };
}

public class DepartmentHierarchyAppService(OrganizationDbContext db, IOrganizationTimelineService timeline) : IDepartmentHierarchyAppService
{
    public async Task<DepartmentTreeNodeDto> CreateAsync(CreateOrgDepartmentRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var deptId = Guid.NewGuid();
        if (request.ParentDepartmentId.HasValue)
        {
            if (!await db.Departments.AnyAsync(d => d.Id == request.ParentDepartmentId && d.OrganizationId == request.OrganizationId && !d.IsDeleted, ct))
                throw new InvalidOperationException("Parent department not found.");
            var parents = await db.Departments.Where(d => d.OrganizationId == request.OrganizationId && !d.IsDeleted)
                .ToDictionaryAsync(d => d.Id, d => d.ParentDepartmentId, ct);
            parents[deptId] = request.ParentDepartmentId;
            HierarchyValidator.EnsureNoCircularParent(parents, deptId, request.ParentDepartmentId);
        }

        var dept = new Department
        {
            Id = deptId,
            OrganizationId = request.OrganizationId,
            HospitalId = request.HospitalId,
            ParentDepartmentId = request.ParentDepartmentId,
            DepartmentTypeValueId = request.DepartmentTypeValueId,
            Code = request.Code.Trim(),
            Name = request.Name.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Departments.Add(dept);
        db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = dept.Id, OccurredOn = DateTime.UtcNow, Type = "DepartmentCreated", Payload = "{}" });
        await db.SaveChangesAsync(ct);
        await timeline.AppendAsync("DepartmentCreated", dept.Name, request.OrganizationId, "Department", dept.Id, actorUserId, ct: ct);
        return new DepartmentTreeNodeDto { Id = dept.Id, Code = dept.Code, Name = dept.Name };
    }

    public async Task<IntegrationUpsertResponse> UpsertFromIntegrationAsync(IntegrationUpsertRequest request, CancellationToken ct = default)
    {
        var code = GetField(request.Fields, "code", "Code") ?? request.ExternalId
            ?? throw new InvalidOperationException("Department code is required.");
        var name = GetField(request.Fields, "name", "Name") ?? code;
        var hospitalId = ParseGuid(GetField(request.Fields, "hospitalId", "HospitalId"));
        var parentId = ParseGuid(GetField(request.Fields, "parentDepartmentId", "ParentDepartmentId"));

        var dept = await db.Departments.FirstOrDefaultAsync(d =>
            d.OrganizationId == request.OrganizationId && !d.IsDeleted && d.Code == code.Trim(), ct);

        if (dept is null)
        {
            var created = await CreateAsync(new CreateOrgDepartmentRequest
            {
                OrganizationId = request.OrganizationId,
                HospitalId = hospitalId,
                ParentDepartmentId = parentId,
                Code = code.Trim(),
                Name = name.Trim()
            }, Guid.Empty, ct);
            return new IntegrationUpsertResponse { EntityId = created.Id, WasCreated = true };
        }

        dept.Name = name.Trim();
        dept.HospitalId = hospitalId ?? dept.HospitalId;
        dept.ParentDepartmentId = parentId ?? dept.ParentDepartmentId;
        dept.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new IntegrationUpsertResponse { EntityId = dept.Id, WasCreated = false };
    }

    public async Task<bool> RollbackIntegrationAsync(Guid organizationId, Guid departmentId, CancellationToken ct = default)
    {
        var dept = await db.Departments.FirstOrDefaultAsync(d =>
            d.Id == departmentId && d.OrganizationId == organizationId && !d.IsDeleted, ct);
        if (dept is null) return false;
        dept.IsDeleted = true;
        dept.IsActive = false;
        dept.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static string? GetField(Dictionary<string, string?> fields, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (fields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }
        return null;
    }

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out var id) ? id : null;

    public async Task<IReadOnlyList<DepartmentTreeNodeDto>> GetTreeAsync(Guid organizationId, Guid? hospitalId, CancellationToken ct = default)
    {
        var q = db.Departments.Where(d => d.OrganizationId == organizationId && !d.IsDeleted);
        if (hospitalId.HasValue) q = q.Where(d => d.HospitalId == null || d.HospitalId == hospitalId);
        var depts = await q.ToListAsync(ct);
        return BuildDeptTree(depts, null);
    }

    private static List<DepartmentTreeNodeDto> BuildDeptTree(List<Department> depts, Guid? parentId)
        => depts.Where(d => d.ParentDepartmentId == parentId).Select(d => new DepartmentTreeNodeDto
        {
            Id = d.Id,
            Code = d.Code,
            Name = d.Name,
            IsActive = d.IsActive,
            HospitalId = d.HospitalId,
            ParentDepartmentId = d.ParentDepartmentId,
            Children = BuildDeptTree(depts, d.Id)
        }).ToList();
}

public class OrganizationGraphAppService(OrganizationDbContext db) : IOrganizationGraphAppService
{
    public async Task<OrganizationGraphDto> GetGraphAsync(Guid organizationId, CancellationToken ct = default)
    {
        var nodes = new List<GraphNodeDto>();
        var edges = new List<GraphEdgeDto>();

        var org = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == organizationId && !o.IsDeleted, ct);
        if (org == null) return new OrganizationGraphDto();
        var orgNode = $"org:{org.Id}";
        nodes.Add(new GraphNodeDto { Id = orgNode, Label = org.Name, NodeType = "Organization" });

        var hospitals = await db.Hospitals.AsNoTracking().Where(h => h.OrganizationId == organizationId && !h.IsDeleted).ToListAsync(ct);
        foreach (var h in hospitals)
        {
            var hNode = $"hospital:{h.Id}";
            nodes.Add(new GraphNodeDto { Id = hNode, Label = h.Name, NodeType = "Hospital" });
            edges.Add(new GraphEdgeDto { From = orgNode, To = hNode, Relation = "has" });
        }

        var members = await db.Members.AsNoTracking().Where(m => m.OrganizationId == organizationId && !m.IsDeleted && m.MembershipStatus == MembershipStatuses.Active).Take(200).ToListAsync(ct);
        foreach (var m in members)
        {
            var mNode = $"member:{m.Id}";
            nodes.Add(new GraphNodeDto { Id = mNode, Label = m.UserId.ToString(), NodeType = "Membership" });
            edges.Add(new GraphEdgeDto { From = orgNode, To = mNode, Relation = "member" });
            if (m.HospitalId.HasValue)
                edges.Add(new GraphEdgeDto { From = mNode, To = $"hospital:{m.HospitalId}", Relation = "affiliated" });
        }

        return new OrganizationGraphDto { Nodes = nodes, Edges = edges };
    }

    public async Task<OrganizationTreeNodeDto?> GetOrganizationTreeAsync(Guid organizationId, CancellationToken ct = default)
    {
        var org = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == organizationId && !o.IsDeleted, ct);
        if (org == null) return null;
        var hospitals = await db.Hospitals.AsNoTracking().Where(h => h.OrganizationId == organizationId && !h.IsDeleted).ToListAsync(ct);
        var units = await db.OrganizationalUnits.AsNoTracking().Where(u => u.OrganizationId == organizationId && !u.IsDeleted).ToListAsync(ct);

        return new OrganizationTreeNodeDto
        {
            Id = org.Id,
            Name = org.Name,
            NodeType = "Organization",
            Children = hospitals.Select(h => new OrganizationTreeNodeDto
            {
                Id = h.Id,
                Name = h.Name,
                NodeType = "Hospital",
                Children = units.Where(u => u.HospitalId == h.Id && u.ParentUnitId == null).Select(u => MapUnitTree(u, units)).ToList()
            }).ToList()
        };
    }

    private static OrganizationTreeNodeDto MapUnitTree(OrganizationalUnit unit, List<OrganizationalUnit> all)
        => new()
        {
            Id = unit.Id,
            Name = unit.Name,
            NodeType = "OrganizationalUnit",
            Children = all.Where(u => u.ParentUnitId == unit.Id).Select(u => MapUnitTree(u, all)).ToList()
        };
}
