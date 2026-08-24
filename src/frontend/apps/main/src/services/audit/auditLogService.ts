import api from "../core/api";
import type {
  AuditLogEntry,
} from "@/types/audit";
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

const auditLogService = {
  async getByEntity(
    entityName: string,
    entityId: string,
  ): Promise<AuditLogEntry[]> {
    return (
      await api.get<AuditLogEntry[]>("/api/AuditLog/GetEntityHistory", {
        params: { entityName, entityId },
      })
    ).data;
  },

  async search(query: NieDataTableQuery): Promise<ServerDataTablePage<AuditLogEntry>> {
    return (
      await api.post<ServerDataTablePage<AuditLogEntry>>(
        "/api/AuditLog/Search",
        toApiDataTableRequest(query),
      )
    ).data;
  },

  async getFilterOptions(
    request: NieDataTableFilterOptionsRequest,
  ): Promise<NieDataTableFilterOptionPage> {
    return (
      await api.post<NieDataTableFilterOptionPage>(
        "/api/AuditLog/GetFilterOptions",
        toApiFilterOptionsRequest(request),
      )
    ).data;
  },
};

export default auditLogService;

