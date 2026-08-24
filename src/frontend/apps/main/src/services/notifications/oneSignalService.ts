/**
 * Provider contract for web push notifications.
 *
 * OneSignal is the default implementation today, but the rest of the app uses
 * PushNotificationProvider so a future Firebase/Graph/etc. provider can be
 * swapped in without changing settings UI or auth cleanup code.
 */

import { FRONTEND_CONSTANTS } from "@nie/platform";

export type PushNotificationPermission =
  | NotificationPermission
  | "unsupported";

export interface PushSubscriptionState {
  providerName: string;
  supported: boolean;
  permission: PushNotificationPermission;
  optedIn: boolean;
  subscriptionId: string | null;
  token: string | null;
}

export interface PushNotificationProvider {
  providerName: string;
  isEnabled(): boolean;
  init(): void;
  requestPermission(): Promise<PushNotificationPermission>;
  setExternalUserId(userId: string): void;
  removeExternalUserId(): void;
  setSubscribed(enabled: boolean): Promise<PushSubscriptionState>;
  getSubscriptionState(): Promise<PushSubscriptionState>;
}

declare global {
  interface Window {
    OneSignalDeferred?: Array<(oneSignal: OneSignalInstance) => void>;
  }
}

interface OneSignalPushSubscription {
  id: string | null;
  token: string | null;
  optedIn: boolean;
  optIn(): Promise<void>;
  optOut(): Promise<void>;
}

interface OneSignalUser {
  PushSubscription: OneSignalPushSubscription;
}

interface OneSignalNotifications {
  permission: boolean;
  isPushSupported(): boolean;
  requestPermission(): Promise<boolean | void>;
}

interface OneSignalInstance {
  init(config: { appId: string; allowLocalhostAsSecureOrigin?: boolean }):
    | void
    | Promise<void>;
  login(externalId: string): Promise<void>;
  logout(): Promise<void>;
  Notifications: OneSignalNotifications;
  User: OneSignalUser;
}

let initialized = false;

function getBrowserPermission(): PushNotificationPermission {
  if (typeof Notification === "undefined") {
    return "unsupported";
  }

  return Notification.permission;
}

function isBrowserPushSupported(): boolean {
  return typeof window !== "undefined" && getBrowserPermission() !== "unsupported";
}

function createSubscriptionState(
  providerName: string,
  permission: PushNotificationPermission,
  optedIn: boolean,
  subscriptionId: string | null = null,
  token: string | null = null,
): PushSubscriptionState {
  return {
    providerName,
    supported: permission !== "unsupported",
    permission,
    optedIn,
    subscriptionId,
    token,
  };
}

class BrowserPushNotificationProvider implements PushNotificationProvider {
  providerName = "browser";

  isEnabled(): boolean {
    return isBrowserPushSupported();
  }

  init(): void {
    // Native browser notifications do not need SDK bootstrapping.
  }

  async requestPermission(): Promise<PushNotificationPermission> {
    if (!this.isEnabled()) {
      return "unsupported";
    }

    return await Notification.requestPermission();
  }

  setExternalUserId(): void {
    // Browser notifications have no external user mapping.
  }

  removeExternalUserId(): void {
    // Browser notifications have no external user mapping.
  }

  async setSubscribed(enabled: boolean): Promise<PushSubscriptionState> {
    const permission = getBrowserPermission();
    return createSubscriptionState(
      this.providerName,
      permission,
      enabled && permission === "granted",
    );
  }

  async getSubscriptionState(): Promise<PushSubscriptionState> {
    const permission = getBrowserPermission();
    return createSubscriptionState(
      this.providerName,
      permission,
      permission === "granted",
    );
  }
}

class OneSignalPushNotificationProvider implements PushNotificationProvider {
  providerName = "onesignal";

  isEnabled(): boolean {
    return (
      typeof window !== "undefined" &&
      FRONTEND_CONSTANTS.oneSignal.enabled &&
      Boolean(FRONTEND_CONSTANTS.oneSignal.appId)
    );
  }

  init(): void {
    const appId = FRONTEND_CONSTANTS.oneSignal.appId;
    if (!this.isEnabled() || !appId || initialized) {
      return;
    }

    initialized = true;
    window.OneSignalDeferred = window.OneSignalDeferred || [];
    window.OneSignalDeferred.push(async (oneSignal) => {
      await oneSignal.init({
        appId,
        allowLocalhostAsSecureOrigin:
          FRONTEND_CONSTANTS.oneSignal.allowLocalhostAsSecureOrigin,
      });
    });

    if (document.querySelector('script[data-provider="onesignal"]')) {
      return;
    }

    const script = document.createElement("script");
    script.src = "https://cdn.onesignal.com/sdks/web/v16/OneSignalSDK.page.js";
    script.defer = true;
    script.dataset.provider = "onesignal";
    document.head.appendChild(script);
  }

  async requestPermission(): Promise<PushNotificationPermission> {
    return await this.enqueue(
      async (oneSignal) => {
        if (!oneSignal.Notifications.isPushSupported()) {
          return "unsupported";
        }

        await oneSignal.Notifications.requestPermission();
        return this.resolvePermission(oneSignal);
      },
      getBrowserPermission(),
    );
  }

  setExternalUserId(userId: string): void {
    if (!this.isEnabled()) {
      return;
    }

    this.init();
    window.OneSignalDeferred = window.OneSignalDeferred || [];
    window.OneSignalDeferred.push((oneSignal) => {
      void oneSignal.login(userId);
    });
  }

  removeExternalUserId(): void {
    if (!this.isEnabled()) {
      return;
    }

    this.init();
    window.OneSignalDeferred = window.OneSignalDeferred || [];
    window.OneSignalDeferred.push((oneSignal) => {
      void oneSignal.logout();
    });
  }

  async setSubscribed(enabled: boolean): Promise<PushSubscriptionState> {
    return await this.enqueue(
      async (oneSignal) => {
        if (!oneSignal.Notifications.isPushSupported()) {
          return createSubscriptionState(
            this.providerName,
            "unsupported",
            false,
          );
        }

        if (enabled) {
          await oneSignal.User.PushSubscription.optIn();
        } else {
          await oneSignal.User.PushSubscription.optOut();
        }

        return this.toSubscriptionState(oneSignal);
      },
      createSubscriptionState(
        this.providerName,
        getBrowserPermission(),
        false,
      ),
    );
  }

  async getSubscriptionState(): Promise<PushSubscriptionState> {
    return await this.enqueue(
      async (oneSignal) => this.toSubscriptionState(oneSignal),
      createSubscriptionState(
        this.providerName,
        getBrowserPermission(),
        false,
      ),
    );
  }

  private async enqueue<T>(
    callback: (oneSignal: OneSignalInstance) => Promise<T> | T,
    fallback: T,
  ): Promise<T> {
    if (!this.isEnabled()) {
      return fallback;
    }

    this.init();
    window.OneSignalDeferred = window.OneSignalDeferred || [];

    return await new Promise<T>((resolve) => {
      window.OneSignalDeferred?.push(async (oneSignal) => {
        try {
          resolve(await callback(oneSignal));
        } catch {
          resolve(fallback);
        }
      });
    });
  }

  private resolvePermission(
    oneSignal: OneSignalInstance,
  ): PushNotificationPermission {
    if (!oneSignal.Notifications.isPushSupported()) {
      return "unsupported";
    }

    if (oneSignal.Notifications.permission) {
      return "granted";
    }

    return getBrowserPermission();
  }

  private toSubscriptionState(
    oneSignal: OneSignalInstance,
  ): PushSubscriptionState {
    const subscription = oneSignal.User.PushSubscription;
    return createSubscriptionState(
      this.providerName,
      this.resolvePermission(oneSignal),
      Boolean(subscription.optedIn),
      subscription.id,
      subscription.token,
    );
  }
}

export const oneSignalPushNotificationProvider =
  new OneSignalPushNotificationProvider();

const browserPushNotificationProvider = new BrowserPushNotificationProvider();

let activePushNotificationProvider: PushNotificationProvider =
  oneSignalPushNotificationProvider.isEnabled()
    ? oneSignalPushNotificationProvider
    : browserPushNotificationProvider;

export function setPushNotificationProvider(
  provider: PushNotificationProvider,
): void {
  activePushNotificationProvider = provider;
}

export function getPushNotificationProvider(): PushNotificationProvider {
  return activePushNotificationProvider;
}

export function initPushNotifications(): void {
  getPushNotificationProvider().init();
}

export async function requestPushNotificationPermission(): Promise<PushNotificationPermission> {
  return await getPushNotificationProvider().requestPermission();
}

export async function setPushNotificationsSubscribed(
  enabled: boolean,
): Promise<PushSubscriptionState> {
  return await getPushNotificationProvider().setSubscribed(enabled);
}

export function setPushNotificationExternalUserId(userId: string): void {
  getPushNotificationProvider().setExternalUserId(userId);
}

export function removePushNotificationExternalUserId(): void {
  getPushNotificationProvider().removeExternalUserId();
}

export function initOneSignal(): void {
  oneSignalPushNotificationProvider.init();
}

export function setOneSignalExternalUserId(userId: string): void {
  oneSignalPushNotificationProvider.setExternalUserId(userId);
}

export function removeOneSignalExternalUserId(): void {
  oneSignalPushNotificationProvider.removeExternalUserId();
}
