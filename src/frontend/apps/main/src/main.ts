import { createApp } from "vue";
import { initTheme, nieDataTablePreferenceStoreKey } from "@nie/ui";
import {
  FRONTEND_CONSTANTS,
  getFrontendAssetUrl,
  i18n,
  initSentry,
} from "@nie/platform";
import "@nie/ui/styles";
import "./style.css";
import App from "./App.vue";
import router from "./router";
import { mainThemeConfig } from "./theme/appTheme";
import { initPushNotifications } from "./services/notifications/oneSignalService";
import { dataTablePreferenceStore } from "./services/preferences/dataTablePreferenceService";

initTheme(mainThemeConfig);

const app = createApp(App);
app.use(router);
app.use(i18n);
app.provide(nieDataTablePreferenceStoreKey, dataTablePreferenceStore);

initSentry({
  app,
  dsn: FRONTEND_CONSTANTS.sentry.dsn,
  environment: FRONTEND_CONSTANTS.sentry.environment,
  openTelemetry: {
    enabled: FRONTEND_CONSTANTS.openTelemetry.enabled,
    exporterEndpoint: FRONTEND_CONSTANTS.openTelemetry.exporterEndpoint,
    serviceName: "application-main-web",
  },
  replaysOnErrorSampleRate: FRONTEND_CONSTANTS.sentry.replaysOnErrorSampleRate,
  replaysSessionSampleRate: FRONTEND_CONSTANTS.sentry.replaysSessionSampleRate,
  router,
  tags: {
    app: "main",
  },
  tracesSampleRate: FRONTEND_CONSTANTS.sentry.tracesSampleRate,
});

// Initialize the active push provider (no-op if no provider is configured).
initPushNotifications();

// Register service worker for offline support
if (window.top === window.self && "serviceWorker" in navigator) {
  navigator.serviceWorker.register(getFrontendAssetUrl("sw.js")).catch(() => {
    /* SW registration failed – app still works online */
  });
}

app.mount("#app");

