using Api.Controllers;
using Api.Tests.TestSupport;
using Application.Contracts.Report;
using Application.Features.Reports;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Api.Tests.Controllers;

public sealed class ReportControllerTests
{
    private const string KnownReport = "po-summary";

    private readonly IPdfGenerationService _pdfService = Substitute.For<IPdfGenerationService>();

    public ReportControllerTests()
    {
        _pdfService.GetAvailableReportTypes().Returns([Definition()]);
        _pdfService
            .GenerateReportHtmlAsync(Arg.Any<string>(), Arg.Any<ReportRequestDto>(), Arg.Any<CancellationToken>())
            .Returns("<html>report</html>");
        _pdfService
            .GeneratePdfFromHtmlAsync(Arg.Any<string>(), Arg.Any<PdfOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new byte[] { 1, 2, 3 });
    }

    [Fact]
    public async Task Previewing_an_unknown_report_is_a_404_problem()
    {
        var result = await CreateController().PreviewReport(
            new ReportRequestDto { ReportType = "does-not-exist" },
            TestContext.Current.CancellationToken);

        var problem = AssertProblem(result, StatusCodes.Status404NotFound);
        Assert.Equal("Report not found", problem.Title);
        Assert.Contains("does-not-exist", problem.Detail ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Rendering_an_unknown_report_never_reaches_the_generator()
    {
        await CreateController().PdfReport(
            new ReportRequestDto { ReportType = "does-not-exist" },
            TestContext.Current.CancellationToken);

        await _pdfService.DidNotReceive().GenerateReportHtmlAsync(
            Arg.Any<string>(),
            Arg.Any<ReportRequestDto>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Downloading_an_unknown_report_is_a_404_problem()
    {
        var result = await CreateController().DownloadReport(
            new ReportRequestDto { ReportType = "does-not-exist" },
            TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Report_type_lookup_ignores_case()
    {
        var result = await CreateController().PreviewReport(
            new ReportRequestDto { ReportType = "PO-SUMMARY" },
            TestContext.Current.CancellationToken);

        Assert.IsType<ContentResult>(result);
    }

    [Fact]
    public async Task A_preview_is_returned_as_html()
    {
        var result = await CreateController().PreviewReport(
            new ReportRequestDto { ReportType = KnownReport },
            TestContext.Current.CancellationToken);

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal("<html>report</html>", content.Content);
        Assert.Equal("text/html", content.ContentType);
    }

    [Fact]
    public async Task A_pdf_generation_failure_degrades_to_service_unavailable()
    {
        _pdfService
            .GeneratePdfFromHtmlAsync(Arg.Any<string>(), Arg.Any<PdfOptions?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("playwright is not installed"));

        var result = await CreateController().PdfReport(
            new ReportRequestDto { ReportType = KnownReport },
            TestContext.Current.CancellationToken);

        var problem = AssertProblem(result, StatusCodes.Status503ServiceUnavailable);
        Assert.Equal("Report PDF generation failed", problem.Title);
        Assert.DoesNotContain("playwright is not installed", problem.Detail ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_download_generation_failure_degrades_to_service_unavailable()
    {
        _pdfService
            .GenerateReportHtmlAsync(Arg.Any<string>(), Arg.Any<ReportRequestDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("renderer timed out"));

        var result = await CreateController().DownloadReport(
            new ReportRequestDto { ReportType = KnownReport },
            TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task A_cancelled_request_is_not_disguised_as_a_generation_failure()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _pdfService
            .GenerateReportHtmlAsync(Arg.Any<string>(), Arg.Any<ReportRequestDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => CreateController().PdfReport(
                new ReportRequestDto { ReportType = KnownReport },
                cancellation.Token));
    }

    [Fact]
    public async Task A_cancellation_that_the_caller_did_not_request_is_still_reported_as_a_failure()
    {
        _pdfService
            .GenerateReportHtmlAsync(Arg.Any<string>(), Arg.Any<ReportRequestDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var result = await CreateController().PdfReport(
            new ReportRequestDto { ReportType = KnownReport },
            TestContext.Current.CancellationToken);

        AssertProblem(result, StatusCodes.Status503ServiceUnavailable);
    }

    [Theory]
    [InlineData("letter", "Portrait", "Letter", false)]
    [InlineData("a3", "landscape", "A3", true)]
    [InlineData(null, null, "A4", false)]
    [InlineData("tabloid", "sideways", "A4", false)]
    public async Task The_requested_page_setup_is_normalised_onto_the_pdf_options(
        string? format,
        string? orientation,
        string expectedFormat,
        bool expectedLandscape)
    {
        PdfOptions? captured = null;
        _pdfService
            .GeneratePdfFromHtmlAsync(
                Arg.Any<string>(),
                Arg.Do<PdfOptions?>(options => captured = options),
                Arg.Any<CancellationToken>())
            .Returns(new byte[] { 1 });

        await CreateController().PdfReport(
            new ReportRequestDto { ReportType = KnownReport, Format = format, Orientation = orientation },
            TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.Equal(expectedFormat, captured.Format);
        Assert.Equal(expectedLandscape, captured.Landscape);
    }

    [Fact]
    public async Task A_locked_page_setup_overrides_whatever_the_caller_asked_for()
    {
        _pdfService.GetAvailableReportTypes().Returns([
            Definition(new ReportPageSetupDefinition
            {
                DefaultFormat = "A3",
                DefaultOrientation = "Landscape",
                AllowFormatChange = false,
                AllowOrientationChange = false,
            })]);
        PdfOptions? captured = null;
        _pdfService
            .GeneratePdfFromHtmlAsync(
                Arg.Any<string>(),
                Arg.Do<PdfOptions?>(options => captured = options),
                Arg.Any<CancellationToken>())
            .Returns(new byte[] { 1 });

        await CreateController().PdfReport(
            new ReportRequestDto { ReportType = KnownReport, Format = "A5", Orientation = "Portrait" },
            TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.Equal("A3", captured.Format);
        Assert.True(captured.Landscape);
    }

    [Fact]
    public async Task A_download_is_returned_as_a_dated_pdf_attachment()
    {
        var result = await CreateController().DownloadReport(
            new ReportRequestDto { ReportType = KnownReport },
            TestContext.Current.CancellationToken);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.StartsWith($"{KnownReport}_", file.FileDownloadName, StringComparison.Ordinal);
        Assert.EndsWith(".pdf", file.FileDownloadName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_inline_pdf_has_no_download_name()
    {
        var result = await CreateController().PdfReport(
            new ReportRequestDto { ReportType = KnownReport },
            TestContext.Current.CancellationToken);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.True(string.IsNullOrEmpty(file.FileDownloadName));
    }

    private static ReportTypeDefinition Definition(ReportPageSetupDefinition? pageSetup = null) =>
        new()
        {
            Id = KnownReport,
            Name = "Purchase order summary",
            Description = "Spend by vendor",
            Category = "Procurement",
            PageSetup = pageSetup ?? new ReportPageSetupDefinition(),
        };

    private static ProblemDetails AssertProblem(IActionResult result, int expectedStatusCode)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(expectedStatusCode, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(expectedStatusCode, problem.Status);
        return problem;
    }

    private ReportController CreateController() =>
        new(_pdfService, NullLogger<ReportController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            ProblemDetailsFactory = new TestProblemDetailsFactory(),
        };
}
