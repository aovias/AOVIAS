namespace OrganizationService.Application.DTOs;

public sealed class OrganizationBrandingDto
{
    public Guid OrganizationId { get; set; }
    public bool CanEdit { get; set; }
    public BrandingAssetUrlsDto Assets { get; set; } = new();
    public OrganizationBrandingConfigDto Config { get; set; } = new();
    public string? RowVersion { get; set; }
}

public sealed class BrandingAssetUrlsDto
{
    public string? PrimaryLogoUrl { get; set; }
    public string? SecondaryLogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? ReportLogoUrl { get; set; }
    public string? EmailHeaderLogoUrl { get; set; }
    public string? EmailFooterLogoUrl { get; set; }
    public string? LoginBackgroundUrl { get; set; }
}

public sealed class OrganizationBrandingConfigDto
{
    public BrandingColorsDto Colors { get; set; } = new();
    public BrandingReportDto Report { get; set; } = new();
    public BrandingEmailDto Email { get; set; } = new();
    public BrandingLoginDto Login { get; set; } = new();
}

public sealed class BrandingColorsDto
{
    public string Primary { get; set; } = "#4F46E5";
    public string Secondary { get; set; } = "#1E293B";
    public string Accent { get; set; } = "#0EA5E9";
}

public sealed class BrandingReportDto
{
    public string Header { get; set; } = string.Empty;
    public string Footer { get; set; } = string.Empty;
    public string Motto { get; set; } = string.Empty;
}

public sealed class BrandingEmailDto
{
    public string Header { get; set; } = string.Empty;
    public string Footer { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}

public sealed class BrandingLoginDto
{
    public string WelcomeMessage { get; set; } = string.Empty;
    public string Tagline { get; set; } = string.Empty;
}

public sealed class UpdateOrganizationBrandingRequest
{
    public OrganizationBrandingConfigDto Config { get; set; } = new();
    public string? RowVersion { get; set; }
}
