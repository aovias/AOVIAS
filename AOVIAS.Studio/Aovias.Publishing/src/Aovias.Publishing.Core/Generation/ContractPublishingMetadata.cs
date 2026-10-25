using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Common;

namespace Aovias.Publishing.Core.Generation;

/// <summary>
/// The publish-time inputs <see cref="InfrastructureSpecificationCompiler"/> stamps onto the
/// schema v2 envelope (Sprint M2.2 D1/D2). Kept as its own small, fully-required type — rather
/// than passing <see cref="PublishingBuildIdentity"/> straight through — so the compiler's
/// inputs are explicit and so unit tests can supply a fixed, deterministic value without
/// constructing a full publishing pipeline context.
/// </summary>
internal sealed class ContractPublishingMetadata
{
    public required string SourceRevision { get; init; }

    public required string PublishingEngineVersion { get; init; }

    public required DateTimeOffset GeneratedUtc { get; init; }

    /// <summary>
    /// Builds metadata from the pipeline's build identity, established once per publish run
    /// (<see cref="PublishingBuildIdentity.BuildUtc"/>) — never a fresh clock read here, so that
    /// compiling the same publish run's contract twice is byte-for-byte deterministic (D2).
    /// </summary>
    public static ContractPublishingMetadata FromBuildIdentity(PublishingBuildIdentity? identity) => new()
    {
        SourceRevision = identity?.SourceRevision ?? string.Empty,
        PublishingEngineVersion = identity?.PublishingEngineVersion ?? PublishingVersionPolicy.EngineVersion,
        GeneratedUtc = identity?.BuildUtc ?? DateTimeOffset.UtcNow
    };
}
