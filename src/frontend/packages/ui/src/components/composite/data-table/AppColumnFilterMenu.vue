<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from "vue";
import {
  ChevronDownIcon,
  FunnelIcon,
  MagnifyingGlassIcon,
  XMarkIcon,
} from "@heroicons/vue/24/outline";
import { CheckIcon } from "@heroicons/vue/20/solid";

type FilterValue = string | number | boolean;

interface FilterOption {
  label: string;
  value: FilterValue;
  count?: number;
}

interface Props {
  columnLabel: string;
  modelValue?: FilterValue[];
  options?: FilterOption[];
  align?: "left" | "right";
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: () => [],
  options: () => [],
  align: "left",
});

const emit = defineEmits<{
  "update:modelValue": [value: FilterValue[]];
  "search-all": [query: string];
}>();

const MAX_VISIBLE_OPTIONS = 50;

const wrapperRef = ref<HTMLElement | null>(null);
const triggerRef = ref<HTMLElement | null>(null);
const panelRef = ref<HTMLElement | null>(null);
const isOpen = ref(false);
const optionQuery = ref("");
const popupStyle = ref<Record<string, string>>({});

const selectedCount = computed(() => props.modelValue.length);

const filteredOptions = computed(() => {
  const query = optionQuery.value.trim().toLowerCase();

  if (!query) {
    return props.options;
  }

  return props.options.filter((option) => {
    const searchable = `${option.label} ${String(option.value)}`.toLowerCase();
    return searchable.includes(query);
  });
});

const visibleOptions = computed(() =>
  filteredOptions.value.slice(0, MAX_VISIBLE_OPTIONS),
);

const hiddenOptionCount = computed(() =>
  Math.max(filteredOptions.value.length - MAX_VISIBLE_OPTIONS, 0),
);

function isSelected(optionValue: FilterValue): boolean {
  return props.modelValue.some(
    (value) => String(value) === String(optionValue),
  );
}

function isOptionDisabled(option: FilterOption): boolean {
  if (typeof option.count !== "number" || option.count > 0) {
    return false;
  }

  return !isSelected(option.value);
}

function updatePopupPosition(): void {
  if (!triggerRef.value) return;
  const rect = triggerRef.value.getBoundingClientRect();
  const popupWidth = 256; // 16rem
  const viewportWidth = window.innerWidth;

  let left: number;
  if (props.align === "right") {
    left = rect.right - popupWidth;
  } else {
    left = rect.left;
  }

  // Clamp to viewport
  if (left + popupWidth > viewportWidth - 8) {
    left = viewportWidth - popupWidth - 8;
  }
  if (left < 8) {
    left = 8;
  }

  popupStyle.value = {
    position: "fixed",
    top: `${rect.bottom + 8}px`,
    left: `${left}px`,
    width: `${popupWidth}px`,
    zIndex: "9999",
  };
}

function toggleOpen(): void {
  if (!props.options.length) {
    return;
  }

  isOpen.value = !isOpen.value;

  if (isOpen.value) {
    updatePopupPosition();
  } else {
    optionQuery.value = "";
  }
}

function closePanel(): void {
  isOpen.value = false;
  optionQuery.value = "";
}

function toggleOption(optionValue: FilterValue): void {
  const isActive = isSelected(optionValue);
  const nextValue = isActive
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

onMounted(() => {
  document.addEventListener("mousedown", handleClickOutside);
  window.addEventListener("scroll", closePanel, true);
  window.addEventListener("resize", closePanel);
});

onUnmounted(() => {
  document.removeEventListener("mousedown", handleClickOutside);
  window.removeEventListener("scroll", closePanel, true);
  window.removeEventListener("resize", closePanel);
});
</script>

<template>
  <div ref="wrapperRef" class="relative shrink-0">
    <button
      ref="triggerRef"
      type="button"
      class="inline-flex h-7 items-center gap-1.5 rounded-lg border px-2 text-[11px] font-semibold shadow-sm transition-colors"
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
        class="inline-flex min-w-[1.15rem] items-center justify-center rounded-full bg-current/10 px-1.5 py-0.5 text-[10px] font-bold"
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
          class="overflow-hidden rounded-xl border border-secondary-200 bg-white shadow-lg dark:border-secondary-700 dark:bg-secondary-900"
          :style="popupStyle"
        >
          <div
            class="flex items-center justify-between border-b border-secondary-200 px-3 py-2.5 dark:border-secondary-700"
          >
            <p
              class="text-xs font-semibold text-secondary-700 dark:text-secondary-300"
            >
              {{ columnLabel }}
            </p>

            <button
              v-if="selectedCount > 0"
              type="button"
              class="text-[11px] font-medium text-primary-600 transition hover:text-primary-700 dark:text-primary-300"
              @click="clearFilters"
            >
              Clear
            </button>
          </div>

          <div
            class="border-b border-secondary-200 px-3 py-2 dark:border-secondary-700"
          >
            <label class="relative block">
              <MagnifyingGlassIcon
                class="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-secondary-400"
              />
              <input
                v-model="optionQuery"
                type="search"
                placeholder="Filter values"
                class="w-full rounded-lg border border-secondary-200 bg-secondary-50 py-1.5 pl-8 pr-8 text-xs text-secondary-900 outline-none transition focus:border-primary-400 focus:bg-white focus:ring-1 focus:ring-primary-500/20 dark:border-secondary-700 dark:bg-secondary-800 dark:text-secondary-100 dark:focus:border-primary-500"
              />
              <button
                v-if="optionQuery"
                type="button"
                class="absolute right-1.5 top-1/2 -translate-y-1/2 rounded-full p-0.5 text-secondary-400 transition hover:bg-secondary-200 hover:text-secondary-700 dark:hover:bg-secondary-700 dark:hover:text-secondary-100"
                @click="optionQuery = ''"
              >
                <XMarkIcon class="h-3.5 w-3.5" />
              </button>
            </label>
            <button
              v-if="optionQuery"
              type="button"
              class="mt-1.5 w-full rounded-lg border border-primary-200 bg-primary-50 px-2 py-1 text-[11px] font-medium text-primary-700 transition hover:bg-primary-100 dark:border-primary-500/30 dark:bg-primary-500/10 dark:text-primary-300 dark:hover:bg-primary-500/20"
              @click="emit('search-all', optionQuery)"
            >
              Search all records
            </button>
          </div>

          <div class="max-h-[14rem] overflow-y-auto p-1.5">
            <div
              v-if="visibleOptions.length === 0"
              class="rounded-lg border border-dashed border-secondary-200 px-3 py-5 text-center text-xs text-secondary-500 dark:border-secondary-700 dark:text-secondary-400"
            >
              No matching values.
            </div>

            <div v-else class="space-y-0.5">
              <button
                v-for="option in visibleOptions"
                :key="`${columnLabel}-${String(option.value)}`"
                type="button"
                class="flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-left transition-colors"
                :class="[
                  isSelected(option.value)
                    ? 'bg-primary-50 text-primary-800 dark:bg-primary-500/10 dark:text-primary-100'
                    : 'text-secondary-700 hover:bg-secondary-50 dark:text-secondary-200 dark:hover:bg-secondary-800',
                  isOptionDisabled(option)
                    ? 'cursor-not-allowed opacity-50'
                    : '',
                ]"
                :disabled="isOptionDisabled(option)"
                @click="toggleOption(option.value)"
              >
                <span
                  class="flex h-3.5 w-3.5 shrink-0 items-center justify-center rounded border text-white"
                  :class="
                    isSelected(option.value)
                      ? 'border-primary-600 bg-primary-600 dark:border-primary-400 dark:bg-primary-400'
                      : 'border-secondary-300 bg-white dark:border-secondary-600 dark:bg-secondary-900'
                  "
                >
                  <CheckIcon
                    v-if="isSelected(option.value)"
                    class="h-2.5 w-2.5"
                  />
                </span>

                <span class="min-w-0 flex-1 truncate text-xs font-medium">
                  {{ option.label }}
                </span>

                <span
                  v-if="typeof option.count === 'number'"
                  class="rounded-full bg-secondary-200 px-1.5 py-0.5 text-[10px] font-semibold text-secondary-600 dark:bg-secondary-700 dark:text-secondary-300"
                >
                  {{ option.count }}
                </span>
              </button>
            </div>
          </div>

          <div
            v-if="hiddenOptionCount > 0"
            class="border-t border-secondary-200 px-3 py-2 text-[10px] font-medium text-secondary-500 dark:border-secondary-700 dark:text-secondary-400"
          >
            Showing first {{ MAX_VISIBLE_OPTIONS }}. Narrow filter to refine.
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>
