/**
 * Role DTO - matches backend ERole enum
 */
export enum ERole {
  Administrator = 1,
  User = 2,
  Manager = 3,
  Viewer = 4,
}

/**
 * Role display names
 */
export const RoleNames: Record<ERole, string> = {
  [ERole.Administrator]: "Administrator",
  [ERole.User]: "User",
  [ERole.Manager]: "Manager",
  [ERole.Viewer]: "Viewer",
};

export interface AccessFunction {
  id: number;
  code: string;
  name: string;
  description?: string | null;
  module: string;
  type: number;
  resourceName: string;
  route?: string | null;
  httpMethod?: string | null;
  isActive: boolean;
  isSystemFunction: boolean;
  displayOrder: number;
}

export interface Role {
  id: number;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  isSystemRole: boolean;
  displayOrder: number;
  assignedUserCount: number;
  accessFunctions: AccessFunction[];
  accessFunctionIds: number[];
}

export interface UserRoleAssignment {
  id: number;
  userId: string;
  roleId: number;
  roleCode: string;
  roleName: string;
  assignedOn: string;
  assignedBy?: string | null;
  expiresOn?: string | null;
  isActive: boolean;
}

export interface CurrentAccessProfile {
  userId: string;
  roleCodes: string[];
  roleNames: string[];
  accessFunctionCodes: string[];
}
