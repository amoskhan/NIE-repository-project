namespace Application.Contracts.Report;

/// <summary>
/// Request payload used to generate or preview an application report.
/// </summary>
public sealed record ReportRequestDto
{
    /// <summary>The report type identifier, such as po-summary or audit-trail.</summary>
    public required string ReportType { get; init; }

    /// <summary>Optional purchase-order status filter.</summary>
    public string? Status { get; init; }

    /// <summary>Optional inclusive start date for the report period.</summary>
    public DateOnly? DateFrom { get; init; }

    /// <summary>Optional inclusive end date for the report period.</summary>
    public DateOnly? DateTo { get; init; }

    /// <summary>Optional vendor identifier filter.</summary>
    public Guid? VendorId { get; init; }

    /// <summary>Optional report category filter.</summary>
    public string? Category { get; init; }

    /// <summary>Optional user identifier filter.</summary>
    public string? UserId { get; init; }

    /// <summary>Paper format: A4, A3, A5, Letter, Legal. Defaults to A4.</summary>
    public string? Format { get; init; }

    /// <summary>Page orientation: "Portrait" (default) or "Landscape".</summary>
    public string? Orientation { get; init; }
}
