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

const inputClasses = computed(() =>
  cn(
    "block min-h-11 w-full rounded-xl border px-3 py-2.5 text-sm transition-colors duration-200",
    "focus:outline-none focus:ring-2 focus:ring-offset-0",
    "disabled:cursor-not-allowed disabled:opacity-50",
    props.error
      ? "border-red-300 text-red-900 placeholder-red-300 focus:border-red-500 focus:ring-red-500 dark:border-red-600 dark:text-red-400"
      : "border-secondary-300 text-secondary-900 placeholder-secondary-400 focus:border-primary-500 focus:ring-primary-500 dark:border-secondary-600 dark:bg-secondary-800 dark:text-secondary-100 dark:placeholder-secondary-500",
    props.class,
  ),
);

const handleInput = (event: Event) => {
  const target = event.target as HTMLInputElement;
  const value = props.type === "number" ? Number(target.value) : target.value;
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
      :id="inputId"
      :type="type"
      :value="modelValue ?? ''"
      :placeholder="placeholder"
      :disabled="disabled"
      :readonly="readonly"
      :class="inputClasses"
      @input="handleInput"
      @blur="emit('blur', $event)"
      @focus="emit('focus', $event)"
    />
    <p v-if="error" class="text-sm text-red-600 dark:text-red-400">
      {{ error }}
    </p>
  </div>
</template>
