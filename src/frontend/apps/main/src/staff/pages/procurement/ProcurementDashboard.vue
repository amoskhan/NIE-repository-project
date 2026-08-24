<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { useRouter } from "vue-router";
import {
  NieButton,
  NieLoaderSymbol,
  NieResultState,
  useToast,
} from "@nie/ui";
import purchaseOrderService from "@/services/procurement/purchaseOrderService";
import type { SpendOverviewDto } from "@/services/procurement/purchaseOrderService";
import {
  getPurchaseOrderStatusClass,
  getPurchaseOrderStatusLabel,
} from "@/types/procurementStatus";

const router = useRouter();
const toast = useToast();
const loading = ref(true);
const loadError = ref<string | null>(null);
const overview = ref<SpendOverviewDto | null>(null);

async function loadOverview() {
  loading.value = true;
  loadError.value = null;
  try {
    overview.value = await purchaseOrderService.getSpendOverview();
  } catch {
    overview.value = null;
    loadError.value = "Dashboard data could not be loaded.";
    toast.error(loadError.value);
  } finally {
    loading.value = false;
  }
}

onMounted(loadOverview);

const summaryCards = computed(() => {
  if (!overview.value) return [];
  return [
    {
      label: "Pending Approvals",
      value: overview.value.pendingApprovals,
      icon: "pending_actions",
      tone: "bg-warning-50 text-warning-700",
    },
    {
      label: "Monthly Spend",
      value: formatCurrency(overview.value.monthlySpend),
      icon: "payments",
      tone: "bg-success-50 text-success-700",
    },
    {
      label: "Recent Orders",
      value: overview.value.recentOrders,
      icon: "shopping_cart",
      tone: "bg-info-50 text-info-700",
    },
    {
      label: "Active Vendors",
      value: overview.value.totalVendors,
      icon: "storefront",
      tone: "bg-primary-50 text-primary-700",
    },
  ];
});

const maxSpendAmount = computed(() => {
  return Math.max(
    ...(overview.value?.monthlySpendTrend.map((m) => m.amount) ?? [1]),
    1,
  );
});

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat("en-SG", {
    style: "currency",
    currency: "SGD",
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount);
}

function formatCurrencyFull(amount: number): string {
  return new Intl.NumberFormat("en-SG", {
    style: "currency",
    currency: "SGD",
  }).format(amount);
}

function formatDate(date: string): string {
  return new Date(date).toLocaleDateString("en-SG", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

function openOrder(orderId: string | null | undefined) {
  if (orderId) void router.push(`/purchase-order/${orderId}`);
}
</script>

<template>
  <div class="flex min-w-0 flex-col gap-8">
    <div v-if="loading" class="flex justify-center py-16">
      <NieLoaderSymbol size="lg" variant="brand" label="Loading dashboard" />
    </div>

    <NieResultState
      v-else-if="loadError"
      variant="error"
      title="Unable to load dashboard"
      :description="loadError"
    >
      <template #actions>
        <NieButton variant="outline" @click="loadOverview">Try again</NieButton>
      </template>
    </NieResultState>

    <template v-else-if="overview">
      <!-- Summary Cards -->
      <div class="grid grid-cols-1 gap-5 sm:grid-cols-2 xl:grid-cols-4">
        <article
          v-for="card in summaryCards"
          :key="card.label"
          class="rounded-2xl border border-secondary-100 bg-white p-5 shadow-soft"
        >
          <div class="flex items-start justify-between gap-3">
            <div>
              <p
                class="text-xs font-bold uppercase tracking-wide text-secondary-400"
              >
                {{ card.label }}
              </p>
              <p class="mt-3 text-4xl font-bold text-secondary-800">
                {{ card.value }}
              </p>
            </div>
            <div class="rounded-2xl px-3 py-3" :class="card.tone">
              <span class="material-symbols-outlined text-hero">{{
                card.icon
              }}</span>
            </div>
          </div>
        </article>
      </div>

      <div class="grid gap-5 lg:grid-cols-2">
        <!-- Monthly Spend Trend -->
        <section
          class="min-w-0 rounded-2xl border border-secondary-100 bg-white p-6 shadow-soft"
        >
          <div class="flex items-center justify-between mb-6">
            <div>
              <h2 class="text-lg font-bold text-secondary-800">
                Monthly Spend Trend
              </h2>
              <p class="mt-1 text-sm text-secondary-500">Last 6 months spending</p>
            </div>
            <div
              class="rounded-full bg-secondary-100 px-3 py-1 text-xs font-bold text-secondary-500"
            >
              {{ formatCurrency(overview.totalSpend) }} total
            </div>
          </div>
          <div class="space-y-3">
            <div
              v-for="item in overview.monthlySpendTrend"
              :key="item.month"
              class="flex items-center gap-3"
            >
              <span class="w-20 text-xs font-medium text-secondary-500 shrink-0">{{
                item.month
              }}</span>
              <div class="flex-1 h-7 bg-secondary-50 rounded-lg overflow-hidden">
                <div
                  class="h-full rounded-lg transition-all duration-500"
                  style="background-color: var(--color-primary)"
                  :style="{
                    width: `${Math.max((item.amount / maxSpendAmount) * 100, 2)}%`,
                    opacity: item.amount > 0 ? 1 : 0.3,
                  }"
                ></div>
              </div>
              <span class="w-24 text-right text-xs font-bold text-secondary-700">{{
                formatCurrency(item.amount)
              }}</span>
            </div>
          </div>
        </section>

        <!-- Status Breakdown -->
        <section
          class="min-w-0 rounded-2xl border border-secondary-100 bg-white p-6 shadow-soft"
        >
          <div class="mb-6">
            <h2 class="text-lg font-bold text-secondary-800">Order Status</h2>
            <p class="mt-1 text-sm text-secondary-500">
              {{ overview.totalOrders }} total orders
            </p>
          </div>
          <div class="space-y-3">
            <div
              v-for="item in overview.statusBreakdown"
              :key="item.status"
              class="flex items-center justify-between p-3 rounded-xl bg-secondary-50"
            >
              <div class="flex items-center gap-3">
                <span
                  class="px-2.5 py-1 rounded-lg text-xs font-bold"
                  :class="getPurchaseOrderStatusClass(item.status)"
                  >{{ getPurchaseOrderStatusLabel(item.status) }}</span
                >
              </div>
              <span class="text-lg font-bold text-secondary-800">{{
                item.count
              }}</span>
            </div>
            <div
              v-if="overview.statusBreakdown.length === 0"
              class="text-center py-8 text-sm text-secondary-400"
            >
              No orders yet
            </div>
          </div>
        </section>
      </div>

      <div class="grid gap-5 lg:grid-cols-[1fr,1.2fr]">
        <!-- Top Vendors -->
        <section
          class="min-w-0 rounded-2xl border border-secondary-100 bg-white p-6 shadow-soft"
        >
          <h2 class="text-lg font-bold text-secondary-800 mb-4">Top Vendors</h2>
          <div class="space-y-3">
            <div
              v-for="(vendor, idx) in overview.topVendors"
              :key="vendor.vendorName"
              class="flex items-center gap-4 p-3 rounded-xl bg-secondary-50"
            >
              <div
                class="size-9 rounded-full flex items-center justify-center text-on-warning font-bold text-sm"
                :class="
                  idx === 0
                    ? 'bg-warning-500'
                    : idx === 1
                      ? 'bg-secondary-400'
                      : 'bg-warning-400'
                "
              >
                {{ idx + 1 }}
              </div>
              <div class="flex-1 min-w-0">
                <p class="text-sm font-semibold text-secondary-800 truncate">
                  {{ vendor.vendorName }}
                </p>
                <p class="text-xs text-secondary-500">
                  {{ vendor.orderCount }} orders
                </p>
              </div>
              <span
                class="max-w-[7rem] break-words text-right text-sm font-bold text-secondary-700 sm:max-w-none"
                >{{ formatCurrencyFull(vendor.totalSpend) }}</span
              >
            </div>
            <div
              v-if="overview.topVendors.length === 0"
              class="text-center py-8 text-sm text-secondary-400"
            >
              No vendor data yet
            </div>
          </div>
        </section>

        <!-- Recent Orders -->
        <section
          class="min-w-0 rounded-2xl border border-secondary-100 bg-white p-6 shadow-soft"
        >
          <div class="flex items-center justify-between mb-4">
            <h2 class="text-lg font-bold text-secondary-800">Recent Orders</h2>
            <button
              class="inline-flex min-h-10 items-center rounded-lg px-3 text-xs font-bold text-primary-700 hover:bg-accent-light hover:underline dark:text-primary-300"
              @click="router.push({ name: 'order-history' })"
            >
              View All
            </button>
          </div>
          <div
            class="max-h-[min(32rem,calc(100dvh-20rem))] overflow-auto overscroll-contain"
            role="region"
            tabindex="0"
            aria-label="Recent orders"
          >
            <table class="w-full text-sm">
              <thead class="sticky top-0 z-10 bg-white">
                <tr class="border-b border-secondary-100">
                  <th class="text-left py-2 text-xs font-bold text-secondary-400">
                    PO #
                  </th>
                  <th class="text-left py-2 text-xs font-bold text-secondary-400">
                    Vendor
                  </th>
                  <th class="text-right py-2 text-xs font-bold text-secondary-400">
                    Amount
                  </th>
                  <th class="text-center py-2 text-xs font-bold text-secondary-400">
                    Status
                  </th>
                  <th class="text-right py-2 text-xs font-bold text-secondary-400">
                    Date
                  </th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="order in overview.recentOrdersList"
                  :key="order.id"
                  class="cursor-pointer border-b border-secondary-50 transition-colors hover:bg-secondary-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-primary-500"
                  role="row"
                  tabindex="0"
                  :aria-label="`Open order ${order.poNumber}`"
                  @click="openOrder(order.id)"
                  @keydown.enter="openOrder(order.id)"
                  @keydown.space.prevent="openOrder(order.id)"
                >
                  <td class="py-2.5 font-semibold text-secondary-800">
                    {{ order.poNumber }}
                  </td>
                  <td class="py-2.5 text-secondary-600">
                    {{ order.vendorName }}
                  </td>
                  <td class="py-2.5 text-right font-medium text-secondary-800">
                    {{ formatCurrencyFull(order.totalAmount) }}
                  </td>
                  <td class="py-2.5 text-center">
                    <span
                      class="px-2 py-0.5 rounded-lg text-caption font-bold"
                      :class="getPurchaseOrderStatusClass(order.status)"
                      >{{ getPurchaseOrderStatusLabel(order.status) }}</span
                    >
                  </td>
                  <td class="py-2.5 text-right text-secondary-500">
                    {{ formatDate(order.requestDate) }}
                  </td>
                </tr>
              </tbody>
            </table>
            <div
              v-if="overview.recentOrdersList.length === 0"
              class="text-center py-8 text-sm text-secondary-400"
            >
              No orders yet
            </div>
          </div>
        </section>
      </div>
    </template>
  </div>
</template>
