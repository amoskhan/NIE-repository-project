<script setup lang="ts">
import NieLoaderSymbol from "./NieLoaderSymbol.vue";

interface Props {
  show?: boolean;
  message?: string;
  fullscreen?: boolean;
}

withDefaults(defineProps<Props>(), {
  show: true,
  message: "Loading...",
  fullscreen: false,
});
</script>

<template>
  <Transition name="fade">
    <div
      v-if="show"
      :class="[
        'flex items-center justify-center bg-white/80 dark:bg-secondary-900/80 backdrop-blur-sm',
        fullscreen ? 'fixed inset-0 z-50' : 'absolute inset-0 rounded-lg',
      ]"
    >
      <div class="flex flex-col items-center gap-3">
        <NieLoaderSymbol
          size="lg"
          variant="brand"
          :label="message || 'Loading'"
        />
        <p v-if="message" class="text-sm text-secondary-600 dark:text-secondary-400">
          {{ message }}
        </p>
      </div>
    </div>
  </Transition>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
