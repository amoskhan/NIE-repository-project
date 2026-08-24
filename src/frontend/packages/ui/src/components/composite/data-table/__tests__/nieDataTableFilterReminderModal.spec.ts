import { mount } from "@vue/test-utils";
import { afterEach, describe, expect, it } from "vitest";
import { nextTick } from "vue";
import NieDataTableFilterReminderModal from "../NieDataTableFilterReminderModal.vue";

type ReminderProps = InstanceType<
  typeof NieDataTableFilterReminderModal
>["$props"];

const filters = [
  { key: "status", label: "Status", count: 2 },
  { key: "owner", label: "Owner", count: 1 },
];

function mountReminder(props: Partial<ReminderProps> = {}) {
  return mount(NieDataTableFilterReminderModal, {
    attachTo: document.body,
    props: { modelValue: true, filters, ...props } as ReminderProps,
  });
}

function actionButton(label: string): HTMLButtonElement | null {
  return document.querySelector<HTMLButtonElement>(`[aria-label="${label}"]`);
}

afterEach(() => {
  document.body.innerHTML = "";
  document.body.style.overflow = "";
});

describe("NieDataTableFilterReminderModal visibility", () => {
  it("renders nothing while closed", () => {
    const wrapper = mountReminder({ modelValue: false });

    expect(document.querySelector("[data-table-filter-reminder]")).toBeNull();
    wrapper.unmount();
  });

  it("opens as a labelled dialog the user cannot dismiss by accident", async () => {
    const wrapper = mountReminder();
    await nextTick();

    const dialog = document.querySelector('[role="dialog"]');
    expect(dialog?.getAttribute("aria-label")).toBe("Review saved table filters");
    expect(document.querySelector('[aria-label="Close dialog"]')).toBeNull();
    wrapper.unmount();
  });

  it("ignores Escape so the user has to make a choice", async () => {
    const wrapper = mountReminder();

    document.dispatchEvent(
      new KeyboardEvent("keydown", { key: "Escape", cancelable: true }),
    );
    await nextTick();

    expect(document.querySelector("[data-table-filter-reminder]")).not.toBeNull();
    wrapper.unmount();
  });

  it("puts initial focus on the non-destructive choice", async () => {
    const wrapper = mountReminder();
    await nextTick();
    await nextTick();

    expect(document.activeElement?.getAttribute("aria-label")).toBe(
      "Keep saved filters for another week",
    );
    wrapper.unmount();
  });
});

describe("NieDataTableFilterReminderModal content", () => {
  it("lists every active filter with its selection count", () => {
    const wrapper = mountReminder();

    const chips = [
      ...document.querySelectorAll('[aria-label="Active saved filters"] li'),
    ];
    expect(chips.map((chip) => chip.textContent?.trim())).toEqual([
      "Status (2)",
      "Owner (1)",
    ]);
    wrapper.unmount();
  });

  it("explains why records may be missing", () => {
    const wrapper = mountReminder();

    expect(document.body.textContent).toContain("Saved filters are active");
    expect(document.body.textContent).toContain(
      "Some records may be hidden from this view.",
    );
    wrapper.unmount();
  });

  it("shows no error banner by default", () => {
    const wrapper = mountReminder();

    expect(document.querySelector('[role="alert"]')).toBeNull();
    wrapper.unmount();
  });

  it("surfaces a save failure as an alert", () => {
    const wrapper = mountReminder({ error: "Could not save your choice." });

    expect(document.querySelector('[role="alert"]')?.textContent).toContain(
      "Could not save your choice.",
    );
    wrapper.unmount();
  });
});

describe("NieDataTableFilterReminderModal choices", () => {
  it("emits keep from the keep button", async () => {
    const wrapper = mountReminder();

    actionButton("Keep saved filters for another week")?.click();
    await nextTick();

    expect(wrapper.emitted("keep")).toHaveLength(1);
    expect(wrapper.emitted("remove")).toBeUndefined();
    wrapper.unmount();
  });

  it("emits remove from the destructive button", async () => {
    const wrapper = mountReminder();

    actionButton("Remove saved default filters")?.click();
    await nextTick();

    expect(wrapper.emitted("remove")).toHaveLength(1);
    wrapper.unmount();
  });

  it("locks both choices and shows progress while saving", () => {
    const wrapper = mountReminder({ saving: true });

    const keep = actionButton("Keep saved filters for another week");
    expect(actionButton("Remove saved default filters")?.disabled).toBe(true);
    expect(keep?.disabled).toBe(true);
    expect(
      keep?.querySelector('[data-testid="nie-loader-symbol"]'),
    ).not.toBeNull();
    wrapper.unmount();
  });
});
