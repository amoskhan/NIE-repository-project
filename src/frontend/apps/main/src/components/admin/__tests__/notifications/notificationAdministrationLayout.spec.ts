import { flushPromises, mount } from "@vue/test-utils";
import { beforeEach, describe, expect, it, vi } from "vitest";
import NotificationAdministration from "@/components/admin/notifications/NotificationAdministration.vue";

const service = vi.hoisted(() => ({
  getOverview: vi.fn(),
  updatePolicy: vi.fn(),
  saveTemplate: vi.fn(),
  publishTemplate: vi.fn(),
  retryDelivery: vi.fn(),
  sendTest: vi.fn(),
}));

const routing = vi.hoisted(() => ({
  route: { query: {} as Record<string, string> },
  replace: vi.fn(),
}));

vi.mock("vue-router", () => ({
  useRoute: () => routing.route,
  useRouter: () => ({ replace: routing.replace }),
}));

vi.mock("@/services/notifications/notificationAdministrationService", () => ({
  default: service,
}));

function policy(overrides: Record<string, unknown> = {}) {
  return {
    id: "0198fc41-bdf2-7a85-8475-7a2412147ebc",
    eventKey: "procurement.purchase-order.submitted",
    displayName: "Purchase order submitted",
    description: "Transactional update",
    category: "Order updates",
    inAppEnabled: true,
    emailEnabled: true,
    pushEnabled: false,
    isActive: true,
    supportsReminderConfiguration: false,
    reminderAfterHours: null,
    escalationAfterHours: null,
    ...overrides,
  };
}

describe("NotificationAdministration layout", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    routing.route.query = {};
    service.getOverview.mockResolvedValue({
      policies: [
        policy(),
        policy({
          id: "0198fc41-bdf2-7a85-8475-7a2412147ebd",
          eventKey: "procurement.purchase-order.delivery.reminder",
          displayName: "Delivery reminder",
          supportsReminderConfiguration: true,
          reminderAfterHours: 24,
          escalationAfterHours: 72,
        }),
      ],
      templates: [],
      recentDeliveries: [],
      channelHealth: {
        emailConfigured: true,
        pushNotificationsConfigured: false,
        realtimeConfigured: true,
      },
      allowedPlaceholders: [],
    });
  });

  it("starts with the shared tabs and omits the orchestration summary", async () => {
    const wrapper = mount(NotificationAdministration);
    await flushPromises();

    const root = wrapper.get(".notification-admin");
    expect(root.element.firstElementChild?.getAttribute("role")).toBe(
      "tablist",
    );
    expect(wrapper.text()).not.toContain("Delivery orchestration");
    expect(wrapper.text()).not.toContain("Refresh");
    expect(wrapper.find(".channel-rail").exists()).toBe(false);
    expect(wrapper.get('[role="tabpanel"]').attributes("aria-labelledby")).toBe(
      "notification-tabs-policies",
    );
  });

  it("places optional reminder timing below the channel switches", async () => {
    const wrapper = mount(NotificationAdministration);
    await flushPromises();

    const rows = wrapper.findAll(".policy-row");
    const reminderRow = rows.find((row) =>
      row.text().includes("Delivery reminder"),
    );
    const standardRow = rows.find((row) =>
      row.text().includes("Purchase order submitted"),
    );

    expect(reminderRow).toBeDefined();
    const configuration = reminderRow!.get(".policy-row__configuration");
    expect(configuration.element.children[0]).toBe(
      reminderRow!.get(".policy-row__channels").element,
    );
    expect(configuration.element.children[1]).toBe(
      reminderRow!.get(".policy-row__timing").element,
    );
    expect(standardRow!.find(".policy-row__timing").exists()).toBe(false);
  });

  it("normalizes an invalid tab query on the initial route", async () => {
    routing.route.query = { tab: "bogus", filter: "open" };

    mount(NotificationAdministration);
    await flushPromises();

    expect(routing.replace).toHaveBeenCalledWith({
      name: "notification-administration",
      query: { filter: "open" },
    });
  });

  it("keeps an overview failure visible and recovers through the retry action", async () => {
    service.getOverview.mockRejectedValueOnce(new Error("unavailable"));

    const wrapper = mount(NotificationAdministration);
    await flushPromises();

    expect(wrapper.text()).toContain("Unable to load notification configuration");
    expect(wrapper.text()).toContain(
      "Notification configuration could not be loaded. Try again.",
    );
    expect(wrapper.find(".policy-groups").exists()).toBe(false);

    const recoveredOverview = {
      policies: [policy()],
      templates: [],
      recentDeliveries: [],
      channelHealth: {
        emailConfigured: true,
        pushNotificationsConfigured: false,
        realtimeConfigured: true,
      },
      allowedPlaceholders: [],
    };
    service.getOverview.mockResolvedValueOnce(recoveredOverview);

    await wrapper.get('button[aria-label="Retry loading notification configuration"]').trigger("click");
    await flushPromises();

    expect(service.getOverview).toHaveBeenCalledTimes(2);
    expect(wrapper.text()).not.toContain("Unable to load notification configuration");
    expect(wrapper.text()).toContain("Purchase order submitted");
  });
});
