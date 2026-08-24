using Application.Contracts.Report;

namespace Application.Features.Reports;

/// <summary>
/// Describes a single report filter field.
/// </summary>
public sealed record ReportFilter
{
    /// <summary>The stable filter name sent in report requests.</summary>
    public required string Name { get; init; }

    /// <summary>The user-facing filter label.</summary>
    public required string Label { get; init; }

    /// <summary>The filter input type, such as dropdown, daterange, number, or text.</summary>
    public string Type { get; init; } = "dropdown";

    /// <summary>Optional dropdown options for filters with fixed values.</summary>
    public IReadOnlyList<string>? Options { get; init; }
}
