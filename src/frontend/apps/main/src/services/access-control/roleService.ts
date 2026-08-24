import api from "../core/api";
import type {
  AccessControlOverview,
  ApplicationAccess,
  AssignAccessRequest,
  GlobalRoleAssignment,
  Role,
  SaveRoleRequest,
  StaffDetails,
} from "@/types";
import type {
  NieDataTableFilterOptionPage,
  NieDataTableFilterOptionsRequest,
  NieDataTableQuery,
} from "@nie/ui";
import type { ServerDataTablePage } from "@/composables/data-tables/useServerDataTable";
import {
  toApiDataTableRequest,
  toApiFilterOptionsRequest,
} from "../core/dataTableApi";
import type { UserAccessSummary } from "@/types";

const roleService = {
  async searchUsers(
    query: NieDataTableQuery,
  ): Promise<ServerDataTablePage<UserAccessSummary>> {
    return (
      await api.post<ServerDataTablePage<UserAccessSummary>>(
        "/api/AccessControl/SearchUsers",
        toApiDataTableRequest(query),
      )
    ).data;
  },

  async getUserFilterOptions(
    request: NieDataTableFilterOptionsRequest,
  ): Promise<NieDataTableFilterOptionPage> {
    return (
      await api.post<NieDataTableFilterOptionPage>(
        "/api/AccessControl/GetUserFilterOptions",
        toApiFilterOptionsRequest(request),
      )
    ).data;
  },

  async getOverview(): Promise<AccessControlOverview> {
    return (
      await api.get<AccessControlOverview>("/api/AccessControl/GetOverview")
    ).data;
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

  async deleteRole(id: string): Promise<void> {
    await api.delete(`/api/AccessControl/DeleteRole/${id}`);
  },

  async lookupStaff(email: string): Promise<StaffDetails> {
    return (
      await api.get<StaffDetails>(
        `/api/AccessControl/LookupStaff?email=${encodeURIComponent(email)}`,
      )
    ).data;
  },

  async assignAccess(request: AssignAccessRequest): Promise<{
    globalAssignments: GlobalRoleAssignment[];
    applicationAssignments: ApplicationAccess[];
  }> {
    return (
      await api.post("/api/AccessControl/AssignAccess", {
        userId: request.userId,
        scope: request.scope === "application" ? 2 : 1,
        roleIds: request.roleIds,
        applicationIds:
          request.scope === "application" ? request.applicationIds : [],
        expiresOn: request.expiresOn ?? null,
      })
    ).data as {
      globalAssignments: GlobalRoleAssignment[];
      applicationAssignments: ApplicationAccess[];
    };
  },

  async removeGlobalAssignment(id: string): Promise<void> {
    await api.delete(`/api/AccessControl/RemoveAssignment/${id}`);
  },

  async removeApplicationAssignment(id: string): Promise<void> {
    await api.delete(`/api/AccessControl/RemoveApplicationAccess/${id}`);
  },
};

export default roleService;
