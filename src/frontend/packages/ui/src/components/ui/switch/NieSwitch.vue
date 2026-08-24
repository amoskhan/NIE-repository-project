<script setup lang="ts">
import { computed } from "vue";
import { cn } from "../../../lib/utils";

type SwitchSize = "sm" | "md" | "lg";

interface Props {
  modelValue: boolean;
  disabled?: boolean;
  label?: string;
  ariaLabel?: string;
  size?: SwitchSize;
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  disabled: false,
  size: "md",
});

const emit = defineEmits<{
  "update:modelValue": [value: boolean];
}>();

const toggle = () => {
  if (!props.disabled) {
    emit("update:modelValue", !props.modelValue);
  }
};

const switchSizeClasses: Record<SwitchSize, string> = {
  sm: "h-5 w-9 px-0.5",
  md: "h-6 w-11 px-0.5",
  lg: "h-11 w-[4.25rem] px-1",
};

const knobSizeClasses: Record<SwitchSize, string> = {
  sm: "h-4 w-4",
  md: "h-5 w-5",
  lg: "h-7 w-7",
};

const knobTranslateClasses: Record<SwitchSize, string> = {
  sm: "translate-x-4",
  md: "translate-x-5",
  lg: "translate-x-8",
};

const switchClasses = computed(() =>
  cn(
    "relative inline-flex flex-shrink-0 cursor-pointer items-center rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2",
    switchSizeClasses[props.size],
    props.modelValue ? "bg-primary-600" : "bg-secondary-200 dark:bg-secondary-700",
    props.disabled ? "opacity-50 cursor-not-allowed" : "",
    props.class
  )
);

const knobClasses = computed(() =>
  cn(
    "pointer-events-none inline-block transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out",
    knobSizeClasses[props.size],
    props.modelValue ? knobTranslateClasses[props.size] : "translate-x-0"
  )
);

const resolvedAriaLabel = computed(
  () => props.ariaLabel ?? props.label ?? "Toggle setting"
);
</script>

<template>
  <label class="inline-flex min-h-11 items-center gap-3">
    <button
      type="button"
      role="switch"
      :aria-checked="modelValue"
      :aria-label="label ? undefined : resolvedAriaLabel"
      :class="switchClasses"
      :disabled="disabled"
      @click="toggle"
    >
      <span :class="knobClasses"></span>
    </button>
    <span v-if="label" class="text-sm text-secondary-700 dark:text-secondary-300">
      {{ label }}
    </span>
  </label>
</template>
