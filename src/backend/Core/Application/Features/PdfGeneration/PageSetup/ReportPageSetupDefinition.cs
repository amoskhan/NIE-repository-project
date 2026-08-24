using Application.Contracts.Report;

namespace Application.Features.Reports;

/// <summary>
/// Defines default report paper settings and whether users may change them.
/// </summary>
public sealed record ReportPageSetupDefinition
{
    /// <summary>Default paper format: A4, A3, A5, Letter, or Legal.</summary>
    public string DefaultFormat { get; init; } = "A4";

    /// <summary>Default page orientation: Portrait or Landscape.</summary>
    public string DefaultOrientation { get; init; } = "Portrait";

    /// <summary>Whether users may choose a different paper format.</summary>
    public bool AllowFormatChange { get; init; } = true;

    /// <summary>Whether users may choose a different page orientation.</summary>
    public bool AllowOrientationChange { get; init; } = true;

    /// <summary>Allowed paper formats shown to users when format changes are enabled.</summary>
    public IReadOnlyList<string> Formats { get; init; } = ReportPageSetupDefaults.Formats;

    /// <summary>Allowed orientations shown to users when orientation changes are enabled.</summary>
    public IReadOnlyList<string> Orientations { get; init; } = ReportPageSetupDefaults.Orientations;
}
