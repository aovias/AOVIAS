using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Aovias.Configuration.Settings;
using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Building;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Infrastructure.Provisioners;
using Aovias.Deployment.Contracts.Provisioning;
using Aovias.Deployment.Contracts.Smoke;
using Aovias.DeploymentValidation.Reliability;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Runtime.Agent.Infrastructure.Deployment;
using Xunit;

namespace Aovias.DeploymentValidation.Tests.Reliability;

/// <summary>
/// Sprint 11 deployment regression suite. Every previously fixed deployment bug is pinned here.
/// </summary>
public sealed class DeploymentRegressionTests
{
    [Fact]
    public void Deployment_matrix_declares_all_sprint11_scenarios_with_regression_ids()
    {
        Assert.Equal(16, PlatformDeploymentScenarioMatrix.All.Count);
        Assert.All(PlatformDeploymentScenarioMatrix.All, scenario =>
            Assert.False(string.IsNullOrWhiteSpace(scenario.RegressionTestId)));
    }

    [Theory]
    [MemberData(nameof(NegativeScenarioCases))]
    public void Scenario_evaluator_matches_expected_failure_guidance(
        string scenarioId,
        string failureMessage)
    {
        var scenario = PlatformDeploymentScenarioMatrix.Find(scenarioId)
            ?? throw new InvalidOperationException($"Scenario '{scenarioId}' not found.");

        var result = PlatformDeploymentScenarioEvaluator.Evaluate(scenario, operationSucceeded: false, failureMessage);

        Assert.True(result.Passed, result.Message);
    }

    [Fact]
    public void DEP_REG_LOG_PERM_fails_when_logs_directory_is_read_only()
    {
        var blockingFile = Path.GetTempFileName();
        try
        {
            var error = HostPathWriteAccessValidator.ValidateSingleWritablePath(blockingFile);

            Assert.NotNull(error);
            Assert.Contains("Permission denied", error, StringComparison.OrdinalIgnoreCase);

            var scenario = PlatformDeploymentScenarioMatrix.Find("log-permission-denied")!;
            var evaluation = PlatformDeploymentScenarioEvaluator.Evaluate(scenario, false, error!);
            Assert.True(evaluation.Passed, evaluation.Message);
        }
        finally
        {
            File.Delete(blockingFile);
        }
    }

    [Fact]
    public void DEP_S11_005_missing_certificate_fails_before_compose_with_clear_message()
    {
        var configRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(configRoot, "secrets", "local"));
        File.WriteAllText(
            Path.Combine(configRoot, "secrets", "local", "settings.secrets.json"),
            """{"certificatePassword":"secret"}""");

        try
        {
            var contract = CreateIdentityCertificateContract();
            var report = ContractCertificateValidator.Validate(configRoot, contract);

            Assert.False(report.Passed);
            var message = string.Join("; ", report.Results.SelectMany(item => item.Errors));
            Assert.Contains("Certificate file does not exist", message, StringComparison.Ordinal);

            var scenario = PlatformDeploymentScenarioMatrix.Find("missing-certificate")!;
            var evaluation = PlatformDeploymentScenarioEvaluator.Evaluate(scenario, false, message);
            Assert.True(evaluation.Passed, evaluation.Message);
        }
        finally
        {
            Directory.Delete(configRoot, recursive: true);
        }
    }

    [Fact]
    public void DEP_S11_006_wrong_certificate_password_fails_before_compose()
    {
        var configRoot = CreateConfigRoot("wrong-password");
        var contract = CreateIdentityCertificateContract();
        CreatePfx(configRoot, contract, "correct-password");

        var report = ContractCertificateValidator.Validate(configRoot, contract);

        Assert.False(report.Passed);
        var message = string.Join("; ", report.Results.SelectMany(item => item.Errors));
        Assert.Contains("Certificate password is incorrect", message, StringComparison.OrdinalIgnoreCase);

        var scenario = PlatformDeploymentScenarioMatrix.Find("wrong-certificate-password")!;
        var evaluation = PlatformDeploymentScenarioEvaluator.Evaluate(scenario, false, message);
        Assert.True(evaluation.Passed, evaluation.Message);
    }

    [Fact]
    public void DEP_S11_012_missing_secret_reports_exact_secret_name()
    {
        var configRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(configRoot, "secrets", "local"));
        File.WriteAllText(
            Path.Combine(configRoot, "secrets", "local", "settings.secrets.json"),
            """{"jwtSigningKey":"value"}""");

        try
        {
            var report = ContractSecretValidator.Validate(
                configRoot,
                new SecretContract
                {
                    Secrets =
                    [
                        new SecretRequirementContract
                        {
                            Id = "certificate-password",
                            Name = "certificatePassword",
                            Required = true,
                            ConfigurationKey = "Identity:Signing:CertificatePassword"
                        }
                    ]
                });

            Assert.False(report.Passed);
            Assert.Contains(report.Errors, error => error.Contains("certificatePassword", StringComparison.Ordinal));

            var scenario = PlatformDeploymentScenarioMatrix.Find("missing-secrets")!;
            var evaluation = PlatformDeploymentScenarioEvaluator.Evaluate(scenario, false, string.Join("; ", report.Errors));
            Assert.True(evaluation.Passed, evaluation.Message);
        }
        finally
        {
            Directory.Delete(configRoot, recursive: true);
        }
    }

    [Fact]
    public async Task DEP_S11_011_missing_folders_are_auto_created()
    {
        var configRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var bundle = ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = "gateway",
            DisplayName = "Gateway",
            Version = "1.0.0",
            ImageReference = "aovias/gateway:1.0.0",
            ContainerName = "gateway",
            EntryPoint = "Gateway.dll",
            HealthEndpoint = "/health",
            Ports = [5212]
        });

        var hostPaths = new HostPathContext
        {
            HostConfigRoot = configRoot,
            HostLogsRoot = Path.Combine(configRoot, "logs"),
            HostPackagesRoot = Path.Combine(configRoot, "packages")
        };

        var context = new InfrastructureProvisioningContext
        {
            Bundle = bundle,
            Contract = InfrastructureContractBuilder.Build(bundle),
            HostPaths = hostPaths,
            RuntimeCapabilities = bundle.RuntimeCapabilities
        };

        var report = await InfrastructureProvisioningPipeline.ExecuteAsync(context);
        var filesystemStage = report.Stages.First(stage => stage.Stage == "Filesystem");

        Assert.True(filesystemStage.Passed);
        Assert.Contains(
            filesystemStage.Messages,
            message => message.Contains("Created or verified directory", StringComparison.OrdinalIgnoreCase));

        Directory.Delete(configRoot, recursive: true);
    }

    [Fact]
    public async Task DEP_S11_008_sql_unavailable_includes_recovery_advice()
    {
        var execution = new UnreachableSqlExecutionContext();
        var configRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(configRoot, "secrets", "local"));
        File.WriteAllText(
            Path.Combine(configRoot, "secrets", "local", "settings.secrets.json"),
            """{"databaseConnectionString":"Server=sql-unavailable.test;Database=Compliance;User Id=sa;Password=secret;TrustServerCertificate=True;"}""");

        var bundle = ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = "compliance-service",
            DisplayName = "Compliance Service",
            Version = "1.0.0",
            ImageReference = "aovias/compliance-service:1.0.0",
            ContainerName = "compliance-service",
            EntryPoint = "ComplianceService.dll",
            HealthEndpoint = "/health",
            Ports = [5001]
        });

        var context = new InfrastructureProvisioningContext
        {
            Bundle = bundle,
            Contract = InfrastructureContractBuilder.Build(bundle),
            HostPaths = new HostPathContext
            {
                HostConfigRoot = configRoot,
                HostLogsRoot = Path.Combine(configRoot, "logs"),
                HostPackagesRoot = Path.Combine(configRoot, "packages")
            },
            RuntimeCapabilities = new RuntimeCapabilities { Docker = true, SqlServer = true },
            Execution = execution
        };

        var databaseStage = await new DatabaseInfrastructureProvisioner().ProvisionAsync(context);
        Assert.False(databaseStage.Passed);
        var message = string.Join("; ", databaseStage.Errors);
        Assert.Contains("Database server is unreachable", message, StringComparison.Ordinal);

        var recovery = InfrastructureRecoveryCoordinator.DescribeRecoveryActions(
            InfrastructureReadinessReport.Create(bundle, [databaseStage], []));
        Assert.Contains(recovery, action => action.Contains("verify database connection", StringComparison.OrdinalIgnoreCase));

        var scenario = PlatformDeploymentScenarioMatrix.Find("sql-unavailable")!;
        var evaluation = PlatformDeploymentScenarioEvaluator.Evaluate(scenario, false, message + " | " + string.Join("; ", recovery));
        Assert.True(evaluation.Passed, evaluation.Message);

        Directory.Delete(configRoot, recursive: true);
    }

    [Fact]
    public async Task DEP_S11_009_rabbitmq_unavailable_fails_with_recovery_advice()
    {
        var execution = new UnreachableRabbitExecutionContext();
        var configRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(configRoot, "secrets", "local"));
        File.WriteAllText(
            Path.Combine(configRoot, "secrets", "local", "settings.secrets.json"),
            """{"RabbitMQ:Password":"amqp://guest:guest@rabbit-unavailable.test:5672/"}""");

        var bundle = ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = "compliance-service",
            DisplayName = "Compliance Service",
            Version = "1.0.0",
            ImageReference = "aovias/compliance-service:1.0.0",
            ContainerName = "compliance-service",
            EntryPoint = "ComplianceService.dll",
            HealthEndpoint = "/health",
            Ports = [5001]
        });

        var contract = InfrastructureContractBuilder.Build(bundle);
        var context = new InfrastructureProvisioningContext
        {
            Bundle = bundle,
            Contract = contract,
            HostPaths = new HostPathContext
            {
                HostConfigRoot = configRoot,
                HostLogsRoot = Path.Combine(configRoot, "logs"),
                HostPackagesRoot = Path.Combine(configRoot, "packages")
            },
            RuntimeCapabilities = new RuntimeCapabilities { Docker = true, RabbitMq = true },
            Execution = execution
        };

        var providerStage = await new ProviderInfrastructureProvisioner().ProvisionAsync(context);
        Assert.False(providerStage.Passed);
        var message = string.Join("; ", providerStage.Errors);
        Assert.Contains("RabbitMQ is unreachable", message, StringComparison.OrdinalIgnoreCase);

        var scenario = PlatformDeploymentScenarioMatrix.Find("rabbitmq-unavailable")!;
        var evaluation = PlatformDeploymentScenarioEvaluator.Evaluate(scenario, false, message);
        Assert.True(evaluation.Passed, evaluation.Message);

        Directory.Delete(configRoot, recursive: true);
    }

    [Fact]
    public void DEP_S11_013_wrong_api_key_is_rejected()
    {
        var store = new EnvironmentRegistrationStore();
        var registration = store.Register(new AgentRegistrationRequest
        {
            RegistrationToken = "demo-vps-registration-token",
            EnvironmentName = "Demo",
            Hostname = "demo.test",
            PublicUrl = "https://demo.test",
            EnvironmentType = EnvironmentType.Demo,
            RuntimeVersion = "1.0.0"
        });

        Assert.False(store.ValidateApiKey("invalid-api-key"));
        Assert.True(store.ValidateApiKey(registration.ApiKey));

        var scenario = PlatformDeploymentScenarioMatrix.Find("wrong-api-key")!;
        var evaluation = PlatformDeploymentScenarioEvaluator.Evaluate(scenario, false, "API key is invalid.");
        Assert.True(evaluation.Passed, evaluation.Message);
    }

    [Fact]
    public void DEP_S11_014_and_015_persist_recoverable_deployment_sessions()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"aovias-runtime-{Guid.NewGuid():N}");
        var releaseRoot = Path.Combine(tempRoot, "releases");
        var packageRoot = Path.Combine(tempRoot, "runtime", "packages");
        var pathResolver = RuntimeAgentDeploymentTestSupport.CreateResolver(releaseRoot, packageRoot);
        var store = new DeploymentSessionStore(pathResolver);
        var record = new PlatformDeploymentRecord
        {
            DeploymentId = Guid.NewGuid().ToString("N"),
            Operator = "system",
            Runtime = "docker",
            Environment = "demo",
            State = PlatformDeploymentState.Deploying
        };

        try
        {
            store.Save(record);
            var recoverable = store.ListRecoverable();

            Assert.Contains(recoverable, item => item.DeploymentId == record.DeploymentId);
            Assert.All(
                PlatformDeploymentScenarioMatrix.All.Where(item =>
                    item.Id is "runtime-restart-during-deploy" or "power-interruption"),
                scenario =>
                {
                    var evaluation = PlatformDeploymentScenarioEvaluator.Evaluate(
                        scenario,
                        false,
                        "Recoverable deployment session persisted.");
                    Assert.True(evaluation.Passed, evaluation.Message);
                });

            store.Delete(record.DeploymentId);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    [Fact]
    public void Certification_checklist_contains_all_sprint11_rows()
    {
        var evidence = new PlatformCertificationEvidence
        {
            PackageGenerated = true,
            ChecksumsVerified = true,
            ContractsGenerated = true,
            ManifestValid = true,
            UploadComplete = true,
            UploadHashVerified = true,
            PackageStored = true,
            FilesystemReady = true,
            CertificatesReady = true,
            SecretsReady = true,
            DatabaseReady = true,
            NetworksReady = true,
            StorageReady = true,
            ImagesLoaded = true,
            ContainersStarted = true,
            HealthGreen = true,
            GatewayReachable = true,
            IdentityIssuesToken = true,
            ComplianceReachesDatabase = true,
            PortalLoads = true,
            RuntimeAgentHealthy = true
        };

        var checklist = PlatformCertificationChecklistBuilder.Build(evidence);

        Assert.Equal(21, checklist.Items.Count);
        Assert.True(checklist.AllRequiredPassed);
    }

    [Fact]
    public void Platform_is_not_certified_when_functional_checks_fail()
    {
        var manifest = new PlatformDeploymentManifest
        {
            Profile = "demo",
            Version = "1.0.0",
            Services = []
        };

        var report = PlatformCertificationBuilder.Build(
            "dep-fail",
            manifest,
            new InfrastructureReadinessSnapshot
            {
                Infrastructure = "PASS",
                Contracts = "PASS",
                Containers = "PASS",
                Dependencies = "PASS",
                Health = "PASS",
                Runtime = "Healthy"
            },
            new SmokeTestSuiteResult
            {
                Passed = false,
                PassedCount = 0,
                FailedCount = 1,
                Results =
                [
                    new SmokeTestExecutionResult
                    {
                        ServiceId = "gateway",
                        TestId = "health",
                        TestName = "health",
                        Passed = false,
                        Message = "Connection refused"
                    }
                ]
            },
            []);

        Assert.False(report.IsCertified);
        Assert.False(report.Checklist.AllRequiredPassed);
    }

    public static IEnumerable<object[]> NegativeScenarioCases()
    {
        foreach (var scenario in PlatformDeploymentScenarioMatrix.All.Where(item =>
                     item.ExpectedOutcome == DeploymentScenarioOutcome.FailWithClearMessage))
        {
            yield return [scenario.Id, string.Join(' ', scenario.ExpectedMessageFragments)];
        }
    }

    private static CertificateContract CreateIdentityCertificateContract()
        => new()
        {
            Certificates =
            [
                new CertificateRequirementContract
                {
                    Id = "identity-signing",
                    Required = true,
                    Type = "PFX",
                    Category = "Signing",
                    MountPath = PlatformRuntimeDefaults.CertificatesRoot,
                    FileName = "identity-signing.pfx",
                    SecretKey = "certificatePassword"
                }
            ]
        };

    private static string CreateConfigRoot(string certificatePassword)
    {
        var configRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var secretsDirectory = Path.Combine(configRoot, "secrets", "local");
        Directory.CreateDirectory(secretsDirectory);
        File.WriteAllText(
            Path.Combine(secretsDirectory, "settings.secrets.json"),
            JsonSerializer.Serialize(new { certificatePassword }));
        return configRoot;
    }

    private static void CreatePfx(string configRoot, CertificateContract contract, string password)
    {
        var requirement = contract.Certificates[0];
        var certificatePath = ContractCertificateProvisioner.ResolveCertificatePath(configRoot, requirement);
        Directory.CreateDirectory(Path.GetDirectoryName(certificatePath)!);

        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Test Signing",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
        File.WriteAllBytes(certificatePath, certificate.Export(X509ContentType.Pfx, password));
    }

    private sealed class UnreachableSqlExecutionContext : IInfrastructureExecutionContext
    {
        public Task<DockerEngineStatus> InspectDockerEngineAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new DockerEngineStatus { Available = true, ComposeAvailable = true, Version = "test" });

        public Task EnsureDockerNetworkAsync(string networkName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<bool> ImageExistsLocallyAsync(string imageReference, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<DatabaseReachabilityResult> ValidateDatabaseAsync(string connectionString, CancellationToken cancellationToken = default)
            => Task.FromResult(DatabaseReachabilityResult.Failure("Database server is unreachable: connection refused"));

        public Task<MessageBusReachabilityResult> ValidateMessageBusAsync(string connectionString, CancellationToken cancellationToken = default)
            => Task.FromResult(MessageBusReachabilityResult.Success());
    }

    private sealed class UnreachableRabbitExecutionContext : IInfrastructureExecutionContext
    {
        public Task<DockerEngineStatus> InspectDockerEngineAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new DockerEngineStatus { Available = true, ComposeAvailable = true, Version = "test" });

        public Task EnsureDockerNetworkAsync(string networkName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<bool> ImageExistsLocallyAsync(string imageReference, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<DatabaseReachabilityResult> ValidateDatabaseAsync(string connectionString, CancellationToken cancellationToken = default)
            => Task.FromResult(DatabaseReachabilityResult.Success());

        public Task<MessageBusReachabilityResult> ValidateMessageBusAsync(string connectionString, CancellationToken cancellationToken = default)
            => Task.FromResult(MessageBusReachabilityResult.Failure("connection refused"));
    }
}
