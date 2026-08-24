import { describe, expect, it } from "vitest";
import {
  NIE_DATA_TABLE_FILTER_REMINDER_INTERVAL_MS,
  NieDataTablePreferenceConflictError,
  hasActiveDataTablePreferenceFilters,
  isDataTableFilterReminderDue,
  nieDataTablePreferenceStoreKey,
} from "../preferences";
import type { NieDataTableFilterValue } from "../types";

const NOW = Date.parse("2026-08-07T00:00:00.000Z");

function filters(
  values: Record<string, NieDataTableFilterValue[]> = {},
): { filters: Record<string, NieDataTableFilterValue[]> } {
  return { filters: values };
}

describe("NIE_DATA_TABLE_FILTER_REMINDER_INTERVAL_MS", () => {
  it("is exactly one week", () => {
    expect(NIE_DATA_TABLE_FILTER_REMINDER_INTERVAL_MS).toBe(604_800_000);
  });
});

describe("hasActiveDataTablePreferenceFilters", () => {
  it("is false when there are no filters at all", () => {
    expect(hasActiveDataTablePreferenceFilters(filters())).toBe(false);
  });

  it("is false when every filter key holds an empty selection", () => {
    expect(
      hasActiveDataTablePreferenceFilters(filters({ status: [], owner: [] })),
    ).toBe(false);
  });

  it("is true as soon as one filter has a selected value", () => {
    expect(
      hasActiveDataTablePreferenceFilters(
        filters({ status: [], owner: ["ada"] }),
      ),
    ).toBe(true);
  });

  it("treats falsy filter values as real selections", () => {
    expect(
      hasActiveDataTablePreferenceFilters(filters({ archived: [false] })),
    ).toBe(true);
    expect(hasActiveDataTablePreferenceFilters(filters({ count: [0] }))).toBe(
      true,
    );
  });
});

describe("isDataTableFilterReminderDue", () => {
  it("never nags when no filter is active", () => {
    expect(
      isDataTableFilterReminderDue(
        { filters: {}, filterReminderAcknowledgedAtUtc: null },
        NOW,
      ),
    ).toBe(false);
    expect(
      isDataTableFilterReminderDue(
        { filters: { status: [] }, filterReminderAcknowledgedAtUtc: null },
        NOW,
      ),
    ).toBe(false);
  });

  it("is due immediately for filters that were never acknowledged", () => {
    expect(
      isDataTableFilterReminderDue(
        { filters: { status: ["open"] }, filterReminderAcknowledgedAtUtc: null },
        NOW,
      ),
    ).toBe(true);
    expect(
      isDataTableFilterReminderDue({ filters: { status: ["open"] } }, NOW),
    ).toBe(true);
  });

  it("is due again exactly one week after the acknowledgement", () => {
    const acknowledged = new Date(
      NOW - NIE_DATA_TABLE_FILTER_REMINDER_INTERVAL_MS,
    ).toISOString();

    expect(
      isDataTableFilterReminderDue(
        {
          filters: { status: ["open"] },
          filterReminderAcknowledgedAtUtc: acknowledged,
        },
        NOW,
      ),
    ).toBe(true);
  });

  it("stays quiet for the week following an acknowledgement", () => {
    const acknowledged = new Date(
      NOW - NIE_DATA_TABLE_FILTER_REMINDER_INTERVAL_MS + 1,
    ).toISOString();

    expect(
      isDataTableFilterReminderDue(
        {
          filters: { status: ["open"] },
          filterReminderAcknowledgedAtUtc: acknowledged,
        },
        NOW,
      ),
    ).toBe(false);
  });

  it("re-prompts when the stored acknowledgement cannot be parsed", () => {
    expect(
      isDataTableFilterReminderDue(
        {
          filters: { status: ["open"] },
          filterReminderAcknowledgedAtUtc: "not-a-timestamp",
        },
        NOW,
      ),
    ).toBe(true);
  });

  it("defaults to the current clock when no time is supplied", () => {
    expect(
      isDataTableFilterReminderDue({
        filters: { status: ["open"] },
        filterReminderAcknowledgedAtUtc: new Date().toISOString(),
      }),
    ).toBe(false);
  });
});

describe("NieDataTablePreferenceConflictError", () => {
  it("is an Error that can be recognised by name and by instance", () => {
    const error = new NieDataTablePreferenceConflictError();

    expect(error).toBeInstanceOf(Error);
    expect(error).toBeInstanceOf(NieDataTablePreferenceConflictError);
    expect(error.name).toBe("NieDataTablePreferenceConflictError");
    expect(error.message).toBe(
      "The saved table preference changed in another session.",
    );
  });
});

describe("nieDataTablePreferenceStoreKey", () => {
  it("is a described symbol so provide/inject stays collision-free", () => {
    expect(typeof nieDataTablePreferenceStoreKey).toBe("symbol");
    expect(nieDataTablePreferenceStoreKey.description).toBe(
      "nie-data-table-preference-store",
    );
  });
});
