import api from "../core/api";
import type {
  NieDataTableFilterOptionPage,
  NieDataTableFilterOptionsRequest,
  NieDataTableQuery,
} from "@nie/ui";
import type { ServerDataTablePage } from "@/composables/data-tables/useServerDataTable";
import {
  toApiDataTableRequest,
  toApiFilterOptionsRequest,
} from "../core/dataTableApi";

export interface NotificationPolicy {
  id: string;
  eventKey: string;
  displayName: string;
  description: string;
  category: string;
  inAppEnabled: boolean;
  emailEnabled: boolean;
  pushEnabled: boolean;
  isActive: boolean;
  supportsReminderConfiguration: boolean;
  reminderAfterHours: number | null;
  escalationAfterHours: number | null;
}

export interface NotificationPolicyUpdatePayload {
  inAppEnabled: boolean;
  emailEnabled: boolean;
  pushEnabled: boolean;
  isActive: boolean;
  reminderAfterHours: number | null;
  escalationAfterHours: number | null;
}
export interface NotificationTemplate {
  id: string;
  eventKey: string;
  channel: string;
  version: number;
  subject: string;
  content: string;
  isPublished: boolean;
  publishedBy?: string | null;
  publishedOn?: string | null;
}

export interface NotificationDelivery {
  id: string;
  eventKey: string;
  correlationKey: string;
  recipientUserId: string;
  recipientName?: string | null;
  recipientEmail?: string | null;
  channel: string;
  status: string;
  attempts: number;
  sentOn?: string | null;
  nextAttemptOn?: string | null;
  lastError?: string | null;
  createdOn?: string | null;
}

export interface NotificationChannelHealth {
  emailConfigured: boolean;
  pushNotificationsConfigured: boolean;
  realtimeConfigured: boolean;
}

export interface NotificationAdministrationOverview {
  policies: NotificationPolicy[];
  templates: NotificationTemplate[];
  recentDeliveries: NotificationDelivery[];
  deliveryStatusCounts: Record<string, number>;
  channelHealth: NotificationChannelHealth;
  allowedPlaceholders: string[];
}

const notificationAdministrationService = {
  async searchDeliveries(
    query: NieDataTableQuery,
  ): Promise<ServerDataTablePage<NotificationDelivery>> {
    return (
      await api.post<ServerDataTablePage<NotificationDelivery>>(
        "/api/NotificationAdministration/SearchDeliveries",
        toApiDataTableRequest(query),
      )
    ).data;
  },

  async getDeliveryFilterOptions(
    request: NieDataTableFilterOptionsRequest,
  ): Promise<NieDataTableFilterOptionPage> {
    return (
      await api.post<NieDataTableFilterOptionPage>(
        "/api/NotificationAdministration/GetDeliveryFilterOptions",
        toApiFilterOptionsRequest(request),
      )
    ).data;
  },

  async getOverview(): Promise<NotificationAdministrationOverview> {
    return (
      await api.get<NotificationAdministrationOverview>(
        "/api/NotificationAdministration/GetOverview",
      )
    ).data;
  },

  async updatePolicy(
    eventKey: string,
    payload: NotificationPolicyUpdatePayload,
  ): Promise<void> {
    await api.put(
      `/api/NotificationAdministration/UpdatePolicy/${encodeURIComponent(eventKey)}`,
      payload,
    );
  },

  async saveTemplate(input: {
    eventKey: string;
    subject: string;
    content: string;
    publish: boolean;
  }): Promise<NotificationTemplate> {
    return (
      await api.post<NotificationTemplate>(
        "/api/NotificationAdministration/SaveTemplate",
        input,
      )
    ).data;
  },

  async publishTemplate(id: string): Promise<void> {
    await api.post(`/api/NotificationAdministration/PublishTemplate/${id}`);
  },

  async retryDelivery(id: string): Promise<void> {
    await api.post(`/api/NotificationAdministration/RetryDelivery/${id}`);
  },

  async sendTest(channel: "InApp" | "Email" | "Push"): Promise<void> {
    await api.post("/api/NotificationAdministration/SendTest", { channel });
  },
};

export default notificationAdministrationService;
