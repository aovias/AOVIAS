namespace Shared.Communication;

public sealed class CommunicationRequestDto
{
    public required string ModuleCode { get; init; }
    public required string EventType { get; init; }
    public Guid? PersonId { get; init; }
    public string? RecipientEmail { get; init; }
    public string? RecipientPhone { get; init; }
    public string? TemplateCode { get; init; }
    public string? Subject { get; init; }
    public string? Body { get; init; }
    public Dictionary<string, string>? Variables { get; init; }
    public bool Critical { get; init; }
    public string? DedupeKey { get; init; }
    public IReadOnlyList<string>? PreferredChannels { get; init; }
}

public sealed class CommunicationSendResultDto
{
    public Guid OutboundMessageId { get; init; }
    public string Status { get; init; } = string.Empty;
}

public interface ICommunicationClient
{
    Task<CommunicationSendResultDto> SendAsync(CommunicationRequestDto request, CancellationToken ct = default);
}
