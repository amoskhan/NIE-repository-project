<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { toTypedSchema } from "@vee-validate/zod";
import Cookie from "js-cookie";
import { useForm } from "vee-validate";
import { z } from "zod";
import { NieButton, useTheme } from "@nie/ui";
import {
  FRONTEND_CONSTANTS,
  getBackendUrl,
  getCookieAttributes,
  getValidationFieldErrors,
} from "@nie/platform";
import { BRAND_LOGO } from "../app-config/branding";

const loginSchema = toTypedSchema(
  z.object({
    userid: z
      .string()
      .trim()
      .min(1, "Username is required")
      .max(100, "Username must not exceed 100 characters"),
    pd: z
      .string()
      .min(1, "Password is required")
      .max(512, "Password must not exceed 512 characters"),
  }),
);
const { defineField, errors, handleSubmit, setErrors } = useForm({
  validationSchema: loginSchema,
  initialValues: { userid: "", pd: "" },
});
const [username] = defineField("userid");
const [password] = defineField("pd");
const showPassword = ref(false);
const pageLoaded = ref(false);
const isLoading = ref(false);
const isSsoLoading = ref(false);
const errorMessage = ref("");
const ssoStatusMessage = ref("");
const { brandLabel } = useTheme();

const cookieSettings = getCookieAttributes();
const isPortalSsoEnabled = FRONTEND_CONSTANTS.auth.portalSsoEnabled;
const isBusy = computed(() => isLoading.value || isSsoLoading.value);
const heroDescription = computed(
  () => `Secure access for ${brandLabel.value} teams and workflows.`,
);
const heroHighlights = [
  { label: "Secure access", icon: "verified_user" },
  { label: "Workflow ready", icon: "hub" },
  { label: "Operations hub", icon: "insights" },
];

const clearAuthCookies = () => {
  Cookie.remove(FRONTEND_CONSTANTS.cookies.session, cookieSettings);
  Cookie.remove(FRONTEND_CONSTANTS.cookies.user, cookieSettings);
};

const revokeExistingSession = async (sessionToken?: string) => {
  if (!sessionToken) {
    return;
  }

  try {
    await fetch(getBackendUrl("auth", "/api/Auth/Logout"), {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Session-Id": sessionToken,
      },
      body: JSON.stringify(sessionToken),
      credentials: "include",
    });
  } catch {
    // The login screen still needs to clear stale local state if auth is offline.
  }
};

const completeLogin = (data: {
  sessionToken?: string;
  userId?: string;
  userName?: string;
  fullName?: string;
  email?: string;
  department?: string;
  role?: unknown;
  roles?: Array<{ RoleName?: string | null } | string>;
  permissions?: string[];
}) => {
  const roleNames =
    data.roles
      ?.map((role) =>
        typeof role === "string" ? role : (role.RoleName ?? null),
      )
      .filter(Boolean) ?? [];

  if (data.sessionToken) {
    Cookie.set(
      FRONTEND_CONSTANTS.cookies.session,
      data.sessionToken,
      cookieSettings,
    );
    Cookie.set(
      FRONTEND_CONSTANTS.cookies.user,
      JSON.stringify({
        userId: data.userId,
        fullName: data.fullName || data.userName || "",
        email: data.email || "",
        department: data.department || "",
        role: data.role,
        roles: roleNames,
        roleNames,
        permissions: data.permissions?.filter(Boolean) || [],
      }),
      cookieSettings,
    );
  }

  window.location.href = FRONTEND_CONSTANTS.apps.main;
};

const getSsoReturnUrl = () => {
  const url = new URL(window.location.href);
  url.searchParams.delete("state");
  url.searchParams.set("sso", "1");
  return url.toString();
};

const sleep = (ms: number) =>
  new Promise((resolve) => {
    window.setTimeout(resolve, ms);
  });

const startSsoFlow = async () => {
  if (!isPortalSsoEnabled || isBusy.value) {
    return;
  }

  isSsoLoading.value = true;
  errorMessage.value = "";
  ssoStatusMessage.value = "Connecting to portal...";

  try {
    const params = new URLSearchParams({
      returnUrl: getSsoReturnUrl(),
    });

    const response = await fetch(
      `${getBackendUrl("auth", "/api/Auth/SsoStart")}?${params.toString()}`,
      {
        method: "GET",
        credentials: "include",
      },
    );

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.message || "Unable to start portal sign-in.");
    }

    const data = (await response.json()) as {
      launchUrl: string;
    };

    if (!data.launchUrl) {
      throw new Error("Portal sign-in did not provide a launch URL.");
    }

    window.location.href = data.launchUrl;
  } catch (error) {
    errorMessage.value =
      error instanceof Error ? error.message : "Portal sign-in failed.";
    ssoStatusMessage.value = "";
    isSsoLoading.value = false;
  }
};

const finalizeSsoFlow = async (state: string) => {
  if (!state) {
    return;
  }

  isSsoLoading.value = true;
  errorMessage.value = "";
  ssoStatusMessage.value = "Completing portal sign-in...";

  try {
    for (let attempt = 0; attempt < 40; attempt += 1) {
      const response = await fetch(
        `${getBackendUrl("auth", "/api/Auth/SsoFinalize")}?state=${encodeURIComponent(state)}`,
        {
          method: "GET",
          credentials: "include",
        },
      );

      if (response.status === 202) {
        const pending = (await response.json().catch(() => null)) as {
          pollIntervalMs?: number;
        } | null;
        await sleep(pending?.pollIntervalMs ?? 1500);
        continue;
      }

      if (response.ok) {
        const data = await response.json();
        completeLogin(data);
        return;
      }

      const errorData = await response.json().catch(() => null);
      throw new Error(
        errorData?.message || "Portal sign-in could not be completed.",
      );
    }

    throw new Error("Portal sign-in timed out. Please try again.");
  } catch (error) {
    errorMessage.value =
      error instanceof Error ? error.message : "Portal sign-in failed.";
  } finally {
    ssoStatusMessage.value = "";
    isSsoLoading.value = false;
  }
};

const handleLogin = handleSubmit(async (credentials) => {
  if (isBusy.value) {
    return;
  }

  isLoading.value = true;
  errorMessage.value = "";

  try {
    const response = await fetch(getBackendUrl("auth", "/api/Auth/Login"), {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(credentials),
      credentials: "include",
    });

    if (response.ok) {
      const data = await response.json();
      completeLogin(data);
      return;
    }

    const errorData = await response.json().catch(() => null);
    setErrors(getValidationFieldErrors(errorData));
    errorMessage.value =
      errorData?.detail ||
      errorData?.message ||
      "Login failed. Please check your credentials.";
  } catch {
    errorMessage.value = "An error occurred during login. Please try again.";
  } finally {
    isLoading.value = false;
  }
});

onMounted(() => {
  window.setTimeout(() => {
    pageLoaded.value = true;
  }, 200);

  void revokeExistingSession(Cookie.get(FRONTEND_CONSTANTS.cookies.session));
  clearAuthCookies();

  const params = new URLSearchParams(window.location.search);
  const state = params.get("state");
  const shouldStartSso = params.get("sso");

  if (isPortalSsoEnabled && state) {
    void finalizeSsoFlow(state);
    return;
  }

  if (
    isPortalSsoEnabled &&
    (shouldStartSso === "1" || shouldStartSso === "true")
  ) {
    void startSsoFlow();
  }
});
</script>

<template>
  <div
    class="login-page min-h-screen flex flex-col lg:flex-row"
    :class="{ 'is-mounted': pageLoaded }"
  >
    <!-- ── Login Box (mobile: full screen, desktop: right side) ── -->
    <div
      class="login-panel flex flex-1 items-center justify-center p-4 sm:p-6 md:p-12 order-1 lg:order-2 min-h-screen lg:min-h-0"
    >
      <div class="fade-in-up w-full max-w-md">
        <div class="login-card">
          <!-- Logo inside the box -->
          <div class="mb-6 flex justify-center">
            <img
              :src="BRAND_LOGO"
              alt="NIE Logo"
              class="h-20 sm:h-24 md:h-28"
              data-testid="tic-login-logo"
            />
          </div>

          <h1
            class="login-title mb-2 text-center"
          >
            Sign In
          </h1>
          <p class="login-subtitle mb-8 text-center">
            Enter your credentials to continue.
          </p>

          <transition name="fade">
            <div
              v-if="errorMessage"
              class="auth-alert auth-alert--danger shake mb-6"
            >
              <span class="auth-icon material-symbols-outlined">error</span>
              {{ errorMessage }}
            </div>
          </transition>

          <transition name="fade">
            <div
              v-if="ssoStatusMessage"
              class="auth-alert auth-alert--info mb-6"
            >
              <span class="auth-icon material-symbols-outlined">sync</span>
              {{ ssoStatusMessage }}
            </div>
          </transition>

          <form class="flex flex-col gap-5" @submit.prevent="handleLogin">
            <div>
              <label
                for="username"
                class="auth-label"
              >
                Username
              </label>
              <div class="input-shell">
                <span
                  class="auth-input-icon material-symbols-outlined"
                  >person</span
                >
                <input
                  id="username"
                  v-model="username"
                  type="text"
                  class="auth-input"
                  placeholder="Enter your username"
                  autocomplete="username"
                  :aria-invalid="errors.userid ? 'true' : undefined"
                  :aria-describedby="
                    errors.userid ? 'username-error' : undefined
                  "
                />
              </div>
              <p
                v-if="errors.userid"
                id="username-error"
                role="alert"
                class="auth-field-error"
              >
                {{ errors.userid }}
              </p>
            </div>

            <div>
              <label
                for="password"
                class="auth-label"
              >
                Password
              </label>
              <div class="input-shell">
                <span
                  class="auth-input-icon material-symbols-outlined"
                  >lock</span
                >
                <input
                  id="password"
                  v-model="password"
                  :type="showPassword ? 'text' : 'password'"
                  class="auth-input"
                  placeholder="Enter your password"
                  autocomplete="current-password"
                  :aria-invalid="errors.pd ? 'true' : undefined"
                  :aria-describedby="errors.pd ? 'password-error' : undefined"
                />
                <button
                  type="button"
                  class="password-toggle"
                  :aria-label="showPassword ? 'Hide password' : 'Show password'"
                  @click="showPassword = !showPassword"
                >
                  <span class="auth-icon material-symbols-outlined">{{
                    showPassword ? "visibility_off" : "visibility"
                  }}</span>
                </button>
              </div>
              <p
                v-if="errors.pd"
                id="password-error"
                role="alert"
                class="auth-field-error"
              >
                {{ errors.pd }}
              </p>
            </div>

            <NieButton
              type="submit"
              size="lg"
              class="login-button w-full"
              :disabled="isBusy"
              :loading="isLoading"
            >
              <span v-if="!isLoading">Login</span>
              <span
                v-if="!isLoading"
                class="button-icon material-symbols-outlined"
                >arrow_forward</span
              >
            </NieButton>

            <div
              v-if="isPortalSsoEnabled"
              class="portal-divider mt-1"
            >
              <span class="portal-divider__line"></span>
              <span>Portal</span>
              <span class="portal-divider__line"></span>
            </div>

            <NieButton
              v-if="isPortalSsoEnabled"
              type="button"
              variant="outline"
              size="lg"
              class="portal-button w-full"
              :disabled="isBusy"
              :loading="isSsoLoading"
              @click="startSsoFlow"
            >
              <span v-if="!isSsoLoading">Continue with Portal</span>
              <span
                v-if="!isSsoLoading"
                class="button-icon material-symbols-outlined"
                >open_in_new</span
              >
            </NieButton>
          </form>
        </div>
      </div>
    </div>

    <!-- ── Branding Panel (hidden on mobile, left side on desktop) ── -->
    <div
      class="brand-panel relative hidden overflow-hidden lg:flex lg:w-1/2 lg:flex-col lg:justify-between order-2 lg:order-1"
    >
      <div class="absolute inset-0 perspective-[1200px]">
        <div class="orb orb-1"></div>
        <div class="orb orb-2"></div>
        <div class="orb orb-3"></div>
        <div class="grid-floor"></div>
        <div class="center-pulse"></div>
      </div>

      <div class="relative z-10 flex h-full flex-col justify-between p-12">
        <div class="fade-in-down">
          <span
            class="brand-eyebrow"
            >{{ brandLabel }}</span
          >
        </div>

        <div class="fade-in-up max-w-lg" style="animation-delay: 0.2s">
          <h2
            class="brand-title mb-5"
          >
            <span class="gradient-text">{{ brandLabel }}</span>
          </h2>
          <div class="brand-description space-y-4">
            <p>{{ heroDescription }}</p>
          </div>

          <div class="mt-10 grid grid-cols-1 gap-4 xl:grid-cols-3">
            <div
              v-for="(highlight, index) in heroHighlights"
              :key="highlight.label"
              class="feature-card"
              :style="{ animationDelay: `${index * 0.1}s` }"
            >
              <span
                class="feature-icon material-symbols-outlined mb-2 block"
                >{{ highlight.icon }}</span
              >
              <h3 class="feature-title mb-1">
                {{ highlight.label }}
              </h3>
            </div>
          </div>
        </div>

        <div
          class="brand-stats fade-in-up flex items-center gap-8"
          style="animation-delay: 0.45s"
        >
          <div class="stat-item">
            <p class="stat-value">Secure</p>
            <p class="stat-label">Session-based auth</p>
          </div>
          <div class="brand-stats__divider"></div>
          <div class="stat-item">
            <p class="stat-value">Fast</p>
            <p class="stat-label">Role-based access</p>
          </div>
          <div class="brand-stats__divider"></div>
          <div class="stat-item">
            <p class="stat-value">Ready</p>
            <p class="stat-label">Production-grade</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.login-panel {
  background: linear-gradient(
    160deg,
    var(--theme-color-surface-canvas) 0%,
    color-mix(
        in srgb,
        var(--theme-color-brand-50) 72%,
        var(--theme-color-surface-panel)
      )
      50%,
    var(--theme-color-surface-subtle) 100%
  );
}

.brand-panel {
  background: linear-gradient(
    135deg,
    var(--theme-color-neutral-900) 0%,
    color-mix(
        in srgb,
        var(--theme-color-neutral-900) 70%,
        var(--theme-color-brand-900)
      )
      40%,
    color-mix(
        in srgb,
        var(--theme-color-brand-900) 68%,
        var(--theme-color-neutral-800)
      )
      70%,
    var(--theme-color-neutral-800) 100%
  );
}

.login-title {
  color: var(--theme-color-text-strong);
  font-size: var(--theme-font-size-page-title);
  font-weight: var(--theme-font-weight-bold);
  letter-spacing: var(--theme-letter-spacing-tight);
}

.login-subtitle {
  color: var(--theme-color-text-muted);
  font-size: var(--theme-font-size-body-lg);
}

.auth-alert {
  display: flex;
  align-items: center;
  gap: var(--theme-space-2);
  padding: var(--theme-space-3) var(--theme-space-4);
  border: 1px solid;
  border-radius: var(--theme-radius-control);
  font-size: var(--theme-font-size-body);
  font-weight: var(--theme-font-weight-medium);
}

.auth-icon,
.auth-input-icon {
  font-size: var(--theme-font-size-card-title);
}

.button-icon {
  font-size: var(--theme-font-size-section-title);
}

.auth-alert--danger {
  border-color: color-mix(
    in srgb,
    var(--theme-color-danger-solid) 25%,
    transparent
  );
  background: var(--theme-color-danger-surface);
  color: var(--theme-color-danger-text);
}

.auth-alert--info {
  border-color: color-mix(
    in srgb,
    var(--theme-color-info-solid) 25%,
    transparent
  );
  background: var(--theme-color-info-surface);
  color: var(--theme-color-info-text);
}

.auth-label {
  display: block;
  margin-bottom: var(--theme-space-2);
  color: var(--theme-color-text-soft);
  font-size: var(--theme-font-size-body);
  font-weight: var(--theme-font-weight-semibold);
}

.auth-input-icon,
.password-toggle {
  color: var(--theme-color-neutral-400);
}

.password-toggle {
  display: inline-flex;
  min-width: var(--theme-control-height-md);
  min-height: var(--theme-control-height-md);
  align-items: center;
  justify-content: center;
  border: 0;
  background: transparent;
  transition: color 180ms ease;
}

.password-toggle:hover {
  color: var(--theme-color-text-soft);
}

.auth-field-error {
  margin-top: var(--theme-space-1);
  color: var(--theme-color-danger-text);
  font-size: var(--theme-font-size-body);
  font-weight: var(--theme-font-weight-medium);
}

.portal-divider {
  display: flex;
  align-items: center;
  gap: var(--theme-space-3);
  color: var(--theme-color-neutral-400);
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-semibold);
  letter-spacing: var(--theme-letter-spacing-wide);
  text-transform: uppercase;
}

.portal-divider__line {
  height: 1px;
  flex: 1;
  background: var(--theme-color-border-default);
}

.brand-eyebrow,
.feature-icon {
  color: var(--theme-color-brand-300);
}

.brand-eyebrow {
  font-size: var(--theme-font-size-body);
  font-weight: var(--theme-font-weight-semibold);
  letter-spacing: var(--theme-letter-spacing-wide);
  text-transform: uppercase;
}

.brand-title,
.feature-title,
.stat-value {
  color: var(--theme-color-static-white);
  font-weight: var(--theme-font-weight-bold);
}

.brand-title {
  font-size: var(--theme-font-size-hero);
  line-height: 1.2;
  letter-spacing: var(--theme-letter-spacing-tight);
}

.brand-description {
  color: color-mix(
    in srgb,
    var(--theme-color-static-white) 72%,
    transparent
  );
  font-size: var(--theme-font-size-card-title);
  line-height: 1.625;
}

.feature-icon {
  font-size: var(--theme-font-size-page-title);
}

.feature-title {
  font-size: var(--theme-font-size-body);
}

.brand-stats {
  font-size: var(--theme-font-size-body);
}

.brand-stats__divider {
  width: 1px;
  height: var(--theme-space-8);
  background: color-mix(
    in srgb,
    var(--theme-color-static-white) 10%,
    transparent
  );
}

.stat-value {
  font-size: var(--theme-font-size-page-title);
}

.stat-label {
  color: color-mix(
    in srgb,
    var(--theme-color-static-white) 58%,
    transparent
  );
}

.fade-in-down {
  animation: fadeInDown 0.8s ease-out both;
}

.fade-in-up {
  animation: fadeInUp 0.8s ease-out both;
}

@keyframes fadeInDown {
  from {
    opacity: 0;
    transform: translateY(-30px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(30px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.logo-3d {
  transition: transform 0.4s ease;
  filter: brightness(1.1);
}

.gradient-text {
  background: linear-gradient(
    135deg,
    var(--theme-color-brand-300),
    var(--theme-color-info-300),
    color-mix(
      in srgb,
      var(--theme-color-brand-300) 70%,
      var(--theme-color-danger-300)
    )
  );
  background-size: 200% 200%;
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  animation: gradientShift 4s ease infinite;
}

@keyframes gradientShift {
  0%,
  100% {
    background-position: 0% 50%;
  }
  50% {
    background-position: 100% 50%;
  }
}

.orb {
  position: absolute;
  border-radius: var(--theme-radius-circle);
  filter: blur(80px);
}

.orb-1 {
  top: -10%;
  right: -5%;
  width: 400px;
  height: 400px;
  background: radial-gradient(
    circle,
    color-mix(in srgb, var(--theme-color-brand-400) 25%, transparent),
    transparent 70%
  );
  animation: float3d1 8s ease-in-out infinite;
}

.orb-2 {
  bottom: 10%;
  left: -5%;
  width: 300px;
  height: 300px;
  background: radial-gradient(
    circle,
    color-mix(in srgb, var(--theme-color-brand-300) 20%, transparent),
    transparent 70%
  );
  animation: float3d2 10s ease-in-out infinite;
}

.orb-3 {
  top: 40%;
  right: 20%;
  width: 200px;
  height: 200px;
  background: radial-gradient(
    circle,
    color-mix(in srgb, var(--theme-color-info-500) 20%, transparent),
    transparent 70%
  );
  animation: float3d3 7s ease-in-out infinite;
}

.center-pulse {
  position: absolute;
  top: 50%;
  left: 50%;
  width: 160px;
  height: 160px;
  transform: translate(-50%, -50%);
  border-radius: var(--theme-radius-pill);
  background: radial-gradient(
    circle,
    color-mix(in srgb, var(--theme-color-brand-300) 18%, transparent),
    transparent 68%
  );
  animation: pulseHalo 4s ease-in-out infinite;
}

@keyframes pulseHalo {
  0%,
  100% {
    transform: translate(-50%, -50%) scale(0.92);
    opacity: 0.6;
  }
  50% {
    transform: translate(-50%, -50%) scale(1.08);
    opacity: 1;
  }
}

@keyframes float3d1 {
  0%,
  100% {
    transform: translate3d(0, 0, 0) scale(1);
  }
  50% {
    transform: translate3d(-30px, 40px, 50px) scale(1.1);
  }
}

@keyframes float3d2 {
  0%,
  100% {
    transform: translate3d(0, 0, 0) scale(1);
  }
  50% {
    transform: translate3d(40px, -30px, -30px) scale(0.9);
  }
}

@keyframes float3d3 {
  0%,
  100% {
    transform: translate3d(0, 0, 0);
  }
  33% {
    transform: translate3d(20px, -20px, 40px);
  }
  66% {
    transform: translate3d(-20px, 30px, -20px);
  }
}

.grid-floor {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 40%;
  background:
    linear-gradient(
      to top,
      color-mix(in srgb, var(--theme-color-brand-400) 6%, transparent),
      transparent
    ),
    repeating-linear-gradient(
      90deg,
      color-mix(in srgb, var(--theme-color-brand-400) 4%, transparent) 0px,
      transparent 1px,
      transparent 60px
    ),
    repeating-linear-gradient(
      0deg,
      color-mix(in srgb, var(--theme-color-brand-400) 4%, transparent) 0px,
      transparent 1px,
      transparent 60px
    );
  transform: perspective(500px) rotateX(45deg);
  transform-origin: bottom center;
  mask-image: linear-gradient(
    to top,
    color-mix(in srgb, var(--theme-color-static-black) 80%, transparent),
    transparent
  );
  -webkit-mask-image: linear-gradient(
    to top,
    color-mix(in srgb, var(--theme-color-static-black) 80%, transparent),
    transparent
  );
}

.feature-card {
  border: 1px solid
    color-mix(in srgb, var(--theme-color-static-white) 8%, transparent);
  border-radius: var(--theme-radius-panel);
  background: color-mix(
    in srgb,
    var(--theme-color-static-white) 4%,
    transparent
  );
  padding: var(--theme-space-4);
  backdrop-filter: blur(12px);
  transition: all 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);
  animation: fadeInUp 0.8s ease-out both;
}

.feature-card:hover {
  border-color: color-mix(
    in srgb,
    var(--theme-color-brand-300) 30%,
    transparent
  );
  background: color-mix(
    in srgb,
    var(--theme-color-static-white) 8%,
    transparent
  );
  transform: perspective(800px) rotateY(-3deg) translateY(-4px) translateZ(10px);
  box-shadow: var(--theme-shadow-card);
}

.stat-item {
  transition: transform 0.3s ease;
}

.stat-item:hover {
  transform: translateY(-2px);
}

.login-card {
  border-radius: var(--theme-radius-dialog);
  background: var(--theme-color-surface-panel);
  padding: var(--theme-space-10);
  box-shadow: var(--theme-shadow-card);
  transition:
    transform 0.3s ease,
    box-shadow 0.3s ease;
}

.login-card:hover {
  transform: perspective(1000px) rotateX(1deg) rotateY(-1deg) translateY(-2px);
  box-shadow: var(--theme-shadow-float);
}

.input-shell {
  display: flex;
  align-items: center;
  gap: var(--theme-space-3);
  min-height: var(--theme-control-height-md);
  border: 1px solid var(--theme-color-border-default);
  border-radius: var(--theme-radius-control);
  background: var(--theme-color-surface-subtle);
  padding: 0 var(--theme-space-3);
  transition:
    border-color 0.25s ease,
    box-shadow 0.25s ease,
    transform 0.25s ease;
}

.input-shell:focus-within {
  border-color: var(--theme-color-brand-500);
  box-shadow: 0 0 0 3px
    color-mix(in srgb, var(--theme-color-brand-500) 12%, transparent);
  transform: translateY(-1px);
}

.auth-input {
  width: 100%;
  border: 0;
  background: transparent !important;
  min-height: var(--theme-control-height-md);
  padding: var(--theme-space-2) 0;
  color: var(--theme-color-text-strong);
  font-size: var(--theme-font-size-body);
  outline: none;
}

.auth-input::placeholder {
  color: var(--theme-color-neutral-400);
}

.login-button {
  position: relative;
  overflow: hidden;
  transition: all 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275);
}

.login-button::before {
  content: "";
  position: absolute;
  inset: 0;
  background: linear-gradient(
    135deg,
    transparent,
    color-mix(in srgb, var(--theme-color-static-white) 10%, transparent),
    transparent
  );
  transform: translateX(-100%);
  transition: transform 0.6s ease;
}

.login-button:hover:not(:disabled) {
  transform: translateY(-2px) scale(1.02);
  box-shadow: var(--theme-shadow-card);
}

.login-button:hover:not(:disabled)::before {
  transform: translateX(100%);
}

.login-button:disabled {
  cursor: wait;
  opacity: 0.8;
}

.shake {
  animation: shake 0.5s ease-in-out;
}

@keyframes shake {
  0%,
  100% {
    transform: translateX(0);
  }
  20% {
    transform: translateX(-8px);
  }
  40% {
    transform: translateX(8px);
  }
  60% {
    transform: translateX(-4px);
  }
  80% {
    transform: translateX(4px);
  }
}

.login-page {
  opacity: 0;
  transition: opacity 0.5s ease;
}

.login-page.is-mounted {
  opacity: 1;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

@media (max-width: 1024px) {
  .login-card {
    padding: var(--theme-space-8);
  }
}

@media (max-width: 640px) {
  .login-card {
    border-radius: var(--theme-radius-panel);
    padding: var(--theme-space-6);
  }
}

@media (prefers-reduced-motion: reduce) {
  .login-page,
  .fade-in-down,
  .fade-in-up,
  .gradient-text,
  .orb,
  .center-pulse,
  .feature-card,
  .shake {
    animation: none;
    transition: none;
    transform: none;
  }

  .login-page {
    opacity: 1;
  }
}
</style>
