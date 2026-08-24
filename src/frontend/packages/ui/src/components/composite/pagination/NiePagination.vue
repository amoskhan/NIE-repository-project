<script setup lang="ts">
import { computed } from "vue";
import {
  ChevronDownIcon,
  ChevronDoubleLeftIcon,
  ChevronDoubleRightIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
} from "@heroicons/vue/24/outline";
import { NieButton } from "../../ui/button";

interface Props {
  currentPage: number;
  totalPages: number;
  totalItems?: number;
  itemsPerPage?: number;
  showInfo?: boolean;
  pageSizeOptions?: number[];
  showPageSizeSelector?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  showInfo: true,
  itemsPerPage: 10,
  pageSizeOptions: () => [10, 20, 50, 100],
  showPageSizeSelector: true,
});

const emit = defineEmits<{
  "update:currentPage": [page: number];
  "page-change": [page: number];
  "update:itemsPerPage": [pageSize: number];
}>();

const normalizedPageSizeOptions = computed(() =>
  [...new Set(props.pageSizeOptions)]
    .filter((value) => Number.isInteger(value) && value > 0 && value <= 100)
    .sort((left, right) => left - right),
);

const goToPage = (page: number) => {
  if (page >= 1 && page <= props.totalPages && page !== props.currentPage) {
    emit("update:currentPage", page);
    emit("page-change", page);
  }
};

function updateItemsPerPage(event: Event): void {
  const pageSize = Number((event.target as HTMLSelectElement).value);
  if (!normalizedPageSizeOptions.value.includes(pageSize)) return;
  emit("update:itemsPerPage", pageSize);
}
</script>

<template>
  <div
    data-pagination-layout
    class="flex min-w-0 items-center justify-between gap-2"
  >
    <label
      v-if="showPageSizeSelector"
      data-pagination-page-size
      class="inline-flex shrink-0 items-center justify-self-start text-sm font-medium text-secondary-600 dark:text-secondary-300"
    >
      <span data-pagination-page-size-icon class="relative inline-flex">
        <select
          :value="itemsPerPage"
          class="min-h-10 w-[4.25rem] appearance-none rounded-[var(--theme-radius-control)] border border-secondary-300 bg-white py-1.5 pl-3 pr-8 text-sm font-semibold text-secondary-800 outline-none transition focus:border-primary-500 focus:ring-2 focus:ring-primary-500/25 dark:border-secondary-600 dark:bg-secondary-800 dark:text-secondary-100"
          aria-label="Rows per page"
          data-testid="nie-page-size-select"
          @change="updateItemsPerPage"
        >
          <option
            v-for="option in normalizedPageSizeOptions"
            :key="option"
            :value="option"
          >
            {{ option }}
          </option>
        </select>
        <ChevronDownIcon
          aria-hidden="true"
          class="pointer-events-none absolute right-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-secondary-400"
        />
      </span>
    </label>

    <nav
      data-pagination-pages
      class="flex min-w-0 items-center justify-self-center gap-1"
      aria-label="Table pagination"
    >
      <NieButton
        variant="ghost"
        size="sm"
        class="min-w-10 px-0"
        aria-label="First page"
        :disabled="currentPage === 1"
        @click="goToPage(1)"
      >
        <ChevronDoubleLeftIcon aria-hidden="true" class="h-4 w-4" />
      </NieButton>

      <NieButton
        variant="ghost"
        size="sm"
        class="min-w-10 px-0"
        aria-label="Previous page"
        :disabled="currentPage === 1"
        @click="goToPage(currentPage - 1)"
      >
        <ChevronLeftIcon aria-hidden="true" class="h-4 w-4" />
      </NieButton>

      <span
        data-pagination-current-page
        class="inline-flex min-h-10 min-w-10 items-center justify-center rounded-[var(--theme-radius-control)] bg-primary-600 px-2 text-sm font-semibold text-on-brand dark:bg-primary-600"
        aria-current="page"
        :aria-label="`Page ${currentPage} of ${Math.max(totalPages, 1)}`"
      >
        {{ currentPage }}
      </span>

      <NieButton
        variant="ghost"
        size="sm"
        class="min-w-10 px-0"
        aria-label="Next page"
        :disabled="currentPage === totalPages"
        @click="goToPage(currentPage + 1)"
      >
        <ChevronRightIcon aria-hidden="true" class="h-4 w-4" />
      </NieButton>

      <NieButton
        variant="ghost"
        size="sm"
        class="min-w-10 px-0"
        aria-label="Last page"
        :disabled="currentPage === totalPages"
        @click="goToPage(totalPages)"
      >
        <ChevronDoubleRightIcon aria-hidden="true" class="h-4 w-4" />
      </NieButton>
    </nav>
  </div>
</template>
