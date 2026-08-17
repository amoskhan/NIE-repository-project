import { ref } from "vue";
import { FRONTEND_CONSTANTS } from "@apptemplate/shared";

let apiActivityTimeout: ReturnType<typeof setTimeout> | null = null;
export const showPopup = ref(false);
let currentTimeoutId = 0;

export const resetSessionTimer = () => {
  if (apiActivityTimeout) {
    clearTimeout(apiActivityTimeout);
  }
  const timeoutId = ++currentTimeoutId;
  apiActivityTimeout = setTimeout(
    () => {
      if (timeoutId === currentTimeoutId) {
        showPopup.value = true;
      }
    },
    FRONTEND_CONSTANTS.session.timeoutMinutes * 60 * 1000,
  );
};

export const getShowPopup = () => showPopup;
