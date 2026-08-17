import api from "./api";
import type {
  AccessFunction,
  Role,
  SaveRoleRequest,
  UserRoleAssignment,
} from "@/types";

/** Wire shape of Shared.Dto.UserRoleDto. */
interface UserRoleResponse {
  id: number;
  userId: string;
  roleId: number;
  roleCode: string;
  roleName: string;
  isActive: boolean;
}

interface AccessControlOverview {
  users: {
    userId: string;
    assignments: UserRoleResponse[];
    accessFunctionCodes: string[];
  }[];
  roles: Role[];
  accessFunctions: AccessFunction[];
}

/** Wire shape of Shared.Dto.CurrentAccessProfileDto. */
interface CurrentAccessProfile {
  userId: string;
  roleCodes: string[];
  roleNames: string[];
  accessFunctionCodes: string[];
}

/** The subset of UserAccountDto this service needs to join assignments onto accounts. */
interface UserAccountIdentity {
  id: number;
  userId: string;
}

const roleService = {
  async getAllAccessFunctions(): Promise<AccessFunction[]> {
    const overview = (
      await api.get<AccessControlOverview>("/api/AccessControl/GetOverview")
    ).data;
    return overview.accessFunctions;
  },

  async getAccessFunctionsByModule(module: string): Promise<AccessFunction[]> {
    const all = await this.getAllAccessFunctions();
    return all.filter((af) => af.module === module);
  },

  async getAllRoles(): Promise<Role[]> {
    const overview = (
      await api.get<AccessControlOverview>("/api/AccessControl/GetOverview")
    ).data;
    return overview.roles;
  },

  async getAllRolesWithAccessFunctions(): Promise<Role[]> {
    return this.getAllRoles();
  },

  async getRoleById(id: number): Promise<Role> {
    const roles = await this.getAllRoles();
    const role = roles.find((r) => r.id === id);
    if (!role) throw new Error(`Role ${id} not found`);
    return role;
  },

  async saveRole(request: SaveRoleRequest): Promise<Role> {
    if (request.id) {
      return (
        await api.post<Role>("/api/AccessControl/UpdateRole", {
          id: request.id,
          code: request.code,
          name: request.name,
          description: request.description,
          isActive: request.isActive,
          accessFunctionIds: request.accessFunctionIds,
        })
      ).data;
    }
    return (
      await api.post<Role>("/api/AccessControl/CreateRole", {
        code: request.code,
        name: request.name,
        description: request.description,
        isActive: request.isActive,
        accessFunctionIds: request.accessFunctionIds,
      })
    ).data;
  },

  // NOTE: there is deliberately no saveAccessFunction here. Access functions are a
  // code-owned catalogue (Shared/Security/AccessFunctionCatalog.cs) seeded on startup,
  // not runtime-editable data, so AccessControlController exposes no write endpoint for
  // them — roles are what administrators compose at runtime.

  /**
   * Assignments are stored against the login name (UserRole.UserId). `staffUserId` is
   * resolved here by joining that login onto the real UserAccount.Id from
   * GetUserAccounts, so Users.vue can match assignments to the rows it renders. It is
   * null for a login with no account row — never a row index.
   */
  async getAllAssignments(): Promise<UserRoleAssignment[]> {
    const [overview, accounts] = await Promise.all([
      api.get<AccessControlOverview>("/api/AccessControl/GetOverview"),
      api.get<UserAccountIdentity[]>("/api/AccessControl/GetUserAccounts"),
    ]);

    const accountIdByLogin = new Map(
      accounts.data.map((account) => [
        account.userId.toLowerCase(),
        account.id,
      ]),
    );

    return overview.data.users.flatMap((user) =>
      user.assignments.map((a) => {
        const login = a.userId || user.userId;

        return {
          id: a.id,
          userId: login,
          staffUserId: accountIdByLogin.get(login.toLowerCase()) ?? null,
          roleId: a.roleId,
          department: null,
          isActive: a.isActive,
          role: undefined,
          staffUser: undefined,
        };
      }),
    );
  },

  async getAssignmentsByStaffId(
    staffId: number,
  ): Promise<UserRoleAssignment[]> {
    const all = await this.getAllAssignments();
    return all.filter((a) => a.staffUserId === staffId);
  },

  /**
   * GetCurrentAccessProfile is scoped to the signed-in user — there is no per-user
   * variant — so this returns the CURRENT user's access function codes.
   */
  async getCurrentAccessFunctions(): Promise<string[]> {
    const profile = (
      await api.get<CurrentAccessProfile>(
        "/api/AccessControl/GetCurrentAccessProfile",
      )
    ).data;

    return profile.accessFunctionCodes;
  },

  /** AssignRoleDto.UserId is the LOGIN NAME, not a numeric account id. */
  async saveAssignment(assignment: {
    userId: string;
    roleId: number;
  }): Promise<UserRoleAssignment> {
    const result = (
      await api.post<UserRoleResponse>("/api/AccessControl/AssignRole", {
        userId: assignment.userId,
        roleId: assignment.roleId,
      })
    ).data;

    return {
      id: result.id,
      userId: result.userId,
      staffUserId: null,
      roleId: result.roleId,
      department: null,
      isActive: result.isActive,
    };
  },

  async deleteAssignment(
    id: number,
  ): Promise<{ userDeactivated: boolean; staffUserId?: number }> {
    await api.delete(`/api/AccessControl/RemoveAssignment/${id}`);
    return { userDeactivated: false };
  },
};

export default roleService;
