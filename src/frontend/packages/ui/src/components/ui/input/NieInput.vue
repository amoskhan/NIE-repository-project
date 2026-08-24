<script setup lang="ts">
import { computed } from "vue";
import { cn } from "../../../lib/utils";

interface Props {
  modelValue?: string | number | null;
  type?:
    | "text"
    | "email"
    | "password"
    | "number"
    | "tel"
    | "url"
    | "search"
    | "date"
    | "datetime-local"
    | "time";
  placeholder?: string;
  disabled?: boolean;
  readonly?: boolean;
  error?: string;
  label?: string;
  id?: string;
  class?: string;
  min?: string | number;
  max?: string | number;
  step?: string | number;
  maxlength?: number;
  autocomplete?: string;
}

const props = withDefaults(defineProps<Props>(), {
  type: "text",
  disabled: false,
  readonly: false,
});

const emit = defineEmits<{
  "update:modelValue": [value: string | number | null];
  blur: [event: FocusEvent];
  focus: [event: FocusEvent];
}>();

const inputId = computed(
  () => props.id || `input-${Math.random().toString(36).slice(2, 9)}`,
);
const errorId = computed(() => `${inputId.value}-error`);

const inputClasses = computed(() =>
  cn(
    "nie-input-control block min-h-11 w-full rounded-[var(--theme-radius-control)] border px-3 py-2 text-sm transition-colors duration-200",
    "focus:outline-none focus:ring-2 focus:ring-offset-0",
    "disabled:cursor-not-allowed disabled:opacity-50",
    props.error
      ? "border-danger-300 text-danger-900 placeholder-danger-300 focus:border-danger-500 focus:ring-danger-500 dark:border-danger-600 dark:text-danger-400"
      : "border-secondary-300 text-secondary-900 placeholder-secondary-400 focus:border-primary-500 focus:ring-primary-500 dark:border-secondary-600 dark:bg-secondary-800 dark:text-secondary-100 dark:placeholder-secondary-500",
    props.class,
  ),
);

const handleInput = (event: Event) => {
  const target = event.target as HTMLInputElement;
  const value =
    props.type === "number"
      ? target.value === ""
        ? null
        : Number(target.value)
      : target.value;
  emit("update:modelValue", value);
};
</script>

<template>
  <div class="space-y-1">
    <label
      v-if="label"
      :for="inputId"
      class="block text-sm font-medium text-secondary-700 dark:text-secondary-300"
    >
      {{ label }}
    </label>
    <input
      data-nie-control="input"
      :id="inputId"
      :type="type"
      :value="modelValue ?? ''"
      :placeholder="placeholder"
      :disabled="disabled"
      :readonly="readonly"
      :min="min"
      :max="max"
      :step="step"
      :maxlength="maxlength"
      :autocomplete="autocomplete"
      :class="inputClasses"
      :aria-invalid="error ? 'true' : undefined"
      :aria-describedby="error ? errorId : undefined"
      @input="handleInput"
      @blur="emit('blur', $event)"
      @focus="emit('focus', $event)"
    />
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
.nie-input-control {
  box-sizing: border-box;
  min-height: var(--theme-control-height-md);
  padding-block: var(--theme-space-2);
  border-radius: var(--theme-radius-control);
  font-size: var(--theme-font-size-body);
  line-height: 1.25rem;
}
</style>
