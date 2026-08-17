<script setup lang="ts">
// PROJECT-OWNED — safe to edit.
//
// The two-panel frame every auth screen sits in: a branding panel on the left (desktop
// only) and a centred card on the right. Sign In, Register, Forgot Password and Reset
// Password all render through here, so they stay visually identical for free.
//
// Slots:
//   default  — the form body
//   footer   — links under the form (e.g. "Back to sign in")

import { onMounted, ref } from "vue";
import { useTheme } from "@apptemplate/ui";
import { BRAND_LOGO } from "../app-config/branding";

interface Props {
  /** Card heading, e.g. "Sign In". */
  title: string;
  /** One-line explanation under the heading. */
  subtitle?: string;
}

defineProps<Props>();

const { brandLabel } = useTheme();
const pageLoaded = ref(false);

// Small delay so the fade-in reads as intentional rather than a flash of content.
onMounted(() => {
  window.setTimeout(() => {
    pageLoaded.value = true;
  }, 200);
});

const heroHighlights = [
  { label: "Secure access", icon: "verified_user" },
  { label: "Workflow ready", icon: "hub" },
  { label: "Operations hub", icon: "insights" },
];
</script>

<template>
  <div
    class="login-page min-h-screen flex flex-col lg:flex-row"
    :class="{ 'is-mounted': pageLoaded }"
  >
    <!-- ── Card (mobile: full screen, desktop: right side) ── -->
    <div
      class="flex flex-1 items-center justify-center p-4 sm:p-6 md:p-12 order-1 lg:order-2 min-h-screen lg:min-h-0"
      style="
        background: linear-gradient(
          160deg,
          #f8fafc 0%,
          #eef2ff 50%,
          #e0e7ff 100%
        );
      "
    >
      <div class="fade-in-up w-full max-w-md">
        <div class="auth-card">
          <div class="mb-6 flex justify-center">
            <img
              :src="BRAND_LOGO"
              :alt="`${brandLabel} logo`"
              class="h-20 sm:h-24 md:h-28 drop-shadow-lg"
              data-testid="app-login-logo"
            />
          </div>

          <h1
            class="mb-2 text-2xl sm:text-3xl font-extrabold tracking-tight text-slate-800 text-center"
          >
            {{ title }}
          </h1>
          <p
            v-if="subtitle"
            class="mb-8 text-slate-500 text-center text-sm sm:text-base"
          >
            {{ subtitle }}
          </p>

          <slot />

          <div v-if="$slots.footer" class="mt-6 text-center text-sm">
            <slot name="footer" />
          </div>
        </div>
      </div>
    </div>

    <!-- ── Branding panel (hidden on mobile, left side on desktop) ── -->
    <div
      class="relative hidden overflow-hidden lg:flex lg:w-[50%] lg:flex-col lg:justify-between order-2 lg:order-1"
      style="
        background: linear-gradient(
          135deg,
          #0f172a 0%,
          #1e1b4b 40%,
          #312e81 70%,
          #1e293b 100%
        );
      "
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
            class="text-sm font-semibold tracking-widest text-indigo-400 uppercase"
            >{{ brandLabel }}</span
          >
        </div>

        <div class="fade-in-up max-w-lg" style="animation-delay: 0.2s">
          <h2
            class="mb-5 text-4xl xl:text-5xl font-extrabold leading-tight tracking-tight text-white"
          >
            <span class="gradient-text">{{ brandLabel }}</span>
          </h2>
          <div class="space-y-4 text-lg leading-relaxed text-slate-300/90">
            <p>Secure access for {{ brandLabel }} teams and workflows.</p>
          </div>

          <div class="mt-10 grid grid-cols-1 gap-4 xl:grid-cols-3">
            <div
              v-for="(highlight, index) in heroHighlights"
              :key="highlight.label"
              class="feature-card"
              :style="{ animationDelay: `${index * 0.1}s` }"
            >
              <span
                class="material-symbols-outlined mb-2 block text-[28px] text-indigo-400"
                >{{ highlight.icon }}</span
              >
              <h3 class="mb-1 text-sm font-bold text-white">
                {{ highlight.label }}
              </h3>
            </div>
          </div>
        </div>

        <div
          class="fade-in-up flex items-center gap-8 text-sm"
          style="animation-delay: 0.45s"
        >
          <div class="stat-item">
            <p class="text-2xl font-extrabold text-white">Secure</p>
            <p class="text-slate-400">Session-based auth</p>
          </div>
          <div class="h-8 w-px bg-white/10"></div>
          <div class="stat-item">
            <p class="text-2xl font-extrabold text-white">Fast</p>
            <p class="text-slate-400">Role-based access</p>
          </div>
          <div class="h-8 w-px bg-white/10"></div>
          <div class="stat-item">
            <p class="text-2xl font-extrabold text-white">Ready</p>
            <p class="text-slate-400">Production-grade</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Layout + branding panel styling. Scoped: only this component's own markup uses it. */
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

.gradient-text {
  background: linear-gradient(135deg, #818cf8, #60a5fa, #a78bfa);
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
  border-radius: 50%;
  filter: blur(80px);
}

.orb-1 {
  top: -10%;
  right: -5%;
  width: 400px;
  height: 400px;
  background: radial-gradient(
    circle,
    rgba(99, 102, 241, 0.25),
    transparent 70%
  );
  animation: float3d1 8s ease-in-out infinite;
}

.orb-2 {
  bottom: 10%;
  left: -5%;
  width: 300px;
  height: 300px;
  background: radial-gradient(circle, rgba(139, 92, 246, 0.2), transparent 70%);
  animation: float3d2 10s ease-in-out infinite;
}

.orb-3 {
  top: 40%;
  right: 20%;
  width: 200px;
  height: 200px;
  background: radial-gradient(circle, rgba(59, 130, 246, 0.2), transparent 70%);
  animation: float3d3 7s ease-in-out infinite;
}

.center-pulse {
  position: absolute;
  top: 50%;
  left: 50%;
  width: 160px;
  height: 160px;
  transform: translate(-50%, -50%);
  border-radius: 999px;
  background: radial-gradient(
    circle,
    rgba(129, 140, 248, 0.18),
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
    linear-gradient(to top, rgba(99, 102, 241, 0.06), transparent),
    repeating-linear-gradient(
      90deg,
      rgba(99, 102, 241, 0.04) 0px,
      transparent 1px,
      transparent 60px
    ),
    repeating-linear-gradient(
      0deg,
      rgba(99, 102, 241, 0.04) 0px,
      transparent 1px,
      transparent 60px
    );
  transform: perspective(500px) rotateX(45deg);
  transform-origin: bottom center;
  mask-image: linear-gradient(to top, rgba(0, 0, 0, 0.8), transparent);
  -webkit-mask-image: linear-gradient(to top, rgba(0, 0, 0, 0.8), transparent);
}

.feature-card {
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.04);
  padding: 16px;
  backdrop-filter: blur(12px);
  transition: all 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);
  animation: fadeInUp 0.8s ease-out both;
}

.feature-card:hover {
  border-color: rgba(129, 140, 248, 0.3);
  background: rgba(255, 255, 255, 0.08);
  transform: perspective(800px) rotateY(-3deg) translateY(-4px) translateZ(10px);
  box-shadow: 0 20px 40px -15px rgba(99, 102, 241, 0.15);
}

.stat-item {
  transition: transform 0.3s ease;
}

.stat-item:hover {
  transform: translateY(-2px);
}

.auth-card {
  border-radius: 24px;
  background: white;
  padding: 40px;
  box-shadow:
    0 4px 6px -1px rgba(0, 0, 0, 0.05),
    0 20px 50px -12px rgba(0, 0, 0, 0.08);
  transition:
    transform 0.3s ease,
    box-shadow 0.3s ease;
}

.auth-card:hover {
  transform: perspective(1000px) rotateX(1deg) rotateY(-1deg) translateY(-2px);
  box-shadow:
    0 4px 6px -1px rgba(0, 0, 0, 0.05),
    0 25px 60px -12px rgba(0, 0, 0, 0.12);
}

.login-page {
  opacity: 0;
  transition: opacity 0.5s ease;
}

.login-page.is-mounted {
  opacity: 1;
}

@media (max-width: 1024px) {
  .auth-card {
    padding: 32px;
  }
}

@media (max-width: 640px) {
  .auth-card {
    border-radius: 20px;
    padding: 24px;
  }
}
</style>

<style>
/*
 * Shared form primitives for every auth screen.
 *
 * Deliberately NOT scoped: the markup that uses these classes lives in the *pages*
 * (LoginPage, RegisterPage, ...) which pass it through <slot />, so a scoped rule here
 * would never match it. AuthShell is imported by all four pages, so this block loads
 * exactly once. Everything is namespaced `auth-*` to keep it out of the global soup.
 */
.auth-form {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.auth-field-label {
  display: block;
  margin-bottom: 0.5rem;
  font-size: 0.875rem;
  font-weight: 600;
  color: #334155;
}

.auth-input-shell {
  display: flex;
  align-items: center;
  gap: 10px;
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  background: #f8fafc;
  padding: 0 14px;
  transition:
    border-color 0.25s ease,
    box-shadow 0.25s ease,
    transform 0.25s ease;
}

.auth-input-shell:focus-within {
  border-color: #6366f1;
  box-shadow: 0 0 0 3px rgba(99, 102, 241, 0.12);
  transform: translateY(-1px);
}

.auth-input {
  width: 100%;
  border: 0;
  background: transparent;
  padding: 15px 0;
  font-size: 0.95rem;
  color: #0f172a;
  outline: none;
}

.auth-input::placeholder {
  color: #94a3b8;
}

.auth-submit {
  position: relative;
  display: flex;
  height: 52px;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  overflow: hidden;
  border-radius: 0.75rem;
  background-image: linear-gradient(to right, #4f46e5, #6366f1);
  font-size: 1rem;
  font-weight: 700;
  color: #fff;
  box-shadow: 0 10px 15px -3px rgba(99, 102, 241, 0.25);
  transition: all 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275);
}

.auth-submit::before {
  content: "";
  position: absolute;
  inset: 0;
  background: linear-gradient(
    135deg,
    transparent,
    rgba(255, 255, 255, 0.1),
    transparent
  );
  transform: translateX(-100%);
  transition: transform 0.6s ease;
}

.auth-submit:hover:not(:disabled) {
  transform: translateY(-2px) scale(1.02);
  box-shadow: 0 12px 30px -8px rgba(99, 102, 241, 0.4);
}

.auth-submit:hover:not(:disabled)::before {
  transform: translateX(100%);
}

.auth-submit:disabled {
  cursor: wait;
  opacity: 0.8;
}

.auth-provider-button {
  display: flex;
  height: 52px;
  width: 100%;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  border: 1px solid #e2e8f0;
  border-radius: 0.75rem;
  background: #fff;
  font-size: 1rem;
  font-weight: 700;
  color: #334155;
  box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
  transition:
    border-color 0.25s ease,
    transform 0.25s ease;
}

.auth-provider-button:hover:not(:disabled) {
  border-color: #c7d2fe;
  transform: translateY(-1px);
}

.auth-provider-button:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

.auth-divider {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-size: 0.75rem;
  font-weight: 600;
  letter-spacing: 0.2em;
  text-transform: uppercase;
  color: #94a3b8;
}

.auth-divider::before,
.auth-divider::after {
  content: "";
  height: 1px;
  flex: 1;
  background: #e2e8f0;
}

.auth-link {
  font-weight: 600;
  color: #4f46e5;
  transition: color 0.2s ease;
}

.auth-link:hover {
  color: #4338ca;
  text-decoration: underline;
}

.auth-spinner {
  display: inline-block;
  width: 20px;
  height: 20px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-radius: 50%;
  border-top-color: #fff;
  animation: authSpin 0.8s linear infinite;
}

@keyframes authSpin {
  to {
    transform: rotate(360deg);
  }
}

.auth-shake {
  animation: authShake 0.5s ease-in-out;
}

@keyframes authShake {
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

.auth-fade-enter-active,
.auth-fade-leave-active {
  transition: opacity 0.2s ease;
}

.auth-fade-enter-from,
.auth-fade-leave-to {
  opacity: 0;
}
</style>
