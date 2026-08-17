<script setup lang="ts">
// PROJECT-OWNED — safe to edit.
//
// Self-service account creation against the LOCAL identity provider.
// POST /api/Auth/Register { userId, email, fullName, password, department? }
//
// The client-side checks below are a UX convenience only. The backend re-validates
// everything (uniqueness, password policy) and is the real gatekeeper — never rely on
// a browser check for a security rule.

import { computed, ref } from "vue";
import { useRouter } from "vue-router";
import AuthShell from "./AuthShell.vue";
import AuthMessage from "./AuthMessage.vue";
import { register } from "../services/authApi";

const router = useRouter();

const form = ref({
  userId: "",
  fullName: "",
  email: "",
  department: "",
  password: "",
  confirmPassword: "",
});

const showPassword = ref(false);
const isLoading = ref(false);
const errorMessage = ref("");
const successMessage = ref("");
const fieldErrors = ref<Record<string, string>>({});

/** Mirror of the backend password policy — keep the two in step when you change either. */
const PASSWORD_MIN_LENGTH = 10;

const passwordChecks = computed(() => {
  const value = form.value.password;
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

function validate(): boolean {
  const errors: Record<string, string> = {};
  const { userId, fullName, email, password, confirmPassword } = form.value;

  if (!userId.trim()) {
    errors.userId = "Choose a username.";
  } else if (!/^[a-zA-Z0-9._-]{3,50}$/.test(userId.trim())) {
    errors.userId =
      "Use 3-50 characters: letters, numbers, dot, underscore or hyphen.";
  }

  if (!fullName.trim()) {
    errors.fullName = "Enter your full name.";
  }

  // Intentionally permissive: the only authoritative email check is a delivered message.
  if (!/^\S+@\S+\.\S+$/.test(email.trim())) {
    errors.email = "Enter a valid email address.";
  }

  if (passwordChecks.value.some((check) => !check.met)) {
    errors.password = "Your password does not meet all the requirements below.";
  }

  if (password !== confirmPassword) {
    errors.confirmPassword = "Passwords do not match.";
  }

  fieldErrors.value = errors;
  return Object.keys(errors).length === 0;
}

const handleRegister = async () => {
  if (isLoading.value || !validate()) {
    return;
  }

  isLoading.value = true;
  errorMessage.value = "";
  successMessage.value = "";

  const result = await register({
    userId: form.value.userId.trim(),
    email: form.value.email.trim(),
    fullName: form.value.fullName.trim(),
    password: form.value.password,
    department: form.value.department.trim() || undefined,
  });

  isLoading.value = false;

  if (!result.ok) {
    errorMessage.value = result.message;
    return;
  }

  successMessage.value = "Account created. Taking you to sign in…";
  window.setTimeout(() => router.push("/"), 1500);
};
</script>

<template>
  <AuthShell
    title="Create Account"
    subtitle="Register for access with your email address."
  >
    <AuthMessage :text="errorMessage" tone="error" spaced />
    <AuthMessage :text="successMessage" tone="success" spaced />

    <form class="auth-form" @submit.prevent="handleRegister">
      <div>
        <label for="register-userid" class="auth-field-label">Username</label>
        <div class="auth-input-shell">
          <span class="material-symbols-outlined text-[18px] text-slate-400"
            >person</span
          >
          <input
            id="register-userid"
            v-model="form.userId"
            type="text"
            class="auth-input"
            placeholder="e.g. jane.doe"
            autocomplete="username"
            required
          />
        </div>
        <p v-if="fieldErrors.userId" class="mt-1 text-sm text-red-600">
          {{ fieldErrors.userId }}
        </p>
      </div>

      <div>
        <label for="register-fullname" class="auth-field-label"
          >Full name</label
        >
        <div class="auth-input-shell">
          <span class="material-symbols-outlined text-[18px] text-slate-400"
            >badge</span
          >
          <input
            id="register-fullname"
            v-model="form.fullName"
            type="text"
            class="auth-input"
            placeholder="e.g. Jane Doe"
            autocomplete="name"
            required
          />
        </div>
        <p v-if="fieldErrors.fullName" class="mt-1 text-sm text-red-600">
          {{ fieldErrors.fullName }}
        </p>
      </div>

      <div>
        <label for="register-email" class="auth-field-label">Email</label>
        <div class="auth-input-shell">
          <span class="material-symbols-outlined text-[18px] text-slate-400"
            >mail</span
          >
          <input
            id="register-email"
            v-model="form.email"
            type="email"
            class="auth-input"
            placeholder="e.g. jane@example.edu"
            autocomplete="email"
            required
          />
        </div>
        <p v-if="fieldErrors.email" class="mt-1 text-sm text-red-600">
          {{ fieldErrors.email }}
        </p>
      </div>

      <div>
        <label for="register-department" class="auth-field-label">
          Department <span class="font-normal text-slate-400">(optional)</span>
        </label>
        <div class="auth-input-shell">
          <span class="material-symbols-outlined text-[18px] text-slate-400"
            >apartment</span
          >
          <input
            id="register-department"
            v-model="form.department"
            type="text"
            class="auth-input"
            placeholder="e.g. School of Computing"
            autocomplete="organization"
          />
        </div>
      </div>

      <div>
        <label for="register-password" class="auth-field-label">Password</label>
        <div class="auth-input-shell">
          <span class="material-symbols-outlined text-[18px] text-slate-400"
            >lock</span
          >
          <input
            id="register-password"
            v-model="form.password"
            :type="showPassword ? 'text' : 'password'"
            class="auth-input"
            placeholder="Create a password"
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
        <p v-if="fieldErrors.password" class="mt-1 text-sm text-red-600">
          {{ fieldErrors.password }}
        </p>
      </div>

      <div>
        <label for="register-confirm" class="auth-field-label">
          Confirm password
        </label>
        <div class="auth-input-shell">
          <span class="material-symbols-outlined text-[18px] text-slate-400"
            >lock_reset</span
          >
          <input
            id="register-confirm"
            v-model="form.confirmPassword"
            :type="showPassword ? 'text' : 'password'"
            class="auth-input"
            placeholder="Repeat your password"
            autocomplete="new-password"
            required
          />
        </div>
        <p v-if="fieldErrors.confirmPassword" class="mt-1 text-sm text-red-600">
          {{ fieldErrors.confirmPassword }}
        </p>
      </div>

      <button type="submit" class="auth-submit" :disabled="isLoading">
        <span v-if="!isLoading">Create account</span>
        <span v-else class="auth-spinner"></span>
        <span v-if="!isLoading" class="material-symbols-outlined text-[20px]"
          >person_add</span
        >
      </button>
    </form>

    <template #footer>
      <span class="text-slate-500">Already have an account?</span>
      <RouterLink to="/" class="auth-link ml-1">Sign in</RouterLink>
    </template>
  </AuthShell>
</template>
