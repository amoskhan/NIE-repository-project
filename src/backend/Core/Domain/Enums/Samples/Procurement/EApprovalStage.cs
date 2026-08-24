namespace Domain.Enums;

/// <summary>
/// Approval stages for procurement purchase orders. The integer ordinal is used as
/// <c>PurchaseOrderApproval.StageOrder</c>; do NOT renumber existing values once they
/// reach production data.
///
/// String form (via <c>enum.ToString()</c>) is what's persisted in the database column
/// thanks to <c>HasConversion&lt;string&gt;()</c> in <c>MainDbContext.OnModelCreating</c>.
/// Mirror to the frontend in <c>src/frontend/apps/main/src/types/procurement.ts</c> with
/// matching string values.
/// </summary>
public enum EApprovalStage
{
    Manager = 1,
    Finance = 2,
    Procurement = 3
}
