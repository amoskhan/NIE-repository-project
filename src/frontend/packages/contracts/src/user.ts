/**
 * User DTO
 */
export interface User {
  id: string;
  email: string;
  name: string;
  roles: string[];
}

/**
 * User with roles
 */
export interface UserWithRoles {
  userId: string;
  userName: string;
  userEmail: string;
  roles: UserRole[];
}

/**
 * User role assignment
 */
export interface UserRole {
  id: string;
  roleId: string;
  roleName: string;
  assignedOn: string;
  expiresOn?: string;
  isActive: boolean;
}
