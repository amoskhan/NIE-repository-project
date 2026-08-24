<script setup lang="ts">
import { ref, computed, watch, nextTick, onMounted, onUnmounted } from "vue";
import { cn } from "../../../lib/utils";

interface Option {
  value: string | number;
  label: string;
  disabled?: boolean;
}

interface Props {
  modelValue?: string | number | null;
  options: Option[];
  placeholder?: string;
  disabled?: boolean;
  error?: string;
  label?: string;
  id?: string;
  searchable?: boolean;
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  disabled: false,
  placeholder: "Select an option",
  searchable: false,
});

const emit = defineEmits<{
  "update:modelValue": [value: string | number | null];
  change: [value: string | number | null];
}>();

const selectId = computed(
  () => props.id || `select-${Math.random().toString(36).slice(2, 9)}`,
);
const errorId = computed(() => `${selectId.value}-error`);

const isOpen = ref(false);
const search = ref("");
const highlightedIndex = ref(-1);
const wrapperRef = ref<HTMLElement | null>(null);
const listRef = ref<HTMLElement | null>(null);
const searchInputRef = ref<HTMLInputElement | null>(null);

const selectedLabel = computed(() => {
  if (
    props.modelValue === undefined ||
    props.modelValue === null ||
    props.modelValue === ""
  )
    return "";
  const found = props.options.find(
    (o) => String(o.value) === String(props.modelValue),
  );
  return found?.label ?? "";
});

const filteredOptions = computed(() => {
  if (!props.searchable || !search.value) return props.options;
  const q = search.value.toLowerCase();
  return props.options.filter((o) => o.label.toLowerCase().includes(q));
});

function toggle() {
  if (props.disabled) return;
  if (isOpen.value) {
    close();
  } else {
    open();
  }
}

function open() {
  if (props.disabled) return;
  isOpen.value = true;
  search.value = "";
  highlightedIndex.value = -1;
  nextTick(() => {
    if (props.searchable && searchInputRef.value) {
      searchInputRef.value.focus();
    }
  });
}

function close() {
  isOpen.value = false;
  search.value = "";
  highlightedIndex.value = -1;
}

function selectOption(option: Option) {
  if (option.disabled) return;
  emit("update:modelValue", option.value);
  emit("change", option.value);
  close();
}

function onKeydown(e: KeyboardEvent) {
  if (!isOpen.value) {
    if (e.key === "Enter" || e.key === " " || e.key === "ArrowDown") {
      e.preventDefault();
      open();
    }
    return;
  }

  const opts = filteredOptions.value;
  switch (e.key) {
    case "ArrowDown":
      e.preventDefault();
      highlightedIndex.value = Math.min(
        highlightedIndex.value + 1,
        opts.length - 1,
      );
      scrollToHighlighted();
      break;
    case "ArrowUp":
      e.preventDefault();
      highlightedIndex.value = Math.max(highlightedIndex.value - 1, 0);
      scrollToHighlighted();
      break;
    case "Enter":
      e.preventDefault();
      if (highlightedIndex.value >= 0 && opts[highlightedIndex.value]) {
        selectOption(opts[highlightedIndex.value]);
      }
      break;
    case "Escape":
      e.preventDefault();
      close();
      break;
  }
}

function scrollToHighlighted() {
  nextTick(() => {
    const el = listRef.value?.querySelector("[data-highlighted]");
    el?.scrollIntoView({ block: "nearest" });
  });
}

function handleClickOutside(e: MouseEvent) {
  if (wrapperRef.value && !wrapperRef.value.contains(e.target as Node)) {
    close();
  }
}

watch(
  () => search.value,
  () => {
    highlightedIndex.value = 0;
  },
);

onMounted(() => document.addEventListener("mousedown", handleClickOutside));
onUnmounted(() =>
  document.removeEventListener("mousedown", handleClickOutside),
);

const triggerClasses = computed(() =>
  cn(
    "nie-select-trigger flex min-h-11 items-center justify-between w-full rounded-[var(--theme-radius-control)] border px-3 py-2 text-sm transition-colors duration-200 cursor-pointer",
    "focus:outline-none focus:ring-2 focus:ring-offset-0",
    "disabled:cursor-not-allowed disabled:opacity-50",
    props.error
      ? "border-danger-300 text-danger-900 focus:border-danger-500 focus:ring-danger-500 dark:border-danger-600 dark:text-danger-400"
      : "border-secondary-300 text-secondary-900 focus:border-primary-500 focus:ring-primary-500 dark:border-secondary-600 dark:bg-secondary-800 dark:text-secondary-100",
    props.class,
  ),
);
</script>

<template>
  <div ref="wrapperRef" class="space-y-1 relative">
    <label
      v-if="label"
      :for="selectId"
      class="block text-sm font-medium text-secondary-700 dark:text-secondary-300"
    >
      {{ label }}
    </label>
    <button
      data-nie-control="select"
      :id="selectId"
      type="button"
      :class="triggerClasses"
      :disabled="disabled"
      role="combobox"
      :aria-expanded="isOpen"
      aria-haspopup="listbox"
      :aria-invalid="error ? 'true' : undefined"
      :aria-describedby="error ? errorId : undefined"
      @click="toggle"
      @keydown="onKeydown"
    >
      <span :class="selectedLabel ? '' : 'text-secondary-400'">
        {{ selectedLabel || placeholder }}
      </span>
      <svg
        class="h-4 w-4 text-secondary-400 transition-transform duration-200"
        :class="{ 'rotate-180': isOpen }"
        fill="none"
        viewBox="0 0 24 24"
        stroke="currentColor"
      >
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="2"
          d="M19 9l-7 7-7-7"
        />
      </svg>
    </button>

    <Transition
      enter-active-class="transition ease-out duration-150"
      enter-from-class="opacity-0 translate-y-1"
      enter-to-class="opacity-100 translate-y-0"
      leave-active-class="transition ease-in duration-100"
      leave-from-class="opacity-100 translate-y-0"
      leave-to-class="opacity-0 translate-y-1"
    >
      <div
        v-if="isOpen"
        class="absolute z-50 mt-1 w-full rounded-[var(--theme-radius-panel)] border border-secondary-200 bg-white shadow-[var(--theme-shadow-float)] dark:border-secondary-600 dark:bg-secondary-800 overflow-hidden"
      >
        <!-- Search -->
        <div
          v-if="searchable"
          class="p-2 border-b border-secondary-100 dark:border-secondary-700"
        >
          <input
            ref="searchInputRef"
            v-model="search"
            type="text"
            class="w-full rounded-lg border border-secondary-200 bg-secondary-50 px-3 py-1.5 text-sm outline-none focus:border-primary-500 focus:ring-1 focus:ring-primary-500 dark:border-secondary-600 dark:bg-secondary-700 dark:text-secondary-100"
            placeholder="Search..."
            :aria-label="`Search ${label || placeholder}`"
            @keydown="onKeydown"
          />
        </div>
        <!-- Options -->
        <ul ref="listRef" role="listbox" class="max-h-60 overflow-y-auto py-1">
          <li
            v-for="(option, index) in filteredOptions"
            :key="option.value"
            role="option"
            :aria-selected="String(modelValue) === String(option.value)"
            :data-highlighted="highlightedIndex === index ? '' : undefined"
            class="flex items-center gap-2 px-3 py-2 text-sm cursor-pointer transition-colors"
            :class="[
              option.disabled
                ? 'opacity-50 cursor-not-allowed'
                : highlightedIndex === index
                  ? 'bg-primary-50 text-primary-700 dark:bg-primary-900/30 dark:text-primary-300'
                  : 'hover:bg-secondary-50 dark:hover:bg-secondary-700',
              String(modelValue) === String(option.value)
                ? 'font-semibold'
                : '',
            ]"
            @click="!option.disabled && selectOption(option)"
            @mouseenter="highlightedIndex = index"
          >
            <span class="flex-1">{{ option.label }}</span>
            <svg
              v-if="String(modelValue) === String(option.value)"
              class="h-4 w-4 text-primary-500"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M5 13l4 4L19 7"
              />
            </svg>
          </li>
          <li
            v-if="filteredOptions.length === 0"
            class="px-3 py-4 text-sm text-center text-secondary-400"
          >
            No options found.
          </li>
        </ul>
      </div>
    </Transition>

    <p
      v-if="error"
      :id="errorId"
      role="alert"
      class="text-sm text-danger-600 dark:text-danger-400"
    >
      {{ error }}
    </p>
  </div>
</template>

<style scoped>
.nie-select-trigger {
  box-sizing: border-box;
  min-height: var(--theme-control-height-md);
  padding-block: var(--theme-space-2);
  border-radius: var(--theme-radius-control);
  font-size: var(--theme-font-size-body);
  line-height: 1.25rem;
}
</style>
