<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from "vue";
import {
  ChevronDownIcon,
  FunnelIcon,
  XMarkIcon,
} from "@heroicons/vue/24/outline";
import { NieButton } from "../../ui/button";

type FilterValue = string | number | boolean;

interface FilterOption {
  label: string;
  value: FilterValue;
  count?: number;
}

interface FilterGroup {
  key: string;
  label: string;
  options: FilterOption[];
}

interface Props {
  modelValue?: Record<string, FilterValue[]>;
  groups?: FilterGroup[];
  buttonLabel?: string;
  open?: boolean;
  triggerVisibility?: "always" | "desktop-only" | "hidden";
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: () => ({}),
  groups: () => [],
  buttonLabel: "Filters",
  triggerVisibility: "always",
});

const emit = defineEmits<{
  "update:modelValue": [value: Record<string, FilterValue[]>];
  "update:open": [value: boolean];
}>();

const wrapperRef = ref<HTMLElement | null>(null);
const internalOpen = ref(false);
const isMobileViewport = ref(false);

const groupsWithOptions = computed(() =>
  props.groups.filter((group) => group.options.length > 0),
);

const activeCount = computed(() =>
  Object.values(props.modelValue ?? {}).reduce(
    (sum, values) => sum + values.length,
    0,
  ),
);

const isOpen = computed({
  get: () => (props.open === undefined ? internalOpen.value : props.open),
  set: (value: boolean) => {
    internalOpen.value = value;
    emit("update:open", value);
  },
});

function cloneFilters(
  value: Record<string, FilterValue[]> | undefined,
): Record<string, FilterValue[]> {
  if (!value) {
    return {};
  }

  return Object.fromEntries(
    Object.entries(value).map(([key, items]) => [key, [...items]]),
  );
}

function isSelected(groupKey: string, optionValue: FilterValue): boolean {
  const selected = props.modelValue?.[groupKey] ?? [];
  return selected.some((value) => String(value) === String(optionValue));
}

function isOptionDisabled(groupKey: string, option: FilterOption): boolean {
  if (typeof option.count !== "number" || option.count > 0) {
    return false;
  }

  return !isSelected(groupKey, option.value);
}

function toggleOpen() {
  if (!groupsWithOptions.value.length) {
    return;
  }

  isOpen.value = !isOpen.value;
}

function syncViewport() {
  isMobileViewport.value = window.innerWidth < 768;
}

function closePanel() {
  isOpen.value = false;
}

function clearAll() {
  emit("update:modelValue", {});
}

function toggleOption(groupKey: string, optionValue: FilterValue) {
  const option = props.groups
    .find((group) => group.key === groupKey)
    ?.options.find((item) => String(item.value) === String(optionValue));

  if (option && isOptionDisabled(groupKey, option)) {
    return;
  }

  const next = cloneFilters(props.modelValue);
  const existing = next[groupKey] ?? [];
  const isActive = existing.some(
    (value) => String(value) === String(optionValue),
  );

  next[groupKey] = isActive
    ? existing.filter((value) => String(value) !== String(optionValue))
    : [...existing, optionValue];

  if (!next[groupKey].length) {
    delete next[groupKey];
  }

  emit("update:modelValue", next);
}

function handleClickOutside(event: MouseEvent) {
  const target = event.target as HTMLElement;

  if (
    wrapperRef.value &&
    !wrapperRef.value.contains(target) &&
    !target.closest(".nie-smart-filter-sheet")
  ) {
    closePanel();
  }
}

onMounted(() => {
  syncViewport();
  document.addEventListener("mousedown", handleClickOutside);
  window.addEventListener("resize", syncViewport);
});

onUnmounted(() => {
  document.removeEventListener("mousedown", handleClickOutside);
  window.removeEventListener("resize", syncViewport);
});
</script>

<template>
  <div v-if="groupsWithOptions.length" ref="wrapperRef" class="relative">
    <NieButton
      v-if="triggerVisibility !== 'hidden'"
      variant="outline"
      class="min-w-[8.75rem] justify-between gap-3"
      :class="
        triggerVisibility === 'desktop-only' ? 'hidden md:inline-flex' : ''
      "
      @click="toggleOpen"
    >
      <span class="inline-flex items-center gap-2">
        <FunnelIcon class="h-4 w-4" />
        <span>{{ buttonLabel }}</span>
        <span
          v-if="activeCount > 0"
          class="inline-flex min-w-[1.4rem] items-center justify-center rounded-full bg-primary-600 px-1.5 py-0.5 text-caption font-semibold text-on-brand"
        >
          {{ activeCount }}
        </span>
      </span>
      <ChevronDownIcon
        class="h-4 w-4 transition-transform duration-200"
        :class="{ 'rotate-180': isOpen }"
      />
    </NieButton>

    <Transition
      enter-active-class="transition ease-out duration-150"
      enter-from-class="opacity-0 translate-y-1"
      enter-to-class="opacity-100 translate-y-0"
      leave-active-class="transition ease-in duration-100"
      leave-from-class="opacity-100 translate-y-0"
      leave-to-class="opacity-0 translate-y-1"
    >
      <div
        v-if="isOpen && !isMobileViewport"
        class="absolute right-0 z-[90] mt-2 w-[22rem] overflow-hidden rounded-2xl border border-secondary-200 bg-white shadow-[var(--theme-shadow-float)] dark:border-secondary-700 dark:bg-secondary-900"
      >
        <div
          class="flex items-center justify-between border-b border-secondary-200 px-4 py-3 dark:border-secondary-700"
        >
          <p
            class="text-sm font-semibold text-secondary-900 dark:text-secondary-100"
          >
            Filters
          </p>
          <button
            type="button"
            class="text-xs font-semibold text-primary-600 transition hover:text-primary-700 dark:text-primary-300"
            @click="clearAll"
          >
            Clear all
          </button>
        </div>

        <div class="max-h-[26rem] overflow-y-auto p-3">
          <section
            v-for="group in groupsWithOptions"
            :key="group.key"
            class="rounded-xl border border-secondary-200 bg-secondary-50/60 p-3 dark:border-secondary-700 dark:bg-secondary-800/70"
          >
            <p
              class="mb-2 text-xs font-semibold uppercase tracking-wide text-secondary-500 dark:text-secondary-400"
            >
              {{ group.label }}
            </p>

            <div class="space-y-1.5">
              <label
                v-for="option in group.options"
                :key="`${group.key}-${String(option.value)}`"
                class="flex items-center gap-3 rounded-lg px-2 py-1.5 transition"
                :class="
                  isOptionDisabled(group.key, option)
                    ? 'cursor-not-allowed opacity-50'
                    : 'cursor-pointer hover:bg-white dark:hover:bg-secondary-900'
                "
              >
                <input
                  :checked="isSelected(group.key, option.value)"
                  :disabled="isOptionDisabled(group.key, option)"
                  type="checkbox"
                  class="h-4 w-4 rounded border-secondary-300 text-primary-600 focus:ring-primary-500 dark:border-secondary-600"
                  @change="toggleOption(group.key, option.value)"
                />
                <span
                  class="min-w-0 flex-1 text-sm text-secondary-700 dark:text-secondary-200"
                >
                  {{ option.label }}
                </span>
                <span
                  v-if="typeof option.count === 'number'"
                  class="rounded-full bg-secondary-200 px-2 py-0.5 text-caption font-semibold text-secondary-600 dark:bg-secondary-700 dark:text-secondary-300"
                >
                  {{ option.count }}
                </span>
              </label>
            </div>
          </section>
        </div>
      </div>
    </Transition>

    <Teleport to="body">
      <Transition
        enter-active-class="transition ease-out duration-200"
        enter-from-class="translate-y-4 opacity-0"
        enter-to-class="translate-y-0 opacity-100"
        leave-active-class="transition ease-in duration-150"
        leave-from-class="translate-y-0 opacity-100"
        leave-to-class="translate-y-4 opacity-0"
      >
        <div
          v-if="isOpen && isMobileViewport"
          class="fixed inset-0 z-[160] flex items-end justify-center px-3 pt-3"
        >
          <button
            type="button"
            class="absolute inset-0 bg-secondary-900/55"
            aria-label="Close filters"
            @click="closePanel"
          />

          <div
            class="nie-smart-filter-sheet relative flex max-h-[84dvh] w-full max-w-md flex-col overflow-hidden rounded-[var(--theme-radius-dialog)] border border-secondary-200 bg-white shadow-[var(--theme-shadow-float)] dark:border-secondary-700 dark:bg-secondary-900"
            @click.stop
          >
            <div
              class="mx-auto mt-3 h-1.5 w-14 rounded-full bg-secondary-200 dark:bg-secondary-700"
            ></div>

            <div
              class="flex items-center justify-between gap-3 border-b border-secondary-200 px-4 pb-3 pt-2 dark:border-secondary-700"
            >
              <div>
                <p
                  class="text-base font-bold text-secondary-900 dark:text-secondary-100"
                >
                  Filters
                </p>
                <p
                  class="mt-1 text-xs text-secondary-500 dark:text-secondary-400"
                >
                  {{ activeCount }} selected
                </p>
              </div>

              <button
                type="button"
                class="inline-flex h-9 w-9 items-center justify-center rounded-full border border-secondary-200 text-secondary-400 transition-colors hover:bg-secondary-50 hover:text-secondary-700 dark:border-secondary-700 dark:hover:bg-secondary-800 dark:hover:text-secondary-200"
                aria-label="Close filters"
                @click="closePanel"
              >
                <XMarkIcon class="h-5 w-5" />
              </button>
            </div>

            <div
              class="flex items-center justify-between border-b border-secondary-200 px-4 py-3 dark:border-secondary-700"
            >
              <p
                class="text-sm font-medium text-secondary-600 dark:text-secondary-300"
              >
                Refine the current list
              </p>
              <button
                type="button"
                class="text-xs font-semibold text-primary-600 transition hover:text-primary-700 disabled:cursor-not-allowed disabled:opacity-40 dark:text-primary-300"
                :disabled="activeCount === 0"
                @click="clearAll"
              >
                Clear all
              </button>
            </div>

            <div class="min-h-0 flex-1 space-y-3 overflow-y-auto p-3">
              <section
                v-for="group in groupsWithOptions"
                :key="`mobile-${group.key}`"
                class="rounded-2xl border border-secondary-200 bg-secondary-50/60 p-3 dark:border-secondary-700 dark:bg-secondary-800/70"
              >
                <p
                  class="mb-2 text-xs font-semibold uppercase tracking-wide text-secondary-500 dark:text-secondary-400"
                >
                  {{ group.label }}
                </p>

                <div class="space-y-1.5">
                  <label
                    v-for="option in group.options"
                    :key="`mobile-${group.key}-${String(option.value)}`"
                    class="flex items-center gap-3 rounded-xl px-2 py-2 transition"
                    :class="
                      isOptionDisabled(group.key, option)
                        ? 'cursor-not-allowed opacity-50'
                        : 'cursor-pointer hover:bg-white dark:hover:bg-secondary-900'
                    "
                  >
                    <input
                      :checked="isSelected(group.key, option.value)"
                      :disabled="isOptionDisabled(group.key, option)"
                      type="checkbox"
                      class="h-4 w-4 rounded border-secondary-300 text-primary-600 focus:ring-primary-500 dark:border-secondary-600"
                      @change="toggleOption(group.key, option.value)"
                    />
                    <span
                      class="min-w-0 flex-1 text-sm text-secondary-700 dark:text-secondary-200"
                    >
                      {{ option.label }}
                    </span>
                    <span
                      v-if="typeof option.count === 'number'"
                      class="rounded-full bg-secondary-200 px-2 py-0.5 text-caption font-semibold text-secondary-600 dark:bg-secondary-700 dark:text-secondary-300"
                    >
                      {{ option.count }}
                    </span>
                  </label>
                </div>
              </section>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>
