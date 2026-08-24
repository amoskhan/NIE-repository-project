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
      <p v-if="retentionDays" class="composer-notice composer-notice--retention">
        <span class="material-symbols-outlined text-body">shield</span>
        <span>Each conversation is deleted after {{ retentionDays }} days.</span>
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
          aria-label="Chat message"
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
          <span class="material-symbols-outlined text-section-title">send</span>
        </button>
        <button
          v-else
          type="button"
          class="composer-stop"
          aria-label="Stop generating"
          @click="emit('stop')"
        >
          <span class="material-symbols-outlined text-body-lg">stop</span>
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
  --chat-primary: var(--color-primary, var(--theme-color-brand-600));
  --chat-border: color-mix(
    in srgb,
    var(--chat-primary) 9%,
    var(--color-border, var(--theme-color-border-default)) 91%
  );
  --chat-panel: var(--color-surface, var(--theme-color-static-white));
  --chat-bg: var(--color-bg-light, var(--theme-color-surface-canvas));
  --chat-active: var(--color-sidebar-active, var(--theme-color-brand-50));
  --chat-text: var(--color-text, var(--theme-color-text-strong));
  --chat-muted: var(--color-text-muted, var(--theme-color-text-muted));

  padding: var(--theme-space-2) var(--theme-space-3) var(--theme-space-3);
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
  margin: 0 auto var(--theme-space-2);
  gap: var(--theme-space-1);
}

.composer-notice {
  display: inline-flex;
  align-items: center;
  gap: var(--theme-space-2);
  width: fit-content;
  max-width: 100%;
  margin: 0;
  border: 1px solid color-mix(in srgb, var(--chat-primary) 7%, var(--chat-border));
  border-radius: var(--theme-radius-pill);
  padding: var(--theme-space-1) var(--theme-space-3);
  background: color-mix(in srgb, var(--chat-panel) 88%, transparent);
  color: color-mix(in srgb, var(--chat-muted) 82%, var(--chat-text) 18%);
  font-size: var(--theme-font-size-caption);
  line-height: 1.45;
  backdrop-filter: blur(12px);
}

.composer-notice--quota {
  border-color: color-mix(in srgb, var(--chat-primary) 28%, var(--chat-border) 72%);
  background: color-mix(in srgb, var(--chat-primary) 8%, var(--chat-panel) 92%);
  color: color-mix(in srgb, var(--chat-primary) 72%, var(--chat-text) 28%);
  font-weight: var(--theme-font-weight-semibold);
}

.composer {
  position: relative;
  width: 100%;
  max-width: 56rem;
  margin: 0 auto;
  padding: var(--theme-space-3) var(--theme-space-3) var(--theme-space-2);
  border: 1px solid color-mix(in srgb, var(--chat-primary) 7%, var(--chat-border));
  border-radius: var(--theme-radius-panel);
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
  box-shadow: var(--theme-shadow-inset), var(--theme-shadow-soft);
  transition: border-color 0.16s ease, box-shadow 0.16s ease;
}

.composer:focus-within {
  border-color: color-mix(in srgb, var(--chat-primary) 22%, var(--chat-border) 78%);
  box-shadow: var(--theme-shadow-inset), var(--theme-shadow-card);
}

.composer-row {
  display: flex;
  align-items: flex-end;
  gap: var(--theme-space-3);
  min-width: 0;
}

.composer-input {
  flex: 1;
  box-sizing: border-box;
  width: 100%;
  min-height: var(--theme-control-height-md);
  border: 0;
  border-radius: var(--theme-radius-control);
  padding: var(--theme-space-2) var(--theme-space-1) var(--theme-space-2) var(--theme-space-1);
  resize: none;
  background: transparent;
  color: var(--chat-text);
  font-size: var(--theme-font-size-body);
  line-height: 1.5;
  outline: none;
  font-family: inherit;
  scrollbar-width: thin;
  scrollbar-color: color-mix(in srgb, var(--chat-primary) 22%, var(--theme-color-border-default) 78%)
    transparent;
}

.composer-input::-webkit-scrollbar {
  width: 10px;
  height: 10px;
}

.composer-input::-webkit-scrollbar-thumb {
  border: 3px solid transparent;
  border-radius: var(--theme-radius-pill);
  background: color-mix(in srgb, var(--chat-primary) 22%, var(--theme-color-border-default) 78%);
  background-clip: padding-box;
}

.composer-input::placeholder {
  color: var(--chat-muted);
}

.composer-helper {
  display: flex;
  flex-wrap: wrap;
  gap: var(--theme-space-1);
  margin-top: var(--theme-space-1);
  color: color-mix(in srgb, var(--chat-muted) 88%, transparent);
  font-size: var(--theme-font-size-caption);
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
  border-radius: var(--theme-radius-pill);
  background: color-mix(in srgb, var(--chat-primary) 18%, var(--chat-panel) 82%);
  color: var(--theme-color-on-brand);
  cursor: pointer;
  transition: background-color 0.16s ease, transform 0.16s ease, opacity 0.16s ease;
}

.composer-send:enabled {
  background: var(--chat-primary);
  box-shadow: var(--theme-shadow-card);
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
  gap: var(--theme-space-2);
  align-self: flex-end;
  padding: var(--theme-space-2) var(--theme-space-4);
  border-radius: var(--theme-radius-pill);
  border: 1px solid var(--theme-color-danger-500);
  background: var(--chat-panel);
  color: var(--theme-color-danger-500);
  font-size: var(--theme-font-size-label);
  font-weight: var(--theme-font-weight-semibold);
  cursor: pointer;
}

.composer-stop:hover {
  background: var(--theme-color-danger-surface);
}

.composer-error {
  width: 100%;
  max-width: 56rem;
  margin: var(--theme-space-2) auto 0;
  color: var(--theme-color-danger-600);
  font-size: var(--theme-font-size-label);
}

@media (min-width: 768px) {
  .composer-wrap {
    padding: var(--theme-space-3) var(--theme-space-5) var(--theme-space-3);
  }
}

@media (min-width: 1024px) {
  .composer-wrap {
    padding: var(--theme-space-3) var(--theme-space-8) var(--theme-space-4);
  }
}
</style>
