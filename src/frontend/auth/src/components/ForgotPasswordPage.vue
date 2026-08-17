<script setup lang="ts">
// PROJECT-OWNED — safe to edit.
//
// Step 1 of password recovery: ask the local identity provider to email a reset link.
// POST /api/Auth/ForgotPassword { userIdOrEmail }
//
// The backend accepts EITHER the login name or the email address (ForgotPasswordRequest
// .UserIdOrEmail), so this field is a free-text "username or email" — not type="email".
//
// SECURITY: the endpoint always answers 200, whether or not the account is registered,
// so nobody can use this form to enumerate accounts. The UI must therefore show the SAME
// confirmation either way — do not add a "no such user" branch.

import { ref } from "vue";
import AuthShell from "./AuthShell.vue";
import AuthMessage from "./AuthMessage.vue";
import { forgotPassword } from "../services/authApi";

const userIdOrEmail = ref("");
const isLoading = ref(false);
const errorMessage = ref("");
const isSubmitted = ref(false);

const handleSubmit = async () => {
  if (isLoading.value) {
    return;
  }

  isLoading.value = true;
  errorMessage.value = "";

  const result = await forgotPassword(userIdOrEmail.value.trim());

  isLoading.value = false;

  if (result.ok) {
    // Deliberately unconditional: same wording for known and unknown addresses.
    isSubmitted.value = true;
    return;
  }

  // A non-200 here means the service itself is unavailable, not "unknown email".
  errorMessage.value = result.message;
};
</script>

<template>
  <AuthShell
    title="Forgot Password"
    subtitle="Tell us who you are and we'll email you a link to choose a new password."
  >
    <AuthMessage :text="errorMessage" tone="error" spaced />

    <div v-if="isSubmitted" data-testid="forgot-password-sent">
      <AuthMessage
        tone="success"
        text="If that account exists, a reset link is on its way to the email address we hold for it. The link expires shortly, so use it soon."
      />
      <p class="mt-4 text-center text-sm text-slate-500">
        Didn't get anything? Check your spam folder, then
        <button type="button" class="auth-link" @click="isSubmitted = false">
          try again
        </button>
        .
      </p>
    </div>

    <form v-else class="auth-form" @submit.prevent="handleSubmit">
      <div>
        <label for="forgot-identifier" class="auth-field-label">
          Username or email
        </label>
        <div class="auth-input-shell">
          <span class="material-symbols-outlined text-[18px] text-slate-400"
            >mail</span
          >
          <input
            id="forgot-identifier"
            v-model="userIdOrEmail"
            type="text"
            class="auth-input"
            placeholder="e.g. jane or jane@example.edu"
            autocomplete="username"
            required
          />
        </div>
      </div>

      <button type="submit" class="auth-submit" :disabled="isLoading">
        <span v-if="!isLoading">Send reset link</span>
        <span v-else class="auth-spinner"></span>
        <span v-if="!isLoading" class="material-symbols-outlined text-[20px]"
          >send</span
        >
      </button>
    </form>

    <template #footer>
      <RouterLink to="/" class="auth-link">Back to sign in</RouterLink>
    </template>
  </AuthShell>
</template>
