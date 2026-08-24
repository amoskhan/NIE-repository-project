import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import NiePagination from "../../pagination/NiePagination.vue";

type PaginationProps = InstanceType<typeof NiePagination>["$props"];

function mountPagination(props: Partial<PaginationProps> = {}) {
  return mount(NiePagination, {
    props: { currentPage: 2, totalPages: 5, ...props } as PaginationProps,
  });
}

function navButton(wrapper: ReturnType<typeof mountPagination>, label: string) {
  return wrapper.get(`button[aria-label="${label}"]`);
}

describe("NiePagination navigation", () => {
  it("labels every navigation control", () => {
    const wrapper = mountPagination();

    for (const label of [
      "First page",
      "Previous page",
      "Next page",
      "Last page",
    ]) {
      expect(wrapper.find(`button[aria-label="${label}"]`).exists()).toBe(true);
    }
    expect(wrapper.get("nav").attributes("aria-label")).toBe("Table pagination");
  });

  it("marks the current page for assistive technology", () => {
    const wrapper = mountPagination({ currentPage: 3, totalPages: 7 });
    const current = wrapper.get("[data-pagination-current-page]");

    expect(current.attributes("aria-current")).toBe("page");
    expect(current.attributes("aria-label")).toBe("Page 3 of 7");
    expect(current.text()).toBe("3");
  });

  it("reports at least one page even when the total is zero", () => {
    const wrapper = mountPagination({ currentPage: 1, totalPages: 0 });

    expect(
      wrapper.get("[data-pagination-current-page]").attributes("aria-label"),
    ).toBe("Page 1 of 1");
  });

  it("emits both page events when moving", async () => {
    const wrapper = mountPagination();

    await navButton(wrapper, "Next page").trigger("click");

    expect(wrapper.emitted("update:currentPage")).toEqual([[3]]);
    expect(wrapper.emitted("page-change")).toEqual([[3]]);
  });

  it("jumps to the first and last page", async () => {
    const wrapper = mountPagination({ currentPage: 3, totalPages: 9 });

    await navButton(wrapper, "First page").trigger("click");
    await navButton(wrapper, "Last page").trigger("click");

    expect(wrapper.emitted("update:currentPage")).toEqual([[1], [9]]);
  });

  it("disables backwards navigation on the first page", async () => {
    const wrapper = mountPagination({ currentPage: 1, totalPages: 5 });

    expect(navButton(wrapper, "First page").attributes("disabled")).toBeDefined();
    expect(
      navButton(wrapper, "Previous page").attributes("disabled"),
    ).toBeDefined();
    expect(navButton(wrapper, "Next page").attributes("disabled")).toBeUndefined();
  });

  it("disables forward navigation on the last page", () => {
    const wrapper = mountPagination({ currentPage: 5, totalPages: 5 });

    expect(navButton(wrapper, "Next page").attributes("disabled")).toBeDefined();
    expect(navButton(wrapper, "Last page").attributes("disabled")).toBeDefined();
  });

  it("never emits a page outside the range", async () => {
    const first = mountPagination({ currentPage: 1, totalPages: 3 });
    await navButton(first, "Previous page").trigger("click");
    expect(first.emitted("update:currentPage")).toBeUndefined();

    const last = mountPagination({ currentPage: 3, totalPages: 3 });
    await navButton(last, "Next page").trigger("click");
    expect(last.emitted("update:currentPage")).toBeUndefined();
  });

  it("does not re-emit the page the user is already on", async () => {
    const wrapper = mountPagination({ currentPage: 1, totalPages: 3 });

    await navButton(wrapper, "First page").trigger("click");

    expect(wrapper.emitted("update:currentPage")).toBeUndefined();
  });
});

describe("NiePagination page size", () => {
  it("offers the default page sizes", () => {
    const wrapper = mountPagination();
    const select = wrapper.get('[data-testid="nie-page-size-select"]');

    expect(select.attributes("aria-label")).toBe("Rows per page");
    expect(select.findAll("option").map((option) => option.text())).toEqual([
      "10",
      "20",
      "50",
      "100",
    ]);
  });

  it("sorts, de-duplicates and rejects out-of-range options", () => {
    const wrapper = mountPagination({
      pageSizeOptions: [50, 10, 10, 0, -5, 250, 20.5, 20],
    });

    expect(
      wrapper.findAll("option").map((option) => option.text()),
    ).toEqual(["10", "20", "50"]);
  });

  it("shows the active page size", () => {
    const wrapper = mountPagination({ itemsPerPage: 50 });

    expect(
      (wrapper.get("select").element as HTMLSelectElement).value,
    ).toBe("50");
  });

  it("emits the newly chosen page size", async () => {
    const wrapper = mountPagination();

    await wrapper.get("select").setValue("50");

    expect(wrapper.emitted("update:itemsPerPage")).toEqual([[50]]);
  });

  it("ignores a page size that is not on offer", async () => {
    const wrapper = mountPagination({ pageSizeOptions: [10, 20] });
    const select = wrapper.get("select").element as HTMLSelectElement;

    select.value = "999";
    await wrapper.get("select").trigger("change");

    expect(wrapper.emitted("update:itemsPerPage")).toBeUndefined();
  });

  it("can be hidden entirely", () => {
    const wrapper = mountPagination({ showPageSizeSelector: false });

    expect(wrapper.find("[data-pagination-page-size]").exists()).toBe(false);
    expect(wrapper.find("select").exists()).toBe(false);
  });
});
