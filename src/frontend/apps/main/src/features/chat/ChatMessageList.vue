<script setup lang="ts">
import type { ChatMessage } from "./types";

defineProps<{
  messages: readonly ChatMessage[];
}>();
</script>

<template>
  <ol class="message-list" aria-live="polite" aria-label="Chat conversation">
    <li
      v-for="message in messages"
      :key="message.id"
      class="message-row"
      :class="`message-row--${message.author}`"
    >
      <p class="message-author">
        {{ message.author === "assistant" ? "Assistant" : "You" }}
      </p>
      <p class="message-bubble">{{ message.text }}</p>
      <p v-if="message.citation" class="message-citation">Source: {{ message.citation }}</p>
    </li>
  </ol>
</template>

<style scoped>
.message-list {
  display: flex;
  flex: 1;
  flex-direction: column;
  gap: 1.25rem;
  min-height: 17rem;
  margin: 0;
  padding: 1.5rem;
  list-style: none;
  overflow-y: auto;
}

.message-row {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  max-width: 85%;
}

.message-row--educator {
  align-self: flex-end;
  align-items: flex-end;
}

.message-author {
  margin: 0 0 0.35rem;
  color: var(--muted);
  font-size: 0.72rem;
  font-weight: 750;
}

.message-bubble {
  margin: 0;
  padding: 0.8rem 0.95rem;
  border: 1px solid var(--border);
  border-radius: 0.3rem 1rem 1rem 1rem;
  background: #f4f7fa;
  color: var(--ink);
  font-size: 0.95rem;
  line-height: 1.55;
  white-space: pre-line;
}

.message-citation {
  margin: 0.45rem 0 0;
  color: var(--muted);
  font-size: 0.72rem;
  font-weight: 650;
  line-height: 1.35;
}

.message-row--educator .message-bubble {
  border-color: var(--blue);
  border-radius: 1rem 0.3rem 1rem 1rem;
  background: var(--blue);
  color: #fff;
}
</style>
