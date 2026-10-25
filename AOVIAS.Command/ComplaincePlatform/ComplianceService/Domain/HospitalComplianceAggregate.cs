namespace ComplianceService.Domain
{
    public class HospitalComplianceAggregate : AggregateRoot
    {
        private readonly List<ComplianceTask> _tasks = new();
        private readonly List<Risk> _risks = new();
        private readonly List<ComplianceChapter> _chapters = new();
        
        // Internal mapping to track task-to-chapter relationships for scoring
        private readonly Dictionary<Guid, Guid> _taskChapterMap = new();

        public Guid HospitalId { get; private set; }
        public Guid FrameworkId { get; private set; }

        public IReadOnlyCollection<ComplianceTask> Tasks => _tasks.AsReadOnly();
        public IReadOnlyCollection<Risk> Risks => _risks.AsReadOnly();
        public IReadOnlyCollection<ComplianceChapter> Chapters => _chapters.AsReadOnly();

        private HospitalComplianceAggregate() { }

        public HospitalComplianceAggregate(Guid hospitalId, Guid frameworkId)
        {
            HospitalId = hospitalId;
            FrameworkId = frameworkId;
        }

        public void CompleteTask(Guid taskId, IComplianceScoringService scoringService)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task == null)
            {
                throw new InvalidOperationException($"Task with ID {taskId} not found in aggregate.");
            }

            // Update task state
            task.Status = Enums.ComplianceTaskStatus.Completed;

            // Build scoring context and calculate score
            var context = BuildScoringContext();
            var scoreResult = scoringService.Calculate(context);

            // Add domain event to trigger scoring/snapshot projection
            AddDomainEvent(new ComplianceScoreUpdatedDomainEvent(HospitalId, FrameworkId));
        }

        public ComplianceScoringContext BuildScoringContext()
        {
            var objectiveElementSnapshots = _tasks.Select(task =>
            {
                var chapterId = _taskChapterMap.TryGetValue(task.Id, out var chId)
                    ? chId
                    : throw new InvalidOperationException($"Chapter mapping not found for task {task.Id}");

                return new ObjectiveElementWorkSnapshot(
                    task.ObjectiveElementId,
                    chapterId,
                    task.Status.ToScoringStatusString(),
                    task.Weight
                );
            }).ToList();

            var riskSnapshots = _risks.Select(risk =>
            {
                Guid? chapterId = null;
                Guid? taskId = null;

                if (risk.ObjectiveElementId.HasValue && risk.ObjectiveElement?.ComplianceStandard != null)
                {
                    chapterId = risk.ObjectiveElement.ComplianceStandard.ChapterId;
                }
                else if (risk.TaskId.HasValue)
                {
                    taskId = risk.TaskId.Value;
                    chapterId = _taskChapterMap.TryGetValue(risk.TaskId.Value, out var chId) ? chId : null;
                }

                return new RiskSnapshot(
                    risk.Id,
                    chapterId,
                    taskId,
                    risk.Severity,
                    risk.RiskScore
                );
            }).ToList();

            var chapterSnapshots = _chapters.Select(chapter =>
                new ChapterSnapshot(chapter.Id, chapter.Weight)
            ).ToList();

            return new ComplianceScoringContext(
                HospitalId,
                FrameworkId,
                objectiveElementSnapshots,
                riskSnapshots,
                chapterSnapshots
            );
        }

        // Internal methods to populate the aggregate (typically called by repository/factory)
        internal void AddTask(ComplianceTask task, Guid chapterId)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (task.HospitalId != HospitalId)
            {
                throw new InvalidOperationException("Task does not belong to this hospital.");
            }

            _tasks.Add(task);
            _taskChapterMap[task.Id] = chapterId;
        }

        internal void AddRisk(Risk risk)
        {
            if (risk == null)
            {
                throw new ArgumentNullException(nameof(risk));
            }

            if (risk.HospitalId != HospitalId)
            {
                throw new InvalidOperationException("Risk does not belong to this hospital.");
            }

            _risks.Add(risk);
        }

        internal void AddChapter(ComplianceChapter chapter)
        {
            if (chapter == null)
            {
                throw new ArgumentNullException(nameof(chapter));
            }

            if (chapter.FrameworkId != FrameworkId)
            {
                throw new InvalidOperationException("Chapter does not belong to this framework.");
            }

            _chapters.Add(chapter);
        }

        /// <summary>
        /// Enrolls the hospital in the framework.
        /// Raises FrameworkEnrolledDomainEvent to signal that a lifecycle journey
        /// should be (or has been) created in the ComplianceJourney engine.
        /// This aggregate no longer owns or mutates lifecycle state.
        /// </summary>
        public void Enroll(Guid requestedByUserId)
        {
            // Raise enrollment domain event
            AddDomainEvent(new FrameworkEnrolledDomainEvent(HospitalId, FrameworkId, requestedByUserId));
        }
    }
}
