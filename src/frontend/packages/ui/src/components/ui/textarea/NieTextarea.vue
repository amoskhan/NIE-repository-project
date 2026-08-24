<script setup lang="ts">
import { computed, useId } from "vue";
import { cn } from "../../../lib/utils";

interface Props {
  id?: string;
  label?: string;
  placeholder?: string;
  disabled?: boolean;
  readonly?: boolean;
  required?: boolean;
  rows?: number;
  maxlength?: number;
  spellcheck?: boolean;
  error?: string;
  hint?: string;
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  disabled: false,
  readonly: false,
  required: false,
  rows: 4,
});

const model = defineModel<string>({ default: "" });
const generatedId = useId();
const textareaId = computed(() => props.id ?? `textarea-${generatedId}`);
const errorId = computed(() => `${textareaId.value}-error`);
const hintId = computed(() => `${textareaId.value}-hint`);
const describedBy = computed(() => {
  if (props.error) return errorId.value;
  if (props.hint) return hintId.value;
  return undefined;
});

const textareaClasses = computed(() =>
  cn(
    "nie-textarea-control block min-h-24 w-full resize-y rounded-[var(--theme-radius-control)] border px-3 py-2 text-sm transition-colors duration-200",
    "focus:outline-none focus:ring-2 focus:ring-offset-0",
    "disabled:cursor-not-allowed disabled:opacity-50",
    props.error
      ? "border-danger-300 text-danger-900 placeholder-danger-300 focus:border-danger-500 focus:ring-danger-500 dark:border-danger-600 dark:text-danger-400"
      : "border-secondary-300 text-secondary-900 placeholder-secondary-400 focus:border-primary-500 focus:ring-primary-500 dark:border-secondary-600 dark:bg-secondary-800 dark:text-secondary-100 dark:placeholder-secondary-500",
    props.class,
  ),
);
</script>

<template>
  <div class="space-y-1">
    <label
      v-if="label"
      :for="textareaId"
      class="block text-sm font-medium text-secondary-700 dark:text-secondary-300"
    >
      {{ label }}
    </label>
    <textarea
      data-nie-control="textarea"
      :id="textareaId"
      v-model="model"
      :placeholder="placeholder"
      :disabled="disabled"
      :readonly="readonly"
      :required="required"
      :rows="rows"
      :maxlength="maxlength"
      :spellcheck="spellcheck"
      :class="textareaClasses"
      :aria-invalid="error ? 'true' : undefined"
      :aria-describedby="describedBy"
    ></textarea>
    <p
      v-if="error"
      :id="errorId"
      role="alert"
      class="text-sm text-danger-600 dark:text-danger-400"
    >
      {{ error }}
    </p>
    <p
      v-else-if="hint"
      :id="hintId"
      class="text-xs text-secondary-500 dark:text-secondary-400"
    >
      {{ hint }}
    </p>
  </div>
</template>

<style scoped>
.nie-textarea-control {
  box-sizing: border-box;
  padding-block: var(--theme-space-2);
  border-radius: var(--theme-radius-control);
  font-size: var(--theme-font-size-body);
  line-height: 1.5;
}
</style>
