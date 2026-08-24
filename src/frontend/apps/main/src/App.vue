<script setup lang="ts">
import { ArrowPathIcon } from "@heroicons/vue/24/outline";
import { NieAppFeedbackHub, NieButton, NieResultState } from "@nie/ui";
import { onErrorCaptured, shallowRef } from "vue";
import { RouterView } from "vue-router";

const hasUnhandledError = shallowRef(false);

onErrorCaptured(() => {
  hasUnhandledError.value = true;
});

function reloadApplication(): void {
  window.location.reload();
}
</script>

<template>
  <NieResultState
    v-if="hasUnhandledError"
    :status-code="500"
    class="min-h-dvh"
    title="Something went wrong"
    description="The application could not finish loading this screen. Reload to try again."
  >
    <template #actions>
      <NieButton @click="reloadApplication">
        <ArrowPathIcon class="h-4 w-4" />
        Reload application
      </NieButton>
    </template>
  </NieResultState>

  <RouterView v-else v-slot="{ Component }">
    <transition name="fade" mode="out-in">
      <component :is="Component" />
    </transition>
  </RouterView>
  <NieAppFeedbackHub v-if="!hasUnhandledError" />
</template>
