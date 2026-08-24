import axios from "axios";
import Cookie from "js-cookie";
import { FRONTEND_CONSTANTS, getCookieAttributes } from "@nie/platform";

const cookieSettings = getCookieAttributes();

const api = axios.create({
  baseURL: FRONTEND_CONSTANTS.backend.main,
  timeout: 30000,
});

api.interceptors.request.use((config) => {
  const sessionId = Cookie.get(FRONTEND_CONSTANTS.cookies.session);
  if (sessionId) {
    config.headers["X-Session-Id"] = sessionId;
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      Cookie.remove(FRONTEND_CONSTANTS.cookies.session, cookieSettings);
      Cookie.remove(FRONTEND_CONSTANTS.cookies.user, cookieSettings);
      window.location.href = FRONTEND_CONSTANTS.apps.auth;
    }
    return Promise.reject(error);
  },
);

export default api;
