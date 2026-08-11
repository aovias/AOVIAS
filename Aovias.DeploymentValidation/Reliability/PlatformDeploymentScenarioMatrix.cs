namespace Aovias.DeploymentValidation.Reliability;

/// <summary>
/// Expected outcome for a deployment hardening scenario (Sprint 11 test matrix).
/// </summary>
public enum DeploymentScenarioOutcome
{
    Pass,
    FailWithClearMessage,
    AutoRecover
}

/// <summary>
/// One row in the Sprint 11 deployment test matrix.
/// </summary>
public sealed class DeploymentScenarioDefinition
{
    public required string Id { get; init; }

    public required string Scenario { get; init; }

    public DeploymentScenarioOutcome ExpectedOutcome { get; init; }

    public IReadOnlyList<string> ExpectedMessageFragments { get; init; } = [];

    public string RegressionTestId { get; init; } = string.Empty;
}

/// <summary>
/// Canonical deployment test matrix — every scenario must pass before production promotion.
/// </summary>
public static class PlatformDeploymentScenarioMatrix
{
    public static IReadOnlyList<DeploymentScenarioDefinition> All { get; } =
    [
        new()
        {
            Id = "fresh-vps",
            Scenario = "Fresh VPS",
            ExpectedOutcome = DeploymentScenarioOutcome.Pass,
            RegressionTestId = "DEP-S11-001"
        },
        new()
        {
            Id = "redeploy-same-version",
            Scenario = "Redeploy same version",
            ExpectedOutcome = DeploymentScenarioOutcome.Pass,
            RegressionTestId = "DEP-S11-002"
        },
        new()
        {
            Id = "upgrade-version",
            Scenario = "Upgrade version",
            ExpectedOutcome = DeploymentScenarioOutcome.Pass,
            RegressionTestId = "DEP-S11-003"
        },
        new()
        {
            Id = "rollback",
            Scenario = "Rollback",
            ExpectedOutcome = DeploymentScenarioOutcome.Pass,
            RegressionTestId = "DEP-S11-004"
        },
        new()
        {
            Id = "missing-certificate",
            Scenario = "Missing certificate",
            ExpectedOutcome = DeploymentScenarioOutcome.FailWithClearMessage,
            ExpectedMessageFragments = ["Certificate file does not exist"],
            RegressionTestId = "DEP-S11-005"
        },
        new()
        {
            Id = "wrong-certificate-password",
            Scenario = "Wrong certificate password",
            ExpectedOutcome = DeploymentScenarioOutcome.FailWithClearMessage,
            ExpectedMessageFragments = ["Certificate password is incorrect", "certificatePassword"],
            RegressionTestId = "DEP-S11-006"
        },
        new()
        {
            Id = "missing-database",
            Scenario = "Missing database",
            ExpectedOutcome = DeploymentScenarioOutcome.FailWithClearMessage,
            ExpectedMessageFragments = ["Database connection secret is missing", "settings.secrets.json"],
            RegressionTestId = "DEP-S11-007"
        },
        new()
        {
            Id = "sql-unavailable",
            Scenario = "SQL unavailable",
            ExpectedOutcome = DeploymentScenarioOutcome.FailWithClearMessage,
            ExpectedMessageFragments = ["Database server is unreachable", "Recovery: verify database connection"],
            RegressionTestId = "DEP-S11-008"
        },
        new()
        {
            Id = "rabbitmq-unavailable",
            Scenario = "RabbitMQ unavailable",
            ExpectedOutcome = DeploymentScenarioOutcome.FailWithClearMessage,
            ExpectedMessageFragments = ["RabbitMQ", "unreachable"],
            RegressionTestId = "DEP-S11-009"
        },
        new()
        {
            Id = "missing-docker-network",
            Scenario = "Missing Docker network",
            ExpectedOutcome = DeploymentScenarioOutcome.AutoRecover,
            ExpectedMessageFragments = ["Docker network"],
            RegressionTestId = "DEP-S11-010"
        },
        new()
        {
            Id = "missing-folders",
            Scenario = "Missing folders",
            ExpectedOutcome = DeploymentScenarioOutcome.AutoRecover,
            ExpectedMessageFragments = ["Created or verified directory", "filesystem directory"],
            RegressionTestId = "DEP-S11-011"
        },
        new()
        {
            Id = "missing-secrets",
            Scenario = "Missing secrets",
            ExpectedOutcome = DeploymentScenarioOutcome.FailWithClearMessage,
            ExpectedMessageFragments = ["Required secret", "settings.secrets.json"],
            RegressionTestId = "DEP-S11-012"
        },
        new()
        {
            Id = "wrong-api-key",
            Scenario = "Wrong API key",
            ExpectedOutcome = DeploymentScenarioOutcome.FailWithClearMessage,
            ExpectedMessageFragments = ["API key is invalid", "Authentication"],
            RegressionTestId = "DEP-S11-013"
        },
        new()
        {
            Id = "runtime-restart-during-deploy",
            Scenario = "Runtime restart during deploy",
            ExpectedOutcome = DeploymentScenarioOutcome.AutoRecover,
            ExpectedMessageFragments = ["Recoverable deployment", "resume"],
            RegressionTestId = "DEP-S11-014"
        },
        new()
        {
            Id = "power-interruption",
            Scenario = "Power interruption",
            ExpectedOutcome = DeploymentScenarioOutcome.AutoRecover,
            ExpectedMessageFragments = ["Recoverable deployment"],
            RegressionTestId = "DEP-S11-015"
        },
        new()
        {
            Id = "log-permission-denied",
            Scenario = "Log directory permission denied",
            ExpectedOutcome = DeploymentScenarioOutcome.FailWithClearMessage,
            ExpectedMessageFragments = ["Permission denied", "logs"],
            RegressionTestId = "DEP-REG-LOG-PERM"
        }
    ];

    public static DeploymentScenarioDefinition? Find(string id)
        => All.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
}
