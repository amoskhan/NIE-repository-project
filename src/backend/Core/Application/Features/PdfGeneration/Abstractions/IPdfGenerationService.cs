using Application.Contracts.Report;

namespace Application.Features.Reports;

public interface IPdfGenerationService
{
    /// <summary>
    /// Generate a PDF from an HTML string.
    /// </summary>
    Task<byte[]> GeneratePdfFromHtmlAsync(
        string html,
        PdfOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the available report types for the system.
    /// </summary>
    IReadOnlyList<ReportTypeDefinition> GetAvailableReportTypes();

    /// <summary>
    /// Generate HTML content for a specific report type with filters.
    /// </summary>
    Task<string> GenerateReportHtmlAsync(
        string reportType,
        ReportRequestDto filters,
        CancellationToken cancellationToken = default);
}
