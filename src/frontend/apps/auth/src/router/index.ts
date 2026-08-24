import { createRouter, createWebHashHistory } from "vue-router";
import type { RouteRecordRaw } from "vue-router";

const routes: RouteRecordRaw[] = [
  {
    path: "/",
    name: "login",
    component: () => import("../components/LoginPage.vue"),
  },
  {
    path: "/:pathMatch(.*)*",
    name: "not-found",
    component: () => import("../components/AuthStatusPage.vue"),
    props: { statusCode: 404 },
  },
];

export default createRouter({
  history: createWebHashHistory(),
  routes,
});
