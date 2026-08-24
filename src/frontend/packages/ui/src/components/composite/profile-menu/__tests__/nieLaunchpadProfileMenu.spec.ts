import { mount } from "@vue/test-utils";
import { afterEach, describe, expect, it } from "vitest";
import { nextTick } from "vue";
import NieLaunchpadProfileMenu from "../../profile-menu/NieLaunchpadProfileMenu.vue";

const palettes = [
  { id: "cobalt", name: "Cobalt", swatch: "#1500f8" },
  { id: "violet", name: "Violet", swatch: "#7c3aed" },
];

type MenuProps = InstanceType<typeof NieLaunchpadProfileMenu>["$props"];

function setViewportWidth(width: number): void {
  Object.defineProperty(window, "innerWidth", {
    configurable: true,
    writable: true,
    value: width,
  });
}

function mountMenu(props: Partial<MenuProps> = {}) {
  return mount(NieLaunchpadProfileMenu, {
    attachTo: document.body,
    props: {
      userName: "Ada Lovelace",
      mode: "light",
      palette: "cobalt",
      palettes,
      ...props,
    } as MenuProps,
  });
}

async function openMenu(wrapper: ReturnType<typeof mountMenu>) {
  await wrapper.get('[aria-label="Open profile menu"]').trigger("click");
  await nextTick();
}

afterEach(() => {
  setViewportWidth(1024);
  document.body.innerHTML = "";
});

describe("NieLaunchpadProfileMenu trigger", () => {
  it("shows the user name and role next to the avatar", () => {
    const wrapper = mountMenu({ userRole: "Administrator" });

    expect(wrapper.get('[aria-label="Open profile menu"]').text()).toContain(
      "Ada Lovelace",
    );
    expect(wrapper.text()).toContain("Administrator");
    wrapper.unmount();
  });

  it("omits the role line when there is no role", () => {
    const wrapper = mountMenu();

    expect(wrapper.findAll("span")).toHaveLength(1);
    wrapper.unmount();
  });

  it("derives at most two initials from the name", () => {
    for (const [userName, expected] of [
      ["Ada Lovelace", "AL"],
      ["  ada   byron   lovelace ", "AB"],
      ["Ada", "A"],
      ["", "U"],
      ["   ", "U"],
    ]) {
      const wrapper = mountMenu({ userName });

      expect(wrapper.get(".nie-launchpad-avatar").text()).toBe(expected);
      wrapper.unmount();
    }
  });

  it("reports its expanded state", async () => {
    const wrapper = mountMenu();
    const trigger = wrapper.get('[aria-label="Open profile menu"]');

    expect(trigger.attributes("aria-expanded")).toBe("false");
    await openMenu(wrapper);
    expect(trigger.attributes("aria-expanded")).toBe("true");
    wrapper.unmount();
  });
});

describe("NieLaunchpadProfileMenu open and close", () => {
  it("announces every open-state change once", async () => {
    const wrapper = mountMenu();

    await openMenu(wrapper);
    await wrapper.get('[aria-label="Open profile menu"]').trigger("click");
    await nextTick();

    expect(wrapper.emitted("open-change")).toEqual([[true], [false]]);
    wrapper.unmount();
  });

  it("closes when the user clicks elsewhere on the page", async () => {
    const wrapper = mountMenu();
    await openMenu(wrapper);

    document.body.dispatchEvent(new MouseEvent("click", { bubbles: true }));
    await nextTick();

    expect(wrapper.find(".nie-launchpad-popover").exists()).toBe(false);
    wrapper.unmount();
  });

  it("stays open for clicks inside the menu", async () => {
    const wrapper = mountMenu();
    await openMenu(wrapper);

    wrapper.get(".nie-profile-sheet").element.dispatchEvent(
      new MouseEvent("click", { bubbles: true }),
    );
    await nextTick();

    expect(wrapper.find(".nie-launchpad-popover").exists()).toBe(true);
    wrapper.unmount();
  });

  it("closes when the host bumps the close signal", async () => {
    const wrapper = mountMenu();
    await openMenu(wrapper);

    await wrapper.setProps({ closeSignal: 1 });
    await nextTick();

    expect(wrapper.find(".nie-launchpad-popover").exists()).toBe(false);
    expect(wrapper.emitted("open-change")).toEqual([[true], [false]]);
    wrapper.unmount();
  });

  it("does not re-announce a close signal while already closed", async () => {
    const wrapper = mountMenu();

    await wrapper.setProps({ closeSignal: 1 });
    await nextTick();

    expect(wrapper.emitted("open-change")).toBeUndefined();
    wrapper.unmount();
  });

  it("stops listening after unmount", async () => {
    const wrapper = mountMenu();
    await openMenu(wrapper);
    wrapper.unmount();

    expect(() => {
      document.body.dispatchEvent(new MouseEvent("click", { bubbles: true }));
      window.dispatchEvent(new Event("resize"));
    }).not.toThrow();
  });
});

describe("NieLaunchpadProfileMenu identity panel", () => {
  it("shows the name, email and role", async () => {
    const wrapper = mountMenu({
      userEmail: "ada@nie.edu.sg",
      userRole: "Administrator",
    });
    await openMenu(wrapper);

    expect(wrapper.get(".nie-profile-card__name").text()).toBe("Ada Lovelace");
    expect(wrapper.get(".nie-profile-card__email").text()).toBe(
      "ada@nie.edu.sg",
    );
    expect(wrapper.get(".nie-profile-card__role").text()).toBe("Administrator");
    wrapper.unmount();
  });

  it("omits the email and role lines when they are unknown", async () => {
    const wrapper = mountMenu();
    await openMenu(wrapper);

    expect(wrapper.find(".nie-profile-card__email").exists()).toBe(false);
    expect(wrapper.find(".nie-profile-card__role").exists()).toBe(false);
    wrapper.unmount();
  });

  it("formats the last login in the Singapore locale", async () => {
    const wrapper = mountMenu({ userLastLogin: "2026-08-07T02:30:00.000Z" });
    await openMenu(wrapper);

    const formatted = new Intl.DateTimeFormat("en-SG", {
      month: "short",
      day: "numeric",
      year: "numeric",
      hour: "numeric",
      minute: "2-digit",
    }).format(new Date("2026-08-07T02:30:00.000Z"));
    expect(wrapper.get(".nie-profile-meta__value").text()).toBe(formatted);
    wrapper.unmount();
  });

  it("says the last login is unavailable when it is missing or unparseable", async () => {
    for (const userLastLogin of [null, "", "not-a-date"]) {
      const wrapper = mountMenu({ userLastLogin });
      await openMenu(wrapper);

      expect(wrapper.get(".nie-profile-meta__value").text()).toBe(
        "Unavailable",
      );
      wrapper.unmount();
    }
  });
});

describe("NieLaunchpadProfileMenu notification preferences", () => {
  it("uses the default summary and hides an empty status", async () => {
    const wrapper = mountMenu();
    await openMenu(wrapper);

    expect(wrapper.get(".nie-topbar-choice__hint").text()).toBe(
      "Manage subscriptions",
    );
    expect(wrapper.find(".nie-topbar-choice__meta").exists()).toBe(false);
    wrapper.unmount();
  });

  it("marks an enabled status differently from any other status", async () => {
    const enabled = mountMenu({ notificationPreferencesStatus: "Enabled" });
    await openMenu(enabled);
    expect(enabled.get(".nie-topbar-choice__meta").classes()).toContain(
      "nie-topbar-choice__meta--active",
    );
    enabled.unmount();

    const blocked = mountMenu({ notificationPreferencesStatus: "Blocked" });
    await openMenu(blocked);
    expect(blocked.get(".nie-topbar-choice__meta").classes()).toContain(
      "nie-topbar-choice__meta--inactive",
    );
    blocked.unmount();
  });

  it("closes the menu and asks the host to open the preferences", async () => {
    const wrapper = mountMenu();
    await openMenu(wrapper);

    await wrapper.get(".nie-topbar-choice--stacked").trigger("click");

    expect(wrapper.emitted("open-notification-preferences")).toHaveLength(1);
    expect(wrapper.emitted("open-change")).toEqual([[true], [false]]);
    wrapper.unmount();
  });
});

describe("NieLaunchpadProfileMenu theme controls", () => {
  it("highlights the active mode", async () => {
    const wrapper = mountMenu({ mode: "dark" });
    await openMenu(wrapper);

    const modes = wrapper.findAll(".nie-topbar-choice--compact");
    expect(modes.map((button) => button.text())).toEqual([
      "Light",
      "Dark",
      "System",
    ]);
    expect(modes[1].classes()).toContain("nie-topbar-choice--active");
    wrapper.unmount();
  });

  it("emits the chosen mode", async () => {
    const wrapper = mountMenu();
    await openMenu(wrapper);

    await wrapper.findAll(".nie-topbar-choice--compact")[2].trigger("click");

    expect(wrapper.emitted("set-mode")).toEqual([["system"]]);
    wrapper.unmount();
  });

  it("renders one labelled swatch per palette and marks the active one", async () => {
    const wrapper = mountMenu({ palette: "violet" });
    await openMenu(wrapper);

    const dots = wrapper.findAll(".nie-topbar-palette-dot");
    expect(dots.map((dot) => dot.attributes("aria-label"))).toEqual([
      "Cobalt",
      "Violet",
    ]);
    expect(dots[1].classes()).toContain("nie-topbar-palette-dot--active");
    expect(
      dots[0].get(".nie-topbar-palette-dot__swatch").attributes("style"),
    ).toContain("rgb(21, 0, 248)");
    wrapper.unmount();
  });

  it("emits the chosen palette", async () => {
    const wrapper = mountMenu();
    await openMenu(wrapper);

    await wrapper.findAll(".nie-topbar-palette-dot")[1].trigger("click");

    expect(wrapper.emitted("set-palette")).toEqual([["violet"]]);
    wrapper.unmount();
  });

  it("hides the palette row when there is nothing to choose", async () => {
    const wrapper = mountMenu({ palettes: [] });
    await openMenu(wrapper);

    expect(wrapper.find(".nie-topbar-palette-row").exists()).toBe(false);
    wrapper.unmount();
  });
});

describe("NieLaunchpadProfileMenu sign out", () => {
  it("closes the menu and emits logout", async () => {
    const wrapper = mountMenu();
    await openMenu(wrapper);

    await wrapper.get(".nie-topbar-choice--danger").trigger("click");

    expect(wrapper.emitted("logout")).toHaveLength(1);
    expect(wrapper.find(".nie-launchpad-popover").exists()).toBe(false);
    wrapper.unmount();
  });
});

describe("NieLaunchpadProfileMenu mobile sheet", () => {
  it("replaces the popover with a bottom sheet on narrow viewports", async () => {
    setViewportWidth(400);
    const wrapper = mountMenu();
    await openMenu(wrapper);

    expect(wrapper.find(".nie-launchpad-popover").exists()).toBe(false);
    expect(document.querySelector(".nie-mobile-sheet")).not.toBeNull();
    expect(
      document.querySelector(".nie-mobile-sheet-title")?.textContent,
    ).toBe("Profile");
    wrapper.unmount();
  });

  it("closes from the backdrop and from the close button", async () => {
    setViewportWidth(400);
    const wrapper = mountMenu();
    await openMenu(wrapper);

    document
      .querySelector<HTMLButtonElement>(".nie-mobile-sheet-backdrop")
      ?.click();
    await nextTick();
    expect(document.querySelector(".nie-mobile-sheet")).toBeNull();

    await openMenu(wrapper);
    document.querySelector<HTMLButtonElement>(".nie-mobile-sheet-close")?.click();
    await nextTick();
    expect(document.querySelector(".nie-mobile-sheet")).toBeNull();
    wrapper.unmount();
  });

  it("offers the same theme and sign-out controls", async () => {
    setViewportWidth(400);
    const wrapper = mountMenu({ mode: "system" });
    await openMenu(wrapper);

    const sheet = document.querySelector(".nie-mobile-sheet");
    const modes = [
      ...(sheet?.querySelectorAll<HTMLButtonElement>(
        ".nie-topbar-choice--compact",
      ) ?? []),
    ];
    expect(modes).toHaveLength(3);
    expect(modes[2].className).toContain("nie-topbar-choice--active");

    modes[0].click();
    sheet
      ?.querySelector<HTMLButtonElement>(".nie-topbar-palette-dot")
      ?.click();
    sheet?.querySelector<HTMLButtonElement>(".nie-topbar-choice--danger")?.click();
    await nextTick();

    expect(wrapper.emitted("set-mode")).toEqual([["light"]]);
    expect(wrapper.emitted("set-palette")).toEqual([["cobalt"]]);
    expect(wrapper.emitted("logout")).toHaveLength(1);
    wrapper.unmount();
  });

  it("swaps back to the popover when the viewport widens", async () => {
    setViewportWidth(400);
    const wrapper = mountMenu();
    await openMenu(wrapper);
    expect(document.querySelector(".nie-mobile-sheet")).not.toBeNull();

    setViewportWidth(1200);
    window.dispatchEvent(new Event("resize"));
    await nextTick();

    expect(document.querySelector(".nie-mobile-sheet")).toBeNull();
    expect(wrapper.find(".nie-launchpad-popover").exists()).toBe(true);
    wrapper.unmount();
  });
});
