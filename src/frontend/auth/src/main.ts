import { createApp } from "vue";
import { initTheme } from "@apptemplate/ui";
import { FRONTEND_CONSTANTS, i18n, initSentry } from "@apptemplate/shared";
import "./style.css";
import App from "./App.vue";
import router from "./router";
import { authThemeConfig } from "./theme/appTheme";

initTheme(authThemeConfig);

const app = createApp(App);
app.use(router);
app.use(i18n);

initSentry({
  app,
  dsn: FRONTEND_CONSTANTS.sentry.dsn,
  environment: FRONTEND_CONSTANTS.sentry.environment,
  openTelemetry: {
    enabled: FRONTEND_CONSTANTS.openTelemetry.enabled,
    exporterEndpoint: FRONTEND_CONSTANTS.openTelemetry.exporterEndpoint,
    serviceName: "apptemplate-auth-web",
  },
  replaysOnErrorSampleRate: FRONTEND_CONSTANTS.sentry.replaysOnErrorSampleRate,
  replaysSessionSampleRate: FRONTEND_CONSTANTS.sentry.replaysSessionSampleRate,
  tags: {
    app: "auth",
  },
  tracesSampleRate: FRONTEND_CONSTANTS.sentry.tracesSampleRate,
});

app.mount("#app");
