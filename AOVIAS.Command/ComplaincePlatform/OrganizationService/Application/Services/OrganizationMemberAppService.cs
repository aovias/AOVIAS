using OrganizationService.Application.Contracts;
using OrganizationService.Application.DTOs;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Services;
using System.Linq;

namespace OrganizationService.Application.Services;

public class OrganizationMemberAppService
{
    private readonly IOrganizationMemberRepository _repo;
    private readonly IUnitOfWork _uow;

    public OrganizationMemberAppService(
        IOrganizationMemberRepository repo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<OrganizationMember> CreateAsync(CreateOrganizationMemberDto dto)
    {
        // Check if member already exists
        var existing = await _repo.GetByUserAndOrganizationAsync(dto.UserId, dto.OrganizationId);
        if (existing != null)
        {
            throw new InvalidOperationException($"User is already a member of this organization.");
        }

        var normalizedEmail = NormalizeEmail(dto.Email);
        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            var emailConflict = await _repo.GetByEmailAsync(dto.OrganizationId, normalizedEmail);
            if (emailConflict != null)
            {
                throw new InvalidOperationException("A member with this email already exists in the organization.");
            }
        }

        ValidateMemberRole(dto.Role);
        var canonicalRole = RoleLegacyMap.ToCanonical(dto.Role);

        var member = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = dto.OrganizationId,
            UserId = dto.UserId,
            DepartmentId = dto.DepartmentId,
            Designation = NormalizeOptional(dto.Designation),
            ReportsToUserId = dto.ReportsToUserId,
            Role = canonicalRole,
            FirstName = NormalizeOptional(dto.FirstName),
            LastName = NormalizeOptional(dto.LastName),
            Email = normalizedEmail,
            PhoneNumber = NormalizePhone(dto.PhoneNumber),
            AlternatePhoneNumber = NormalizePhone(dto.AlternatePhoneNumber),
            ProfilePicUrl = NormalizeOptional(dto.ProfilePicUrl),
            Preferences = NormalizeOptional(dto.Preferences),
            MembershipStatus = Domain.Enums.MembershipStatuses.Active,
            EffectiveFrom = DateTime.UtcNow,
            IsPrimary = true,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8]
        };

        await _repo.AddAsync(member);
        await _uow.SaveChangesAsync();
        return member;
    }

    public async Task<OrganizationMember?> UpdateAsync(Guid memberId, UpdateOrganizationMemberDto dto, CancellationToken cancellationToken = default)
    {
        var member = await _repo.GetByIdAsync(memberId, cancellationToken);
        if (member == null)
        {
            return null;
        }

        if (dto.Role != null)
        {
            ValidateMemberRole(dto.Role);
            member.Role = RoleLegacyMap.ToCanonical(dto.Role);
        }
        if (dto.DepartmentId.HasValue)
        {
            member.DepartmentId = dto.DepartmentId;
        }
        if (dto.Designation != null)
        {
            member.Designation = NormalizeOptional(dto.Designation);
        }
        if (dto.ReportsToUserId.HasValue)
        {
            member.ReportsToUserId = dto.ReportsToUserId;
        }
        if (dto.FirstName != null)
        {
            member.FirstName = NormalizeOptional(dto.FirstName);
        }
        if (dto.LastName != null)
        {
            member.LastName = NormalizeOptional(dto.LastName);
        }
        if (dto.Email != null)
        {
            var normalizedEmail = NormalizeEmail(dto.Email);
            if (!string.IsNullOrWhiteSpace(normalizedEmail))
            {
                var emailConflict = await _repo.GetByEmailAsync(member.OrganizationId, normalizedEmail, member.Id, cancellationToken);
                if (emailConflict != null)
                {
                    throw new InvalidOperationException("A member with this email already exists in the organization.");
                }
            }
            member.Email = normalizedEmail;
        }
        if (dto.PhoneNumber != null)
        {
            member.PhoneNumber = NormalizePhone(dto.PhoneNumber);
        }
        if (dto.AlternatePhoneNumber != null)
        {
            member.AlternatePhoneNumber = NormalizePhone(dto.AlternatePhoneNumber);
        }
        if (dto.ProfilePicUrl != null)
        {
            member.ProfilePicUrl = NormalizeOptional(dto.ProfilePicUrl);
        }
        if (dto.Preferences != null)
        {
            member.Preferences = NormalizeOptional(dto.Preferences);
        }

        _repo.Update(member);
        await _uow.SaveChangesAsync();
        return member;
    }

    public async Task<OrganizationMember?> GetAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        return await _repo.GetByIdAsync(memberId, cancellationToken);
    }

    public async Task<List<OrganizationMember>> ListByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _repo.ListByOrganizationAsync(organizationId, cancellationToken);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeEmail(string? value)
    {
        var normalized = NormalizeOptional(value);
        return normalized?.ToLowerInvariant();
    }

    private static void ValidateMemberRole(string role)
    {
        var canonical = RoleLegacyMap.ToCanonical(role);
        if (!ApplicationRoles.IsAssignableRole(canonical))
            throw new InvalidOperationException("Invalid organization member role.");
    }

    private static string? NormalizePhone(string? value)
    {
        var normalized = NormalizeOptional(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return new string(normalized.Where(c => char.IsDigit(c) || c == '+').ToArray());
    }
}
