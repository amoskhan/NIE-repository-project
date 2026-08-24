import type {
  NieDataTableFilterOptionsRequest,
  NieDataTableQuery,
} from "@nie/ui";

interface ApiDataTableFilter {
  key: string;
  values: string[];
}

export interface ApiDataTableRequest {
  page: number;
  pageSize: number;
  search: string;
  sortBy: string | null;
  sortDirection: "asc" | "desc";
  sorts: Array<{ key: string; direction: "asc" | "desc" }>;
  filters: ApiDataTableFilter[];
}

export interface ApiFilterOptionsRequest extends ApiDataTableRequest {
  columnKey: string;
  optionSearch: string;
  optionPage: number;
  optionPageSize: number;
}

function mapFilters(
  filters: NieDataTableQuery["filters"],
): ApiDataTableFilter[] {
  return Object.entries(filters)
    .filter(([, values]) => values.length > 0)
    .map(([key, values]) => ({
      key,
      values: values.map(String),
    }));
}

export function toApiDataTableRequest(
  query: NieDataTableQuery,
): ApiDataTableRequest {
  return {
    page: query.page,
    pageSize: query.pageSize,
    search: query.search,
    sortBy: query.sortBy,
    sortDirection: query.sortDirection ?? "asc",
    sorts: query.sorts.map((sort) => ({ ...sort })),
    filters: mapFilters(query.filters),
  };
}

export function createInitialDataTableQuery(
  pageSize = 20,
): NieDataTableQuery {
  return {
    page: 1,
    pageSize,
    search: "",
    sortBy: null,
    sortDirection: null,
    sorts: [],
    filters: {},
  };
}

export function toApiFilterOptionsRequest(
  request: NieDataTableFilterOptionsRequest,
): ApiFilterOptionsRequest {
  return {
    page: 1,
    pageSize: 25,
    search: request.tableSearch,
    sortBy: null,
    sortDirection: "asc",
    sorts: [],
    filters: mapFilters(request.filters),
    columnKey: request.columnKey,
    optionSearch: request.search,
    optionPage: request.page,
    optionPageSize: request.pageSize,
  };
}
