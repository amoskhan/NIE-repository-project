<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from "vue";
import { XMarkIcon } from "@heroicons/vue/24/outline";

interface PaletteOption {
  id: string;
  name: string;
  swatch: string;
}

interface Props {
  userName: string;
  userEmail?: string;
  userRole?: string;
  userLastLogin?: string | null;
  mode: "light" | "dark" | "system";
  palette: string;
  palettes: PaletteOption[];
  notificationPreferencesStatus?: string;
  notificationPreferencesSummary?: string;
  closeSignal?: number;
}

const props = withDefaults(defineProps<Props>(), {
  userEmail: "",
  userRole: "",
  userLastLogin: null,
  notificationPreferencesStatus: "",
  notificationPreferencesSummary: "Manage subscriptions",
  closeSignal: 0,
});

const emit = defineEmits<{
  logout: [];
  "open-notification-preferences": [];
  "open-change": [value: boolean];
  "set-mode": [mode: "light" | "dark" | "system"];
  "set-palette": [palette: string];
}>();

const wrapperRef = ref<HTMLElement | null>(null);
const isOpen = ref(false);
const isMobileViewport = ref(false);

const userInitials = computed(() => {
  const initials = props.userName
    .trim()
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part.charAt(0).toUpperCase())
    .join("");

  return initials || "U";
});

const themeOptions = [
  { value: "light", label: "Light" },
  { value: "dark", label: "Dark" },
  { value: "system", label: "System" },
] as const;

const lastLoginFormatted = computed(() => {
  if (!props.userLastLogin) {
    return "Unavailable";
  }

  const date = new Date(props.userLastLogin);
  if (Number.isNaN(date.getTime())) {
    return "Unavailable";
  }

  return new Intl.DateTimeFormat("en-SG", {
    month: "short",
    day: "numeric",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit",
  }).format(date);
});

function syncViewport() {
  isMobileViewport.value = window.innerWidth < 768;
}

function setMenuOpen(nextValue: boolean) {
  if (isOpen.value === nextValue) {
    return;
  }

  isOpen.value = nextValue;
  emit("open-change", nextValue);
}

function closeMenu() {
  setMenuOpen(false);
}

function toggleMenu() {
  setMenuOpen(!isOpen.value);
}

function handleClickOutside(event: MouseEvent) {
  if (wrapperRef.value && !wrapperRef.value.contains(event.target as Node)) {
    closeMenu();
  }
}

function handleLogout() {
  isOpen.value = false;
  emit("logout");
}

function handleNotificationPreferencesClick() {
  closeMenu();
  emit("open-notification-preferences");
}

watch(
  () => props.closeSignal,
  () => {
    closeMenu();
  },
);

onMounted(() => {
  syncViewport();
  document.addEventListener("click", handleClickOutside);
  window.addEventListener("resize", syncViewport);
});

onUnmounted(() => {
  document.removeEventListener("click", handleClickOutside);
  window.removeEventListener("resize", syncViewport);
});
</script>

<template>
  <div ref="wrapperRef" class="relative">
    <button
      type="button"
      class="flex items-center gap-3"
      aria-label="Open profile menu"
      :aria-expanded="isOpen"
      @click.stop="toggleMenu"
    >
      <div class="hidden sm:flex flex-col items-end">
        <span class="text-sm font-semibold" style="color: var(--color-text)">
          {{ userName }}
        </span>
        <span
          v-if="userRole"
          class="text-xs"
          style="color: var(--color-text-muted)"
        >
          {{ userRole }}
        </span>
      </div>

      <div class="app-launchpad-avatar">
        {{ userInitials }}
      </div>
    </button>

    <div
      v-if="isOpen && !isMobileViewport"
      class="app-launchpad-popover app-launchpad-popover--profile"
    >
      <div class="app-profile-sheet">
        <section class="app-profile-section app-profile-section--identity">
          <div class="app-profile-card">
            <div class="app-launchpad-avatar app-launchpad-avatar--large">
              {{ userInitials }}
            </div>
            <div class="min-w-0">
              <p class="app-profile-card__name">{{ userName }}</p>
              <p v-if="userEmail" class="app-profile-card__email">
                {{ userEmail }}
              </p>
              <p v-if="userRole" class="app-profile-card__role">
                {{ userRole }}
              </p>
            </div>
          </div>
        </section>

        <section class="app-profile-section app-profile-section--meta">
          <div class="app-profile-meta">
            <p class="app-profile-meta__label">Last login</p>
            <p class="app-profile-meta__value">{{ lastLoginFormatted }}</p>
          </div>
        </section>

        <section class="app-profile-section">
          <div class="app-topbar-section-header">
            <p class="app-topbar-label">Notifications</p>
          </div>
          <button
            type="button"
            class="app-topbar-choice app-topbar-choice--stacked"
            @click="handleNotificationPreferencesClick"
          >
            <span class="app-topbar-choice__content">
              <span class="app-topbar-choice__title"
                >Notification Preferences</span
              >
              <span class="app-topbar-choice__hint">
                {{ notificationPreferencesSummary }}
              </span>
            </span>
            <span
              v-if="notificationPreferencesStatus"
              class="app-topbar-choice__meta"
              :class="{
                'app-topbar-choice__meta--active':
                  notificationPreferencesStatus === 'Enabled',
                'app-topbar-choice__meta--inactive':
                  notificationPreferencesStatus !== 'Enabled',
              }"
            >
              {{ notificationPreferencesStatus }}
            </span>
          </button>
        </section>

        <section class="app-profile-section">
          <div class="app-topbar-section-header">
            <p class="app-topbar-label">Theme</p>
          </div>
          <div class="app-topbar-theme-grid app-topbar-theme-grid--compact">
            <button
              v-for="option in themeOptions"
              :key="option.value"
              type="button"
              class="app-topbar-choice app-topbar-choice--compact"
              :class="{ 'app-topbar-choice--active': mode === option.value }"
              @click="emit('set-mode', option.value)"
            >
              {{ option.label }}
            </button>
          </div>
          <div v-if="palettes.length" class="app-topbar-palette-row">
            <button
              v-for="option in palettes"
              :key="option.id"
              type="button"
              class="app-topbar-palette-dot"
              :class="{
                'app-topbar-palette-dot--active': palette === option.id,
              }"
              :aria-label="option.name"
              :title="option.name"
              @click="emit('set-palette', option.id)"
            >
              <span
                class="app-topbar-palette-dot__swatch"
                :style="{ backgroundColor: option.swatch }"
              />
            </button>
          </div>
        </section>

        <section class="app-profile-section app-profile-section--actions">
          <div class="app-topbar-actions app-topbar-actions--single">
            <button
              type="button"
              class="app-topbar-choice app-topbar-choice--danger"
              @click="handleLogout"
            >
              Sign Out
            </button>
          </div>
        </section>
      </div>
    </div>
  </div>

  <Teleport to="body">
    <Transition name="app-mobile-sheet">
      <div v-if="isOpen && isMobileViewport" class="app-mobile-sheet-shell">
        <button
          type="button"
          class="app-mobile-sheet-backdrop"
          aria-label="Close profile menu"
          @click="closeMenu"
        />

        <div class="app-mobile-sheet">
          <div class="app-mobile-sheet-grip" />

          <div class="app-mobile-sheet-header">
            <div class="flex items-center justify-between gap-3">
              <h3 class="app-mobile-sheet-title">Profile</h3>
              <button
                type="button"
                class="app-mobile-sheet-close"
                aria-label="Close profile menu"
                @click="closeMenu"
              >
                <XMarkIcon class="h-5 w-5" />
              </button>
            </div>
          </div>

          <div class="app-profile-sheet">
            <section class="app-profile-section app-profile-section--identity">
              <div class="app-profile-card">
                <div class="app-launchpad-avatar app-launchpad-avatar--large">
                  {{ userInitials }}
                </div>
                <div class="min-w-0">
                  <p class="app-profile-card__name">{{ userName }}</p>
                  <p v-if="userEmail" class="app-profile-card__email">
                    {{ userEmail }}
                  </p>
                  <p v-if="userRole" class="app-profile-card__role">
                    {{ userRole }}
                  </p>
                </div>
              </div>
            </section>

            <section class="app-profile-section app-profile-section--meta">
              <div class="app-profile-meta">
                <p class="app-profile-meta__label">Last login</p>
                <p class="app-profile-meta__value">{{ lastLoginFormatted }}</p>
              </div>
            </section>

            <section class="app-profile-section">
              <div class="app-topbar-section-header">
                <p class="app-topbar-label">Notifications</p>
              </div>
              <button
                type="button"
                class="app-topbar-choice app-topbar-choice--stacked"
                @click="handleNotificationPreferencesClick"
              >
                <span class="app-topbar-choice__content">
                  <span class="app-topbar-choice__title"
                    >Notification Preferences</span
                  >
                  <span class="app-topbar-choice__hint">
                    {{ notificationPreferencesSummary }}
                  </span>
                </span>
                <span
                  v-if="notificationPreferencesStatus"
                  class="app-topbar-choice__meta"
                  :class="{
                    'app-topbar-choice__meta--active':
                      notificationPreferencesStatus === 'Enabled',
                    'app-topbar-choice__meta--inactive':
                      notificationPreferencesStatus !== 'Enabled',
                  }"
                >
                  {{ notificationPreferencesStatus }}
                </span>
              </button>
            </section>

            <section class="app-profile-section">
              <div class="app-topbar-section-header">
                <p class="app-topbar-label">Theme</p>
              </div>
              <div
                class="app-topbar-theme-grid app-topbar-theme-grid--mobile app-topbar-theme-grid--compact"
              >
                <button
                  v-for="option in themeOptions"
                  :key="option.value"
                  type="button"
                  class="app-topbar-choice app-topbar-choice--compact"
                  :class="{
                    'app-topbar-choice--active': mode === option.value,
                  }"
                  @click="emit('set-mode', option.value)"
                >
                  {{ option.label }}
                </button>
              </div>
              <div v-if="palettes.length" class="app-topbar-palette-row">
                <button
                  v-for="option in palettes"
                  :key="option.id"
                  type="button"
                  class="app-topbar-palette-dot"
                  :class="{
                    'app-topbar-palette-dot--active': palette === option.id,
                  }"
                  :aria-label="option.name"
                  :title="option.name"
                  @click="emit('set-palette', option.id)"
                >
                  <span
                    class="app-topbar-palette-dot__swatch"
                    :style="{ backgroundColor: option.swatch }"
                  />
                </button>
              </div>
            </section>

            <section class="app-profile-section app-profile-section--actions">
              <div class="app-topbar-actions app-topbar-actions--single">
                <button
                  type="button"
                  class="app-topbar-choice app-topbar-choice--danger"
                  @click="handleLogout"
                >
                  Sign Out
                </button>
              </div>
            </section>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.app-launchpad-avatar {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.75rem;
  height: 2.75rem;
  border-radius: 999px;
  background: linear-gradient(
    135deg,
    var(--color-primary),
    var(--color-primary-dark)
  );
  color: #fff;
  font-size: 0.98rem;
  font-weight: 700;
  box-shadow: 0 8px 20px -18px rgba(15, 23, 42, 0.55);
}

.app-launchpad-avatar--large {
  width: 3rem;
  height: 3rem;
  border-radius: 1.1rem;
}

.app-launchpad-popover {
  position: absolute;
  top: calc(100% + 0.8rem);
  right: 0;
  z-index: 120;
  width: min(24rem, calc(100vw - 2rem));
  padding: 0.5rem 0.9rem 0.7rem;
  border: 1px solid var(--color-border);
  border-radius: 1.25rem;
  background: color-mix(in srgb, var(--color-surface) 95%, transparent);
  box-shadow:
    0 30px 60px -30px rgba(15, 23, 42, 0.42),
    0 18px 30px -24px rgba(15, 23, 42, 0.28);
  backdrop-filter: blur(20px);
}

.app-profile-sheet {
  display: flex;
  flex-direction: column;
}

.app-profile-section {
  padding: 0.7rem 0.15rem;
}

.app-profile-section + .app-profile-section {
  border-top: 1px solid color-mix(in srgb, var(--color-border) 78%, transparent);
}

.app-profile-section--identity {
  padding-top: 0.35rem;
}

.app-profile-section--actions {
  padding-top: 0.7rem;
  padding-bottom: 0.15rem;
}

.app-profile-card {
  display: flex;
  align-items: center;
  gap: 0.85rem;
}

.app-profile-card__name {
  margin: 0;
  color: var(--color-text);
  font-size: 0.92rem;
  font-weight: 700;
}

.app-profile-card__email {
  margin: 0.25rem 0 0;
  color: var(--color-text-muted);
  font-size: 0.78rem;
  word-break: break-word;
}

.app-profile-card__role {
  margin: 0.32rem 0 0;
  color: var(--color-primary);
  font-size: 0.74rem;
  font-weight: 700;
}

.app-profile-meta__label,
.app-topbar-label {
  margin: 0;
  color: var(--color-text-muted);
  font-size: 0.72rem;
  font-weight: 700;
  letter-spacing: 0.12em;
  text-transform: uppercase;
}

.app-profile-meta__value {
  margin: 0.32rem 0 0;
  color: var(--color-text);
  font-size: 0.84rem;
  font-weight: 600;
}

.app-topbar-section-header {
  margin-bottom: 0.45rem;
}

.app-topbar-theme-grid,
.app-topbar-palette-grid,
.app-topbar-actions {
  display: grid;
  gap: 0.55rem;
}

.app-topbar-theme-grid {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.app-topbar-theme-grid--mobile {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.app-topbar-theme-grid--compact {
  gap: 0.4rem;
}

.app-topbar-palette-grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.app-topbar-actions--single {
  grid-template-columns: minmax(0, 1fr);
}

.app-topbar-palette-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.4rem;
  margin-top: 0.55rem;
}

.app-topbar-palette-dot {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  height: 2rem;
  padding: 0;
  border: 1px solid var(--color-border);
  border-radius: 999px;
  background: var(--color-surface);
  cursor: pointer;
  transition:
    border-color 0.2s ease,
    box-shadow 0.2s ease,
    transform 0.2s ease;
}

.app-topbar-palette-dot:hover {
  transform: translateY(-1px);
  border-color: color-mix(
    in srgb,
    var(--color-primary) 35%,
    var(--color-border)
  );
}

.app-topbar-palette-dot--active {
  border-color: color-mix(
    in srgb,
    var(--color-primary) 60%,
    var(--color-border)
  );
  box-shadow: 0 0 0 3px
    color-mix(in srgb, var(--color-primary) 14%, transparent);
}

.app-topbar-palette-dot__swatch {
  width: 1rem;
  height: 1rem;
  border: 2px solid rgba(255, 255, 255, 0.85);
  border-radius: 999px;
  box-shadow: 0 0 0 1px rgba(15, 23, 42, 0.1);
}

.app-topbar-choice {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.45rem;
  width: 100%;
  min-height: 2.75rem;
  padding: 0.7rem 0.85rem;
  border: 1px solid var(--color-border);
  border-radius: 0.95rem;
  background: var(--color-surface);
  color: var(--color-text-muted);
  font-size: 0.85rem;
  font-weight: 600;
  transition:
    border-color 0.2s ease,
    background-color 0.2s ease,
    color 0.2s ease,
    transform 0.2s ease;
}

.app-topbar-choice--compact {
  min-height: 2.15rem;
  padding: 0.4rem 0.65rem;
  border-radius: 0.7rem;
  font-size: 0.78rem;
}

.app-topbar-choice:hover {
  transform: translateY(-1px);
  border-color: color-mix(
    in srgb,
    var(--color-primary) 35%,
    var(--color-border)
  );
}

.app-topbar-choice--palette {
  justify-content: flex-start;
}

.app-topbar-choice--stacked {
  justify-content: space-between;
  text-align: left;
}

.app-topbar-choice__content {
  display: flex;
  min-width: 0;
  flex: 1 1 auto;
  flex-direction: column;
  align-items: flex-start;
}

.app-topbar-choice__title {
  color: var(--color-text);
  font-size: 0.84rem;
  font-weight: 700;
}

.app-topbar-choice__hint {
  margin-top: 0.18rem;
  color: var(--color-text-muted);
  font-size: 0.72rem;
  line-height: 1.35;
}

.app-topbar-choice__meta {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border-radius: 999px;
  padding: 0.28rem 0.7rem;
  font-size: 0.72rem;
  font-weight: 700;
  white-space: nowrap;
}

.app-topbar-choice__meta--active {
  background: var(--color-sidebar-active);
  color: var(--color-primary);
}

.app-topbar-choice__meta--inactive {
  background: color-mix(in srgb, var(--color-border) 74%, white);
  color: var(--color-text-muted);
}

.app-topbar-choice--active {
  border-color: color-mix(
    in srgb,
    var(--color-primary) 60%,
    var(--color-border)
  );
  background: var(--color-sidebar-active);
  color: var(--color-primary);
}

.app-topbar-choice--danger {
  color: #dc2626;
}

.app-topbar-swatch {
  width: 0.9rem;
  height: 0.9rem;
  border: 2px solid rgba(255, 255, 255, 0.75);
  border-radius: 999px;
  box-shadow: 0 0 0 1px rgba(15, 23, 42, 0.08);
}

.app-mobile-sheet-shell {
  position: fixed;
  inset: 0;
  z-index: 150;
  display: flex;
  align-items: flex-end;
  justify-content: center;
  padding: 0 0.75rem max(env(safe-area-inset-bottom, 0px), 0.75rem);
}

.app-mobile-sheet-backdrop {
  position: absolute;
  inset: 0;
  border: 0;
  background: rgba(15, 23, 42, 0.56);
}

.app-mobile-sheet {
  position: relative;
  display: flex;
  flex-direction: column;
  width: min(100%, 32rem);
  max-height: min(84dvh, 42rem);
  overflow: hidden;
  padding: 0.25rem 1rem calc(env(safe-area-inset-bottom, 0px) + 1rem);
  border: 1px solid var(--color-border);
  border-bottom: 0;
  border-top-left-radius: 1.6rem;
  border-top-right-radius: 1.6rem;
  background: color-mix(in srgb, var(--color-surface) 98%, transparent);
  box-shadow:
    0 30px 60px -30px rgba(15, 23, 42, 0.42),
    0 18px 30px -24px rgba(15, 23, 42, 0.28);
}

.app-mobile-sheet-grip {
  width: 3.4rem;
  height: 0.32rem;
  margin: 0.6rem auto 0.35rem;
  border-radius: 999px;
  background: color-mix(
    in srgb,
    var(--color-border) 78%,
    var(--color-text-muted) 22%
  );
}

.app-mobile-sheet-header {
  position: sticky;
  top: 0;
  z-index: 1;
  padding-top: 0.3rem;
  padding-bottom: 1rem;
  background: inherit;
}

.app-mobile-sheet-title {
  margin: 0;
  color: var(--color-text);
  font-size: 0.95rem;
  font-weight: 700;
}

.app-mobile-sheet-close {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.25rem;
  height: 2.25rem;
  border: 1px solid var(--color-border);
  border-radius: 999px;
  background: var(--color-surface);
  color: var(--color-text-muted);
}

.app-mobile-sheet-enter-active,
.app-mobile-sheet-leave-active {
  transition: opacity 0.24s ease;
}

.app-mobile-sheet-enter-active .app-mobile-sheet,
.app-mobile-sheet-leave-active .app-mobile-sheet {
  transition:
    transform 0.24s ease,
    opacity 0.24s ease;
}

.app-mobile-sheet-enter-from,
.app-mobile-sheet-leave-to {
  opacity: 0;
}

.app-mobile-sheet-enter-from .app-mobile-sheet,
.app-mobile-sheet-leave-to .app-mobile-sheet {
  transform: translateY(1.5rem);
  opacity: 0;
}
</style>
