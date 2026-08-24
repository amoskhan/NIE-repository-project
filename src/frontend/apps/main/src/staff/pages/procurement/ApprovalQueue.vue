<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import {
  useToast,
  NieButton,
  NieLoaderSymbol,
  NieModal,
  NieResultState,
  NieTextarea,
} from "@nie/ui";
import purchaseOrderService from "@/services/procurement/purchaseOrderService";
import type { PurchaseOrderDto } from "@/services/procurement/purchaseOrderService";
import {
  getPurchaseOrderApprovalStageLabel,
  getPurchaseOrderStatusClass,
} from "@/types/procurementStatus";

const router = useRouter();
const toast = useToast();

const loading = ref(true);
const orders = ref<PurchaseOrderDto[]>([]);
const loadError = ref<string | null>(null);

// Approval action state
const processingId = ref<string | null>(null);
const showCommentModal = ref(false);
const actionType = ref<"approve" | "reject">("approve");
const actionTargetId = ref<string | null>(null);
const comments = ref("");

const fetchPending = async () => {
  loading.value = true;
  loadError.value = null;
  try {
    orders.value = await purchaseOrderService.getPendingApprovals();
  } catch {
    loadError.value = "Pending approvals could not be loaded.";
    toast.error(loadError.value);
    orders.value = [];
  } finally {
    loading.value = false;
  }
};

onMounted(fetchPending);

function stageLabel(order: PurchaseOrderDto): string {
  return getPurchaseOrderApprovalStageLabel(order.statusName);
}

function stageBadgeClass(order: PurchaseOrderDto): string {
  return getPurchaseOrderStatusClass(order.statusName);
}

function formatCurrency(amount: number): string {
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

function startAction(id: string, type: "approve" | "reject") {
  actionTargetId.value = id;
  actionType.value = type;
  comments.value = "";
  showCommentModal.value = true;
}

async function confirmAction() {
  if (!actionTargetId.value) return;
  processingId.value = actionTargetId.value;
  try {
    await purchaseOrderService.processApproval({
      purchaseOrderId: actionTargetId.value,
      action: actionType.value === "approve" ? 0 : 1,
      comments: comments.value || null,
    });
    toast.success(
      actionType.value === "approve"
        ? "Order approved successfully"
        : "Order rejected",
    );
    showCommentModal.value = false;
    await fetchPending();
  } catch {
    toast.error("Failed to process approval");
  } finally {
    processingId.value = null;
  }
}
</script>

<template>
  <div class="space-y-6">
    <div v-if="loading" class="flex justify-center py-16">
      <NieLoaderSymbol size="lg" variant="brand" label="Loading approvals" />
    </div>

    <NieResultState
      v-else-if="loadError"
      variant="error"
      title="Unable to load approvals"
      :description="loadError"
    >
      <template #actions>
        <NieButton variant="outline" @click="fetchPending">Try again</NieButton>
      </template>
    </NieResultState>

    <!-- Empty State -->
    <NieResultState
      v-else-if="orders.length === 0"
      title="All caught up"
      description="No pending approvals at this time."
    >
    </NieResultState>

    <!-- Approval Cards -->
    <div v-else class="space-y-4">
      <article
        v-for="order in orders"
        :key="order.id"
        class="rounded-2xl border border-secondary-100 bg-white p-5 shadow-soft transition-shadow hover:shadow-[var(--theme-shadow-card)] dark:border-secondary-700 dark:bg-secondary-800"
      >
        <div
          class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between"
        >
          <div class="flex items-start gap-4 min-w-0">
            <div
              class="flex size-12 shrink-0 items-center justify-center rounded-2xl bg-secondary-100"
            >
              <span class="material-symbols-outlined text-xl text-secondary-500 dark:text-secondary-300"
                >description</span
              >
            </div>
            <div class="min-w-0">
              <div class="flex items-center gap-3 flex-wrap">
                <button
                  class="inline-flex min-h-10 items-center rounded-lg px-1 text-base font-bold text-primary-700 hover:underline dark:text-primary-300"
                  @click="router.push(`/purchase-order/${order.id}`)"
                >
                  {{ order.poNumber }}
                </button>
                <span
                  class="rounded-lg px-2 py-0.5 text-caption font-bold"
                  :class="stageBadgeClass(order)"
                  >{{ stageLabel(order) }}</span
                >
              </div>
              <p class="mt-1 text-sm text-secondary-500 dark:text-secondary-300 truncate">
                <span class="font-medium text-secondary-700 dark:text-secondary-200">{{
                  order.vendorName
                }}</span>
                <span class="mx-2 text-secondary-300" aria-hidden="true">|</span>
                {{ order.lines?.length ?? 0 }} items
                <span class="mx-2 text-secondary-300" aria-hidden="true">|</span>
                Requested by {{ order.requestedByName ?? "—" }}
              </p>
              <p class="mt-1 text-xs text-secondary-400 dark:text-secondary-400">
                Requested {{ formatDate(order.requestDate ?? "") }}
              </p>
            </div>
          </div>

          <div class="flex items-center gap-4 shrink-0">
            <span class="text-xl font-bold text-secondary-800 dark:text-secondary-100">
              {{ formatCurrency(order.totalAmount ?? 0) }}
            </span>
            <div class="flex gap-2">
              <NieButton
                variant="secondary"
                size="sm"
                :disabled="processingId === order.id"
                @click="startAction(order.id!, 'reject')"
              >
                <span
                  class="material-symbols-outlined text-base text-danger-500 mr-1"
                  >close</span
                >
                Reject
              </NieButton>
              <NieButton
                variant="primary"
                size="sm"
                :disabled="processingId === order.id"
                @click="startAction(order.id!, 'approve')"
              >
                <span class="material-symbols-outlined text-base mr-1"
                  >check</span
                >
                Approve
              </NieButton>
            </div>
          </div>
        </div>

        <!-- Approval Timeline -->
        <div
          v-if="order.approvals && order.approvals.length > 0"
          class="mt-4 flex items-center gap-2 border-t border-secondary-50 pt-4"
        >
          <span class="material-symbols-outlined text-sm text-secondary-300"
            >timeline</span
          >
          <div
            v-for="(approval, idx) in order.approvals"
            :key="idx"
            class="flex items-center gap-1"
          >
            <span
              class="rounded-full px-2 py-0.5 text-caption font-bold"
              :class="
                approval.action === 0
                  ? 'bg-success-100 text-success-700'
                  : approval.action === 1
                    ? 'bg-danger-100 text-danger-700'
                    : 'bg-secondary-100 text-secondary-500 dark:text-secondary-300'
              "
              >{{ approval.approvalStage }}</span
            >
            <span
              v-if="idx < order.approvals.length - 1"
              class="text-secondary-200"
              aria-hidden="true"
              >&rarr;</span
            >
          </div>
        </div>
      </article>
    </div>

    <NieModal
      v-model="showCommentModal"
      size="md"
      placement="mobile-sheet"
      :title="actionType === 'approve' ? 'Approve Order' : 'Reject Order'"
    >
              <NieTextarea
                v-model="comments"
                :rows="3"
                :label="`Comments ${actionType === 'reject' ? '(recommended)' : '(optional)'}`"
                :placeholder="
                  actionType === 'reject'
                    ? 'Reason for rejection...'
                    : 'Any comments...'
                "
              />
      <template #footer>
            <div class="flex flex-wrap justify-end gap-3">
              <NieButton variant="secondary" @click="showCommentModal = false"
                >Cancel</NieButton
              >
              <NieButton
                :variant="actionType === 'approve' ? 'primary' : 'danger'"
                :loading="!!processingId"
                @click="confirmAction"
              >
                {{ actionType === "approve" ? "Confirm Approve" : "Confirm Reject" }}
              </NieButton>
            </div>
      </template>
    </NieModal>
  </div>
</template>
