import { z } from "zod";

type RuntimeEnvironment = "local" | "dev" | "stg" | "prd";
type BackendService = "auth" | "main";
type FrontendApp = "auth" | "main";

const sameOriginPathSchema = z
  .string()
  .trim()
  .min(1)
  .max(2048)
  .refine(
    (value) =>
      value.startsWith("/") &&
      !value.startsWith("//") &&
      !value.includes("\\") &&
      !value.includes("?") &&
      !value.includes("#"),
    "Runtime routes must be same-origin absolute paths without a query or fragment.",
  );
const cookieNameSchema = z
  .string()
  .trim()
  .min(1)
  .max(128)
  .regex(/^[A-Za-z0-9!#$%&'*+.^_`|~-]+$/);
const cookieDomainSchema = z
  .string()
  .trim()
  .max(253)
  .refine(
    (value) =>
      value === "" ||
      /^\.?(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?)(?:\.(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?))*$/i.test(
        value,
      ),
    "Cookie domain is invalid.",
  );
const optionalStringSchema = z
  .string()
  .trim()
  .max(2048)
  .optional()
  .catch(undefined);
const optionalUrlSchema = z.string().trim().url().optional().catch(undefined);

const applicationRuntimeConfigSchema = z.object({
  backendAuthBaseUrl: sameOriginPathSchema.optional().catch(undefined),
  backendMainBaseUrl: sameOriginPathSchema.optional().catch(undefined),
  cookieDomain: cookieDomainSchema.optional().catch(undefined),
  cookiePath: sameOriginPathSchema.optional().catch(undefined),
  frontendAuthUrl: sameOriginPathSchema.optional().catch(undefined),
  frontendMainUrl: sameOriginPathSchema.optional().catch(undefined),
  oneSignalAppId: optionalStringSchema,
  openTelemetryExporterEndpoint: optionalUrlSchema,
  portalSsoEnabled: z.boolean().optional().catch(undefined),
  sentryDsn: optionalUrlSchema,
  sentryEnvironment: optionalStringSchema,
  sentryTracesSampleRate: z.number().min(0).max(1).optional().catch(undefined),
  sessionCookieName: cookieNameSchema.optional().catch(undefined),
  userCookieName: cookieNameSchema.optional().catch(undefined),
});

type ApplicationRuntimeConfig = z.infer<typeof applicationRuntimeConfigSchema>;

const runtimeServiceIdSchema = z.string().regex(/^[a-z][a-z0-9-]{0,39}$/);
const runtimeServiceSchema = z.object({
  id: runtimeServiceIdSchema,
  displayName: z.string().trim().min(1).max(100),
  kind: z.enum(["frontend", "backend"]),
  baseUrl: sameOriginPathSchema,
});
const runtimeServicesSchema = z
  .object({
    version: z.literal(1),
    services: z.record(runtimeServiceIdSchema, runtimeServiceSchema),
  })
  .superRefine((runtime, context) => {
    for (const [id, service] of Object.entries(runtime.services)) {
      if (service.id !== id) {
        context.addIssue({
          code: z.ZodIssueCode.custom,
          message: `Runtime service key ${id} does not match its id.`,
          path: ["services", id, "id"],
        });
      }
    }
  });

type RuntimeServices = z.infer<typeof runtimeServicesSchema>;

declare global {
  interface Window {
    __NIE_APPLICATION_CONFIG__?: unknown;
    __NIE_RUNTIME_SERVICES__?: unknown;
  }
}

const BACKEND_SEGMENTS: Record<BackendService, string> = {
  auth: "api-auth",
  main: "api-main",
};

const BACKEND_OVERRIDE_KEYS: Record<
  BackendService,
  keyof ApplicationRuntimeConfig
> = {
  auth: "backendAuthBaseUrl",
  main: "backendMainBaseUrl",
};

const FRONTEND_SEGMENTS: Record<FrontendApp, string> = {
  auth: "login",
  main: "",
};

const FRONTEND_OVERRIDE_KEYS: Record<
  FrontendApp,
  keyof ApplicationRuntimeConfig
> = {
  auth: "frontendAuthUrl",
  main: "frontendMainUrl",
};

const LOCAL_FRONTEND_PORTS: Record<FrontendApp, number> = {
  auth: 8001,
  main: 8002,
};

const ROOT_LEVEL_SEGMENTS = new Set([
  "assets",
  "api-auth",
  "api-main",
  "favicon.ico",
  "login",
  "manifest.json",
  "showcase",
  "status-pages",
  "sw.js",
]);

const DEFAULT_SESSION_TIMEOUT_MINUTES = 60;

function getRuntimeConfig(): ApplicationRuntimeConfig {
  if (typeof window === "undefined") {
    return {};
  }

  const result = applicationRuntimeConfigSchema.safeParse(
    window.__NIE_APPLICATION_CONFIG__,
  );
  return result.success ? result.data : {};
}

function getRuntimeServices(): RuntimeServices | undefined {
  if (typeof window === "undefined") {
    return undefined;
  }

  const result = runtimeServicesSchema.safeParse(
    window.__NIE_RUNTIME_SERVICES__,
  );
  return result.success ? result.data : undefined;
}

function getMetaContent(name: string): string | undefined {
  if (typeof document === "undefined") {
    return undefined;
  }

  const meta = document.querySelector<HTMLMetaElement>(
    `meta[name="nie:${name}"]`,
  );
  return meta?.content?.trim() || undefined;
}

function getRuntimeString(
  key: keyof ApplicationRuntimeConfig,
): string | undefined {
  const config = getRuntimeConfig();
  const value = config[key];
  if (typeof value === "string") {
    return value;
  }

  const metaValue = getMetaContent(String(key));
  if (metaValue === undefined) {
    return undefined;
  }

  const fieldSchema = applicationRuntimeConfigSchema.shape[key];
  const parsed = fieldSchema.safeParse(metaValue);
  return parsed.success && typeof parsed.data === "string"
    ? parsed.data
    : undefined;
}

function getRuntimeBoolean(
  key: keyof ApplicationRuntimeConfig,
  fallback: boolean,
): boolean {
  const value = getRuntimeConfig()[key];
  if (typeof value === "boolean") {
    return value;
  }

  const metaValue = getMetaContent(String(key))?.toLowerCase();
  if (metaValue === "true") {
    return true;
  }

  if (metaValue === "false") {
    return false;
  }

  return fallback;
}

function getRuntimeNumber(
  key: keyof ApplicationRuntimeConfig,
  fallback: number,
): number {
  const value = getRuntimeConfig()[key];
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }

  const metaValue = getMetaContent(String(key));
  if (metaValue) {
    const parsed = Number(metaValue);
    const fieldSchema = applicationRuntimeConfigSchema.shape[key];
    const validated = fieldSchema.safeParse(parsed);
    if (validated.success && typeof validated.data === "number") {
      return validated.data;
    }
  }

  return fallback;
}

export function getRuntimeHostname(): string {
  if (typeof window === "undefined") {
    return "";
  }

  return window.location.hostname.toLowerCase();
}

function getBuildMode(): string {
  const meta = import.meta as ImportMeta & { env?: { MODE?: string } };
  return meta.env?.MODE?.toLowerCase() ?? "development";
}

export function isLocalHostname(hostname = getRuntimeHostname()): boolean {
  return (
    hostname === "localhost" || hostname === "127.0.0.1" || hostname === "::1"
  );
}

export function getRuntimeEnvironment(
  hostname = getRuntimeHostname(),
): RuntimeEnvironment {
  if (isLocalHostname(hostname)) {
    return "local";
  }

  if (hostname.endsWith(".stg.nie.edu.sg")) {
    return "stg";
  }

  if (hostname.endsWith(".dev.nie.edu.sg")) {
    return "dev";
  }

  if (hostname.endsWith(".nie.edu.sg")) {
    return "prd";
  }

  const mode = getBuildMode();
  if (mode === "production") {
    return "prd";
  }

  if (mode === "staging") {
    return "stg";
  }

  return "dev";
}

function normalizePath(path: string): string {
  if (!path || path === "/") {
    return "";
  }

  return `/${path.replace(/^\/+|\/+$/g, "")}`;
}

function joinPath(...parts: string[]): string {
  const joined = parts
    .map((part) => part.replace(/^\/+|\/+$/g, ""))
    .filter(Boolean)
    .join("/");

  return joined ? `/${joined}` : "/";
}

function ensureTrailingSlash(path: string): string {
  return path.endsWith("/") ? path : `${path}/`;
}

function appendPath(baseUrl: string, path = ""): string {
  const normalizedPath = normalizePath(path);
  if (!normalizedPath) {
    return baseUrl || "/";
  }

  return `${baseUrl.replace(/\/+$/, "")}${normalizedPath}`;
}

export function getRuntimeServiceBaseUrl(
  serviceId: string,
): string | undefined {
  const parsedId = runtimeServiceIdSchema.safeParse(serviceId);
  if (!parsedId.success) {
    return undefined;
  }

  return getRuntimeServices()?.services[parsedId.data]?.baseUrl;
}

export function getRuntimeServiceUrl(
  serviceId: string,
  path = "",
): string | undefined {
  const baseUrl = getRuntimeServiceBaseUrl(serviceId);
  return baseUrl === undefined ? undefined : appendPath(baseUrl, path);
}

export function getAppBasePath(pathname?: string): string {
  if (typeof window === "undefined" && !pathname) {
    return "";
  }

  const path = pathname ?? window.location.pathname;
  const segments = path.split("/").filter(Boolean);
  if (segments.length === 0) {
    return "";
  }

  const first = segments[0].toLowerCase();
  if (ROOT_LEVEL_SEGMENTS.has(first)) {
    return "";
  }

  return `/${segments[0]}`;
}

function getLocalHostForUrl(): string {
  return getRuntimeHostname() === "127.0.0.1" ? "127.0.0.1" : "localhost";
}

function getLocalFrontendUrl(app: FrontendApp): string {
  const segment = FRONTEND_SEGMENTS[app];
  const path = segment ? `/${segment}/` : "/";
  return `http://${getLocalHostForUrl()}:${LOCAL_FRONTEND_PORTS[app]}${path}`;
}

export function getFrontendUrl(app: FrontendApp): string {
  const runtimeUrl = getRuntimeServiceBaseUrl(
    app === "auth" ? "preview-auth" : "preview-main",
  );
  if (runtimeUrl) {
    return ensureTrailingSlash(runtimeUrl);
  }

  const override = getRuntimeString(FRONTEND_OVERRIDE_KEYS[app]);
  if (override) {
    return ensureTrailingSlash(override);
  }

  if (isLocalHostname()) {
    return getLocalFrontendUrl(app);
  }

  const segment = FRONTEND_SEGMENTS[app];
  return ensureTrailingSlash(
    segment ? joinPath(getAppBasePath(), segment) : joinPath(getAppBasePath()),
  );
}

export function getFrontendAssetUrl(
  assetPath: string,
  app: FrontendApp = "main",
): string {
  const segment = FRONTEND_SEGMENTS[app];
  return joinPath(getAppBasePath(), segment, normalizePath(assetPath));
}

export function getBackendBaseUrl(service: BackendService): string {
  const runtimeUrl = getRuntimeServiceBaseUrl(
    service === "auth" ? "auth-api" : "main-api",
  );
  if (runtimeUrl) {
    return runtimeUrl.replace(/\/+$/, "");
  }

  const override = getRuntimeString(BACKEND_OVERRIDE_KEYS[service]);
  if (override) {
    return override.replace(/\/+$/, "");
  }

  return joinPath(getAppBasePath(), BACKEND_SEGMENTS[service]);
}

export function getBackendUrl(service: BackendService, path = ""): string {
  return appendPath(getBackendBaseUrl(service), path);
}

const sentryDsn = getRuntimeString("sentryDsn") ?? "";
const sentryEnvironment =
  getRuntimeString("sentryEnvironment") ?? getRuntimeEnvironment();
const openTelemetryExporterEndpoint =
  getRuntimeString("openTelemetryExporterEndpoint") ?? "";

export const FRONTEND_CONSTANTS = {
  api: {
    auth: getBackendUrl("auth", "/api"),
    main: getBackendUrl("main", "/api"),
  },
  apps: {
    auth: getFrontendUrl("auth"),
    main: getFrontendUrl("main"),
  },
  auth: {
    portalSsoEnabled: getRuntimeBoolean("portalSsoEnabled", false),
    ssoBaseUrl: getBackendUrl("auth", "/api"),
  },
  backend: {
    auth: getBackendBaseUrl("auth"),
    main: getBackendBaseUrl("main"),
  },
  cookies: {
    domain: getRuntimeString("cookieDomain"),
    path: getRuntimeString("cookiePath"),
    session:
      getRuntimeString("sessionCookieName") ?? "Application-SessionToken",
    user: getRuntimeString("userCookieName") ?? "Application-User",
  },
  features: {
    useDemoNotifications: true,
  },
  oneSignal: {
    appId: getRuntimeString("oneSignalAppId") ?? "",
    allowLocalhostAsSecureOrigin: isLocalHostname(),
    enabled:
      !isLocalHostname() && Boolean(getRuntimeString("oneSignalAppId") ?? ""),
  },
  openTelemetry: {
    enabled: !isLocalHostname() && openTelemetryExporterEndpoint.length > 0,
    exporterEndpoint: openTelemetryExporterEndpoint,
  },
  sentry: {
    dsn: sentryDsn,
    enabled: sentryDsn.length > 0,
    environment: sentryEnvironment,
    replaysOnErrorSampleRate: 0.1,
    replaysSessionSampleRate: 0,
    tracesSampleRate: getRuntimeNumber("sentryTracesSampleRate", 0.2),
  },
  session: {
    timeoutMinutes: DEFAULT_SESSION_TIMEOUT_MINUTES,
  },
} as const;

export function getCookieAttributes(): { domain?: string; path?: string } {
  return {
    ...(FRONTEND_CONSTANTS.cookies.domain
      ? { domain: FRONTEND_CONSTANTS.cookies.domain }
      : {}),
    ...(FRONTEND_CONSTANTS.cookies.path
      ? { path: FRONTEND_CONSTANTS.cookies.path }
      : {}),
  };
}
