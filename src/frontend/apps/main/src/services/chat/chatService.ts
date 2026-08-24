import api from "../core/api";
import { SSEClient } from "@/utils/sseClient";

export interface Conversation {
  id: string;
  title: string;
  source: string;
  lastMessageAt: string;
  messageCount: number;
}

export interface ChatSourceItem {
  title?: string;
  url?: string;
  excerpt?: string;
  sourceType?: string;
  sourceId?: string;
}

export interface ChatToolActivity {
  tool: string;
  detail?: string;
}

export interface ChatMessage {
  id: string;
  role: "user" | "assistant" | "system";
  content: string;
  createdAt: string;
  conversationId: string;
  tokenCount?: number;
  feedbackType?: "thumbs_up" | "thumbs_down" | null;
  sourceItems?: ChatSourceItem[];
  toolActivity?: ChatToolActivity[];
}

export interface ChatQuotaStatus {
  conversationsToday: number;
  conversationsDailyLimit: number;
  tokensToday: number;
  tokensDailyLimit: number;
  retentionDays: number;
  warnings: string[];
  conversationsExceeded: boolean;
  tokensExceeded: boolean;
}

export interface ChatSearchResponse {
  query: string;
  results: unknown[];
  message: string;
}

export interface StreamHandlers {
  onChunk: (text: string) => void;
  onToolStart?: (payload: { toolName?: string; toolInput?: string }) => void;
  onToolResult?: (payload: {
    toolName?: string;
    toolOutput?: string;
    sourceItems?: ChatSourceItem[];
  }) => void;
  onSession?: (payload: unknown) => void;
  onDone?: (metadata: unknown) => void;
  onError?: (error: string) => void;
  signal?: AbortSignal;
}

const chatService = {
  async getConversations(source?: string): Promise<Conversation[]> {
    const params = source ? `?source=${encodeURIComponent(source)}` : "";
    const res = await api.get(`/api/Chat/conversations${params}`);
    return res.data;
  },

  async getMessages(conversationId: string): Promise<ChatMessage[]> {
    const res = await api.get(
      `/api/Chat/conversations/${conversationId}/messages`,
    );
    return res.data;
  },

  async createConversation(
    source: string,
    title: string,
  ): Promise<Conversation> {
    const res = await api.post("/api/Chat/conversations", { title, source });
    return res.data;
  },

  async deleteConversation(id: string): Promise<void> {
    await api.delete(`/api/Chat/conversations/${id}`);
  },

  async renameConversation(id: string, title: string): Promise<void> {
    await api.post(`/api/Chat/conversations/${id}/rename`, { title });
  },

  async submitFeedback(
    messageId: string,
    type: "thumbs_up" | "thumbs_down",
    comment?: string,
  ): Promise<void> {
    await api.post(`/api/Chat/messages/${messageId}/feedback`, {
      type,
      comment,
    });
  },

  async getQuota(): Promise<ChatQuotaStatus> {
    const res = await api.get<ChatQuotaStatus>("/api/Chat/quota");
    return res.data;
  },

  async sendMessage(
    conversationId: string,
    content: string,
  ): Promise<ChatMessage> {
    const res = await api.post(
      `/api/Chat/conversations/${conversationId}/send`,
      { content },
    );
    return res.data;
  },

  /**
   * Stream an assistant reply using named SSE events.
   * Backend emits: message / tool_start / tool_result / metadata / stop / done / error.
   */
  async streamMessage(
    conversationId: string,
    content: string,
    handlers: StreamHandlers,
  ): Promise<void> {
    const baseUrl = api.defaults.baseURL || "";

    await SSEClient.stream(
      `${baseUrl}/api/Chat/conversations/${conversationId}/send`,
      { content },
      {
        signal: handlers.signal,
        onMessage: handlers.onChunk,
        onToolStart: handlers.onToolStart,
        onToolResult: handlers.onToolResult,
        onSession: handlers.onSession,
        onDone: handlers.onDone,
        onError: handlers.onError,
      },
    );
  },

  async search(query: string): Promise<ChatSearchResponse> {
    const res = await api.get<ChatSearchResponse>(
      `/api/Chat/search?q=${encodeURIComponent(query)}`,
    );
    return res.data;
  },
};

export default chatService;
