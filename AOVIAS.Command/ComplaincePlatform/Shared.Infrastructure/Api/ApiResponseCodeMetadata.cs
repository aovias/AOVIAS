namespace Shared.Infrastructure.Api;

/// <summary>
/// Metadata describing a response code: default message and HTTP status.
/// </summary>
public class ApiResponseCodeMetadata
{
    public ApiResponseCode Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public int HttpStatus { get; set; }
}

