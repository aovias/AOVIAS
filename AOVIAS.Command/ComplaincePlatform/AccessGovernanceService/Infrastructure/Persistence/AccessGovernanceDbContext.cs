using AccessGovernanceService.Domain.Aggregates;
using AccessGovernanceService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain;

namespace AccessGovernanceService.Infrastructure.Persistence;

public class AccessGovernanceDbContext : DbContext
{
    public AccessGovernanceDbContext(DbContextOptions<AccessGovernanceDbContext> options) : base(options) { }

    public DbSet<RoleDefinition> RoleDefinitions => Set<RoleDefinition>();
    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<PermissionAssignment> PermissionAssignments => Set<PermissionAssignment>();
    public DbSet<AuthorizationPolicy> AuthorizationPolicies => Set<AuthorizationPolicy>();
    public DbSet<PolicyRule> PolicyRules => Set<PolicyRule>();
    public DbSet<PolicyCondition> PolicyConditions => Set<PolicyCondition>();
    public DbSet<Delegation> Delegations => Set<Delegation>();
    public DbSet<DelegationHistory> DelegationHistories => Set<DelegationHistory>();
    public DbSet<ResponsibilityAssignment> ResponsibilityAssignments => Set<ResponsibilityAssignment>();
    public DbSet<ApprovalAuthority> ApprovalAuthorities => Set<ApprovalAuthority>();
    public DbSet<AccessEvaluation> AccessEvaluations => Set<AccessEvaluation>();
    public DbSet<AccessAuditLog> AccessAuditLogs => Set<AccessAuditLog>();
    public DbSet<AccessGovernanceLookupCategory> LookupCategories => Set<AccessGovernanceLookupCategory>();
    public DbSet<AccessGovernanceLookupValue> LookupValues => Set<AccessGovernanceLookupValue>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<PlatformSequence> PlatformSequences => Set<PlatformSequence>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<RoleDefinition>(e =>
        {
            e.ToTable("RoleDefinitions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.RoleCode).IsUnique();
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasMany(x => x.Assignments).WithOne().HasForeignKey(x => x.RoleDefinitionId);
            e.HasMany(x => x.Permissions).WithOne().HasForeignKey(x => x.RoleDefinitionId);
            e.Navigation(x => x.Assignments).UsePropertyAccessMode(PropertyAccessMode.Field);
            e.Navigation(x => x.Permissions).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<RoleAssignment>(e =>
        {
            e.ToTable("RoleAssignments");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.RoleDefinitionId, x.OrganizationId, x.HospitalId, x.DepartmentId });
            e.HasIndex(x => new { x.UserId, x.IsRevoked, x.EffectiveFrom, x.EffectiveTo });
        });

        builder.Entity<Permission>(e =>
        {
            e.ToTable("Permissions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        builder.Entity<PermissionAssignment>(e =>
        {
            e.ToTable("PermissionAssignments");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.RoleDefinitionId, x.PermissionId });
        });

        builder.Entity<AuthorizationPolicy>(e =>
        {
            e.ToTable("AuthorizationPolicies");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PolicyCode).IsUnique();
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasMany(x => x.Rules).WithOne().HasForeignKey(x => x.AuthorizationPolicyId);
            e.Navigation(x => x.Rules).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<PolicyRule>(e =>
        {
            e.ToTable("PolicyRules");
            e.HasKey(x => x.Id);
            e.HasMany(x => x.Conditions).WithOne().HasForeignKey(x => x.PolicyRuleId);
            e.Navigation(x => x.Conditions).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Entity<PolicyCondition>(e => e.ToTable("PolicyConditions"));
        builder.Entity<Delegation>(e =>
        {
            e.ToTable("Delegations");
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasMany(x => x.History).WithOne().HasForeignKey(x => x.DelegationId);
            e.Navigation(x => x.History).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        builder.Entity<DelegationHistory>(e => e.ToTable("DelegationHistories"));
        builder.Entity<ResponsibilityAssignment>(e =>
        {
            e.ToTable("ResponsibilityAssignments");
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsRowVersion();
        });
        builder.Entity<ApprovalAuthority>(e =>
        {
            e.ToTable("ApprovalAuthorities");
            e.HasKey(x => x.Id);
            e.Property(x => x.RowVersion).IsRowVersion();
        });
        builder.Entity<AccessEvaluation>(e => e.ToTable("AccessEvaluations"));
        builder.Entity<AccessAuditLog>(e => e.ToTable("AccessAuditLogs"));
        builder.Entity<AccessGovernanceLookupCategory>(e =>
        {
            e.ToTable("LookupCategories");
            e.HasIndex(x => x.Code).IsUnique();
        });
        builder.Entity<AccessGovernanceLookupValue>(e =>
        {
            e.ToTable("LookupValues");
            e.HasIndex(x => new { x.CategoryId, x.Code }).IsUnique();
            e.HasOne(x => x.Category).WithMany(x => x.Values).HasForeignKey(x => x.CategoryId);
        });
        builder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("OutboxMessages");
            e.HasIndex(x => x.ProcessedOn);
        });
        builder.Entity<PlatformSequence>(e => e.ToTable("PlatformSequences"));
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified))
        {
            if (entry.Metadata.FindProperty("RowVersion") != null && entry.State == EntityState.Added)
                entry.Property("RowVersion").CurrentValue = new byte[8];
        }
        return await base.SaveChangesAsync(cancellationToken);
    }
}
