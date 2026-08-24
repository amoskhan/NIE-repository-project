<script setup lang="ts">
import { ArrowLeftIcon } from "@heroicons/vue/24/outline";
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

function returnToLogin(): void {
  void router.push({ name: "login" });
}
</script>

<template>
  <main class="min-h-dvh bg-white dark:bg-secondary-950">
    <NieResultState :status-code="statusCode" class="min-h-dvh">
      <template #actions>
        <NieButton @click="returnToLogin">
          <ArrowLeftIcon class="h-4 w-4" />
          Return to sign in
        </NieButton>
      </template>
    </NieResultState>
  </main>
</template>
