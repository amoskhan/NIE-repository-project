using Application.Features.DataTablePreferences;

namespace Application.Tests;

public sealed class DataTablePreferenceTableKeyTests
{
    [Theory]
    [InlineData("abc", "abc")]
    [InlineData("purchase-orders", "purchase-orders")]
    [InlineData("procurement.purchase-order.list", "procurement.purchase-order.list")]
    [InlineData("PROCUREMENT.PO.LIST", "procurement.po.list")]
    [InlineData("   Purchase-Orders   ", "purchase-orders")]
    [InlineData("a1b", "a1b")]
    public void Accepts_and_lowercases_a_well_formed_table_key(string input, string expected)
    {
        var accepted = DataTablePreferenceTableKey.TryNormalize(input, out var normalized);

        Assert.True(accepted);
        Assert.Equal(expected, normalized);
        Assert.Equal(expected, DataTablePreferenceTableKey.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("     ")]
    [InlineData("ab")]
    [InlineData(".abc")]
    [InlineData("-abc")]
    [InlineData("abc.")]
    [InlineData("abc-")]
    [InlineData("abc_def")]
    [InlineData("abc def")]
    [InlineData("abc/def")]
    [InlineData("orders;drop")]
    [InlineData("orders'--")]
    [InlineData("<script>")]
    public void Rejects_a_key_outside_the_allowed_shape(string? input)
    {
        var accepted = DataTablePreferenceTableKey.TryNormalize(input, out var normalized);

        Assert.False(accepted);
        Assert.NotNull(normalized);
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(159, true)]
    [InlineData(160, true)]
    [InlineData(161, false)]
    public void Enforces_the_three_to_one_hundred_and_sixty_character_bounds(int length, bool expected)
    {
        var key = new string('a', length);

        Assert.Equal(expected, DataTablePreferenceTableKey.TryNormalize(key, out _));
    }

    [Fact]
    public void Measures_the_length_bounds_after_trimming()
    {
        var padded = "   " + new string('a', 160) + "   ";

        Assert.True(DataTablePreferenceTableKey.TryNormalize(padded, out var normalized));
        Assert.Equal(160, normalized.Length);
    }

    [Fact]
    public void Normalize_reports_the_offending_argument_when_the_key_is_rejected()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = DataTablePreferenceTableKey.Normalize("bad key");
        });

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Normalize_rejects_a_null_key_as_an_argument_error()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            _ = DataTablePreferenceTableKey.Normalize(null);
        });
    }

    [Fact]
    public void Reports_an_empty_normalized_value_for_a_null_key()
    {
        Assert.False(DataTablePreferenceTableKey.TryNormalize(null, out var normalized));
        Assert.Equal(string.Empty, normalized);
    }
}
