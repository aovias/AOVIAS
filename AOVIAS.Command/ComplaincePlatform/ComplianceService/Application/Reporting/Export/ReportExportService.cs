using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using ComplianceService.Application.Reporting.Contracts;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ComplianceService.Application.Reporting.Export;

public sealed class ReportExportService : IReportExportService
{
    static ReportExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public (byte[] Content, string ContentType, string FileName) Export(ReportExportModel model, ReportExportFormat format)
    {
        var safeTitle = SanitizeFileName(model.Title);
        return format switch
        {
            ReportExportFormat.Csv => (ExportCsv(model), "text/csv", $"{safeTitle}.csv"),
            ReportExportFormat.Excel => (ExportExcel(model), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{safeTitle}.xlsx"),
            ReportExportFormat.Pdf => (ExportPdf(model), "application/pdf", $"{safeTitle}.pdf"),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    private static byte[] ExportCsv(ReportExportModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"{model.Title}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Organization,{Escape(model.OrganizationName)}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Framework,{Escape(model.FrameworkName)}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Generated,{model.GeneratedAt:u}");

        foreach (var kpi in model.Kpis)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"{Escape(kpi.Label)},{Escape(kpi.Value)}");
        }

        foreach (var table in model.Tables)
        {
            sb.AppendLine();
            sb.AppendLine(CultureInfo.InvariantCulture, $"{table.Title}");
            sb.AppendLine(string.Join(",", table.Headers.Select(Escape)));
            foreach (var row in table.Rows)
            {
                sb.AppendLine(string.Join(",", row.Select(Escape)));
            }
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] ExportExcel(ReportExportModel model)
    {
        using var workbook = new XLWorkbook();
        var summary = workbook.Worksheets.Add("Summary");
        summary.Cell(1, 1).Value = model.Title;
        summary.Cell(2, 1).Value = "Organization";
        summary.Cell(2, 2).Value = model.OrganizationName;
        summary.Cell(3, 1).Value = "Framework";
        summary.Cell(3, 2).Value = model.FrameworkName;
        summary.Cell(4, 1).Value = "Generated";
        summary.Cell(4, 2).Value = model.GeneratedAt;

        var row = 6;
        foreach (var kpi in model.Kpis)
        {
            summary.Cell(row, 1).Value = kpi.Label;
            summary.Cell(row, 2).Value = kpi.Value;
            row++;
        }

        var sheetIndex = 1;
        foreach (var table in model.Tables)
        {
            var ws = workbook.Worksheets.Add(SanitizeSheetName(table.Title, sheetIndex++));
            for (var c = 0; c < table.Headers.Count; c++)
            {
                ws.Cell(1, c + 1).Value = table.Headers[c];
            }

            for (var r = 0; r < table.Rows.Count; r++)
            {
                for (var c = 0; c < table.Rows[r].Count; c++)
                {
                    ws.Cell(r + 2, c + 1).Value = table.Rows[r][c];
                }
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] ExportPdf(ReportExportModel model)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Text(model.Title).Bold().FontSize(18);
                page.Content().Column(col =>
                {
                    col.Item().Text($"Organization: {model.OrganizationName}");
                    col.Item().Text($"Framework: {model.FrameworkName}");
                    col.Item().Text($"Generated: {model.GeneratedAt:u}");
                    col.Item().PaddingTop(10);

                    foreach (var kpi in model.Kpis)
                    {
                        col.Item().Text($"{kpi.Label}: {kpi.Value}");
                    }

                    foreach (var table in model.Tables)
                    {
                        col.Item().PaddingTop(15).Text(table.Title).Bold();
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                for (var i = 0; i < table.Headers.Count; i++)
                                {
                                    c.RelativeColumn();
                                }
                            });

                            t.Header(h =>
                            {
                                foreach (var header in table.Headers)
                                {
                                    h.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(header).Bold();
                                }
                            });

                            foreach (var dataRow in table.Rows)
                            {
                                foreach (var cell in dataRow)
                                {
                                    t.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(cell);
                                }
                            }
                        });
                    }
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string Escape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return string.IsNullOrWhiteSpace(name) ? "report" : name.Replace(" ", "-", StringComparison.Ordinal);
    }

    private static string SanitizeSheetName(string name, int index)
    {
        var invalid = new[] { '\\', '/', '*', '?', ':', '[', ']' };
        foreach (var c in invalid)
        {
            name = name.Replace(c, '_');
        }

        name = name.Length > 28 ? name[..28] : name;
        return string.IsNullOrWhiteSpace(name) ? $"Data{index}" : name;
    }
}
