<script setup lang="ts">
import { onErrorCaptured, shallowRef } from "vue";
import { RouterView } from "vue-router";
import { ArrowPathIcon } from "@heroicons/vue/24/outline";
import { NieButton, NieResultState } from "@nie/ui";

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
  >
    <template #actions>
      <NieButton @click="reloadApplication">
        <ArrowPathIcon class="h-4 w-4" />
        Reload application
      </NieButton>
    </template>
  </NieResultState>
  <RouterView v-else />
</template>
