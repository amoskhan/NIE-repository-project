<script setup lang="ts">
import { shallowRef } from "vue";

const emit = defineEmits<{
  send: [question: string];
}>();

const question = shallowRef("");

function submitQuestion(): void {
  const text = question.value.trim();

  if (!text) {
    return;
  }

  emit("send", text);
  question.value = "";
}
</script>

<template>
  <form class="composer" data-tour="syllabus-question" @submit.prevent="submitQuestion">
    <label class="composer-label" for="syllabus-question">Ask a syllabus question</label>
    <div class="composer-row">
      <textarea
        id="syllabus-question"
        v-model="question"
        class="composer-input"
        rows="2"
        maxlength="1000"
        placeholder="For example: What should pupils learn about safe landing?"
      ></textarea>
      <button class="send-button" type="submit" :disabled="!question.trim()">Send</button>
    </div>
  </form>
</template>

<style scoped>
.composer {
  padding: 1.25rem 1.5rem 1.4rem;
  border-top: 1px solid var(--border);
  background: var(--surface);
}

.composer-label {
  display: block;
  margin-bottom: 0.55rem;
  color: var(--ink);
  font-size: 0.85rem;
  font-weight: 750;
}

.composer-row {
  display: flex;
  align-items: flex-end;
  gap: 0.75rem;
}

.composer-input {
  box-sizing: border-box;
  width: 100%;
  resize: vertical;
  min-height: 3.3rem;
  padding: 0.7rem 0.8rem;
  border: 1px solid #b8c6d3;
  border-radius: 0.7rem;
  color: var(--ink);
  font: inherit;
  line-height: 1.45;
}

.composer-input:focus {
  border-color: var(--blue);
  outline: 3px solid rgb(24 102 163 / 18%);
}

.send-button {
  min-height: 3.3rem;
  padding: 0.7rem 1rem;
  border: 0;
  border-radius: 0.7rem;
  background: var(--blue);
  color: #fff;
  cursor: pointer;
  font: inherit;
  font-weight: 750;
}

.send-button:hover:not(:disabled) {
  background: #0b5187;
}

.send-button:focus-visible {
  outline: 3px solid rgb(24 102 163 / 35%);
  outline-offset: 2px;
}

.send-button:disabled {
  cursor: not-allowed;
  opacity: 0.48;
}

@media (max-width: 500px) {
  .composer-row {
    flex-direction: column;
    align-items: stretch;
  }
}
</style>
