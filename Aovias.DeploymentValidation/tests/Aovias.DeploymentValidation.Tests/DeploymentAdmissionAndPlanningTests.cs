using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Admission;
using Aovias.Deployment.Contracts.Building;
using Aovias.Deployment.Contracts.Capabilities;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Migration;
using Aovias.Deployment.Contracts.Planning;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

/// <summary>
/// Sprint M2.4 D10 regression suite: the Admission Engine (D1/D4/D8), the Deployment Planner
/// (D2/D5/D7), and Plan validation (D9).
/// </summary>
public sealed class DeploymentAdmissionAndPlanningTests
{
    private static InfrastructureContract MinimalContract(string serviceId, IReadOnlyList<Requirement> requirements) => new()
    {
        ServiceId = serviceId,
        SchemaVersion = ContractSchemaVersions.Current,
        ContractVersion = ContractSchemaVersions.ContractVersionV2,
        Compatibility = new ContractCompatibilityMetadata
        {
            MinimumSupportedSchemaVersion = ContractSchemaVersions.V1,
            MaximumSupportedSchemaVersion = ContractSchemaVersions.Current
        },
        Integrity = new ContractIntegrityMetadata { ContentHash = "test-content-hash" },
        Requirements = requirements
    };

    private static AdmissionRequest MinimalApprovableRequest(string serviceId, IReadOnlyList<Requirement> requirements, IReadOnlyList<Capability> capabilities) => new()
    {
        ServiceId = serviceId,
        CorrelationId = "corr-1",
        Contract = MinimalContract(serviceId, requirements),
        PackageIntegrity = PackageIntegrityFacts.Ok(),
        RuntimeAgentVersion = "1.0.0",
        AvailableCapabilities = capabilities,
        Environment = new EnvironmentRegistrationFacts { EnvironmentId = "local", Registered = true },
        InfrastructureReadiness = new InfrastructureReadinessFacts { Ready = true }
    };

    // ---- Admission: success ----

    [Fact]
    public void Admission_Approves_WhenEveryStagePasses()
    {
        var requirements = new List<Requirement>
        {
            new() { Id = "primary-database", Category = RequirementCategories.SqlDatabase, Mandatory = true, VersionConstraint = ">=2019.0" }
        };
        var request = MinimalApprovableRequest("test-service", requirements, [CapabilityRegistry.FindById("sqlserver")!]);

        var result = new DeploymentAdmissionEngine().Evaluate(request);

        Assert.Equal(AdmissionDecision.Approved, result.Decision);
        Assert.Empty(result.Blockers);
        Assert.Equal("corr-1", result.CorrelationId);
        Assert.Equal(7, result.EvaluatedCategories.Count); // all seven categories evaluated when nothing blocks
    }

    [Fact]
    public void Admission_Approves_WithWarning_WhenOptionalRequirementUnmet()
    {
        var requirements = new List<Requirement>
        {
            new() { Id = "ai-provider", Category = RequirementCategories.ExternalAiProvider, Mandatory = false }
        };
        var request = MinimalApprovableRequest("ai", requirements, []);

        var result = new DeploymentAdmissionEngine().Evaluate(request);

        Assert.Equal(AdmissionDecision.Approved, result.Decision);
        Assert.Empty(result.Blockers);
        Assert.Single(result.Warnings);
        Assert.Equal(AdmissionCategory.RequirementMatching, result.Warnings[0].Category);
    }

    // ---- Admission: failure categories ----

    [Fact]
    public void Admission_Rejects_OnPackageIntegrityFailure_AndStopsImmediately()
    {
        var baseRequest = MinimalApprovableRequest("svc", [], []);
        var request = new AdmissionRequest
        {
            ServiceId = baseRequest.ServiceId,
            CorrelationId = baseRequest.CorrelationId,
            Contract = baseRequest.Contract,
            PackageIntegrity = PackageIntegrityFacts.Failed("checksum mismatch"),
            RuntimeAgentVersion = baseRequest.RuntimeAgentVersion,
            AvailableCapabilities = baseRequest.AvailableCapabilities,
            Environment = baseRequest.Environment,
            InfrastructureReadiness = baseRequest.InfrastructureReadiness
        };

        var result = new DeploymentAdmissionEngine().Evaluate(request);

        Assert.Equal(AdmissionDecision.Rejected, result.Decision);
        Assert.Single(result.Blockers);
        Assert.Equal(AdmissionCategory.PackageIntegrity, result.Blockers[0].Category);
        Assert.Equal("checksum mismatch", result.Blockers[0].Reason);
        // Fail-fast: only the one category that actually failed was evaluated.
        Assert.Single(result.EvaluatedCategories);
        Assert.Equal("svc", result.Blockers[0].AffectedService);
        Assert.Equal("corr-1", result.Blockers[0].CorrelationId);
    }

    [Fact]
    public void Admission_Rejects_OnUnsupportedSchemaVersion()
    {
        var c = MinimalContract("svc", []);
        var contract = new InfrastructureContract
        {
            ServiceId = c.ServiceId,
            SchemaVersion = 99,
            ContractVersion = c.ContractVersion,
            Compatibility = c.Compatibility,
            Integrity = c.Integrity,
            Requirements = c.Requirements
        };

        var request = new AdmissionRequest
        {
            ServiceId = "svc",
            CorrelationId = "corr-1",
            Contract = contract,
            PackageIntegrity = PackageIntegrityFacts.Ok(),
            RuntimeAgentVersion = "1.0.0",
            AvailableCapabilities = [],
            Environment = new EnvironmentRegistrationFacts { EnvironmentId = "local", Registered = true },
            InfrastructureReadiness = new InfrastructureReadinessFacts { Ready = true }
        };

        var result = new DeploymentAdmissionEngine().Evaluate(request);

        Assert.Equal(AdmissionDecision.Rejected, result.Decision);
        Assert.Equal(AdmissionCategory.SchemaCompatibility, result.Blockers[0].Category);
    }

    [Fact]
    public void Admission_Rejects_OnRequirementMissing()
    {
        var requirements = new List<Requirement>
        {
            new() { Id = "primary-database", Category = RequirementCategories.SqlDatabase, Mandatory = true }
        };
        var request = MinimalApprovableRequest("svc", requirements, []); // no capabilities at all

        var result = new DeploymentAdmissionEngine().Evaluate(request);

        Assert.Equal(AdmissionDecision.Rejected, result.Decision);
        Assert.Equal(AdmissionCategory.RequirementMatching, result.Blockers[0].Category);
        Assert.Equal(RequirementCategories.SqlDatabase, result.Blockers[0].AffectedCapability);
    }

    [Fact]
    public void Admission_Rejects_OnCapabilityVersionIncompatible()
    {
        var requirements = new List<Requirement>
        {
            new() { Id = "primary-database", Category = RequirementCategories.SqlDatabase, Mandatory = true, VersionConstraint = ">=2025.0" }
        };
        var request = MinimalApprovableRequest("svc", requirements, [CapabilityRegistry.FindById("sqlserver")!]); // 2022.0.0, too old

        var result = new DeploymentAdmissionEngine().Evaluate(request);

        Assert.Equal(AdmissionDecision.Rejected, result.Decision);
        Assert.Equal(AdmissionCategory.CapabilityMatching, result.Blockers[0].Category);
    }

    [Fact]
    public void Admission_Rejects_OnRuntimeVersionMismatch()
    {
        var baseRequest = MinimalApprovableRequest("svc", [], []);
        var request = new AdmissionRequest
        {
            ServiceId = baseRequest.ServiceId,
            CorrelationId = baseRequest.CorrelationId,
            Contract = baseRequest.Contract,
            PackageIntegrity = baseRequest.PackageIntegrity,
            RuntimeAgentVersion = "1.5.0",
            MinimumRuntimeAgentVersion = "2.0.0",
            AvailableCapabilities = baseRequest.AvailableCapabilities,
            Environment = baseRequest.Environment,
            InfrastructureReadiness = baseRequest.InfrastructureReadiness
        };

        var result = new DeploymentAdmissionEngine().Evaluate(request);

        Assert.Equal(AdmissionDecision.Rejected, result.Decision);
        Assert.Equal(AdmissionCategory.RuntimeCompatibility, result.Blockers[0].Category);
    }

    [Fact]
    public void Admission_Rejects_OnInfrastructureNotReady()
    {
        var baseRequest = MinimalApprovableRequest("svc", [], []);
        var request = new AdmissionRequest
        {
            ServiceId = baseRequest.ServiceId,
            CorrelationId = baseRequest.CorrelationId,
            Contract = baseRequest.Contract,
            PackageIntegrity = baseRequest.PackageIntegrity,
            RuntimeAgentVersion = baseRequest.RuntimeAgentVersion,
            AvailableCapabilities = baseRequest.AvailableCapabilities,
            Environment = baseRequest.Environment,
            InfrastructureReadiness = new InfrastructureReadinessFacts { Ready = false, Blockers = ["disk full"] }
        };

        var result = new DeploymentAdmissionEngine().Evaluate(request);

        Assert.Equal(AdmissionDecision.Rejected, result.Decision);
        Assert.Equal(AdmissionCategory.InfrastructureReadiness, result.Blockers[0].Category);
        Assert.Equal("disk full", result.Blockers[0].Reason);
    }

    [Fact]
    public void Admission_Rejects_OnUnregisteredEnvironment()
    {
        var baseRequest = MinimalApprovableRequest("svc", [], []);
        var request = new AdmissionRequest
        {
            ServiceId = baseRequest.ServiceId,
            CorrelationId = baseRequest.CorrelationId,
            Contract = baseRequest.Contract,
            PackageIntegrity = baseRequest.PackageIntegrity,
            RuntimeAgentVersion = baseRequest.RuntimeAgentVersion,
            AvailableCapabilities = baseRequest.AvailableCapabilities,
            Environment = new EnvironmentRegistrationFacts { EnvironmentId = "unregistered-env", Registered = false },
            InfrastructureReadiness = baseRequest.InfrastructureReadiness
        };

        var result = new DeploymentAdmissionEngine().Evaluate(request);

        Assert.Equal(AdmissionDecision.Rejected, result.Decision);
        Assert.Equal(AdmissionCategory.EnvironmentValidation, result.Blockers[0].Category);
    }

    [Fact]
    public void Admission_IsDeterministic_AcrossRepeatedEvaluations()
    {
        var requirements = new List<Requirement>
        {
            new() { Id = "primary-database", Category = RequirementCategories.SqlDatabase, Mandatory = true }
        };
        var request = MinimalApprovableRequest("svc", requirements, [CapabilityRegistry.FindById("sqlserver")!]);
        var engine = new DeploymentAdmissionEngine();

        var first = engine.Evaluate(request);
        var second = engine.Evaluate(request);

        Assert.Equal(first.Decision, second.Decision);
        Assert.Equal(first.Diagnostics.Count, second.Diagnostics.Count);
        Assert.Equal(first.EvaluatedCategories, second.EvaluatedCategories);
    }

    // ---- Deployment Planner ----

    private static ServiceDeploymentBundle BuildBundle(string serviceId, int port, IReadOnlyList<string> dependencies)
        => ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = serviceId,
            DisplayName = serviceId,
            Version = "1.0.0",
            ImageReference = $"aovias/{serviceId}:1.0.0",
            ContainerName = serviceId,
            EntryPoint = "App.dll",
            HealthEndpoint = "/health",
            Ports = port > 0 ? [port] : [],
            Dependencies = dependencies
        });

    [Fact]
    public void Planner_Throws_WhenAdmissionNotApproved()
    {
        var bundle = BuildBundle("identity-service", 5062, []);
        var contract = MinimalContract("identity-service", []);
        var rejected = new AdmissionResult { Decision = AdmissionDecision.Rejected, CorrelationId = "corr-1" };

        Assert.Throws<InvalidOperationException>(() => new DeploymentPlanBuilder().CreatePlan(new DeploymentPlanRequest
        {
            Bundle = bundle,
            Contract = contract,
            Admission = rejected,
            EnvironmentId = "local",
            AvailableCapabilities = [],
            GeneratedUtc = DateTimeOffset.UtcNow
        }));
    }

    [Fact]
    public void Planner_GeneratesAllEightPhasesInCanonicalOrder()
    {
        var plan = CreateApprovedPlan("identity-service", 5062, [], ["primary-database", "sqlserver"]);

        var names = plan.ExecutionPhases.OrderBy(phase => phase.Order).Select(phase => phase.Name).ToList();
        Assert.Equal(
        [
            DeploymentExecutionPhaseName.Preflight,
            DeploymentExecutionPhaseName.Infrastructure,
            DeploymentExecutionPhaseName.Upload,
            DeploymentExecutionPhaseName.Configuration,
            DeploymentExecutionPhaseName.Deployment,
            DeploymentExecutionPhaseName.Health,
            DeploymentExecutionPhaseName.Certification,
            DeploymentExecutionPhaseName.Completion
        ], names);
    }

    [Fact]
    public void Planner_IncludesDatabaseMigrationRollbackCheckpoint_WhenDatabaseRequired()
    {
        var plan = CreateApprovedPlan("identity-service", 5062, [], []);

        Assert.Contains(plan.RollbackPoints, point => point.Id == "before-database-migration");
        Assert.Contains(plan.RollbackPoints, point => point.Id == "before-certification");
        Assert.Contains(plan.RollbackPoints, point => point.Id == "before-upload");
    }

    [Fact]
    public void Planner_DependencyOrdering_IncludesDependenciesAndPrimaryServiceLast()
    {
        var plan = CreateApprovedPlan("gateway", 5212, ["runtime-agent"], []);

        Assert.Contains("runtime-agent", plan.Dependencies);
        Assert.Equal("gateway", plan.Dependencies[^1]);
    }

    [Fact]
    public void Planner_IsDeterministic_ContentHashIgnoresCorrelationIdAndTimestamp()
    {
        var bundle = BuildBundle("identity-service", 5062, []);
        var contract = MinimalContract("identity-service", []);

        var planA = new DeploymentPlanBuilder().CreatePlan(new DeploymentPlanRequest
        {
            Bundle = bundle,
            Contract = contract,
            Admission = new AdmissionResult { Decision = AdmissionDecision.Approved, CorrelationId = "corr-A" },
            EnvironmentId = "local",
            AvailableCapabilities = [],
            GeneratedUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });

        var planB = new DeploymentPlanBuilder().CreatePlan(new DeploymentPlanRequest
        {
            Bundle = bundle,
            Contract = contract,
            Admission = new AdmissionResult { Decision = AdmissionDecision.Approved, CorrelationId = "corr-B" },
            EnvironmentId = "local",
            AvailableCapabilities = [],
            GeneratedUtc = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)
        });

        Assert.Equal(planA.Diagnostics.ContentHash, planB.Diagnostics.ContentHash);
        Assert.NotEqual(planA.Metadata.CorrelationId, planB.Metadata.CorrelationId);
    }

    private static DeploymentPlan CreateApprovedPlan(string serviceId, int port, IReadOnlyList<string> dependencies, IReadOnlyList<string> unused)
    {
        _ = unused;
        var bundle = BuildBundle(serviceId, port, dependencies);
        var contract = MinimalContract(serviceId, []);
        var admission = new AdmissionResult { Decision = AdmissionDecision.Approved, CorrelationId = "corr-1" };

        return new DeploymentPlanBuilder().CreatePlan(new DeploymentPlanRequest
        {
            Bundle = bundle,
            Contract = contract,
            Admission = admission,
            EnvironmentId = "local",
            AvailableCapabilities = [],
            GeneratedUtc = DateTimeOffset.UtcNow
        });
    }

    // ---- Plan validation (D9) ----

    private static DeploymentPlan MinimalValidPlan()
    {
        var task1 = new DeploymentTask { TaskId = "preflight.check", Description = "check" };
        var task2 = new DeploymentTask { TaskId = "infrastructure.provision", Description = "provision", DependsOnTaskIds = ["preflight.check"] };
        var task3 = new DeploymentTask { TaskId = "upload.stage", Description = "stage", DependsOnTaskIds = ["infrastructure.provision"] };
        var task4 = new DeploymentTask { TaskId = "configuration.apply", Description = "apply", DependsOnTaskIds = ["upload.stage"] };
        var task5 = new DeploymentTask { TaskId = "deployment.activate", Description = "activate", DependsOnTaskIds = ["configuration.apply"] };
        var task6 = new DeploymentTask { TaskId = "health.verify", Description = "verify", DependsOnTaskIds = ["deployment.activate"] };
        var task7 = new DeploymentTask { TaskId = "certification.verify", Description = "verify", DependsOnTaskIds = ["health.verify"] };
        var task8 = new DeploymentTask { TaskId = "completion.finalize", Description = "finalize", DependsOnTaskIds = ["certification.verify"] };

        return new DeploymentPlan
        {
            Metadata = new DeploymentPlanMetadata { PlanId = "p1", ServiceId = "svc", ServiceVersion = "1.0.0", SchemaVersion = ContractSchemaVersions.Current, CorrelationId = "corr-1", GeneratedUtc = DateTimeOffset.UtcNow },
            Environment = new DeploymentPlanEnvironment { EnvironmentId = "local" },
            Services = [new DeploymentPlanServiceEntry { ServiceId = "svc", Required = true, Type = "primary" }],
            Infrastructure = new DeploymentPlanInfrastructure(),
            Dependencies = ["svc"],
            ExecutionPhases =
            [
                new DeploymentExecutionPhase { Name = DeploymentExecutionPhaseName.Preflight, Order = 1, Tasks = [task1] },
                new DeploymentExecutionPhase { Name = DeploymentExecutionPhaseName.Infrastructure, Order = 2, Tasks = [task2] },
                new DeploymentExecutionPhase { Name = DeploymentExecutionPhaseName.Upload, Order = 3, Tasks = [task3] },
                new DeploymentExecutionPhase { Name = DeploymentExecutionPhaseName.Configuration, Order = 4, Tasks = [task4] },
                new DeploymentExecutionPhase { Name = DeploymentExecutionPhaseName.Deployment, Order = 5, Tasks = [task5] },
                new DeploymentExecutionPhase { Name = DeploymentExecutionPhaseName.Health, Order = 6, Tasks = [task6] },
                new DeploymentExecutionPhase { Name = DeploymentExecutionPhaseName.Certification, Order = 7, Tasks = [task7] },
                new DeploymentExecutionPhase { Name = DeploymentExecutionPhaseName.Completion, Order = 8, Tasks = [task8] }
            ],
            HealthGates = [],
            RollbackPoints =
            [
                new RollbackCheckpoint { Id = "before-deployment", BeforePhase = DeploymentExecutionPhaseName.Deployment, Description = "d" },
                new RollbackCheckpoint { Id = "before-certification", BeforePhase = DeploymentExecutionPhaseName.Certification, Description = "d" }
            ],
            CertificationGates = [new CertificationGate { Id = "gate", Phase = DeploymentExecutionPhaseName.Certification, Description = "d" }],
            Recovery = new RecoveryStrategy { Type = RecoveryStrategyType.RollbackToPrevious, Description = "d" },
            Diagnostics = new PlanDiagnostics { AdmissionCorrelationId = "corr-1", ContentHash = "hash" }
        };
    }

    [Fact]
    public void PlanValidator_Passes_ForBuilderOutput()
    {
        var plan = CreateApprovedPlan("identity-service", 5062, [], []);
        var result = DeploymentPlanValidator.Validate(plan);
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void PlanValidator_Passes_ForMinimalValidPlan()
    {
        var result = DeploymentPlanValidator.Validate(MinimalValidPlan());
        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Fact]
    public void PlanValidator_Fails_OnMissingPhase()
    {
        var plan = MinimalValidPlan();
        var withoutCompletion = new DeploymentPlan
        {
            Metadata = plan.Metadata,
            Environment = plan.Environment,
            Services = plan.Services,
            Infrastructure = plan.Infrastructure,
            Dependencies = plan.Dependencies,
            ExecutionPhases = plan.ExecutionPhases.Where(phase => phase.Name != DeploymentExecutionPhaseName.Completion).ToList(),
            HealthGates = plan.HealthGates,
            RollbackPoints = plan.RollbackPoints,
            CertificationGates = plan.CertificationGates,
            Recovery = plan.Recovery,
            Diagnostics = plan.Diagnostics
        };

        var result = DeploymentPlanValidator.Validate(withoutCompletion);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("missing required execution phase"));
    }

    [Fact]
    public void PlanValidator_Fails_OnOrphanTaskDependency()
    {
        var plan = MinimalValidPlan();
        var brokenPhase = plan.ExecutionPhases[0];
        var mutatedPhases = plan.ExecutionPhases.ToList();
        mutatedPhases[0] = new DeploymentExecutionPhase
        {
            Name = brokenPhase.Name,
            Order = brokenPhase.Order,
            Tasks = [new DeploymentTask { TaskId = "preflight.check", Description = "check", DependsOnTaskIds = ["nonexistent-task"] }]
        };

        var broken = new DeploymentPlan
        {
            Metadata = plan.Metadata,
            Environment = plan.Environment,
            Services = plan.Services,
            Infrastructure = plan.Infrastructure,
            Dependencies = plan.Dependencies,
            ExecutionPhases = mutatedPhases,
            HealthGates = plan.HealthGates,
            RollbackPoints = plan.RollbackPoints,
            CertificationGates = plan.CertificationGates,
            Recovery = plan.Recovery,
            Diagnostics = plan.Diagnostics
        };

        var result = DeploymentPlanValidator.Validate(broken);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("orphaned task dependency"));
    }

    [Fact]
    public void PlanValidator_Fails_OnCircularTaskDependency()
    {
        var plan = MinimalValidPlan();
        var mutatedPhases = plan.ExecutionPhases.ToList();
        mutatedPhases[0] = new DeploymentExecutionPhase
        {
            Name = DeploymentExecutionPhaseName.Preflight,
            Order = 1,
            Tasks =
            [
                new DeploymentTask { TaskId = "preflight.a", Description = "a", DependsOnTaskIds = ["preflight.b"] },
                new DeploymentTask { TaskId = "preflight.b", Description = "b", DependsOnTaskIds = ["preflight.a"] }
            ]
        };

        var broken = new DeploymentPlan
        {
            Metadata = plan.Metadata,
            Environment = plan.Environment,
            Services = plan.Services,
            Infrastructure = plan.Infrastructure,
            Dependencies = plan.Dependencies,
            ExecutionPhases = mutatedPhases,
            HealthGates = plan.HealthGates,
            RollbackPoints = plan.RollbackPoints,
            CertificationGates = plan.CertificationGates,
            Recovery = plan.Recovery,
            Diagnostics = plan.Diagnostics
        };

        var result = DeploymentPlanValidator.Validate(broken);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Circular task dependency"));
    }

    [Fact]
    public void PlanValidator_Fails_OnMissingRollbackCheckpoints()
    {
        var plan = MinimalValidPlan();
        var broken = new DeploymentPlan
        {
            Metadata = plan.Metadata,
            Environment = plan.Environment,
            Services = plan.Services,
            Infrastructure = plan.Infrastructure,
            Dependencies = plan.Dependencies,
            ExecutionPhases = plan.ExecutionPhases,
            HealthGates = plan.HealthGates,
            RollbackPoints = [],
            CertificationGates = plan.CertificationGates,
            Recovery = plan.Recovery,
            Diagnostics = plan.Diagnostics
        };

        var result = DeploymentPlanValidator.Validate(broken);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("no rollback checkpoints"));
    }

    [Fact]
    public void PlanValidator_Fails_OnInvalidServiceOrdering()
    {
        var plan = MinimalValidPlan();
        var broken = new DeploymentPlan
        {
            Metadata = plan.Metadata,
            Environment = plan.Environment,
            Services = plan.Services,
            Infrastructure = plan.Infrastructure,
            Dependencies = ["svc", "undeclared-service"],
            ExecutionPhases = plan.ExecutionPhases,
            HealthGates = plan.HealthGates,
            RollbackPoints = plan.RollbackPoints,
            CertificationGates = plan.CertificationGates,
            Recovery = plan.Recovery,
            Diagnostics = plan.Diagnostics
        };

        var result = DeploymentPlanValidator.Validate(broken);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("undeclared service"));
    }

    [Fact]
    public void PlanValidator_Fails_OnDuplicatePhase()
    {
        var plan = MinimalValidPlan();
        var mutatedPhases = plan.ExecutionPhases.ToList();
        mutatedPhases.Add(mutatedPhases[0]);

        var broken = new DeploymentPlan
        {
            Metadata = plan.Metadata,
            Environment = plan.Environment,
            Services = plan.Services,
            Infrastructure = plan.Infrastructure,
            Dependencies = plan.Dependencies,
            ExecutionPhases = mutatedPhases,
            HealthGates = plan.HealthGates,
            RollbackPoints = plan.RollbackPoints,
            CertificationGates = plan.CertificationGates,
            Recovery = plan.Recovery,
            Diagnostics = plan.Diagnostics
        };

        var result = DeploymentPlanValidator.Validate(broken);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("Duplicate execution phase"));
    }
}
