namespace Aovias.Platform.Core;

/// <summary>
/// Base exception for platform-level failures.
/// </summary>
public class PlatformException : Exception
{
    public PlatformException(string message)
        : base(message)
    {
    }

    public PlatformException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public PlatformException(Error error)
        : base(error.Message)
    {
        Error = error;
    }

    public Error? Error { get; }
}
