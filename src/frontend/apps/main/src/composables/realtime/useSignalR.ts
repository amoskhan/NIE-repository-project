import { ref, onUnmounted } from "vue";
import {
  HubConnectionBuilder,
  HubConnection,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import Cookie from "js-cookie";
import { FRONTEND_CONSTANTS } from "@nie/platform";

const connection = ref<HubConnection | null>(null);
const connected = ref(false);

type EventHandler = (...args: unknown[]) => void;
const handlers = new Map<string, Set<EventHandler>>();

function getSessionId(): string {
  return Cookie.get(FRONTEND_CONSTANTS.cookies.session) ?? "";
}

function getUserId(): string {
  const json = Cookie.get(FRONTEND_CONSTANTS.cookies.user);
  if (!json) return "";
  try {
    return JSON.parse(json).id?.toString() ?? "";
  } catch {
    return "";
  }
}

function getUserRoles(): string {
  const json = Cookie.get(FRONTEND_CONSTANTS.cookies.user);
  if (!json) return "";
  try {
    const user = JSON.parse(json);
    return (user.roles ?? []).join(",");
  } catch {
    return "";
  }
}

export function useSignalR() {
  async function start() {
    if (connection.value?.state === HubConnectionState.Connected) return;

    const sessionId = getSessionId();
    const userId = getUserId();
    const roles = getUserRoles();

    if (!sessionId || !userId) return;

    const hubUrl = `${FRONTEND_CONSTANTS.backend.main}/hubs/notifications?userId=${encodeURIComponent(userId)}&roles=${encodeURIComponent(roles)}`;

    connection.value = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        headers: { "X-Session-Id": sessionId },
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build();

    connection.value.onreconnected(() => {
      connected.value = true;
    });

    connection.value.onclose(() => {
      connected.value = false;
    });

    // Re-register all handlers on the new connection
    for (const [event, eventHandlers] of handlers) {
      for (const handler of eventHandlers) {
        connection.value.on(event, handler);
      }
    }

    try {
      await connection.value.start();
      connected.value = true;
    } catch {
      connected.value = false;
    }
  }

  function on(event: string, handler: EventHandler) {
    if (!handlers.has(event)) handlers.set(event, new Set());
    handlers.get(event)!.add(handler);
    if (connection.value) {
      connection.value.on(event, handler);
    }
  }

  function off(event: string, handler: EventHandler) {
    handlers.get(event)?.delete(handler);
    if (connection.value) {
      connection.value.off(event, handler);
    }
  }

  async function stop() {
    if (connection.value) {
      try {
        await connection.value.stop();
      } catch {
        // ignore
      }
      connected.value = false;
    }
  }

  onUnmounted(() => {
    // Clean up only the handlers registered in this component's scope
    // The connection is shared — don't stop it here
  });

  return { connection, connected, start, stop, on, off };
}
