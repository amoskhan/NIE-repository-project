<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { AppButton, AppModal, AppSwitch, useToast } from "@apptemplate/ui";
import { useAuth } from "@/composables/useAuth";
import {
  getPushNotificationProvider,
  requestPushNotificationPermission,
  setPushNotificationsSubscribed,
} from "@/services/oneSignalService";
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
} from "@/services/notificationPreferencesService";

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
  <AppModal
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
            <span
              class="notification-settings__pill notification-settings__pill--primary"
            >
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
            <AppButton
              v-if="browserPermission !== 'granted'"
              class="w-full whitespace-normal"
              variant="outline"
              size="sm"
              @click="requestPermission"
            >
              Request Permission
            </AppButton>
            <AppButton
              class="w-full whitespace-normal"
              variant="outline"
              size="sm"
              @click="selectAll()"
            >
              Enable All
            </AppButton>
            <AppButton
              class="w-full whitespace-normal"
              variant="outline"
              size="sm"
              @click="deselectAll()"
            >
              Disable All
            </AppButton>
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
            <AppSwitch
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
            <AppButton
              class="w-full"
              variant="outline"
              size="sm"
              @click="selectAll(category.id)"
            >
              All
            </AppButton>
            <AppButton
              class="w-full"
              variant="outline"
              size="sm"
              @click="deselectAll(category.id)"
            >
              None
            </AppButton>
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
              <AppSwitch
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
        <AppButton class="w-full sm:w-auto" variant="ghost" @click="closeModal">
          Cancel
        </AppButton>
        <AppButton
          class="w-full sm:w-auto"
          :loading="saving"
          @click="savePreferences"
        >
          Save
        </AppButton>
      </div>
    </template>
  </AppModal>
</template>

<style scoped>
:global(.notification-settings-modal) {
  width: min(48rem, calc(100vw - 0.75rem));
  max-height: min(46rem, calc(100dvh - 0.75rem));
  border-radius: 12px;
}

:global(.notification-settings-modal > div:first-child),
:global(.notification-settings-modal > div:last-child) {
  padding-left: 1rem;
  padding-right: 1rem;
}

:global(.notification-settings-modal > div:nth-child(2)) {
  padding: 1rem;
}

.notification-settings {
  display: grid;
  min-width: 0;
  gap: 1rem;
}

.notification-settings__overview,
.notification-settings__group {
  min-width: 0;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background-color: var(--color-surface);
  padding: 1rem;
}

.notification-settings__overview {
  display: grid;
  gap: 1rem;
}

.notification-settings__summary {
  display: grid;
  min-width: 0;
  gap: 0.875rem;
}

.notification-settings__pills {
  display: flex;
  min-width: 0;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.notification-settings__pill {
  max-width: 100%;
  border-radius: 999px;
  background-color: var(--color-surface-muted, #f8fafc);
  color: var(--color-text-muted);
  padding: 0.25rem 0.75rem;
  font-size: 0.75rem;
  font-weight: 700;
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
  font-size: 0.8125rem;
  line-height: 1.35rem;
}

.notification-settings__actions {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(9rem, 100%), 1fr));
  gap: 0.5rem;
}

.notification-settings__desktop-card,
.notification-settings__item {
  display: grid;
  min-width: 0;
  grid-template-columns: minmax(0, 1fr) auto;
  align-items: center;
  gap: 0.75rem;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background-color: var(--color-surface-muted, #f8fafc);
  padding: 0.875rem 1rem;
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
  font-size: 0.875rem;
  font-weight: 700;
  line-height: 1.35rem;
  overflow-wrap: anywhere;
}

.notification-settings__group-header {
  display: grid;
  min-width: 0;
  gap: 0.875rem;
}

.notification-settings__group-actions {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.5rem;
}

.notification-settings__items {
  display: grid;
  gap: 0.625rem;
  margin-top: 1rem;
}

.notification-settings__item-copy {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
}

.notification-settings__badge {
  display: inline-flex;
  flex: 0 0 auto;
  align-items: center;
  border-radius: 999px;
  background-color: var(--color-surface);
  color: var(--color-text-muted);
  padding: 0.25rem 0.625rem;
  font-size: 0.6875rem;
  font-weight: 700;
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
  gap: 0.75rem;
  width: 100%;
}

@media (min-width: 640px) {
  :global(.notification-settings-modal) {
    width: min(48rem, calc(100vw - 2rem));
    max-height: min(46rem, calc(100dvh - 2rem));
    border-radius: 16px;
  }

  :global(.notification-settings-modal > div:first-child),
  :global(.notification-settings-modal > div:last-child) {
    padding-left: 1.25rem;
    padding-right: 1.25rem;
  }

  :global(.notification-settings-modal > div:nth-child(2)) {
    padding: 1.25rem;
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
