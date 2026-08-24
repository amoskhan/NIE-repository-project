<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { NieButton, NieModal, NieSwitch, useToast } from "@nie/ui";
import { useAuth } from "@/composables/auth/useAuth";
import {
  getPushNotificationProvider,
  requestPushNotificationPermission,
  setPushNotificationsSubscribed,
} from "@/services/notifications/oneSignalService";
import {
  USER_NOTIFICATION_CATEGORIES,
  USER_NOTIFICATION_DEFINITIONS,
  getDefaultNotificationPreferences,
  getSubscribedNotificationDefinitions,
  isNotificationPreferencesEnabled,
  loadNotificationPreferences,
  normalizeNotificationPreferences,
  saveNotificationPreferences,
  setAllNotificationSubscriptions,
  type UserNotificationPreferences,
} from "@/services/notifications/notificationPreferencesService";

interface Props {
  modelValue: boolean;
}

const props = defineProps<Props>();

const emit = defineEmits<{
  "update:modelValue": [value: boolean];
  saved: [preferences: UserNotificationPreferences];
}>();

const toast = useToast();
const { currentUser } = useAuth();

const localPreferences = ref<UserNotificationPreferences>(
  getDefaultNotificationPreferences(),
);
const saving = ref(false);
const browserPermission = ref<NotificationPermission | "unsupported">(
  "default",
);

const permissionLabel = computed(() => {
  if (browserPermission.value === "unsupported") {
    return "Unsupported";
  }

  if (browserPermission.value === "granted") {
    return "Granted";
  }

  if (browserPermission.value === "denied") {
    return "Blocked";
  }

  return "Not requested";
});

const overallStatusLabel = computed(() =>
  isNotificationPreferencesEnabled(
    localPreferences.value,
    browserPermission.value,
  )
    ? "Enabled"
    : "Disabled",
);

const subscribedDefinitions = computed(() =>
  getSubscribedNotificationDefinitions(localPreferences.value),
);

const groupedPreferences = computed(() =>
  USER_NOTIFICATION_CATEGORIES.map((category) => {
    const items = USER_NOTIFICATION_DEFINITIONS.filter(
      (definition) => definition.categoryId === category.id,
    );

    return {
      ...category,
      items,
      enabledCount: items.filter(
        (definition) => localPreferences.value.subscriptions[definition.key],
      ).length,
    };
  }).filter((category) => category.items.length > 0),
);

async function syncPermission() {
  browserPermission.value = (
    await getPushNotificationProvider().getSubscriptionState()
  ).permission;
}

function loadPreferences() {
  localPreferences.value = normalizeNotificationPreferences(
    loadNotificationPreferences(currentUser.value?.userId),
  );
}

async function openModalState() {
  await syncPermission();
  loadPreferences();
}

watch(
  () => props.modelValue,
  (isOpen) => {
    if (isOpen) {
      void openModalState();
    }
  },
  { immediate: true },
);

function closeModal() {
  emit("update:modelValue", false);
}

async function requestPermission() {
  const result = await requestPushNotificationPermission();
  browserPermission.value = result;

  if (result === "unsupported") {
    toast.error("Browser notifications are not supported in this environment");
    return;
  }

  if (result === "granted") {
    await setPushNotificationsSubscribed(true);
    updateDesktopAlerts(true);
    toast.success("Browser notifications enabled");
    return;
  }

  toast.info("Notification permission was not granted");
}

function updateDesktopAlerts(enabled: boolean) {
  localPreferences.value = {
    ...localPreferences.value,
    desktopAlerts: enabled,
  };
}

function updateSubscription(key: string, enabled: boolean) {
  localPreferences.value = {
    ...localPreferences.value,
    subscriptions: {
      ...localPreferences.value.subscriptions,
      [key]: enabled,
    },
  };
}

function selectAll(categoryId?: string) {
  localPreferences.value = setAllNotificationSubscriptions(
    localPreferences.value,
    true,
    categoryId,
  );
}

function deselectAll(categoryId?: string) {
  localPreferences.value = setAllNotificationSubscriptions(
    localPreferences.value,
    false,
    categoryId,
  );
}

async function savePreferences() {
  saving.value = true;
  const saved = saveNotificationPreferences(
    currentUser.value?.userId,
    localPreferences.value,
  );

  try {
    if (!saved.desktopAlerts || browserPermission.value === "denied") {
      await setPushNotificationsSubscribed(false);
    } else if (browserPermission.value === "granted") {
      await setPushNotificationsSubscribed(true);
    }

    toast.success("Notification preferences saved");
    emit("saved", saved);
    emit("update:modelValue", false);
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <NieModal
    :model-value="modelValue"
    title="Notification Settings"
    size="full"
    class="notification-settings-modal"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <div class="notification-settings">
      <section class="notification-settings__overview">
        <div class="notification-settings__summary">
          <div class="notification-settings__pills">
            <span class="notification-settings__pill notification-settings__pill--primary">
              Status: {{ overallStatusLabel }}
            </span>
            <span class="notification-settings__pill">
              Browser: {{ permissionLabel }}
            </span>
            <span class="notification-settings__pill">
              {{ subscribedDefinitions.length }} of
              {{ USER_NOTIFICATION_DEFINITIONS.length }} enabled
            </span>
          </div>

          <p class="notification-settings__hint">
            Choose which alerts appear in your inbox and browser.
          </p>

          <div class="notification-settings__actions">
            <NieButton
              v-if="browserPermission !== 'granted'"
              class="w-full whitespace-normal"
              variant="outline"
              size="sm"
              @click="requestPermission"
            >
              Request Permission
            </NieButton>
            <NieButton
              class="w-full whitespace-normal"
              variant="outline"
              size="sm"
              @click="selectAll()"
            >
              Enable All
            </NieButton>
            <NieButton
              class="w-full whitespace-normal"
              variant="outline"
              size="sm"
              @click="deselectAll()"
            >
              Disable All
            </NieButton>
          </div>
        </div>

        <div class="notification-settings__desktop-card">
          <div class="notification-settings__desktop-copy">
            <p class="notification-settings__desktop-title">Desktop alerts</p>
            <p class="notification-settings__desktop-hint">
              Show browser pop-ups for enabled items.
            </p>
          </div>

          <div class="notification-settings__switch-wrap">
            <NieSwitch
              aria-label="Toggle desktop alerts"
              :model-value="localPreferences.desktopAlerts"
              size="sm"
              @update:model-value="updateDesktopAlerts(Boolean($event))"
            />
          </div>
        </div>
      </section>

      <section
        v-for="category in groupedPreferences"
        :key="category.id"
        class="notification-settings__group"
      >
        <div class="notification-settings__group-header">
          <div class="notification-settings__group-copy">
            <p class="notification-settings__group-title">
              {{ category.label }}
            </p>
            <p class="notification-settings__group-count">
              {{ category.enabledCount }} of {{ category.items.length }} enabled
            </p>
          </div>

          <div class="notification-settings__group-actions">
            <NieButton
              class="w-full"
              variant="outline"
              size="sm"
              @click="selectAll(category.id)"
            >
              All
            </NieButton>
            <NieButton
              class="w-full"
              variant="outline"
              size="sm"
              @click="deselectAll(category.id)"
            >
              None
            </NieButton>
          </div>
        </div>

        <div class="notification-settings__items">
          <article
            v-for="definition in category.items"
            :key="definition.key"
            class="notification-settings__item"
          >
            <div class="notification-settings__item-copy">
              <p class="notification-settings__item-title">
                {{ definition.label }}
              </p>
              <span
                class="notification-settings__badge"
                :class="
                  localPreferences.subscriptions[definition.key]
                    ? 'notification-settings__badge--enabled'
                    : ''
                "
              >
                {{
                  localPreferences.subscriptions[definition.key]
                    ? "Enabled"
                    : "Disabled"
                }}
              </span>
            </div>

            <div class="notification-settings__switch-wrap">
              <NieSwitch
                :aria-label="`Toggle ${definition.label}`"
                :model-value="localPreferences.subscriptions[definition.key]"
                size="sm"
                @update:model-value="
                  updateSubscription(definition.key, Boolean($event))
                "
              />
            </div>
          </article>
        </div>
      </section>
    </div>

    <template #footer>
      <div class="notification-settings__footer">
        <NieButton class="w-full sm:w-auto" variant="ghost" @click="closeModal">
          Cancel
        </NieButton>
        <NieButton
          class="w-full sm:w-auto"
          :loading="saving"
          @click="savePreferences"
        >
          Save
        </NieButton>
      </div>
    </template>
  </NieModal>
</template>

<style scoped>
:global(.notification-settings-modal) {
  width: min(48rem, calc(100vw - 0.75rem));
  max-height: min(46rem, calc(100dvh - 0.75rem));
  border-radius: var(--theme-radius-control);
}

:global(.notification-settings-modal > div:first-child),
:global(.notification-settings-modal > div:last-child) {
  padding-left: var(--theme-space-4);
  padding-right: var(--theme-space-4);
}

:global(.notification-settings-modal > div:nth-child(2)) {
  padding: var(--theme-space-4);
}

.notification-settings {
  display: grid;
  min-width: 0;
  gap: var(--theme-space-4);
}

.notification-settings__overview,
.notification-settings__group {
  min-width: 0;
  border: 1px solid var(--color-border);
  border-radius: var(--theme-radius-control);
  background-color: var(--color-surface);
  padding: var(--theme-space-4);
}

.notification-settings__overview {
  display: grid;
  gap: var(--theme-space-4);
}

.notification-settings__summary {
  display: grid;
  min-width: 0;
  gap: var(--theme-space-3);
}

.notification-settings__pills {
  display: flex;
  min-width: 0;
  flex-wrap: wrap;
  gap: var(--theme-space-2);
}

.notification-settings__pill {
  max-width: 100%;
  border-radius: var(--theme-radius-pill);
  background-color: var(--color-surface-muted, var(--theme-color-surface-subtle));
  color: var(--color-text-muted);
  padding: var(--theme-space-1) var(--theme-space-3);
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-bold);
  line-height: 1.25rem;
}

.notification-settings__pill--primary {
  background-color: var(--color-sidebar-active);
  color: var(--color-primary);
}

.notification-settings__hint,
.notification-settings__desktop-hint,
.notification-settings__group-count {
  color: var(--color-text-muted);
  font-size: var(--theme-font-size-label);
  line-height: 1.35rem;
}

.notification-settings__actions {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(9rem, 100%), 1fr));
  gap: var(--theme-space-2);
}

.notification-settings__desktop-card,
.notification-settings__item {
  display: grid;
  min-width: 0;
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: center;
  gap: var(--theme-space-3);
  border: 1px solid var(--color-border);
  border-radius: var(--theme-radius-control);
  background-color: var(--color-surface-muted, var(--theme-color-surface-subtle));
  padding: var(--theme-space-3) var(--theme-space-4);
}

.notification-settings__desktop-copy,
.notification-settings__group-copy,
.notification-settings__item-copy {
  min-width: 0;
}

.notification-settings__desktop-title,
.notification-settings__group-title,
.notification-settings__item-title {
  color: var(--color-text);
  font-size: var(--theme-font-size-body);
  font-weight: var(--theme-font-weight-bold);
  line-height: 1.35rem;
  overflow-wrap: anywhere;
}

.notification-settings__group-header {
  display: grid;
  min-width: 0;
  gap: var(--theme-space-3);
}

.notification-settings__group-actions {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--theme-space-2);
}

.notification-settings__items {
  display: grid;
  gap: var(--theme-space-2);
  margin-top: var(--theme-space-4);
}

.notification-settings__item-copy {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--theme-space-2);
}

.notification-settings__badge {
  display: inline-flex;
  flex: 0 0 auto;
  align-items: center;
  border-radius: var(--theme-radius-pill);
  background-color: var(--color-surface);
  color: var(--color-text-muted);
  padding: var(--theme-space-1) var(--theme-space-2);
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-bold);
  line-height: 1rem;
}

.notification-settings__badge--enabled {
  background-color: var(--color-sidebar-active);
  color: var(--color-primary);
}

.notification-settings__switch-wrap {
  display: flex;
  justify-content: flex-end;
}

.notification-settings__footer {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--theme-space-3);
  width: 100%;
}

@media (min-width: 640px) {
  :global(.notification-settings-modal) {
    width: min(48rem, calc(100vw - 2rem));
    max-height: min(46rem, calc(100dvh - 2rem));
    border-radius: var(--theme-radius-panel);
  }

  :global(.notification-settings-modal > div:first-child),
  :global(.notification-settings-modal > div:last-child) {
    padding-left: var(--theme-space-5);
    padding-right: var(--theme-space-5);
  }

  :global(.notification-settings-modal > div:nth-child(2)) {
    padding: var(--theme-space-5);
  }

  .notification-settings__footer {
    display: flex;
    justify-content: flex-end;
  }
}

@media (min-width: 768px) {
  .notification-settings__overview {
    grid-template-columns: minmax(0, 1fr) minmax(16rem, 0.85fr);
    align-items: start;
  }

  .notification-settings__group-header {
    grid-template-columns: minmax(0, 1fr) auto;
    align-items: start;
  }
}

@media (max-width: 420px) {
  .notification-settings__desktop-card,
  .notification-settings__item {
    grid-template-columns: minmax(0, 1fr);
  }
}
</style>
