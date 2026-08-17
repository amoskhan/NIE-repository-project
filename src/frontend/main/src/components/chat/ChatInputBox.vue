<script setup lang="ts">
/**
 * ChatInputBox — auto-resizing message composer with send / stop controls,
 * optional retention and quota notices. Inspired by launchpad-v2's ChatbotComposer.
 */
import { nextTick, onMounted, ref, watch } from "vue";

const props = defineProps<{
  disabled?: boolean;
  errorMessage?: string | null;
  quotaWarnings?: string[];
  retentionDays?: number | null;
  placeholder?: string;
}>();

const emit = defineEmits<{
  (e: "send", content: string): void;
  (e: "stop"): void;
}>();

const input = ref("");
const textareaRef = ref<HTMLTextAreaElement | null>(null);
const maxVisibleLines = 6;

function resizeTextarea(el: HTMLTextAreaElement | null = textareaRef.value) {
  if (!el) return;
  const styles = window.getComputedStyle(el);
  const lineHeight = Number.parseFloat(styles.lineHeight) || 21;
  const paddingTop = Number.parseFloat(styles.paddingTop) || 0;
  const paddingBottom = Number.parseFloat(styles.paddingBottom) || 0;
  const maxHeight = lineHeight * maxVisibleLines + paddingTop + paddingBottom;
  el.style.height = "auto";
  el.style.height = `${Math.min(el.scrollHeight, maxHeight)}px`;
  el.style.overflowY = el.scrollHeight > maxHeight ? "auto" : "hidden";
}

watch(input, async () => {
  await nextTick();
  resizeTextarea();
});

onMounted(() => resizeTextarea());

function handleSend() {
  const content = input.value.trim();
  if (!content || props.disabled) return;
  emit("send", content);
  input.value = "";
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === "Enter" && !e.shiftKey) {
    e.preventDefault();
    handleSend();
  }
}
</script>

<template>
  <footer class="composer-wrap">
    <div
      v-if="retentionDays || (quotaWarnings && quotaWarnings.length > 0)"
      class="composer-notices"
    >
      <p
        v-if="retentionDays"
        class="composer-notice composer-notice--retention"
      >
        <span class="material-symbols-outlined text-[14px]">shield</span>
        <span
          >Each conversation is deleted after {{ retentionDays }} days.</span
        >
      </p>
      <p
        v-for="warning in quotaWarnings ?? []"
        :key="warning"
        class="composer-notice composer-notice--quota"
      >
        {{ warning }}
      </p>
    </div>

    <form class="composer" @submit.prevent="handleSend">
      <div class="composer-row">
        <textarea
          ref="textareaRef"
          v-model="input"
          rows="1"
          class="composer-input"
          :placeholder="placeholder ?? 'Ask anything...'"
          :disabled="disabled"
          @keydown="handleKeydown"
        />
        <button
          v-if="!disabled"
          type="submit"
          class="composer-send"
          aria-label="Send message"
          :disabled="!input.trim()"
        >
          <span class="material-symbols-outlined text-[20px]">send</span>
        </button>
        <button
          v-else
          type="button"
          class="composer-stop"
          aria-label="Stop generating"
          @click="emit('stop')"
        >
          <span class="material-symbols-outlined text-[16px]">stop</span>
          <span>Stop</span>
        </button>
      </div>

      <div class="composer-helper">
        <span>Enter to send</span>
        <span aria-hidden="true">&middot;</span>
        <span>Shift + Enter for a new line</span>
      </div>
    </form>

    <p v-if="errorMessage" class="composer-error">{{ errorMessage }}</p>
  </footer>
</template>

<style scoped>
/* Launchpad-style composer: pinned at the bottom of the chat shell with a   */
/* gradient frame, blurred backdrop, and a larger primary send button.       */

.composer-wrap {
  --chat-primary: var(--color-primary, #4f46e5);
  --chat-border: color-mix(
    in srgb,
    var(--chat-primary) 9%,
    var(--color-border, #e2e8f0) 91%
  );
  --chat-panel: var(--color-surface, #ffffff);
  --chat-bg: var(--color-bg-light, #f7f6fb);
  --chat-active: var(--color-sidebar-active, #eef0fe);
  --chat-text: var(--color-text, #0f172a);
  --chat-muted: var(--color-text-muted, #64748b);

  padding: 0.55rem 0.85rem 0.72rem;
  border-top: 1px solid color-mix(in srgb, var(--chat-border) 52%, transparent);
  background: linear-gradient(
    180deg,
    transparent 0%,
    color-mix(in srgb, var(--chat-panel) 54%, var(--chat-bg) 46%) 22%
  );
  backdrop-filter: blur(18px);
}

.composer-notices {
  display: grid;
  width: 100%;
  max-width: 56rem;
  margin: 0 auto 0.5rem;
  gap: 0.35rem;
}

.composer-notice {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  width: fit-content;
  max-width: 100%;
  margin: 0;
  border: 1px solid
    color-mix(in srgb, var(--chat-primary) 7%, var(--chat-border));
  border-radius: 999px;
  padding: 0.34rem 0.66rem;
  background: color-mix(in srgb, var(--chat-panel) 88%, transparent);
  color: color-mix(in srgb, var(--chat-muted) 82%, var(--chat-text) 18%);
  font-size: 0.71875rem;
  line-height: 1.45;
  backdrop-filter: blur(12px);
}

.composer-notice--quota {
  border-color: color-mix(
    in srgb,
    var(--chat-primary) 28%,
    var(--chat-border) 72%
  );
  background: color-mix(in srgb, var(--chat-primary) 8%, var(--chat-panel) 92%);
  color: color-mix(in srgb, var(--chat-primary) 72%, var(--chat-text) 28%);
  font-weight: 650;
}

.composer {
  position: relative;
  width: 100%;
  max-width: 56rem;
  margin: 0 auto;
  padding: 0.78rem 0.86rem 0.58rem;
  border: 1px solid
    color-mix(in srgb, var(--chat-primary) 7%, var(--chat-border));
  border-radius: 1.05rem;
  background:
    radial-gradient(
      circle at 100% 100%,
      color-mix(in srgb, var(--chat-primary) 4%, transparent) 0,
      transparent 13rem
    ),
    linear-gradient(
      135deg,
      color-mix(in srgb, var(--chat-panel) 98%, var(--chat-active) 2%) 0%,
      color-mix(in srgb, var(--chat-panel) 93%, var(--chat-bg) 7%) 100%
    );
  box-shadow:
    inset 0 1px 0 rgba(255, 255, 255, 0.88),
    0 20px 48px -38px rgba(15, 23, 42, 0.32);
  transition:
    border-color 0.16s ease,
    box-shadow 0.16s ease;
}

.composer:focus-within {
  border-color: color-mix(
    in srgb,
    var(--chat-primary) 22%,
    var(--chat-border) 78%
  );
  box-shadow:
    inset 0 1px 0 rgba(255, 255, 255, 0.88),
    0 22px 52px -34px color-mix(in srgb, var(--chat-primary) 38%, transparent);
}

.composer-row {
  display: flex;
  align-items: flex-end;
  gap: 0.7rem;
  min-width: 0;
}

.composer-input {
  flex: 1;
  box-sizing: border-box;
  width: 100%;
  min-height: 2.6rem;
  border: 0;
  padding: 0.45rem 0.18rem 0.45rem 0.35rem;
  resize: none;
  background: transparent;
  color: var(--chat-text);
  font-size: 0.95rem;
  line-height: 1.5;
  outline: none;
  font-family: inherit;
  scrollbar-width: thin;
  scrollbar-color: color-mix(in srgb, var(--chat-primary) 22%, #d7deeb 78%)
    transparent;
}

.composer-input::-webkit-scrollbar {
  width: 10px;
  height: 10px;
}

.composer-input::-webkit-scrollbar-thumb {
  border: 3px solid transparent;
  border-radius: 999px;
  background: color-mix(in srgb, var(--chat-primary) 22%, #d7deeb 78%);
  background-clip: padding-box;
}

.composer-input::placeholder {
  color: var(--chat-muted);
}

.composer-helper {
  display: flex;
  flex-wrap: wrap;
  gap: 0.32rem;
  margin-top: 0.28rem;
  color: color-mix(in srgb, var(--chat-muted) 88%, transparent);
  font-size: 0.6875rem;
  line-height: 1.25;
}

.composer-send {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.65rem;
  height: 2.65rem;
  border: 0;
  flex-shrink: 0;
  align-self: flex-end;
  border-radius: 999px;
  background: color-mix(
    in srgb,
    var(--chat-primary) 18%,
    var(--chat-panel) 82%
  );
  color: #ffffff;
  cursor: pointer;
  transition:
    background-color 0.16s ease,
    transform 0.16s ease,
    opacity 0.16s ease;
}

.composer-send:enabled {
  background: linear-gradient(
    135deg,
    color-mix(in srgb, var(--chat-primary) 72%, #ffffff 28%),
    var(--chat-primary)
  );
  box-shadow: 0 16px 28px -18px
    color-mix(in srgb, var(--chat-primary) 72%, transparent);
}

.composer-send:enabled:hover {
  transform: translateY(-1px);
}

.composer-send:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.composer-stop {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  align-self: flex-end;
  padding: 0.55rem 0.9rem;
  border-radius: 999px;
  border: 1px solid #ef4444;
  background: var(--chat-panel);
  color: #ef4444;
  font-size: 0.82rem;
  font-weight: 650;
  cursor: pointer;
}

.composer-stop:hover {
  background: #fef2f2;
}

.composer-error {
  width: 100%;
  max-width: 56rem;
  margin: 0.55rem auto 0;
  color: #dc2626;
  font-size: 0.78rem;
}

@media (min-width: 768px) {
  .composer-wrap {
    padding: 0.7rem 1.2rem 0.85rem;
  }
}

@media (min-width: 1024px) {
  .composer-wrap {
    padding: 0.75rem 2rem 0.95rem;
  }
}
</style>
