<script setup lang="ts">
import { computed, onMounted } from "vue";
import { useRoute } from "vue-router";

const route = useRoute();

const authCode = computed(
  () => (route.query.code as string) || (route.query.authCode as string) || "",
);
const state = computed(() => (route.query.state as string) || "");
const error = computed(
  () =>
    (route.query.error as string) ||
    (route.query.error_description as string) ||
    "",
);

onMounted(() => {
  if (window.opener) {
    window.opener.postMessage(
      {
        type: "application-auth-callback",
        code: authCode.value,
        state: state.value,
        error: error.value,
      },
      window.location.origin,
    );
  }
  window.close();
});
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-secondary-50 px-6">
    <div
      class="max-w-md w-full rounded-2xl bg-white border border-secondary-200 p-8 shadow-[var(--theme-shadow-soft)] text-center"
    >
      <div
        class="mx-auto size-12 rounded-full bg-accent/10 text-accent flex items-center justify-center mb-4"
      >
        <span class="material-symbols-outlined text-page-title">lock</span>
      </div>
      <h1 class="text-lg font-bold text-secondary-900">
        Returning to NIE Template
      </h1>
      <p class="mt-2 text-sm text-secondary-500">
        This window will close automatically after completing the authentication
        handoff.
      </p>
    </div>
  </div>
</template>
