import {
  CompositePropagator,
  W3CBaggagePropagator,
  W3CTraceContextPropagator,
} from "@opentelemetry/core";
import { OTLPTraceExporter } from "@opentelemetry/exporter-trace-otlp-http";
import { registerInstrumentations } from "@opentelemetry/instrumentation";
import { DocumentLoadInstrumentation } from "@opentelemetry/instrumentation-document-load";
import { FetchInstrumentation } from "@opentelemetry/instrumentation-fetch";
import { XMLHttpRequestInstrumentation } from "@opentelemetry/instrumentation-xml-http-request";
import { resourceFromAttributes } from "@opentelemetry/resources";
import {
  BatchSpanProcessor,
  ParentBasedSampler,
  TraceIdRatioBasedSampler,
  WebTracerProvider,
} from "@opentelemetry/sdk-trace-web";
import * as Sentry from "@sentry/vue";
import type { App } from "vue";
import type { Router } from "vue-router";

type TracePropagationTarget = string | RegExp;

interface BrowserOpenTelemetryConfig {
  enabled?: boolean;
  environment?: string;
  exporterEndpoint?: string;
  serviceName: string;
  serviceNamespace?: string;
  tracesSampleRate?: number;
  tracePropagationTargets?: TracePropagationTarget[];
}

interface SentryConfig {
  app: App;
  dsn: string;
  enableLogs?: boolean;
  environment?: string;
  openTelemetry?: BrowserOpenTelemetryConfig;
  replaysOnErrorSampleRate?: number;
  replaysSessionSampleRate?: number;
  router?: Router;
  tags?: Record<string, string>;
  tracePropagationTargets?: TracePropagationTarget[];
  tracesSampleRate?: number;
}

const defaultTracePropagationTargets: TracePropagationTarget[] = [
  /^\//,
  /^https?:\/\/localhost:\d+/i,
  /^https?:\/\/127\.0\.0\.1:\d+/i,
  /^https?:\/\/\[::1\]:\d+/i,
];

const telemetryRequestIgnoreUrls: Array<string | RegExp> = [
  /sentry\.io/i,
  /ingest\.[\w.-]*sentry\.io/i,
  /\/api\/\d+\/(envelope|store|minidump)\//i,
];

let openTelemetryInitialized = false;

export function initSentry({
  app,
  dsn,
  enableLogs = true,
  environment,
  openTelemetry,
  replaysOnErrorSampleRate = 0,
  replaysSessionSampleRate = 0,
  router,
  tags,
  tracePropagationTargets,
  tracesSampleRate = 0.2,
}: SentryConfig): void {
  const resolvedTraceTargets =
    tracePropagationTargets ??
    openTelemetry?.tracePropagationTargets ??
    defaultTracePropagationTargets;

  initBrowserOpenTelemetry({
    enabled: openTelemetry?.enabled,
    environment: openTelemetry?.environment ?? environment,
    exporterEndpoint: openTelemetry?.exporterEndpoint,
    serviceName: openTelemetry?.serviceName ?? "application-web",
    serviceNamespace: openTelemetry?.serviceNamespace,
    tracesSampleRate: openTelemetry?.tracesSampleRate ?? tracesSampleRate,
    tracePropagationTargets: resolvedTraceTargets,
  });

  if (!dsn) {
    return;
  }

  const integrations = [
    router
      ? Sentry.browserTracingIntegration({ router })
      : Sentry.browserTracingIntegration(),
  ];

  if (replaysSessionSampleRate > 0 || replaysOnErrorSampleRate > 0) {
    integrations.push(
      Sentry.replayIntegration({
        blockAllMedia: true,
        maskAllText: true,
      }),
    );
  }

  Sentry.init({
    app,
    dsn,
    enableLogs,
    environment: environment || "development",
    initialScope: {
      tags: {
        service: openTelemetry?.serviceName ?? "application-web",
        ...tags,
      },
    },
    integrations,
    replaysOnErrorSampleRate,
    replaysSessionSampleRate,
    sendDefaultPii: false,
    tracesSampleRate,
    tracePropagationTargets: resolvedTraceTargets,
    beforeSend(event) {
      if (event.request?.cookies) {
        delete event.request.cookies;
      }

      return event;
    },
  });
}

function initBrowserOpenTelemetry({
  enabled = true,
  environment = "development",
  exporterEndpoint,
  serviceName,
  serviceNamespace = "application.frontend",
  tracesSampleRate = 0.2,
  tracePropagationTargets = defaultTracePropagationTargets,
}: BrowserOpenTelemetryConfig): void {
  if (!enabled || openTelemetryInitialized || typeof window === "undefined") {
    return;
  }

  const safeSampleRate = Math.min(Math.max(tracesSampleRate, 0), 1);
  const spanProcessors = exporterEndpoint
    ? [
        new BatchSpanProcessor(
          new OTLPTraceExporter({
            url: exporterEndpoint,
          }),
        ),
      ]
    : [];

  const provider = new WebTracerProvider({
    resource: resourceFromAttributes({
      "deployment.environment": environment,
      "service.name": serviceName,
      "service.namespace": serviceNamespace,
    }),
    sampler: new ParentBasedSampler({
      root: new TraceIdRatioBasedSampler(safeSampleRate),
    }),
    spanProcessors,
  });

  provider.register({
    propagator: new CompositePropagator({
      propagators: [
        new W3CTraceContextPropagator(),
        new W3CBaggagePropagator(),
      ],
    }),
  });

  registerInstrumentations({
    instrumentations: [
      new DocumentLoadInstrumentation(),
      new FetchInstrumentation({
        clearTimingResources: true,
        ignoreNetworkEvents: true,
        ignoreUrls: telemetryRequestIgnoreUrls,
        propagateTraceHeaderCorsUrls: tracePropagationTargets,
      }),
      new XMLHttpRequestInstrumentation({
        clearTimingResources: true,
        ignoreNetworkEvents: true,
        ignoreUrls: telemetryRequestIgnoreUrls,
        propagateTraceHeaderCorsUrls: tracePropagationTargets,
      }),
    ],
    tracerProvider: provider,
  });

  openTelemetryInitialized = true;
}

export { Sentry };
