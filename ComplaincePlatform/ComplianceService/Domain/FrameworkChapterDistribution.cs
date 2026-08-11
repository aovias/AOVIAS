using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain
{
    public enum DistributionSource
    {
        Seed = 0,
        Ingest = 1,
        Computed = 2
    }

    public class FrameworkChapterDistribution : BaseEntity
    {
        public Guid FrameworkId { get; set; }
        public Guid ChapterId { get; set; }

        public int TotalStandards { get; set; }
        public int TotalObjectiveElements { get; set; }

        public int CoreCount { get; set; }
        public int CommitmentCount { get; set; }
        public int AchievementCount { get; set; }
        public int ExcellenceCount { get; set; }

        public DistributionSource Source { get; set; }
        public string Version { get; set; } = "v1";
        public DateTime CalculatedAt { get; set; }
    }
}

