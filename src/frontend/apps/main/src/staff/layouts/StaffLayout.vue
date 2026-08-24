<script setup lang="ts">
// TEMPLATE-OWNED SHELL — do not add project data here.
// Menu items / routes / access codes live in src/frontend/apps/main/src/app-config/*;
// the brand label lives in theme/appTheme.ts; the logo + feedback prefix in app-config/branding.ts.
// See .ai/GLOBAL-RULES.md and .ai/FEATURE-app-shell-navigation.md.
import { ref, computed, nextTick, onMounted, onUnmounted, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useAuth } from "@/composables/auth/useAuth";
import { usePermissions } from "@/composables/access-control/usePermissions";
import { useTheme, PALETTES } from "@/composables/shell/useTheme";
import { useSignalR } from "@/composables/realtime/useSignalR";
import { useSwipe } from "@/composables/shell/useSwipe";
import {
  NieButton,
  NieLaunchpadProfileMenu,
  NieLoaderSymbol,
  NieModal,
  useToast,
} from "@nie/ui";
import FloatingFeedbackButton from "@/components/feedback/FloatingFeedbackButton.vue";
import NotificationPreferencesModal from "@/components/notifications/NotificationPreferencesModal.vue";
import notificationService from "@/services/notifications/notificationService";
import { setPushNotificationExternalUserId } from "@/services/notifications/oneSignalService";
import {
  ENotificationType,
  canShowDesktopNotification,
  filterNotificationsByPreferences,
  getNotificationPreferenceSummary,
  isNotificationPreferencesEnabled,
  loadNotificationPreferences,
  type UserNotificationPreferences,
} from "@/services/notifications/notificationPreferencesService";
import type { NavItem } from "@/composables/access-control/usePermissions";
import type { NotificationItem } from "@/types";
import { BRAND_LOGO, FEEDBACK_FUNCTION_PREFIX } from "@/app-config/branding";

const { currentUser, logout } = useAuth();
const { navItems, adminNavItems, userRoleLabel } = usePermissions();
const { mode, palette, setMode, setPalette, brandLabel } = useTheme();
const toast = useToast();
const route = useRoute();
const router = useRouter();
const {
  start: startSignalR,
  stop: stopSignalR,
  on: onSignalR,
  connected: signalRConnected,
} = useSignalR();

const showNotificationsPopup = ref(false);
const showNotificationPreferencesModal = ref(false);
const sidebarCollapsed = ref(false);
const showMobileSidebar = ref(false);
const isMobileViewport = ref(false);
const profileMenuCloseSignal = ref(0);
const notifications = ref<NotificationItem[]>([]);
const unreadCount = ref(0);
const loadingNotifications = ref(false);
const notificationPreferences = ref<UserNotificationPreferences>(
  loadNotificationPreferences(currentUser.value?.userId),
);
const browserNotificationPermission = ref<
  NotificationPermission | "unsupported"
>("default");
let notificationRefreshTimer: number | null = null;
let notificationPreferencesOpenTimer: number | null = null;
const seenNotificationIds = new Set<number>();

// Swipe support for mobile sidebar
const mainContentRef = ref<HTMLElement>();
const mobileSidebarRef = ref<HTMLElement>();
const mobileMenuButtonRef = ref<HTMLButtonElement>();
const mobileSidebarCloseButtonRef = ref<HTMLButtonElement>();

function openMobileSidebar() {
  showMobileSidebar.value = true;
}

function closeMobileSidebar(restoreFocus = true) {
  showMobileSidebar.value = false;
  if (restoreFocus) {
    void nextTick(() => mobileMenuButtonRef.value?.focus());
  }
}

function handleMobileSidebarKeydown(event: KeyboardEvent) {
  if (event.key === "Escape") {
    event.preventDefault();
    closeMobileSidebar();
    return;
  }
  if (event.key !== "Tab") return;

  const focusable = [
    ...(mobileSidebarRef.value?.querySelectorAll<HTMLElement>(
      "button:not([disabled]), a[href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex='-1'])",
    ) ?? []),
  ].filter((element) => element.offsetParent !== null);
  const first = focusable[0];
  const last = focusable.at(-1);
  if (!first || !last) {
    event.preventDefault();
    mobileSidebarRef.value?.focus();
  } else if (event.shiftKey && document.activeElement === first) {
    event.preventDefault();
    last.focus();
  } else if (!event.shiftKey && document.activeElement === last) {
    event.preventDefault();
    first.focus();
  }
}

watch(showMobileSidebar, async (isOpen) => {
  if (!isOpen) return;
  await nextTick();
  mobileSidebarCloseButtonRef.value?.focus();
});

useSwipe(mainContentRef, {
  onSwipeRight: () => {
    openMobileSidebar();
  },
  onSwipeLeft: () => {
    closeMobileSidebar(false);
  },
});

const feedbackFunctionId = computed(
  () => `${FEEDBACK_FUNCTION_PREFIX}.${String(route.name ?? "page")}`,
);

const currentPageTitle = computed(
  () => (route.meta?.title as string | undefined) ?? "",
);

const feedbackQuestionText = computed(() =>
  currentPageTitle.value ? `Was ${currentPageTitle.value} useful?` : undefined,
);

const notificationPreferencesStatus = computed(() =>
  isNotificationPreferencesEnabled(
    notificationPreferences.value,
    browserNotificationPermission.value,
  )
    ? "Enabled"
    : "Disabled",
);

const notificationPreferencesSummary = computed(() =>
  getNotificationPreferenceSummary(notificationPreferences.value),
);

async function pollUnreadNotifications(showPopups: boolean) {
  try {
    const unreadNotifications = await notificationService.getUnread();
    const visibleUnreadNotifications = filterNotificationsByPreferences(
      unreadNotifications,
      notificationPreferences.value,
    );
    unreadCount.value = visibleUnreadNotifications.length;

    const newNotifications = visibleUnreadNotifications.filter(
      (notification) => !seenNotificationIds.has(notification.id),
    );

    visibleUnreadNotifications.forEach((notification) =>
      seenNotificationIds.add(notification.id),
    );

    if (showPopups) {
      newNotifications
        .slice()
        .reverse()
        .forEach(showNotificationAlert);
    }
  } catch {
    unreadCount.value = 0;
  }
}

async function loadNotifications() {
  loadingNotifications.value = true;
  try {
    notifications.value = filterNotificationsByPreferences(
      await notificationService.getAll(),
      notificationPreferences.value,
    );
    unreadCount.value = notifications.value.filter(
      (item) => !item.isRead,
    ).length;
  } catch {
    notifications.value = [];
  } finally {
    loadingNotifications.value = false;
  }
}

async function markAllAsRead() {
  try {
    await notificationService.markAllAsRead();
    notifications.value = notifications.value.map((item) => ({
      ...item,
      isRead: true,
    }));
    unreadCount.value = 0;
  } catch {
    // Ignore header polling failures.
  }
}

async function markAsRead(id: string) {
  const notification = notifications.value.find((item) => item.id === id);
  if (!notification || notification.isRead) return;

  try {
    await notificationService.markAsRead(id);
    notification.isRead = true;
    unreadCount.value = Math.max(0, unreadCount.value - 1);
  } catch {
    // Ignore header polling failures.
  }
}

async function toggleNotificationsPopup() {
  profileMenuCloseSignal.value += 1;
  showNotificationsPopup.value = !showNotificationsPopup.value;

  if (showNotificationsPopup.value) {
    syncNotificationPermission();
    await loadNotifications();
  }
}

function syncViewport() {
  isMobileViewport.value = window.innerWidth < 768;
}

function syncNotificationPermission() {
  if (typeof Notification === "undefined") {
    browserNotificationPermission.value = "unsupported";
    return;
  }

  browserNotificationPermission.value = Notification.permission;
}

function syncNotificationPreferences() {
  notificationPreferences.value = loadNotificationPreferences(
    currentUser.value?.userId,
  );
}

function syncPushUserMapping() {
  const userId = currentUser.value?.userId;
  if (userId !== undefined && userId !== null) {
    setPushNotificationExternalUserId(String(userId));
  }
}

async function openNotificationPreferences() {
  showNotificationsPopup.value = false;
  profileMenuCloseSignal.value += 1;

  if (notificationPreferencesOpenTimer !== null) {
    window.clearTimeout(notificationPreferencesOpenTimer);
    notificationPreferencesOpenTimer = null;
  }

  await nextTick();

  const openModal = () => {
    notificationPreferencesOpenTimer = null;
    showNotificationPreferencesModal.value = true;
  };

  if (isMobileViewport.value) {
    notificationPreferencesOpenTimer = window.setTimeout(openModal, 180);
    return;
  }

  openModal();
}

function handleProfileMenuOpenChange(isOpen: boolean) {
  if (isOpen) {
    showNotificationsPopup.value = false;
  }
}

function handleNotificationPreferencesSaved(
  preferences: UserNotificationPreferences,
) {
  notificationPreferences.value = preferences;
  syncNotificationPermission();
  notifications.value = filterNotificationsByPreferences(
    notifications.value,
    preferences,
  );
  unreadCount.value = notifications.value.filter((item) => !item.isRead).length;
}

async function handleNotificationClick(notif: NotificationItem) {
  await markAsRead(notif.id);
  showNotificationsPopup.value = false;
  const resolvedLink = resolveNotificationLink(notif);
  if (resolvedLink) {
    router.push(resolvedLink);
  }
}

function resolveNotificationLink(notif: NotificationItem): string | null {
  if (notif.sourceEntityType === "SupportTicket" && notif.sourceEntityId) {
    return `/support-tickets?ticketId=${notif.sourceEntityId}`;
  }

  if (
    notif.sourceEntityType === "AdmissionApplication" &&
    notif.sourceEntityId
  ) {
    return `/application/${notif.sourceEntityId}`;
  }

  return notif.link ?? null;
}

function notificationIcon(type: string): string {
  switch (type) {
    case ENotificationType.InterviewScheduled:
      return "event";
    case ENotificationType.SupportTicketCreated:
    case ENotificationType.SupportTicketReply:
      return "support_agent";
    case ENotificationType.AdmissionMessageReceived:
      return "mail";
    case ENotificationType.ApplicationSubmitted:
      return "assignment";
    case ENotificationType.ApprovalUpdate:
    case ENotificationType.ApprovalDecision:
    case ENotificationType.ApprovalReminder:
      return "approval";
    case ENotificationType.CatalogRefresh:
      return "inventory_2";
    case ENotificationType.VendorUpdate:
      return "storefront";
    default:
      return "notifications";
  }
}

function showNotificationAlert(notification: NotificationItem) {
  if (
    canShowDesktopNotification(
      notification,
      notificationPreferences.value,
      browserNotificationPermission.value,
    ) &&
    typeof Notification !== "undefined"
  ) {
    const desktopNotification = new Notification(notification.title, {
      body: notification.message,
      data: {
        id: notification.id,
      },
    });
    desktopNotification.onclick = () => {
      window.focus();
      void handleNotificationClick(notification);
    };
    return;
  }

  if (notificationPreferences.value.desktopAlerts) {
    toast.info(`${notification.title}: ${notification.message}`);
  }
}

function formatRelativeTime(date: string): string {
  const diffMs = Date.now() - new Date(date).getTime();
  const diffMinutes = Math.floor(diffMs / 60000);

  if (diffMinutes < 1) return "Just now";
  if (diffMinutes < 60) return `${diffMinutes}m ago`;

  const diffHours = Math.floor(diffMinutes / 60);
  if (diffHours < 24) return `${diffHours}h ago`;

  const diffDays = Math.floor(diffHours / 24);
  if (diffDays < 7) return `${diffDays}d ago`;

  return new Date(date).toLocaleDateString("en-SG", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

function handleClickOutside(e: MouseEvent) {
  const target = e.target as HTMLElement;
  if (
    !target.closest(".notif-popup-area") &&
    !target.closest(".notif-mobile-sheet")
  )
    showNotificationsPopup.value = false;
}

onMounted(async () => {
  document.addEventListener("click", handleClickOutside);
  window.addEventListener("resize", syncViewport);
  syncViewport();
  syncNotificationPermission();
  syncNotificationPreferences();
  syncPushUserMapping();
  void pollUnreadNotifications(false);

  // Start SignalR for real-time notifications
  await startSignalR();
  onSignalR("ReceiveNotification", (notif: unknown) => {
    const n = notif as NotificationItem;
    if (
      !filterNotificationsByPreferences([n], notificationPreferences.value)
        .length
    ) {
      return;
    }

    unreadCount.value++;
    showNotificationAlert(n);
  });

  // Fall back to polling when SignalR is not connected
  notificationRefreshTimer = window.setInterval(() => {
    if (!signalRConnected.value) {
      void pollUnreadNotifications(true);
    }
  }, 20000);
});

watch(
  () => currentUser.value?.userId,
  () => {
    syncNotificationPreferences();
    syncPushUserMapping();
  },
);

onUnmounted(() => {
  document.removeEventListener("click", handleClickOutside);
  window.removeEventListener("resize", syncViewport);
  if (notificationRefreshTimer !== null) {
    window.clearInterval(notificationRefreshTimer);
  }
  if (notificationPreferencesOpenTimer !== null) {
    window.clearTimeout(notificationPreferencesOpenTimer);
  }
  void stopSignalR();
});

function isActive(routeName: string): boolean {
  return route.name === routeName;
}

function isNavItemActive(
  item: Pick<NavItem, "route" | "activeRoutes">,
): boolean {
  const routeNames = item.activeRoutes?.length
    ? item.activeRoutes
    : [item.route];
  return routeNames.some((routeName) => isActive(routeName));
}

function navigate(routeName: string) {
  router.push({ name: routeName });
  closeMobileSidebar(false);
}
</script>

<template>
  <div
    ref="mainContentRef"
    class="flex h-screen w-full overflow-hidden bg-background-light"
  >
    <!-- Skip to main content (WCAG 2.4.1) -->
    <a
      href="#main-content"
      class="sr-only focus:not-sr-only focus:absolute focus:top-2 focus:left-2 focus:z-50 focus:bg-accent focus:text-on-brand focus:px-4 focus:py-2 focus:rounded-lg focus:text-sm focus:font-medium"
    >
      Skip to main content
    </a>

    <!-- Sidebar -->
    <aside
      class="hidden lg:flex flex-col border-r bg-white transition-all duration-300"
      :style="{
        borderColor: 'var(--color-border)',
        width: sidebarCollapsed ? '72px' : '260px',
      }"
      role="navigation"
      aria-label="Main navigation"
    >
      <!-- Logo -->
      <div
        class="flex flex-col items-center gap-2 px-3 py-5 border-b"
        style="border-color: var(--color-border)"
      >
        <img :src="BRAND_LOGO" alt="NIE" class="w-[90%] shrink-0" />
        <span
          v-if="!sidebarCollapsed"
          class="text-xl font-bold tracking-tight text-center"
          style="color: var(--color-text)"
          >{{ brandLabel }}</span
        >
      </div>

      <!-- Nav Items -->
      <nav
        class="flex-1 flex flex-col gap-1 p-3 mt-1 overflow-y-auto no-scrollbar"
        aria-label="Staff navigation"
      >
        <button
          v-for="item in navItems"
          :key="item.route"
          class="flex items-center gap-3 px-3 py-2.5 rounded-lg font-semibold transition-all w-full text-left text-navigation"
          :class="
            isNavItemActive(item)
              ? 'bg-sidebar-active font-bold'
              : 'hover:bg-secondary-50'
          "
          :style="{
            color: isNavItemActive(item)
              ? mode === 'dark'
                ? 'var(--theme-color-brand-200)'
                : 'var(--color-primary)'
              : 'var(--color-text-muted)',
          }"
          :title="sidebarCollapsed ? item.name : undefined"
          :aria-current="isNavItemActive(item) ? 'page' : undefined"
          :aria-label="item.name"
          @click="navigate(item.route)"
        >
          <span
            class="material-symbols-outlined text-page-title"
            :style="{
              fontVariationSettings: isNavItemActive(item)
                ? `'FILL' 1`
                : `'FILL' 0`,
            }"
            >{{ item.icon }}</span
          >
          <span v-if="!sidebarCollapsed">{{ item.name }}</span>
        </button>

        <div
          v-if="adminNavItems.length > 0"
          class="mt-4 border-t pt-4"
          style="border-color: var(--color-border)"
        >
          <p
            v-if="!sidebarCollapsed"
            class="px-3 pb-2 text-caption font-bold uppercase tracking-wide"
            style="color: var(--color-text-muted)"
          >
            Administration
          </p>

          <button
            v-for="item in adminNavItems"
            :key="item.route"
            class="flex items-center gap-3 px-3 py-2.5 rounded-lg font-semibold transition-all w-full text-left text-navigation"
            :class="
              isNavItemActive(item)
                ? 'bg-sidebar-active font-bold'
                : 'hover:bg-secondary-50'
            "
            :style="{
              color: isNavItemActive(item)
                ? mode === 'dark'
                  ? 'var(--theme-color-brand-200)'
                  : 'var(--color-primary)'
                : 'var(--color-text-muted)',
            }"
            :title="sidebarCollapsed ? item.name : undefined"
            :aria-current="isNavItemActive(item) ? 'page' : undefined"
            :aria-label="item.name"
            @click="navigate(item.route)"
          >
            <span
              class="material-symbols-outlined text-page-title"
              :style="{
                fontVariationSettings: isNavItemActive(item)
                  ? `'FILL' 1`
                  : `'FILL' 0`,
              }"
              >{{ item.icon }}</span
            >
            <span v-if="!sidebarCollapsed">{{ item.name }}</span>
          </button>
        </div>
      </nav>

      <!-- Collapse toggle + Logout -->
      <div class="p-3 border-t" style="border-color: var(--color-border)">
        <button
          class="flex items-center gap-3 px-3 py-2.5 rounded-lg w-full text-navigation font-semibold transition-colors hover:bg-secondary-50"
          style="color: var(--color-text-muted)"
          :aria-label="sidebarCollapsed ? 'Expand navigation' : 'Collapse navigation'"
          :title="sidebarCollapsed ? 'Expand navigation' : undefined"
          @click="sidebarCollapsed = !sidebarCollapsed"
        >
          <span class="material-symbols-outlined text-page-title">{{
            sidebarCollapsed ? "chevron_right" : "chevron_left"
          }}</span>
          <span v-if="!sidebarCollapsed">Collapse</span>
        </button>
        <button
          class="flex items-center gap-3 px-3 py-2.5 rounded-lg w-full text-navigation font-semibold transition-colors hover:bg-danger-50 mt-1"
          :style="{
            color:
              mode === 'dark'
                ? 'var(--theme-color-danger-300)'
                : 'var(--theme-color-danger-700)',
          }"
          aria-label="Sign out"
          @click="logout"
        >
          <span class="material-symbols-outlined text-page-title">logout</span>
          <span v-if="!sidebarCollapsed">Sign Out</span>
        </button>
      </div>
    </aside>

    <!-- Mobile sidebar overlay -->
    <div
      v-if="showMobileSidebar"
      class="lg:hidden fixed inset-0 z-50 bg-black/30"
      @click="closeMobileSidebar()"
    >
      <div
        ref="mobileSidebarRef"
        class="absolute left-0 top-0 h-full bg-white p-4 shadow-[var(--theme-shadow-float)]"
        style="background-color: var(--color-surface); width: 260px"
        role="dialog"
        aria-modal="true"
        aria-label="Mobile navigation"
        tabindex="-1"
        @click.stop
        @keydown="handleMobileSidebarKeydown"
      >
        <div class="mb-6 flex items-start gap-2 px-2">
          <div class="flex min-w-0 flex-1 flex-col items-center gap-2">
            <img :src="BRAND_LOGO" alt="NIE" class="w-[90%]" />
            <span
              class="text-center text-xl font-bold"
              style="color: var(--color-text)"
              >{{ brandLabel }}</span
            >
          </div>
          <button
            ref="mobileSidebarCloseButtonRef"
            type="button"
            class="flex size-11 shrink-0 items-center justify-center rounded-[var(--theme-radius-control)] hover:bg-secondary-100"
            aria-label="Close navigation menu"
            @click="closeMobileSidebar()"
          >
            <span class="material-symbols-outlined" aria-hidden="true">close</span>
          </button>
        </div>
        <nav class="flex flex-col gap-1" aria-label="Mobile staff navigation">
          <button
            v-for="item in navItems"
            :key="item.route"
            class="flex items-center gap-3 px-3 py-2.5 rounded-lg font-semibold transition-all w-full text-left text-navigation"
            :class="
              isNavItemActive(item)
                ? 'bg-sidebar-active font-bold'
                : 'hover:bg-secondary-50'
            "
            :style="{
              color: isNavItemActive(item)
                ? mode === 'dark'
                  ? 'var(--theme-color-brand-200)'
                  : 'var(--color-primary)'
                : 'var(--color-text-muted)',
            }"
            :aria-current="isNavItemActive(item) ? 'page' : undefined"
            :aria-label="item.name"
            @click="navigate(item.route)"
          >
            <span class="material-symbols-outlined text-page-title">{{
              item.icon
            }}</span>
            {{ item.name }}
          </button>

          <div
            v-if="adminNavItems.length > 0"
            class="mt-4 border-t pt-4"
            style="border-color: var(--color-border)"
          >
            <p
              class="px-3 pb-2 text-caption font-bold uppercase tracking-wide"
              style="color: var(--color-text-muted)"
            >
              Administration
            </p>

            <button
              v-for="item in adminNavItems"
              :key="item.route"
              class="flex items-center gap-3 px-3 py-2.5 rounded-lg font-semibold transition-all w-full text-left text-navigation"
              :class="
                isNavItemActive(item)
                  ? 'bg-sidebar-active font-bold'
                  : 'hover:bg-secondary-50'
              "
              :style="{
                color: isNavItemActive(item)
                  ? mode === 'dark'
                    ? 'var(--theme-color-brand-200)'
                    : 'var(--color-primary)'
                  : 'var(--color-text-muted)',
              }"
              :aria-current="isNavItemActive(item) ? 'page' : undefined"
              :aria-label="item.name"
              @click="navigate(item.route)"
            >
              <span class="material-symbols-outlined text-page-title">{{
                item.icon
              }}</span>
              {{ item.name }}
            </button>
          </div>
        </nav>
        <div
          class="mt-4 pt-3 border-t"
          style="border-color: var(--color-border)"
        >
          <button
            class="flex items-center gap-3 px-3 py-2.5 rounded-lg w-full text-navigation font-semibold transition-colors hover:bg-danger-50"
            :style="{
              color:
                mode === 'dark'
                  ? 'var(--theme-color-danger-300)'
                  : 'var(--theme-color-danger-700)',
            }"
            aria-label="Sign out"
            @click="logout"
          >
            <span class="material-symbols-outlined text-page-title">logout</span>
            Sign Out
          </button>
        </div>
      </div>
    </div>

    <!-- Main Column -->
    <div class="flex-1 flex flex-col h-full overflow-hidden">
      <!-- Top Bar -->
      <header
        class="flex flex-wrap items-center justify-between gap-y-1 border-b bg-white px-4 py-3 md:px-8"
        style="
          border-color: var(--color-border);
          background-color: var(--color-surface);
        "
        role="banner"
        aria-label="Staff portal header"
      >
        <div
          class="flex w-full min-w-0 flex-none basis-full items-center gap-3 sm:w-auto sm:flex-1 sm:basis-auto"
        >
          <button
            ref="mobileMenuButtonRef"
            class="lg:hidden flex size-11 items-center justify-center rounded-lg hover:bg-secondary-100"
            style="color: var(--color-text-muted)"
            aria-label="Open navigation menu"
            @click="openMobileSidebar"
          >
            <span class="material-symbols-outlined">menu</span>
          </button>
          <div class="hidden sm:flex lg:hidden items-center gap-2">
            <img :src="BRAND_LOGO" alt="NIE" class="h-7" />
          </div>
          <div class="flex min-w-0 flex-1 items-center gap-2">
            <h1
              v-if="currentPageTitle"
              class="staff-page-title min-w-0 break-words text-lg font-bold leading-tight sm:text-xl md:text-2xl"
              style="color: var(--color-text)"
            >
              {{ currentPageTitle }}
            </h1>
            <div class="staff-page-feedback shrink-0">
              <FloatingFeedbackButton
                :function-id="feedbackFunctionId"
                :question-text="feedbackQuestionText"
              />
            </div>
          </div>
        </div>
        <div class="ml-auto flex items-center gap-2 md:gap-4">
          <!-- Notifications -->
          <div class="relative notif-popup-area">
            <button
              class="relative flex size-11 items-center justify-center rounded-full transition-colors hover:bg-secondary-100"
              style="color: var(--color-text-muted)"
              :aria-label="`Notifications${unreadCount > 0 ? `, ${unreadCount} unread` : ''}`"
              @click.stop="toggleNotificationsPopup"
            >
              <span class="material-symbols-outlined text-page-title">
                notifications
              </span>
              <span
                v-if="unreadCount > 0"
                class="absolute top-1 right-1 flex h-2 w-2"
              >
                <span
                  class="absolute inline-flex h-full w-full animate-ping rounded-full opacity-75"
                  style="background-color: var(--color-primary)"
                ></span>
                <span
                  class="relative inline-flex h-2 w-2 rounded-full"
                  style="background-color: var(--color-primary)"
                ></span>
              </span>
            </button>

            <div
              v-if="showNotificationsPopup && !isMobileViewport"
              class="absolute right-0 top-full z-50 mt-3 w-[min(30rem,calc(100vw-1rem))] rounded-3xl border p-2 shadow-[var(--theme-shadow-float)] backdrop-blur-xl"
              style="
                background: color-mix(
                  in srgb,
                  var(--color-surface) 95%,
                  transparent
                );
                border-color: var(--color-border);
              "
            >
              <div
                class="flex max-h-[75vh] flex-col overflow-hidden rounded-xl"
              >
                <div class="flex items-center justify-between gap-3 px-4 py-3">
                  <div class="flex min-w-0 items-center gap-2">
                    <h3
                      class="truncate text-sm font-bold"
                      style="color: var(--color-text)"
                    >
                      Notifications
                    </h3>
                    <span
                      class="inline-flex items-center rounded-full px-2.5 py-1 text-caption font-bold"
                      style="
                        background-color: var(--color-sidebar-active);
                        color: var(--color-primary);
                      "
                    >
                      {{ unreadCount }} unread
                    </span>
                  </div>

                  <button
                    class="shrink-0 text-xs font-semibold transition-opacity hover:opacity-80 disabled:cursor-not-allowed disabled:opacity-40"
                    :disabled="unreadCount === 0"
                    style="color: var(--color-primary)"
                    @click="markAllAsRead"
                  >
                    Mark all as read
                  </button>
                </div>

                <div class="min-h-0 space-y-2 overflow-y-auto px-1 pb-1">
                  <button
                    v-for="notif in notifications"
                    :key="notif.id"
                    class="w-full rounded-xl border px-4 py-3 text-left transition-colors hover:-translate-y-0.5 hover:bg-secondary-50"
                    :style="{
                      borderColor: 'var(--color-border)',
                      backgroundColor: notif.isRead
                        ? 'var(--color-surface)'
                        : 'var(--color-accent-light)',
                    }"
                    @click="handleNotificationClick(notif)"
                  >
                    <div class="flex items-start gap-3">
                      <span
                        class="mt-2 size-2 shrink-0 rounded-full"
                        :style="{
                          backgroundColor: notif.isRead
                            ? 'var(--color-border)'
                            : 'var(--color-primary)',
                        }"
                      ></span>
                      <div
                        class="flex size-9 shrink-0 items-center justify-center rounded-full"
                        :style="{
                          backgroundColor: notif.isRead
                            ? 'var(--color-surface-muted, var(--theme-color-surface-subtle))'
                            : 'var(--color-accent-light)',
                          color: notif.isRead
                            ? 'var(--color-text-muted)'
                            : 'var(--color-primary)',
                        }"
                      >
                        <span class="material-symbols-outlined text-card-title">
                          {{ notificationIcon(notif.type) }}
                        </span>
                      </div>
                      <div class="min-w-0 flex-1">
                        <p
                          class="text-sm font-semibold"
                          style="color: var(--color-text)"
                        >
                          {{ notif.title }}
                        </p>
                        <p
                          class="mt-1 whitespace-normal text-xs leading-5"
                          style="color: var(--color-text-muted)"
                        >
                          {{ notif.message }}
                        </p>
                        <div class="mt-2 flex items-center gap-2 text-caption">
                          <span style="color: var(--color-text-muted)">
                            {{ formatRelativeTime(notif.createdOn) }}
                          </span>
                          <span
                            class="rounded-full px-2 py-0.5 font-semibold"
                            :style="{
                              backgroundColor: notif.isRead
                                ? 'var(--color-surface-muted, var(--theme-color-surface-subtle))'
                                : 'var(--color-sidebar-active)',
                              color: notif.isRead
                                ? 'var(--color-text-muted)'
                                : 'var(--color-primary)',
                            }"
                          >
                            {{ notif.isRead ? "Read" : "New" }}
                          </span>
                        </div>
                      </div>
                    </div>
                  </button>

                  <div
                    v-if="loadingNotifications"
                    class="flex items-center justify-center gap-2 py-10 text-sm text-secondary-500 dark:text-secondary-300"
                  >
                    <NieLoaderSymbol size="sm" label="Loading notifications" />
                    <span>Loading notifications...</span>
                  </div>
                  <div
                    v-else-if="notifications.length === 0"
                    class="rounded-xl border px-5 py-10 text-center text-sm"
                    style="
                      border-color: var(--color-border);
                      color: var(--color-text-muted);
                    "
                  >
                    <p
                      class="text-sm font-semibold"
                      style="color: var(--color-text)"
                    >
                      You're all caught up.
                    </p>
                  </div>
                </div>

                <div
                  class="border-t px-4 py-3"
                  style="border-color: var(--color-border)"
                >
                  <button
                    class="text-xs font-semibold transition-opacity hover:opacity-80"
                    style="color: var(--color-primary)"
                    @click="openNotificationPreferences"
                  >
                    Notification settings
                  </button>
                </div>
              </div>
            </div>

            <NieModal
              v-if="isMobileViewport"
              v-model="showNotificationsPopup"
              title="Notifications"
              size="md"
              placement="mobile-sheet"
            >
                  <div
                    class="notif-mobile-sheet flex min-h-0 flex-col"
                  >
                    <div class="mb-3 flex items-center justify-end">
                      <span
                        class="inline-flex items-center rounded-full px-2.5 py-1 text-caption font-bold"
                        style="background-color: var(--color-sidebar-active); color: var(--color-primary)"
                      >
                        {{ unreadCount }} unread
                      </span>
                    </div>

                    <div
                      class="min-h-0 flex-1 space-y-2 overflow-y-auto px-3 py-3"
                    >
                      <button
                        v-for="notif in notifications"
                        :key="`mobile-${notif.id}`"
                        class="w-full rounded-xl border px-4 py-3 text-left transition-colors"
                        :style="{
                          borderColor: 'var(--color-border)',
                          backgroundColor: notif.isRead
                            ? 'var(--color-surface)'
                            : 'var(--color-accent-light)',
                        }"
                        @click="handleNotificationClick(notif)"
                      >
                        <div class="flex items-start gap-3">
                          <span
                            class="mt-2 size-2 shrink-0 rounded-full"
                            :style="{
                              backgroundColor: notif.isRead
                                ? 'var(--color-border)'
                                : 'var(--color-primary)',
                            }"
                          ></span>
                          <div
                            class="flex size-9 shrink-0 items-center justify-center rounded-full"
                            :style="{
                              backgroundColor: notif.isRead
                                ? 'var(--color-surface-muted, var(--theme-color-surface-subtle))'
                                : 'var(--color-accent-light)',
                              color: notif.isRead
                                ? 'var(--color-text-muted)'
                                : 'var(--color-primary)',
                            }"
                          >
                            <span class="material-symbols-outlined text-card-title">
                              {{ notificationIcon(notif.type) }}
                            </span>
                          </div>
                          <div class="min-w-0 flex-1">
                            <p
                              class="text-sm font-semibold"
                              style="color: var(--color-text)"
                            >
                              {{ notif.title }}
                            </p>
                            <p
                              class="mt-1 whitespace-normal text-xs leading-5"
                              style="color: var(--color-text-muted)"
                            >
                              {{ notif.message }}
                            </p>
                            <div
                              class="mt-2 flex items-center gap-2 text-caption"
                            >
                              <span style="color: var(--color-text-muted)">
                                {{ formatRelativeTime(notif.createdOn) }}
                              </span>
                              <span
                                class="rounded-full px-2 py-0.5 font-semibold"
                                :style="{
                                  backgroundColor: notif.isRead
                                    ? 'var(--color-surface-muted, var(--theme-color-surface-subtle))'
                                    : 'var(--color-sidebar-active)',
                                  color: notif.isRead
                                    ? 'var(--color-text-muted)'
                                    : 'var(--color-primary)',
                                }"
                              >
                                {{ notif.isRead ? "Read" : "New" }}
                              </span>
                            </div>
                          </div>
                        </div>
                      </button>

                      <div
                        v-if="loadingNotifications"
                        class="flex items-center justify-center gap-2 py-10 text-sm text-secondary-500 dark:text-secondary-300"
                      >
                        <NieLoaderSymbol size="sm" label="Loading notifications" />
                        <span>Loading notifications...</span>
                      </div>
                      <div
                        v-else-if="notifications.length === 0"
                        class="rounded-xl border px-5 py-10 text-center text-sm"
                        style="
                          border-color: var(--color-border);
                          color: var(--color-text-muted);
                        "
                      >
                        <p
                          class="text-sm font-semibold"
                          style="color: var(--color-text)"
                        >
                          You're all caught up.
                        </p>
                      </div>
                    </div>
                  </div>

              <template #footer>
                    <div class="flex flex-wrap items-center justify-between gap-3">
                      <NieButton
                        size="sm"
                        variant="ghost"
                        :disabled="unreadCount === 0"
                        @click="markAllAsRead"
                      >
                        Mark all as read
                      </NieButton>
                      <NieButton
                        size="sm"
                        variant="ghost"
                        @click="openNotificationPreferences"
                      >
                        Notification settings
                      </NieButton>
                    </div>
              </template>
            </NieModal>
          </div>

          <div
            class="flex items-center gap-3 border-l pl-2 md:pl-4"
            style="border-color: var(--color-border)"
          >
            <NieLaunchpadProfileMenu
              :user-name="currentUser?.fullName || 'Staff User'"
              :user-email="currentUser?.email || ''"
              :user-role="userRoleLabel"
              :user-last-login="currentUser?.lastLoginAt ?? null"
              :mode="mode"
              :palette="palette"
              :palettes="PALETTES"
              :close-signal="profileMenuCloseSignal"
              :notification-preferences-status="notificationPreferencesStatus"
              :notification-preferences-summary="notificationPreferencesSummary"
              @open-change="handleProfileMenuOpenChange"
              @open-notification-preferences="openNotificationPreferences"
              @set-mode="setMode"
              @set-palette="setPalette"
              @logout="logout"
            />
          </div>
        </div>
      </header>

      <!-- Content -->
      <main
        id="main-content"
        class="flex-1 overflow-y-auto"
        style="background-color: var(--color-bg-light)"
        role="main"
        aria-label="Main content"
      >
        <div class="p-4 md:p-8 min-h-full flex flex-col">
          <RouterView />
        </div>
      </main>
    </div>

    <NotificationPreferencesModal
      v-model="showNotificationPreferencesModal"
      @saved="handleNotificationPreferencesSaved"
    />

  </div>
</template>

