<script setup lang="ts">
import type { SyllabusSource } from "./types";

defineProps<{
  error: string;
  isProcessing: boolean;
  progressLabel: string;
  source: SyllabusSource | null;
}>();

const emit = defineEmits<{
  remove: [];
  upload: [file: File];
}>();

function handleFileChange(event: Event): void {
  const input = event.target as HTMLInputElement;
  const [file] = Array.from(input.files ?? []);

  if (file) {
    emit("upload", file);
  }

  input.value = "";
}
</script>

<template>
  <section class="source-panel" aria-labelledby="source-title">
    <div class="source-copy">
      <p id="source-title" class="source-title">2024 PE Syllabus</p>
      <p v-if="!source" class="source-description">
        Upload the school-approved PDF to search and cite it in this browser session.
      </p>
      <p v-else class="source-description">
        <span class="source-file">{{ source.fileName }}</span> · {{ source.pageCount }}
        {{ source.pageCount === 1 ? "page" : "pages" }} ready
      </p>
    </div>

    <div class="source-actions">
      <label class="upload-button" :class="{ 'upload-button--busy': isProcessing }">
        <input
          class="visually-hidden"
          type="file"
          accept="application/pdf,.pdf"
          :disabled="isProcessing"
          @change="handleFileChange"
        />
        {{ source ? "Replace PDF" : "Upload PDF" }}
      </label>
      <button v-if="source" class="remove-button" type="button" :disabled="isProcessing" @click="emit('remove')">
        Remove
      </button>
    </div>

    <p v-if="isProcessing" class="source-progress" role="status">{{ progressLabel }}</p>
    <p v-if="error" class="source-error" role="alert">{{ error }}</p>
  </section>
</template>

<style scoped>
.source-panel {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.8rem 1rem;
  margin: 0.35rem 1.5rem 0;
  padding: 0.85rem 1rem;
  border: 1px solid #c7d8e5;
  border-radius: 0.85rem;
  background: #f6fbfe;
}

.visually-hidden {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}

.source-copy {
  min-width: 0;
  flex: 1 1 16rem;
}

.source-title,
.source-description,
.source-progress,
.source-error {
  margin: 0;
}

.source-title {
  color: var(--navy);
  font-size: 0.85rem;
  font-weight: 800;
}

.source-description,
.source-progress,
.source-error {
  margin-top: 0.2rem;
  color: var(--muted);
  font-size: 0.78rem;
  line-height: 1.4;
}

.source-file {
  color: var(--ink);
  font-weight: 700;
  overflow-wrap: anywhere;
}

.source-actions {
  display: flex;
  flex: 0 0 auto;
  gap: 0.5rem;
}

.upload-button,
.remove-button {
  display: inline-flex;
  min-height: 2.25rem;
  align-items: center;
  justify-content: center;
  padding: 0.45rem 0.7rem;
  border-radius: 0.6rem;
  cursor: pointer;
  font: inherit;
  font-size: 0.78rem;
  font-weight: 750;
}

.upload-button {
  border: 1px solid var(--blue);
  background: var(--blue);
  color: #fff;
}

.upload-button:hover,
.upload-button:focus-within {
  background: #0b5187;
}

.upload-button--busy,
.remove-button:disabled {
  cursor: wait;
  opacity: 0.6;
}

.remove-button {
  border: 1px solid #b8c6d3;
  background: #fff;
  color: var(--ink);
}

.remove-button:hover:not(:disabled) {
  border-color: var(--blue);
  color: var(--blue);
}

.upload-button:focus-within,
.remove-button:focus-visible {
  outline: 3px solid rgb(24 102 163 / 22%);
  outline-offset: 2px;
}

.source-progress,
.source-error {
  width: 100%;
}

.source-error {
  color: #a22922;
  font-weight: 650;
}

@media (max-width: 500px) {
  .source-actions {
    width: 100%;
  }

  .upload-button,
  .remove-button {
    flex: 1;
  }
}
</style>
