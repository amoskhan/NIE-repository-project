import { mount } from "@vue/test-utils";
import { afterEach, describe, expect, it, vi } from "vitest";
import { nextTick } from "vue";
import NieDataTableDefaultFilterBuilder from "../NieDataTableDefaultFilterBuilder.vue";
import type {
  NieDataTableFilterGroup,
  NieDataTableFilterValue,
} from "../types";

const groups: NieDataTableFilterGroup[] = [
  {
    key: "status",
    label: "Status",
    options: [
      { label: "Open", value: "open", count: 4 },
      { label: "Approved", value: "approved" },
    ],
  },
  {
    key: "owner",
    label: "Owner",
    options: [
      { label: "Ada", value: "ada" },
      { label: "Grace", value: "grace" },
    ],
  },
];

type BuilderProps = InstanceType<
  typeof NieDataTableDefaultFilterBuilder
>["$props"];

function mountBuilder(props: Partial<BuilderProps> = {}) {
  return mount(NieDataTableDefaultFilterBuilder, {
    props: {
      modelValue: {} as Record<string, NieDataTableFilterValue[]>,
      groups,
      ...props,
    } as BuilderProps,
  });
}

function lastRequest(
  wrapper: ReturnType<typeof mountBuilder>,
): unknown[] | undefined {
  const emitted = wrapper.emitted("request-options") ?? [];
  return emitted[emitted.length - 1];
}

afterEach(() => {
  vi.useRealTimers();
});

describe("NieDataTableDefaultFilterBuilder empty state", () => {
  it("explains when no column can be filtered", () => {
    const wrapper = mountBuilder({ groups: [] });

    expect(wrapper.text()).toContain(
      "This table has no columns available for default filtering.",
    );
    expect(wrapper.find("select").exists()).toBe(false);
  });
});

describe("NieDataTableDefaultFilterBuilder column picker", () => {
  it("starts on the first column when nothing is configured", () => {
    const wrapper = mountBuilder();

    expect(
      (wrapper.get("select").element as HTMLSelectElement).value,
    ).toBe("status");
    expect(wrapper.text()).toContain("Status values");
  });

  it("starts on the first column that already has a default filter", () => {
    const wrapper = mountBuilder({ modelValue: { owner: ["ada"] } });

    expect(
      (wrapper.get("select").element as HTMLSelectElement).value,
    ).toBe("owner");
  });

  it("offers every configured group", () => {
    const wrapper = mountBuilder();

    expect(wrapper.findAll("option").map((option) => option.text())).toEqual([
      "Status",
      "Owner",
    ]);
  });

  it("switches columns from the picker", async () => {
    const wrapper = mountBuilder();

    await wrapper.get("select").setValue("owner");

    expect(wrapper.text()).toContain("Owner values");
    expect(
      wrapper
        .findAll("[data-default-filter-value]")
        .map((node) => node.attributes("data-default-filter-value")),
    ).toEqual(["ada", "grace"]);
  });

  it("falls back to the first column when the current one disappears", async () => {
    const wrapper = mountBuilder({ modelValue: { owner: ["ada"] } });
    expect(
      (wrapper.get("select").element as HTMLSelectElement).value,
    ).toBe("owner");

    await wrapper.setProps({ groups: [groups[0]] });

    expect(
      (wrapper.get("select").element as HTMLSelectElement).value,
    ).toBe("status");
  });
});

describe("NieDataTableDefaultFilterBuilder value selection", () => {
  it("marks selected values as pressed", () => {
    const wrapper = mountBuilder({ modelValue: { status: ["open"] } });

    expect(
      wrapper
        .findAll("[data-default-filter-value]")
        .map((node) => node.attributes("aria-pressed")),
    ).toEqual(["true", "false"]);
    expect(wrapper.text()).toContain("1 selected");
  });

  it("adds a value that was not selected", async () => {
    const wrapper = mountBuilder({ modelValue: { status: ["open"] } });

    await wrapper.findAll("[data-default-filter-value]")[1].trigger("click");

    expect(wrapper.emitted("update:modelValue")).toEqual([
      [{ status: ["open", "approved"] }],
    ]);
  });

  it("removes a value and drops the column once it is empty", async () => {
    const wrapper = mountBuilder({
      modelValue: { status: ["open"], owner: ["ada"] },
    });

    await wrapper.findAll("[data-default-filter-value]")[0].trigger("click");

    expect(wrapper.emitted("update:modelValue")).toEqual([
      [{ owner: ["ada"] }],
    ]);
  });

  it("shows the option counts when the group provides them", () => {
    const wrapper = mountBuilder();

    expect(
      wrapper.findAll("[data-default-filter-value]")[0].text(),
    ).toContain("4");
  });
});

describe("NieDataTableDefaultFilterBuilder applied summary", () => {
  it("is hidden while nothing is configured", () => {
    const wrapper = mountBuilder();

    expect(wrapper.text()).not.toContain("Applied on first load");
  });

  it("summarises each configured column with its selection count", () => {
    const wrapper = mountBuilder({
      modelValue: { status: ["open", "approved"], owner: ["ada"] },
    });

    expect(wrapper.text()).toContain("Applied on first load");
    expect(wrapper.get('[aria-label="Edit Status default filter"]').text()).toBe(
      "Status · 2",
    );
    expect(wrapper.get('[aria-label="Edit Owner default filter"]').text()).toBe(
      "Owner · 1",
    );
  });

  it("jumps to the column being edited", async () => {
    const wrapper = mountBuilder({ modelValue: { owner: ["ada"] } });
    await wrapper.get("select").setValue("status");

    await wrapper.get('[aria-label="Edit Owner default filter"]').trigger("click");

    expect(wrapper.text()).toContain("Owner values");
  });

  it("removes the whole column from its chip", async () => {
    const wrapper = mountBuilder({
      modelValue: { status: ["open", "approved"], owner: ["ada"] },
    });

    await wrapper
      .get('[aria-label="Remove Status default filter"]')
      .trigger("click");

    expect(wrapper.emitted("update:modelValue")).toEqual([
      [{ owner: ["ada"] }],
    ]);
  });
});

describe("NieDataTableDefaultFilterBuilder local search", () => {
  it("narrows the visible values", async () => {
    const wrapper = mountBuilder();

    await wrapper.get('input[type="search"]').setValue("appro");

    expect(
      wrapper
        .findAll("[data-default-filter-value]")
        .map((node) => node.attributes("data-default-filter-value")),
    ).toEqual(["approved"]);
  });

  it("says when nothing matches", async () => {
    const wrapper = mountBuilder();

    await wrapper.get('input[type="search"]').setValue("zzz");

    expect(wrapper.text()).toContain("No values match this search.");
  });

  it("clears the search when the column changes", async () => {
    const wrapper = mountBuilder();
    await wrapper.get('input[type="search"]').setValue("appro");

    await wrapper.get("select").setValue("owner");

    expect(
      (wrapper.get('input[type="search"]').element as HTMLInputElement).value,
    ).toBe("");
    expect(wrapper.findAll("[data-default-filter-value]")).toHaveLength(2);
  });
});

describe("NieDataTableDefaultFilterBuilder remote options", () => {
  it("asks for the first page of the initial column", () => {
    const wrapper = mountBuilder({ remote: true });

    expect(wrapper.emitted("request-options")).toEqual([
      [
        {
          columnKey: "status",
          page: 1,
          pageSize: 25,
          search: "",
          filters: {},
        },
      ],
    ]);
  });

  it("makes no request when it is not in remote mode", () => {
    const wrapper = mountBuilder();

    expect(wrapper.emitted("request-options")).toBeUndefined();
  });

  it("asks again when the column changes, carrying the current filters", async () => {
    const wrapper = mountBuilder({
      remote: true,
      modelValue: { status: ["open"] },
    });

    await wrapper.get("select").setValue("owner");

    expect(lastRequest(wrapper)).toEqual([
      {
        columnKey: "owner",
        page: 1,
        pageSize: 25,
        search: "",
        filters: { status: ["open"] },
      },
    ]);
  });

  it("debounces the remote search", async () => {
    vi.useFakeTimers();
    const wrapper = mountBuilder({ remote: true });

    await wrapper.get('input[type="search"]').setValue("ap");
    await wrapper.get('input[type="search"]').setValue("appro");
    expect(wrapper.emitted("request-options")).toHaveLength(1);

    vi.advanceTimersByTime(250);

    expect(lastRequest(wrapper)).toEqual([
      {
        columnKey: "status",
        page: 1,
        pageSize: 25,
        search: "appro",
        filters: {},
      },
    ]);
  });

  it("does not re-query when clearing the search on a column switch", async () => {
    vi.useFakeTimers();
    const wrapper = mountBuilder({ remote: true });
    await wrapper.get('input[type="search"]').setValue("appro");
    vi.advanceTimersByTime(250);
    const before = wrapper.emitted("request-options")?.length ?? 0;

    await wrapper.get("select").setValue("owner");
    vi.advanceTimersByTime(250);

    // One request for the new column, none for the search reset.
    expect(wrapper.emitted("request-options")).toHaveLength(before + 1);
  });

  it("renders the remote page of values instead of the local ones", () => {
    const wrapper = mountBuilder({
      remote: true,
      optionPages: {
        status: {
          items: [{ label: "Remote value", value: "remote" }],
          page: 1,
          pageSize: 25,
          totalCount: 1,
          totalPages: 1,
        },
      },
    });

    expect(
      wrapper
        .findAll("[data-default-filter-value]")
        .map((node) => node.attributes("data-default-filter-value")),
    ).toEqual(["remote"]);
  });

  it("shows the loading state only while there is nothing to show", async () => {
    const wrapper = mountBuilder({
      remote: true,
      optionPages: {
        status: {
          items: [],
          page: 1,
          pageSize: 25,
          totalCount: 0,
          totalPages: 0,
          loading: true,
        },
      },
    });
    expect(wrapper.get('[role="status"]').text()).toContain("Loading values");

    await wrapper.setProps({
      optionPages: {
        status: {
          items: [{ label: "Remote value", value: "remote" }],
          page: 1,
          pageSize: 25,
          totalCount: 1,
          totalPages: 1,
          loading: true,
        },
      },
    });
    expect(wrapper.find('[role="status"]').exists()).toBe(false);
  });

  it("offers a retry when the values could not be loaded", async () => {
    const wrapper = mountBuilder({
      remote: true,
      optionPages: {
        status: {
          items: [],
          page: 1,
          pageSize: 25,
          totalCount: 0,
          totalPages: 0,
          error: "Server unavailable",
        },
      },
    });
    const before = wrapper.emitted("request-options")?.length ?? 0;

    expect(wrapper.get('[role="alert"]').text()).toContain(
      "Values could not be loaded",
    );
    await wrapper.get('[role="alert"] button').trigger("click");

    expect(wrapper.emitted("request-options")).toHaveLength(before + 1);
  });

  it("pages through the remote values", async () => {
    const wrapper = mountBuilder({
      remote: true,
      optionPages: {
        status: {
          items: [{ label: "Remote value", value: "remote" }],
          page: 2,
          pageSize: 25,
          totalCount: 75,
          totalPages: 3,
        },
      },
    });
    await nextTick();

    for (const label of [
      "First default filter values page",
      "Previous default filter values page",
      "Next default filter values page",
      "Last default filter values page",
    ]) {
      await wrapper.get(`[aria-label="${label}"]`).trigger("click");
    }

    expect(
      wrapper
        .emitted("request-options")
        ?.slice(1)
        .map((call) => (call[0] as { page: number }).page),
    ).toEqual([1, 1, 3, 3]);
  });

  it("hides the pager for a single page", () => {
    const wrapper = mountBuilder({
      remote: true,
      optionPages: {
        status: {
          items: [{ label: "Remote value", value: "remote" }],
          page: 1,
          pageSize: 25,
          totalCount: 1,
          totalPages: 1,
        },
      },
    });

    expect(
      wrapper.find('[aria-label="Default filter value pages"]').exists(),
    ).toBe(false);
  });

  it("disables the pager controls at the ends of the range", () => {
    const wrapper = mountBuilder({
      remote: true,
      optionPages: {
        status: {
          items: [{ label: "Remote value", value: "remote" }],
          page: 1,
          pageSize: 25,
          totalCount: 50,
          totalPages: 2,
        },
      },
    });

    expect(
      wrapper
        .get('[aria-label="First default filter values page"]')
        .attributes("disabled"),
    ).toBeDefined();
    expect(
      wrapper
        .get('[aria-label="Next default filter values page"]')
        .attributes("disabled"),
    ).toBeUndefined();
  });

  it("keeps the search out of the local filter while remote", async () => {
    const wrapper = mountBuilder({
      remote: true,
      optionPages: {
        status: {
          items: [
            { label: "Alpha", value: "alpha" },
            { label: "Beta", value: "beta" },
          ],
          page: 1,
          pageSize: 25,
          totalCount: 2,
          totalPages: 1,
        },
      },
    });

    await wrapper.get('input[type="search"]').setValue("alp");

    expect(wrapper.findAll("[data-default-filter-value]")).toHaveLength(2);
  });
});
