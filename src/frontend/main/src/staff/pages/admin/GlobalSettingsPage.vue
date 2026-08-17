<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import {
  AppButton,
  AppDataTable,
  AppInput,
  AppModal,
  AppSelect,
  AppSwitch,
  useToast,
} from "@apptemplate/ui";
import globalSettingsService from "@/services/globalSettingsService";
import {
  ADMIN_NOTIFICATION_CATEGORIES,
  ADMIN_NOTIFICATION_SETTING_DEFINITIONS,
  type AdminNotificationSettingDefinition,
} from "@/services/notificationPreferencesService";
import type { GlobalSettings } from "@/types";
import { buildFilterOptions } from "@/utils/listFilterOptions";

const toast = useToast();
const router = useRouter();
const settings = ref<GlobalSettings[]>([]);
const loading = ref(true);
const saving = ref(false);
const savingAdminNotifications = ref(false);
const showModal = ref(false);
const editing = ref<GlobalSettings | null>(null);
const search = ref("");
const selectedFilters = ref<Record<string, Array<string | number | boolean>>>(
  {},
);
const updatingAdminNotificationKey = ref<string | null>(null);

const form = ref({
  key: "",
  value: "",
  description: "",
  dataType: "String",
});

const columns = [
  { key: "key", label: "Key" },
  { key: "value", label: "Value" },
  { key: "dataType", label: "Type" },
  { key: "description", label: "Description" },
];

const dataTypeOptions = ["String", "Integer", "Decimal", "Boolean", "Json"];

const filterGroups = computed(() => [
  {
    key: "dataType",
    label: "Type",
    options: buildFilterOptions(settings.value, (setting) => setting.dataType),
  },
]);

const adminNotificationGroups = computed(() =>
  ADMIN_NOTIFICATION_CATEGORIES.map((category) => {
    const items = ADMIN_NOTIFICATION_SETTING_DEFINITIONS.filter(
      (definition) => definition.categoryId === category.id,
    );

    return {
      ...category,
      items,
      enabledCount: items.filter((definition) =>
        isAdminNotificationEnabled(definition),
      ).length,
    };
  }).filter((category) => category.items.length > 0),
);

const enabledAdminNotificationCount = computed(
  () =>
    ADMIN_NOTIFICATION_SETTING_DEFINITIONS.filter((definition) =>
      isAdminNotificationEnabled(definition),
    ).length,
);

onMounted(async () => {
  await loadSettings();
});

async function loadSettings() {
  loading.value = true;
  try {
    settings.value = await globalSettingsService.getAll();
  } catch {
    toast.error("Failed to load settings");
  } finally {
    loading.value = false;
  }
}

function openModal(setting?: GlobalSettings) {
  if (setting) {
    editing.value = setting;
    form.value = {
      key: setting.key,
      value: setting.value,
      description: setting.description || "",
      dataType: setting.dataType,
    };
  } else {
    editing.value = null;
    form.value = { key: "", value: "", description: "", dataType: "String" };
  }
  showModal.value = true;
}

async function save() {
  if (!form.value.key.trim()) {
    toast.error("Key is required.");
    return;
  }

  saving.value = true;

  try {
    const payload = {
      ...(editing.value ? { id: editing.value.id } : {}),
      ...form.value,
    };

    const result = await globalSettingsService.save(payload);

    if (editing.value) {
      const index = settings.value.findIndex((item) => item.id === result.id);
      if (index >= 0) {
        settings.value[index] = result;
      }
    } else {
      settings.value.push(result);
    }

    toast.success("Setting saved");
    showModal.value = false;
  } catch {
    toast.error("Failed to save setting");
  } finally {
    saving.value = false;
  }
}

async function deleteSetting(setting: GlobalSettings) {
  try {
    await globalSettingsService.delete(setting.id);
    settings.value = settings.value.filter((item) => item.id !== setting.id);
    toast.success("Setting deleted");
  } catch {
    toast.error("Failed to delete setting");
  }
}

function getDataTypeBadge(dataType: string): string {
  const map: Record<string, string> = {
    String: "bg-blue-100 text-blue-700",
    Integer: "bg-purple-100 text-purple-700",
    Decimal: "bg-orange-100 text-orange-700",
    Boolean: "bg-emerald-100 text-emerald-700",
    Json: "bg-slate-100 text-slate-700",
  };

  return map[dataType] || "bg-slate-100 text-slate-600";
}

function findSettingByKey(key: string): GlobalSettings | undefined {
  return settings.value.find((setting) => setting.key === key);
}

function upsertSetting(nextSetting: GlobalSettings) {
  const index = settings.value.findIndex(
    (setting) =>
      setting.id === nextSetting.id || setting.key === nextSetting.key,
  );

  if (index >= 0) {
    settings.value[index] = nextSetting;
    return;
  }

  settings.value.push(nextSetting);
}

function isAdminNotificationEnabled(
  definition: AdminNotificationSettingDefinition,
): boolean {
  const setting = findSettingByKey(definition.key);

  if (!setting) {
    return definition.defaultValue;
  }

  return setting.value.trim().toLowerCase() === "true";
}

function isAdminNotificationUpdating(key: string): boolean {
  return (
    savingAdminNotifications.value || updatingAdminNotificationKey.value === key
  );
}

async function updateAdminNotificationSetting(
  definition: AdminNotificationSettingDefinition,
  enabled: boolean,
) {
  updatingAdminNotificationKey.value = definition.key;

  try {
    const existing = findSettingByKey(definition.key);
    const saved = await globalSettingsService.save({
      id: existing?.id,
      key: definition.key,
      value: enabled ? "true" : "false",
      description: existing?.description ?? definition.description,
      dataType: "Boolean",
    });

    upsertSetting(saved);
    toast.success(`${definition.label} ${enabled ? "enabled" : "disabled"}`);
  } catch {
    toast.error(`Failed to update ${definition.label.toLowerCase()}`);
  } finally {
    if (updatingAdminNotificationKey.value === definition.key) {
      updatingAdminNotificationKey.value = null;
    }
  }
}

async function setAllAdminNotifications(enabled: boolean) {
  savingAdminNotifications.value = true;
  updatingAdminNotificationKey.value = null;

  try {
    for (const definition of ADMIN_NOTIFICATION_SETTING_DEFINITIONS) {
      const existing = findSettingByKey(definition.key);
      const saved = await globalSettingsService.save({
        id: existing?.id,
        key: definition.key,
        value: enabled ? "true" : "false",
        description: existing?.description ?? definition.description,
        dataType: "Boolean",
      });

      upsertSetting(saved);
    }

    toast.success(
      `Administration notification defaults ${enabled ? "enabled" : "disabled"}`,
    );
  } catch {
    toast.error("Failed to update administration notification defaults");
  } finally {
    savingAdminNotifications.value = false;
  }
}

function handleModalChange(value: boolean) {
  showModal.value = value;
}
</script>

<template>
  <div class="space-y-4 flex flex-col flex-1 min-h-0">
    <AppDataTable
      class="flex-1 min-h-0"
      v-model:search="search"
      v-model:selected-filters="selectedFilters"
      :columns="columns"
      :data="settings"
      row-key="id"
      :loading="loading"
      :filter-groups="filterGroups"
      search-placeholder="Search all settings"
      create-label="New Setting"
      @create="openModal()"
      @edit="openModal"
      @delete="deleteSetting"
      @retry="loadSettings"
    >
      <template #toolbar-actions>
        <div class="flex flex-wrap items-center gap-2">
          <AppButton
            variant="outline"
            size="sm"
            @click="router.push({ name: 'monitoring' })"
          >
            <span class="material-symbols-outlined text-[18px]"
              >monitoring</span
            >
            <span>Monitoring</span>
          </AppButton>
        </div>
      </template>

      <template #cell-key="{ value }">
        <span class="font-mono font-semibold">{{ value }}</span>
      </template>

      <template #cell-dataType="{ value }">
        <span
          class="rounded-full px-2 py-1 text-xs font-bold"
          :class="getDataTypeBadge(String(value ?? ''))"
        >
          {{ value }}
        </span>
      </template>

      <template #cell-description="{ value }">
        {{ value || "-" }}
      </template>
    </AppDataTable>

    <section
      class="overflow-hidden rounded-3xl border border-slate-200 bg-white p-6 shadow-sm"
    >
      <div
        class="flex flex-col gap-3 md:flex-row md:items-start md:justify-between"
      >
        <div class="max-w-3xl">
          <p
            class="text-xs font-bold uppercase tracking-[0.2em] text-slate-400"
          >
            Administration Notifications
          </p>
          <h2 class="mt-2 text-xl font-bold text-slate-900">
            Global Notification Defaults
          </h2>
          <p class="mt-2 text-sm leading-6 text-slate-500">
            Administration-related notification settings live here now. Personal
            notification preferences are available from the top-right profile
            menu and the notification tray.
          </p>
        </div>

        <div
          class="flex w-full flex-wrap items-center gap-2 md:w-auto md:justify-end"
        >
          <span
            class="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600"
          >
            {{ enabledAdminNotificationCount }} enabled
          </span>
          <AppButton
            variant="outline"
            size="sm"
            :disabled="savingAdminNotifications"
            @click="setAllAdminNotifications(true)"
          >
            Select All
          </AppButton>
          <AppButton
            variant="outline"
            size="sm"
            :disabled="savingAdminNotifications"
            @click="setAllAdminNotifications(false)"
          >
            Deselect All
          </AppButton>
        </div>
      </div>

      <div class="mt-6 space-y-4">
        <section
          v-for="category in adminNotificationGroups"
          :key="category.id"
          class="min-w-0 rounded-2xl border border-slate-200 p-5"
        >
          <div
            class="flex flex-col gap-3 md:flex-row md:items-start md:justify-between"
          >
            <div>
              <p
                class="text-xs font-bold uppercase tracking-[0.2em] text-slate-400"
              >
                {{ category.label }}
              </p>
              <p class="mt-2 text-sm leading-6 text-slate-500">
                {{ category.description }}
              </p>
            </div>

            <span
              class="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-600"
            >
              {{ category.enabledCount }} of {{ category.items.length }} enabled
            </span>
          </div>

          <div class="mt-5 grid gap-3 lg:grid-cols-2">
            <article
              v-for="definition in category.items"
              :key="definition.key"
              class="min-w-0 rounded-2xl border border-slate-200 p-4"
            >
              <div
                class="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between"
              >
                <div class="min-w-0">
                  <h3 class="text-sm font-semibold text-slate-900">
                    {{ definition.label }}
                  </h3>
                  <p class="mt-1 text-sm leading-6 text-slate-500">
                    {{ definition.description }}
                  </p>
                  <p
                    class="mt-2 break-all text-[11px] font-mono text-slate-400"
                  >
                    {{ definition.key }}
                  </p>
                </div>

                <div
                  class="flex shrink-0 items-center gap-3 self-start sm:self-auto"
                >
                  <span
                    class="text-xs font-semibold uppercase tracking-[0.16em] text-slate-400"
                  >
                    {{
                      isAdminNotificationEnabled(definition)
                        ? "Enabled"
                        : "Disabled"
                    }}
                  </span>
                  <AppSwitch
                    :model-value="isAdminNotificationEnabled(definition)"
                    :disabled="isAdminNotificationUpdating(definition.key)"
                    @update:model-value="
                      updateAdminNotificationSetting(
                        definition,
                        Boolean($event),
                      )
                    "
                  />
                </div>
              </div>
            </article>
          </div>
        </section>
      </div>
    </section>

    <AppModal
      :model-value="showModal"
      :title="editing ? 'Edit Setting' : 'New Setting'"
      @update:model-value="handleModalChange"
    >
      <div class="space-y-4">
        <AppInput
          v-model="form.key"
          label="Key"
          :disabled="!!editing"
          placeholder="e.g. MaxPreferences"
        />

        <AppInput
          v-model="form.value"
          label="Value"
          placeholder="Setting value"
        />

        <AppSelect
          v-model="form.dataType"
          label="Data Type"
          :options="
            dataTypeOptions.map((dataType) => ({
              value: dataType,
              label: dataType,
            }))
          "
          placeholder="Select data type"
        />

        <div>
          <label
            class="mb-1 block text-sm font-medium text-secondary-700 dark:text-secondary-300"
          >
            Description
          </label>
          <textarea
            v-model="form.description"
            rows="3"
            class="w-full rounded-xl border border-secondary-300 px-3 py-2.5 text-sm text-secondary-900 outline-none transition focus:border-primary-500 focus:ring-2 focus:ring-primary-500 dark:border-secondary-600 dark:bg-secondary-800 dark:text-secondary-100"
            placeholder="Optional description"
          ></textarea>
        </div>
      </div>

      <template #footer>
        <div class="flex items-center justify-end gap-3">
          <AppButton variant="ghost" @click="showModal = false"
            >Cancel</AppButton
          >
          <AppButton :loading="saving" @click="save">Save</AppButton>
        </div>
      </template>
    </AppModal>
  </div>
</template>
