using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Gateway.Application.Http;

/// <summary>
/// Copies the incoming request body into memory before forwarding via <see cref="HttpClient"/>.
/// Forwarding <see cref="HttpRequest.Body"/> directly can fail or send an empty body if the stream
/// was partially read or is not rewindable in some host/proxy setups.
/// </summary>
internal static class MultipartProxyRequestBody
{
    /// <summary>
    /// Buffers the full request body for proxying and returns the outbound content plus byte length for diagnostics.
    /// </summary>
    public static async Task<(StreamContent Content, long BodyByteLength)> CreateStreamContentAsync(
        HttpRequest request,
        CancellationToken cancellationToken,
        ILogger? logger = null)
    {
        request.EnableBuffering();
        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        }

        var initialCapacity = request.ContentLength is > 0 and <= int.MaxValue
            ? (int)request.ContentLength.Value
            : 64 * 1024;

        var buffer = new MemoryStream(initialCapacity);
        await request.Body.CopyToAsync(buffer, cancellationToken);
        var bodyByteLength = buffer.Length;
        buffer.Position = 0;

        if (bodyByteLength == 0 && request.ContentLength is > 0)
        {
            logger?.LogWarning(
                "Multipart proxy buffered 0 bytes but Content-Length was {ContentLength}. " +
                "Ensure MultipartRequestBufferingMiddleware runs first so the body is not drained earlier. ContentType={ContentType}",
                request.ContentLength,
                request.ContentType);
        }

        var content = new StreamContent(buffer);
        var ct = request.ContentType;
        if (!string.IsNullOrEmpty(ct))
        {
            content.Headers.ContentType = NormalizeForwardedMultipartContentType(ct);
        }

        return (content, bodyByteLength);
    }

    /// <summary>
    /// Browsers sometimes send <c>multipart/form-data; boundary=...; charset=UTF-8</c>.
    /// Charset on multipart is not meaningful for binary parts and can cause ASP.NET Core's form reader
    /// to mis-bind <see cref="Microsoft.AspNetCore.Http.IFormFile"/> (text fields still parse, file length 0).
    /// </summary>
    private static MediaTypeHeaderValue NormalizeForwardedMultipartContentType(string contentTypeHeader)
    {
        if (!MediaTypeHeaderValue.TryParse(contentTypeHeader, out var mediaType))
        {
            return MediaTypeHeaderValue.Parse(contentTypeHeader);
        }

        if (!string.Equals(mediaType.MediaType, "multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            return mediaType;
        }

        var toDrop = new List<NameValueHeaderValue>();
        foreach (var p in mediaType.Parameters)
        {
            if (string.Equals(p.Name, "charset", StringComparison.OrdinalIgnoreCase))
            {
                toDrop.Add(p);
            }
        }

        foreach (var p in toDrop)
        {
            mediaType.Parameters.Remove(p);
        }

        return mediaType;
    }
}
