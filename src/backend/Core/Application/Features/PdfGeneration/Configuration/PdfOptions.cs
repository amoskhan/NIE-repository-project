using Application.Contracts.Report;

namespace Application.Features.Reports;

public class PdfOptions
{
    public string Format { get; set; } = "A4";
    public bool Landscape { get; set; } = false;
    public string? HeaderTemplate { get; set; }
    public string? FooterTemplate { get; set; }
    public bool PrintBackground { get; set; } = true;
    public string? Margin { get; set; } = "10mm";
}
