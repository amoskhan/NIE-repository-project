<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import { useToast, AppButton, AppBadge } from "@apptemplate/ui";
import purchaseOrderService from "@/services/purchaseOrderService";
import type { PurchaseOrderDto } from "@/services/purchaseOrderService";
import {
  getPurchaseOrderApprovalStageLabel,
  getPurchaseOrderStatusClass,
} from "@/types/procurementStatus";

const router = useRouter();
const toast = useToast();

const loading = ref(true);
const orders = ref<PurchaseOrderDto[]>([]);

// Approval action state
const processingId = ref<number | null>(null);
const showCommentModal = ref(false);
const actionType = ref<"approve" | "reject">("approve");
const actionTargetId = ref<number | null>(null);
const comments = ref("");

const fetchPending = async () => {
  loading.value = true;
  try {
    orders.value = await purchaseOrderService.getPendingApprovals();
  } catch {
    toast.error("Failed to load pending approvals");
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

function startAction(id: number, type: "approve" | "reject") {
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
      <div
        class="size-10 animate-spin rounded-full border-4 border-accent/30 border-t-accent"
      ></div>
    </div>

    <!-- Empty State -->
    <div
      v-else-if="orders.length === 0"
      class="rounded-2xl border border-slate-100 bg-white p-16 text-center shadow-soft"
    >
      <span class="material-symbols-outlined text-6xl text-slate-200"
        >task_alt</span
      >
      <p class="mt-4 text-lg font-semibold text-slate-600">All caught up!</p>
      <p class="mt-1 text-sm text-slate-400">
        No pending approvals at this time.
      </p>
    </div>

    <!-- Approval Cards -->
    <div v-else class="space-y-4">
      <article
        v-for="order in orders"
        :key="order.id"
        class="rounded-2xl border border-slate-100 bg-white p-5 shadow-soft transition-shadow hover:shadow-md"
      >
        <div
          class="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between"
        >
          <div class="flex items-start gap-4 min-w-0">
            <div
              class="flex size-12 shrink-0 items-center justify-center rounded-2xl bg-slate-100"
            >
              <span class="material-symbols-outlined text-xl text-slate-500"
                >description</span
              >
            </div>
            <div class="min-w-0">
              <div class="flex items-center gap-3 flex-wrap">
                <button
                  class="inline-flex min-h-10 items-center rounded-lg px-1 text-base font-bold hover:underline"
                  style="color: var(--color-primary)"
                  @click="router.push(`/purchase-order/${order.id}`)"
                >
                  {{ order.poNumber }}
                </button>
                <span
                  class="rounded-lg px-2 py-0.5 text-[10px] font-bold"
                  :class="stageBadgeClass(order)"
                  >{{ stageLabel(order) }}</span
                >
              </div>
              <p class="mt-1 text-sm text-slate-500 truncate">
                <span class="font-medium text-slate-700">{{
                  order.vendorName
                }}</span>
                <span class="mx-2 text-slate-300">|</span>
                {{ order.lines?.length ?? 0 }} items
                <span class="mx-2 text-slate-300">|</span>
                Requested by {{ order.requestedByName ?? "—" }}
              </p>
              <p class="mt-1 text-xs text-slate-400">
                Requested {{ formatDate(order.requestDate ?? "") }}
              </p>
            </div>
          </div>

          <div class="flex items-center gap-4 shrink-0">
            <span class="text-xl font-extrabold text-slate-800">
              {{ formatCurrency(order.totalAmount ?? 0) }}
            </span>
            <div class="flex gap-2">
              <AppButton
                variant="secondary"
                size="sm"
                :disabled="processingId === order.id"
                @click="startAction(order.id!, 'reject')"
              >
                <span
                  class="material-symbols-outlined text-base text-red-500 mr-1"
                  >close</span
                >
                Reject
              </AppButton>
              <AppButton
                variant="primary"
                size="sm"
                :disabled="processingId === order.id"
                @click="startAction(order.id!, 'approve')"
              >
                <span class="material-symbols-outlined text-base mr-1"
                  >check</span
                >
                Approve
              </AppButton>
            </div>
          </div>
        </div>

        <!-- Approval Timeline -->
        <div
          v-if="order.approvals && order.approvals.length > 0"
          class="mt-4 flex items-center gap-2 border-t border-slate-50 pt-4"
        >
          <span class="material-symbols-outlined text-sm text-slate-300"
            >timeline</span
          >
          <div
            v-for="(approval, idx) in order.approvals"
            :key="idx"
            class="flex items-center gap-1"
          >
            <span
              class="rounded-full px-2 py-0.5 text-[10px] font-bold"
              :class="
                approval.action === 0
                  ? 'bg-emerald-100 text-emerald-700'
                  : approval.action === 1
                    ? 'bg-red-100 text-red-700'
                    : 'bg-slate-100 text-slate-500'
              "
              >{{ approval.approvalStage }}</span
            >
            <span v-if="idx < order.approvals.length - 1" class="text-slate-200"
              >&rarr;</span
            >
          </div>
        </div>
      </article>
    </div>

    <!-- Comment Modal -->
    <Teleport to="body">
      <Transition name="fade">
        <div
          v-if="showCommentModal"
          class="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
          @click.self="showCommentModal = false"
        >
          <div class="mx-4 w-full max-w-md rounded-2xl bg-white p-6 shadow-xl">
            <h3 class="text-lg font-bold text-slate-800 mb-4">
              {{ actionType === "approve" ? "Approve Order" : "Reject Order" }}
            </h3>
            <div>
              <label class="mb-1 block text-xs font-semibold text-slate-600"
                >Comments
                {{
                  actionType === "reject" ? "(recommended)" : "(optional)"
                }}</label
              >
              <textarea
                v-model="comments"
                rows="3"
                class="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm focus:border-blue-400 focus:ring-1 focus:ring-blue-400"
                :placeholder="
                  actionType === 'reject'
                    ? 'Reason for rejection...'
                    : 'Any comments...'
                "
              ></textarea>
            </div>
            <div class="mt-5 flex justify-end gap-3">
              <AppButton variant="secondary" @click="showCommentModal = false"
                >Cancel</AppButton
              >
              <AppButton
                :variant="actionType === 'approve' ? 'primary' : 'danger'"
                :disabled="!!processingId"
                @click="confirmAction"
              >
                {{
                  processingId
                    ? "Processing..."
                    : actionType === "approve"
                      ? "Confirm Approve"
                      : "Confirm Reject"
                }}
              </AppButton>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
