export interface AuditLogEntry {
  id: string;
  entityName: string;
  entityId?: string | null;
  action: number;
  actionName: string;
  category: number;
  categoryName: string;
  severity: number;
  severityName: string;
  oldValues?: string | null;
  newValues?: string | null;
  changedProperties?: string | null;
  additionalData?: string | null;
  userId?: string | null;
  userName?: string | null;
  timestamp: string;
  ipAddress?: string | null;
  userAgent?: string | null;
  sessionId?: string | null;
  correlationId?: string | null;
  requestMethod?: string | null;
  requestUrl?: string | null;
  durationMs?: number | null;
  outcome?: string | null;
}

export interface AuditLogSearchParams {
  search?: string;
  entityName?: string | null;
  userId?: string | null;
  action?: string | null;
  from?: string | null;
  to?: string | null;
  page?: number;
  pageSize?: number;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage?: boolean;
  hasNextPage?: boolean;
}
