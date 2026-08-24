using Application.Contracts.Report;
using Application.Features.Reports;

namespace Application.Tests;

public sealed class ReportPageSetupResolverTests
{
    private static ReportRequestDto Request(string? format = null, string? orientation = null) =>
        new()
        {
            ReportType = "po-summary",
            Status = "Approved",
            Category = "Procurement",
            Format = format,
            Orientation = orientation,
        };

    [Theory]
    [InlineData("a4", "A4")]
    [InlineData("A4", "A4")]
    [InlineData("  a3  ", "A3")]
    [InlineData("letter", "Letter")]
    [InlineData("LEGAL", "Legal")]
    [InlineData("a5", "A5")]
    public void Normalises_a_supported_paper_format_to_its_canonical_spelling(string input, string expected)
    {
        Assert.Equal(expected, ReportPageSetupResolver.NormalizeFormat(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("B5")]
    [InlineData("A4;DROP")]
    [InlineData("tabloid")]
    public void Refuses_to_normalise_a_paper_format_outside_the_whitelist(string? input)
    {
        Assert.Null(ReportPageSetupResolver.NormalizeFormat(input));
    }

    [Theory]
    [InlineData("portrait", "Portrait")]
    [InlineData("  LANDSCAPE ", "Landscape")]
    public void Normalises_a_supported_orientation_to_its_canonical_spelling(string input, string expected)
    {
        Assert.Equal(expected, ReportPageSetupResolver.NormalizeOrientation(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("sideways")]
    public void Refuses_to_normalise_an_orientation_outside_the_whitelist(string? input)
    {
        Assert.Null(ReportPageSetupResolver.NormalizeOrientation(input));
    }

    [Theory]
    [InlineData("Legal")]
    [InlineData("A3")]
    [InlineData(null)]
    public void Ignores_the_requested_format_when_the_report_locks_it(string? requested)
    {
        var pageSetup = new ReportPageSetupDefinition { DefaultFormat = "A5", AllowFormatChange = false };

        Assert.Equal("A5", ReportPageSetupResolver.ResolveFormat(requested, pageSetup));
    }

    [Fact]
    public void Falls_back_to_a4_when_the_locked_default_format_is_not_recognised()
    {
        var pageSetup = new ReportPageSetupDefinition { DefaultFormat = "Tabloid", AllowFormatChange = false };

        Assert.Equal("A4", ReportPageSetupResolver.ResolveFormat("Legal", pageSetup));
    }

    [Fact]
    public void Honours_a_requested_format_that_the_report_allows()
    {
        var pageSetup = new ReportPageSetupDefinition { Formats = ["A4", "Legal"] };

        Assert.Equal("Legal", ReportPageSetupResolver.ResolveFormat("legal", pageSetup));
    }

    [Fact]
    public void Replaces_a_requested_format_that_the_report_does_not_allow()
    {
        var pageSetup = new ReportPageSetupDefinition { DefaultFormat = "A4", Formats = ["A4", "Legal"] };

        Assert.Equal("A4", ReportPageSetupResolver.ResolveFormat("A3", pageSetup));
    }

    [Fact]
    public void Replaces_a_requested_format_that_is_not_a_known_paper_size()
    {
        var pageSetup = new ReportPageSetupDefinition { DefaultFormat = "A3" };

        Assert.Equal("A3", ReportPageSetupResolver.ResolveFormat("B5", pageSetup));
    }

    [Fact]
    public void Uses_the_shared_defaults_when_a_report_lists_no_allowed_formats()
    {
        var pageSetup = new ReportPageSetupDefinition { Formats = [] };

        Assert.Equal("Legal", ReportPageSetupResolver.ResolveFormat("Legal", pageSetup));
    }

    [Fact]
    public void Discards_unknown_entries_from_a_reports_allowed_format_list()
    {
        var pageSetup = new ReportPageSetupDefinition { DefaultFormat = "A4", Formats = ["Tabloid", "B5"] };

        Assert.Equal("Legal", ReportPageSetupResolver.ResolveFormat("Legal", pageSetup));
    }

    [Fact]
    public void Always_permits_the_reports_default_format_even_when_it_is_missing_from_the_list()
    {
        var pageSetup = new ReportPageSetupDefinition { DefaultFormat = "A5", Formats = ["A3"] };

        Assert.Equal("A5", ReportPageSetupResolver.ResolveFormat("A5", pageSetup));
        Assert.Equal("A3", ReportPageSetupResolver.ResolveFormat("A3", pageSetup));
        Assert.Equal("A5", ReportPageSetupResolver.ResolveFormat("Legal", pageSetup));
    }

    [Fact]
    public void Ignores_the_requested_orientation_when_the_report_locks_it()
    {
        var pageSetup = new ReportPageSetupDefinition
        {
            DefaultOrientation = "Landscape",
            AllowOrientationChange = false,
        };

        Assert.Equal("Landscape", ReportPageSetupResolver.ResolveOrientation("Portrait", pageSetup));
    }

    [Fact]
    public void Falls_back_to_portrait_when_the_locked_default_orientation_is_not_recognised()
    {
        var pageSetup = new ReportPageSetupDefinition
        {
            DefaultOrientation = "diagonal",
            AllowOrientationChange = false,
        };

        Assert.Equal("Portrait", ReportPageSetupResolver.ResolveOrientation("Landscape", pageSetup));
    }

    [Fact]
    public void Honours_a_requested_orientation_that_the_report_allows()
    {
        var pageSetup = new ReportPageSetupDefinition();

        Assert.Equal("Landscape", ReportPageSetupResolver.ResolveOrientation("landscape", pageSetup));
    }

    [Fact]
    public void Replaces_an_orientation_that_the_report_does_not_allow()
    {
        var pageSetup = new ReportPageSetupDefinition
        {
            DefaultOrientation = "Portrait",
            Orientations = ["Portrait"],
        };

        Assert.Equal("Portrait", ReportPageSetupResolver.ResolveOrientation("Landscape", pageSetup));
    }

    [Fact]
    public void Uses_the_shared_defaults_when_a_report_lists_no_allowed_orientations()
    {
        var pageSetup = new ReportPageSetupDefinition { Orientations = [] };

        Assert.Equal("Landscape", ReportPageSetupResolver.ResolveOrientation("Landscape", pageSetup));
    }

    [Fact]
    public void Apply_writes_the_resolved_page_setup_onto_the_request()
    {
        var definition = new ReportTypeDefinition
        {
            Id = "po-summary",
            Name = "Purchase order summary",
            Description = "Summary",
            Category = "Procurement",
            PageSetup = new ReportPageSetupDefinition
            {
                DefaultFormat = "A3",
                DefaultOrientation = "Landscape",
                AllowFormatChange = false,
                AllowOrientationChange = false,
            },
        };

        var resolved = ReportPageSetupResolver.Apply(definition, Request("Legal", "Portrait"));

        Assert.Equal("A3", resolved.Format);
        Assert.Equal("Landscape", resolved.Orientation);
    }

    [Fact]
    public void Apply_preserves_every_filter_on_the_request()
    {
        var definition = new ReportTypeDefinition
        {
            Id = "po-summary",
            Name = "Purchase order summary",
            Description = "Summary",
            Category = "Procurement",
        };
        var request = Request("a4", "landscape") with
        {
            DateFrom = new DateOnly(2026, 1, 1),
            DateTo = new DateOnly(2026, 3, 31),
            VendorId = Guid.Parse("0199a1a2-0000-7000-8000-000000000009"),
            UserId = "staff-1",
        };

        var resolved = ReportPageSetupResolver.Apply(definition, request);

        Assert.Equal("A4", resolved.Format);
        Assert.Equal("Landscape", resolved.Orientation);
        Assert.Equal("po-summary", resolved.ReportType);
        Assert.Equal("Approved", resolved.Status);
        Assert.Equal("Procurement", resolved.Category);
        Assert.Equal(new DateOnly(2026, 1, 1), resolved.DateFrom);
        Assert.Equal(new DateOnly(2026, 3, 31), resolved.DateTo);
        Assert.Equal("staff-1", resolved.UserId);
        Assert.NotNull(resolved.VendorId);
    }

    [Fact]
    public void Apply_supplies_defaults_for_a_request_that_asks_for_nothing()
    {
        var definition = new ReportTypeDefinition
        {
            Id = "audit-trail",
            Name = "Audit trail",
            Description = "Audit",
            Category = "Audit",
        };

        var resolved = ReportPageSetupResolver.Apply(definition, Request());

        Assert.Equal("A4", resolved.Format);
        Assert.Equal("Portrait", resolved.Orientation);
    }
}
