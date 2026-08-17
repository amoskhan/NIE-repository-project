import api from "./api";
import type { GlobalSettings } from "@/types";
import { FRONTEND_CONSTANTS } from "@apptemplate/shared";

const GLOBAL_SETTINGS_STORAGE_KEY = "app-template-demo-global-settings";
const useDemoGlobalSettings = FRONTEND_CONSTANTS.features.useDemoGlobalSettings;

function createDemoSettings(): GlobalSettings[] {
  return [
    {
      id: 1,
      key: "Procurement.DefaultCurrency",
      value: "SGD",
      description:
        "Default currency applied to procurement totals and order summaries.",
      dataType: "String",
    },
    {
      id: 2,
      key: "Procurement.DefaultDeliveryLocation",
      value: "North Wing",
      description:
        "Primary seeded delivery location used when a request starts without an explicit destination.",
      dataType: "String",
    },
    {
      id: 3,
      key: "Notifications.Push.Enabled",
      value: "true",
      description:
        "Enable browser push notifications for approval and admin alerts.",
      dataType: "Boolean",
    },
    {
      id: 4,
      key: "Notifications.Push.Provider",
      value: "OneSignal",
      description:
        "Push delivery provider configured for the current frontend environment.",
      dataType: "String",
    },
    {
      id: 5,
      key: "Monitoring.Sentry.Enabled",
      value: FRONTEND_CONSTANTS.sentry.enabled ? "true" : "false",
      description:
        "Indicates whether Sentry error monitoring is configured for the frontend application.",
      dataType: "Boolean",
    },
    {
      id: 6,
      key: "Monitoring.Sentry.Environment",
      value: FRONTEND_CONSTANTS.sentry.environment,
      description:
        "Sentry environment name used for error and performance events.",
      dataType: "String",
    },
    {
      id: 7,
      key: "Monitoring.Uptime.Endpoint",
      value: "/health",
      description:
        "Primary backend uptime probe endpoint used by local monitoring checks.",
      dataType: "String",
    },
    {
      id: 8,
      key: "Notifications.Admin.ApprovalBacklog.Enabled",
      value: "true",
      description:
        "Notify administrators when approval queues age, spike, or breach SLA thresholds.",
      dataType: "Boolean",
    },
    {
      id: 9,
      key: "Notifications.Admin.Monitoring.Enabled",
      value: "true",
      description:
        "Notify administrators when uptime probes or monitoring integrations report incidents.",
      dataType: "Boolean",
    },
    {
      id: 10,
      key: "Notifications.Admin.AccessControl.Enabled",
      value: "true",
      description:
        "Notify administrators when roles, assignments, or access functions change.",
      dataType: "Boolean",
    },
    {
      id: 11,
      key: "Notifications.Admin.Audit.Enabled",
      value: "false",
      description:
        "Notify administrators when unusual audit activity needs follow-up.",
      dataType: "Boolean",
    },
    {
      id: 12,
      key: "Notifications.Admin.Configuration.Enabled",
      value: "true",
      description:
        "Notify administrators when global settings or notification defaults change.",
      dataType: "Boolean",
    },
  ];
}

function readDemoSettings(): GlobalSettings[] {
  const fallback = createDemoSettings();

  if (typeof window === "undefined") {
    return fallback;
  }

  try {
    const raw = window.localStorage.getItem(GLOBAL_SETTINGS_STORAGE_KEY);
    if (!raw) {
      window.localStorage.setItem(
        GLOBAL_SETTINGS_STORAGE_KEY,
        JSON.stringify(fallback),
      );
      return fallback;
    }

    return JSON.parse(raw) as GlobalSettings[];
  } catch {
    return fallback;
  }
}

function writeDemoSettings(settings: GlobalSettings[]) {
  if (typeof window === "undefined") {
    return;
  }

  try {
    window.localStorage.setItem(
      GLOBAL_SETTINGS_STORAGE_KEY,
      JSON.stringify(settings),
    );
  } catch {
    // Ignore local storage failures for demo-only settings.
  }
}

const globalSettingsService = {
  async getAll(): Promise<GlobalSettings[]> {
    if (useDemoGlobalSettings) {
      return readDemoSettings();
    }

    return (await api.get<GlobalSettings[]>("/api/GlobalSettings/GetAll")).data;
  },

  async getByKey(key: string): Promise<GlobalSettings> {
    if (useDemoGlobalSettings) {
      const match = readDemoSettings().find((setting) => setting.key === key);
      if (!match) {
        throw new Error(`Setting not found: ${key}`);
      }
      return match;
    }

    return (
      await api.get<GlobalSettings>(`/api/GlobalSettings/GetByKey?key=${key}`)
    ).data;
  },

  async save(setting: Partial<GlobalSettings>): Promise<GlobalSettings> {
    if (useDemoGlobalSettings) {
      const settings = readDemoSettings();
      const existingIndex = setting.id
        ? settings.findIndex((item) => item.id === setting.id)
        : -1;

      const nextSetting: GlobalSettings = {
        id:
          setting.id ??
          settings.reduce((maxId, item) => Math.max(maxId, item.id), 0) + 1,
        key: setting.key?.trim() || "",
        value: setting.value?.trim() || "",
        description: setting.description?.trim() || null,
        dataType: setting.dataType || "String",
      };

      if (existingIndex >= 0) {
        settings.splice(existingIndex, 1, nextSetting);
      } else {
        settings.push(nextSetting);
      }

      writeDemoSettings(settings);
      return nextSetting;
    }

    return (await api.post<GlobalSettings>("/api/GlobalSettings/Save", setting))
      .data;
  },

  async setValue(key: string, value: string): Promise<GlobalSettings> {
    if (useDemoGlobalSettings) {
      const setting = await this.getByKey(key);
      return this.save({ ...setting, value });
    }

    return (
      await api.post<GlobalSettings>("/api/GlobalSettings/SetValue", {
        key,
        value,
      })
    ).data;
  },

  async delete(id: number): Promise<void> {
    if (useDemoGlobalSettings) {
      writeDemoSettings(
        readDemoSettings().filter((setting) => setting.id !== id),
      );
      return;
    }

    await api.delete(`/api/GlobalSettings/Delete?id=${id}`);
  },
};

export default globalSettingsService;
