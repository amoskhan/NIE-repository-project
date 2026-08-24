import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { NieDataTable, NieResultState } from "@nie/ui";

describe("application status experience", () => {
  it.each([401, 403, 404, 408, 429, 500, 502, 503])(
    "renders HTTP %s directly on the page canvas",
    (statusCode) => {
      const wrapper = mount(NieResultState, {
        props: { statusCode },
      });

      const state = wrapper.get('[data-testid="nie-result-state"]');
      expect(state.text()).toContain(String(statusCode));
      expect(state.attributes("data-result-layout")).toBe("split");
      expect(state.findAll("[data-result-orbit]")).toHaveLength(2);
      expect(state.find("[data-result-content]").exists()).toBe(true);
      expect(state.find("[data-result-visual]").exists()).toBe(true);
      expect(state.classes()).not.toContain("border");
      expect(state.classes()).not.toContain("bg-white");
      expect(state.classes()).not.toContain("shadow-sm");
    },
  );

  it.each(["empty", "info", "success", "warning", "error", "loading"] as const)(
    "renders the shared %s application state",
    (variant) => {
      const wrapper = mount(NieResultState, { props: { variant } });

      expect(wrapper.get('[data-testid="nie-result-state"]').text()).not.toBe("");
    },
  );

  it("maps a table 404 to a safe contained result instead of exposing Axios text", () => {
    const wrapper = mount(NieDataTable, {
      props: {
        columns: [{ key: "name", label: "Name" }],
        data: [],
        rowKey: "id",
        error: "Request failed with status code 404",
        hideCreate: true,
        hideActions: true,
      },
    });

    const state = wrapper.get('[data-testid="nie-result-state"]');
    expect(state.attributes("data-result-status")).toBe("404");
    expect(state.text()).toContain("Page not found");
    expect(state.text()).not.toContain("Request failed with status code 404");
    expect(wrapper.get("button").text()).toContain("Retry");
  });

  it("keeps the illustrated empty result inside the visible table viewport", () => {
    const wrapper = mount(NieDataTable, {
      props: {
        columns: [
          { key: "name", label: "Name" },
          { key: "status", label: "Status" },
        ],
        data: [],
        rowKey: "id",
        hideCreate: true,
        hideActions: true,
      },
    });

    expect(wrapper.find("thead").exists()).toBe(true);

    const frame = wrapper.get("[data-table-result-frame]");
    expect(frame.classes()).toEqual(
      expect.arrayContaining(["sticky", "left-0", "w-full", "min-w-0"]),
    );

    const state = frame.get('[data-testid="nie-result-state"]');
    expect(state.attributes("data-result-layout")).toBe("split");
    expect(state.text()).toContain("No records found");
    expect(state.find("[data-result-visual]").exists()).toBe(true);
    expect(state.find("[data-result-visual] svg").exists()).toBe(true);
  });

  it("routes permission denial to 403 and unknown main-app paths to 404", () => {
    const source = readFileSync(
      resolve(process.cwd(), "src/router/index.ts"),
      "utf8",
    );

    expect(source).toContain('name: "forbidden"');
    expect(source).toContain('name: "not-found"');
    expect(source).toMatch(/return\s*\{[\s\S]*?name:\s*"forbidden"/);
    expect(source).not.toMatch(/pathMatch\(\.\*\)\*.*redirect:\s*["']\/["']/s);
  });

  it("gives the Auth application a deliberate 404 route and 500 boundary", () => {
    const routerSource = readFileSync(
      resolve(process.cwd(), "../auth/src/router/index.ts"),
      "utf8",
    );
    const appSource = readFileSync(
      resolve(process.cwd(), "../auth/src/App.vue"),
      "utf8",
    );

    expect(routerSource).toContain('name: "not-found"');
    expect(routerSource).toContain(":pathMatch(.*)*");
    expect(appSource).toContain("onErrorCaptured");
    expect(appSource).toContain(':status-code="500"');
  });

  it("keeps a 500 boundary around the authenticated Main application", () => {
    const appSource = readFileSync(resolve(process.cwd(), "src/App.vue"), "utf8");

    expect(appSource).toContain("onErrorCaptured");
    expect(appSource).toContain(':status-code="500"');
  });
});
