using IdentityService.Domain;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain;

namespace IdentityService.Infrastructure;

public class IdentityDbContext : Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<PlatformSequence> PlatformSequences { get; set; } = default!;
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<PasswordHistory> PasswordHistory => Set<PasswordHistory>();
    public DbSet<IdentityAuditLog> IdentityAuditLogs => Set<IdentityAuditLog>();
    public DbSet<UserSecurityPolicy> UserSecurityPolicies => Set<UserSecurityPolicy>();
    public DbSet<TrustedDevice> TrustedDevices => Set<TrustedDevice>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<RevokedAccessToken> RevokedAccessTokens => Set<RevokedAccessToken>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<MfaChallenge> MfaChallenges => Set<MfaChallenge>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    public DbSet<UserRiskScore> UserRiskScores => Set<UserRiskScore>();
    public DbSet<SessionHistory> SessionHistories => Set<SessionHistory>();
    public DbSet<SecurityIncident> SecurityIncidents => Set<SecurityIncident>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(entity =>
        {
            entity.Property(u => u.PlatformId).HasMaxLength(16).IsRequired();
            entity.HasIndex(u => u.PlatformId).IsUnique();
            entity.Property(u => u.Status).HasConversion<int>();
        });

        builder.Entity<UserSession>(entity =>
        {
            entity.ToTable("UserSessions");
            entity.HasKey(x => x.SessionId);
            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.RefreshTokenHash).IsUnique();
            entity.HasIndex(x => x.TokenFamilyId);
            entity.Property(x => x.Status).HasConversion<int>();
        });

        builder.Entity<LoginAttempt>(entity =>
        {
            entity.ToTable("LoginAttempts");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.HasIndex(x => new { x.Email, x.CreatedAt });
        });

        builder.Entity<PasswordHistory>(entity =>
        {
            entity.ToTable("PasswordHistory");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId);
        });

        builder.Entity<IdentityAuditLog>(entity =>
        {
            entity.ToTable("IdentityAuditLogs");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.OccurredAt);
            entity.HasIndex(x => x.TargetUserId);
        });

        builder.Entity<UserSecurityPolicy>(entity =>
        {
            entity.ToTable("UserSecurityPolicies");
            entity.HasKey(x => x.Id);
        });

        builder.Entity<TrustedDevice>(entity =>
        {
            entity.ToTable("TrustedDevices");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.DeviceId }).IsUnique();
            entity.Property(x => x.RiskLevel).HasConversion<int>();
            entity.Property(x => x.VerificationStatus).HasConversion<int>();
        });

        builder.Entity<EmailVerificationToken>(entity =>
        {
            entity.ToTable("EmailVerificationTokens");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId);
        });

        builder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("PasswordResetTokens");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId);
        });

        builder.Entity<RevokedAccessToken>(entity =>
        {
            entity.ToTable("RevokedAccessTokens");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Jti).IsUnique();
        });

        builder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ProcessedOn);
        });

        builder.Entity<MfaChallenge>(entity =>
        {
            entity.ToTable("MfaChallenges");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId);
        });

        builder.Entity<SecurityEvent>(entity =>
        {
            entity.ToTable("SecurityEvents");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.OccurredAt });
            entity.Property(x => x.Severity).HasConversion<int>();
        });

        builder.Entity<UserRiskScore>(entity =>
        {
            entity.ToTable("UserRiskScores");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.Property(x => x.Level).HasConversion<int>();
        });

        builder.Entity<SessionHistory>(entity =>
        {
            entity.ToTable("SessionHistories");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.OccurredAt });
            entity.Property(x => x.Action).HasConversion<int>();
        });

        builder.Entity<SecurityIncident>(entity =>
        {
            entity.ToTable("SecurityIncidents");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.UserId);
            entity.Property(x => x.Status).HasConversion<int>();
        });

        builder.Entity<PlatformSequence>(entity =>
        {
            entity.ToTable("PlatformSequences");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Prefix).IsUnique();
        });
    }
}
