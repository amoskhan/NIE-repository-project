// Access-control DTOs
// Staff Details from NTU Staff Store API
export interface StaffDetails {
  workerId: string;
  name: string;
  department: string;
  departmentDescription: string;
  userId: string;
  email: string;
  designation: string;
  joiningDate?: string | null;
  title: string;
}

// Access Function (RBAC)
export interface AccessFunction {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  module: string;
  type?: number | string;
  resourceName?: string;
  route?: string | null;
  httpMethod?: string | null;
  isActive: boolean;
  isSystemFunction?: boolean;
  displayOrder: number;
}

// Role (RBAC)
export interface Role {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  isSystemRole?: boolean;
  displayOrder: number;
  assignedUserCount?: number;
  accessFunctions: AccessFunction[];
  accessFunctionIds: string[];
}

export interface SaveRoleRequest {
  id?: string;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  displayOrder?: number;
  accessFunctionIds: string[];
}

export interface GlobalRoleAssignment {
  id: string;
  userId: string;
  roleId: string;
  roleCode: string;
  roleName: string;
  assignedOn: string;
  assignedBy?: string | null;
  expiresOn?: string | null;
  isActive: boolean;
}

export interface ApplicationSummary {
  id: string;
  name: string;
  description?: string | null;
  repository?: string | null;
  branch?: string | null;
  projectKey: string;
  isActive: boolean;
}

export interface ApplicationAccess {
  id: string;
  applicationId: string;
  applicationName: string;
  applicationProjectKey: string;
  userId: string;
  roleId: string;
  roleCode: string;
  roleName: string;
  assignedOn: string;
  assignedBy?: string | null;
  expiresOn?: string | null;
  isActive: boolean;
}

export interface UserAccessSummary {
  userId: string;
  displayName?: string | null;
  email?: string | null;
  department?: string | null;
  departmentDescription?: string | null;
  designation?: string | null;
  title?: string | null;
  profileSource?: string | null;
  assignments: GlobalRoleAssignment[];
  applicationAccesses: ApplicationAccess[];
  accessFunctionCodes: string[];
}

export interface AccessControlOverview {
  users: UserAccessSummary[];
  roles: Role[];
  accessFunctions: AccessFunction[];
  applications: ApplicationSummary[];
}

export interface AssignAccessRequest {
  scope: "global" | "application";
  userId: string;
  roleIds: string[];
  applicationIds: string[];
  expiresOn?: string | null;
}

// Account Status constants (matches backend AccountStatus class)
export const AccountStatus = {
  Unverified: "Unverified",
  Verified: "Verified",
  Locked: "Locked",
  Suspended: "Suspended",
  Active: "Active",
  Inactive: "Inactive",
  PendingApproval: "PendingApproval",
} as const;

// Notification
export interface NotificationItem {
  id: string;
  recipientType: string;
  recipientUserId?: string | null;
  recipientEmail?: string | null;
  recipientName?: string | null;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  readAt?: string | null;
  link?: string | null;
  sourceEntityType?: string | null;
  sourceEntityId?: string | null;
  createdOn: string;
}

// Generic API response
export interface ApiResponse<T> {
  data: T;
  success: boolean;
  message?: string;
}

// Error response
export interface ApiError {
  message: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

// Paged result
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
