import { describe, expect, it, vi } from "vitest";
import { useServerDataTable } from "@/composables/data-tables/useServerDataTable";

describe("useServerDataTable", () => {
  it("loads one server page and one independently paged filter facet", async () => {
    const search = vi.fn().mockResolvedValue({
      items: [{ id: "1", name: "Alpha" }],
      totalCount: 64,
      page: 1,
      pageSize: 25,
      totalPages: 3,
    });
    const getFilterOptions = vi.fn().mockResolvedValue({
      items: [{ label: "Active", value: "Active", count: 40 }],
      totalCount: 4,
      page: 1,
      pageSize: 25,
      totalPages: 1,
    });
    const table = useServerDataTable({ search, getFilterOptions });

    await table.load({
      page: 1,
      pageSize: 25,
      search: "alpha",
      sortBy: "name",
      sortDirection: "asc",
      filters: { status: ["Active"] },
    });
    await table.loadFilterOptions({
      columnKey: "status",
      page: 1,
      pageSize: 25,
      search: "act",
      tableSearch: "alpha",
      filters: { status: ["Active"] },
    });

    expect(table.rows.value).toEqual([{ id: "1", name: "Alpha" }]);
    expect(table.totalItems.value).toBe(64);
    expect(table.filterOptionPages.value.status?.items).toHaveLength(1);
    expect(search).toHaveBeenCalledOnce();
    expect(getFilterOptions).toHaveBeenCalledOnce();
  });

  it("does not let a slower obsolete response replace a later query", async () => {
    let resolveFirst: ((value: unknown) => void) | undefined;
    const first = new Promise((resolve) => {
      resolveFirst = resolve;
    });
    const search = vi
      .fn()
      .mockReturnValueOnce(first)
      .mockResolvedValueOnce({
        items: [{ id: "2", name: "Latest" }],
        totalCount: 1,
        page: 1,
        pageSize: 25,
        totalPages: 1,
      });
    const table = useServerDataTable({
      search,
      getFilterOptions: vi.fn(),
    });
    const query = {
      page: 1,
      pageSize: 25,
      search: "",
      sortBy: null,
      sortDirection: null,
      filters: {},
    } as const;

    const obsoleteLoad = table.load(query);
    await table.load({ ...query, search: "latest" });
    resolveFirst?.({
      items: [{ id: "1", name: "Obsolete" }],
      totalCount: 1,
      page: 1,
      pageSize: 25,
      totalPages: 1,
    });
    await obsoleteLoad;

    expect(table.rows.value).toEqual([{ id: "2", name: "Latest" }]);
  });
});
