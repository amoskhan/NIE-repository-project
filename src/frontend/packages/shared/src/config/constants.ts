type RuntimeEnvironment = "local" | "dev" | "stg" | "prd";
type BackendService = "auth" | "main";
type FrontendApp = "auth" | "main";

/**
 * Non-secret values the deployment injects at RUNTIME, so one build artifact can be
 * promoted from dev to staging to production untouched.
 *
 * Two equivalent delivery channels (checked in this order):
 *   1. `window.__APP_TEMPLATE_CONFIG__ = { ... }` — an inline <script> in index.html.
 *   2. `<meta name="app:<key>" content="<value>">` — one meta tag per key.
 *
 * Do NOT add `.env` files or `import.meta.env.VITE_*` application config here; those
 * bake values into the bundle at build time and break environment promotion.
 */
interface AppTemplateRuntimeConfig {
  /**
   * Absolute path this bundle is mounted on, e.g. "/MYAPP" or
   * "/ignite/coder/@jane/ws-42/apps/preview-main/".
   *
   * OVERRIDES the first-path-segment heuristic in getAppBasePath(). Set this whenever the
   * mount is deeper than one segment — the heuristic reads only the FIRST segment, so under
   * "/ignite/coder/@jane/ws-42/apps/preview-main/" it would answer "/ignite" and every URL
   * derived from it would 404. A trailing slash is accepted and normalised away.
   */
  appBasePath?: string;
  /**
   * Absolute URL or absolute path the AUTH API is served from, e.g. "/api-auth" or
   * "https://api.example.edu/auth". Overrides the "<appBasePath>/api-auth" convention —
   * set it when the API does not live beside the app under a shared prefix.
   */
  authApiBaseUrl?: string;
  /**
   * Absolute URL or absolute path of the AUTH frontend, e.g. "/login/". Overrides the
   * "<appBasePath>/login/" convention, used when redirecting an unauthenticated user.
   */
  authAppUrl?: string;
  /** Cookie `domain` attribute; leave unset to scope cookies to the serving host. */
  cookieDomain?: string;
  /**
   * Which deployment this bundle is serving: "local" | "dev" | "stg" | "prd".
   * This is the PRIMARY signal for getRuntimeEnvironment() — set it per environment.
   */
  environment?: RuntimeEnvironment | string;
  /**
   * OPTIONAL last-resort hostname matching, used only when `environment` is absent.
   * Map a hostname SUFFIX to an environment, e.g.
   *   { ".stg.my-school.edu": "stg", ".dev.my-school.edu": "dev" }
   * Longest suffix wins. Leave unset to rely on `environment` + the build mode.
   */
  environmentHostnameSuffixes?: Record<string, RuntimeEnvironment | string>;
  /**
   * Absolute URL or absolute path the MAIN API is served from, e.g. "/api-main".
   * Overrides the "<appBasePath>/api-main" convention.
   */
  mainApiBaseUrl?: string;
  /**
   * Absolute URL or absolute path of the MAIN frontend, e.g. "/". Overrides the
   * "<appBasePath>/" convention, used when redirecting a signed-in user to the dashboard.
   */
  mainAppUrl?: string;
  oneSignalAppId?: string;
  openTelemetryExporterEndpoint?: string;
  sentryDsn?: string;
  sentryEnvironment?: RuntimeEnvironment | string;
  sentryTracesSampleRate?: number;
  /**
   * Name of the session-token cookie; defaults to "AppTemplate-SessionToken".
   *
   * Override this whenever several deployments of this app share ONE public hostname
   * (an Ignite/Coder workspace preview is the motivating case: every student workspace is
   * served from the same host). A fixed cookie name means two such deployments open in one
   * browser silently overwrite each other's session, so this is an isolation fix.
   */
  sessionCookieName?: string;
  /** Name of the cached-user cookie; defaults to "AppTemplate-User". See sessionCookieName. */
  userCookieName?: string;
}

declare global {
  interface Window {
    __APP_TEMPLATE_CONFIG__?: AppTemplateRuntimeConfig;
  }
}

/** Prefix for the `<meta name="app:<key>">` runtime-config channel. */
const RUNTIME_META_PREFIX = "app";

const BACKEND_SEGMENTS: Record<BackendService, string> = {
  auth: "api-auth",
  main: "api-main",
};

const FRONTEND_SEGMENTS: Record<FrontendApp, string> = {
  auth: "login",
  main: "",
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

const DEFAULT_SESSION_COOKIE_NAME = "AppTemplate-SessionToken";
const DEFAULT_USER_COOKIE_NAME = "AppTemplate-User";

/** Runtime-config key holding an explicit base URL for each backend service. */
const BACKEND_URL_KEYS: Record<BackendService, keyof AppTemplateRuntimeConfig> =
  {
    auth: "authApiBaseUrl",
    main: "mainApiBaseUrl",
  };

/** Runtime-config key holding an explicit URL for each frontend app. */
const FRONTEND_URL_KEYS: Record<FrontendApp, keyof AppTemplateRuntimeConfig> = {
  auth: "authAppUrl",
  main: "mainAppUrl",
};

function getRuntimeConfig(): AppTemplateRuntimeConfig {
  if (typeof window === "undefined") {
    return {};
  }

  return window.__APP_TEMPLATE_CONFIG__ ?? {};
}

function getMetaContent(name: string): string | undefined {
  if (typeof document === "undefined") {
    return undefined;
  }

  const meta = document.querySelector<HTMLMetaElement>(
    `meta[name="${RUNTIME_META_PREFIX}:${name}"]`,
  );
  return meta?.content?.trim() || undefined;
}

function getRuntimeString(
  key: keyof AppTemplateRuntimeConfig,
): string | undefined {
  const value = getRuntimeConfig()[key];
  if (typeof value === "string" && value.trim()) {
    return value.trim();
  }

  return getMetaContent(String(key));
}

function getRuntimeNumber(
  key: keyof AppTemplateRuntimeConfig,
  fallback: number,
): number {
  const value = getRuntimeConfig()[key];
  if (typeof value === "number" && Number.isFinite(value)) {
    return value;
  }

  const metaValue = getMetaContent(String(key));
  if (metaValue) {
    const parsed = Number(metaValue);
    if (Number.isFinite(parsed)) {
      return parsed;
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

const RUNTIME_ENVIRONMENTS: readonly RuntimeEnvironment[] = [
  "local",
  "dev",
  "stg",
  "prd",
];

/** Narrow an arbitrary string to a RuntimeEnvironment, or undefined if unrecognised. */
function coerceRuntimeEnvironment(
  value: string | undefined,
): RuntimeEnvironment | undefined {
  const normalized = value?.trim().toLowerCase();
  return RUNTIME_ENVIRONMENTS.find((env) => env === normalized);
}

/**
 * OPTIONAL, configurable last resort: match the hostname against suffix -> environment
 * pairs supplied via runtime config. Ships EMPTY on purpose — the template must not
 * hardcode any single institution's DNS. Longest matching suffix wins so that
 * ".dev.example.edu" beats ".example.edu".
 */
function getEnvironmentFromHostnameSuffixes(
  hostname: string,
): RuntimeEnvironment | undefined {
  const suffixes = getRuntimeConfig().environmentHostnameSuffixes;
  if (!suffixes || !hostname) {
    return undefined;
  }

  let matched: { length: number; env: RuntimeEnvironment } | undefined;

  for (const [suffix, env] of Object.entries(suffixes)) {
    const normalizedSuffix = suffix.trim().toLowerCase();
    const resolved = coerceRuntimeEnvironment(env);
    if (
      !normalizedSuffix ||
      !resolved ||
      !hostname.endsWith(normalizedSuffix)
    ) {
      continue;
    }

    if (!matched || normalizedSuffix.length > matched.length) {
      matched = { length: normalizedSuffix.length, env: resolved };
    }
  }

  return matched?.env;
}

/**
 * Resolve which deployment this bundle is running in.
 *
 * Resolution order (first hit wins):
 *   1. `localhost` / `127.0.0.1` / `::1`            -> "local"
 *   2. runtime config `environment`                 -> the configured value
 *      (window.__APP_TEMPLATE_CONFIG__.environment or <meta name="app:environment">)
 *   3. runtime config `environmentHostnameSuffixes` -> optional hostname matching
 *   4. the Vite build mode                          -> production/staging/otherwise dev
 *
 * Deployments should set (2). Steps (3) and (4) exist so an unconfigured build still
 * degrades to something sensible instead of guessing from a hardcoded domain.
 */
export function getRuntimeEnvironment(
  hostname = getRuntimeHostname(),
): RuntimeEnvironment {
  if (isLocalHostname(hostname)) {
    return "local";
  }

  const configured = coerceRuntimeEnvironment(getRuntimeString("environment"));
  if (configured) {
    return configured;
  }

  const fromHostname = getEnvironmentFromHostnameSuffixes(hostname);
  if (fromHostname) {
    return fromHostname;
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

/** True for values the browser treats as an origin-carrying URL rather than a path. */
function isAbsoluteUrl(value: string): boolean {
  return /^[a-z][a-z0-9+.-]*:\/\//i.test(value);
}

/**
 * Normalise a base URL supplied by runtime config into the same shape the derived
 * defaults use: no trailing slash, leading slash for paths, origins left intact.
 */
function normalizeConfiguredBaseUrl(value: string): string {
  if (isAbsoluteUrl(value)) {
    return value.replace(/\/+$/, "");
  }

  return normalizePath(value);
}

/** joinPath(), but safe when `base` is an absolute URL (joinPath would mangle the scheme). */
function joinBaseAndPath(base: string, path: string): string {
  const suffix = normalizePath(path);
  if (isAbsoluteUrl(base)) {
    return `${base.replace(/\/+$/, "")}${suffix}`;
  }

  return joinPath(base, suffix);
}

/**
 * The path prefix this app is mounted on, WITHOUT a trailing slash ("" at the root).
 *
 * Runtime config `appBasePath` wins when present: it is a statement of fact about the
 * deployment, whereas the fallback below can only guess from the current URL and assumes a
 * single-segment mount. Pass `pathname` to exercise that fallback in tests.
 */
export function getAppBasePath(pathname?: string): string {
  const configured = getRuntimeString("appBasePath");
  if (configured) {
    return normalizeConfiguredBaseUrl(configured);
  }

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

/**
 * Where the given frontend app lives, with a trailing slash.
 *
 * Runtime config `mainAppUrl` / `authAppUrl` wins when present — the two apps are not always
 * siblings under one prefix (under a per-app proxy mount they are not). Otherwise the
 * historical behaviour applies: localhost dev ports, else "<appBasePath>/<segment>/".
 */
export function getFrontendUrl(app: FrontendApp): string {
  const configured = getRuntimeString(FRONTEND_URL_KEYS[app]);
  if (configured) {
    return ensureTrailingSlash(normalizeConfiguredBaseUrl(configured));
  }

  if (isLocalHostname()) {
    return getLocalFrontendUrl(app);
  }

  const segment = FRONTEND_SEGMENTS[app];
  return ensureTrailingSlash(joinBaseAndPath(getAppBasePath(), segment));
}

export function getFrontendAssetUrl(
  assetPath: string,
  app: FrontendApp = "main",
): string {
  const segment = FRONTEND_SEGMENTS[app];
  return joinBaseAndPath(
    getAppBasePath(),
    joinPath(segment, normalizePath(assetPath)),
  );
}

/**
 * Base URL of a backend service, WITHOUT a trailing slash.
 *
 * Runtime config `mainApiBaseUrl` / `authApiBaseUrl` wins when present; otherwise the API is
 * assumed to sit beside the app under the shared prefix ("<appBasePath>/api-main").
 */
export function getBackendBaseUrl(service: BackendService): string {
  const configured = getRuntimeString(BACKEND_URL_KEYS[service]);
  if (configured) {
    return normalizeConfiguredBaseUrl(configured);
  }

  return joinBaseAndPath(getAppBasePath(), BACKEND_SEGMENTS[service]);
}

export function getBackendUrl(service: BackendService, path = ""): string {
  return joinBaseAndPath(getBackendBaseUrl(service), path);
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
  backend: {
    auth: getBackendBaseUrl("auth"),
    main: getBackendBaseUrl("main"),
  },
  cookies: {
    domain: getRuntimeString("cookieDomain"),
    // Overridable so that deployments sharing one public hostname do not clobber each
    // other's cookies. See AppTemplateRuntimeConfig.sessionCookieName.
    session:
      getRuntimeString("sessionCookieName") ?? DEFAULT_SESSION_COOKIE_NAME,
    user: getRuntimeString("userCookieName") ?? DEFAULT_USER_COOKIE_NAME,
  },
  features: {
    useDemoGlobalSettings: true,
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

export function getCookieAttributes(): { domain?: string } {
  return FRONTEND_CONSTANTS.cookies.domain
    ? { domain: FRONTEND_CONSTANTS.cookies.domain }
    : {};
}
