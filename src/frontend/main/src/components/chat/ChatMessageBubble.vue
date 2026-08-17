<script setup lang="ts">
/**
 * ChatMessageBubble — user / assistant message with feedback controls, tool
 * activity transcript, source items, and streaming cursor. Inspired by
 * launchpad-v2's ChatbotMessage but uses Material Symbols and theme tokens.
 */
import { computed, ref } from "vue";
import type { ChatMessage } from "@/services/chatService";

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
const hasNegative = computed(
  () => props.message.feedbackType === "thumbs_down",
);

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
  <article class="msg" :class="isUser ? 'msg--user' : 'msg--assistant'">
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
            <span class="material-symbols-outlined text-[16px]"
              >auto_awesome</span
            >
          </div>
          <div class="msg-assistant-copy">
            <p class="msg-assistant-name">
              {{ assistantName ?? "AI Assistant" }}
            </p>
            <p class="msg-assistant-meta">
              {{ formatTime(message.createdAt) }}
            </p>
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
              Thinking &middot; {{ message.toolActivity.length }} step{{
                message.toolActivity.length === 1 ? "" : "s"
              }}
            </span>
            <span
              class="material-symbols-outlined msg-thinking-chevron text-[16px]"
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
              <a
                v-if="item.url"
                :href="item.url"
                target="_blank"
                rel="noopener"
              >
                {{ item.title ?? item.url }}
              </a>
              <span v-else>{{
                item.title ?? item.sourceType ?? "Source"
              }}</span>
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
            <span class="material-symbols-outlined text-[14px]">thumb_up</span>
            Helpful
          </button>
          <button
            type="button"
            class="msg-action"
            :class="{ 'is-selected': hasNegative }"
            :aria-pressed="hasNegative"
            @click="emit('feedback', { message, type: 'thumbs_down' })"
          >
            <span class="material-symbols-outlined text-[14px]"
              >thumb_down</span
            >
            Needs work
          </button>
          <button
            type="button"
            class="msg-action"
            @click="emit('copy', message)"
          >
            <span class="material-symbols-outlined text-[14px]"
              >content_copy</span
            >
            Copy
          </button>
          <button
            v-if="isLastAssistantMessage"
            type="button"
            class="msg-action"
            @click="emit('regenerate')"
          >
            <span class="material-symbols-outlined text-[14px]">refresh</span>
            Regenerate
          </button>
        </div>
      </div>
    </template>
  </article>
</template>

<style scoped>
.msg + .msg {
  margin-top: 18px;
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
  color: var(--color-text-muted, #6b7280);
  font-size: 11.5px;
  font-weight: 500;
}

.msg-meta--user {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  margin-bottom: 6px;
}

.msg-user-bubble {
  max-width: 88%;
  padding: 13px 16px;
  border-radius: 18px 18px 6px 18px;
  background: var(--color-primary, #3b82f6);
  color: #fff;
  font-size: 14.5px;
  line-height: 1.55;
  white-space: pre-wrap;
  word-wrap: break-word;
  box-shadow: 0 18px 34px -24px
    color-mix(in srgb, var(--color-primary, #3b82f6) 62%, transparent);
}

.msg-assistant-card {
  border: 1px solid var(--color-border, #e5e7eb);
  border-radius: 18px;
  background: var(--color-surface, #fff);
  padding: 14px 16px;
  box-shadow:
    inset 0 1px 0 rgba(255, 255, 255, 0.6),
    0 12px 28px -22px rgba(15, 23, 42, 0.18);
}

.msg-assistant-head {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 8px;
}

.msg-assistant-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border-radius: 9px;
  background: linear-gradient(
    135deg,
    var(--color-primary, #3b82f6),
    color-mix(in srgb, var(--color-primary, #3b82f6) 60%, #000 40%)
  );
  color: #fff;
}

.msg-assistant-name {
  color: var(--color-text, #111827);
  font-size: 13px;
  font-weight: 600;
}

.msg-assistant-meta {
  color: var(--color-text-muted, #6b7280);
  font-size: 11.5px;
  line-height: 1.3;
}

.msg-streaming {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  color: var(--color-text-muted, #6b7280);
  font-size: 13px;
}

.msg-content {
  position: relative;
  color: var(--color-text, #111827);
  font-size: 14.5px;
  line-height: 1.6;
  white-space: pre-wrap;
  word-wrap: break-word;
}

.msg-cursor {
  display: inline-block;
  width: 5px;
  height: 16px;
  margin-left: 2px;
  border-radius: 999px;
  background: var(--color-primary, #3b82f6);
  vertical-align: -2px;
  animation: blink 0.9s steps(2, start) infinite;
}

.msg-thinking-block {
  margin-top: 12px;
  overflow: hidden;
  border: 1px solid var(--color-border, #e5e7eb);
  border-radius: 12px;
  background: color-mix(in srgb, var(--color-surface, #fff) 80%, #f8fafc);
}

.msg-thinking-toggle {
  display: flex;
  width: 100%;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  border: 0;
  background: transparent;
  color: var(--color-text, #111827);
  font-size: 12.5px;
  font-weight: 500;
  font-family: inherit;
  text-align: left;
  cursor: pointer;
}

.msg-thinking-chevron {
  margin-left: auto;
  color: var(--color-text-muted, #6b7280);
  transition: transform 0.16s ease;
}

.msg-thinking-chevron.is-open {
  transform: rotate(180deg);
}

.msg-tool-list {
  display: grid;
  gap: 6px;
  padding: 2px 14px 12px 32px;
}

.msg-tool {
  position: relative;
  display: grid;
  gap: 2px;
  color: var(--color-text-muted, #6b7280);
  font-size: 12.5px;
  line-height: 1.5;
}

.msg-tool::before {
  content: "";
  position: absolute;
  left: -18px;
  top: 7px;
  width: 6px;
  height: 6px;
  border-radius: 999px;
  background: var(--color-primary, #3b82f6);
}

.msg-tool strong {
  color: var(--color-text, #111827);
  font-weight: 600;
}

.msg-sources {
  margin-top: 12px;
  padding: 10px 12px;
  border: 1px dashed var(--color-border, #e5e7eb);
  border-radius: 10px;
  background: color-mix(in srgb, var(--color-surface, #fff) 88%, #f3f4f6 12%);
}

.msg-sources-title {
  font-size: 11.5px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: var(--color-text-muted, #6b7280);
  margin: 0 0 6px;
}

.msg-source-list {
  margin: 0;
  padding-left: 18px;
  font-size: 12.5px;
  line-height: 1.5;
}

.msg-source-excerpt {
  color: var(--color-text-muted, #6b7280);
}

.msg-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 12px;
}

.msg-action {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 11px;
  border: 1px solid var(--color-border, #e5e7eb);
  border-radius: 999px;
  background: color-mix(in srgb, var(--color-surface, #fff) 90%, transparent);
  color: var(--color-text-muted, #6b7280);
  font-size: 11.5px;
  font-weight: 500;
  cursor: pointer;
  transition:
    border-color 0.16s ease,
    background-color 0.16s ease,
    color 0.16s ease;
}

.msg-action:hover,
.msg-action.is-selected {
  border-color: color-mix(
    in srgb,
    var(--color-primary, #3b82f6) 35%,
    var(--color-border, #e5e7eb)
  );
  background: color-mix(in srgb, var(--color-primary, #3b82f6) 8%, #fff 92%);
  color: var(--color-text, #111827);
}

.msg-dots {
  display: inline-flex;
  gap: 3px;
}

.msg-dots span {
  width: 5px;
  height: 5px;
  border-radius: 999px;
  background: var(--color-primary, #3b82f6);
  animation: bounce 0.9s infinite ease-in-out;
}

.msg-dots span:nth-child(2) {
  animation-delay: 0.15s;
}

.msg-dots span:nth-child(3) {
  animation-delay: 0.3s;
}

@keyframes blink {
  50% {
    opacity: 0.2;
  }
}

@keyframes rise {
  from {
    opacity: 0;
    transform: translateY(6px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes bounce {
  0%,
  80%,
  100% {
    transform: scale(0.6);
    opacity: 0.6;
  }
  40% {
    transform: scale(1);
    opacity: 1;
  }
}

@media (min-width: 1024px) {
  .msg-user-bubble {
    max-width: 72%;
  }
}
</style>
