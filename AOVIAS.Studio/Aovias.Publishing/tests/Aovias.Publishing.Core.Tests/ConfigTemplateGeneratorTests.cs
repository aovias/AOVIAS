using Aovias.DeploymentValidation.Rules;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Generation;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

public sealed class ConfigTemplateGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_redacts_secret_keys_in_appsettings()
    {
        var root = Path.Combine(Path.GetTempPath(), "ape-config-test", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(root, "appsettings.json"),
                """
                {
                  "Application": {
                    "Environment": "Development"
                  },
                  "Database": {
                    "Password": "super-secret"
                  }
                }
                """);

            var context = new PublishingContext
            {
                Request = new PublishingRequest { ProjectPath = Path.Combine(root, "Sample.csproj") },
                Discovery = new ProjectDiscoveryResult
                {
                    ProjectType = PublishingProjectType.AspNetCore,
                    ProjectPath = Path.Combine(root, "Sample.csproj"),
                    ProjectRoot = root,
                    ServiceId = "sample",
                    ServiceName = "Sample",
                    ProviderName = "DotNet"
                }
            };

            var generator = new ConfigTemplateGenerator();
            var template = await generator.GenerateAsync(context, CancellationToken.None);

            Assert.Equal("Development", template["appsettings:Application:Environment"]);
            Assert.Equal("${Password}", template["appsettings:Database:Password"]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task GenerateAsync_runtime_agent_emits_bootstrap_only_template()
    {
        var context = new PublishingContext
        {
            Request = new PublishingRequest { ProjectPath = "Sample.csproj" },
            Discovery = new ProjectDiscoveryResult
            {
                ProjectType = PublishingProjectType.AspNetCore,
                ProjectPath = "src/Aovias.Runtime.Agent.Api/Aovias.Runtime.Agent.Api.csproj",
                ProjectRoot = "Aovias.Runtime.Agent",
                ServiceId = "runtime-agent",
                ServiceName = "Runtime Agent",
                ProviderName = "DotNet"
            }
        };

        var generator = new ConfigTemplateGenerator();
        await generator.GenerateAsync(context, CancellationToken.None);

        Assert.DoesNotContain("Application", context.ConfigurationTemplateJson, StringComparison.Ordinal);
        Assert.Contains("${AOVIAS_CONFIG_ROOT}", context.ConfigurationTemplateJson, StringComparison.Ordinal);
        Assert.NotNull(context.ConfigurationTemplateJson);
        Assert.Empty(EnvironmentNeutralRules.FindViolations(
            context.ConfigurationTemplateJson!,
            DeployPackageLayout.ConfigTemplateFile));
    }

    [Fact]
    public async Task GenerateAsync_neutralizes_localhost_application_settings()
    {
        var root = Path.Combine(Path.GetTempPath(), "ape-config-test", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(root, "appsettings.json"),
                """
                {
                  "Api": {
                    "BaseUrl": "http://localhost:7080"
                  }
                }
                """);

            var context = new PublishingContext
            {
                Request = new PublishingRequest { ProjectPath = Path.Combine(root, "Sample.csproj") },
                Discovery = new ProjectDiscoveryResult
                {
                    ProjectType = PublishingProjectType.AspNetCore,
                    ProjectPath = Path.Combine(root, "Sample.csproj"),
                    ProjectRoot = root,
                    ServiceId = "sample-service",
                    ServiceName = "Sample",
                    ProviderName = "DotNet"
                }
            };

            var generator = new ConfigTemplateGenerator();
            await generator.GenerateAsync(context, CancellationToken.None);

            Assert.DoesNotContain("localhost", context.ConfigurationTemplateJson, StringComparison.OrdinalIgnoreCase);
            Assert.NotNull(context.ConfigurationTemplateJson);
            Assert.Empty(EnvironmentNeutralRules.FindViolations(
                context.ConfigurationTemplateJson!,
                DeployPackageLayout.ConfigTemplateFile));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task GenerateAsync_neutralizes_service_urls_and_connection_strings()
    {
        var root = Path.Combine(Path.GetTempPath(), "ape-config-test", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(root, "appsettings.json"),
                """
                {
                  "ConnectionStrings": {
                    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=Sample;Trusted_Connection=true"
                  },
                  "Services": {
                    "ComplianceService": {
                      "BaseUrl": "https://localhost:7087"
                    },
                    "KnowledgeBaseService": {
                      "BaseUrl": "http://localhost:5120"
                    }
                  }
                }
                """);

            var context = new PublishingContext
            {
                Request = new PublishingRequest { ProjectPath = Path.Combine(root, "AIOrchestratorService.csproj") },
                Discovery = new ProjectDiscoveryResult
                {
                    ProjectType = PublishingProjectType.AspNetCore,
                    ProjectPath = Path.Combine(root, "AIOrchestratorService.csproj"),
                    ProjectRoot = root,
                    ServiceId = "AIOrchestratorService",
                    ServiceName = "AI Orchestrator",
                    ProviderName = "DotNet"
                }
            };

            var generator = new ConfigTemplateGenerator();
            await generator.GenerateAsync(context, CancellationToken.None);

            Assert.NotNull(context.ConfigurationTemplateJson);
            Assert.DoesNotContain("localhost", context.ConfigurationTemplateJson, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("${DefaultConnection}", context.ConfigurationTemplateJson, StringComparison.Ordinal);
            Assert.Contains("${BaseUrl}", context.ConfigurationTemplateJson, StringComparison.Ordinal);
            Assert.Empty(EnvironmentNeutralRules.FindViolations(
                context.ConfigurationTemplateJson!,
                DeployPackageLayout.ConfigTemplateFile));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void PrepareConfigurationTemplateJson_neutralizes_existing_localhost_payload()
    {
        const string raw = """
            {
              "Aovias": {
                "ConfigRoot": "${AOVIAS_CONFIG_ROOT}",
                "ActiveProfile": "${AOVIAS_ACTIVE_PROFILE}"
              },
              "Application": {
                "appsettings_Services_ComplianceService_BaseUrl": "https://localhost:7087"
              }
            }
            """;

        var sanitized = DeploymentArtifactSanitizer.PrepareConfigurationTemplateJson(raw);

        Assert.DoesNotContain("localhost", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(EnvironmentNeutralRules.FindViolations(
            sanitized,
            DeployPackageLayout.ConfigTemplateFile));
    }

    [Fact]
    public async Task GenerateAsync_excludes_environment_specific_bootstrap_keys()
    {
        var root = Path.Combine(Path.GetTempPath(), "ape-config-test", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(root, "appsettings.json"),
                """
                {
                  "Aovias": {
                    "ConfigRoot": "AOVIAS.Infrastructure/aovias-config",
                    "ActiveProfile": "local",
                    "ProfilesDirectory": "profiles",
                    "SecretsDirectory": "secrets"
                  }
                }
                """);

            await File.WriteAllTextAsync(
                Path.Combine(root, "appsettings.Development.json"),
                """
                {
                  "Aovias": {
                    "ActiveProfile": "local"
                  }
                }
                """);

            var context = new PublishingContext
            {
                Request = new PublishingRequest { ProjectPath = Path.Combine(root, "Sample.csproj") },
                Discovery = new ProjectDiscoveryResult
                {
                    ProjectType = PublishingProjectType.AspNetCore,
                    ProjectPath = Path.Combine(root, "Sample.csproj"),
                    ProjectRoot = root,
                    ServiceId = "runtime-agent",
                    ServiceName = "Runtime Agent",
                    ProviderName = "DotNet"
                }
            };

            var generator = new ConfigTemplateGenerator();
            var template = await generator.GenerateAsync(context, CancellationToken.None);

            Assert.DoesNotContain(template.Keys, key => key.Contains("Aovias:ConfigRoot", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(template.Keys, key => key.Contains("Aovias:ActiveProfile", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(template.Keys, key => key.StartsWith("appsettings.Development:", StringComparison.OrdinalIgnoreCase));
            Assert.Contains("${AOVIAS_CONFIG_ROOT}", context.ConfigurationTemplateJson, StringComparison.Ordinal);
            Assert.Contains("${AOVIAS_ACTIVE_PROFILE}", context.ConfigurationTemplateJson, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
