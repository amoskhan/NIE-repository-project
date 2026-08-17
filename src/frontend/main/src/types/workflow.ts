/** Workflow state enum — mirrors backend Domain.Enum.EWorkflowState */
export enum EWorkflowState {
  Draft = "Draft",
  Submitted = "Submitted",
  UnderReview = "UnderReview",
  Approved = "Approved",
  Rejected = "Rejected",
  Completed = "Completed",
  Cancelled = "Cancelled",
  ReturnedForRevision = "ReturnedForRevision",
}

export interface WorkflowTransition {
  id: number;
  fromState: string;
  toState: string;
  requiredRole: string;
  displayLabel?: string;
  requiresRemarks: boolean;
  isActive: boolean;
  displayOrder: number;
  uiConditions?: string;
}

export interface WorkflowStateLog {
  id: number;
  fromState: string;
  toState: string;
  remarks?: string;
  performedByUserId?: string;
  performedByName?: string;
  performedByRole?: string;
  transitionedAt: string;
  ipAddress?: string;
  ownerType: string;
  ownerId: number;
  notificationSent: boolean;
}

export interface TransitionRequest {
  toState: string;
  remarks?: string;
}
