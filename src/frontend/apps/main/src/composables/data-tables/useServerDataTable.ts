import { readonly, shallowRef } from "vue";
import type {
  NieDataTableFilterOptionPage,
  NieDataTableFilterOptionsRequest,
  NieDataTableQuery,
} from "@nie/ui";

export interface ServerDataTablePage<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage?: boolean;
  hasNextPage?: boolean;
}

export interface ServerDataTableSource<T> {
  search(query: NieDataTableQuery): Promise<ServerDataTablePage<T>>;
  getFilterOptions(
    request: NieDataTableFilterOptionsRequest,
  ): Promise<NieDataTableFilterOptionPage>;
}

export function useServerDataTable<T>(source: ServerDataTableSource<T>) {
  const rows = shallowRef<T[]>([]);
  const totalItems = shallowRef(0);
  const loading = shallowRef(false);
  const error = shallowRef<string | null>(null);
  const filterOptionPages = shallowRef<
    Record<string, NieDataTableFilterOptionPage>
  >({});
  let rowRequestSequence = 0;
  const facetRequestSequences = new Map<string, number>();
  let lastQuery: NieDataTableQuery | null = null;

  async function load(query: NieDataTableQuery): Promise<void> {
    const requestSequence = ++rowRequestSequence;
    lastQuery = {
      ...query,
      filters: Object.fromEntries(
        Object.entries(query.filters).map(([key, values]) => [key, [...values]]),
      ),
    };
    loading.value = true;
    error.value = null;

    try {
      const result = await source.search(lastQuery);
      if (requestSequence !== rowRequestSequence) return;
      rows.value = result.items;
      totalItems.value = result.totalCount;
    } catch (reason) {
      if (requestSequence !== rowRequestSequence) return;
      rows.value = [];
      totalItems.value = 0;
      error.value =
        reason instanceof Error ? reason.message : "Table data could not be loaded.";
    } finally {
      if (requestSequence === rowRequestSequence) loading.value = false;
    }
  }

  async function loadFilterOptions(
    request: NieDataTableFilterOptionsRequest,
  ): Promise<void> {
    const requestSequence =
      (facetRequestSequences.get(request.columnKey) ?? 0) + 1;
    facetRequestSequences.set(request.columnKey, requestSequence);
    const previous = filterOptionPages.value[request.columnKey];
    filterOptionPages.value = {
      ...filterOptionPages.value,
      [request.columnKey]: {
        items: previous?.items ?? [],
        page: request.page,
        pageSize: request.pageSize,
        totalCount: previous?.totalCount ?? 0,
        totalPages: previous?.totalPages ?? 0,
        loading: true,
        error: null,
      },
    };

    try {
      const result = await source.getFilterOptions(request);
      if (facetRequestSequences.get(request.columnKey) !== requestSequence) {
        return;
      }
      filterOptionPages.value = {
        ...filterOptionPages.value,
        [request.columnKey]: { ...result, loading: false, error: null },
      };
    } catch (reason) {
      if (facetRequestSequences.get(request.columnKey) !== requestSequence) {
        return;
      }
      filterOptionPages.value = {
        ...filterOptionPages.value,
        [request.columnKey]: {
          ...(filterOptionPages.value[request.columnKey] ?? {
            items: [],
            page: request.page,
            pageSize: request.pageSize,
            totalCount: 0,
            totalPages: 0,
          }),
          loading: false,
          error:
            reason instanceof Error
              ? reason.message
              : "Filter values could not be loaded.",
        },
      };
    }
  }

  async function reload(): Promise<void> {
    if (lastQuery) await load(lastQuery);
  }

  return {
    rows: readonly(rows),
    totalItems: readonly(totalItems),
    loading: readonly(loading),
    error: readonly(error),
    filterOptionPages: readonly(filterOptionPages),
    load,
    loadFilterOptions,
    reload,
  };
}
