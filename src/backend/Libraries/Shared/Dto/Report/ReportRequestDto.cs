using System.ComponentModel.DataAnnotations;

namespace Shared.Dto.Report;

/// <summary>
/// Request payload used to generate or preview an application report.
/// </summary>
public sealed record ReportRequestDto
{
    /// <summary>The report type identifier, such as po-summary or audit-trail.</summary>
    [Required]
    [MaxLength(100)]
    public required string ReportType { get; init; }

    /// <summary>Optional purchase-order status filter.</summary>
    [MaxLength(50)]
    public string? Status { get; init; }

    /// <summary>Optional inclusive start date for the report period.</summary>
    public DateOnly? DateFrom { get; init; }

    /// <summary>Optional inclusive end date for the report period.</summary>
    public DateOnly? DateTo { get; init; }

    /// <summary>Optional vendor identifier filter.</summary>
    [Range(1, int.MaxValue)]
    public int? VendorId { get; init; }

    /// <summary>Optional report category filter.</summary>
    [MaxLength(100)]
    public string? Category { get; init; }

    /// <summary>Optional user identifier filter.</summary>
    [MaxLength(100)]
    public string? UserId { get; init; }

    /// <summary>Paper format: A4, A3, A5, Letter, Legal. Defaults to A4.</summary>
    [MaxLength(10)]
    public string? Format { get; init; }

    /// <summary>Page orientation: "Portrait" (default) or "Landscape".</summary>
    [MaxLength(10)]
    public string? Orientation { get; init; }
}
