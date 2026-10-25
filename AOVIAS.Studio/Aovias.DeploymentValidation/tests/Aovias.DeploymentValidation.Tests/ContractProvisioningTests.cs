using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Building;
using Aovias.Deployment.Contracts.Provisioning;
using Aovias.Deployment.Contracts.Reading;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Xunit;

namespace Aovias.DeploymentValidation.Tests;

public sealed class ContractProvisioningTests
{
    [Fact]
    public void IdentityDeploymentContractBuilder_WritesIdentityContractFiles()
    {
        var bundle = CreateIdentityBundle();
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            ServiceDeploymentBundleIO.WriteDirectory(directory, bundle);

            Assert.True(File.Exists(Path.Combine(directory, DeploymentPackageLayout.IdentityContractFile)));
            Assert.True(File.Exists(Path.Combine(directory, DeploymentPackageLayout.IdentitySecretsContractFile)));

            var identityContract = IdentityDeploymentContractBuilder.BuildIdentityContract(bundle);
            Assert.Equal("Pfx", identityContract.SigningCredential.Type);
            Assert.Equal("/opt/aovias/runtime/config/certificates/identity-signing.pfx", identityContract.SigningCredential.Path);
            Assert.Equal("certificatePassword", identityContract.SigningCredential.PasswordSecret);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void ContractCertificateValidator_FailsWhenPasswordDoesNotOpenPfx()
    {
        var configRoot = CreateConfigRoot("wrong-password");
        var contract = CreateIdentityCertificateContract();
        CreatePfx(configRoot, contract, "correct-password");

        var report = ContractCertificateValidator.Validate(configRoot, contract);

        Assert.False(report.Passed);
        Assert.Contains(report.Results, item => item.Errors.Any(error => error.Contains("password is incorrect", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void ContractCertificateValidator_PassesWhenPasswordOpensPfx()
    {
        const string password = "correct-password";
        var configRoot = CreateConfigRoot(password);
        var contract = CreateIdentityCertificateContract();
        CreatePfx(configRoot, contract, password);

        var report = ContractCertificateValidator.Validate(configRoot, contract);

        Assert.True(report.Passed);
        Assert.All(report.Results, item => Assert.True(item.Passed));
    }

    [Fact]
    public void ContractSecretValidator_FailsWhenRequiredSecretMissing()
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
        }
        finally
        {
            Directory.Delete(configRoot, recursive: true);
        }
    }

    private static ServiceDeploymentBundle CreateIdentityBundle()
        => ServiceContractBuilder.Build(new ServiceContractBuildRequest
        {
            ServiceId = "identity-service",
            DisplayName = "Identity Service",
            Version = "1.0.0",
            ImageReference = "aovias/identity-service:1.0.0",
            ContainerName = "identity-service",
            EntryPoint = "IdentityService.dll",
            HealthEndpoint = "/health",
            Ports = [5062],
            ConfigurationKeys = ["ConnectionStrings:DefaultConnection"]
        });

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
}
