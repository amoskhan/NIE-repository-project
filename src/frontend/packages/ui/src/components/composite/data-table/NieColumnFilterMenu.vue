<script setup lang="ts">
import {
  computed,
  nextTick,
  onMounted,
  onUnmounted,
  shallowRef,
  watch,
} from "vue";
import {
  ChevronDownIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  FunnelIcon,
  MagnifyingGlassIcon,
  XMarkIcon,
} from "@heroicons/vue/24/outline";
import { CheckIcon } from "@heroicons/vue/20/solid";
import type {
  NieDataTableFilterOption,
  NieDataTableFilterValue,
} from "./types";

interface Props {
  columnLabel: string;
  modelValue?: NieDataTableFilterValue[];
  options?: NieDataTableFilterOption[];
  align?: "left" | "right";
  remote?: boolean;
  loading?: boolean;
  error?: string | null;
  page?: number;
  pageSize?: number;
  totalCount?: number;
  totalPages?: number;
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: () => [],
  options: () => [],
  align: "left",
  remote: false,
  loading: false,
  error: null,
  page: 1,
  pageSize: 25,
  totalCount: 0,
  totalPages: 0,
});

const emit = defineEmits<{
  "update:modelValue": [value: NieDataTableFilterValue[]];
  "request-options": [request: { page: number; pageSize: number; search: string }];
}>();

const MAX_VISIBLE_LOCAL_OPTIONS = 50;
const wrapperRef = shallowRef<HTMLElement | null>(null);
const triggerRef = shallowRef<HTMLElement | null>(null);
const panelRef = shallowRef<HTMLElement | null>(null);
const isOpen = shallowRef(false);
const optionQuery = shallowRef("");
const popupStyle = shallowRef<Record<string, string>>({});
let searchTimeout: ReturnType<typeof setTimeout> | null = null;

const selectedCount = computed(() => props.modelValue.length);
const filteredLocalOptions = computed(() => {
  if (props.remote) return props.options;
  const query = optionQuery.value.trim().toLowerCase();
  if (!query) return props.options;
  return props.options.filter((option) =>
    `${option.label} ${String(option.value)}`.toLowerCase().includes(query),
  );
});
const visibleOptions = computed(() =>
  props.remote
    ? filteredLocalOptions.value
    : filteredLocalOptions.value.slice(0, MAX_VISIBLE_LOCAL_OPTIONS),
);
const hiddenOptionCount = computed(() =>
  props.remote
    ? 0
    : Math.max(
        filteredLocalOptions.value.length - MAX_VISIBLE_LOCAL_OPTIONS,
        0,
      ),
);
const effectiveTotalPages = computed(() =>
  Math.max(props.totalPages, props.totalCount > 0 ? 1 : 0),
);

function isSelected(optionValue: NieDataTableFilterValue): boolean {
  return props.modelValue.some(
    (value) => String(value) === String(optionValue),
  );
}

function isOptionDisabled(option: NieDataTableFilterOption): boolean {
  return typeof option.count === "number" && option.count <= 0
    ? !isSelected(option.value)
    : false;
}

function updatePopupPosition(): void {
  if (!triggerRef.value) return;
  const rect = triggerRef.value.getBoundingClientRect();
  const popupWidth = 288;
  const viewportWidth = window.innerWidth;
  const viewportHeight = window.innerHeight;
  const measuredHeight = panelRef.value?.offsetHeight ?? 420;
  let left = props.align === "right" ? rect.right - popupWidth : rect.left;

  left = Math.max(8, Math.min(left, viewportWidth - popupWidth - 8));
  const below = rect.bottom + 8;
  const top =
    below + measuredHeight <= viewportHeight - 8
      ? below
      : Math.max(8, rect.top - measuredHeight - 8);

  popupStyle.value = {
    position: "fixed",
    top: `${top}px`,
    left: `${left}px`,
    width: `${popupWidth}px`,
    zIndex: "9999",
  };
}

function requestOptions(page = 1): void {
  emit("request-options", {
    page,
    pageSize: props.pageSize,
    search: optionQuery.value.trim(),
  });
}

async function toggleOpen(): Promise<void> {
  if (!props.remote && !props.options.length) return;
  isOpen.value = !isOpen.value;

  if (!isOpen.value) {
    optionQuery.value = "";
    return;
  }

  await nextTick();
  updatePopupPosition();
  if (props.remote) requestOptions(1);
}

function closePanel(): void {
  isOpen.value = false;
  optionQuery.value = "";
}

function toggleOption(optionValue: NieDataTableFilterValue): void {
  const nextValue = isSelected(optionValue)
    ? props.modelValue.filter((value) => String(value) !== String(optionValue))
    : [...props.modelValue, optionValue];
  emit("update:modelValue", nextValue);
}

function clearFilters(): void {
  emit("update:modelValue", []);
}

function handleClickOutside(event: MouseEvent): void {
  const target = event.target as Node;
  if (
    wrapperRef.value &&
    !wrapperRef.value.contains(target) &&
    (!panelRef.value || !panelRef.value.contains(target))
  ) {
    closePanel();
  }
}

function handleKeydown(event: KeyboardEvent): void {
  if (event.key === "Escape" && isOpen.value) {
    closePanel();
    triggerRef.value?.focus();
  }
}

function handleViewportChange(): void {
  if (isOpen.value) updatePopupPosition();
}

watch(optionQuery, () => {
  if (!props.remote || !isOpen.value) return;
  if (searchTimeout) clearTimeout(searchTimeout);
  searchTimeout = setTimeout(() => requestOptions(1), 250);
});

onMounted(() => {
  document.addEventListener("mousedown", handleClickOutside);
  document.addEventListener("keydown", handleKeydown);
  window.addEventListener("scroll", handleViewportChange, true);
  window.addEventListener("resize", handleViewportChange);
});

onUnmounted(() => {
  document.removeEventListener("mousedown", handleClickOutside);
  document.removeEventListener("keydown", handleKeydown);
  window.removeEventListener("scroll", handleViewportChange, true);
  window.removeEventListener("resize", handleViewportChange);
  if (searchTimeout) clearTimeout(searchTimeout);
});
</script>

<template>
  <div ref="wrapperRef" class="relative shrink-0">
    <button
      ref="triggerRef"
      type="button"
      class="column-filter-trigger inline-flex h-8 min-w-8 items-center justify-center gap-1 rounded-[var(--theme-radius-control)] border px-1.5 text-caption font-semibold shadow-[var(--theme-shadow-soft)] transition-colors"
      :class="
        selectedCount > 0
          ? 'border-primary-200 bg-primary-50 text-primary-700 dark:border-primary-500/40 dark:bg-primary-500/10 dark:text-primary-100'
          : 'border-secondary-200 bg-white text-secondary-500 hover:border-secondary-300 hover:text-secondary-700 dark:border-secondary-700 dark:bg-secondary-900 dark:text-secondary-400 dark:hover:border-secondary-600 dark:hover:text-secondary-100'
      "
      :aria-label="`Filter ${columnLabel}`"
      :aria-expanded="isOpen"
      @click.stop="toggleOpen"
    >
      <FunnelIcon class="h-3.5 w-3.5" />
      <span
        v-if="selectedCount > 0"
        class="inline-flex min-w-[1.15rem] items-center justify-center rounded-full bg-current/10 px-1.5 py-0.5 text-caption font-bold"
      >
        {{ selectedCount }}
      </span>
      <ChevronDownIcon
        class="h-3.5 w-3.5 transition-transform duration-200"
        :class="{ 'rotate-180': isOpen }"
      />
    </button>

    <Teleport to="body">
      <Transition
        enter-active-class="transition ease-out duration-150"
        enter-from-class="translate-y-1 opacity-0"
        enter-to-class="translate-y-0 opacity-100"
        leave-active-class="transition ease-in duration-100"
        leave-from-class="translate-y-0 opacity-100"
        leave-to-class="translate-y-1 opacity-0"
      >
        <div
          v-if="isOpen"
          ref="panelRef"
          role="dialog"
          :aria-label="`Filter ${columnLabel} values`"
          class="overflow-hidden rounded-xl border border-secondary-200 bg-white shadow-[var(--theme-shadow-float)] dark:border-secondary-700 dark:bg-secondary-900"
          :style="popupStyle"
        >
          <div
            class="flex items-center justify-between border-b border-secondary-200 px-3 py-2.5 dark:border-secondary-700"
          >
            <p class="text-xs font-semibold text-secondary-700 dark:text-secondary-300">
              {{ columnLabel }}
            </p>
            <button
              v-if="selectedCount > 0"
              type="button"
              class="text-caption font-medium text-primary-600 transition hover:text-primary-700 dark:text-primary-300"
              @click="clearFilters"
            >
              Clear
            </button>
          </div>

          <div class="border-b border-secondary-200 px-3 py-2 dark:border-secondary-700">
            <label class="relative block">
              <MagnifyingGlassIcon
                class="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-secondary-400"
              />
              <input
                v-model="optionQuery"
                type="search"
                placeholder="Filter values"
                :aria-label="`Filter ${columnLabel} values`"
                class="w-full rounded-lg border border-secondary-200 bg-secondary-50 py-1.5 pl-8 pr-8 text-xs text-secondary-900 outline-none transition focus:border-primary-400 focus:bg-white focus:ring-1 focus:ring-primary-500/20 dark:border-secondary-700 dark:bg-secondary-800 dark:text-secondary-100 dark:focus:border-primary-500"
              />
              <button
                v-if="optionQuery"
                type="button"
                aria-label="Clear filter value search"
                class="absolute right-1.5 top-1/2 -translate-y-1/2 rounded-full p-0.5 text-secondary-400 transition hover:bg-secondary-200 hover:text-secondary-700 dark:hover:bg-secondary-700 dark:hover:text-secondary-100"
                @click="optionQuery = ''"
              >
                <XMarkIcon class="h-3.5 w-3.5" />
              </button>
            </label>
          </div>

          <div class="max-h-[14rem] overflow-y-auto p-1.5">
            <div v-if="loading" class="px-3 py-5 text-center text-xs text-secondary-500">
              Loading values...
            </div>
            <div
              v-else-if="error"
              class="rounded-lg border border-danger-200 px-3 py-4 text-center text-xs text-danger-700"
            >
              {{ error }}
            </div>
            <div
              v-else-if="visibleOptions.length === 0"
              class="rounded-lg border border-dashed border-secondary-200 px-3 py-5 text-center text-xs text-secondary-500 dark:border-secondary-700 dark:text-secondary-400"
            >
              No matching values.
            </div>
            <div v-else class="space-y-0.5">
              <button
                v-for="option in visibleOptions"
                :key="`${columnLabel}-${String(option.value)}`"
                type="button"
                :data-filter-value="String(option.value)"
                class="flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-left transition-colors"
                :class="[
                  isSelected(option.value)
                    ? 'bg-primary-50 text-primary-800 dark:bg-primary-500/10 dark:text-primary-100'
                    : 'text-secondary-700 hover:bg-secondary-50 dark:text-secondary-200 dark:hover:bg-secondary-800',
                  isOptionDisabled(option) ? 'cursor-not-allowed opacity-50' : '',
                ]"
                :disabled="isOptionDisabled(option)"
                @click="toggleOption(option.value)"
              >
                <span
                  class="flex h-3.5 w-3.5 shrink-0 items-center justify-center rounded border text-on-brand"
                  :class="
                    isSelected(option.value)
                      ? 'border-primary-600 bg-primary-600 dark:border-primary-400 dark:bg-primary-400'
                      : 'border-secondary-300 bg-white dark:border-secondary-600 dark:bg-secondary-900'
                  "
                >
                  <CheckIcon v-if="isSelected(option.value)" class="h-2.5 w-2.5" />
                </span>
                <span class="min-w-0 flex-1 truncate text-xs font-medium">{{ option.label }}</span>
                <span
                  v-if="typeof option.count === 'number'"
                  class="rounded-full bg-secondary-200 px-1.5 py-0.5 text-caption font-semibold text-secondary-600 dark:bg-secondary-700 dark:text-secondary-300"
                >
                  {{ option.count }}
                </span>
              </button>
            </div>
          </div>

          <div
            v-if="remote && (totalCount > 0 || effectiveTotalPages > 1)"
            class="flex items-center justify-between border-t border-secondary-200 px-3 py-2 dark:border-secondary-700"
          >
            <span class="text-caption font-medium text-secondary-500">
              Page {{ page }} of {{ Math.max(effectiveTotalPages, 1) }} · {{ totalCount }} values
            </span>
            <div class="flex items-center gap-1">
              <button
                type="button"
                aria-label="Previous filter values page"
                :disabled="page <= 1 || loading"
                class="inline-flex size-8 items-center justify-center rounded-lg border border-secondary-200 text-secondary-600 disabled:opacity-40 dark:border-secondary-700 dark:text-secondary-300"
                @click="requestOptions(page - 1)"
              >
                <ChevronLeftIcon class="h-4 w-4" />
              </button>
              <button
                type="button"
                aria-label="Next filter values page"
                :disabled="page >= effectiveTotalPages || loading"
                class="inline-flex size-8 items-center justify-center rounded-lg border border-secondary-200 text-secondary-600 disabled:opacity-40 dark:border-secondary-700 dark:text-secondary-300"
                @click="requestOptions(page + 1)"
              >
                <ChevronRightIcon class="h-4 w-4" />
              </button>
            </div>
          </div>
          <div
            v-else-if="hiddenOptionCount > 0"
            class="border-t border-secondary-200 px-3 py-2 text-caption font-medium text-secondary-500 dark:border-secondary-700 dark:text-secondary-400"
          >
            Showing first {{ MAX_VISIBLE_LOCAL_OPTIONS }}. Narrow filter to refine.
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>
