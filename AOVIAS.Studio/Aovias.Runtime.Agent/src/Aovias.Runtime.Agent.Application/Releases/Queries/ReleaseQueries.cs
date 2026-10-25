using Aovias.Release.Contracts.Releases;
using Aovias.ReleaseManager.Abstractions;
using MediatR;

namespace Aovias.Runtime.Agent.Application.Releases.Queries;

/// <summary>
/// Gets all releases known to the runtime agent.
/// </summary>
public sealed record GetReleasesQuery : IRequest<IReadOnlyList<ReleaseListItem>>;

/// <summary>
/// Handles <see cref="GetReleasesQuery"/>.
/// </summary>
public sealed class GetReleasesQueryHandler : IRequestHandler<GetReleasesQuery, IReadOnlyList<ReleaseListItem>>
{
    private readonly IReleaseManager releaseManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetReleasesQueryHandler"/> class.
    /// </summary>
    public GetReleasesQueryHandler(IReleaseManager releaseManager)
    {
        this.releaseManager = releaseManager;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ReleaseListItem>> Handle(GetReleasesQuery request, CancellationToken cancellationToken)
        => releaseManager.GetReleasesAsync(cancellationToken);
}

/// <summary>
/// Gets release history entries.
/// </summary>
public sealed record GetReleaseHistoryQuery(ReleaseHistorySearchCriteria? Criteria = null) : IRequest<IReadOnlyList<ReleaseHistoryEntry>>;

/// <summary>
/// Gets deployment history detail events.
/// </summary>
public sealed record GetReleaseHistoryDetailQuery(string DeploymentId) : IRequest<IReadOnlyList<Aovias.Release.Contracts.Audit.AuditEvent>>;

/// <summary>
/// Handles <see cref="GetReleaseHistoryQuery"/>.
/// </summary>
public sealed class GetReleaseHistoryQueryHandler : IRequestHandler<GetReleaseHistoryQuery, IReadOnlyList<ReleaseHistoryEntry>>
{
    private readonly IReleaseManager releaseManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetReleaseHistoryQueryHandler"/> class.
    /// </summary>
    public GetReleaseHistoryQueryHandler(IReleaseManager releaseManager)
    {
        this.releaseManager = releaseManager;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ReleaseHistoryEntry>> Handle(GetReleaseHistoryQuery request, CancellationToken cancellationToken)
        => releaseManager.GetHistoryAsync(request.Criteria, cancellationToken);
}

/// <summary>
/// Handles <see cref="GetReleaseHistoryDetailQuery"/>.
/// </summary>
public sealed class GetReleaseHistoryDetailQueryHandler : IRequestHandler<GetReleaseHistoryDetailQuery, IReadOnlyList<Aovias.Release.Contracts.Audit.AuditEvent>>
{
    private readonly IReleaseHistoryProvider historyProvider;

    public GetReleaseHistoryDetailQueryHandler(IReleaseHistoryProvider historyProvider)
    {
        this.historyProvider = historyProvider;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Aovias.Release.Contracts.Audit.AuditEvent>> Handle(
        GetReleaseHistoryDetailQuery request,
        CancellationToken cancellationToken)
        => historyProvider.GetDeploymentDetailAsync(request.DeploymentId, cancellationToken);
}

/// <summary>
/// Gets a release by identifier.
/// </summary>
public sealed record GetReleaseByIdQuery(string ReleaseId) : IRequest<ReleaseRecord?>;

/// <summary>
/// Handles <see cref="GetReleaseByIdQuery"/>.
/// </summary>
public sealed class GetReleaseByIdQueryHandler : IRequestHandler<GetReleaseByIdQuery, ReleaseRecord?>
{
    private readonly IReleaseManager releaseManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetReleaseByIdQueryHandler"/> class.
    /// </summary>
    public GetReleaseByIdQueryHandler(IReleaseManager releaseManager)
    {
        this.releaseManager = releaseManager;
    }

    /// <inheritdoc />
    public Task<ReleaseRecord?> Handle(GetReleaseByIdQuery request, CancellationToken cancellationToken)
        => releaseManager.GetReleaseAsync(request.ReleaseId, cancellationToken);
}
