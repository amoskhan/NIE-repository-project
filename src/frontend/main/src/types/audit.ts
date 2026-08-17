export interface AuditLogEntry {
  id: number;
  systemName: string;
  entityName: string;
  entityId?: string | null;
  action: string;
  actionDescription?: string | null;
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
