using Application.Contracts.Report;

namespace Application.Features.Reports;

/// <summary>
/// Resolves report page setup defaults and locked settings into an effective request.
/// </summary>
public static class ReportPageSetupResolver
{
    public static ReportRequestDto Apply(ReportTypeDefinition definition, ReportRequestDto request)
    {
        var pageSetup = definition.PageSetup;
        return request with
        {
            Format = ResolveFormat(request.Format, pageSetup),
            Orientation = ResolveOrientation(request.Orientation, pageSetup)
        };
    }

    public static string ResolveFormat(string? requestedFormat, ReportPageSetupDefinition pageSetup)
    {
        var defaultFormat = NormalizeFormat(pageSetup.DefaultFormat) ?? "A4";
        if (!pageSetup.AllowFormatChange)
        {
            return defaultFormat;
        }

        var allowedFormats = NormalizedFormats(pageSetup.Formats);
        if (allowedFormats.Count == 0)
        {
            allowedFormats = NormalizedFormats(ReportPageSetupDefaults.Formats);
        }

        if (!allowedFormats.Contains(defaultFormat, StringComparer.OrdinalIgnoreCase))
        {
            allowedFormats.Add(defaultFormat);
        }

        var requested = NormalizeFormat(requestedFormat);
        return requested is not null && allowedFormats.Contains(requested, StringComparer.OrdinalIgnoreCase)
            ? requested
            : defaultFormat;
    }

    public static string ResolveOrientation(string? requestedOrientation, ReportPageSetupDefinition pageSetup)
    {
        var defaultOrientation = NormalizeOrientation(pageSetup.DefaultOrientation) ?? "Portrait";
        if (!pageSetup.AllowOrientationChange)
        {
            return defaultOrientation;
        }

        var allowedOrientations = NormalizedOrientations(pageSetup.Orientations);
        if (allowedOrientations.Count == 0)
        {
            allowedOrientations = NormalizedOrientations(ReportPageSetupDefaults.Orientations);
        }

        if (!allowedOrientations.Contains(defaultOrientation, StringComparer.OrdinalIgnoreCase))
        {
            allowedOrientations.Add(defaultOrientation);
        }

        var requested = NormalizeOrientation(requestedOrientation);
        return requested is not null && allowedOrientations.Contains(requested, StringComparer.OrdinalIgnoreCase)
            ? requested
            : defaultOrientation;
    }

    public static string? NormalizeFormat(string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return null;
        }

        return format.Trim().ToUpperInvariant() switch
        {
            "A3" => "A3",
            "A4" => "A4",
            "A5" => "A5",
            "LETTER" => "Letter",
            "LEGAL" => "Legal",
            _ => null
        };
    }

    public static string? NormalizeOrientation(string? orientation)
    {
        if (string.IsNullOrWhiteSpace(orientation))
        {
            return null;
        }

        return orientation.Trim().ToUpperInvariant() switch
        {
            "PORTRAIT" => "Portrait",
            "LANDSCAPE" => "Landscape",
            _ => null
        };
    }

    private static List<string> NormalizedFormats(IEnumerable<string> formats) =>
        formats
            .Select(NormalizeFormat)
            .Where(format => format is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(format => format!)
            .ToList();

    private static List<string> NormalizedOrientations(IEnumerable<string> orientations) =>
        orientations
            .Select(NormalizeOrientation)
            .Where(orientation => orientation is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(orientation => orientation!)
            .ToList();
}
