<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { AppDataTable, useToast } from "@apptemplate/ui";
import purchaseOrderService, {
  type PurchaseOrderDto,
} from "@/services/purchaseOrderService";
import {
  getPurchaseOrderStatusClass,
  getPurchaseOrderStatusLabel,
} from "@/types/procurementStatus";
import { buildFilterOptions } from "@/utils/listFilterOptions";

const router = useRouter();
const toast = useToast();

const loading = ref(true);
const rows = ref<PurchaseOrderDto[]>([]);
const search = ref("");
const selectedFilters = ref<Record<string, Array<string | number | boolean>>>(
  {},
);

const columns = [
  { key: "poNumber", label: "PO #" },
  { key: "vendorName", label: "Vendor" },
  { key: "totalAmount", label: "Amount", type: "number" as const },
  { key: "statusName", label: "Status" },
  { key: "requestedByName", label: "Requested By" },
  { key: "requestDate", label: "Date", type: "date" as const },
];

const filterGroups = computed(() => [
  {
    key: "statusName",
    label: "Status",
    options: buildFilterOptions(
      rows.value,
      (row) => row.statusName,
      (value) => getPurchaseOrderStatusLabel(String(value)),
    ),
  },
  {
    key: "vendorName",
    label: "Vendor",
    options: buildFilterOptions(rows.value, (row) => row.vendorName),
  },
]);

async function loadOrders() {
  loading.value = true;

  try {
    const orders = await purchaseOrderService.getAll();
    rows.value = orders.slice().sort((left, right) => {
      const leftTime = left.requestDate
        ? new Date(left.requestDate).getTime()
        : 0;
      const rightTime = right.requestDate
        ? new Date(right.requestDate).getTime()
        : 0;
      return rightTime - leftTime;
    });
  } catch {
    toast.error("Failed to load orders");
    rows.value = [];
  } finally {
    loading.value = false;
  }
}

function openOrder(row: PurchaseOrderDto) {
  if (!row.id) {
    return;
  }

  router.push(`/purchase-order/${row.id}`);
}

function formatCurrency(amount: number | null | undefined): string {
  return new Intl.NumberFormat("en-SG", {
    style: "currency",
    currency: "SGD",
  }).format(amount ?? 0);
}

function formatDate(date: string | null | undefined): string {
  if (!date) {
    return "-";
  }

  return new Date(date).toLocaleDateString("en-SG", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

onMounted(() => {
  void loadOrders();
});
</script>

<template>
  <div class="space-y-4 flex flex-col flex-1 min-h-0">
    <AppDataTable
      class="flex-1 min-h-0"
      v-model:search="search"
      v-model:selected-filters="selectedFilters"
      :columns="columns"
      :data="rows"
      row-key="id"
      :loading="loading"
      :filter-groups="filterGroups"
      search-placeholder="Search all orders"
      create-label="New Order"
      hide-actions
      row-clickable
      @create="router.push({ name: 'new-purchase-request' })"
      @retry="loadOrders"
      @row-click="openOrder"
    >
      <template #cell-totalAmount="{ value }">
        <span class="font-semibold">{{ formatCurrency(value) }}</span>
      </template>

      <template #cell-statusName="{ value }">
        <span
          class="rounded-lg px-2.5 py-1 text-[10px] font-bold"
          :class="getPurchaseOrderStatusClass(String(value ?? ''))"
        >
          {{ getPurchaseOrderStatusLabel(String(value ?? "")) }}
        </span>
      </template>

      <template #cell-requestDate="{ value }">
        {{ formatDate(value) }}
      </template>

      <template #cell-requestedByName="{ value }">
        {{ value || "-" }}
      </template>
    </AppDataTable>
  </div>
</template>
