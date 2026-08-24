<script setup lang="ts">
import { computed, ref } from "vue";
import { NieInput, NieSelect } from "@nie/ui";
import AccessFunctionGrantRow from "./AccessFunctionGrantRow.vue";
import type { AccessFunction } from "@/types";

const props = defineProps<{ accessFunctions: AccessFunction[] }>();
const search = ref("");
const moduleFilter = ref<string | null>(null);
const typeFilter = ref<string | null>(null);

const moduleOptions = computed(() => [
  { value: "", label: "All groups" },
  ...[...new Set(props.accessFunctions.map((item) => item.module))]
    .sort()
    .map((module) => ({ value: module, label: module })),
]);
const typeOptions = [
  { value: "", label: "All types" },
  { value: "screen", label: "Screen" },
  { value: "api", label: "API" },
];
const filtered = computed(() => {
  const query = search.value.trim().toLowerCase();
  return props.accessFunctions.filter((item) => {
    const type = String(item.type).toLowerCase();
    const typeLabel = type === "1" || type === "screen" ? "screen" : "api";
    return (
      (!moduleFilter.value || item.module === moduleFilter.value) &&
      (!typeFilter.value || typeLabel === typeFilter.value) &&
      (!query ||
        [
          item.name,
          item.code,
          item.description,
          item.module,
          item.route,
          item.httpMethod,
        ]
          .filter(Boolean)
          .some((value) => String(value).toLowerCase().includes(query)))
    );
  });
});
const groups = computed(() => {
  const values = new Map<string, AccessFunction[]>();
  for (const item of filtered.value) {
    values.set(item.module, [...(values.get(item.module) ?? []), item]);
  }
  return [...values.entries()]
    .map(([name, accessFunctions]) => ({
      name,
      accessFunctions: accessFunctions.sort(
        (left, right) => left.displayOrder - right.displayOrder,
      ),
    }))
    .sort((left, right) => left.name.localeCompare(right.name));
});
</script>

<template>
  <div
    class="flex min-h-0 flex-col gap-5 lg:h-[calc(100dvh-13.5rem)] lg:max-h-[56rem]"
  >
    <div
      class="grid shrink-0 gap-3 rounded-2xl border border-secondary-200 bg-white p-4 md:grid-cols-[minmax(0,1fr)_14rem_12rem] dark:border-secondary-700 dark:bg-secondary-900"
    >
      <NieInput
        v-model="search"
        type="search"
        placeholder="Search access functions"
      />
      <NieSelect
        v-model="moduleFilter"
        :options="moduleOptions"
        placeholder="All groups"
      />
      <NieSelect
        v-model="typeFilter"
        :options="typeOptions"
        placeholder="All types"
      />
    </div>

    <div
      class="min-h-0 flex-1 space-y-5 overflow-y-auto overscroll-contain pr-2"
      role="region"
      tabindex="0"
      aria-label="Access function catalog"
    >
      <section
        v-for="group in groups"
        :key="group.name"
        class="rounded-3xl border border-secondary-200 bg-white p-5 shadow-[var(--theme-shadow-soft)] dark:border-secondary-700 dark:bg-secondary-900"
      >
        <div class="mb-4 flex items-center justify-between">
          <h2 class="text-lg font-bold text-secondary-900 dark:text-white">
            {{ group.name }}
          </h2>
          <span class="text-xs font-semibold text-secondary-400">
            {{ group.accessFunctions.length }} functions
          </span>
        </div>
        <div class="grid gap-3 xl:grid-cols-2">
          <AccessFunctionGrantRow
            v-for="accessFunction in group.accessFunctions"
            :key="accessFunction.id"
            :access-function="accessFunction"
            :selectable="false"
          />
        </div>
      </section>

      <div
        v-if="groups.length === 0"
        class="rounded-3xl border border-dashed border-secondary-300 py-16 text-center text-secondary-500"
      >
        No access functions match the selected filters.
      </div>
    </div>
  </div>
</template>
