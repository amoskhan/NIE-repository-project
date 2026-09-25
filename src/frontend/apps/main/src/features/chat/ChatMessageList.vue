<script setup lang="ts">
import type { ChatMessage } from "./types";

defineProps<{
  messages: readonly ChatMessage[];
  sourceUrl: string;
  disabled: boolean;
}>();
defineEmits<{ ask: [question: string] }>();
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
      <details v-if="message.references?.length" class="source-evidence">
        <summary>Supporting syllabus text</summary>
        <blockquote v-for="reference in message.references" :key="`${reference.pdfPage}-${reference.evidence}`">
          {{ reference.evidence }} — <a :href="`${sourceUrl}#page=${reference.pdfPage}`" target="_blank" rel="noopener noreferrer">PDF page {{ reference.pdfPage }}</a>
        </blockquote>
      </details>
      <div v-if="message.suggestions?.length" class="suggestions" aria-label="Available questions">
        <button v-for="question in message.suggestions" :key="question" type="button" :disabled="disabled" @click="$emit('ask', question)">{{ question }}</button>
      </div>
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

.source-evidence { margin-top: 0.5rem; font-size: 0.8rem; color: var(--muted); }
.source-evidence summary { cursor: pointer; }
.source-evidence blockquote { margin: 0.65rem 0; padding-left: 0.8rem; border-left: 2px solid var(--border); line-height: 1.5; }
.suggestions { display: flex; gap: 0.4rem; flex-wrap: wrap; margin-top: 0.6rem; }
.suggestions button { padding: 0.5rem; border: 1px solid var(--border); border-radius: 0.4rem; background: white; color: var(--blue); font: inherit; font-size: 0.8rem; cursor: pointer; }
</style>
