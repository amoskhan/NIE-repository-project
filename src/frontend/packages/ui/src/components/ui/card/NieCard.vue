<script setup lang="ts">
import { computed } from "vue";
import { cn } from "../../../lib/utils";

interface Props {
  class?: string;
  padding?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  padding: true,
});

const cardClasses = computed(() =>
  cn(
    "rounded-[var(--theme-radius-panel)] border border-secondary-200 bg-white shadow-[var(--theme-shadow-soft)] dark:border-secondary-700 dark:bg-secondary-800",
    props.class
  )
);
</script>

<template>
  <div :class="cardClasses">
    <div v-if="$slots.header" class="border-b border-secondary-200 px-6 py-4 dark:border-secondary-700">
      <slot name="header"></slot>
    </div>
    <div :class="[padding ? 'p-6' : '']">
      <slot></slot>
    </div>
    <div v-if="$slots.footer" class="border-t border-secondary-200 px-6 py-4 dark:border-secondary-700">
      <slot name="footer"></slot>
    </div>
  </div>
</template>
