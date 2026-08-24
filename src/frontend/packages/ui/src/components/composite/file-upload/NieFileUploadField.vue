<script setup lang="ts">
import { computed, ref } from "vue";
import {
  ArrowUpTrayIcon,
  DocumentTextIcon,
  XMarkIcon,
} from "@heroicons/vue/24/outline";
import { cn, generateId } from "../../../lib/utils";
import { NieButton } from "../../ui/button";

export interface UploadedFileItem {
  id: string;
  name: string;
  size: number;
  type: string;
  file: File;
}

interface Props {
  modelValue?: UploadedFileItem[];
  label?: string;
  description?: string;
  accept?: string;
  multiple?: boolean;
  maxFiles?: number;
  disabled?: boolean;
  hint?: string;
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  modelValue: () => [],
  label: "Files",
  description: "",
  accept: "",
  multiple: true,
  maxFiles: undefined,
  disabled: false,
  hint: "Drop files here or browse from your device.",
});

const emit = defineEmits<{
  "update:modelValue": [value: UploadedFileItem[]];
}>();

const fileInput = ref<HTMLInputElement | null>(null);
const isDragging = ref(false);

const selectedFiles = computed(() => props.modelValue ?? []);

const wrapperClasses = computed(() =>
  cn("space-y-3", props.class),
);

const remainingSlots = computed(() => {
  if (!props.maxFiles) {
    return null;
  }

  return Math.max(props.maxFiles - selectedFiles.value.length, 0);
});

const canAddFiles = computed(() => {
  if (props.disabled) {
    return false;
  }

  if (!props.maxFiles) {
    return true;
  }

  return selectedFiles.value.length < props.maxFiles;
});

const formatFileSize = (size: number) => {
  if (size >= 1024 * 1024) {
    return `${(size / (1024 * 1024)).toFixed(1)} MB`;
  }

  if (size >= 1024) {
    return `${Math.round(size / 1024)} KB`;
  }

  return `${size} B`;
};

const emitFiles = (files: UploadedFileItem[]) => {
  emit("update:modelValue", files);
};

const normaliseFiles = (files: File[]) => {
  const mapped = files.map((file) => ({
    id: generateId("upload"),
    name: file.name,
    size: file.size,
    type: file.type || "application/octet-stream",
    file,
  }));

  const nextItems = props.multiple
    ? [...selectedFiles.value, ...mapped]
    : mapped.slice(0, 1);

  const seen = new Set<string>();
  let deduped = nextItems.filter((item) => {
    const signature = `${item.name}:${item.size}:${item.file.lastModified}`;
    if (seen.has(signature)) {
      return false;
    }

    seen.add(signature);
    return true;
  });

  if (props.maxFiles) {
    deduped = deduped.slice(0, props.maxFiles);
  }

  emitFiles(deduped);

  if (fileInput.value) {
    fileInput.value.value = "";
  }
};

const handleInput = (event: Event) => {
  const target = event.target as HTMLInputElement;
  if (!target.files?.length) {
    return;
  }

  normaliseFiles(Array.from(target.files));
};

const removeFile = (id: string) => {
  emitFiles(selectedFiles.value.filter((file) => file.id !== id));
};

const openPicker = () => {
  if (canAddFiles.value) {
    fileInput.value?.click();
  }
};

const handleDrop = (event: DragEvent) => {
  event.preventDefault();
  isDragging.value = false;

  if (props.disabled || !event.dataTransfer?.files?.length) {
    return;
  }

  normaliseFiles(Array.from(event.dataTransfer.files));
};
</script>

<template>
  <div :class="wrapperClasses">
    <div class="flex items-center justify-between gap-3">
      <div class="space-y-1">
        <h3 class="text-sm font-semibold text-secondary-900 dark:text-secondary-50">
          {{ label }}
        </h3>
        <p
          v-if="description"
          class="text-sm text-secondary-500 dark:text-secondary-400"
        >
          {{ description }}
        </p>
      </div>

      <span
        v-if="typeof maxFiles === 'number'"
        class="rounded-full bg-secondary-100 px-3 py-1 text-xs font-medium text-secondary-600 dark:bg-secondary-800 dark:text-secondary-300"
      >
        {{ selectedFiles.length }}/{{ maxFiles }} files
      </span>
    </div>

    <div
      :class="[
        'rounded-2xl border border-dashed p-6 transition',
        canAddFiles
          ? 'cursor-pointer border-secondary-300 bg-secondary-50/70 hover:border-primary-400 hover:bg-primary-50/40 dark:border-secondary-600 dark:bg-secondary-900/70 dark:hover:border-primary-400 dark:hover:bg-primary-950/10'
          : 'border-secondary-200 bg-secondary-100/70 opacity-70 dark:border-secondary-700 dark:bg-secondary-900/60',
        isDragging && 'border-primary-500 bg-primary-50 dark:bg-primary-950/10',
      ]"
      @click="openPicker"
      @dragover.prevent="isDragging = true"
      @dragleave.prevent="isDragging = false"
      @drop="handleDrop"
    >
      <input
        ref="fileInput"
        type="file"
        class="hidden"
        :accept="accept"
        :multiple="multiple"
        :disabled="disabled"
        @change="handleInput"
      />

      <div class="flex flex-col items-center gap-3 text-center">
        <div
          class="flex h-14 w-14 items-center justify-center rounded-2xl bg-primary-100 text-primary-600 dark:bg-primary-900/30 dark:text-primary-300"
        >
          <ArrowUpTrayIcon class="h-7 w-7" />
        </div>

        <div class="space-y-2">
          <p class="text-sm font-medium text-secondary-900 dark:text-secondary-50">
            {{ hint }}
          </p>
          <p class="text-xs text-secondary-500 dark:text-secondary-400">
            <span v-if="accept">Accepted: {{ accept }}</span>
            <span v-if="accept && remainingSlots !== null"> • </span>
            <span v-if="remainingSlots !== null">
              {{ remainingSlots }} slot{{ remainingSlots === 1 ? "" : "s" }} left
            </span>
          </p>
        </div>

        <NieButton
          variant="outline"
          size="sm"
          :disabled="!canAddFiles"
          @click.stop="openPicker"
        >
          Browse Files
        </NieButton>
      </div>
    </div>

    <ul v-if="selectedFiles.length" class="space-y-2">
      <li
        v-for="file in selectedFiles"
        :key="file.id"
        class="flex items-center justify-between gap-3 rounded-xl border border-secondary-200 bg-white px-4 py-3 dark:border-secondary-700 dark:bg-secondary-900"
      >
        <div class="flex min-w-0 items-center gap-3">
          <DocumentTextIcon class="h-5 w-5 flex-shrink-0 text-primary-500" />
          <div class="min-w-0">
            <p class="truncate text-sm font-medium text-secondary-900 dark:text-secondary-50">
              {{ file.name }}
            </p>
            <p class="text-xs text-secondary-500 dark:text-secondary-400">
              {{ formatFileSize(file.size) }}
            </p>
          </div>
        </div>

        <button
          type="button"
          :aria-label="`Remove ${file.name}`"
          class="rounded-lg p-1 text-secondary-400 transition hover:bg-secondary-100 hover:text-secondary-600 dark:hover:bg-secondary-800 dark:hover:text-secondary-300"
          @click.stop="removeFile(file.id)"
        >
          <XMarkIcon class="h-4 w-4" />
        </button>
      </li>
    </ul>
  </div>
</template>
