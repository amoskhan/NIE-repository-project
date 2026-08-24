using Api.Authorization;
using Application.Contracts.Report;
using Application.Features.Reports;
using Application.Security;
using BuildingBlocks.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportController : BaseController
{
    private readonly IPdfGenerationService _pdfService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(
        IPdfGenerationService pdfService,
        ILogger<ReportController> logger)
    {
        _pdfService = pdfService;
        _logger = logger;
    }

    /// <summary>
    /// Get all available report types grouped by category.
    /// </summary>
    [HttpGet("types")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ReportRead)]
    [ProducesResponseType(typeof(IEnumerable<ReportTypeDefinition>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<ReportTypeDefinition>> GetReportTypes()
    {
        var types = _pdfService.GetAvailableReportTypes();
        return Ok(types);
    }

    /// <summary>
    /// Generate HTML preview for a report.
    /// </summary>
    [HttpPost("preview")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ReportRead)]
    [Produces("text/html")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewReport(
        [FromBody] ReportRequestDto request,
        CancellationToken cancellationToken)
    {
        var definition = FindReportType(request.ReportType);
        if (definition is null)
        {
            return ReportNotFound(request.ReportType);
        }

        var effectiveRequest = ReportPageSetupResolver.Apply(definition, request);
        var html = await _pdfService.GenerateReportHtmlAsync(request.ReportType, effectiveRequest, cancellationToken);
        return Content(html, "text/html");
    }

    /// <summary>
    /// Generate a PDF report for inline preview.
    /// </summary>
    [HttpPost("pdf")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ReportRead)]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> PdfReport(
        [FromBody] ReportRequestDto request,
        CancellationToken cancellationToken)
    {
        var definition = FindReportType(request.ReportType);
        if (definition is null)
        {
            return ReportNotFound(request.ReportType);
        }

        try
        {
            var effectiveRequest = ReportPageSetupResolver.Apply(definition, request);
            var html = await _pdfService.GenerateReportHtmlAsync(request.ReportType, effectiveRequest, cancellationToken);
            var pdf = await _pdfService.GeneratePdfFromHtmlAsync(html, PdfOptionsFromRequest(effectiveRequest), cancellationToken);
            return File(pdf, "application/pdf");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate PDF report {ReportType}", request.ReportType);
            return Problem(
                title: "Report PDF generation failed",
                detail: "The report PDF could not be generated. Verify that Playwright is installed on the host.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    /// <summary>
    /// Generate and download a PDF report.
    /// </summary>
    [HttpPost("download")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ReportRead)]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> DownloadReport(
        [FromBody] ReportRequestDto request,
        CancellationToken cancellationToken)
    {
        var definition = FindReportType(request.ReportType);
        if (definition is null)
        {
            return ReportNotFound(request.ReportType);
        }

        try
        {
            var effectiveRequest = ReportPageSetupResolver.Apply(definition, request);
            var html = await _pdfService.GenerateReportHtmlAsync(request.ReportType, effectiveRequest, cancellationToken);
            var pdf = await _pdfService.GeneratePdfFromHtmlAsync(html, PdfOptionsFromRequest(effectiveRequest), cancellationToken);
            return File(pdf, "application/pdf", $"{request.ReportType}_{DateTimeHelper.Now:yyyyMMdd}.pdf");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download PDF report {ReportType}", request.ReportType);
            return Problem(
                title: "Report PDF generation failed",
                detail: "The report PDF could not be generated. Verify that Playwright is installed on the host.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private ReportTypeDefinition? FindReportType(string reportType) =>
        _pdfService.GetAvailableReportTypes()
            .FirstOrDefault(report => string.Equals(report.Id, reportType, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Maps the user-chosen paper format and orientation onto Playwright's
    /// <see cref="PdfOptions"/>. Falls back to A4 portrait if unrecognised so
    /// a malformed request never breaks the download.
    /// </summary>
    private static PdfOptions PdfOptionsFromRequest(ReportRequestDto request)
    {
        var format = ReportPageSetupResolver.NormalizeFormat(request.Format) ?? "A4";
        var landscape = string.Equals(
            ReportPageSetupResolver.NormalizeOrientation(request.Orientation),
            "Landscape",
            StringComparison.Ordinal);

        return new PdfOptions
        {
            Format = format,
            Landscape = landscape
        };
    }

    private ObjectResult ReportNotFound(string reportType) =>
        Problem(
            title: "Report not found",
            detail: $"The report type '{reportType}' is not available.",
            statusCode: StatusCodes.Status404NotFound);
}
