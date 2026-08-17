<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from "vue";
import { useRoute } from "vue-router";
import { useToast } from "@apptemplate/ui";
import ChatSidebar from "@/components/chat/ChatSidebar.vue";
import ChatMessageBubble from "@/components/chat/ChatMessageBubble.vue";
import ChatInputBox from "@/components/chat/ChatInputBox.vue";
import chatService, {
  type Conversation,
  type ChatMessage,
  type ChatQuotaStatus,
  type ChatSourceItem,
  type ChatToolActivity,
} from "@/services/chatService";

const route = useRoute();
const toast = useToast();

const conversations = ref<Conversation[]>([]);
const messages = ref<ChatMessage[]>([]);
const currentConversation = ref<Conversation | null>(null);
const loadingConv = ref(false);
const loadingMsgs = ref(false);
const streaming = ref(false);
const streamingContent = ref("");
const streamingToolActivity = ref<ChatToolActivity[]>([]);
const streamingSources = ref<ChatSourceItem[]>([]);
const errorMessage = ref<string | null>(null);
const quotaStatus = ref<ChatQuotaStatus | null>(null);
const messagesContainer = ref<HTMLElement>();
let abortController: AbortController | null = null;

const source = computed(() => (route.params.source as string) || "procurement");

const lastAssistantId = computed(() => {
  for (let i = messages.value.length - 1; i >= 0; i--) {
    if (messages.value[i].role === "assistant") return messages.value[i].id;
  }
  return -1;
});

const quotaWarnings = computed(() => quotaStatus.value?.warnings ?? []);
const retentionDays = computed(() => quotaStatus.value?.retentionDays ?? null);

const suggestions = [
  { icon: "assignment", text: "What are my pending purchase orders?" },
  { icon: "storefront", text: "Show me vendor performance summary" },
  { icon: "payments", text: "What is the total spend this month?" },
  { icon: "account_tree", text: "Explain the approval workflow" },
];

function setConversations(nextConversations: Conversation[]) {
  conversations.value = nextConversations;

  if (!currentConversation.value) return;

  const latestCurrentConversation = nextConversations.find(
    (conv) => conv.id === currentConversation.value?.id,
  );
  if (latestCurrentConversation) {
    currentConversation.value = latestCurrentConversation;
  }
}

async function loadConversations() {
  loadingConv.value = true;
  try {
    setConversations(await chatService.getConversations(source.value));
  } catch {
    toast.error("Failed to load conversations");
  } finally {
    loadingConv.value = false;
  }
}

async function loadQuota() {
  try {
    quotaStatus.value = await chatService.getQuota();
  } catch {
    quotaStatus.value = null;
  }
}

async function selectConversation(conv: Conversation) {
  currentConversation.value = conv;
  loadingMsgs.value = true;
  errorMessage.value = null;
  try {
    messages.value = await chatService.getMessages(conv.id);
    await nextTick();
    scrollToBottom();
  } catch {
    toast.error("Failed to load messages");
  } finally {
    loadingMsgs.value = false;
  }
}

async function newConversation() {
  try {
    const conv = await chatService.createConversation(source.value, "New Chat");
    conversations.value.unshift(conv);
    await selectConversation(conv);
  } catch {
    toast.error("Failed to create conversation");
  }
}

async function deleteConversation(conv: Conversation) {
  try {
    await chatService.deleteConversation(conv.id);
    conversations.value = conversations.value.filter((c) => c.id !== conv.id);
    if (currentConversation.value?.id === conv.id) {
      currentConversation.value = null;
      messages.value = [];
    }
  } catch {
    toast.error("Failed to delete conversation");
  }
}

async function renameConversation(conv: Conversation, newTitle: string) {
  const title = newTitle.trim();
  if (!title) return;

  try {
    await chatService.renameConversation(conv.id, title);
    const renamedConversation = { ...conv, title };
    conversations.value = conversations.value.map((conversation) =>
      conversation.id === conv.id ? renamedConversation : conversation,
    );

    if (currentConversation.value?.id === conv.id) {
      currentConversation.value = {
        ...currentConversation.value,
        title,
      };
    }
  } catch {
    toast.error("Failed to rename");
  }
}

async function sendMessage(content: string) {
  if (!currentConversation.value) await newConversation();
  if (!currentConversation.value) return;

  errorMessage.value = null;
  const convId = currentConversation.value.id;

  messages.value.push({
    id: Date.now(),
    role: "user",
    content,
    createdAt: new Date().toISOString(),
    conversationId: convId,
  });

  streaming.value = true;
  streamingContent.value = "";
  streamingToolActivity.value = [];
  streamingSources.value = [];
  abortController = new AbortController();

  await nextTick();
  scrollToBottom();

  try {
    await chatService.streamMessage(convId, content, {
      signal: abortController.signal,
      onChunk: (chunk) => {
        streamingContent.value += chunk;
        scrollToBottom();
      },
      onToolStart: ({ toolName, toolInput }) => {
        if (toolName) {
          streamingToolActivity.value.push({
            tool: toolName,
            detail: toolInput,
          });
        }
      },
      onToolResult: ({ toolName, toolOutput, sourceItems }) => {
        if (toolName) {
          const existing = streamingToolActivity.value.find(
            (t) => t.tool === toolName && !t.detail?.includes("->"),
          );
          if (existing && toolOutput) {
            existing.detail =
              `${existing.detail ?? ""} -> ${toolOutput}`.trim();
          }
        }
        if (sourceItems && sourceItems.length > 0) {
          streamingSources.value.push(...(sourceItems as ChatSourceItem[]));
        }
      },
      onError: (err) => {
        errorMessage.value = err;
      },
    });
  } catch (err) {
    if ((err as Error).name !== "AbortError") {
      errorMessage.value = (err as Error).message || "Streaming failed";
    }
  } finally {
    if (streamingContent.value || streamingToolActivity.value.length > 0) {
      messages.value.push({
        id: Date.now() + 1,
        role: "assistant",
        content: streamingContent.value,
        createdAt: new Date().toISOString(),
        conversationId: convId,
        toolActivity: streamingToolActivity.value.length
          ? [...streamingToolActivity.value]
          : undefined,
        sourceItems: streamingSources.value.length
          ? [...streamingSources.value]
          : undefined,
      });
    }
    streaming.value = false;
    streamingContent.value = "";
    streamingToolActivity.value = [];
    streamingSources.value = [];
    abortController = null;
    await nextTick();
    scrollToBottom();
    loadConversations();
    loadQuota();
  }
}

function stopStreaming() {
  abortController?.abort();
}

async function handleFeedback({
  message,
  type,
}: {
  message: ChatMessage;
  type: "thumbs_up" | "thumbs_down";
}) {
  try {
    await chatService.submitFeedback(message.id, type);
    message.feedbackType = type;
    toast.success("Thanks for your feedback");
  } catch {
    toast.error("Could not submit feedback");
  }
}

async function handleCopy(message: ChatMessage) {
  try {
    await navigator.clipboard.writeText(message.content);
    toast.success("Copied to clipboard");
  } catch {
    toast.error("Copy failed");
  }
}

async function handleRegenerate() {
  const lastUser = [...messages.value].reverse().find((m) => m.role === "user");
  if (lastUser) await sendMessage(lastUser.content);
}

function scrollToBottom() {
  const el = messagesContainer.value;
  if (el) el.scrollTop = el.scrollHeight;
}

async function startWithPrompt(prompt: string) {
  await newConversation();
  await sendMessage(prompt);
}

onMounted(() => {
  loadConversations();
  loadQuota();
});

watch(source, () => {
  currentConversation.value = null;
  messages.value = [];
  loadConversations();
});
</script>

<template>
  <div class="chat-view">
    <div class="chat-shell">
      <ChatSidebar
        :conversations="conversations"
        :current-conversation-id="currentConversation?.id"
        :is-loading="loadingConv"
        @select="selectConversation"
        @new="newConversation"
        @delete="deleteConversation"
        @rename="renameConversation"
      />

      <section class="main-area">
        <header v-if="currentConversation" class="conversation-header">
          <h2>{{ currentConversation.title }}</h2>
          <span class="source-badge">{{ currentConversation.source }}</span>
        </header>

        <div ref="messagesContainer" class="main-scroll">
          <!-- Empty / welcome state — scrolls inside the main area; composer stays pinned below -->
          <div v-if="!currentConversation" class="welcome">
            <div class="welcome-hero">
              <div class="welcome-copy">
                <span class="welcome-eyebrow">
                  <span class="material-symbols-outlined text-[14px]"
                    >auto_awesome</span
                  >
                  AI Assistant
                </span>
                <h1 class="welcome-title">How can I help today?</h1>
                <p class="welcome-description">
                  Ask questions about procurement, vendors, orders, or any
                  system data. Conversations are saved to your account.
                </p>
                <div class="welcome-warning">
                  <span class="material-symbols-outlined text-[14px]"
                    >info</span
                  >
                  <span
                    >Verify important answers against authoritative
                    sources.</span
                  >
                </div>
              </div>

              <div class="welcome-visual" aria-hidden="true">
                <div class="welcome-orb">
                  <span class="material-symbols-outlined text-[56px]"
                    >smart_toy</span
                  >
                </div>
                <span class="welcome-status">
                  <span class="welcome-status-dot" />
                  Ready
                </span>
              </div>
            </div>

            <div class="welcome-cards" aria-label="Suggested prompts">
              <button
                v-for="(s, i) in suggestions"
                :key="s.text"
                class="welcome-card"
                :style="{ '--card-index': i }"
                @click="startWithPrompt(s.text)"
              >
                <span class="welcome-card-icon">
                  <span class="material-symbols-outlined text-[20px]">{{
                    s.icon
                  }}</span>
                </span>
                <span class="welcome-card-body">
                  <span class="welcome-card-title">{{ s.text }}</span>
                </span>
              </button>
            </div>
          </div>

          <!-- Conversation thread -->
          <div v-else class="thread">
            <div v-if="loadingMsgs" class="loading">
              <span class="material-symbols-outlined text-[22px]"
                >progress_activity</span
              >
              <span>Loading messages...</span>
            </div>

            <ChatMessageBubble
              v-for="msg in messages"
              :key="msg.id"
              :message="msg"
              :is-last-assistant-message="msg.id === lastAssistantId"
              :is-streaming="false"
              @feedback="handleFeedback"
              @copy="handleCopy"
              @regenerate="handleRegenerate"
            />

            <ChatMessageBubble
              v-if="streaming"
              :message="{
                id: -1,
                role: 'assistant',
                content: streamingContent,
                createdAt: new Date().toISOString(),
                conversationId: currentConversation.id,
                toolActivity: streamingToolActivity,
                sourceItems: streamingSources,
              }"
              :is-last-assistant-message="true"
              :is-streaming="true"
            />
          </div>
        </div>

        <ChatInputBox
          :disabled="streaming"
          :error-message="errorMessage"
          :quota-warnings="quotaWarnings"
          :retention-days="retentionDays"
          :placeholder="
            currentConversation
              ? 'Ask anything...'
              : 'Start a new conversation...'
          "
          @send="sendMessage"
          @stop="stopStreaming"
        />
      </section>
    </div>
  </div>
</template>

<style scoped>
/* ------------------------------------------------------------------------- */
/* Launchpad-inspired chat shell. The composer is ALWAYS pinned at the       */
/* bottom of `.main-area` — the welcome / empty state scrolls inside         */
/* `.main-scroll`, so the textbox never floats in the middle of the page.    */
/* ------------------------------------------------------------------------- */

.chat-view {
  /* Tokens — fall back to launchpad-ish defaults when theme tokens missing. */
  --chat-primary: var(--color-primary, #4f46e5);
  --chat-primary-strong: var(--color-primary-dark, #3730a3);
  --chat-border: color-mix(
    in srgb,
    var(--chat-primary) 9%,
    var(--color-border, #e2e8f0) 91%
  );
  --chat-border-strong: color-mix(
    in srgb,
    var(--chat-primary) 22%,
    var(--color-border, #c7cdf7) 78%
  );
  --chat-bg: var(--color-bg-light, #f7f6fb);
  --chat-panel: var(--color-surface, #ffffff);
  --chat-panel-alt: var(--color-surface-alt, #f8fafc);
  --chat-active: var(--color-sidebar-active, #eef0fe);
  --chat-text: var(--color-text, #0f172a);
  --chat-muted: var(--color-text-muted, #64748b);
  --chat-radius-frame: 1.35rem;
  --chat-shadow-soft: 0 24px 70px -48px rgba(15, 23, 42, 0.42);

  display: flex;
  min-height: calc(100dvh - 120px);
  width: 100%;
  min-width: 0;
  color: var(--chat-text);
}

.chat-shell {
  position: relative;
  isolation: isolate;
  display: flex;
  width: 100%;
  min-width: 0;
  min-height: 0;
  flex: 1;
  flex-direction: column;
  overflow: hidden;
  border: 1px solid var(--chat-border-strong);
  border-radius: var(--chat-radius-frame);
  background:
    radial-gradient(
      circle at 0% 0%,
      color-mix(in srgb, var(--chat-primary) 10%, transparent) 0,
      transparent 20rem
    ),
    radial-gradient(
      circle at 100% 0%,
      color-mix(in srgb, var(--chat-primary) 8%, transparent) 0,
      transparent 22rem
    ),
    linear-gradient(
      135deg,
      color-mix(in srgb, var(--chat-panel) 82%, var(--chat-bg) 18%) 0%,
      var(--chat-bg) 46%,
      color-mix(in srgb, var(--chat-primary) 4%, var(--chat-bg) 96%) 100%
    );
  box-shadow:
    inset 0 1px 0 rgba(255, 255, 255, 0.65),
    var(--chat-shadow-soft);
}

.main-area {
  position: relative;
  display: flex;
  min-width: 0;
  min-height: 0;
  flex: 1;
  flex-direction: column;
  background:
    radial-gradient(
      circle at 50% 0%,
      color-mix(in srgb, var(--chat-primary) 6%, transparent) 0,
      transparent 20rem
    ),
    linear-gradient(
      180deg,
      color-mix(in srgb, var(--chat-panel) 52%, var(--chat-bg) 48%) 0%,
      var(--chat-bg) 22rem
    );
}

.conversation-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px 20px;
  border-bottom: 1px solid
    color-mix(in srgb, var(--chat-border) 65%, transparent);
  background: color-mix(in srgb, var(--chat-panel) 78%, transparent);
  backdrop-filter: blur(12px);
}

.conversation-header h2 {
  flex: 1;
  margin: 0;
  color: var(--chat-text);
  font-size: 15px;
  font-weight: 650;
}

.source-badge {
  padding: 3px 10px;
  border-radius: 999px;
  background: color-mix(
    in srgb,
    var(--chat-primary) 10%,
    var(--chat-panel) 90%
  );
  color: var(--chat-primary);
  font-size: 11px;
  font-weight: 650;
  text-transform: capitalize;
}

.main-scroll {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding: 1rem 0.85rem 0.45rem;
  scrollbar-width: thin;
  scrollbar-color: color-mix(in srgb, var(--chat-primary) 22%, #d7deeb 78%)
    transparent;
}

.main-scroll::-webkit-scrollbar {
  width: 10px;
  height: 10px;
}

.main-scroll::-webkit-scrollbar-thumb {
  border: 3px solid transparent;
  border-radius: 999px;
  background: color-mix(in srgb, var(--chat-primary) 22%, #d7deeb 78%);
  background-clip: padding-box;
}

.thread {
  width: 100%;
  max-width: 56rem;
  min-width: 0;
  margin: 0 auto;
}

.loading {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 40px;
  color: var(--chat-muted);
  font-size: 13px;
}

.loading .material-symbols-outlined {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

/* --------------------------------------------------------------------- */
/* Welcome / empty state — hero card + suggestion grid, scrollable.      */
/* Mirrors launchpad's ChatbotEmptyState (hero card, source pills, grid).*/
/* --------------------------------------------------------------------- */

.welcome {
  display: flex;
  min-height: 100%;
  width: 100%;
  max-width: 58rem;
  margin: 0 auto;
  flex-direction: column;
  justify-content: center;
  gap: 1rem;
  padding: 1rem 0 1.5rem;
}

.welcome-hero {
  display: grid;
  align-items: center;
  gap: 1.35rem;
  padding: 1.5rem;
  border: 1px solid color-mix(in srgb, var(--chat-primary) 3%, transparent);
  border-radius: 1.2rem;
  background:
    radial-gradient(
      circle at 84% 42%,
      color-mix(in srgb, var(--chat-primary) 8%, transparent) 0,
      transparent 10rem
    ),
    radial-gradient(
      circle at 8% 0%,
      rgba(255, 255, 255, 0.92) 0,
      transparent 18rem
    ),
    linear-gradient(
      135deg,
      color-mix(in srgb, var(--chat-panel) 97%, var(--chat-active) 3%) 0%,
      color-mix(in srgb, var(--chat-panel) 80%, var(--chat-bg) 20%) 100%
    );
  box-shadow:
    inset 0 1px 0 rgba(255, 255, 255, 0.85),
    0 18px 50px -42px rgba(15, 23, 42, 0.34);
}

.welcome-copy {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.welcome-eyebrow {
  display: inline-flex;
  align-items: center;
  gap: 0.38rem;
  align-self: flex-start;
  padding: 0.28rem 0.62rem;
  border-radius: 999px;
  border: 1px solid color-mix(in srgb, var(--chat-primary) 7%, transparent);
  background: color-mix(in srgb, var(--chat-active) 58%, var(--chat-panel) 42%);
  color: var(--chat-primary);
  font-size: 0.64rem;
  font-weight: 650;
  letter-spacing: 0.12em;
  text-transform: uppercase;
}

.welcome-title {
  margin: 0.95rem 0 0;
  color: var(--chat-text);
  max-width: 34rem;
  font-size: clamp(1.65rem, 2.4vw, 2.2rem);
  font-weight: 600;
  letter-spacing: 0;
  line-height: 1.16;
}

.welcome-description {
  margin: 0.62rem 0 0;
  color: var(--chat-muted);
  max-width: 39rem;
  font-size: 0.92rem;
  line-height: 1.72;
}

.welcome-warning {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
  width: fit-content;
  max-width: 100%;
  margin-top: 0.95rem;
  padding: 0.5rem 0.72rem;
  border: 1px solid color-mix(in srgb, #eab308 18%, transparent);
  border-radius: 999px;
  background: color-mix(in srgb, #fffbeb 76%, var(--chat-panel) 24%);
  color: #8a6208;
  font-size: 0.75rem;
  line-height: 1.45;
}

.welcome-visual {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  min-height: 11.75rem;
  border-radius: 999px;
  background: radial-gradient(
    circle at 50% 48%,
    color-mix(in srgb, var(--chat-primary) 10%, transparent) 0,
    color-mix(in srgb, var(--chat-primary) 4%, transparent) 42%,
    transparent 68%
  );
  overflow: hidden;
}

.welcome-visual::before {
  content: "";
  position: absolute;
  inset: 1.05rem;
  border-radius: 999px;
  background:
    radial-gradient(
      circle at 36% 34%,
      rgba(255, 255, 255, 0.95),
      transparent 42%
    ),
    color-mix(in srgb, var(--chat-active) 68%, transparent);
  opacity: 0.72;
}

.welcome-orb {
  position: relative;
  z-index: 1;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 7rem;
  height: 7rem;
  border-radius: 999px;
  background: linear-gradient(
    135deg,
    color-mix(in srgb, var(--chat-primary) 70%, #ffffff 30%),
    var(--chat-primary)
  );
  color: #ffffff;
  box-shadow: 0 22px 44px -22px
    color-mix(in srgb, var(--chat-primary) 68%, transparent);
  transform: translateY(-0.5rem);
}

.welcome-status {
  position: absolute;
  left: 50%;
  bottom: 0.15rem;
  display: inline-flex;
  align-items: center;
  gap: 0.38rem;
  padding: 0.32rem 0.58rem;
  border: 1px solid
    color-mix(in srgb, var(--chat-primary) 7%, var(--chat-border));
  border-radius: 999px;
  background: color-mix(in srgb, var(--chat-panel) 86%, transparent);
  color: var(--chat-text);
  font-size: 0.68rem;
  font-weight: 650;
  backdrop-filter: blur(12px);
  transform: translateX(-50%);
}

.welcome-status-dot {
  width: 0.45rem;
  height: 0.45rem;
  border-radius: 999px;
  background: #10b981;
  box-shadow: 0 0 0 0.18rem rgba(16, 185, 129, 0.14);
}

.welcome-cards {
  display: grid;
  grid-template-columns: 1fr;
  gap: 0.78rem;
}

.welcome-card {
  position: relative;
  display: flex;
  align-items: center;
  gap: 0.95rem;
  min-height: 5.5rem;
  padding: 1rem 1.08rem;
  border: 1px solid color-mix(in srgb, var(--chat-primary) 4%, transparent);
  border-radius: 0.95rem;
  background:
    radial-gradient(
      circle at 0% 0%,
      color-mix(in srgb, var(--chat-primary) 4%, transparent) 0,
      transparent 8rem
    ),
    color-mix(in srgb, var(--chat-panel) 94%, transparent);
  color: inherit;
  cursor: pointer;
  text-align: left;
  font: inherit;
  box-shadow:
    inset 0 1px 0 rgba(255, 255, 255, 0.82),
    0 18px 42px -37px rgba(15, 23, 42, 0.32);
  animation: chat-card-rise 0.38s ease both;
  animation-delay: calc(var(--card-index, 0) * 55ms);
  transition:
    border-color 0.16s ease,
    transform 0.16s ease,
    box-shadow 0.16s ease;
}

.welcome-card:hover {
  border-color: color-mix(
    in srgb,
    var(--chat-primary) 28%,
    var(--chat-border) 72%
  );
  transform: translateY(-1px);
  box-shadow:
    inset 0 1px 0 rgba(255, 255, 255, 0.82),
    0 22px 48px -32px color-mix(in srgb, var(--chat-primary) 32%, transparent);
}

.welcome-card-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.8rem;
  height: 2.8rem;
  flex-shrink: 0;
  border-radius: 999px;
  background:
    radial-gradient(
      circle at 34% 24%,
      rgba(255, 255, 255, 0.95) 0,
      transparent 2.15rem
    ),
    color-mix(in srgb, var(--chat-active) 82%, var(--chat-panel) 18%);
  color: var(--chat-primary);
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.8);
}

.welcome-card-body {
  display: grid;
  min-width: 0;
  gap: 0.28rem;
}

.welcome-card-title {
  color: var(--chat-text);
  font-size: 0.88rem;
  font-weight: 600;
  line-height: 1.36;
}

@keyframes chat-card-rise {
  from {
    opacity: 0;
    transform: translateY(0.55rem);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

/* --------------------------------------------------------------------- */
/* Responsive                                                            */
/* --------------------------------------------------------------------- */

@media (min-width: 640px) {
  .welcome-cards {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (min-width: 900px) {
  .chat-view {
    height: calc(100vh - 120px);
    min-height: 0;
  }

  .chat-shell {
    flex-direction: row;
  }

  .main-scroll {
    padding: 1.55rem 2.2rem 0.7rem;
  }

  .welcome-hero {
    grid-template-columns: minmax(0, 1fr) minmax(8.8rem, 0.27fr);
    padding: 1.7rem 1.9rem;
  }
}

@media (min-width: 1280px) {
  .main-scroll {
    padding-left: 2.6rem;
    padding-right: 2.6rem;
  }
}
</style>
