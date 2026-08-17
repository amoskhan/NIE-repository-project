# Procurement — Verify (in the template repo)

Use this when you change the template's procurement code and want to confirm the reference sample still
works. In a derived project you would normally be **removing** this sample rather than verifying it —
see [`remove.md`](./remove.md).

All data below is the seeder's fictional demo data.

## Backend

```bash
dotnet build src/backend/AppTemplate.sln
```

## API smoke

```bash
SESSION=$(curl -s -X POST http://localhost:5001/api/Auth/CreateTestSession -H "Content-Type: application/json" -d '{"UserId":"alice"}' | jq -r .sessionToken)
curl -H "X-Session-Id: $SESSION" http://localhost:5002/api/Vendor/GetAll
curl -H "X-Session-Id: $SESSION" http://localhost:5002/api/CatalogItem/GetAll
curl -H "X-Session-Id: $SESSION" http://localhost:5002/api/PurchaseOrder/GetAll
```

All three return `200 OK` with seeded data after `dotnet run --project src/backend/API -- seed`.

## Frontend

1. Start `🚀 All Services (Hot Reload)`.
2. Log in at http://localhost:8001 with a seeded demo account.
3. Visit http://localhost:8002 — Procurement Dashboard renders KPI cards.
4. Open Vendors → New → save → confirm new row appears.
5. Open Catalog → New → save → confirm vendor dropdown populated.
6. Open New Purchase Request → add line items → save as draft.
7. Submit the PO → status transitions to `PendingManagerApproval`.
8. Open Approval Queue → approve through 3 stages → status → `Approved`.

## Audit checks

- `AuditLog` table contains create/update rows for the new vendor, catalog item, and PO.
- `PurchaseOrderApproval` rows have `Action` set after each approval step.

## Permissions

- Confirm `Api.ProcurementOrderRead`, `Api.ProcurementOrderManage`, `Api.ProcurementOrderApprove`, `Api.ProcurementVendorRead`, `Api.ProcurementVendorManage`, `Api.ProcurementCatalogRead`, `Api.ProcurementCatalogManage` exist in `AccessFunctions` table.
- Confirm a Viewer-role user gets `403` from `POST /api/Vendor/Save`.
