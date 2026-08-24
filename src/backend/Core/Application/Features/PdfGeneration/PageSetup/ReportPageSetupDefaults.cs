using Application.Contracts.Report;

namespace Application.Features.Reports;

/// <summary>
/// Canonical page setup option values shared by report definitions.
/// </summary>
public static class ReportPageSetupDefaults
{
    public static readonly IReadOnlyList<string> Formats = new[] { "A4", "A3", "A5", "Letter", "Legal" };
    public static readonly IReadOnlyList<string> Orientations = new[] { "Portrait", "Landscape" };
}
