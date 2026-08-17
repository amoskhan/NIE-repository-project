import api from "./api";
import type {
  AuditLogEntry,
  AuditLogSearchParams,
  PaginatedResult,
} from "@/types/audit";

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

  async search(
    params: AuditLogSearchParams,
  ): Promise<PaginatedResult<AuditLogEntry>> {
    return (
      await api.get<PaginatedResult<AuditLogEntry>>(
        "/api/AuditLog/GetAuditLogs",
        {
          params: {
            Keyword: params.search,
            EntityName: params.entityName,
            UserId: params.userId,
            Action: params.action,
            FromDate: params.from,
            ToDate: params.to,
            Page: params.page,
            PageSize: params.pageSize,
          },
        },
      )
    ).data;
  },
};

export default auditLogService;
