using System.Globalization;
using System.Net;
using System.Text;
using Application.Contracts.Report;
using BuildingBlocks.Globals;
using BuildingBlocks.Helpers;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Infrastructure.Providers.PdfGeneration;

/// <summary>
/// Generates application reports as branded HTML and converts them to PDF with Playwright.
/// </summary>
public class PlaywrightPdfGenerationService : IPdfGenerationService
{
    private const string ReportLogoResourceName = "Infrastructure.Providers.Reports.ReportLogo.svg";

    private static readonly CultureInfo SingaporeCulture = CultureInfo.GetCultureInfo("en-SG");

    private readonly MainDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PlaywrightPdfGenerationService> _logger;

    public PlaywrightPdfGenerationService(
        MainDbContext context,
        IConfiguration configuration,
        ILogger<PlaywrightPdfGenerationService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<byte[]> GeneratePdfFromHtmlAsync(
        string html,
        PdfOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var opts = options ?? new PdfOptions();
        try
        {
            var fullHtml = html.Contains("<html", StringComparison.OrdinalIgnoreCase)
                ? html
                : BuildReportDocument("Report", html, new ReportRequestDto { ReportType = "report" });

            cancellationToken.ThrowIfCancellationRequested();
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Args =
                [
                    "--disable-dev-shm-usage",
                    "--no-sandbox",
                    "--disable-gpu",
                    "--disable-extensions",
                    "--disable-background-networking"
                ]
            });

            var page = await browser.NewPageAsync();
            try
            {
                await page.SetContentAsync(fullHtml, new() { WaitUntil = WaitUntilState.Load });

                return await page.PdfAsync(new PagePdfOptions
                {
                    Format = opts.Format,
                    Landscape = opts.Landscape,
                    PrintBackground = opts.PrintBackground,
                    PreferCSSPageSize = true,
                    Margin = CreatePdfMargin(opts.Margin)
                });
            }
            finally
            {
                await page.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Playwright PDF generation failed before conversion completed.");
            throw;
        }
    }

    private static Microsoft.Playwright.Margin? CreatePdfMargin(string? margin)
    {
        if (string.IsNullOrWhiteSpace(margin) || margin == "0")
        {
            return null;
        }

        return new Microsoft.Playwright.Margin
        {
            Top = margin,
            Right = margin,
            Bottom = margin,
            Left = margin
        };
    }

    public IReadOnlyList<ReportTypeDefinition> GetAvailableReportTypes()
    {
        var statusOptions = System.Enum.GetNames<EPurchaseOrderStatus>().Prepend("All").ToList();
        var auditCategoryOptions = System.Enum.GetNames<EAuditCategory>().Prepend("All").ToList();

        return new List<ReportTypeDefinition>
        {
            new()
            {
                Id = "po-summary",
                Name = "Purchase Order Summary",
                Category = "Procurement",
                Icon = "receipt_long",
                Description = "Purchase order status, vendor, and amount summary.",
                Filters = new List<ReportFilter>
                {
                    new() { Name = "status", Label = "Status", Type = "dropdown", Options = statusOptions },
                    new() { Name = "dateRange", Label = "Date Range", Type = "daterange" }
                }
            },
            new()
            {
                Id = "vendor-analysis",
                Name = "Vendor Analysis",
                Category = "Procurement",
                Icon = "storefront",
                Description = "Vendor order volume and approved spend.",
                Filters = new List<ReportFilter>
                {
                    new() { Name = "vendorId", Label = "Vendor ID", Type = "number" },
                    new() { Name = "dateRange", Label = "Date Range", Type = "daterange" }
                }
            },
            new()
            {
                Id = "spending-by-dept",
                Name = "Spending by Requester",
                Category = "Procurement",
                Icon = "payments",
                Description = "Approved spend grouped by requester.",
                Filters = new List<ReportFilter>
                {
                    new() { Name = "dateRange", Label = "Date Range", Type = "daterange" }
                }
            },
            new()
            {
                Id = "approval-timeline",
                Name = "Approval Timeline",
                Category = "Procurement",
                Icon = "approval",
                Description = "Approval actions and most recent processing date.",
                Filters = new List<ReportFilter>
                {
                    new() { Name = "dateRange", Label = "Date Range", Type = "daterange" }
                }
            },
            new()
            {
                Id = "audit-trail",
                Name = "Audit Trail",
                Category = "Audit",
                Icon = "history",
                Description = "Audit events by period, category, and user.",
                PageSetup = new ReportPageSetupDefinition
                {
                    DefaultOrientation = "Landscape"
                },
                Filters = new List<ReportFilter>
                {
                    new() { Name = "dateRange", Label = "Date Range", Type = "daterange" },
                    new() { Name = "category", Label = "Category", Type = "dropdown", Options = auditCategoryOptions }
                }
            },
            new()
            {
                Id = "user-activity",
                Name = "User Activity",
                Category = "Audit",
                Icon = "person_search",
                Description = "User activity counts and most recent action.",
                Filters = new List<ReportFilter>
                {
                    new() { Name = "dateRange", Label = "Date Range", Type = "daterange" },
                    new() { Name = "userId", Label = "User ID", Type = "text" }
                }
            },
        };
    }

    public async Task<string> GenerateReportHtmlAsync(
        string reportType,
        ReportRequestDto filters,
        CancellationToken cancellationToken = default)
    {
        var definition = GetAvailableReportTypes()
            .FirstOrDefault(report => string.Equals(report.Id, reportType, StringComparison.OrdinalIgnoreCase));

        if (definition is null)
        {
            return BuildReportDocument(
                "Report not found",
                "<p class=\"empty-state\">The selected report is not available.</p>",
                filters);
        }

        var effectiveFilters = ReportPageSetupResolver.Apply(definition, filters);
        var content = reportType.ToLowerInvariant() switch
        {
            "po-summary" => await GeneratePoSummaryHtmlAsync(effectiveFilters, cancellationToken),
            "vendor-analysis" => await GenerateVendorAnalysisHtmlAsync(effectiveFilters, cancellationToken),
            "spending-by-dept" => await GenerateSpendingByRequesterHtmlAsync(effectiveFilters, cancellationToken),
            "approval-timeline" => await GenerateApprovalTimelineHtmlAsync(effectiveFilters, cancellationToken),
            "audit-trail" => await GenerateAuditTrailHtmlAsync(effectiveFilters, cancellationToken),
            "user-activity" => await GenerateUserActivityHtmlAsync(effectiveFilters, cancellationToken),
            _ => "<p class=\"empty-state\">The selected report is not available.</p>"
        };

        return BuildReportDocument(definition.Name, content, effectiveFilters);
    }

    private async Task<string> GeneratePoSummaryHtmlAsync(
        ReportRequestDto filters,
        CancellationToken cancellationToken)
    {
        var query = ApplyPurchaseOrderFilters(_context.PurchaseOrders.AsNoTracking(), filters);
        var totalOrders = await query.CountAsync(cancellationToken);
        var totalSpend = await query.SumAsync(po => (decimal?)po.TotalAmount, cancellationToken) ?? 0m;
        var approvedSpend = await query
            .Where(po => po.Status == EPurchaseOrderStatus.Approved)
            .SumAsync(po => (decimal?)po.TotalAmount, cancellationToken) ?? 0m;
        var statusRows = await query
            .GroupBy(po => po.Status)
            .Select(group => new { Status = group.Key, Count = group.Count(), Total = group.Sum(po => po.TotalAmount) })
            .OrderBy(row => row.Status)
            .ToListAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(po => po.RequestDate)
            .Take(50)
            .Select(po => new
            {
                po.PoNumber,
                po.RequestDate,
                po.Status,
                VendorName = po.Vendor.Name,
                po.TotalAmount
            })
            .ToListAsync(cancellationToken);

        var html = new StringBuilder();
        // Page 1: top-level metrics
        html.Append(RenderMetricGrid(new[]
        {
            ("Total Orders", totalOrders.ToString(SingaporeCulture)),
            ("Total Amount", FormatCurrency(totalSpend)),
            ("Approved Spend", FormatCurrency(approvedSpend))
        }));
        html.Append("<h2>Status Breakdown</h2>");
        html.Append(RenderTable(
            new[] { "Status", "Orders", "Amount" },
            statusRows.Select(row => new[]
            {
                row.Status.ToString(),
                row.Count.ToString(SingaporeCulture),
                FormatCurrency(row.Total)
            })));

        // Page 2: recent orders table is its own page so the metrics page stays
        // visually clean and the long table gets the full sheet.
        html.Append("<!-- pagebreak -->");
        html.Append("<h2>Recent Orders</h2>");
        html.Append(RenderTable(
            new[] { "PO Number", "Vendor", "Status", "Request Date", "Amount" },
            rows.Select(row => new[]
            {
                row.PoNumber,
                row.VendorName,
                row.Status.ToString(),
                FormatDate(row.RequestDate),
                FormatCurrency(row.TotalAmount)
            })));

        return html.ToString();
    }

    private async Task<string> GenerateVendorAnalysisHtmlAsync(
        ReportRequestDto filters,
        CancellationToken cancellationToken)
    {
        var query = ApplyPurchaseOrderFilters(_context.PurchaseOrders.AsNoTracking(), filters)
            .Where(po => po.Status == EPurchaseOrderStatus.Approved);

        if (filters.VendorId.HasValue)
        {
            query = query.Where(po => po.VendorId == filters.VendorId.Value);
        }

        var vendors = await query
            .GroupBy(po => new { po.VendorId, po.Vendor.Name })
            .Select(group => new
            {
                group.Key.VendorId,
                VendorName = group.Key.Name,
                Orders = group.Count(),
                Spend = group.Sum(po => po.TotalAmount),
                LastOrderDate = group.Max(po => po.RequestDate)
            })
            .OrderByDescending(row => row.Spend)
            .Take(50)
            .ToListAsync(cancellationToken);

        return RenderTable(
            new[] { "Vendor ID", "Vendor", "Orders", "Approved Spend", "Last Order" },
            vendors.Select(row => new[]
            {
                row.VendorId.ToString(),
                row.VendorName,
                row.Orders.ToString(SingaporeCulture),
                FormatCurrency(row.Spend),
                FormatDate(row.LastOrderDate)
            }));
    }

    private async Task<string> GenerateSpendingByRequesterHtmlAsync(
        ReportRequestDto filters,
        CancellationToken cancellationToken)
    {
        var query = ApplyPurchaseOrderFilters(_context.PurchaseOrders.AsNoTracking(), filters)
            .Where(po => po.Status == EPurchaseOrderStatus.Approved);

        var rows = await query
            .GroupBy(po => po.RequestedByName ?? po.RequestedBy)
            .Select(group => new
            {
                Requester = group.Key,
                Orders = group.Count(),
                Spend = group.Sum(po => po.TotalAmount)
            })
            .OrderByDescending(row => row.Spend)
            .Take(50)
            .ToListAsync(cancellationToken);

        return RenderTable(
            new[] { "Requester", "Orders", "Approved Spend" },
            rows.Select(row => new[]
            {
                row.Requester,
                row.Orders.ToString(SingaporeCulture),
                FormatCurrency(row.Spend)
            }));
    }

    private async Task<string> GenerateApprovalTimelineHtmlAsync(
        ReportRequestDto filters,
        CancellationToken cancellationToken)
    {
        var query = ApplyApprovalDateFilters(_context.PurchaseOrderApprovals.AsNoTracking(), filters);
        var rows = await query
            .Where(approval => approval.ActionDate != null)
            .GroupBy(approval => approval.ApprovalStage)
            .Select(group => new
            {
                Stage = group.Key,
                Actions = group.Count(),
                LastAction = group.Max(approval => approval.ActionDate)
            })
            .OrderBy(row => row.Stage)
            .ToListAsync(cancellationToken);

        return RenderTable(
            new[] { "Stage", "Actions", "Last Action" },
            rows.Select(row => new[]
            {
                row.Stage.ToString(),
                row.Actions.ToString(SingaporeCulture),
                row.LastAction.HasValue ? FormatDate(row.LastAction.Value) : "-"
            }));
    }

    private async Task<string> GenerateAuditTrailHtmlAsync(
        ReportRequestDto filters,
        CancellationToken cancellationToken)
    {
        var query = ApplyAuditFilters(_context.AuditLogs.AsNoTracking(), filters);
        var rows = await query
            .OrderByDescending(log => log.Timestamp)
            .Take(100)
            .Select(log => new
            {
                log.Timestamp,
                log.Category,
                log.Action,
                log.EntityName,
                log.UserId,
                log.Outcome
            })
            .ToListAsync(cancellationToken);

        return RenderTable(
            new[] { "Timestamp", "Category", "Action", "Entity", "User", "Outcome" },
            rows.Select(row => new[]
            {
                FormatDateTime(row.Timestamp),
                row.Category.ToString(),
                row.Action.ToString(),
                row.EntityName,
                row.UserId ?? "-",
                row.Outcome ?? "-"
            }));
    }

    private async Task<string> GenerateUserActivityHtmlAsync(
        ReportRequestDto filters,
        CancellationToken cancellationToken)
    {
        var query = ApplyAuditFilters(_context.AuditLogs.AsNoTracking(), filters);

        if (!string.IsNullOrWhiteSpace(filters.UserId))
        {
            query = query.Where(log => log.UserId == filters.UserId);
        }

        var rows = await query
            .Where(log => log.UserId != null)
            .GroupBy(log => new { log.UserId, log.UserName })
            .Select(group => new
            {
                UserId = group.Key.UserId!,
                UserName = group.Key.UserName,
                Actions = group.Count(),
                LastSeen = group.Max(log => log.Timestamp)
            })
            .OrderByDescending(row => row.Actions)
            .Take(50)
            .ToListAsync(cancellationToken);

        return RenderTable(
            new[] { "User ID", "Name", "Actions", "Last Activity" },
            rows.Select(row => new[]
            {
                row.UserId,
                row.UserName ?? "-",
                row.Actions.ToString(SingaporeCulture),
                FormatDateTime(row.LastSeen)
            }));
    }

    private IQueryable<Domain.Models.PurchaseOrder> ApplyPurchaseOrderFilters(
        IQueryable<Domain.Models.PurchaseOrder> query,
        ReportRequestDto filters)
    {
        query = ApplyPurchaseOrderDateFilters(query, filters);

        if (!string.IsNullOrWhiteSpace(filters.Status)
            && !string.Equals(filters.Status, "All", StringComparison.OrdinalIgnoreCase)
            && System.Enum.TryParse<EPurchaseOrderStatus>(filters.Status, ignoreCase: true, out var status))
        {
            query = query.Where(po => po.Status == status);
        }

        return query;
    }

    private static IQueryable<Domain.Models.PurchaseOrder> ApplyPurchaseOrderDateFilters(
        IQueryable<Domain.Models.PurchaseOrder> query,
        ReportRequestDto filters)
    {
        if (filters.DateFrom.HasValue)
        {
            var start = filters.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(po => po.RequestDate >= start);
        }

        if (filters.DateTo.HasValue)
        {
            var exclusiveEnd = filters.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(po => po.RequestDate < exclusiveEnd);
        }

        return query;
    }

    private static IQueryable<Domain.Models.PurchaseOrderApproval> ApplyApprovalDateFilters(
        IQueryable<Domain.Models.PurchaseOrderApproval> query,
        ReportRequestDto filters)
    {
        if (filters.DateFrom.HasValue)
        {
            var start = filters.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(approval => approval.ActionDate >= start);
        }

        if (filters.DateTo.HasValue)
        {
            var exclusiveEnd = filters.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(approval => approval.ActionDate < exclusiveEnd);
        }

        return query;
    }

    private static IQueryable<Domain.Models.AuditLog> ApplyAuditFilters(
        IQueryable<Domain.Models.AuditLog> query,
        ReportRequestDto filters)
    {
        if (filters.DateFrom.HasValue)
        {
            var start = filters.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(log => log.Timestamp >= start);
        }

        if (filters.DateTo.HasValue)
        {
            var exclusiveEnd = filters.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(log => log.Timestamp < exclusiveEnd);
        }

        if (!string.IsNullOrWhiteSpace(filters.Category)
            && !string.Equals(filters.Category, "All", StringComparison.OrdinalIgnoreCase)
            && System.Enum.TryParse<EAuditCategory>(filters.Category, ignoreCase: true, out var category))
        {
            query = query.Where(log => log.Category == category);
        }

        return query;
    }

    /// <summary>
    /// Page-size metadata in millimetres so the HTML preview can render each
    /// `.report-page` at the exact size that Playwright will print.
    /// </summary>
    private readonly record struct PageDimensions(double WidthMm, double HeightMm);

    private static PageDimensions ResolvePageDimensions(string? format, string? orientation)
    {
        var key = (format ?? "A4").Trim().ToUpperInvariant();
        var dims = key switch
        {
            "A3" => new PageDimensions(297, 420),
            "A5" => new PageDimensions(148, 210),
            "LETTER" => new PageDimensions(215.9, 279.4),
            "LEGAL" => new PageDimensions(215.9, 355.6),
            _ => new PageDimensions(210, 297) // A4
        };

        return string.Equals(orientation, "Landscape", StringComparison.OrdinalIgnoreCase)
            ? new PageDimensions(dims.HeightMm, dims.WidthMm)
            : dims;
    }

    private static string FormatMm(double value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    private string BuildReportDocument(string title, string content, ReportRequestDto filters)
    {
        var appName = Constants.ApplicationName;
        var documentTitle = BuildDocumentTitle(appName, title);
        var generatedAt = DateTimeHelper.Now;
        var logoDataUri = TryGetNieLogoDataUri();

        var page = ResolvePageDimensions(filters.Format, filters.Orientation);
        var pageSizeCss = $"{FormatMm(page.WidthMm)}mm {FormatMm(page.HeightMm)}mm";
        var pageAspectCss = $"{FormatMm(page.WidthMm)} / {FormatMm(page.HeightMm)}";

        // Split content on explicit pagebreak markers. Generators that don't
        // emit any markers produce a single-page report (still rendered with a
        // "Page 1 of 1" footer so pagination is consistently visible).
        var sections = content
            .Split(new[] { "<!-- pagebreak -->" }, StringSplitOptions.None)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();
        if (sections.Count == 0) sections.Add(string.Empty);
        var totalPages = sections.Count;

        var filterSummary = RenderFilterSummary(filters);
        var pagesHtml = new StringBuilder();
        for (var i = 0; i < sections.Count; i++)
        {
            var pageNumber = i + 1;
            // Filter summary appears only on page 1 — it would waste room and
            // be redundant on subsequent pages.
            var pageBody = i == 0
                ? filterSummary + sections[i]
                : sections[i];

            pagesHtml.Append(CultureInfo.InvariantCulture, $"""
                <article class="report-page">
                  <header class="report-header">
                    <div class="brand">{RenderLogo(logoDataUri)}</div>
                    <div class="report-heading">
                      <span class="app-name">{Html(appName.ToUpperInvariant())}</span>
                      <h1 class="report-title-header">{Html(title)}</h1>
                    </div>
                  </header>
                  <main class="report-content">
                    {pageBody}
                  </main>
                  <footer class="report-footer">
                    <span>{Html(appName)}</span>
                    <span class="page-counter">Page {pageNumber.ToString(SingaporeCulture)} of {totalPages.ToString(SingaporeCulture)}</span>
                    <span>Generated {Html(generatedAt.ToString("dd MMM yyyy HH:mm:ss", SingaporeCulture))} SGT</span>
                  </footer>
                </article>
                """);
        }

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{{Html(documentTitle)}}</title>
              <style>
                :root {
                  color-scheme: light;
                  --ink: #172033;
                  --muted: #64748b;
                  --line: #d8dee8;
                  --soft: #f7f9fc;
                  --accent: #b91c1c;
                }
                * { box-sizing: border-box; }
                body {
                  margin: 0;
                  background: #eef2f7;
                  color: var(--ink);
                  font-family: "Segoe UI", Arial, sans-serif;
                  font-size: 13px;
                  line-height: 1.5;
                }
                .report-shell {
                  display: flex;
                  flex-direction: column;
                  align-items: center;
                  gap: 12px;
                  padding: 12px 0;
                  margin: 0 auto;
                }
                /* Each .report-page is a sized paper sheet. We render one per
                   logical section so HTML preview shows visible pagination,
                   and `page-break-after: always` forces real PDF page breaks. */
                .report-page {
                  width: var(--page-w);
                  min-height: var(--page-h);
                  margin: 0 auto;
                  display: flex;
                  flex-direction: column;
                  background: #fff;
                  box-shadow: 0 22px 55px rgba(15, 23, 42, 0.16);
                  page-break-after: always;
                  break-after: page;
                }
                .report-page:last-child {
                  page-break-after: auto;
                  break-after: auto;
                }
                .report-header {
                  flex-shrink: 0;
                  display: grid;
                  grid-template-columns: 1fr 2fr;
                  align-items: center;
                  gap: 18px;
                  padding: 14mm 16mm 10mm;
                  border-bottom: 1px solid var(--line);
                }
                .brand {
                  display: flex;
                  align-items: center;
                  justify-content: flex-start;
                  gap: 10px;
                }
                /* Logo is the dominant brand element — sized large so the
                   institute mark is clearly identifiable at a glance. */
                .brand img { max-width: 220px; max-height: 84px; object-fit: contain; }
                .brand-mark {
                  display: inline-flex;
                  align-items: center;
                  justify-content: center;
                  width: 84px;
                  height: 84px;
                  border: 1px solid var(--line);
                  border-radius: 10px;
                  color: var(--accent);
                  font-size: 22px;
                  font-weight: 800;
                }
                /* Right-align the title block so the brand and report identity
                   read as opposing anchors in the page header. */
                .report-heading {
                  display: flex;
                  flex-direction: column;
                  align-items: flex-end;
                  justify-content: center;
                  text-align: right;
                  gap: 6px;
                  min-width: 0;
                }
                .app-name {
                  color: var(--muted);
                  font-size: 11px;
                  font-weight: 700;
                  letter-spacing: 0.18em;
                  text-transform: uppercase;
                }
                .report-title-header {
                  margin: 0;
                  color: var(--ink);
                  font-size: 28px;
                  font-weight: 800;
                  line-height: 1.15;
                  letter-spacing: 0;
                }
                .report-content {
                  flex: 1;
                  padding: 10mm 16mm 14mm;
                  display: flex;
                  flex-direction: column;
                }
                .filter-summary {
                  display: flex;
                  flex-wrap: wrap;
                  gap: 8px;
                  margin: 12px 0 18px;
                }
                .filter-chip {
                  border: 1px solid var(--line);
                  border-radius: 999px;
                  padding: 4px 9px;
                  color: var(--muted);
                  background: var(--soft);
                  font-size: 11px;
                }
                h2 {
                  margin: 22px 0 8px;
                  font-size: 15px;
                  letter-spacing: 0;
                }
                table {
                  width: 100%;
                  border-collapse: collapse;
                  margin: 8px 0 18px;
                }
                th {
                  background: var(--soft);
                  color: #475569;
                  font-size: 10px;
                  letter-spacing: 0.04em;
                  text-align: left;
                  text-transform: uppercase;
                }
                th, td {
                  border-bottom: 1px solid var(--line);
                  padding: 8px 9px;
                  vertical-align: top;
                }
                td { color: #243044; }
                .metric-grid {
                  display: grid;
                  grid-template-columns: repeat(3, minmax(0, 1fr));
                  gap: 10px;
                  margin: 16px 0 20px;
                }
                .metric {
                  border: 1px solid var(--line);
                  border-radius: 6px;
                  padding: 12px;
                  background: #fff;
                }
                .metric span {
                  display: block;
                  color: var(--muted);
                  font-size: 11px;
                  font-weight: 700;
                  text-transform: uppercase;
                }
                .metric strong {
                  display: block;
                  margin-top: 4px;
                  font-size: 20px;
                }
                .empty-state {
                  padding: 28px;
                  border: 1px dashed var(--line);
                  border-radius: 6px;
                  color: var(--muted);
                  text-align: center;
                }
                .report-footer {
                  display: grid;
                  grid-template-columns: 1fr auto 1fr;
                  gap: 12px;
                  padding: 7mm 16mm 10mm;
                  border-top: 1px solid var(--line);
                  color: var(--muted);
                  font-size: 10px;
                }
                .report-footer span:nth-child(2) { text-align: center; }
                .report-footer span:last-child { text-align: right; }
                .page-counter {
                  font-weight: 700;
                  color: var(--ink);
                }
                @media screen and (max-width: 760px) {
                  body {
                    background: #fff;
                    font-size: 12px;
                  }
                  .report-shell {
                    width: 100%;
                    align-items: stretch;
                    gap: 0;
                    padding: 0;
                  }
                  .report-page {
                    width: 100%;
                    aspect-ratio: var(--page-aspect);
                    min-height: auto;
                    margin: 0;
                    box-shadow: none;
                  }
                  .report-header {
                    grid-template-columns: minmax(0, 0.9fr) minmax(0, 1.1fr);
                    gap: 10px;
                    padding: 14px 14px 12px;
                  }
                  .brand img {
                    max-width: 112px;
                    max-height: 44px;
                  }
                  .brand-mark {
                    width: 48px;
                    height: 48px;
                    border-radius: 8px;
                    font-size: 16px;
                  }
                  .app-name {
                    font-size: 8px;
                    letter-spacing: 0.1em;
                    overflow-wrap: anywhere;
                  }
                  .report-title-header {
                    font-size: 17px;
                    overflow-wrap: anywhere;
                  }
                  .report-content {
                    padding: 12px 14px 16px;
                  }
                  .filter-summary {
                    gap: 6px;
                    margin: 8px 0 12px;
                  }
                  .filter-chip {
                    padding: 3px 7px;
                    font-size: 9px;
                  }
                  h2 {
                    margin: 16px 0 8px;
                    font-size: 13px;
                  }
                  table {
                    table-layout: fixed;
                    margin-bottom: 14px;
                  }
                  th {
                    font-size: 8px;
                  }
                  th, td {
                    padding: 6px;
                    overflow-wrap: anywhere;
                  }
                  .metric-grid {
                    grid-template-columns: repeat(auto-fit, minmax(8.5rem, 1fr));
                    gap: 8px;
                    margin: 12px 0 16px;
                  }
                  .metric {
                    padding: 10px;
                  }
                  .metric span {
                    font-size: 9px;
                  }
                  .metric strong {
                    font-size: 16px;
                  }
                  .report-footer {
                    grid-template-columns: 1fr;
                    gap: 3px;
                    padding: 10px 14px 12px;
                    font-size: 8px;
                  }
                  .report-footer span,
                  .report-footer span:nth-child(2),
                  .report-footer span:last-child {
                    text-align: left;
                  }
                }
                @page { size: {{pageSizeCss}}; margin: 0; }
                @media print {
                  body { background: #fff; }
                  .report-shell { padding: 0; gap: 0; }
                  .report-page { box-shadow: none; margin: 0; }
                }
              </style>
            </head>
            <body style="--page-w: {{FormatMm(page.WidthMm)}}mm; --page-h: {{FormatMm(page.HeightMm)}}mm; --page-aspect: {{pageAspectCss}};">
              <section class="report-shell">
                {{pagesHtml}}
              </section>
            </body>
            </html>
            """;
    }

    private static string RenderMetricGrid(IEnumerable<(string Label, string Value)> metrics)
    {
        var html = new StringBuilder("<div class=\"metric-grid\">");
        foreach (var metric in metrics)
        {
            html.Append(CultureInfo.InvariantCulture, $"""
                <div class="metric">
                  <span>{Html(metric.Label)}</span>
                  <strong>{Html(metric.Value)}</strong>
                </div>
                """);
        }

        html.Append("</div>");
        return html.ToString();
    }

    private static string RenderTable(IEnumerable<string> headers, IEnumerable<IEnumerable<string?>> rows)
    {
        var rowList = rows.ToList();
        if (rowList.Count == 0)
        {
            return "<p class=\"empty-state\">No records match the selected filters.</p>";
        }

        var html = new StringBuilder("<table><thead><tr>");
        foreach (var header in headers)
        {
            html.Append(CultureInfo.InvariantCulture, $"<th>{Html(header)}</th>");
        }

        html.Append("</tr></thead><tbody>");
        foreach (var row in rowList)
        {
            html.Append("<tr>");
            foreach (var value in row)
            {
                html.Append(CultureInfo.InvariantCulture, $"<td>{Html(value ?? "-")}</td>");
            }
            html.Append("</tr>");
        }

        html.Append("</tbody></table>");
        return html.ToString();
    }

    private static string RenderFilterSummary(ReportRequestDto filters)
    {
        var entries = new List<(string Label, string? Value)>
        {
            ("Status", filters.Status),
            ("From", filters.DateFrom.HasValue ? FormatDate(filters.DateFrom.Value) : null),
            ("To", filters.DateTo.HasValue ? FormatDate(filters.DateTo.Value) : null),
            ("Vendor", filters.VendorId?.ToString()),
            ("Category", filters.Category),
            ("User", filters.UserId)
        }
        .Where(entry => !string.IsNullOrWhiteSpace(entry.Value))
        .ToList();

        if (entries.Count == 0)
        {
            return "<div class=\"filter-summary\"><span class=\"filter-chip\">All records</span></div>";
        }

        return "<div class=\"filter-summary\">"
            + string.Join(
                string.Empty,
                entries.Select(entry => $"<span class=\"filter-chip\">{Html(entry.Label)}: {Html(entry.Value)}</span>"))
            + "</div>";
    }

    private static string RenderLogo(string? logoDataUri)
    {
        return string.IsNullOrWhiteSpace(logoDataUri)
            ? "<span class=\"brand-mark\">NIE</span>"
            : $"<img src=\"{Html(logoDataUri)}\" alt=\"NIE\">";
    }

    private string? TryGetNieLogoDataUri()
    {
        try
        {
            using var stream = typeof(PlaywrightPdfGenerationService).Assembly
                .GetManifestResourceStream(ReportLogoResourceName);
            if (stream is null)
            {
                _logger.LogWarning("Embedded report logo resource {ResourceName} was not found.", ReportLogoResourceName);
                return null;
            }

            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            var bytes = memory.ToArray();
            var mimeType = "image/svg+xml";
            return $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to read embedded report logo resource {ResourceName}", ReportLogoResourceName);
            return null;
        }
    }

    private static string BuildDocumentTitle(string appName, string title)
    {
        if (string.IsNullOrWhiteSpace(appName))
        {
            return title;
        }

        return string.IsNullOrWhiteSpace(title)
            ? appName
            : $"{title} | {appName}";
    }

    private static string FormatCurrency(decimal amount) =>
        amount.ToString("C", SingaporeCulture);

    private static string FormatDate(DateTime date) =>
        date.ToString("dd MMM yyyy", SingaporeCulture);

    private static string FormatDate(DateOnly date) =>
        date.ToString("dd MMM yyyy", SingaporeCulture);

    private static string FormatDateTime(DateTime date) =>
        date.ToString("dd MMM yyyy HH:mm", SingaporeCulture);

    private static string Html(string? value) =>
        WebUtility.HtmlEncode(value ?? string.Empty);

}
