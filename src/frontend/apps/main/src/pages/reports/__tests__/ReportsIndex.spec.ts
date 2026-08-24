import { flushPromises, mount } from "@vue/test-utils";
import { beforeEach, describe, expect, it, vi } from "vitest";
import ReportsIndex from "@/pages/reports/ReportsIndex.vue";

const reportService = vi.hoisted(() => ({
  getReportTypes: vi.fn(),
}));

const router = vi.hoisted(() => ({
  push: vi.fn(),
}));

const fallbackReports = vi.hoisted(() => [
  {
    id: "fallback-report",
    name: "Default procurement report",
    description: "Fallback report description",
    category: "Procurement",
    icon: "description",
    filters: [],
  },
]);

vi.mock("vue-router", () => ({
  useRouter: () => router,
}));

vi.mock("@/services/reports/reportService", () => ({
  default: reportService,
  DEFAULT_REPORT_TYPES: fallbackReports,
  isReportRequestCanceled: () => false,
}));

describe("ReportsIndex degraded state", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("keeps fallback reports usable and clears the warning after retry succeeds", async () => {
    reportService.getReportTypes.mockRejectedValueOnce(new Error("unavailable"));

    const wrapper = mount(ReportsIndex);
    await flushPromises();

    expect(wrapper.get('[role="alert"]').text()).toContain(
      "Showing default reports",
    );
    expect(wrapper.text()).toContain("Default procurement report");

    reportService.getReportTypes.mockResolvedValueOnce([
      {
        id: "live-report",
        name: "Live procurement report",
        description: "Live report description",
        category: "Procurement",
        icon: "analytics",
        filters: [],
      },
    ]);

    await wrapper.get('button[aria-label="Retry loading report catalog"]').trigger("click");
    await flushPromises();

    expect(reportService.getReportTypes).toHaveBeenCalledTimes(2);
    expect(wrapper.find('[role="alert"]').exists()).toBe(false);
    expect(wrapper.text()).toContain("Live procurement report");
    expect(wrapper.text()).not.toContain("Default procurement report");
  });
});
