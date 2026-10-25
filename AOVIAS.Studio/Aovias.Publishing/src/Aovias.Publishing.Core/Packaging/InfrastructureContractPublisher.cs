using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Deployment.Contracts.Migration;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Generation;
using Aovias.Deployment.Contracts.Serialization;
namespace Aovias.Publishing.Core.Packaging;

/// <summary>
/// The sole production writer for the authoritative infrastructure contract (Sprint M2.0;
/// schema v2 validation added Sprint M2.2 D6). Fails fast, before anything touches disk, if the
/// compiler ever produced a contract that is not schema-correct or is missing required
/// metadata — Publishing must never seal an invalid contract into a package.
/// </summary>
internal static class InfrastructureContractPublisher
{
    public static InfrastructureContract Write(string packageDirectory, ServiceDeploymentBundle bundle, PublishingBuildIdentity? buildIdentity)
    {
        var contract = InfrastructureSpecificationCompiler.Compile(bundle, ContractPublishingMetadata.FromBuildIdentity(buildIdentity));
        ValidateBeforeWrite(contract);
        DeploymentContractJson.WriteFile(Path.Combine(packageDirectory, DeploymentPackageLayout.InfrastructureContractFile), contract);
        return contract;
    }

    /// <summary>
    /// Publishing-side schema validation (D6): schema correctness, required metadata, and
    /// deterministic (idempotent) serialization. Checksum consistency and the byte-for-byte
    /// round trip are re-verified independently, moments later, when
    /// <see cref="Aovias.Publishing.Core.Packaging.DeployPackageBuilder"/> calls
    /// <c>IDeploymentPackageContractReader.ReadFromDirectory</c> on the freshly written staging
    /// directory — this method exists to fail before any bytes are written at all.
    /// </summary>
    private static void ValidateBeforeWrite(InfrastructureContract contract)
    {
        var errors = new List<string>();

        if (contract.SchemaVersion != ContractSchemaVersions.Current)
        {
            errors.Add($"Compiled contract declares schema version {contract.SchemaVersion}, expected {ContractSchemaVersions.Current}.");
        }

        if (contract.ContractVersion != ContractSchemaVersions.ContractVersionV2)
        {
            errors.Add($"Compiled contract declares contract version '{contract.ContractVersion}', expected '{ContractSchemaVersions.ContractVersionV2}'.");
        }

        if (string.IsNullOrWhiteSpace(contract.ServiceId))
        {
            errors.Add("Compiled contract is missing serviceId.");
        }

        if (string.IsNullOrWhiteSpace(contract.PublishingEngineVersion))
        {
            errors.Add("Compiled contract is missing publishingEngineVersion.");
        }

        if (string.IsNullOrWhiteSpace(contract.Integrity.ContentHash))
        {
            errors.Add("Compiled contract is missing its integrity content hash.");
        }

        if (!ContractSchemaVersions.Supported.Contains(contract.Compatibility.MaximumSupportedSchemaVersion))
        {
            errors.Add($"Compiled contract declares an unrecognized maximum supported schema version {contract.Compatibility.MaximumSupportedSchemaVersion}.");
        }

        var first = DeploymentContractJson.Serialize(contract);
        var second = DeploymentContractJson.Serialize(contract);
        if (first != second)
        {
            errors.Add("Compiled contract does not serialize deterministically.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Infrastructure contract for '{contract.ServiceId}' failed publish-time schema validation: {string.Join("; ", errors)}");
        }
    }
}