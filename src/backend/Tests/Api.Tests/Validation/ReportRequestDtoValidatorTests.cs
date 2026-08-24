using Api.Validation;
using Application.Contracts.Report;

namespace Api.Tests.Validation;

public sealed class ReportRequestDtoValidatorTests
{
    private readonly ReportRequestDtoValidator _validator = new();

    [Fact]
    public void A_minimal_request_naming_a_report_is_accepted()
    {
        Assert.True(_validator.Validate(new ReportRequestDto { ReportType = "po-summary" }).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_request_must_name_a_report(string reportType)
    {
        Assert.False(_validator.Validate(new ReportRequestDto { ReportType = reportType }).IsValid);
    }

    [Theory]
    [InlineData("A4")]
    [InlineData("a4")]
    [InlineData("LETTER")]
    [InlineData("Legal")]
    [InlineData(null)]
    public void Supported_paper_formats_are_accepted_in_any_case(string? format)
    {
        var request = new ReportRequestDto { ReportType = "po-summary", Format = format };

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData("A6")]
    [InlineData("Tabloid")]
    [InlineData("")]
    public void An_unsupported_paper_format_is_rejected(string format)
    {
        var request = new ReportRequestDto { ReportType = "po-summary", Format = format };

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData("Portrait")]
    [InlineData("landscape")]
    [InlineData(null)]
    public void Supported_orientations_are_accepted_in_any_case(string? orientation)
    {
        var request = new ReportRequestDto { ReportType = "po-summary", Orientation = orientation };

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void An_unsupported_orientation_is_rejected()
    {
        var request = new ReportRequestDto { ReportType = "po-summary", Orientation = "sideways" };

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void A_reporting_period_may_not_run_backwards()
    {
        var request = new ReportRequestDto
        {
            ReportType = "po-summary",
            DateFrom = new DateOnly(2026, 3, 1),
            DateTo = new DateOnly(2026, 2, 1),
        };

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void A_single_day_reporting_period_is_accepted()
    {
        var request = new ReportRequestDto
        {
            ReportType = "po-summary",
            DateFrom = new DateOnly(2026, 3, 1),
            DateTo = new DateOnly(2026, 3, 1),
        };

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void An_open_ended_reporting_period_is_accepted()
    {
        var openStart = new ReportRequestDto { ReportType = "po-summary", DateTo = new DateOnly(2026, 3, 1) };
        var openEnd = new ReportRequestDto { ReportType = "po-summary", DateFrom = new DateOnly(2026, 3, 1) };

        Assert.True(_validator.Validate(openStart).IsValid);
        Assert.True(_validator.Validate(openEnd).IsValid);
    }

    [Fact]
    public void A_vendor_filter_must_be_a_real_identifier()
    {
        var empty = new ReportRequestDto { ReportType = "po-summary", VendorId = Guid.Empty };
        var real = new ReportRequestDto { ReportType = "po-summary", VendorId = Guid.CreateVersion7() };
        var absent = new ReportRequestDto { ReportType = "po-summary", VendorId = null };

        Assert.False(_validator.Validate(empty).IsValid);
        Assert.True(_validator.Validate(real).IsValid);
        Assert.True(_validator.Validate(absent).IsValid);
    }

    [Fact]
    public void Free_text_filters_are_length_bounded()
    {
        var request = new ReportRequestDto
        {
            ReportType = "po-summary",
            Status = new string('s', 51),
            Category = new string('c', 101),
            UserId = new string('u', 101),
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Count);
    }
}
