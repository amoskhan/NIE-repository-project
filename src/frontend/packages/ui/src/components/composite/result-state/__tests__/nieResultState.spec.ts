import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import { h } from "vue";
import NieResultState from "../../result-state/NieResultState.vue";
import type {
  NieResultStatus,
  NieResultVariant,
} from "../../result-state/NieResultState.vue";

const statuses: NieResultStatus[] = [401, 403, 404, 408, 429, 500, 502, 503];
const variants: NieResultVariant[] = [
  "empty",
  "info",
  "success",
  "warning",
  "error",
  "loading",
];

describe("NieResultState variants", () => {
  it("defaults to the empty state", () => {
    const wrapper = mount(NieResultState);

    expect(wrapper.get("h1").text()).toBe("No records found");
    expect(wrapper.attributes("aria-label")).toBe("No records found");
    expect(wrapper.attributes("data-result-status")).toBeUndefined();
  });

  it("gives every variant its own copy and eyebrow", () => {
    const titles = variants.map((variant) => {
      const wrapper = mount(NieResultState, { props: { variant } });
      expect(wrapper.get("[data-result-content] p").text()).toBe(variant);
      return wrapper.get("h1").text();
    });

    expect(new Set(titles).size).toBe(variants.length);
  });

  it("shows no large status number for variant-only states", () => {
    const wrapper = mount(NieResultState, { props: { variant: "error" } });

    expect(wrapper.findAll("[data-result-content] p")).toHaveLength(2);
  });
});

describe("NieResultState HTTP statuses", () => {
  it("gives every status its own copy", () => {
    const titles = statuses.map(
      (statusCode) =>
        mount(NieResultState, { props: { statusCode } }).get("h1").text(),
    );

    expect(new Set(titles).size).toBe(statuses.length);
  });

  it("prints the status code as decorative artwork with an HTTP eyebrow", () => {
    const wrapper = mount(NieResultState, { props: { statusCode: 404 } });
    const paragraphs = wrapper.findAll("[data-result-content] p");

    expect(wrapper.attributes("data-result-status")).toBe("404");
    expect(paragraphs[0].text()).toBe("HTTP 404");
    expect(paragraphs[1].text()).toBe("404");
    expect(paragraphs[1].attributes("aria-hidden")).toBe("true");
    expect(wrapper.get("h1").text()).toBe("Page not found");
  });

  it("lets the status code win over the variant", () => {
    const wrapper = mount(NieResultState, {
      props: { statusCode: 503, variant: "empty" },
    });

    expect(wrapper.get("h1").text()).toBe("Service unavailable");
  });
});

describe("NieResultState copy overrides", () => {
  it("uses caller-supplied title and description", () => {
    const wrapper = mount(NieResultState, {
      props: {
        statusCode: 404,
        title: "No such purchase order",
        description: "Check the reference and try again.",
      },
    });

    expect(wrapper.get("h1").text()).toBe("No such purchase order");
    expect(wrapper.attributes("aria-label")).toBe("No such purchase order");
    expect(wrapper.text()).toContain("Check the reference and try again.");
  });

  it("falls back to the built-in copy for empty overrides", () => {
    const wrapper = mount(NieResultState, {
      props: { statusCode: 404, title: "", description: "" },
    });

    expect(wrapper.get("h1").text()).toBe("Page not found");
  });
});

describe("NieResultState loading", () => {
  it("announces itself politely and shows the brand loader", () => {
    const wrapper = mount(NieResultState, { props: { variant: "loading" } });

    expect(wrapper.attributes("aria-live")).toBe("polite");
    expect(
      wrapper
        .get('[data-testid="nie-loader-symbol"]')
        .attributes("data-loader-variant"),
    ).toBe("brand");
  });

  it("is not a live region for any other variant", () => {
    for (const variant of variants.filter((entry) => entry !== "loading")) {
      expect(
        mount(NieResultState, { props: { variant } }).attributes("aria-live"),
      ).toBeUndefined();
    }
  });

  it("prefers the status icon over the loader when a status is given", () => {
    const wrapper = mount(NieResultState, {
      props: { variant: "loading", statusCode: 503 },
    });

    expect(wrapper.find('[data-testid="nie-loader-symbol"]').exists()).toBe(
      false,
    );
  });
});

describe("NieResultState layout", () => {
  it("hides the whole illustration from assistive technology", () => {
    const wrapper = mount(NieResultState, { props: { statusCode: 500 } });

    expect(wrapper.get("[data-result-visual]").attributes("aria-hidden")).toBe(
      "true",
    );
    expect(wrapper.findAll("[data-result-orbit]")).toHaveLength(2);
  });

  it("switches to the compact layout when asked", () => {
    expect(mount(NieResultState).classes()).toContain("min-h-[30rem]");
    expect(
      mount(NieResultState, { props: { compact: true } }).classes(),
    ).toContain("min-h-[20rem]");
  });

  it("merges a caller-supplied class", () => {
    expect(
      mount(NieResultState, { props: { class: "my-12" } }).classes(),
    ).toContain("my-12");
  });

  it("renders no actions region unless the slot is filled", () => {
    expect(mount(NieResultState).find("button").exists()).toBe(false);
  });

  it("passes the status and variant into the actions slot", () => {
    const wrapper = mount(NieResultState, {
      props: { statusCode: 403 },
      slots: {
        actions: (slotProps: {
          statusCode?: NieResultStatus;
          variant: NieResultVariant;
        }) =>
          h(
            "button",
            { type: "button" },
            `${slotProps.statusCode}/${slotProps.variant}`,
          ),
      },
    });

    expect(wrapper.get("button").text()).toBe("403/empty");
  });
});
