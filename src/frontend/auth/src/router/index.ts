// PROJECT-OWNED — safe to edit.
//
// Router for the auth app. Hash history (like the main app) so the whole SPA can be
// served as static files from /login/ without any nginx rewrite rules — every route is
// after the "#", which the server never sees.
//
// Add a screen by adding a record here; every page renders inside AuthShell, so it will
// pick up the shared layout automatically.

import { createRouter, createWebHashHistory } from "vue-router";
import type { RouteRecordRaw } from "vue-router";
import { authThemeConfig } from "../theme/appTheme";

const routes: RouteRecordRaw[] = [
  {
    path: "/",
    name: "login",
    component: () => import("../components/LoginPage.vue"),
    meta: { title: "Sign In" },
  },
  {
    path: "/register",
    name: "register",
    component: () => import("../components/RegisterPage.vue"),
    meta: { title: "Create Account" },
  },
  {
    path: "/forgot-password",
    name: "forgot-password",
    component: () => import("../components/ForgotPasswordPage.vue"),
    meta: { title: "Forgot Password" },
  },
  {
    path: "/reset-password",
    name: "reset-password",
    component: () => import("../components/ResetPasswordPage.vue"),
    meta: { title: "Reset Password" },
  },
  // Anything unrecognised falls back to the login screen.
  { path: "/:pathMatch(.*)*", redirect: "/" },
];

const router = createRouter({
  history: createWebHashHistory(),
  routes,
});

router.afterEach((to) => {
  const brand = authThemeConfig.brandLabel ?? "App Template";
  const title = to.meta?.title as string | undefined;
  document.title = title ? `${brand} | ${title}` : brand;
});

export default router;
