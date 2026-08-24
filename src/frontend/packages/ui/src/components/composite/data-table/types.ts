export type NieDataTableFilterValue = string | number | boolean;
export type NieDataTableSortDirection = "asc" | "desc";
export type NieDataTableChipTone =
  | "default"
  | "primary"
  | "success"
  | "warning"
  | "danger"
  | "info";

export interface NieDataTableChipConfig {
  tone?: NieDataTableChipTone;
  toneMap?: Record<string, NieDataTableChipTone>;
  label?: (value: unknown, row: unknown) => string;
  dot?: boolean;
}

export interface NieDataTableColumnFilterConfig {
  enabled?: boolean;
  label?: string;
  options?: NieDataTableFilterOption[];
  getValue?: (row: unknown) => unknown;
  getLabel?: (value: NieDataTableFilterValue) => string;
}

export interface NieDataTableColumn {
  key: string;
  label: string;
  type?: "text" | "number" | "boolean" | "date" | "custom";
  format?: (value: unknown) => string;
  decimals?: number;
  filter?: boolean | NieDataTableColumnFilterConfig;
  chip?: boolean | NieDataTableChipConfig;
  sortable?: boolean;
  hideable?: boolean;
  persistFilter?: boolean;
}

export interface NieDataTableFilterOption {
  label: string;
  value: NieDataTableFilterValue;
  count?: number;
}

export interface NieDataTableFilterGroup {
  key: string;
  label: string;
  options: NieDataTableFilterOption[];
  getValue?: (row: unknown) => unknown;
}

export interface NieDataTableQuery {
  page: number;
  pageSize: number;
  search: string;
  sortBy: string | null;
  sortDirection: NieDataTableSortDirection | null;
  sorts: NieDataTableSort[];
  filters: Record<string, NieDataTableFilterValue[]>;
}

export interface NieDataTableSort {
  key: string;
  direction: NieDataTableSortDirection;
}

export type NieDataTableDensity = "compact" | "comfortable" | "spacious";
export type NieDataTableAppearance = "elevated" | "minimal" | "striped";

export interface NieDataTablePreferenceSettings {
  pageSize: number;
  sorts: NieDataTableSort[];
  filters: Record<string, NieDataTableFilterValue[]>;
  filterReminderAcknowledgedAtUtc?: string | null;
  columnOrder: string[];
  hiddenColumns: string[];
  density: NieDataTableDensity;
  appearance: NieDataTableAppearance;
}

export interface NieDataTablePreferenceRecord {
  tableKey: string;
  definitionVersion: number;
  revision: number;
  settings: NieDataTablePreferenceSettings;
  repairRequired?: boolean;
  repairReasons?: string[];
}

export interface NieDataTablePreferenceState {
  repairRequired: boolean;
  reasons: string[];
}

export interface NieDataTablePreferenceStore {
  get(tableKey: string): Promise<NieDataTablePreferenceRecord | null>;
  refresh(tableKey: string): Promise<NieDataTablePreferenceRecord | null>;
  save(
    tableKey: string,
    definitionVersion: number,
    settings: NieDataTablePreferenceSettings,
    revision?: number,
  ): Promise<NieDataTablePreferenceRecord>;
  remove(tableKey: string): Promise<void>;
}

export interface NieDataTableFilterOptionPage {
  items: NieDataTableFilterOption[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  loading?: boolean;
  error?: string | null;
}

export interface NieDataTableFilterOptionsRequest {
  columnKey: string;
  page: number;
  pageSize: number;
  search: string;
  tableSearch: string;
  filters: Record<string, NieDataTableFilterValue[]>;
}

export interface NieDataTablePreferenceFilterOptionsRequest {
  columnKey: string;
  page: number;
  pageSize: number;
  search: string;
  filters: Record<string, NieDataTableFilterValue[]>;
}
