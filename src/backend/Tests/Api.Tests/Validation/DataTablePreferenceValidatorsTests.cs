using Api.Validation;
using Application.Contracts;

namespace Api.Tests.Validation;

/// <summary>
/// Saved table preferences are replayed into queries on every page load, so the
/// whitelists for page size, density and appearance must reject anything unexpected.
/// </summary>
public sealed class DataTablePreferenceValidatorsTests
{
    private readonly UpsertUserDataTablePreferenceDtoValidator _validator = new();

    [Fact]
    public void The_default_settings_are_accepted()
    {
        Assert.True(_validator.Validate(new UpsertUserDataTablePreferenceDto()).IsValid);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(100)]
    public void Only_the_whitelisted_page_sizes_are_accepted(int pageSize)
    {
        Assert.True(_validator.Validate(Request(settings => settings.PageSize = pageSize)).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(25)]
    [InlineData(1000)]
    [InlineData(int.MaxValue)]
    public void Page_sizes_outside_the_whitelist_are_rejected(int pageSize)
    {
        Assert.False(_validator.Validate(Request(settings => settings.PageSize = pageSize)).IsValid);
    }

    [Theory]
    [InlineData("compact")]
    [InlineData("comfortable")]
    [InlineData("spacious")]
    public void Only_the_whitelisted_densities_are_accepted(string density)
    {
        Assert.True(_validator.Validate(Request(settings => settings.Density = density)).IsValid);
    }

    [Theory]
    [InlineData("Compact")]
    [InlineData("cosy")]
    [InlineData("")]
    public void Density_matching_is_exact_and_case_sensitive(string density)
    {
        Assert.False(_validator.Validate(Request(settings => settings.Density = density)).IsValid);
    }

    [Theory]
    [InlineData("elevated")]
    [InlineData("minimal")]
    [InlineData("striped")]
    public void Only_the_whitelisted_appearances_are_accepted(string appearance)
    {
        Assert.True(_validator.Validate(Request(settings => settings.Appearance = appearance)).IsValid);
    }

    [Fact]
    public void An_unknown_appearance_is_rejected()
    {
        Assert.False(_validator.Validate(Request(settings => settings.Appearance = "neon")).IsValid);
    }

    [Fact]
    public void A_column_may_not_appear_twice_in_the_column_order()
    {
        var result = _validator.Validate(Request(settings =>
            settings.ColumnOrder = ["status", "STATUS"]));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void A_column_may_not_appear_twice_in_the_hidden_columns()
    {
        var result = _validator.Validate(Request(settings =>
            settings.HiddenColumns = ["notes", "Notes"]));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void At_most_eighty_columns_can_be_ordered_or_hidden()
    {
        Assert.True(_validator.Validate(Request(settings => settings.ColumnOrder = Columns(80))).IsValid);
        Assert.False(_validator.Validate(Request(settings => settings.ColumnOrder = Columns(81))).IsValid);
        Assert.False(_validator.Validate(Request(settings => settings.HiddenColumns = Columns(81))).IsValid);
    }

    [Fact]
    public void A_saved_filter_may_not_be_repeated_for_the_same_column()
    {
        var result = _validator.Validate(Request(settings => settings.Filters =
        [
            new DataTablePreferenceFilterDto { Key = "status", Values = ["Draft"] },
            new DataTablePreferenceFilterDto { Key = "STATUS", Values = ["Approved"] },
        ]));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void A_saved_filter_may_not_select_more_than_a_hundred_values()
    {
        var result = _validator.Validate(Request(settings => settings.Filters =
        [
            new DataTablePreferenceFilterDto
            {
                Key = "status",
                Values = Enumerable.Range(0, 101).Select(index => $"value{index}").ToList(),
            },
        ]));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10_001)]
    public void The_definition_version_must_stay_within_its_supported_range(int definitionVersion)
    {
        var request = new UpsertUserDataTablePreferenceDto { DefinitionVersion = definitionVersion };

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void A_supplied_revision_must_be_positive(int revision)
    {
        var request = new UpsertUserDataTablePreferenceDto { Revision = revision };

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void An_omitted_revision_is_allowed_for_a_first_save()
    {
        Assert.True(_validator.Validate(new UpsertUserDataTablePreferenceDto { Revision = null }).IsValid);
    }

    private static List<string> Columns(int count) =>
        Enumerable.Range(0, count).Select(index => $"column{index}").ToList();

    private static UpsertUserDataTablePreferenceDto Request(Action<DataTablePreferenceSettingsDto> configure)
    {
        var request = new UpsertUserDataTablePreferenceDto();
        configure(request.Settings);
        return request;
    }
}
