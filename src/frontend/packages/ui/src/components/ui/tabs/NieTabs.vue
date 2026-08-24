<script setup lang="ts" generic="T extends string">
import { computed, useId, useTemplateRef } from "vue";
import type { NieTabItem } from "./types";

const props = defineProps<{
  items: readonly NieTabItem<T>[];
  ariaLabel: string;
  idPrefix?: string;
}>();
const model = defineModel<T>({ required: true });
const generatedId = useId();
const tabButtons = useTemplateRef<HTMLButtonElement[]>("tabButtons");

const resolvedIdPrefix = computed(
  () => props.idPrefix?.trim() || `nie-tabs-${generatedId}`,
);

function select(item: NieTabItem<T>): void {
  if (!item.disabled) model.value = item.id;
}

function focusTab(index: number): void {
  tabButtons.value?.[index]?.focus();
}

function enabledIndexFrom(startIndex: number, direction: 1 | -1): number {
  const count = props.items.length;
  for (let offset = 1; offset <= count; offset += 1) {
    const candidate = (startIndex + direction * offset + count) % count;
    if (!props.items[candidate]?.disabled) return candidate;
  }
  return startIndex;
}

function boundaryIndex(fromEnd: boolean): number {
  const indexes = props.items.map((_, index) => index);
  if (fromEnd) indexes.reverse();
  return indexes.find((index) => !props.items[index]?.disabled) ?? 0;
}

function handleKeydown(event: KeyboardEvent, index: number): void {
  let nextIndex: number | null = null;

  if (event.key === "ArrowRight") {
    nextIndex = enabledIndexFrom(index, 1);
  } else if (event.key === "ArrowLeft") {
    nextIndex = enabledIndexFrom(index, -1);
  } else if (event.key === "Home") {
    nextIndex = boundaryIndex(false);
  } else if (event.key === "End") {
    nextIndex = boundaryIndex(true);
  }

  if (nextIndex === null) return;
  event.preventDefault();
  focusTab(nextIndex);
}
</script>

<template>
  <div class="nie-tabs" role="tablist" :aria-label="ariaLabel">
    <button
      v-for="(item, index) in items"
      :id="`${resolvedIdPrefix}-${item.id}`"
      :key="item.id"
      ref="tabButtons"
      type="button"
      role="tab"
      class="nie-tabs__tab"
      :class="{ 'nie-tabs__tab--active': model === item.id }"
      :aria-controls="item.panelId"
      :aria-selected="model === item.id"
      :disabled="item.disabled"
      :tabindex="model === item.id ? 0 : -1"
      @click="select(item)"
      @keydown="handleKeydown($event, index)"
    >
      <span
        v-if="item.icon"
        class="nie-tabs__icon material-symbols-outlined"
        aria-hidden="true"
        >{{ item.icon }}</span
      >
      <span>{{ item.label }}</span>
      <span v-if="item.count !== undefined" class="nie-tabs__count">
        {{ item.count }}
      </span>
    </button>
  </div>
</template>

<style scoped>
.nie-tabs {
  display: flex;
  width: fit-content;
  max-width: 100%;
  gap: var(--theme-space-1);
  overflow-x: auto;
  border-radius: var(--theme-radius-control);
  background: var(--theme-color-surface-subtle);
  padding: var(--theme-space-1);
  scrollbar-width: none;
  -ms-overflow-style: none;
}

.nie-tabs::-webkit-scrollbar {
  display: none;
}

.nie-tabs__tab {
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  justify-content: center;
  gap: var(--theme-space-2);
  border: 0;
  border-radius: var(--theme-radius-control);
  background: transparent;
  min-height: var(--theme-control-height-md);
  padding: var(--theme-space-2) var(--theme-space-4);
  color: var(--theme-color-text-soft);
  font: inherit;
  font-size: var(--theme-font-size-label);
  font-weight: var(--theme-font-weight-bold);
  white-space: nowrap;
  cursor: pointer;
  transition:
    background-color 160ms ease,
    color 160ms ease,
    box-shadow 160ms ease;
}

.nie-tabs__tab:hover:not(:disabled) {
  color: var(--theme-color-text-strong);
}

.nie-tabs__tab:focus-visible {
  outline: 2px solid var(--color-primary);
  outline-offset: 2px;
}

.nie-tabs__tab:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

.nie-tabs__tab--active {
  background: var(--color-surface);
  color: var(--theme-color-text-strong);
  box-shadow: var(--theme-shadow-soft);
}

.nie-tabs__icon {
  font-size: var(--theme-font-size-body-lg);
}

.nie-tabs__count {
  display: grid;
  min-width: 1.25rem;
  height: 1.25rem;
  place-items: center;
  border-radius: var(--theme-radius-pill);
  background: var(--color-sidebar-active);
  padding-inline: var(--theme-space-1);
  font-size: var(--theme-font-size-caption);
}

@media (max-width: 800px) {
  .nie-tabs {
    width: 100%;
  }

  .nie-tabs__tab {
    flex: 1 0 auto;
  }
}
</style>
