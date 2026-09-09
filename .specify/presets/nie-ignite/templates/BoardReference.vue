<script setup lang="ts">
// Composition reference only. Bind the product's real types and actions in its
// production visual; retain its existing shell, theme and typography.
interface BoardRecord {
  id: string;
  title: string;
  owner: string;
  dueLabel: string;
  priority: string;
}
interface BoardColumn {
  id: string;
  title: string;
  description: string;
  records: BoardRecord[];
}
defineProps<{ columns: BoardColumn[] }>();
const emit = defineEmits<{ open: [id: string] }>();
</script>

<template>
  <section class="review-board" aria-label="Work by status">
    <section
      v-for="column in columns"
      :key="column.id"
      class="review-board__lane"
      :aria-labelledby="`lane-${column.id}`"
      data-preview-column
      :data-preview-total="column.records.length"
    >
      <header class="review-board__header">
        <div class="review-board__title-row" data-preview-align="center-y">
          <h2 :id="`lane-${column.id}`" data-preview-align-item>{{ column.title }}</h2>
          <span class="review-board__count" data-preview-align-item>{{ column.records.length }}</span>
        </div>
        <p class="review-board__description">{{ column.description }}</p>
      </header>
      <div class="review-board__records">
        <button
          v-for="record in column.records"
          :key="record.id"
          type="button"
          class="review-board__record"
          :data-preview-record-id="record.id"
          @click="emit('open', record.id)"
        >
          <span class="review-board__record-id">{{ record.id }}</span>
          <strong>{{ record.title }}</strong>
          <span class="review-board__metadata">
            <span>{{ record.owner }}</span><span>{{ record.dueLabel }}</span>
          </span>
          <span class="review-board__priority">{{ record.priority }}</span>
        </button>
        <p v-if="!column.records.length" class="review-board__empty">No items at this stage.</p>
      </div>
    </section>
  </section>
</template>

<style scoped>
.review-board { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 1rem; align-items: start; }
.review-board__lane { min-width: 0; border: 1px solid var(--color-border); border-radius: 1rem; background: var(--color-surface-alt); }
.review-board__header { padding: 1rem; border-bottom: 1px solid var(--color-border); }
.review-board__title-row { display: flex; align-items: center; justify-content: space-between; gap: .75rem; }
.review-board__title-row h2 { margin: 0; color: var(--color-text); font-size: var(--theme-font-size-lg); font-weight: 700; }
.review-board__count { display: inline-grid; min-width: 1.75rem; min-height: 1.75rem; place-items: center; padding: 0 .4rem; border-radius: 999px; color: var(--color-text); background: var(--color-surface); }
.review-board__description { margin: .5rem 0 0; color: var(--color-text-muted); font-size: var(--theme-font-size-md); }
.review-board__records { display: grid; gap: .75rem; padding: .75rem; }
.review-board__record { display: grid; gap: .6rem; width: 100%; padding: 1rem; text-align: left; border: 1px solid var(--color-border); border-radius: .75rem; background: var(--color-surface); color: var(--color-text); box-shadow: 0 2px 4px rgb(0 0 0 / .04); }
.review-board__record:hover { border-color: var(--color-primary); }
.review-board__record:focus-visible { outline: 2px solid var(--color-primary); outline-offset: 3px; }
.review-board__record strong { line-height: 1.5; font-size: var(--theme-font-size-md); }
.review-board__record-id, .review-board__metadata { font-size: var(--theme-font-size-sm); color: var(--color-text-muted); }
.review-board__metadata { display: flex; flex-wrap: wrap; justify-content: space-between; gap: .4rem .75rem; }
.review-board__priority { justify-self: start; border: 1px solid var(--color-border); border-radius: .35rem; padding: .15rem .4rem; font-size: var(--theme-font-size-sm); }
.review-board__empty { margin: 0; padding: 1.5rem .5rem; color: var(--color-text-muted); text-align: center; }
@media (max-width: 767px) { .review-board { grid-template-columns: minmax(0, 1fr); } }
</style>
