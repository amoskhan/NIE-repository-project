// Mirror of Domain.Enum.EApprovalStage — string values match enum.ToString() output.
// Keep in lock-step with src/backend/Core/Domain/Enum/EApprovalStage.cs.
export enum EApprovalStage {
  Manager = "Manager",
  Finance = "Finance",
  Procurement = "Procurement",
}

export const APPROVAL_STAGE_LABELS: Record<EApprovalStage, string> = {
  [EApprovalStage.Manager]: "Manager Approval",
  [EApprovalStage.Finance]: "Finance Approval",
  [EApprovalStage.Procurement]: "Procurement Approval",
};

export function approvalStageLabel(stage: EApprovalStage | string): string {
  return APPROVAL_STAGE_LABELS[stage as EApprovalStage] ?? String(stage);
}
