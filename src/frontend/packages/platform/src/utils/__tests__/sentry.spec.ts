import * as Sentry from "@sentry/vue";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { App } from "vue";
import type { Router } from "vue-router";
import { initSentry } from "../sentry";

const otel = vi.hoisted(() => ({
  registerInstrumentations: vi.fn(),
  register: vi.fn(),
  providerOptions: [] as Record<string, unknown>[],
  exporterOptions: [] as Record<string, unknown>[],
  fetchOptions: [] as Record<string, unknown>[],
  sampleRatios: [] as number[],
}));

vi.mock("@opentelemetry/instrumentation", () => ({
  registerInstrumentations: otel.registerInstrumentations,
}));

vi.mock("@opentelemetry/resources", () => ({
  resourceFromAttributes: (attributes: Record<string, unknown>) => attributes,
}));

vi.mock("@opentelemetry/exporter-trace-otlp-http", () => ({
  OTLPTraceExporter: class {
    constructor(options: Record<string, unknown>) {
      otel.exporterOptions.push(options);
    }
  },
}));

vi.mock("@opentelemetry/instrumentation-document-load", () => ({
  DocumentLoadInstrumentation: class {},
}));

vi.mock("@opentelemetry/instrumentation-fetch", () => ({
  FetchInstrumentation: class {
    constructor(options: Record<string, unknown>) {
      otel.fetchOptions.push(options);
    }
  },
}));

vi.mock("@opentelemetry/instrumentation-xml-http-request", () => ({
  XMLHttpRequestInstrumentation: class {},
}));

vi.mock("@opentelemetry/sdk-trace-web", () => ({
  BatchSpanProcessor: class {},
  ParentBasedSampler: class {},
  TraceIdRatioBasedSampler: class {
    constructor(ratio: number) {
      otel.sampleRatios.push(ratio);
    }
  },
  WebTracerProvider: class {
    register = otel.register;
    constructor(options: Record<string, unknown>) {
      otel.providerOptions.push(options);
    }
  },
}));

vi.mock("@sentry/vue", () => ({
  init: vi.fn(),
  browserTracingIntegration: vi.fn(() => ({ name: "BrowserTracing" })),
  replayIntegration: vi.fn(() => ({ name: "Replay" })),
}));

interface SentryInitOptions {
  dsn: string;
  environment: string;
  enableLogs: boolean;
  sendDefaultPii: boolean;
  tracesSampleRate: number;
  tracePropagationTargets: (string | RegExp)[];
  initialScope: { tags: Record<string, string> };
  integrations: unknown[];
  beforeSend: (event: {
    request?: { cookies?: Record<string, string>; url?: string };
  }) => unknown;
}

const app = { use: vi.fn() } as unknown as App;
const router = { beforeEach: vi.fn() } as unknown as Router;
const disabledOtel = { enabled: false, serviceName: "application-web" };

/**
 * The module guards OpenTelemetry with a module-level "already initialised"
 * flag, so each OpenTelemetry assertion needs a freshly evaluated module.
 * The mocked dependency has to be imported first: importing both at once
 * lets the module under test win the race and resolve the real package.
 */
async function loadFreshInitSentry(): Promise<typeof initSentry> {
  vi.resetModules();
  await import("@sentry/vue");
  const module = await import("../sentry");
  return module.initSentry;
}

function lastInitOptions(): SentryInitOptions {
  const calls = vi.mocked(Sentry.init).mock.calls;
  return calls[calls.length - 1]?.[0] as unknown as SentryInitOptions;
}

beforeEach(() => {
  otel.registerInstrumentations.mockClear();
  otel.register.mockClear();
  otel.providerOptions.length = 0;
  otel.exporterOptions.length = 0;
  otel.fetchOptions.length = 0;
  otel.sampleRatios.length = 0;
  vi.mocked(Sentry.init).mockClear();
  vi.mocked(Sentry.browserTracingIntegration).mockClear();
  vi.mocked(Sentry.replayIntegration).mockClear();
});

describe("initSentry", () => {
  it("skips Sentry entirely when no DSN is configured", () => {
    initSentry({ app, dsn: "", openTelemetry: disabledOtel });

    expect(Sentry.init).not.toHaveBeenCalled();
  });

  it("initialises Sentry with privacy-preserving defaults", () => {
    initSentry({
      app,
      dsn: "https://key@sentry.nie.edu.sg/1",
      openTelemetry: disabledOtel,
    });

    const options = lastInitOptions();
    expect(options.dsn).toBe("https://key@sentry.nie.edu.sg/1");
    expect(options.sendDefaultPii).toBe(false);
    expect(options.environment).toBe("development");
    expect(options.enableLogs).toBe(true);
    expect(options.tracesSampleRate).toBe(0.2);
    expect(options.initialScope.tags).toEqual({ service: "application-web" });
  });

  it("merges caller tags on top of the service tag", () => {
    initSentry({
      app,
      dsn: "https://key@sentry.nie.edu.sg/1",
      environment: "prd",
      enableLogs: false,
      tags: { release: "2026.08.07", service: "custom-web" },
      openTelemetry: disabledOtel,
    });

    const options = lastInitOptions();
    expect(options.environment).toBe("prd");
    expect(options.enableLogs).toBe(false);
    expect(options.initialScope.tags).toEqual({
      service: "custom-web",
      release: "2026.08.07",
    });
  });

  it("registers the router with browser tracing when one is supplied", () => {
    initSentry({
      app,
      dsn: "https://key@sentry.nie.edu.sg/1",
      router,
      openTelemetry: disabledOtel,
    });

    expect(Sentry.browserTracingIntegration).toHaveBeenCalledWith({ router });
    expect(Sentry.replayIntegration).not.toHaveBeenCalled();
    expect(lastInitOptions().integrations).toHaveLength(1);
  });

  it("omits the router argument when routing is not instrumented", () => {
    initSentry({
      app,
      dsn: "https://key@sentry.nie.edu.sg/1",
      openTelemetry: disabledOtel,
    });

    expect(Sentry.browserTracingIntegration).toHaveBeenCalledWith();
  });

  it("adds session replay when an on-error sample rate is requested", () => {
    initSentry({
      app,
      dsn: "https://key@sentry.nie.edu.sg/1",
      replaysOnErrorSampleRate: 0.1,
      openTelemetry: disabledOtel,
    });

    expect(Sentry.replayIntegration).toHaveBeenCalledWith({
      blockAllMedia: true,
      maskAllText: true,
    });
    expect(lastInitOptions().integrations).toHaveLength(2);
  });

  it("adds session replay when a session sample rate is requested", () => {
    initSentry({
      app,
      dsn: "https://key@sentry.nie.edu.sg/1",
      replaysSessionSampleRate: 0.05,
      openTelemetry: disabledOtel,
    });

    expect(Sentry.replayIntegration).toHaveBeenCalledTimes(1);
  });

  it("strips cookies from outgoing events", () => {
    initSentry({
      app,
      dsn: "https://key@sentry.nie.edu.sg/1",
      openTelemetry: disabledOtel,
    });

    const { beforeSend } = lastInitOptions();
    const withCookies = {
      request: { url: "/orders", cookies: { session: "secret" } },
    };
    const withoutCookies = { request: { url: "/orders" } };

    expect(beforeSend(withCookies)).toBe(withCookies);
    expect(withCookies.request).not.toHaveProperty("cookies");
    expect(withCookies.request.url).toBe("/orders");
    expect(beforeSend(withoutCookies)).toBe(withoutCookies);
    expect(beforeSend({})).toEqual({});
  });
});

describe("initSentry trace propagation targets", () => {
  it("defaults to same-origin and loopback targets", () => {
    initSentry({
      app,
      dsn: "https://key@sentry.nie.edu.sg/1",
      openTelemetry: disabledOtel,
    });

    const targets = lastInitOptions().tracePropagationTargets;
    const matches = (candidate: string) =>
      targets.some(
        (target) => target instanceof RegExp && target.test(candidate),
      );

    expect(targets).toHaveLength(4);
    expect(matches("/api-main/api/users")).toBe(true);
    expect(matches("http://localhost:8002/api")).toBe(true);
    expect(matches("http://127.0.0.1:8002/api")).toBe(true);
    expect(matches("https://third-party.example.com/api")).toBe(false);
  });

  it("prefers the explicit targets over the OpenTelemetry ones", () => {
    initSentry({
      app,
      dsn: "https://key@sentry.nie.edu.sg/1",
      tracePropagationTargets: ["https://explicit.nie.edu.sg"],
      openTelemetry: {
        ...disabledOtel,
        tracePropagationTargets: ["https://otel.nie.edu.sg"],
      },
    });

    expect(lastInitOptions().tracePropagationTargets).toEqual([
      "https://explicit.nie.edu.sg",
    ]);
  });

  it("falls back to the OpenTelemetry targets", () => {
    initSentry({
      app,
      dsn: "https://key@sentry.nie.edu.sg/1",
      openTelemetry: {
        ...disabledOtel,
        tracePropagationTargets: ["https://otel.nie.edu.sg"],
      },
    });

    expect(lastInitOptions().tracePropagationTargets).toEqual([
      "https://otel.nie.edu.sg",
    ]);
  });
});

describe("initSentry browser OpenTelemetry", () => {
  it("does nothing when OpenTelemetry is disabled", async () => {
    const freshInitSentry = await loadFreshInitSentry();

    freshInitSentry({ app, dsn: "", openTelemetry: disabledOtel });

    expect(otel.registerInstrumentations).not.toHaveBeenCalled();
    expect(otel.providerOptions).toHaveLength(0);
  });

  it("registers a tracer provider and the browser instrumentations", async () => {
    const freshInitSentry = await loadFreshInitSentry();

    freshInitSentry({
      app,
      dsn: "",
      environment: "stg",
      openTelemetry: { serviceName: "application-web" },
    });

    expect(otel.providerOptions).toHaveLength(1);
    expect(otel.providerOptions[0].resource).toEqual({
      "deployment.environment": "stg",
      "service.name": "application-web",
      "service.namespace": "application.frontend",
    });
    expect(otel.register).toHaveBeenCalledTimes(1);
    expect(otel.registerInstrumentations).toHaveBeenCalledTimes(1);
  });

  it("lets the OpenTelemetry environment and namespace be overridden", async () => {
    const freshInitSentry = await loadFreshInitSentry();

    freshInitSentry({
      app,
      dsn: "",
      environment: "stg",
      openTelemetry: {
        serviceName: "application-web",
        serviceNamespace: "procurement.frontend",
        environment: "prd",
      },
    });

    expect(otel.providerOptions[0].resource).toEqual({
      "deployment.environment": "prd",
      "service.name": "application-web",
      "service.namespace": "procurement.frontend",
    });
  });

  it("creates no exporter when no endpoint is configured", async () => {
    const freshInitSentry = await loadFreshInitSentry();

    freshInitSentry({
      app,
      dsn: "",
      openTelemetry: { serviceName: "application-web" },
    });

    expect(otel.exporterOptions).toHaveLength(0);
    expect(otel.providerOptions[0].spanProcessors).toHaveLength(0);
  });

  it("batches spans to the configured exporter endpoint", async () => {
    const freshInitSentry = await loadFreshInitSentry();

    freshInitSentry({
      app,
      dsn: "",
      openTelemetry: {
        serviceName: "application-web",
        exporterEndpoint: "https://otel.nie.edu.sg/v1/traces",
      },
    });

    expect(otel.exporterOptions).toEqual([
      { url: "https://otel.nie.edu.sg/v1/traces" },
    ]);
    expect(otel.providerOptions[0].spanProcessors).toHaveLength(1);
  });

  it("initialises the tracer provider at most once per page load", async () => {
    const freshInitSentry = await loadFreshInitSentry();

    freshInitSentry({
      app,
      dsn: "",
      openTelemetry: { serviceName: "application-web" },
    });
    freshInitSentry({
      app,
      dsn: "",
      openTelemetry: { serviceName: "application-web" },
    });

    expect(otel.registerInstrumentations).toHaveBeenCalledTimes(1);
    expect(otel.providerOptions).toHaveLength(1);
  });

  it("clamps the sample rate into the 0..1 range", async () => {
    const above = await loadFreshInitSentry();
    above({
      app,
      dsn: "",
      openTelemetry: { serviceName: "application-web", tracesSampleRate: 4 },
    });

    const below = await loadFreshInitSentry();
    below({
      app,
      dsn: "",
      openTelemetry: { serviceName: "application-web", tracesSampleRate: -2 },
    });

    expect(otel.sampleRatios).toEqual([1, 0]);
  });

  it("inherits the Sentry sample rate when OpenTelemetry does not set one", async () => {
    const freshInitSentry = await loadFreshInitSentry();

    freshInitSentry({
      app,
      dsn: "",
      tracesSampleRate: 0.5,
      openTelemetry: { serviceName: "application-web" },
    });

    expect(otel.sampleRatios).toEqual([0.5]);
  });

  it("propagates trace headers only to the configured targets", async () => {
    const freshInitSentry = await loadFreshInitSentry();

    freshInitSentry({
      app,
      dsn: "",
      openTelemetry: {
        serviceName: "application-web",
        tracePropagationTargets: ["https://api.nie.edu.sg"],
      },
    });

    expect(otel.fetchOptions).toHaveLength(1);
    expect(otel.fetchOptions[0].propagateTraceHeaderCorsUrls).toEqual([
      "https://api.nie.edu.sg",
    ]);
  });

  it("never traces requests to the Sentry ingest endpoints", async () => {
    const freshInitSentry = await loadFreshInitSentry();

    freshInitSentry({
      app,
      dsn: "",
      openTelemetry: { serviceName: "application-web" },
    });

    const ignoreUrls = otel.fetchOptions[0].ignoreUrls as RegExp[];
    expect(
      ignoreUrls.some((pattern) => pattern.test("https://o1.ingest.sentry.io/")),
    ).toBe(true);
    expect(
      ignoreUrls.some((pattern) => pattern.test("/api/42/envelope/")),
    ).toBe(true);
    expect(
      ignoreUrls.some((pattern) => pattern.test("https://api.nie.edu.sg/users")),
    ).toBe(false);
  });

  it("falls back to the default service name", async () => {
    const freshInitSentry = await loadFreshInitSentry();

    freshInitSentry({ app, dsn: "" });

    expect(otel.providerOptions[0].resource).toMatchObject({
      "service.name": "application-web",
    });
  });
});
