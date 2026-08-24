<script setup lang="ts">
import { ArrowLeftIcon, HomeIcon } from "@heroicons/vue/24/outline";
import {
  NieButton,
  NieResultState,
  type NieResultStatus,
} from "@nie/ui";
import { useRouter } from "vue-router";

defineProps<{
  statusCode: NieResultStatus;
}>();

const router = useRouter();

function returnToDashboard(): void {
  void router.push({ name: "dashboard" });
}

function returnToPreviousPage(): void {
  if (window.history.length > 1) {
    router.back();
    return;
  }

  returnToDashboard();
}
</script>

<template>
  <NieResultState :status-code="statusCode">
    <template #actions>
      <NieButton @click="returnToDashboard">
        <HomeIcon class="h-4 w-4" />
        Return to dashboard
      </NieButton>
      <NieButton variant="outline" @click="returnToPreviousPage">
        <ArrowLeftIcon class="h-4 w-4" />
        Go back
      </NieButton>
    </template>
  </NieResultState>
</template>
