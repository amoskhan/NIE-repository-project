<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";
import { NieDataTable } from "@nie/ui";
import purchaseOrderService, {
  type PurchaseOrderDto,
} from "@/services/procurement/purchaseOrderService";
import { getPurchaseOrderStatusLabel } from "@/types/procurementStatus";
import { useServerDataTable } from "@/composables/data-tables/useServerDataTable";

const router = useRouter();
const orderTable = useServerDataTable<PurchaseOrderDto>({
  search: purchaseOrderService.searchTable,
  getFilterOptions: purchaseOrderService.getFilterOptions,
});
const {
  rows,
  totalItems,
  loading,
  error,
  filterOptionPages,
  load: loadOrders,
  loadFilterOptions,
  reload: reloadOrders,
} = orderTable;
const search = ref("");
const selectedFilters = ref<Record<string, Array<string | number | boolean>>>(
  {},
);

const columns = [
  { key: "poNumber", label: "PO #" },
  { key: "vendorName", label: "Vendor" },
  { key: "totalAmount", label: "Amount", type: "number" as const },
  {
    key: "statusName",
    label: "Status",
    chip: {
      toneMap: {
        Draft: "default",
        Submitted: "info",
        PendingManagerApproval: "warning",
        PendingFinanceApproval: "warning",
        PendingProcurementApproval: "primary",
        Approved: "success",
        Rejected: "danger",
        Cancelled: "default",
      },
      label: (value: unknown) =>
        getPurchaseOrderStatusLabel(String(value ?? "")),
      dot: true,
    },
  },
  { key: "requestedByName", label: "Requested By" },
  { key: "requestDate", label: "Date", type: "date" as const },
];

function openOrder(row: PurchaseOrderDto) {
  if (!row.id) {
    return;
  }

  router.push(`/purchase-order/${row.id}`);
}

function orderRowLabel(row: PurchaseOrderDto): string {
  return `Open order ${row.poNumber ?? row.id ?? "details"}`;
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

</script>

<template>
  <div class="space-y-4 flex flex-col flex-1 min-h-0">
    <NieDataTable
      preference-key="procurement.order-history"
      :definition-version="1"
      class="flex-1 min-h-0"
      v-model:search="search"
      v-model:selected-filters="selectedFilters"
      :columns="columns"
      :data="rows"
      server-side
      :total-items="totalItems"
      :filter-option-pages="filterOptionPages"
      row-key="id"
      :loading="loading"
      :error="error"
      search-placeholder="Search all orders"
      create-label="New Order"
      hide-actions
      row-clickable
      :row-aria-label="orderRowLabel"
      @create="router.push({ name: 'new-purchase-request' })"
      @query-change="loadOrders"
      @filter-options-request="loadFilterOptions"
      @retry="reloadOrders"
      @row-click="openOrder"
    >
      <template #cell-totalAmount="{ value }">
        <span class="font-semibold">{{ formatCurrency(value) }}</span>
      </template>

      <template #cell-requestDate="{ value }">
        {{ formatDate(value) }}
      </template>

      <template #cell-requestedByName="{ value }">
        {{ value || "-" }}
      </template>
    </NieDataTable>
  </div>
</template>
