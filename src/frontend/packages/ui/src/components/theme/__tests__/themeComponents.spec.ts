import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { h } from "vue";
import NieThemeAuthPanel from "../NieThemeAuthPanel.vue";
import NieThemeReportCard from "../NieThemeReportCard.vue";
import NieThemeShell from "../NieThemeShell.vue";
import NieThemeStatCard from "../NieThemeStatCard.vue";
import NieThemeWizardStepper from "../NieThemeWizardStepper.vue";
import type { ThemeWizardStep } from "../NieThemeWizardStepper.vue";
import type { LayoutVariant } from "../../../theme";

describe("NieThemeStatCard", () => {
  it("renders the label and value", () => {
    const wrapper = mount(NieThemeStatCard, {
      props: { label: "Open orders", value: "128" },
    });

    expect(wrapper.findAll("p").map((p) => p.text())).toEqual([
      "Open orders",
      "128",
    ]);
  });

  it("renders the optional delta and icon", () => {
    const wrapper = mount(NieThemeStatCard, {
      props: {
        label: "Open orders",
        value: "128",
        delta: "+12 this week",
        icon: "trending_up",
      },
    });

    expect(wrapper.text()).toContain("+12 this week");
    expect(wrapper.get(".material-symbols-outlined").text()).toBe(
      "trending_up",
    );
  });

  it("omits the icon badge when there is no icon", () => {
    const wrapper = mount(NieThemeStatCard, {
      props: { label: "Open orders", value: "128", tone: "danger" },
    });

    expect(wrapper.find(".material-symbols-outlined").exists()).toBe(false);
  });

  it("tints the icon badge per tone", () => {
    for (const [tone, expected] of [
      ["brand", "bg-primary-50"],
      ["info", "bg-info-50"],
      ["success", "bg-success-50"],
      ["warning", "bg-warning-50"],
      ["danger", "bg-danger-50"],
    ] as const) {
      const wrapper = mount(NieThemeStatCard, {
        props: { label: "x", value: "1", icon: "star", tone },
      });

      expect(wrapper.get(".h-12").classes()).toContain(expected);
    }
  });

  it("merges a caller-supplied class", () => {
    expect(
      mount(NieThemeStatCard, {
        props: { label: "x", value: "1", class: "col-span-2" },
      }).classes(),
    ).toContain("col-span-2");
  });
});

describe("NieThemeReportCard", () => {
  it("renders the title and summary", () => {
    const wrapper = mount(NieThemeReportCard, {
      props: { title: "Spend by vendor", summary: "Last 12 months." },
    });

    expect(wrapper.get("h3").text()).toBe("Spend by vendor");
    expect(wrapper.get("p").text()).toBe("Last 12 months.");
  });

  it("renders the tag only when supplied", () => {
    const bare = mount(NieThemeReportCard, {
      props: { title: "x", summary: "y" },
    });
    expect(bare.find("span").exists()).toBe(false);

    const tagged = mount(NieThemeReportCard, {
      props: { title: "x", summary: "y", tag: "Monthly" },
    });
    expect(tagged.get("span").text()).toBe("Monthly");
  });

  it("uses the default accent gradient unless overridden", () => {
    const standard = mount(NieThemeReportCard, {
      props: { title: "x", summary: "y" },
    });
    expect(standard.get(".h-2").classes()).toContain("from-primary-600");

    const custom = mount(NieThemeReportCard, {
      props: { title: "x", summary: "y", accentClass: "from-danger-600" },
    });
    expect(custom.get(".h-2").classes()).toContain("from-danger-600");
  });

  it("renders the default slot below the summary", () => {
    const wrapper = mount(NieThemeReportCard, {
      props: { title: "x", summary: "y" },
      slots: { default: () => h("a", { href: "#" }, "Open report") },
    });

    expect(wrapper.get("a").text()).toBe("Open report");
  });
});

describe("NieThemeAuthPanel", () => {
  it("renders the title and description", () => {
    const wrapper = mount(NieThemeAuthPanel, {
      props: { title: "Welcome back", description: "Sign in to continue." },
    });

    expect(wrapper.get("h2").text()).toBe("Welcome back");
    expect(wrapper.text()).toContain("Sign in to continue.");
  });

  it("renders the eyebrow only when supplied", () => {
    const bare = mount(NieThemeAuthPanel, {
      props: { title: "x", description: "y" },
    });
    expect(bare.findAll("p")).toHaveLength(1);

    const withEyebrow = mount(NieThemeAuthPanel, {
      props: { eyebrow: "NIE", title: "x", description: "y" },
    });
    expect(withEyebrow.findAll("p")[0].text()).toBe("NIE");
  });

  it("keeps the decorative background out of the accessibility tree", () => {
    const wrapper = mount(NieThemeAuthPanel, {
      props: { title: "x", description: "y" },
    });

    expect(wrapper.find(".pointer-events-none").exists()).toBe(true);
  });

  it("renders the default slot in the footer grid", () => {
    const wrapper = mount(NieThemeAuthPanel, {
      props: { title: "x", description: "y" },
      slots: { default: () => h("span", { class: "highlight" }, "SSO ready") },
    });

    expect(wrapper.get(".highlight").text()).toBe("SSO ready");
  });
});

describe("NieThemeWizardStepper", () => {
  const steps: ThemeWizardStep[] = [
    { id: "details", label: "Details", hint: "Who is requesting" },
    { id: "items", label: "Items" },
    { id: "review", label: "Review" },
  ];

  it("renders an ordered list with one entry per step", () => {
    const wrapper = mount(NieThemeWizardStepper, {
      props: { steps, activeStep: "items" },
    });

    expect(wrapper.element.tagName).toBe("OL");
    expect(wrapper.findAll("li")).toHaveLength(3);
    expect(wrapper.attributes("style")).toContain("--step-count: 3");
  });

  it("numbers the steps from one", () => {
    const wrapper = mount(NieThemeWizardStepper, {
      props: { steps, activeStep: "details" },
    });

    expect(wrapper.findAll("li .h-10").map((node) => node.text())).toEqual([
      "1",
      "2",
      "3",
    ]);
  });

  it("distinguishes completed, active and upcoming steps", () => {
    const wrapper = mount(NieThemeWizardStepper, {
      props: { steps, activeStep: "items" },
    });
    const markers = wrapper.findAll("li .h-10");

    expect(markers[0].classes()).toContain("bg-success-100");
    expect(markers[1].classes()).toContain("bg-primary-600");
    expect(markers[2].classes()).toContain("bg-secondary-100");
    expect(wrapper.findAll("li")[1].classes()).toContain("ring-2");
  });

  it("renders the hint only for steps that have one", () => {
    const wrapper = mount(NieThemeWizardStepper, {
      props: { steps, activeStep: "details" },
    });

    expect(wrapper.findAll("li")[0].findAll("p")).toHaveLength(2);
    expect(wrapper.findAll("li")[1].findAll("p")).toHaveLength(1);
  });
});

describe("NieThemeShell", () => {
  it("defaults to a stacked bare-content layout", () => {
    const wrapper = mount(NieThemeShell);

    expect(wrapper.attributes("data-layout-variant")).toBe("bare-content");
    expect(wrapper.classes()).toContain("flex-col");
    expect(wrapper.find("aside").exists()).toBe(false);
    expect(wrapper.find("header").exists()).toBe(false);
    expect(wrapper.find("main").exists()).toBe(true);
  });

  it("renders a sidebar only for the layouts that have one", () => {
    const sidebarLayouts: LayoutVariant[] = ["sidebar-admin", "portal-shell"];
    for (const variant of sidebarLayouts) {
      const wrapper = mount(NieThemeShell, {
        props: { variant },
        slots: { sidebar: () => h("nav", "Menu") },
      });

      expect(wrapper.get("aside").text()).toBe("Menu");
      expect(wrapper.classes()).toContain("flex");
    }

    const stacked = mount(NieThemeShell, {
      props: { variant: "topbar-admin" },
      slots: { sidebar: () => h("nav", "Menu") },
    });
    expect(stacked.find("aside").exists()).toBe(false);
  });

  it("renders the hero only for the split auth layout", () => {
    const split = mount(NieThemeShell, {
      props: { variant: "split-auth" },
      slots: { hero: () => h("img", { alt: "" }) },
    });
    expect(split.find(".theme-shell__hero").exists()).toBe(true);
    expect(split.classes()).toContain("grid");

    const other = mount(NieThemeShell, {
      props: { variant: "wizard-shell" },
      slots: { hero: () => h("img", { alt: "" }) },
    });
    expect(other.find(".theme-shell__hero").exists()).toBe(false);
  });

  it("renders the topbar when the slot is filled", () => {
    const wrapper = mount(NieThemeShell, {
      slots: { topbar: () => h("div", "Header") },
    });

    expect(wrapper.get("header").text()).toBe("Header");
  });

  it("merges caller-supplied shell and content classes", () => {
    const wrapper = mount(NieThemeShell, {
      props: { class: "min-h-screen", contentClass: "bg-white" },
    });

    expect(wrapper.classes()).toContain("min-h-screen");
    expect(wrapper.get(".theme-shell__content").classes()).toContain("bg-white");
  });
});
