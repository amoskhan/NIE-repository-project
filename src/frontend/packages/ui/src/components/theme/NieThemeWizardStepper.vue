<script setup lang="ts">
export interface ThemeWizardStep {
  id: string;
  label: string;
  hint?: string;
}

interface Props {
  steps: ThemeWizardStep[];
  activeStep: string;
}

defineProps<Props>();
</script>

<template>
  <ol
    class="wizard-stepper"
    :style="{ '--step-count': steps.length }"
  >
    <li
      v-for="(step, index) in steps"
      :key="step.id"
      class="flex items-start gap-3 rounded-2xl border border-secondary-200 bg-white/95 p-4 shadow-[var(--theme-shadow-soft)] dark:border-secondary-700 dark:bg-secondary-900/90"
      :class="step.id === activeStep ? 'ring-2 ring-primary-500/40' : ''"
    >
      <div
        class="flex h-10 w-10 shrink-0 items-center justify-center rounded-2xl text-sm font-bold"
        :class="
          step.id === activeStep
            ? 'bg-primary-600 text-on-brand dark:bg-primary-600'
            : index < steps.findIndex((item) => item.id === activeStep)
              ? 'bg-success-100 text-success-700 dark:bg-success-950/30 dark:text-success-300'
              : 'bg-secondary-100 text-secondary-500 dark:bg-secondary-800 dark:text-secondary-300'
        "
      >
        {{ index + 1 }}
      </div>

      <div class="min-w-0">
        <p class="text-sm font-semibold text-secondary-900 dark:text-secondary-50">
          {{ step.label }}
        </p>
        <p
          v-if="step.hint"
          class="mt-1 text-xs text-secondary-500 dark:text-secondary-400"
        >
          {{ step.hint }}
        </p>
      </div>
    </li>
  </ol>
</template>

<style scoped>
.wizard-stepper {
  display: grid;
  gap: var(--theme-space-3);
}

@media (min-width: 1024px) {
  .wizard-stepper {
    grid-template-columns: repeat(var(--step-count), minmax(0, 1fr));
  }
}
</style>
