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
  [PurchaseOrderStatus.Draft]: "bg-secondary-100 text-secondary-700",
  [PurchaseOrderStatus.Submitted]: "bg-info-100 text-info-700",
  [PurchaseOrderStatus.PendingManagerApproval]:
    "bg-warning-100 text-warning-700",
  [PurchaseOrderStatus.PendingFinanceApproval]:
    "bg-warning-100 text-warning-700",
  [PurchaseOrderStatus.PendingProcurementApproval]:
    "bg-warning-100 text-warning-700",
  [PurchaseOrderStatus.Approved]: "bg-success-100 text-success-700",
  [PurchaseOrderStatus.Rejected]: "bg-danger-100 text-danger-700",
  [PurchaseOrderStatus.Cancelled]: "bg-secondary-100 text-secondary-700",
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
    : "bg-secondary-100 text-secondary-700";
}
