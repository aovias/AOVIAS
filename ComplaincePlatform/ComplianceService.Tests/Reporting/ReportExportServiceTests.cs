using ComplianceService.Application.Reporting.Export;
using Xunit;

namespace ComplianceService.Tests.Reporting;

public class ReportExportServiceTests
{
    private readonly ReportExportService _sut = new();

    [Theory]
    [InlineData(ReportExportFormat.Csv)]
    [InlineData(ReportExportFormat.Excel)]
    [InlineData(ReportExportFormat.Pdf)]
    public void Export_ProducesNonEmptyContent(ReportExportFormat format)
    {
        var model = new ReportExportModel
        {
            Title = "Compliance Report",
            OrganizationName = "Org-1",
            FrameworkName = "NABH",
            GeneratedAt = DateTime.UtcNow,
            Kpis = [new ReportKpiItem { Label = "Score", Value = "82%" }],
            Tables =
            [
                new ReportTableSection
                {
                    Title = "Chapters",
                    Headers = ["Chapter", "Score"],
                    Rows = [new List<string> { "ACC", "85" }],
                },
            ],
        };

        var (content, contentType, fileName) = _sut.Export(model, format);

        Assert.NotEmpty(content);
        Assert.False(string.IsNullOrWhiteSpace(contentType));
        Assert.Contains("Compliance-Report", fileName, StringComparison.OrdinalIgnoreCase);

        if (format == ReportExportFormat.Csv)
        {
            var text = System.Text.Encoding.UTF8.GetString(content);
            Assert.Contains("Compliance Report", text);
            Assert.Contains("NABH", text);
            Assert.Contains("Score", text);
        }
    }
}
