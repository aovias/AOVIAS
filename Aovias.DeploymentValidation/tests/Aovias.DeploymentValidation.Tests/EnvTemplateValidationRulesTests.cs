using Aovias.Configuration.Deployment;
using Aovias.Configuration.Settings;
using Aovias.DeploymentValidation.Rules;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

public sealed class EnvTemplateValidationRulesTests
{
    [Fact]
    public void Validate_FailsWhenPlaceholderHostPathRemains()
    {
        const string content = """
                               DOTNET_ENVIRONMENT=Production
                               AOVIAS_CONFIG_ROOT=/opt/aovias/runtime/config
                               AOVIAS_ACTIVE_PROFILE=demo
                               AOVIAS_PROFILES_DIRECTORY=profiles
                               AOVIAS_SECRETS_DIRECTORY=secrets
                               AOVIAS_PACKAGES_ROOT=/opt/aovias/runtime/packages
                               AOVIAS_LOGS_ROOT=/opt/aovias/runtime/logs
                               AOVIAS_DOCKER_NETWORK=aovias-network
                               AOVIAS_HOST_CONFIG_ROOT=/path/to/aovias-config
                               AOVIAS_HOST_PACKAGES_ROOT=/opt/aovias/runtime/packages
                               AOVIAS_HOST_LOGS_ROOT=/opt/aovias/runtime/logs
                               """;

        var violations = EnvTemplateValidationRules.Validate(content, ".env.template");

        Assert.Contains(violations, violation => violation.Contains("unresolved placeholder", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_PassesForResolvedDemoProfileValues()
    {
        var expected = new DeploymentHostSettings
        {
            DotNetEnvironment = "Production",
            ConfigRoot = "/opt/aovias/runtime/config",
            PackagesRoot = "/opt/aovias/runtime/packages",
            LogsRoot = "/opt/aovias/runtime/logs",
            HostConfigRoot = "/opt/aovias/runtime/config",
            HostPackagesRoot = "/opt/aovias/runtime/packages",
            HostLogsRoot = "/opt/aovias/runtime/logs",
            DockerNetwork = "aovias-network",
            ProfilesDirectory = "profiles",
            SecretsDirectory = "secrets"
        };

        const string content = """
                               DOTNET_ENVIRONMENT=Production
                               AOVIAS_CONFIG_ROOT=/opt/aovias/runtime/config
                               AOVIAS_ACTIVE_PROFILE=demo
                               AOVIAS_PROFILES_DIRECTORY=profiles
                               AOVIAS_SECRETS_DIRECTORY=secrets
                               AOVIAS_PACKAGES_ROOT=/opt/aovias/runtime/packages
                               AOVIAS_LOGS_ROOT=/opt/aovias/runtime/logs
                               AOVIAS_DEPLOYMENTS_ROOT=/opt/aovias/deployments
                               AOVIAS_DOCKER_NETWORK=aovias-network
                               AOVIAS_HOST_CONFIG_ROOT=/opt/aovias/runtime/config
                               AOVIAS_HOST_PACKAGES_ROOT=/opt/aovias/runtime/packages
                               AOVIAS_HOST_LOGS_ROOT=/opt/aovias/runtime/logs
                               AOVIAS_HOST_DEPLOYMENTS_ROOT=${AOVIAS_HOST_DEPLOYMENTS_ROOT}
                               """;

        var violations = EnvTemplateValidationRules.Validate(content, ".env.template", expected);

        // Sprint G1: first real execution of this test. AOVIAS_DEPLOYMENTS_ROOT/
        // AOVIAS_HOST_DEPLOYMENTS_ROOT became required variables in a later sprint
        // (M4.1 bootstrap) without this fixture being updated to match — added above.
        Assert.Empty(violations);
    }
}
