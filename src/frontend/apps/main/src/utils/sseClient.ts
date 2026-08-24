/**
 * Server-Sent Events (SSE) client.
 *
 * Parses framed SSE messages from a fetch Response.body stream and dispatches
 * typed events (message / done / error / tool_start / tool_result / session / metadata / stop).
 *
 * Wire format produced by the backend:
 *   event: message
 *   data: token text
 *
 *   event: tool_start
 *   data: {"toolName":"...","toolInput":"..."}
 *
 *   event: done
 *   data: {"inputTokens":12,"outputTokens":34}
 *
 * Authentication is via HttpOnly cookie (credentials: "include").
 */

import Cookie from "js-cookie";
import { FRONTEND_CONSTANTS } from "@nie/platform";

export interface SSEMessage {
  event: string;
  data: string;
}

export interface SSEClientOptions {
  onMessage?: (data: string) => void;
  onDone?: (metadata: unknown) => void;
  onError?: (error: string) => void;
  onToolStart?: (payload: { toolName?: string; toolInput?: string }) => void;
  onToolResult?: (payload: {
    toolName?: string;
    toolOutput?: string;
    sourceItems?: unknown[];
  }) => void;
  onSession?: (payload: unknown) => void;
  onMetadata?: (payload: {
    inputTokens?: number;
    outputTokens?: number;
  }) => void;
  onStop?: (payload: { stopReason?: string }) => void;
  headers?: Record<string, string>;
  signal?: AbortSignal;
}

function decodeEventText(value: string): string {
  return value.replace(/\\r/g, "\r").replace(/\\n/g, "\n");
}

function tryParseJson(text: string): unknown {
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

function getSessionHeaders(): Record<string, string> {
  const sessionId = Cookie.get(FRONTEND_CONSTANTS.cookies.session);
  return sessionId ? { "X-Session-Id": sessionId } : {};
}

export class SSEClient {
  /**
   * Stream from an SSE endpoint. POSTs `body` as JSON, then incrementally
   * parses the `text/event-stream` response.
   */
  static async stream(
    url: string,
    body: unknown,
    options: SSEClientOptions,
  ): Promise<void> {
    const { onError, headers = {}, signal } = options;

    const response = await fetch(url, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        ...getSessionHeaders(),
        ...headers,
      },
      body: JSON.stringify(body),
      credentials: "include",
      signal,
    });

    if (!response.ok) {
      const responseText = await response.text().catch(() => "");
      const decodedResponseText = decodeEventText(responseText).trim();
      const errMsg =
        response.status === 401 || response.status === 403
          ? "Session expired"
          : decodedResponseText || `HTTP error! status: ${response.status}`;
      onError?.(errMsg);
      throw new Error(errMsg);
    }

    const reader = response.body?.getReader();
    if (!reader) {
      throw new Error("Response body reader not available");
    }

    const decoder = new TextDecoder();
    let buffer = "";

    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });

        // SSE messages are framed by a blank line.
        const messages = buffer.split(/\r?\n\r?\n/);
        buffer = messages.pop() ?? "";

        for (const raw of messages) {
          SSEClient.dispatchMessage(raw, options);
        }
      }

      if (buffer.trim()) {
        SSEClient.dispatchMessage(buffer, options);
      }
    } catch (err) {
      const errorMessage =
        err instanceof Error ? err.message : "Streaming failed";
      onError?.(errorMessage);
      throw err;
    }
  }

  private static dispatchMessage(
    raw: string,
    options: SSEClientOptions,
  ): void {
    if (!raw.trim()) return;

    const parsed = SSEClient.parseSSEMessage(raw);
    if (!parsed) return;

    switch (parsed.event) {
      case "message":
      case "content":
        options.onMessage?.(decodeEventText(parsed.data));
        break;

      case "tool_start": {
        const data = tryParseJson(parsed.data) as {
          toolName?: string;
          toolInput?: string;
        };
        options.onToolStart?.(typeof data === "object" ? data : {});
        break;
      }

      case "tool_result": {
        const data = tryParseJson(parsed.data) as {
          toolName?: string;
          toolOutput?: string;
          sourceItems?: unknown[];
        };
        options.onToolResult?.(typeof data === "object" ? data : {});
        break;
      }

      case "session":
        options.onSession?.(tryParseJson(parsed.data));
        break;

      case "metadata": {
        const data = tryParseJson(parsed.data) as {
          inputTokens?: number;
          outputTokens?: number;
        };
        options.onMetadata?.(typeof data === "object" ? data : {});
        break;
      }

      case "stop": {
        const data = tryParseJson(parsed.data) as {
          stopReason?: string;
        };
        options.onStop?.(typeof data === "object" ? data : {});
        break;
      }

      case "done":
        options.onDone?.(tryParseJson(parsed.data));
        break;

      case "error": {
        const msg = decodeEventText(parsed.data);
        options.onError?.(msg);
        throw new Error(msg);
      }

      default:
        // Unknown event; ignored.
        break;
    }
  }

  private static parseSSEMessage(message: string): SSEMessage | null {
    const eventMatch = message.match(/^event:\s*(.+)$/m);
    const dataLines = message
      .split(/\r?\n/)
      .filter((line) => line.startsWith("data:"))
      .map((line) => {
        const value = line.slice(5);
        return value.startsWith(" ") ? value.slice(1) : value;
      });

    if (!eventMatch || dataLines.length === 0) return null;

    return {
      event: eventMatch[1].trim(),
      data: dataLines.join("\n"),
    };
  }
}
