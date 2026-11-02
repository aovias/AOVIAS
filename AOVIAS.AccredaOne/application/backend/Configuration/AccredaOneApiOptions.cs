namespace AccredaOne.Api.Configuration;

public sealed class AccredaOneApiOptions
{
    public const string SectionName = "AccredaOne";

    public string ServiceName { get; set; } = "AccredaOne.Api";

    public string ApiVersion { get; set; } = "v1";

    public string Phase { get; set; } = "DEV-03 engineering foundation";
}
