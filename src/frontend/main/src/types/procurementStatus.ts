export const PurchaseOrderStatus = {
  Draft: "Draft",
  Submitted: "Submitted",
  PendingManagerApproval: "PendingManagerApproval",
  PendingFinanceApproval: "PendingFinanceApproval",
  PendingProcurementApproval: "PendingProcurementApproval",
  Approved: "Approved",
  Rejected: "Rejected",
  Cancelled: "Cancelled",
} as const;

export type PurchaseOrderStatusName =
  (typeof PurchaseOrderStatus)[keyof typeof PurchaseOrderStatus];

const statusLabels: Record<PurchaseOrderStatusName, string> = {
  [PurchaseOrderStatus.Draft]: "Draft",
  [PurchaseOrderStatus.Submitted]: "Submitted",
  [PurchaseOrderStatus.PendingManagerApproval]: "Pending Manager",
  [PurchaseOrderStatus.PendingFinanceApproval]: "Pending Finance",
  [PurchaseOrderStatus.PendingProcurementApproval]: "Pending Procurement",
  [PurchaseOrderStatus.Approved]: "Approved",
  [PurchaseOrderStatus.Rejected]: "Rejected",
  [PurchaseOrderStatus.Cancelled]: "Cancelled",
};

const approvalStageLabels: Partial<Record<PurchaseOrderStatusName, string>> = {
  [PurchaseOrderStatus.PendingManagerApproval]: "Manager Review",
  [PurchaseOrderStatus.PendingFinanceApproval]: "Finance Review",
  [PurchaseOrderStatus.PendingProcurementApproval]: "Procurement Review",
};

const statusClasses: Record<PurchaseOrderStatusName, string> = {
  [PurchaseOrderStatus.Draft]: "bg-slate-100 text-slate-600",
  [PurchaseOrderStatus.Submitted]: "bg-blue-100 text-blue-700",
  [PurchaseOrderStatus.PendingManagerApproval]: "bg-amber-100 text-amber-700",
  [PurchaseOrderStatus.PendingFinanceApproval]: "bg-orange-100 text-orange-700",
  [PurchaseOrderStatus.PendingProcurementApproval]:
    "bg-purple-100 text-purple-700",
  [PurchaseOrderStatus.Approved]: "bg-emerald-100 text-emerald-700",
  [PurchaseOrderStatus.Rejected]: "bg-red-100 text-red-700",
  [PurchaseOrderStatus.Cancelled]: "bg-gray-100 text-gray-600",
};

export function isPurchaseOrderStatus(
  status: string | null | undefined,
): status is PurchaseOrderStatusName {
  return Object.values(PurchaseOrderStatus).includes(
    status as PurchaseOrderStatusName,
  );
}

export function getPurchaseOrderStatusLabel(
  status: string | null | undefined,
): string {
  if (!status) return "-";
  return isPurchaseOrderStatus(status) ? statusLabels[status] : status;
}

export function getPurchaseOrderApprovalStageLabel(
  status: string | null | undefined,
): string {
  if (!status) return "-";
  return isPurchaseOrderStatus(status)
    ? (approvalStageLabels[status] ?? statusLabels[status])
    : status;
}

export function getPurchaseOrderStatusClass(
  status: string | null | undefined,
): string {
  return isPurchaseOrderStatus(status)
    ? statusClasses[status]
    : "bg-slate-100 text-slate-600";
}
