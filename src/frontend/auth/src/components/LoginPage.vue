<script setup lang="ts">
// PROJECT-OWNED — safe to edit.
//
// Sign-in screen for the LOCAL identity provider.
//
// The wire contract is fixed: POST /api/Auth/Login with { userid, pd }. On success the
// Auth API returns an IssuedLoginResponse and we hand off to the main app by writing the
// AppTemplate-SessionToken / AppTemplate-User cookies (see services/session.ts).
//
// External OIDC providers are OPTIONAL and ship disabled: the buttons only appear when
// GET /api/Auth/ExternalProviders answers with a non-empty list.

import { computed, onMounted, ref } from "vue";
import AuthShell from "./AuthShell.vue";
import AuthMessage from "./AuthMessage.vue";
import {
  getExternalProviders,
  isDevelopmentEnvironment,
  login,
  startExternalLogin,
  type ExternalProvider,
} from "../services/authApi";
import {
  clearAuthCookies,
  completeLogin,
  revokeExistingSession,
} from "../services/session";

const username = ref("");
const password = ref("");
const showPassword = ref(false);
const isLoading = ref(false);
const errorMessage = ref("");

const externalProviders = ref<ExternalProvider[]>([]);
const pendingProvider = ref("");

const isBusy = computed(
  () => isLoading.value || Boolean(pendingProvider.value),
);
const hasExternalProviders = computed(() => externalProviders.value.length > 0);

// ── Seeded dev-account hint ────────────────────────────────────────────────────
// Students need to know how to get in on a fresh checkout. Shown ONLY on local/dev
// deployments (see isDevelopmentEnvironment) and dismissible, with the choice
// remembered so it does not nag on every reload.
const DEV_HINT_DISMISSED_KEY = "apptemplate_dev_credentials_hint_dismissed";
const showDevHint = ref(false);

function readDevHintPreference(): boolean {
  try {
    return localStorage.getItem(DEV_HINT_DISMISSED_KEY) === "true";
  } catch {
    return false;
  }
}

function dismissDevHint(): void {
  showDevHint.value = false;
  try {
    localStorage.setItem(DEV_HINT_DISMISSED_KEY, "true");
  } catch {
    // Private browsing can block storage; dismissing for this page view is enough.
  }
}

/** Prefill the form from the seeded dev account so the hint is one click to use. */
function useDevCredentials(): void {
  username.value = "admin";
  password.value = "Admin@12345";
}

const handleLogin = async () => {
  if (isBusy.value) {
    return;
  }

  isLoading.value = true;
  errorMessage.value = "";

  const result = await login(username.value, password.value);

  if (result.ok && result.data?.isAuthenticated) {
    completeLogin(result.data);
    return;
  }

  errorMessage.value =
    result.message || "Login failed. Please check your credentials.";
  isLoading.value = false;
};

/** Hand the browser to the external IdP, asking it to come back to this login page. */
const handleExternalLogin = (provider: ExternalProvider) => {
  if (isBusy.value) {
    return;
  }

  pendingProvider.value = provider.name;
  startExternalLogin(provider, window.location.href);
};

onMounted(async () => {
  // Landing on the login page always ends whatever session was left behind.
  void revokeExistingSession();
  clearAuthCookies();

  showDevHint.value = isDevelopmentEnvironment() && !readDevHintPreference();

  // A failure here must not block password login, so getExternalProviders() swallows
  // errors and returns [] — which simply hides the section.
  externalProviders.value = await getExternalProviders();
});
</script>

<template>
  <AuthShell title="Sign In" subtitle="Enter your credentials to continue.">
    <AuthMessage :text="errorMessage" tone="error" spaced />

    <transition name="auth-fade">
      <div
        v-if="showDevHint"
        class="mb-6 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800"
        data-testid="dev-credentials-hint"
      >
        <div class="flex items-start gap-2">
          <span class="material-symbols-outlined text-[18px]">science</span>
          <div class="flex-1">
            <p class="font-semibold">Development environment</p>
            <p class="mt-1">
              Seeded account:
              <code class="rounded bg-amber-100 px-1 font-mono">admin</code> /
              <code class="rounded bg-amber-100 px-1 font-mono"
                >Admin@12345</code
              >
            </p>
            <button
              type="button"
              class="auth-link mt-2 text-xs"
              @click="useDevCredentials"
            >
              Fill the form with these
            </button>
          </div>
          <button
            type="button"
            class="rounded p-1 text-amber-500 hover:bg-amber-100"
            aria-label="Dismiss development credentials hint"
            @click="dismissDevHint"
          >
            <span class="material-symbols-outlined text-[18px]">close</span>
          </button>
        </div>
      </div>
    </transition>

    <form class="auth-form" @submit.prevent="handleLogin">
      <div>
        <label for="username" class="auth-field-label">Username</label>
        <div class="auth-input-shell">
          <span class="material-symbols-outlined text-[18px] text-slate-400"
            >person</span
          >
          <input
            id="username"
            v-model="username"
            type="text"
            name="username"
            class="auth-input"
            placeholder="Enter your username"
            autocomplete="username"
            required
          />
        </div>
      </div>

      <div>
        <label for="password" class="auth-field-label">Password</label>
        <div class="auth-input-shell">
          <span class="material-symbols-outlined text-[18px] text-slate-400"
            >lock</span
          >
          <input
            id="password"
            v-model="password"
            :type="showPassword ? 'text' : 'password'"
            name="password"
            class="auth-input"
            placeholder="Enter your password"
            autocomplete="current-password"
            required
          />
          <button
            type="button"
            class="password-toggle flex items-center justify-center text-slate-400 transition-colors hover:text-slate-600"
            :aria-label="showPassword ? 'Hide password' : 'Show password'"
            tabindex="-1"
            @click="showPassword = !showPassword"
          >
            <span class="material-symbols-outlined text-[18px]">{{
              showPassword ? "visibility_off" : "visibility"
            }}</span>
          </button>
        </div>
      </div>

      <div class="-mt-2 text-right">
        <RouterLink to="/forgot-password" class="auth-link text-sm">
          Forgot password?
        </RouterLink>
      </div>

      <button type="submit" class="auth-submit" :disabled="isBusy">
        <span v-if="!isLoading">Login</span>
        <span v-else class="auth-spinner"></span>
        <span v-if="!isLoading" class="material-symbols-outlined text-[20px]"
          >arrow_forward</span
        >
      </button>

      <!-- Rendered ONLY when the backend advertises at least one external provider. -->
      <template v-if="hasExternalProviders">
        <div class="mt-1 auth-divider"><span>Or</span></div>

        <button
          v-for="provider in externalProviders"
          :key="provider.name"
          type="button"
          class="auth-provider-button"
          :disabled="isBusy"
          :data-testid="`external-provider-${provider.name}`"
          @click="handleExternalLogin(provider)"
        >
          <span v-if="pendingProvider !== provider.name">
            {{ provider.displayName || provider.name }}
          </span>
          <span
            v-else
            class="auth-spinner border-slate-400 border-t-slate-700"
          ></span>
          <span
            v-if="pendingProvider !== provider.name"
            class="material-symbols-outlined text-[20px]"
            >open_in_new</span
          >
        </button>
      </template>
    </form>

    <template #footer>
      <span class="text-slate-500">No account yet?</span>
      <RouterLink to="/register" class="auth-link ml-1">Create one</RouterLink>
    </template>
  </AuthShell>
</template>
