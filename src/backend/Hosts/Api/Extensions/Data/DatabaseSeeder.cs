using BuildingBlocks.Helpers;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Extensions;

/// <summary>
/// Database seeder that populates demo data for development and testing.
/// Usage: dotnet run -- seed
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(MainDbContext context)
    {
        // System seed data is applied by MainDbContextSeeder through EF Core
        // UseSeeding/UseAsyncSeeding when migrations run.

        // === SAMPLE: procurement demo seed (reference vertical; remove only after approved replacement) ===
        await SeedVendorsAsync(context);
        await SeedCatalogItemsAsync(context);
        await SeedPurchaseOrdersAsync(context);
        // === END SAMPLE ===

        // Showcase data: idempotent, additive — runs on top of whatever exists
        // so reports (po-summary / vendor-analysis / spending-by-dept /
        // approval-timeline / audit-trail / user-activity) all show realistic
        // populated rows. Safe to re-invoke on a partially seeded DB.
        // === SAMPLE: procurement report-showcase seed (reference vertical; remove only after approved replacement) ===
        await SeedReportShowcaseAsync(context);
        // === END SAMPLE ===
    }

    /// <summary>
    /// Seeds extra demo data so all reports render with non-trivial rows:
    /// extra vendors / catalog items, ~25 POs spread over 60 days across
    /// 6 named requesters and all status buckets, approval records for every
    /// PO that lacks them, and 200 explicit audit-log entries across users
    /// and categories. Idempotent — uses marker checks (demo vendor code,
    /// "PO-DEMO-" PO-number prefix, "demo-seed-v1" correlation id) so each
    /// step skips if its rows already exist.
    /// </summary>
    public static async Task SeedReportShowcaseAsync(MainDbContext context)
    {
        await SeedExtraVendorsAsync(context);
        await SeedExtraCatalogItemsAsync(context);
        await SeedExtraPurchaseOrdersAsync(context);
        await SeedPurchaseOrderApprovalsAsync(context);
        await SeedDemoAuditLogsAsync(context);
    }

    // === SAMPLE: procurement demo-seed methods below (reference vertical; remove only after approved replacement) ===
    private static async Task SeedVendorsAsync(MainDbContext context)
    {
        if (await context.Vendors.AnyAsync()) return;

        context.Vendors.AddRange(
            new Vendor { Name = "Tech Solutions Pte Ltd", Code = "TECH-001", ContactPerson = "John Lim", Email = "sales@techsolutions.sg", Phone = "+65 6789 0123", Address = "1 Science Park Drive, Singapore 118221", Category = "IT Equipment", IsActive = true, CreatedBy = "seeder", CreatedOn = DateTimeHelper.Now },
            new Vendor { Name = "Office Essentials Singapore", Code = "OES-001", ContactPerson = "Sarah Tan", Email = "orders@officeessentials.sg", Phone = "+65 6234 5678", Address = "88 Thomson Road, Singapore 307684", Category = "Office Supplies", IsActive = true, CreatedBy = "seeder", CreatedOn = DateTimeHelper.Now },
            new Vendor { Name = "FurniCraft Industries", Code = "FCI-001", ContactPerson = "David Wong", Email = "info@furnicraft.sg", Phone = "+65 6890 1234", Address = "25 International Business Park, Singapore 609916", Category = "Furniture", IsActive = true, CreatedBy = "seeder", CreatedOn = DateTimeHelper.Now }
        );
        await context.SaveChangesAsync();
    }

    private static async Task SeedCatalogItemsAsync(MainDbContext context)
    {
        if (await context.CatalogItems.AnyAsync()) return;

        var vendors = await context.Vendors.ToListAsync();
        var techVendor = vendors.First(v => v.Code == "TECH-001");
        var officeVendor = vendors.First(v => v.Code == "OES-001");
        var furniVendor = vendors.First(v => v.Code == "FCI-001");
        var now = DateTimeHelper.Now;

        context.CatalogItems.AddRange(
            // Tech vendor items
            new CatalogItem { Name = "Dell Latitude 5540 Laptop", Sku = "DELL-LAT5540", Description = "14\" business laptop, Intel i7, 16GB RAM, 512GB SSD", Category = "Hardware", UnitOfMeasure = "Each", UnitPrice = 1899.00m, VendorId = techVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new CatalogItem { Name = "Dell 27\" Monitor U2723QE", Sku = "DELL-U2723QE", Description = "27\" 4K UltraSharp USB-C Hub Monitor", Category = "Hardware", UnitOfMeasure = "Each", UnitPrice = 749.00m, VendorId = techVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new CatalogItem { Name = "Microsoft 365 Business License", Sku = "MS365-BIZ", Description = "Annual Microsoft 365 Business Standard license per user", Category = "Software", UnitOfMeasure = "Each", UnitPrice = 264.00m, VendorId = techVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new CatalogItem { Name = "Logitech MX Keys Keyboard", Sku = "LOG-MXKEYS", Description = "Advanced wireless illuminated keyboard", Category = "Hardware", UnitOfMeasure = "Each", UnitPrice = 159.00m, VendorId = techVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            // Office vendor items
            new CatalogItem { Name = "A4 Copy Paper (80gsm)", Sku = "OES-A4PAPER", Description = "Premium A4 paper 80gsm, 500 sheets per ream", Category = "Stationery", UnitOfMeasure = "Ream", UnitPrice = 5.90m, VendorId = officeVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new CatalogItem { Name = "Pilot G-2 Gel Pen (Black)", Sku = "OES-PILOTG2", Description = "Pilot G-2 0.7mm gel pen, box of 12", Category = "Stationery", UnitOfMeasure = "Box", UnitPrice = 18.50m, VendorId = officeVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new CatalogItem { Name = "3M Post-It Notes (3x3)", Sku = "OES-POSTIT3", Description = "Post-It sticky notes 76x76mm, pack of 12 pads", Category = "Stationery", UnitOfMeasure = "Pack", UnitPrice = 12.80m, VendorId = officeVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new CatalogItem { Name = "Hand Sanitizer (500ml)", Sku = "OES-HSANI500", Description = "Antibacterial hand sanitizer 500ml pump bottle", Category = "Cleaning", UnitOfMeasure = "Each", UnitPrice = 8.90m, VendorId = officeVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            // Furniture vendor items
            new CatalogItem { Name = "Ergonomic Office Chair", Sku = "FCI-ERGOCHAIR", Description = "Adjustable ergonomic mesh office chair with lumbar support", Category = "Furniture", UnitOfMeasure = "Each", UnitPrice = 489.00m, VendorId = furniVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new CatalogItem { Name = "Standing Desk (120x60cm)", Sku = "FCI-STDESK120", Description = "Electric height-adjustable standing desk, white top", Category = "Furniture", UnitOfMeasure = "Each", UnitPrice = 699.00m, VendorId = furniVendor.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now }
        );
        await context.SaveChangesAsync();
    }

    private static async Task SeedPurchaseOrdersAsync(MainDbContext context)
    {
        if (await context.PurchaseOrders.AnyAsync()) return;

        var vendors = await context.Vendors.ToListAsync();
        var catalogItems = await context.CatalogItems.ToListAsync();
        var techVendor = vendors.First(v => v.Code == "TECH-001");
        var officeVendor = vendors.First(v => v.Code == "OES-001");
        var furniVendor = vendors.First(v => v.Code == "FCI-001");
        var now = DateTimeHelper.Now;

        // PO 1: Draft - IT equipment
        var po1 = new PurchaseOrder
        {
            PoNumber = "PO-2025-0001",
            RequestedBy = "devia",
            RequestedByName = "Devi Anggraini",
            RequestDate = now.AddDays(-2),
            DeliveryAddress = "NIE Block 7",
            ExpectedDeliveryDate = now.AddDays(14),
            Status = EPurchaseOrderStatus.Draft,
            Notes = "New laptops for incoming research assistants",
            VendorId = techVendor.Id,
            TotalAmount = 0, // Will calculate below
            CreatedBy = "devia",
            CreatedOn = now.AddDays(-2)
        };

        // PO 2: Submitted - Office supplies
        var po2 = new PurchaseOrder
        {
            PoNumber = "PO-2025-0002",
            RequestedBy = "devia",
            RequestedByName = "Devi Anggraini",
            RequestDate = now.AddDays(-5),
            DeliveryAddress = "NIE Block 1",
            ExpectedDeliveryDate = now.AddDays(7),
            Status = EPurchaseOrderStatus.Submitted,
            Notes = "Monthly stationery replenishment for academic group",
            VendorId = officeVendor.Id,
            TotalAmount = 0,
            CreatedBy = "devia",
            CreatedOn = now.AddDays(-5)
        };

        // PO 3: Approved - Furniture
        var po3 = new PurchaseOrder
        {
            PoNumber = "PO-2025-0003",
            RequestedBy = "devia",
            RequestedByName = "Devi Anggraini",
            RequestDate = now.AddDays(-10),
            DeliveryAddress = "NIE Block 2",
            ExpectedDeliveryDate = now.AddDays(21),
            Status = EPurchaseOrderStatus.Approved,
            Notes = "Ergonomic furniture upgrade for faculty offices",
            VendorId = furniVendor.Id,
            TotalAmount = 0,
            CreatedBy = "devia",
            CreatedOn = now.AddDays(-10)
        };

        // PO 4: Rejected - Software
        var po4 = new PurchaseOrder
        {
            PoNumber = "PO-2025-0004",
            RequestedBy = "devia",
            RequestedByName = "Devi Anggraini",
            RequestDate = now.AddDays(-8),
            DeliveryAddress = "NIE Block 1",
            Status = EPurchaseOrderStatus.Rejected,
            Notes = "Additional software licenses",
            RejectionReason = "Budget exceeded for this quarter. Please resubmit in Q3.",
            VendorId = techVendor.Id,
            TotalAmount = 0,
            CreatedBy = "devia",
            CreatedOn = now.AddDays(-8)
        };

        // PO 5: Pending Manager Approval
        var po5 = new PurchaseOrder
        {
            PoNumber = "PO-2025-0005",
            RequestedBy = "devia",
            RequestedByName = "Devi Anggraini",
            RequestDate = now.AddDays(-1),
            DeliveryAddress = "NIE Library",
            ExpectedDeliveryDate = now.AddDays(10),
            Status = EPurchaseOrderStatus.PendingManagerApproval,
            Notes = "Monitors for new hot-desking area",
            VendorId = techVendor.Id,
            TotalAmount = 0,
            CreatedBy = "devia",
            CreatedOn = now.AddDays(-1)
        };

        context.PurchaseOrders.AddRange(po1, po2, po3, po4, po5);
        await context.SaveChangesAsync();

        // Add lines for each PO
        var laptop = catalogItems.First(c => c.Sku == "DELL-LAT5540");
        var monitor = catalogItems.First(c => c.Sku == "DELL-U2723QE");
        var keyboard = catalogItems.First(c => c.Sku == "LOG-MXKEYS");
        var paper = catalogItems.First(c => c.Sku == "OES-A4PAPER");
        var pens = catalogItems.First(c => c.Sku == "OES-PILOTG2");
        var postIt = catalogItems.First(c => c.Sku == "OES-POSTIT3");
        var ms365 = catalogItems.First(c => c.Sku == "MS365-BIZ");
        var chair = catalogItems.First(c => c.Sku == "FCI-ERGOCHAIR");
        var desk = catalogItems.First(c => c.Sku == "FCI-STDESK120");

        // PO1 lines: 3 laptops + 3 keyboards
        context.PurchaseOrderLines.AddRange(
            new PurchaseOrderLine { PurchaseOrderId = po1.Id, LineNumber = 1, ItemName = laptop.Name, Description = laptop.Description, UnitOfMeasure = "Each", Quantity = 3, UnitPrice = laptop.UnitPrice, LineTotal = 3 * laptop.UnitPrice, CatalogItemId = laptop.Id, CreatedBy = "seeder", CreatedOn = now },
            new PurchaseOrderLine { PurchaseOrderId = po1.Id, LineNumber = 2, ItemName = keyboard.Name, Description = keyboard.Description, UnitOfMeasure = "Each", Quantity = 3, UnitPrice = keyboard.UnitPrice, LineTotal = 3 * keyboard.UnitPrice, CatalogItemId = keyboard.Id, CreatedBy = "seeder", CreatedOn = now }
        );
        po1.TotalAmount = (3 * laptop.UnitPrice) + (3 * keyboard.UnitPrice);

        // PO2 lines: paper, pens, post-its
        context.PurchaseOrderLines.AddRange(
            new PurchaseOrderLine { PurchaseOrderId = po2.Id, LineNumber = 1, ItemName = paper.Name, Description = paper.Description, UnitOfMeasure = "Ream", Quantity = 20, UnitPrice = paper.UnitPrice, LineTotal = 20 * paper.UnitPrice, CatalogItemId = paper.Id, CreatedBy = "seeder", CreatedOn = now },
            new PurchaseOrderLine { PurchaseOrderId = po2.Id, LineNumber = 2, ItemName = pens.Name, Description = pens.Description, UnitOfMeasure = "Box", Quantity = 5, UnitPrice = pens.UnitPrice, LineTotal = 5 * pens.UnitPrice, CatalogItemId = pens.Id, CreatedBy = "seeder", CreatedOn = now },
            new PurchaseOrderLine { PurchaseOrderId = po2.Id, LineNumber = 3, ItemName = postIt.Name, Description = postIt.Description, UnitOfMeasure = "Pack", Quantity = 3, UnitPrice = postIt.UnitPrice, LineTotal = 3 * postIt.UnitPrice, CatalogItemId = postIt.Id, CreatedBy = "seeder", CreatedOn = now }
        );
        po2.TotalAmount = (20 * paper.UnitPrice) + (5 * pens.UnitPrice) + (3 * postIt.UnitPrice);

        // PO3 lines: 4 chairs + 4 desks
        context.PurchaseOrderLines.AddRange(
            new PurchaseOrderLine { PurchaseOrderId = po3.Id, LineNumber = 1, ItemName = chair.Name, Description = chair.Description, UnitOfMeasure = "Each", Quantity = 4, UnitPrice = chair.UnitPrice, LineTotal = 4 * chair.UnitPrice, CatalogItemId = chair.Id, CreatedBy = "seeder", CreatedOn = now },
            new PurchaseOrderLine { PurchaseOrderId = po3.Id, LineNumber = 2, ItemName = desk.Name, Description = desk.Description, UnitOfMeasure = "Each", Quantity = 4, UnitPrice = desk.UnitPrice, LineTotal = 4 * desk.UnitPrice, CatalogItemId = desk.Id, CreatedBy = "seeder", CreatedOn = now }
        );
        po3.TotalAmount = (4 * chair.UnitPrice) + (4 * desk.UnitPrice);

        // PO4 lines: 10 M365 licenses
        context.PurchaseOrderLines.AddRange(
            new PurchaseOrderLine { PurchaseOrderId = po4.Id, LineNumber = 1, ItemName = ms365.Name, Description = ms365.Description, UnitOfMeasure = "Each", Quantity = 10, UnitPrice = ms365.UnitPrice, LineTotal = 10 * ms365.UnitPrice, CatalogItemId = ms365.Id, CreatedBy = "seeder", CreatedOn = now }
        );
        po4.TotalAmount = 10 * ms365.UnitPrice;

        // PO5 lines: 6 monitors
        context.PurchaseOrderLines.AddRange(
            new PurchaseOrderLine { PurchaseOrderId = po5.Id, LineNumber = 1, ItemName = monitor.Name, Description = monitor.Description, UnitOfMeasure = "Each", Quantity = 6, UnitPrice = monitor.UnitPrice, LineTotal = 6 * monitor.UnitPrice, CatalogItemId = monitor.Id, CreatedBy = "seeder", CreatedOn = now }
        );
        po5.TotalAmount = 6 * monitor.UnitPrice;

        await context.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Showcase data — runs after the base seed so reports show full tables.
    // ─────────────────────────────────────────────────────────────────────

    private const string DemoVendorMarkerCode = "DEMO-CLN-001";
    private const string DemoPoNumberPrefix = "PO-DEMO-";
    private const string DemoAuditCorrelationId = "demo-seed-v1";

    /// <summary>
    /// Stable named users used across PO requesters and audit logs so the
    /// spending-by-dept and user-activity reports show multiple rows.
    /// </summary>
    private static readonly (string Id, string Name)[] DemoUsers =
    {
        ("devia",       "Devi Anggraini"),
        ("kamaludemy",  "Kamaludeen Yousuf"),
        ("tanlimei",    "Tan Li Mei"),
        ("johnyeo",     "John Yeo"),
        ("sarahcheng",  "Sarah Cheng"),
        ("michaeltay",  "Michael Tay")
    };

    private static async Task SeedExtraVendorsAsync(MainDbContext context)
    {
        if (await context.Vendors.AnyAsync(v => v.Code == DemoVendorMarkerCode)) return;

        var now = DateTimeHelper.Now;
        context.Vendors.AddRange(
            new Vendor { Name = "CleanMax Services", Code = DemoVendorMarkerCode, ContactPerson = "Aisha Rahman", Email = "service@cleanmax.sg", Phone = "+65 6444 1122", Address = "12 Tuas Avenue 4, Singapore 639372", Category = "Maintenance", IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new Vendor { Name = "LogiTrans Express", Code = "DEMO-LGX-001", ContactPerson = "Marcus Lim", Email = "ops@logitrans.sg", Phone = "+65 6900 7788", Address = "5 Changi North Way, Singapore 498771", Category = "Logistics", IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new Vendor { Name = "Innovate Consulting Group", Code = "DEMO-ICG-001", ContactPerson = "Priya Menon", Email = "engage@innovateconsult.sg", Phone = "+65 6233 4455", Address = "78 Shenton Way, #20-01, Singapore 079120", Category = "Consulting", IsActive = true, CreatedBy = "seeder", CreatedOn = now }
        );
        await context.SaveChangesAsync();
    }

    private static async Task SeedExtraCatalogItemsAsync(MainDbContext context)
    {
        var cleanMax = await context.Vendors.FirstOrDefaultAsync(v => v.Code == DemoVendorMarkerCode);
        var logiTrans = await context.Vendors.FirstOrDefaultAsync(v => v.Code == "DEMO-LGX-001");
        var consulting = await context.Vendors.FirstOrDefaultAsync(v => v.Code == "DEMO-ICG-001");
        if (cleanMax is null || logiTrans is null || consulting is null) return;

        if (await context.CatalogItems.AnyAsync(c => c.VendorId == cleanMax.Id)) return;

        var now = DateTimeHelper.Now;
        context.CatalogItems.AddRange(
            new CatalogItem { Name = "Daily Office Cleaning (per visit)", Sku = "CLN-DAILY", Description = "Daily office cleaning service, 3 hours per visit", Category = "Cleaning", UnitOfMeasure = "Each", UnitPrice = 220.00m, VendorId = cleanMax.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new CatalogItem { Name = "Deep Clean Quarterly Package", Sku = "CLN-DEEP-Q", Description = "Quarterly deep-clean package incl. carpet shampoo", Category = "Cleaning", UnitOfMeasure = "Set", UnitPrice = 1200.00m, VendorId = cleanMax.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new CatalogItem { Name = "Same-day Courier (within Singapore)", Sku = "LGX-SDC-SG", Description = "Door-to-door same-day courier delivery within Singapore", Category = "Hardware", UnitOfMeasure = "Each", UnitPrice = 45.00m, VendorId = logiTrans.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new CatalogItem { Name = "Pallet Delivery (per pallet)", Sku = "LGX-PALLET", Description = "Pallet handling and delivery, up to 1 tonne", Category = "Hardware", UnitOfMeasure = "Each", UnitPrice = 180.00m, VendorId = logiTrans.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new CatalogItem { Name = "Strategy Workshop (half-day)", Sku = "ICG-WS-HD", Description = "Facilitated half-day strategy workshop for up to 15 attendees", Category = "Software", UnitOfMeasure = "Each", UnitPrice = 4500.00m, VendorId = consulting.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now },
            new CatalogItem { Name = "Consulting Engagement (per day)", Sku = "ICG-ENG-DAY", Description = "Senior consultant engagement, per consultant-day", Category = "Software", UnitOfMeasure = "Hour", UnitPrice = 2400.00m, VendorId = consulting.Id, IsActive = true, CreatedBy = "seeder", CreatedOn = now }
        );
        await context.SaveChangesAsync();
    }

    private static async Task SeedExtraPurchaseOrdersAsync(MainDbContext context)
    {
        if (await context.PurchaseOrders.AnyAsync(po => po.PoNumber.StartsWith(DemoPoNumberPrefix))) return;

        var vendors = await context.Vendors.ToListAsync();
        var catalogItems = await context.CatalogItems.ToListAsync();
        if (vendors.Count == 0 || catalogItems.Count == 0) return;

        var now = DateTimeHelper.Now;
        // Status mix tuned so each report section has rows:
        //   ~50% Approved (drives vendor-analysis + spending-by-dept)
        //   ~15% Rejected (drives po-summary status breakdown)
        //   remainder spread across Draft / Submitted / Pending* / Cancelled
        var statusPlan = new[]
        {
            EPurchaseOrderStatus.Approved, EPurchaseOrderStatus.Approved, EPurchaseOrderStatus.Approved,
            EPurchaseOrderStatus.Approved, EPurchaseOrderStatus.Approved, EPurchaseOrderStatus.Approved,
            EPurchaseOrderStatus.Approved, EPurchaseOrderStatus.Approved, EPurchaseOrderStatus.Approved,
            EPurchaseOrderStatus.Approved, EPurchaseOrderStatus.Approved, EPurchaseOrderStatus.Approved,
            EPurchaseOrderStatus.Rejected, EPurchaseOrderStatus.Rejected, EPurchaseOrderStatus.Rejected,
            EPurchaseOrderStatus.PendingManagerApproval, EPurchaseOrderStatus.PendingManagerApproval,
            EPurchaseOrderStatus.PendingFinanceApproval, EPurchaseOrderStatus.PendingFinanceApproval,
            EPurchaseOrderStatus.PendingProcurementApproval,
            EPurchaseOrderStatus.Submitted, EPurchaseOrderStatus.Submitted,
            EPurchaseOrderStatus.Draft,
            EPurchaseOrderStatus.Cancelled, EPurchaseOrderStatus.Cancelled
        };

        // Deterministic randomness so re-running gives the same data.
        var rng = new Random(20260524);
        var deliveryLocations = new[] { "NIE Block 1", "NIE Block 2", "NIE Block 7", "NIE Library" };
        var poList = new List<PurchaseOrder>();

        for (var i = 0; i < statusPlan.Length; i++)
        {
            var status = statusPlan[i];
            var (userId, userName) = DemoUsers[i % DemoUsers.Length];
            var vendor = vendors[i % vendors.Count];
            // Spread request dates across the last 60 days so the default
            // "this month" date filter still includes most of them, and the
            // "last 60 days" tail provides longitudinal data for charts.
            var requestDate = now.AddDays(-(i * 2 + 1));

            var po = new PurchaseOrder
            {
                PoNumber = $"{DemoPoNumberPrefix}{(i + 1):D4}",
                RequestedBy = userId,
                RequestedByName = userName,
                RequestDate = requestDate,
                DeliveryAddress = deliveryLocations[i % deliveryLocations.Length],
                ExpectedDeliveryDate = requestDate.AddDays(7 + (i % 14)),
                Status = status,
                Notes = $"Demo PO #{i + 1} — {status} bucket for report showcase",
                RejectionReason = status == EPurchaseOrderStatus.Rejected
                    ? "Budget cap reached for this quarter."
                    : null,
                VendorId = vendor.Id,
                TotalAmount = 0m,
                CreatedBy = userId,
                CreatedOn = requestDate
            };
            poList.Add(po);
        }

        context.PurchaseOrders.AddRange(poList);
        await context.SaveChangesAsync();

        // Add 1-3 lines per PO using items belonging to the PO's vendor when
        // available (so the catalog/vendor linkage is consistent).
        var nowSave = DateTimeHelper.Now;
        foreach (var po in poList)
        {
            var vendorItems = catalogItems.Where(c => c.VendorId == po.VendorId).ToList();
            var pool = vendorItems.Count > 0 ? vendorItems : catalogItems;
            var lineCount = 1 + rng.Next(3); // 1..3 lines

            decimal total = 0m;
            for (var lineIndex = 0; lineIndex < lineCount; lineIndex++)
            {
                var item = pool[rng.Next(pool.Count)];
                var quantity = 1 + rng.Next(8); // 1..8
                var lineTotal = item.UnitPrice * quantity;
                total += lineTotal;

                context.PurchaseOrderLines.Add(new PurchaseOrderLine
                {
                    PurchaseOrderId = po.Id,
                    LineNumber = lineIndex + 1,
                    ItemName = item.Name,
                    Description = item.Description,
                    UnitOfMeasure = item.UnitOfMeasure,
                    Quantity = quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = lineTotal,
                    CatalogItemId = item.Id,
                    CreatedBy = "seeder",
                    CreatedOn = nowSave
                });
            }

            po.TotalAmount = total;
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Adds approval records for every PO that doesn't yet have any. Walks
    /// the Manager → Finance → Procurement chain with realistic action dates
    /// so the approval-timeline report shows entries per stage.
    /// </summary>
    private static async Task SeedPurchaseOrderApprovalsAsync(MainDbContext context)
    {
        // Statuses that should have approval history attached.
        var statusesWithApprovals = new[]
        {
            EPurchaseOrderStatus.PendingManagerApproval,
            EPurchaseOrderStatus.PendingFinanceApproval,
            EPurchaseOrderStatus.PendingProcurementApproval,
            EPurchaseOrderStatus.Approved,
            EPurchaseOrderStatus.Rejected
        };

        var posNeedingApprovals = await context.PurchaseOrders
            .Where(po => statusesWithApprovals.Contains(po.Status) && !po.Approvals.Any())
            .ToListAsync();

        if (posNeedingApprovals.Count == 0) return;

        // Realistic stage approvers per stage.
        var managerApprovers = new[] { ("kamaludemy", "Kamaludeen Yousuf"), ("johnyeo", "John Yeo") };
        var financeApprovers = new[] { ("sarahcheng", "Sarah Cheng") };
        var procurementApprovers = new[] { ("michaeltay", "Michael Tay") };
        var rng = new Random(20260524);

        foreach (var po in posNeedingApprovals)
        {
            var baseDate = po.RequestDate.AddDays(1);
            var stages = new List<(EApprovalStage Stage, EApprovalAction? Action, DateTime? When, string Approver, string ApproverName, string? Comments)>();

            // Manager stage (always reached for any non-Draft).
            var managerApprover = managerApprovers[rng.Next(managerApprovers.Length)];
            switch (po.Status)
            {
                case EPurchaseOrderStatus.PendingManagerApproval:
                    // Manager hasn't acted yet — no action recorded.
                    stages.Add((EApprovalStage.Manager, null, null, managerApprover.Item1, managerApprover.Item2, null));
                    break;
                case EPurchaseOrderStatus.Rejected:
                    stages.Add((EApprovalStage.Manager, EApprovalAction.Reject, baseDate.AddHours(rng.Next(20)),
                        managerApprover.Item1, managerApprover.Item2,
                        "Cost above the approved budget envelope. Resubmit next quarter."));
                    break;
                default:
                    stages.Add((EApprovalStage.Manager, EApprovalAction.Approve, baseDate.AddHours(rng.Next(20)),
                        managerApprover.Item1, managerApprover.Item2, "Approved on behalf of the academic group."));
                    break;
            }

            // Finance stage.
            if (po.Status is EPurchaseOrderStatus.PendingFinanceApproval
                or EPurchaseOrderStatus.PendingProcurementApproval
                or EPurchaseOrderStatus.Approved)
            {
                var financeApprover = financeApprovers[rng.Next(financeApprovers.Length)];
                if (po.Status == EPurchaseOrderStatus.PendingFinanceApproval)
                {
                    stages.Add((EApprovalStage.Finance, null, null, financeApprover.Item1, financeApprover.Item2, null));
                }
                else
                {
                    stages.Add((EApprovalStage.Finance, EApprovalAction.Approve,
                        baseDate.AddDays(1).AddHours(rng.Next(20)),
                        financeApprover.Item1, financeApprover.Item2,
                        "Within department budget. OK to proceed."));
                }
            }

            // Procurement stage.
            if (po.Status is EPurchaseOrderStatus.PendingProcurementApproval
                or EPurchaseOrderStatus.Approved)
            {
                var procurementApprover = procurementApprovers[rng.Next(procurementApprovers.Length)];
                if (po.Status == EPurchaseOrderStatus.PendingProcurementApproval)
                {
                    stages.Add((EApprovalStage.Procurement, null, null, procurementApprover.Item1, procurementApprover.Item2, null));
                }
                else
                {
                    stages.Add((EApprovalStage.Procurement, EApprovalAction.Approve,
                        baseDate.AddDays(2).AddHours(rng.Next(20)),
                        procurementApprover.Item1, procurementApprover.Item2,
                        "Procurement signed off. PO will be issued."));
                }
            }

            for (var i = 0; i < stages.Count; i++)
            {
                var stage = stages[i];
                context.PurchaseOrderApprovals.Add(new PurchaseOrderApproval
                {
                    PurchaseOrderId = po.Id,
                    ApprovalStage = stage.Stage,
                    StageOrder = i + 1,
                    ApproverId = stage.Approver,
                    ApproverName = stage.ApproverName,
                    Action = stage.Action,
                    ActionDate = stage.When,
                    Comments = stage.Comments,
                    CreatedBy = "seeder",
                    CreatedOn = po.RequestDate
                });
            }
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds 200 explicit audit log entries spread across the 6 demo users,
    /// multiple categories, and 60 days — so the audit-trail and user-activity
    /// reports show meaningful breakdowns (not just the implicit
    /// auto-generated "Data Create" rows with null UserId).
    /// </summary>
    private static async Task SeedDemoAuditLogsAsync(MainDbContext context)
    {
        if (await context.AuditLogs.AnyAsync(log => log.CorrelationId == DemoAuditCorrelationId)) return;

        var now = DateTimeHelper.Now;
        var rng = new Random(20260524);

        // Each scenario maps to a category + action set; we sample uniformly
        // so all category-filtered slices of the audit-trail report have rows.
        var scenarios = new[]
        {
            (EAuditCategory.Authentication, new[] { EAuditAction.Login, EAuditAction.Logout, EAuditAction.SessionRefreshed }, new[] { "Authentication" }),
            (EAuditCategory.Authentication, new[] { EAuditAction.FailedLogin }, new[] { "Authentication" }),
            (EAuditCategory.AccessControl, new[] { EAuditAction.RoleAssigned, EAuditAction.RoleRemoved, EAuditAction.PermissionGranted, EAuditAction.PermissionRevoked }, new[] { "Role", "UserRole", "AccessFunction" }),
            (EAuditCategory.Data,          new[] { EAuditAction.Create, EAuditAction.Update, EAuditAction.Delete }, new[] { "PurchaseOrder", "Vendor", "CatalogItem" }),
            (EAuditCategory.FileOperation, new[] { EAuditAction.Create, EAuditAction.Delete }, new[] { "PurchaseOrderDocument", "Document" }),
            (EAuditCategory.DataTransfer,  new[] { EAuditAction.Read, EAuditAction.BulkCreate }, new[] { "Report", "AuditLog" }),
            (EAuditCategory.System,        new[] { EAuditAction.Create, EAuditAction.Update }, new[] { "WorkflowTransition", "ChatConversation" })
        };

        var routes = new[] { "/api/PurchaseOrders", "/api/Vendor", "/api/Auth/Login", "/api/Report/preview", "/api/AccessFunctions" };
        var methods = new[] { "GET", "POST", "PUT", "DELETE" };
        var outcomes = new[] { "Success", "Success", "Success", "Success", "Failure" }; // ~80% success

        for (var i = 0; i < 200; i++)
        {
            var (userId, userName) = DemoUsers[i % DemoUsers.Length];
            var scenario = scenarios[rng.Next(scenarios.Length)];
            var action = scenario.Item2[rng.Next(scenario.Item2.Length)];
            var entity = scenario.Item3[rng.Next(scenario.Item3.Length)];
            // Spread timestamps across the last 60 days, with denser activity
            // in the most recent week so the report's "today" / "this week"
            // filters show variation.
            var hoursAgo = rng.NextDouble() < 0.4
                ? rng.Next(0, 24 * 7)
                : rng.Next(24 * 7, 24 * 60);
            var timestamp = now.AddHours(-hoursAgo);

            context.AuditLogs.Add(new AuditLog
            {
                EntityName = entity,
                EntityId = (1000 + rng.Next(9000)).ToString(SingaporeFormatCulture),
                Action = action,
                Category = scenario.Item1,
                Severity = action is EAuditAction.FailedLogin or EAuditAction.AccessDenied
                    ? EAuditSeverity.Warning
                    : EAuditSeverity.Info,
                UserId = userId,
                UserName = userName,
                Timestamp = timestamp,
                IpAddress = $"10.0.{rng.Next(0, 255)}.{rng.Next(1, 255)}",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Demo-Seeder/1.0",
                CorrelationId = DemoAuditCorrelationId,
                SessionId = $"sess-{userId}-{i}",
                RequestMethod = methods[rng.Next(methods.Length)],
                RequestUrl = routes[rng.Next(routes.Length)],
                DurationMs = rng.Next(5, 850),
                Outcome = outcomes[rng.Next(outcomes.Length)]
            });
        }

        await context.SaveChangesAsync();
    }

    // Singapore culture for formatting numeric ids in audit log entries —
    // matches the report renderer's culture so values look consistent.
    private static readonly System.Globalization.CultureInfo SingaporeFormatCulture =
        System.Globalization.CultureInfo.GetCultureInfo("en-SG");
}
