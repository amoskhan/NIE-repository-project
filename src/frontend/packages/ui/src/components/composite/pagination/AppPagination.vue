<script setup lang="ts">
import { computed } from "vue";
import { ChevronLeftIcon, ChevronRightIcon } from "@heroicons/vue/24/outline";
import { AppButton } from "../../ui/button";

interface Props {
  currentPage: number;
  totalPages: number;
  totalItems?: number;
  itemsPerPage?: number;
  showInfo?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  showInfo: true,
  itemsPerPage: 10,
});

const emit = defineEmits<{
  "update:currentPage": [page: number];
  "page-change": [page: number];
}>();

const startItem = computed(() => {
  if (!props.totalItems) return 0;
  return (props.currentPage - 1) * props.itemsPerPage + 1;
});

const endItem = computed(() => {
  if (!props.totalItems) return 0;
  return Math.min(props.currentPage * props.itemsPerPage, props.totalItems);
});

const visiblePages = computed(() => {
  const pages: (number | string)[] = [];
  const current = props.currentPage;
  const total = props.totalPages;

  if (total <= 7) {
    return Array.from({ length: total }, (_, i) => i + 1);
  }

  pages.push(1);

  if (current > 3) {
    pages.push("...");
  }

  const start = Math.max(2, current - 1);
  const end = Math.min(total - 1, current + 1);

  for (let i = start; i <= end; i++) {
    pages.push(i);
  }

  if (current < total - 2) {
    pages.push("...");
  }

  pages.push(total);

  return pages;
});

const goToPage = (page: number) => {
  if (page >= 1 && page <= props.totalPages && page !== props.currentPage) {
    emit("update:currentPage", page);
    emit("page-change", page);
  }
};
</script>

<template>
  <div
    class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"
  >
    <div
      v-if="showInfo && totalItems"
      class="text-sm text-secondary-600 dark:text-secondary-400"
    >
      Showing {{ startItem }} to {{ endItem }} of {{ totalItems }} results
    </div>

    <nav class="flex items-center gap-1">
      <AppButton
        variant="ghost"
        size="sm"
        class="min-w-10 px-0"
        :disabled="currentPage === 1"
        @click="goToPage(currentPage - 1)"
      >
        <ChevronLeftIcon class="h-4 w-4" />
      </AppButton>

      <template v-for="page in visiblePages" :key="page">
        <span v-if="page === '...'" class="px-2 py-1 text-secondary-400">
          ...
        </span>
        <AppButton
          v-else
          :variant="page === currentPage ? 'primary' : 'ghost'"
          size="sm"
          class="min-w-10 px-0"
          @click="goToPage(page as number)"
        >
          {{ page }}
        </AppButton>
      </template>

      <AppButton
        variant="ghost"
        size="sm"
        class="min-w-10 px-0"
        :disabled="currentPage === totalPages"
        @click="goToPage(currentPage + 1)"
      >
        <ChevronRightIcon class="h-4 w-4" />
      </AppButton>
    </nav>
  </div>
</template>
