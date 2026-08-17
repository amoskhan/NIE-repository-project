<script setup lang="ts">
// PROJECT-OWNED — safe to edit.
//
// Step 2 of password recovery: exchange the single-use token from the reset email for
// a new password. POST /api/Auth/ResetPassword { token, newPassword }
//
// The token is self-identifying: the backend finds the account from the token hash alone,
// so the user never has to type (and this page must never send) a user id.
//
// The token arrives in the URL. This app uses hash routing, so the canonical link is
//   .../login/#/reset-password?token=<token>
// but we also accept a plain ?token= before the hash, because mail clients and reverse
// proxies rewrite links in surprising ways.

import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import AuthShell from "./AuthShell.vue";
import AuthMessage from "./AuthMessage.vue";
import { resetPassword } from "../services/authApi";

const route = useRoute();
const router = useRouter();

const token = ref("");
const password = ref("");
const confirmPassword = ref("");
const showPassword = ref(false);
const isLoading = ref(false);
const errorMessage = ref("");
const isComplete = ref(false);

/** Mirror of the backend password policy — keep the two in step when you change either. */
const PASSWORD_MIN_LENGTH = 10;

const passwordChecks = computed(() => {
  const value = password.value;
  return [
    {
      label: `At least ${PASSWORD_MIN_LENGTH} characters`,
      met: value.length >= PASSWORD_MIN_LENGTH,
    },
    { label: "An uppercase letter", met: /[A-Z]/.test(value) },
    { label: "A lowercase letter", met: /[a-z]/.test(value) },
    { label: "A number", met: /\d/.test(value) },
  ];
});

const isPasswordStrong = computed(() =>
  passwordChecks.value.every((check) => check.met),
);

onMounted(() => {
  const fromRoute = route.query.token;
  const fromSearch = new URLSearchParams(window.location.search).get("token");
  token.value =
    (typeof fromRoute === "string" ? fromRoute : null) ?? fromSearch ?? "";
});

const handleSubmit = async () => {
  if (isLoading.value) {
    return;
  }

  if (!isPasswordStrong.value) {
    errorMessage.value =
      "Your password does not meet all the requirements below.";
    return;
  }

  if (password.value !== confirmPassword.value) {
    errorMessage.value = "Passwords do not match.";
    return;
  }

  isLoading.value = true;
  errorMessage.value = "";

  const result = await resetPassword({
    token: token.value,
    newPassword: password.value,
  });

  isLoading.value = false;

  if (!result.ok) {
    errorMessage.value = result.message;
    return;
  }

  isComplete.value = true;
  window.setTimeout(() => router.push("/"), 2000);
};
</script>

<template>
  <AuthShell
    title="Reset Password"
    subtitle="Choose a new password for your account."
  >
    <!-- No token at all: the user opened this page directly. -->
    <div v-if="!token && !isComplete">
      <AuthMessage
        tone="error"
        text="This page needs a reset link. Request a fresh one to continue."
      />
      <RouterLink
        to="/forgot-password"
        class="auth-submit mt-6 w-full no-underline"
      >
        Request a reset link
      </RouterLink>
    </div>

    <div v-else-if="isComplete" data-testid="reset-password-complete">
      <AuthMessage
        tone="success"
        text="Your password has been updated. Taking you to sign in…"
      />
    </div>

    <form v-else class="auth-form" @submit.prevent="handleSubmit">
      <AuthMessage :text="errorMessage" tone="error" />

      <div>
        <label for="reset-password" class="auth-field-label"
          >New password</label
        >
        <div class="auth-input-shell">
          <span class="material-symbols-outlined text-[18px] text-slate-400"
            >lock</span
          >
          <input
            id="reset-password"
            v-model="password"
            :type="showPassword ? 'text' : 'password'"
            class="auth-input"
            placeholder="Enter a new password"
            autocomplete="new-password"
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
        <ul class="mt-2 grid grid-cols-1 gap-1 sm:grid-cols-2">
          <li
            v-for="check in passwordChecks"
            :key="check.label"
            class="flex items-center gap-1 text-xs"
            :class="check.met ? 'text-emerald-600' : 'text-slate-400'"
          >
            <span class="material-symbols-outlined text-[14px]">{{
              check.met ? "check_circle" : "radio_button_unchecked"
            }}</span>
            {{ check.label }}
          </li>
        </ul>
      </div>

      <div>
        <label for="reset-confirm" class="auth-field-label">
          Confirm new password
        </label>
        <div class="auth-input-shell">
          <span class="material-symbols-outlined text-[18px] text-slate-400"
            >lock_reset</span
          >
          <input
            id="reset-confirm"
            v-model="confirmPassword"
            :type="showPassword ? 'text' : 'password'"
            class="auth-input"
            placeholder="Repeat the new password"
            autocomplete="new-password"
            required
          />
        </div>
      </div>

      <button type="submit" class="auth-submit" :disabled="isLoading">
        <span v-if="!isLoading">Update password</span>
        <span v-else class="auth-spinner"></span>
        <span v-if="!isLoading" class="material-symbols-outlined text-[20px]"
          >check</span
        >
      </button>
    </form>

    <template #footer>
      <RouterLink to="/" class="auth-link">Back to sign in</RouterLink>
    </template>
  </AuthShell>
</template>
