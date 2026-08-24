export type ListFilterValue = string | number | boolean;

export interface ListFilterOption {
  label: string;
  value: ListFilterValue;
  count: number;
}

interface MergeableFilterOption {
  label: string;
  value: ListFilterValue;
  count?: number;
}

type ResolvedFilterValue =
  | ListFilterValue
  | null
  | undefined
  | Array<ListFilterValue | null | undefined>;

export function buildFilterOptions<T>(
  rows: T[],
  getValue: (row: T) => ResolvedFilterValue,
  getLabel?: (value: ListFilterValue) => string,
): ListFilterOption[] {
  const counts = new Map<string, ListFilterOption>();

  rows.forEach((row) => {
    const rawValue = getValue(row);
    const values = Array.isArray(rawValue) ? rawValue : [rawValue];

    values.forEach((value) => {
      if (value === null || value === undefined || value === "") {
        return;
      }

      const key = String(value);
      const existing = counts.get(key);

      if (existing) {
        existing.count += 1;
        return;
      }

      counts.set(key, {
        label: getLabel ? getLabel(value) : String(value),
        value,
        count: 1,
      });
    });
  });

  return [...counts.values()].sort((left, right) =>
    left.label.localeCompare(right.label, undefined, {
      numeric: true,
      sensitivity: "base",
    }),
  );
}

export function mergeFilterOptions(
  ...optionSets: Array<MergeableFilterOption[]>
): ListFilterOption[] {
  const mergedOptions = new Map<string, ListFilterOption>();

  optionSets.forEach((options) => {
    options.forEach((option) => {
      if (
        option.value === null ||
        option.value === undefined ||
        option.value === ""
      ) {
        return;
      }

      const key = String(option.value);
      const existing = mergedOptions.get(key);

      if (existing) {
        existing.count = Math.max(existing.count, option.count ?? 0);
        if (!existing.label && option.label) {
          existing.label = option.label;
        }
        return;
      }

      mergedOptions.set(key, {
        label: option.label,
        value: option.value,
        count: option.count ?? 0,
      });
    });
  });

  return [...mergedOptions.values()].sort((left, right) =>
    left.label.localeCompare(right.label, undefined, {
      numeric: true,
      sensitivity: "base",
    }),
  );
}
