<script setup lang="ts">
/**
 * ChatMessageBubble — user / assistant message with feedback controls, tool
 * activity transcript, source items, and streaming cursor. Inspired by
 * launchpad-v2's ChatbotMessage but uses Material Symbols and theme tokens.
 */
import { computed, ref } from "vue";
import type { ChatMessage } from "@/services/chat/chatService";

const props = defineProps<{
  message: ChatMessage;
  assistantName?: string;
  isLastAssistantMessage?: boolean;
  isStreaming?: boolean;
}>();

const emit = defineEmits<{
  (
    e: "feedback",
    payload: { message: ChatMessage; type: "thumbs_up" | "thumbs_down" },
  ): void;
  (e: "copy", message: ChatMessage): void;
  (e: "regenerate"): void;
}>();

const isUser = computed(() => props.message.role === "user");
const isAssistant = computed(() => props.message.role === "assistant");

const isThinkingOpen = ref(false);
const hasPositive = computed(() => props.message.feedbackType === "thumbs_up");
const hasNegative = computed(() => props.message.feedbackType === "thumbs_down");

const showCursor = computed(
  () =>
    isAssistant.value &&
    props.isLastAssistantMessage &&
    props.isStreaming &&
    Boolean(props.message.content),
);

const showThinking = computed(
  () =>
    isAssistant.value && !props.message.content && Boolean(props.isStreaming),
);

const formatTime = (dateStr: string) => {
  if (!dateStr) return "";
  const d = new Date(dateStr);
  return d.toLocaleTimeString("en-SG", { hour: "2-digit", minute: "2-digit" });
};

function summarizeTool(detail?: string) {
  if (!detail?.trim()) return "Working with grounded sources.";
  const compact = detail.replace(/\s+/g, " ").trim();
  return compact.length > 120 ? `${compact.slice(0, 117)}...` : compact;
}
</script>

<template>
  <article
    class="msg"
    :class="isUser ? 'msg--user' : 'msg--assistant'"
  >
    <template v-if="isUser">
      <div class="msg-meta msg-meta--user">
        <span>You</span>
        <span aria-hidden="true">&middot;</span>
        <span>{{ formatTime(message.createdAt) }}</span>
      </div>
      <div class="msg-user-bubble">{{ message.content }}</div>
    </template>

    <template v-else>
      <div class="msg-assistant-card">
        <div class="msg-assistant-head">
          <div class="msg-assistant-icon">
            <span class="material-symbols-outlined text-body-lg">auto_awesome</span>
          </div>
          <div class="msg-assistant-copy">
            <p class="msg-assistant-name">
              {{ assistantName ?? "AI Assistant" }}
            </p>
            <p class="msg-assistant-meta">{{ formatTime(message.createdAt) }}</p>
          </div>
        </div>

        <div v-if="showThinking" class="msg-streaming">
          <span class="msg-dots" aria-hidden="true">
            <span></span><span></span><span></span>
          </span>
          <span>Thinking...</span>
        </div>

        <div v-else class="msg-content">
          <span v-text="message.content"></span>
          <span v-if="showCursor" class="msg-cursor" aria-hidden="true" />
        </div>

        <div
          v-if="message.toolActivity && message.toolActivity.length > 0"
          class="msg-thinking-block"
        >
          <button
            type="button"
            class="msg-thinking-toggle"
            @click="isThinkingOpen = !isThinkingOpen"
          >
            <span class="msg-dots" aria-hidden="true">
              <span></span><span></span><span></span>
            </span>
            <span>
              Thinking &middot; {{ message.toolActivity.length }}
              step{{ message.toolActivity.length === 1 ? "" : "s" }}
            </span>
            <span
              class="material-symbols-outlined msg-thinking-chevron text-body-lg"
              :class="{ 'is-open': isThinkingOpen }"
              >expand_more</span
            >
          </button>
          <div v-if="isThinkingOpen" class="msg-tool-list">
            <div
              v-for="(tool, idx) in message.toolActivity"
              :key="`${message.id}-${tool.tool}-${idx}`"
              class="msg-tool"
            >
              <strong>{{ tool.tool }}.</strong>
              <span>{{ summarizeTool(tool.detail) }}</span>
            </div>
          </div>
        </div>

        <div
          v-if="message.sourceItems && message.sourceItems.length > 0"
          class="msg-sources"
        >
          <p class="msg-sources-title">Sources</p>
          <ul class="msg-source-list">
            <li
              v-for="(item, idx) in message.sourceItems"
              :key="`${message.id}-src-${idx}`"
              class="msg-source"
            >
              <a v-if="item.url" :href="item.url" target="_blank" rel="noopener">
                {{ item.title ?? item.url }}
              </a>
              <span v-else>{{ item.title ?? item.sourceType ?? "Source" }}</span>
              <span v-if="item.excerpt" class="msg-source-excerpt">
                — {{ item.excerpt }}
              </span>
            </li>
          </ul>
        </div>

        <div v-if="message.content" class="msg-actions">
          <button
            type="button"
            class="msg-action"
            :class="{ 'is-selected': hasPositive }"
            :aria-pressed="hasPositive"
            @click="emit('feedback', { message, type: 'thumbs_up' })"
          >
            <span class="material-symbols-outlined text-body">thumb_up</span>
            Helpful
          </button>
          <button
            type="button"
            class="msg-action"
            :class="{ 'is-selected': hasNegative }"
            :aria-pressed="hasNegative"
            @click="emit('feedback', { message, type: 'thumbs_down' })"
          >
            <span class="material-symbols-outlined text-body">thumb_down</span>
            Needs work
          </button>
          <button type="button" class="msg-action" @click="emit('copy', message)">
            <span class="material-symbols-outlined text-body">content_copy</span>
            Copy
          </button>
          <button
            v-if="isLastAssistantMessage"
            type="button"
            class="msg-action"
            @click="emit('regenerate')"
          >
            <span class="material-symbols-outlined text-body">refresh</span>
            Regenerate
          </button>
        </div>
      </div>
    </template>
  </article>
</template>

<style scoped>
.msg + .msg {
  margin-top: var(--theme-space-4-5);
}

.msg--user {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  animation: rise 0.2s ease both;
}

.msg--assistant {
  animation: rise 0.22s ease both;
}

.msg-meta {
  color: var(--color-text-muted, var(--theme-color-text-muted));
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-medium);
}

.msg-meta--user {
  display: inline-flex;
  align-items: center;
  gap: var(--theme-space-1);
  margin-bottom: var(--theme-space-1-5);
}

.msg-user-bubble {
  max-width: 88%;
  padding: var(--theme-space-3) var(--theme-space-4);
  border-radius: var(--theme-radius-panel) var(--theme-radius-panel) var(--theme-radius-control) var(--theme-radius-panel);
  background: var(--color-primary, var(--theme-color-info-solid));
  color: var(--theme-color-on-brand);
  font-size: var(--theme-font-size-body);
  line-height: 1.55;
  white-space: pre-wrap;
  word-wrap: break-word;
  box-shadow: var(--theme-shadow-card);
}

.msg-assistant-card {
  border: 1px solid var(--color-border, var(--theme-color-border-default));
  border-radius: var(--theme-radius-panel);
  background: var(--color-surface, var(--theme-color-static-white));
  padding: var(--theme-space-3) var(--theme-space-4);
  box-shadow: var(--theme-shadow-inset), var(--theme-shadow-soft);
}

.msg-assistant-head {
  display: flex;
  align-items: center;
  gap: var(--theme-space-2);
  margin-bottom: var(--theme-space-2);
}

.msg-assistant-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border-radius: var(--theme-radius-control);
  background: var(--color-primary, var(--theme-color-info-solid));
  color: var(--theme-color-on-brand);
}

.msg-assistant-name {
  color: var(--color-text, var(--theme-color-text-strong));
  font-size: var(--theme-font-size-label);
  font-weight: var(--theme-font-weight-semibold);
}

.msg-assistant-meta {
  color: var(--color-text-muted, var(--theme-color-text-muted));
  font-size: var(--theme-font-size-caption);
  line-height: 1.3;
}

.msg-streaming {
  display: inline-flex;
  align-items: center;
  gap: var(--theme-space-2);
  color: var(--color-text-muted, var(--theme-color-text-muted));
  font-size: var(--theme-font-size-label);
}

.msg-content {
  position: relative;
  color: var(--color-text, var(--theme-color-text-strong));
  font-size: var(--theme-font-size-body);
  line-height: 1.6;
  white-space: pre-wrap;
  word-wrap: break-word;
}

.msg-cursor {
  display: inline-block;
  width: 5px;
  height: 16px;
  margin-left: var(--theme-space-0-5);
  border-radius: var(--theme-radius-pill);
  background: var(--color-primary, var(--theme-color-info-solid));
  vertical-align: -2px;
  animation: blink 0.9s steps(2, start) infinite;
}

.msg-thinking-block {
  margin-top: var(--theme-space-3);
  overflow: hidden;
  border: 1px solid var(--color-border, var(--theme-color-border-default));
  border-radius: var(--theme-radius-control);
  background: color-mix(in srgb, var(--color-surface, var(--theme-color-static-white)) 80%, var(--theme-color-surface-subtle));
}

.msg-thinking-toggle {
  display: flex;
  width: 100%;
  align-items: center;
  gap: var(--theme-space-2);
  padding: var(--theme-space-2) var(--theme-space-3);
  border: 0;
  background: transparent;
  color: var(--color-text, var(--theme-color-text-strong));
  font-size: var(--theme-font-size-label);
  font-weight: var(--theme-font-weight-medium);
  font-family: inherit;
  text-align: left;
  cursor: pointer;
}

.msg-thinking-chevron {
  margin-left: auto;
  color: var(--color-text-muted, var(--theme-color-text-muted));
  transition: transform 0.16s ease;
}

.msg-thinking-chevron.is-open {
  transform: rotate(180deg);
}

.msg-tool-list {
  display: grid;
  gap: var(--theme-space-1);
  padding: var(--theme-space-1) var(--theme-space-3) var(--theme-space-3) var(--theme-space-8);
}

.msg-tool {
  position: relative;
  display: grid;
  gap: var(--theme-space-1);
  color: var(--color-text-muted, var(--theme-color-text-muted));
  font-size: var(--theme-font-size-label);
  line-height: 1.5;
}

.msg-tool::before {
  content: "";
  position: absolute;
  left: -18px;
  top: 7px;
  width: 6px;
  height: 6px;
  border-radius: var(--theme-radius-pill);
  background: var(--color-primary, var(--theme-color-info-solid));
}

.msg-tool strong {
  color: var(--color-text, var(--theme-color-text-strong));
  font-weight: var(--theme-font-weight-semibold);
}

.msg-sources {
  margin-top: var(--theme-space-3);
  padding: var(--theme-space-2) var(--theme-space-3);
  border: 1px dashed var(--color-border, var(--theme-color-border-default));
  border-radius: var(--theme-radius-control);
  background: color-mix(in srgb, var(--color-surface, var(--theme-color-static-white)) 88%, var(--theme-color-surface-subtle) 12%);
}

.msg-sources-title {
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-semibold);
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--color-text-muted, var(--theme-color-text-muted));
  margin: 0 0 var(--theme-space-1-5);
}

.msg-source-list {
  margin: 0;
  padding-left: var(--theme-space-4);
  font-size: var(--theme-font-size-label);
  line-height: 1.5;
}

.msg-source-excerpt {
  color: var(--color-text-muted, var(--theme-color-text-muted));
}

.msg-actions {
  display: flex;
  flex-wrap: wrap;
  gap: var(--theme-space-1);
  margin-top: var(--theme-space-3);
}

.msg-action {
  display: inline-flex;
  align-items: center;
  gap: var(--theme-space-1);
  padding: var(--theme-space-1) var(--theme-space-3);
  border: 1px solid var(--color-border, var(--theme-color-border-default));
  border-radius: var(--theme-radius-pill);
  background: color-mix(in srgb, var(--color-surface, var(--theme-color-static-white)) 90%, transparent);
  color: var(--color-text-muted, var(--theme-color-text-muted));
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-medium);
  cursor: pointer;
  transition: border-color 0.16s ease, background-color 0.16s ease, color 0.16s ease;
}

.msg-action:hover,
.msg-action.is-selected {
  border-color: color-mix(in srgb, var(--color-primary, var(--theme-color-info-solid)) 35%, var(--color-border, var(--theme-color-border-default)));
  background: color-mix(in srgb, var(--color-primary, var(--theme-color-info-solid)) 8%, var(--theme-color-static-white) 92%);
  color: var(--color-text, var(--theme-color-text-strong));
}

.msg-dots {
  display: inline-flex;
  gap: var(--theme-space-1);
}

.msg-dots span {
  width: 5px;
  height: 5px;
  border-radius: var(--theme-radius-pill);
  background: var(--color-primary, var(--theme-color-info-solid));
  animation: bounce 0.9s infinite ease-in-out;
}

.msg-dots span:nth-child(2) {
  animation-delay: 0.15s;
}

.msg-dots span:nth-child(3) {
  animation-delay: 0.3s;
}

@keyframes blink {
  50% { opacity: 0.2; }
}

@keyframes rise {
  from { opacity: 0; transform: translateY(6px); }
  to { opacity: 1; transform: translateY(0); }
}

@keyframes bounce {
  0%, 80%, 100% { transform: scale(0.6); opacity: 0.6; }
  40% { transform: scale(1); opacity: 1; }
}

@media (min-width: 1024px) {
  .msg-user-bubble {
    max-width: 72%;
  }
}
</style>
