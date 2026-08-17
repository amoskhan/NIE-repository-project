<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useToast, AppButton, AppBadge, AppSelect } from "@apptemplate/ui";
import { getBackendUrl } from "@apptemplate/shared";
import purchaseOrderService from "@/services/purchaseOrderService";
import type {
  PurchaseOrderDto,
  PurchaseOrderDocumentDto,
} from "@/services/purchaseOrderService";
import {
  getPurchaseOrderStatusClass,
  getPurchaseOrderStatusLabel,
} from "@/types/procurementStatus";

const route = useRoute();
const router = useRouter();
const toast = useToast();

const loading = ref(true);
const order = ref<PurchaseOrderDto | null>(null);
const uploadFile = ref<File | null>(null);
const docType = ref("Quote");
const isUploading = ref(false);
const isDeletingDocId = ref<number | null>(null);

const orderId = computed(() => Number(route.params.id));

onMounted(async () => {
  try {
    order.value = await purchaseOrderService.getById(orderId.value);
  } catch {
    toast.error("Failed to load purchase order");
    router.push({ name: "order-history" });
  } finally {
    loading.value = false;
  }
});

// File upload
function onFileChange(e: Event) {
  const input = e.target as HTMLInputElement;
  if (input.files?.length) {
    const file = input.files[0];
    const allowedTypes = [
      "application/pdf",
      "image/png",
      "image/jpeg",
      "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    ];
    if (!allowedTypes.includes(file.type)) {
      toast.error("Only PDF, PNG, JPEG, and XLSX files are allowed");
      return;
    }
    if (file.size > 10 * 1024 * 1024) {
      toast.error("File size must be under 10 MB");
      return;
    }
    uploadFile.value = file;
  }
}

async function uploadDocument() {
  if (!uploadFile.value || !order.value?.id) return;
  isUploading.value = true;
  try {
    const doc = await purchaseOrderService.uploadDocument(
      order.value.id,
      uploadFile.value,
      docType.value,
    );
    order.value.documents = [...(order.value.documents ?? []), doc];
    uploadFile.value = null;
    toast.success("Document uploaded");
  } catch {
    toast.error("Failed to upload document");
  } finally {
    isUploading.value = false;
  }
}

async function deleteDocument(docId: number) {
  isDeletingDocId.value = docId;
  try {
    await purchaseOrderService.deleteDocument(docId);
    if (order.value) {
      order.value.documents = (order.value.documents ?? []).filter(
        (d) => d.id !== docId,
      );
    }
    toast.success("Document deleted");
  } catch {
    toast.error("Failed to delete document");
  } finally {
    isDeletingDocId.value = null;
  }
}

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat("en-SG", {
    style: "currency",
    currency: "SGD",
  }).format(amount);
}

function formatDate(date: string | null | undefined): string {
  if (!date) return "—";
  return new Date(date).toLocaleDateString("en-SG", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

function formatDateTime(date: string | null | undefined): string {
  if (!date) return "—";
  return new Date(date).toLocaleString("en-SG", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function approvalIcon(action: number | null | undefined): string {
  if (action === 0) return "check_circle";
  if (action === 1) return "cancel";
  return "hourglass_top";
}

function approvalColor(action: number | null | undefined): string {
  if (action === 0) return "text-emerald-500";
  if (action === 1) return "text-red-500";
  return "text-slate-300";
}

function downloadUrl(docId: number): string {
  return getBackendUrl("main", `/api/Document/DownloadFile/${docId}`);
}
</script>

<template>
  <div class="space-y-6">
    <!-- Breadcrumbs -->
    <nav class="flex items-center gap-2 text-sm">
      <router-link
        :to="{ name: 'order-history' }"
        class="inline-flex min-h-10 items-center rounded-lg px-1 text-blue-600 hover:underline"
        >Orders</router-link
      >
      <span class="text-slate-400">/</span>
      <span class="text-slate-600">{{ order?.poNumber ?? "Loading..." }}</span>
    </nav>

    <div v-if="loading" class="flex justify-center py-16">
      <div
        class="size-10 animate-spin rounded-full border-4 border-accent/30 border-t-accent"
      ></div>
    </div>

    <template v-else-if="order">
      <!-- Header -->
      <div
        class="flex flex-col gap-4 rounded-2xl border border-slate-100 bg-white p-6 shadow-soft sm:flex-row sm:items-center sm:justify-between"
      >
        <div>
          <div class="flex items-center gap-3 flex-wrap">
            <h1 class="text-2xl font-extrabold text-slate-800">
              {{ order.poNumber }}
            </h1>
            <span
              class="rounded-lg px-3 py-1 text-xs font-bold"
              :class="getPurchaseOrderStatusClass(order.statusName)"
              >{{ getPurchaseOrderStatusLabel(order.statusName) }}</span
            >
          </div>
          <p class="mt-1 text-sm text-slate-500">
            Requested by {{ order.requestedByName ?? "—" }} on
            {{ formatDate(order.requestDate) }}
          </p>
        </div>
        <div class="text-right">
          <p class="text-xs text-slate-400">Total Amount</p>
          <p class="text-3xl font-extrabold text-slate-800">
            {{ formatCurrency(order.totalAmount ?? 0) }}
          </p>
        </div>
      </div>

      <div class="grid gap-6 lg:grid-cols-[1.2fr,1fr]">
        <!-- Left Column -->
        <div class="space-y-6">
          <!-- Line Items -->
          <section
            class="rounded-2xl border border-slate-100 bg-white p-6 shadow-soft"
          >
            <h2 class="text-lg font-bold text-slate-800 mb-4">Line Items</h2>
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-slate-100">
                  <th class="py-2 text-left text-xs font-bold text-slate-400">
                    #
                  </th>
                  <th class="py-2 text-left text-xs font-bold text-slate-400">
                    Item
                  </th>
                  <th class="py-2 text-right text-xs font-bold text-slate-400">
                    Qty
                  </th>
                  <th class="py-2 text-right text-xs font-bold text-slate-400">
                    Unit Price
                  </th>
                  <th class="py-2 text-right text-xs font-bold text-slate-400">
                    Total
                  </th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="line in order.lines"
                  :key="line.lineNumber"
                  class="border-b border-slate-50"
                >
                  <td class="py-2.5 text-slate-400">
                    {{ line.lineNumber }}
                  </td>
                  <td class="py-2.5">
                    <p class="font-medium text-slate-800">
                      {{ line.itemName }}
                    </p>
                    <p v-if="line.description" class="text-xs text-slate-400">
                      {{ line.description }}
                    </p>
                  </td>
                  <td class="py-2.5 text-right text-slate-600">
                    {{ line.quantity }}
                    <span
                      v-if="line.unitOfMeasure"
                      class="text-xs text-slate-400"
                    >
                      {{ line.unitOfMeasure }}
                    </span>
                  </td>
                  <td class="py-2.5 text-right text-slate-600">
                    {{ formatCurrency(line.unitPrice) }}
                  </td>
                  <td class="py-2.5 text-right font-semibold text-slate-800">
                    {{ formatCurrency(line.lineTotal) }}
                  </td>
                </tr>
              </tbody>
              <tfoot>
                <tr class="border-t-2 border-slate-200">
                  <td
                    colspan="4"
                    class="py-3 text-right font-bold text-slate-600"
                  >
                    Total:
                  </td>
                  <td
                    class="py-3 text-right text-lg font-extrabold text-slate-800"
                  >
                    {{ formatCurrency(order.totalAmount ?? 0) }}
                  </td>
                </tr>
              </tfoot>
            </table>
          </section>

          <!-- Documents -->
          <section
            class="rounded-2xl border border-slate-100 bg-white p-6 shadow-soft"
          >
            <h2 class="text-lg font-bold text-slate-800 mb-4">Documents</h2>

            <!-- Upload -->
            <div
              class="mb-4 rounded-xl border-2 border-dashed border-slate-200 p-4"
            >
              <div class="flex flex-wrap items-end gap-3">
                <div class="flex-1 min-w-[180px]">
                  <label class="mb-1 block text-xs font-semibold text-slate-500"
                    >File (PDF, PNG, JPEG, XLSX — max 10 MB)</label
                  >
                  <input
                    type="file"
                    accept=".pdf,.png,.jpg,.jpeg,.xlsx"
                    class="min-h-11 w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm file:mr-3 file:rounded-lg file:border-0 file:bg-slate-100 file:px-3 file:py-1 file:text-xs file:font-semibold"
                    @change="onFileChange"
                  />
                </div>
                <div class="min-w-[120px]">
                  <label class="mb-1 block text-xs font-semibold text-slate-500"
                    >Type</label
                  >
                  <AppSelect
                    v-model="docType"
                    :options="[
                      { value: 'Quote', label: 'Quote' },
                      { value: 'Invoice', label: 'Invoice' },
                      { value: 'Receipt', label: 'Receipt' },
                      { value: 'Other', label: 'Other' },
                    ]"
                    placeholder="Select type"
                  />
                </div>
                <AppButton
                  variant="primary"
                  size="sm"
                  :disabled="!uploadFile || isUploading"
                  @click="uploadDocument"
                >
                  {{ isUploading ? "Uploading..." : "Upload" }}
                </AppButton>
              </div>
            </div>

            <!-- Document List -->
            <div
              v-if="!order.documents?.length"
              class="py-8 text-center text-sm text-slate-400"
            >
              No documents uploaded yet
            </div>
            <div v-else class="space-y-2">
              <div
                v-for="doc in order.documents"
                :key="doc.id"
                class="flex items-center gap-3 rounded-xl bg-slate-50 p-3"
              >
                <span class="material-symbols-outlined text-xl text-slate-400"
                  >description</span
                >
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium text-slate-800 truncate">
                    {{ doc.userFileName }}
                  </p>
                  <p class="text-xs text-slate-400">
                    {{ formatBytes(doc.fileSize) }}
                    <span v-if="doc.documentType" class="ml-2">{{
                      doc.documentType
                    }}</span>
                  </p>
                </div>
                <a
                  :href="downloadUrl(doc.id)"
                  target="_blank"
                  rel="noopener noreferrer"
                  class="inline-flex h-11 w-11 items-center justify-center rounded-lg transition-colors hover:bg-slate-200"
                >
                  <span class="material-symbols-outlined text-lg text-slate-500"
                    >download</span
                  >
                </a>
                <button
                  class="inline-flex h-11 w-11 items-center justify-center rounded-lg transition-colors hover:bg-red-50"
                  :disabled="isDeletingDocId === doc.id"
                  @click="deleteDocument(doc.id)"
                >
                  <span class="material-symbols-outlined text-lg text-red-400"
                    >delete</span
                  >
                </button>
              </div>
            </div>
          </section>
        </div>

        <!-- Right Column -->
        <div class="space-y-6">
          <!-- Order Info -->
          <section
            class="rounded-2xl border border-slate-100 bg-white p-6 shadow-soft"
          >
            <h2 class="text-lg font-bold text-slate-800 mb-4">Order Details</h2>
            <dl class="space-y-3 text-sm">
              <div class="flex justify-between">
                <dt class="text-slate-400">Vendor</dt>
                <dd class="font-medium text-slate-800">
                  {{ order.vendorName }}
                </dd>
              </div>
              <div class="flex justify-between">
                <dt class="text-slate-400">Delivery Location</dt>
                <dd class="font-medium text-slate-800">
                  {{ order.deliveryAddress ?? "—" }}
                </dd>
              </div>
              <div class="flex justify-between">
                <dt class="text-slate-400">Expected Delivery</dt>
                <dd class="font-medium text-slate-800">
                  {{ formatDate(order.expectedDeliveryDate) }}
                </dd>
              </div>
              <div v-if="order.notes" class="border-t border-slate-100 pt-3">
                <dt class="text-slate-400 mb-1">Notes</dt>
                <dd class="font-medium text-slate-800">
                  {{ order.notes }}
                </dd>
              </div>
              <div
                v-if="order.rejectionReason"
                class="rounded-lg bg-red-50 p-3 border border-red-100"
              >
                <dt class="text-red-600 text-xs font-bold mb-1">
                  Rejection Reason
                </dt>
                <dd class="text-red-800">
                  {{ order.rejectionReason }}
                </dd>
              </div>
            </dl>
          </section>

          <!-- Approval Timeline -->
          <section
            class="rounded-2xl border border-slate-100 bg-white p-6 shadow-soft"
          >
            <h2 class="text-lg font-bold text-slate-800 mb-4">
              Approval Timeline
            </h2>
            <div
              v-if="!order.approvals?.length"
              class="py-6 text-center text-sm text-slate-400"
            >
              No approval chain yet
            </div>
            <div v-else class="relative space-y-0">
              <div
                v-for="(approval, idx) in order.approvals"
                :key="idx"
                class="flex gap-4 pb-6 last:pb-0"
              >
                <!-- Timeline line -->
                <div class="flex flex-col items-center">
                  <span
                    class="material-symbols-outlined text-xl"
                    :class="approvalColor(approval.action)"
                    >{{ approvalIcon(approval.action) }}</span
                  >
                  <div
                    v-if="idx < (order.approvals?.length ?? 0) - 1"
                    class="mt-1 w-px flex-1 bg-slate-200"
                  ></div>
                </div>
                <!-- Content -->
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-semibold text-slate-800">
                    {{ approval.approvalStage }}
                  </p>
                  <p class="text-xs text-slate-500">
                    <template v-if="approval.approverName">
                      {{ approval.approverName }}
                      <span v-if="approval.actionDate">
                        — {{ formatDateTime(approval.actionDate) }}
                      </span>
                    </template>
                    <template v-else> Awaiting review </template>
                  </p>
                  <p
                    v-if="approval.comments"
                    class="mt-1 text-xs text-slate-600 italic"
                  >
                    "{{ approval.comments }}"
                  </p>
                </div>
              </div>
            </div>
          </section>

          <!-- Audit Info -->
          <section
            class="rounded-2xl border border-slate-100 bg-white p-6 shadow-soft"
          >
            <h2 class="text-sm font-bold text-slate-400 mb-3">Audit Trail</h2>
            <dl class="space-y-2 text-xs">
              <div class="flex justify-between">
                <dt class="text-slate-400">Created</dt>
                <dd class="text-slate-600">
                  {{ formatDateTime(order.createdOn) }}
                  <span v-if="order.createdBy" class="text-slate-400"
                    >by {{ order.createdBy }}</span
                  >
                </dd>
              </div>
              <div class="flex justify-between">
                <dt class="text-slate-400">Updated</dt>
                <dd class="text-slate-600">
                  {{ formatDateTime(order.updatedOn) }}
                  <span v-if="order.updatedBy" class="text-slate-400"
                    >by {{ order.updatedBy }}</span
                  >
                </dd>
              </div>
            </dl>
          </section>
        </div>
      </div>
    </template>
  </div>
</template>
