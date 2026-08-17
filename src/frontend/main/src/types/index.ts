/**
 * A local account row, projected from UserAccountDto
 * (GET /api/AccessControl/GetUserAccounts).
 *
 * `id` is the real UserAccount primary key — it is what ApproveUser and DeactivateUser
 * take. `username` is UserAccount.UserId, the login name, which is the identity key
 * roles, sessions, and audit logs are written against.
 */
export interface StaffUser {
  id: number;
  username: string;
  email?: string | null;
  fullName?: string | null;
  department?: string | null;
  /** "Active" | "Locked" | "Inactive" — derived from isActive and lockoutEndOn. */
  accountStatus: string;
  /** Mirrors UserAccountDto.IsActive: whether the account may sign in. */
  isApproved: boolean;
  /** Set while an administrator-issued password has not been changed yet. */
  mustChangePassword?: boolean;
  /** When the account is locked out until, or null when it is not locked. */
  lockoutEndOn?: string | null;
  lastLoginAt?: string | null;
}

/**
 * Body of POST /api/AccessControl/RegisterUser (RegisterUserAccountDto).
 *
 * `userId` is the login name and the identity key used everywhere else in the system
 * (UserAccount.UserId, UserRole.UserId, audit logs) — it is NOT a database row id.
 *
 * `initialPassword` is required by the backend: the account is created with
 * MustChangePassword set, so this is a handover value, not a credential to keep.
 */
export interface RegisterStaffRequest {
  userId: string;
  initialPassword: string;
  email?: string | null;
  fullName?: string | null;
  department?: string | null;
}

// Access Function (RBAC)
export interface AccessFunction {
  id: number;
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
  id: number;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  displayOrder: number;
  accessFunctions?: AccessFunction[];
}

export interface SaveRoleRequest {
  id?: number;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  displayOrder: number;
  accessFunctionIds: number[];
}

// User Role Assignment (RBAC)
export interface UserRoleAssignment {
  id: number;
  /**
   * The login name the assignment is stored against (UserRole.UserId). This is the
   * authoritative key — AssignRole takes this, not a numeric id.
   */
  userId: string;
  /**
   * The matching UserAccount.Id, or null when no local account exists for `userId`
   * (a role can be assigned to a login that was never given an account row).
   * Used only to join assignments onto StaffUser rows in the UI.
   */
  staffUserId: number | null;
  roleId: number;
  department?: string | null;
  isActive: boolean;
  role?: Role;
  staffUser?: StaffUser;
}

/**
 * Result of POST /api/AccessControl/DeactivateUser (DeactivateUserResultDto).
 *
 * Deactivating is two writes — the flag in the database and the live sessions in the
 * session store — and the second can fail on its own. `sessionsRevoked` is null when the
 * session store could not be reached, and `warning` then carries a message safe to show.
 */
export interface DeactivateUserResult {
  account: StaffUser;
  sessionsRevoked: number | null;
  warning: string | null;
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
  id: number;
  recipientType: string;
  recipientUserId?: number | null;
  recipientEmail?: string | null;
  recipientName?: string | null;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  readAt?: string | null;
  link?: string | null;
  sourceEntityType?: string | null;
  sourceEntityId?: number | null;
  createdOn: string;
}

// Global Settings
export interface GlobalSettings {
  id: number;
  key: string;
  value: string;
  description?: string | null;
  dataType: string;
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
