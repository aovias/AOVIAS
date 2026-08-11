using Microsoft.EntityFrameworkCore;
using OrganizationService.Domain.Entities;
using Shared.Infrastructure.Domain;

namespace OrganizationService.Infrastructure.Persistence
{
    public class OrganizationDbContext : DbContext
    {
        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<OrganizationMember> Members => Set<OrganizationMember>();
        public DbSet<Hospital> Hospitals => Set<Hospital>();
        public DbSet<PlatformSequence> PlatformSequences { get; set; } = default!;
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<Service> Services => Set<Service>();
        public DbSet<MasterDepartment> MasterDepartments => Set<MasterDepartment>();
        public DbSet<HospitalMasterDepartment> HospitalMasterDepartments => Set<HospitalMasterDepartment>();
        public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
        public DbSet<OrganizationSubscription> OrganizationSubscriptions => Set<OrganizationSubscription>();
        public DbSet<FrameworkSubscription> FrameworkSubscriptions => Set<FrameworkSubscription>();
        public DbSet<OrganizationMemberInvitation> OrganizationMemberInvitations => Set<OrganizationMemberInvitation>();
        public DbSet<OrganizationalUnit> OrganizationalUnits => Set<OrganizationalUnit>();
        public DbSet<OrganizationMembershipHistory> MembershipHistories => Set<OrganizationMembershipHistory>();
        public DbSet<OrganizationTimelineEntry> OrganizationTimeline => Set<OrganizationTimelineEntry>();
        public DbSet<OrganizationAuditLog> OrganizationAuditLogs => Set<OrganizationAuditLog>();
        public DbSet<OrganizationLookupCategory> LookupCategories => Set<OrganizationLookupCategory>();
        public DbSet<OrganizationLookupValue> LookupValues => Set<OrganizationLookupValue>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<OrganizationSettings> OrganizationSettings => Set<OrganizationSettings>();
        public DbSet<OrgHoliday> OrgHolidays => Set<OrgHoliday>();
        public DbSet<LicenseKey> LicenseKeys => Set<LicenseKey>();
        public DbSet<LicenseActivation> LicenseActivations => Set<LicenseActivation>();
        public DbSet<LicenseSeatAssignment> LicenseSeatAssignments => Set<LicenseSeatAssignment>();
        public DbSet<LicenseHeartbeat> LicenseHeartbeats => Set<LicenseHeartbeat>();
        public DbSet<OnboardingTelemetryEvent> OnboardingTelemetryEvents => Set<OnboardingTelemetryEvent>();
        public DbSet<CommercialAdminAuditEvent> CommercialAdminAuditEvents => Set<CommercialAdminAuditEvent>();
        public DbSet<EmailDeliveryLog> EmailDeliveryLogs => Set<EmailDeliveryLog>();
        public DbSet<TrialLifecycleNotificationLog> TrialLifecycleNotificationLogs => Set<TrialLifecycleNotificationLog>();
        public DbSet<TrialLifecycleRun> TrialLifecycleRuns => Set<TrialLifecycleRun>();

        public OrganizationDbContext(DbContextOptions<OrganizationDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Organization>()
                .HasIndex(x => x.PlatformId)
                .IsUnique();

            builder.Entity<Organization>()
                .HasIndex(x => x.OrganizationCode)
                .IsUnique();

            builder.Entity<Organization>(entity =>
            {
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.OrganizationCode).HasMaxLength(20).IsRequired();
                entity.Property(x => x.RowVersion).IsRowVersion();
            });

            builder.Entity<OrganizationSettings>(entity =>
            {
                entity.ToTable("OrganizationSettings");
                entity.HasKey(x => x.OrganizationId);
                entity.Property(x => x.TimeZoneId).HasMaxLength(64).IsRequired();
                entity.Property(x => x.LanguageCode).HasMaxLength(16).IsRequired();
                entity.Property(x => x.WorkingDaysJson).HasColumnType("nvarchar(max)").IsRequired();
                entity.Property(x => x.LogoStoragePath).HasMaxLength(500);
                entity.Property(x => x.BrandingAssetsJson).HasColumnType("nvarchar(max)").HasDefaultValue("{}");
                entity.Property(x => x.BrandingConfigJson).HasColumnType("nvarchar(max)").HasDefaultValue("{}");
                entity.Property(x => x.RowVersion).IsRowVersion();
                entity.HasOne<Organization>()
                    .WithOne()
                    .HasForeignKey<OrganizationSettings>(x => x.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<OrgHoliday>(entity =>
            {
                entity.ToTable("OrgHolidays");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Date).HasColumnType("date");
                entity.HasIndex(x => new { x.OrganizationId, x.Date }).IsUnique();
                entity.HasOne<Organization>()
                    .WithMany()
                    .HasForeignKey(x => x.OrganizationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<OrganizationMember>()
                .Property(x => x.RowVersion)
                .IsRowVersion();

            builder.Entity<OrganizationMember>(entity =>
            {
                entity.Property(x => x.FirstName)
                    .HasMaxLength(100)
                    .IsRequired(false);

                entity.Property(x => x.LastName)
                    .HasMaxLength(100)
                    .IsRequired(false);

                entity.Property(x => x.Email)
                    .HasMaxLength(256)
                    .IsRequired(false);

                entity.Property(x => x.PhoneNumber)
                    .HasMaxLength(20)
                    .IsRequired(false);

                entity.Property(x => x.AlternatePhoneNumber)
                    .HasMaxLength(20)
                    .IsRequired(false);

                entity.Property(x => x.ProfilePicUrl)
                    .HasMaxLength(500)
                    .IsRequired(false);

                entity.Property(x => x.Preferences)
                    .HasColumnType("nvarchar(max)")
                    .IsRequired(false);

                entity.Property(x => x.DepartmentId)
                    .IsRequired(false);

                entity.Property(x => x.Designation)
                    .HasMaxLength(100)
                    .IsRequired(false);

                entity.Property(x => x.MembershipStatus).HasMaxLength(32).HasDefaultValue("Active");
                entity.Property(x => x.EffectiveFrom).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(x => x.IsPrimary).HasDefaultValue(true);
                entity.Property(x => x.IsDeleted).HasDefaultValue(false);

                entity.HasOne<Organization>()
                    .WithMany()
                    .HasForeignKey(x => x.OrganizationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new { x.UserId, x.OrganizationId, x.IsPrimary })
                    .HasFilter("[IsDeleted] = 0 AND [IsPrimary] = 1 AND [MembershipStatus] = 'Active'");
            });

            builder.Entity<Hospital>(entity =>
            {
                entity.HasOne<Organization>()
                    .WithMany()
                    .HasForeignKey(x => x.OrganizationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => x.OrganizationId);

                entity.HasIndex(x => new { x.OrganizationId, x.Code })
                    .IsUnique();

                entity.Property(x => x.Type)
                    .HasMaxLength(100)
                    .IsRequired(false);

                entity.Property(x => x.CountryCode)
                    .HasMaxLength(10)
                    .IsRequired(false);

                entity.Property(x => x.StateCode)
                    .HasMaxLength(10)
                    .IsRequired(false);

                entity.Property(x => x.BedCapacity)
                    .IsRequired(false);

                entity.Property(x => x.BedCapacityRange)
                    .HasMaxLength(50)
                    .IsRequired(false);

                entity.Property(x => x.WizardVersion)
                    .IsRequired(false);

                entity.Property(x => x.IsOnboarded)
                    .HasDefaultValue(false);

                entity.Property(x => x.RowVersion)
                    .IsRowVersion();
            });

            builder.Entity<HospitalMasterDepartment>(entity =>
            {
                entity.ToTable("HospitalMasterDepartments");
                entity.HasKey(x => new { x.HospitalId, x.MasterDepartmentId });

                entity.HasOne(x => x.Hospital)
                    .WithMany()
                    .HasForeignKey(x => x.HospitalId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.MasterDepartment)
                    .WithMany()
                    .HasForeignKey(x => x.MasterDepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PlatformSequence>(entity =>
            {
                entity.ToTable("PlatformSequences");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Prefix)
                      .IsRequired()
                      .HasMaxLength(10);

                entity.HasIndex(x => x.Prefix)
                      .IsUnique();

                entity.Property(x => x.CurrentValue)
                      .IsRequired();
            });

            builder.Entity<Department>(entity =>
            {
                entity.ToTable("Departments");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(x => x.Code)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(x => x.Code)
                    .IsUnique();

                entity.Property(x => x.Category)
                    .HasMaxLength(100);

                entity.Property(x => x.Description)
                    .HasMaxLength(500);

                entity.Property(x => x.IsActive)
                    .HasDefaultValue(true);

                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(x => x.UpdatedAt)
                    .IsRequired(false);
            });

            builder.Entity<MasterDepartment>(entity =>
            {
                entity.ToTable("MasterDepartments");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(x => x.Code)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(x => x.Code)
                    .IsUnique();

                entity.Property(x => x.Category)
                    .HasMaxLength(100);

                entity.Property(x => x.Description)
                    .HasMaxLength(500);

                entity.Property(x => x.DisplayOrder)
                    .IsRequired(false);

                entity.Property(x => x.IsActive)
                    .HasDefaultValue(true);

                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.Property(x => x.UpdatedAt)
                    .IsRequired(false);

                entity.Property(x => x.RowVersion)
                    .IsRowVersion();
            });

            builder.Entity<Service>(entity =>
            {
                entity.ToTable("Services");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(x => x.Code)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(x => x.Code)
                    .IsUnique();

                entity.HasIndex(x => x.DepartmentId);

                entity.HasOne(x => x.Department)
                    .WithMany(x => x.Services)
                    .HasForeignKey(x => x.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(x => x.Category)
                    .HasMaxLength(100);

                entity.Property(x => x.Description)
                    .HasMaxLength(500);

                entity.Property(x => x.IsCritical)
                    .HasDefaultValue(false);

                entity.Property(x => x.IsActive)
                    .HasDefaultValue(true);

                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(x => x.UpdatedAt)
                    .IsRequired(false);
            });

            builder.Entity<SubscriptionPlan>(entity =>
            {
                entity.ToTable("SubscriptionPlans");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Code)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(x => x.Code)
                    .IsUnique();

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(x => x.BillingPeriod)
                    .IsRequired()
                    .HasMaxLength(10);

                entity.Property(x => x.Currency)
                    .HasMaxLength(10);

                entity.Property(x => x.IsActive)
                    .HasDefaultValue(true);

                entity.Property(x => x.LifecycleStatus)
                    .HasMaxLength(20)
                    .HasDefaultValue("Published");
            });

            builder.Entity<OrganizationSubscription>(entity =>
            {
                entity.ToTable("OrganizationSubscriptions");

                entity.HasKey(x => x.Id);

                entity.HasIndex(x => new { x.OrganizationId, x.IsActive });

                entity.Property(x => x.IsActive)
                    .HasDefaultValue(true);

                entity.Property(x => x.StartsAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            builder.Entity<FrameworkSubscription>(entity =>
            {
                entity.ToTable("FrameworkSubscriptions");

                entity.HasKey(x => x.Id);

                entity.HasIndex(x => new { x.OrganizationSubscriptionId, x.FrameworkId, x.IsActive });

                entity.Property(x => x.IsActive)
                    .HasDefaultValue(true);

                entity.Property(x => x.ActivatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            builder.Entity<OrganizationMemberInvitation>(entity =>
            {
                entity.ToTable("OrganizationMemberInvitations");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.RowVersion)
                    .IsRowVersion();

                entity.Property(x => x.CreatedAt)
                    .HasDefaultValueSql("SYSUTCDATETIME()");

                entity.Property(x => x.Designation).HasMaxLength(200);
                entity.Property(x => x.EmploymentType).HasMaxLength(40).HasDefaultValue("Permanent");
                entity.Property(x => x.PipelineStep).HasMaxLength(64);
                entity.Property(x => x.PipelineError).HasMaxLength(1024);
                entity.Property(x => x.RequireSeat).HasDefaultValue(true);
                entity.Property(x => x.SeatAssigned).HasDefaultValue(false);

                entity.HasIndex(e => new { e.OrganizationId, e.NormalizedEmail })
                    .IsUnique()
                    .HasFilter("[Status] = 0");

                entity.HasIndex(e => e.TokenHash);

                entity.HasOne<Organization>()
                    .WithMany()
                    .HasForeignKey(x => x.OrganizationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Hospital>()
                    .WithMany()
                    .HasForeignKey(x => x.HospitalId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<OrganizationalUnit>(e =>
            {
                e.ToTable("OrganizationalUnits");
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.OrganizationId, x.Code });
                e.Property(x => x.RowVersion).IsRowVersion();
            });

            builder.Entity<OrganizationMembershipHistory>(e =>
            {
                e.ToTable("OrganizationMembershipHistories");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.MembershipId);
            });

            builder.Entity<OrganizationTimelineEntry>(e => e.ToTable("OrganizationTimeline"));
            builder.Entity<OrganizationAuditLog>(e => e.ToTable("OrganizationAuditLogs"));
            builder.Entity<OutboxMessage>(e => e.ToTable("OutboxMessages"));

            builder.Entity<OrganizationLookupCategory>(e =>
            {
                e.ToTable("OrganizationLookupCategories");
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Code).IsUnique();
                e.HasMany(x => x.Values).WithOne(x => x.Category!).HasForeignKey(x => x.CategoryId);
            });

            builder.Entity<OrganizationLookupValue>(e =>
            {
                e.ToTable("OrganizationLookupValues");
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.CategoryId, x.Code }).IsUnique();
            });

            builder.Entity<LicenseKey>(e =>
            {
                e.ToTable("LicenseKeys");
                e.HasKey(x => x.Id);
                e.Property(x => x.KeyCode).IsRequired().HasMaxLength(64);
                e.Property(x => x.EntitlementsJson).IsRequired();
                e.Property(x => x.CustomerName).HasMaxLength(256);
                e.Property(x => x.ExtensionCount).HasDefaultValue(0);
                e.HasIndex(x => x.KeyCode).IsUnique();
                e.HasIndex(x => new { x.OrganizationId, x.Status });
            });

            builder.Entity<LicenseActivation>(e =>
            {
                e.ToTable("LicenseActivations");
                e.HasKey(x => x.Id);
                e.Property(x => x.MachineFingerprint).IsRequired().HasMaxLength(256);
                e.Property(x => x.ActivationMode).IsRequired().HasMaxLength(32);
                e.HasIndex(x => new { x.LicenseKeyId, x.IsActive });
            });

            builder.Entity<LicenseSeatAssignment>(e =>
            {
                e.ToTable("LicenseSeatAssignments");
                e.HasKey(x => x.Id);
                e.Property(x => x.UserEmail).HasMaxLength(256);
                e.HasIndex(x => new { x.LicenseKeyId, x.UserId, x.IsActive });
            });

            builder.Entity<LicenseHeartbeat>(e =>
            {
                e.ToTable("LicenseHeartbeats");
                e.HasKey(x => x.Id);
                e.Property(x => x.MachineFingerprint).IsRequired().HasMaxLength(256);
                e.Property(x => x.ProductVersion).HasMaxLength(64);
                e.HasIndex(x => new { x.OrganizationId, x.ReceivedAtUtc });
            });

            builder.Entity<OnboardingTelemetryEvent>(e =>
            {
                e.ToTable("OnboardingTelemetryEvents");
                e.HasKey(x => x.Id);
                e.Property(x => x.EventType).IsRequired().HasMaxLength(128);
                e.Property(x => x.Stage).HasMaxLength(128);
                e.Property(x => x.CorrelationId).HasMaxLength(128);
                e.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)");
                e.HasIndex(x => new { x.OrganizationId, x.OccurredAtUtc });
                e.HasIndex(x => new { x.EventType, x.OccurredAtUtc });
            });

            builder.Entity<CommercialAdminAuditEvent>(e =>
            {
                e.ToTable("CommercialAdminAuditEvents");
                e.HasKey(x => x.Id);
                e.Property(x => x.OperatorEmail).HasMaxLength(256);
                e.Property(x => x.TargetType).IsRequired().HasMaxLength(64);
                e.Property(x => x.TargetId).HasMaxLength(128);
                e.Property(x => x.Action).IsRequired().HasMaxLength(128);
                e.Property(x => x.Reason).IsRequired().HasMaxLength(1024);
                e.Property(x => x.CorrelationId).HasMaxLength(128);
                e.Property(x => x.PreviousJson).HasColumnType("nvarchar(max)");
                e.Property(x => x.NewJson).HasColumnType("nvarchar(max)");
                e.HasIndex(x => new { x.OrganizationId, x.OccurredAtUtc });
                e.HasIndex(x => new { x.Action, x.OccurredAtUtc });
            });

            builder.Entity<EmailDeliveryLog>(e =>
            {
                e.ToTable("EmailDeliveryLogs");
                e.HasKey(x => x.Id);
                e.Property(x => x.TemplateKey).IsRequired().HasMaxLength(128);
                e.Property(x => x.ToAddress).IsRequired().HasMaxLength(256);
                e.Property(x => x.Status).IsRequired().HasMaxLength(32);
                e.Property(x => x.Subject).HasMaxLength(512);
                e.Property(x => x.Error).HasMaxLength(2048);
                e.Property(x => x.CorrelationId).HasMaxLength(128);
                e.HasIndex(x => new { x.OrganizationId, x.SentAtUtc });
                e.HasIndex(x => new { x.Status, x.SentAtUtc });
            });

            builder.Entity<TrialLifecycleNotificationLog>(e =>
            {
                e.ToTable("TrialLifecycleNotificationLogs");
                e.HasKey(x => x.Id);
                e.Property(x => x.Milestone).IsRequired().HasMaxLength(64);
                e.Property(x => x.CorrelationId).HasMaxLength(128);
                e.Property(x => x.Recipient).HasMaxLength(512);
                e.HasIndex(x => new { x.LicenseKeyId, x.Milestone }).IsUnique();
                e.HasIndex(x => new { x.OrganizationId, x.SentAtUtc });
            });

            builder.Entity<TrialLifecycleRun>(e =>
            {
                e.ToTable("TrialLifecycleRuns");
                e.HasKey(x => x.Id);
                e.Property(x => x.Trigger).IsRequired().HasMaxLength(32);
                e.Property(x => x.CorrelationId).IsRequired().HasMaxLength(128);
                e.Property(x => x.FailedLicenseKeyIdsJson).HasColumnType("nvarchar(max)");
                e.Property(x => x.ErrorSummary).HasMaxLength(2048);
                e.HasIndex(x => x.StartedAtUtc);
            });
        }
    }
}
