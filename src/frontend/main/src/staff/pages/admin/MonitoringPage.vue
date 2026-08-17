<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { AppButton } from "@apptemplate/ui";
import { FRONTEND_CONSTANTS } from "@apptemplate/shared";

type HealthStatus = "checking" | "healthy" | "degraded" | "offline";

interface HealthCheckResult {
  id: string;
  label: string;
  url: string;
  status: HealthStatus;
  details: string;
}

const healthChecks = ref<HealthCheckResult[]>([]);

const sentryDsn = FRONTEND_CONSTANTS.sentry.dsn;
const sentryEnvironment = FRONTEND_CONSTANTS.sentry.environment;
const apiBaseUrl = computed(() => FRONTEND_CONSTANTS.backend.main);

const sentryHost = computed(() => {
  if (!sentryDsn) {
    return "Not configured";
  }

  try {
    return new URL(sentryDsn).host;
  } catch {
    return "Invalid DSN";
  }
});

function statusClass(status: HealthStatus): string {
  switch (status) {
    case "healthy":
      return "bg-emerald-100 text-emerald-700";
    case "degraded":
      return "bg-amber-100 text-amber-700";
    case "offline":
      return "bg-rose-100 text-rose-700";
    default:
      return "bg-slate-100 text-slate-600";
  }
}

async function refreshHealthChecks() {
  const endpoints = [
    {
      id: "main-api-health",
      label: "Main API health",
      url: `${apiBaseUrl.value}/health`,
    },
    {
      id: "main-api-ready",
      label: "Main API ready",
      url: `${apiBaseUrl.value}/health`,
    },
    {
      id: "main-api-live",
      label: "Main API live",
      url: `${apiBaseUrl.value}/health/live`,
    },
  ].filter((endpoint) => endpoint.url);

  healthChecks.value = endpoints.map((endpoint) => ({
    ...endpoint,
    status: "checking",
    details: "Running check...",
  }));

  await Promise.all(
    endpoints.map(async (endpoint) => {
      try {
        const response = await fetch(endpoint.url, {
          method: "GET",
          credentials: "include",
        });

        healthChecks.value = healthChecks.value.map((result) =>
          result.id === endpoint.id
            ? {
                ...result,
                status: response.ok ? "healthy" : "degraded",
                details: response.ok
                  ? `HTTP ${response.status}`
                  : response.status === 401
                    ? "Authentication required"
                    : `HTTP ${response.status}`,
              }
            : result,
        );
      } catch {
        healthChecks.value = healthChecks.value.map((result) =>
          result.id === endpoint.id
            ? {
                ...result,
                status: "offline",
                details: "Request failed",
              }
            : result,
        );
      }
    }),
  );
}

onMounted(() => {
  void refreshHealthChecks();
});
</script>

<template>
  <div class="space-y-6">
    <section class="grid gap-4 xl:grid-cols-3">
      <div class="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <p class="text-xs font-bold uppercase tracking-[0.24em] text-slate-400">
          Error Monitoring
        </p>
        <h2 class="mt-3 text-xl font-bold text-slate-900">
          {{ sentryDsn ? "Enabled" : "Disabled" }}
        </h2>
        <p class="mt-2 text-sm text-slate-500">Host: {{ sentryHost }}</p>
      </div>

      <div class="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <p class="text-xs font-bold uppercase tracking-[0.24em] text-slate-400">
          Environment
        </p>
        <h2 class="mt-3 text-xl font-bold text-slate-900">
          {{ sentryEnvironment }}
        </h2>
        <p class="mt-2 text-sm text-slate-500">
          Matches the frontend Sentry environment configuration.
        </p>
      </div>

      <div class="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
        <p class="text-xs font-bold uppercase tracking-[0.24em] text-slate-400">
          Health Endpoint Base
        </p>
        <h2 class="mt-3 text-xl font-bold text-slate-900">
          {{ apiBaseUrl || "Unavailable" }}
        </h2>
        <p class="mt-2 text-sm text-slate-500">
          Used for local uptime probes from the admin console.
        </p>
      </div>
    </section>

    <section class="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
      <div
        class="flex flex-col gap-2 md:flex-row md:items-center md:justify-between"
      >
        <div>
          <h2 class="text-lg font-bold text-slate-900">Uptime Checks</h2>
          <p class="mt-1 text-sm text-slate-500">
            Review backend readiness and liveness probes used for Sentry uptime
            and operational monitoring.
          </p>
        </div>

        <AppButton variant="outline" @click="refreshHealthChecks">
          Refresh Checks
        </AppButton>
      </div>

      <div class="mt-6 grid gap-4 xl:grid-cols-3">
        <article
          v-for="check in healthChecks"
          :key="check.id"
          class="rounded-2xl border border-slate-200 p-4"
        >
          <div class="flex items-start justify-between gap-3">
            <div>
              <h3 class="text-sm font-semibold text-slate-900">
                {{ check.label }}
              </h3>
              <p class="mt-1 break-all text-xs text-slate-500">
                {{ check.url }}
              </p>
            </div>
            <span
              class="rounded-full px-2.5 py-1 text-xs font-bold"
              :class="statusClass(check.status)"
            >
              {{ check.status }}
            </span>
          </div>
          <p class="mt-4 text-sm text-slate-500">{{ check.details }}</p>
        </article>
      </div>
    </section>
  </div>
</template>
