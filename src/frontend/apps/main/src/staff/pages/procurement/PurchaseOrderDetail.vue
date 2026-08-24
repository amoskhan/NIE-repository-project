<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { useRoute } from "vue-router";
import {
  useToast,
  NieButton,
  NieLoaderSymbol,
  NieResultState,
  NieSelect,
} from "@nie/ui";
import { getBackendUrl } from "@nie/platform";
import purchaseOrderService from "@/services/procurement/purchaseOrderService";
import type { PurchaseOrderDto } from "@/services/procurement/purchaseOrderService";
import {
  getPurchaseOrderStatusClass,
  getPurchaseOrderStatusLabel,
} from "@/types/procurementStatus";

const route = useRoute();
const toast = useToast();

const loading = ref(true);
const loadError = ref<string | null>(null);
const order = ref<PurchaseOrderDto | null>(null);
const uploadFile = ref<File | null>(null);
const docType = ref("Quote");
const isUploading = ref(false);
const isDeletingDocId = ref<string | null>(null);

const orderId = computed(() => String(route.params.id ?? ""));

async function loadOrder() {
  loading.value = true;
  loadError.value = null;
  try {
    order.value = await purchaseOrderService.getById(orderId.value);
  } catch {
    order.value = null;
    loadError.value = "This purchase order could not be found or loaded.";
    toast.error(loadError.value);
  } finally {
    loading.value = false;
  }
}

onMounted(loadOrder);

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

async function deleteDocument(docId: string) {
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
  if (action === 0) return "text-success-500";
  if (action === 1) return "text-danger-500";
  return "text-secondary-300";
}

function downloadUrl(docId: string): string {
  return getBackendUrl("main", `/api/Document/DownloadFile/${docId}`);
}
</script>

<template>
  <div class="space-y-6">
    <!-- Breadcrumbs -->
    <nav class="flex items-center gap-2 text-sm">
      <router-link
        :to="{ name: 'order-history' }"
        class="inline-flex min-h-10 items-center rounded-lg px-1 text-info-600 hover:underline"
        >Orders</router-link
      >
      <span class="text-secondary-400">/</span>
      <span class="text-secondary-600">{{ order?.poNumber ?? "Loading..." }}</span>
    </nav>

    <div v-if="loading" class="flex justify-center py-16">
      <NieLoaderSymbol size="lg" variant="brand" label="Loading purchase order" />
    </div>

    <NieResultState
      v-else-if="loadError"
      variant="error"
      title="Purchase order unavailable"
      :description="loadError"
    >
      <template #actions>
        <NieButton variant="outline" @click="loadOrder">Try again</NieButton>
      </template>
    </NieResultState>

    <template v-else-if="order">
      <!-- Header -->
      <div
        class="flex flex-col gap-4 rounded-2xl border border-secondary-100 bg-white p-6 shadow-soft sm:flex-row sm:items-center sm:justify-between"
      >
        <div>
          <div class="flex items-center gap-3 flex-wrap">
            <h1 class="text-2xl font-bold text-secondary-800">
              {{ order.poNumber }}
            </h1>
            <span
              class="rounded-lg px-3 py-1 text-xs font-bold"
              :class="getPurchaseOrderStatusClass(order.statusName)"
              >{{ getPurchaseOrderStatusLabel(order.statusName) }}</span
            >
          </div>
          <p class="mt-1 text-sm text-secondary-500">
            Requested by {{ order.requestedByName ?? "—" }} on
            {{ formatDate(order.requestDate) }}
          </p>
        </div>
        <div class="text-right">
          <p class="text-xs text-secondary-400">Total Amount</p>
          <p class="text-3xl font-bold text-secondary-800">
            {{ formatCurrency(order.totalAmount ?? 0) }}
          </p>
        </div>
      </div>

      <div class="grid gap-6 lg:grid-cols-[1.2fr,1fr]">
        <!-- Left Column -->
        <div class="space-y-6">
          <!-- Line Items -->
          <section
            class="rounded-2xl border border-secondary-100 bg-white p-6 shadow-soft"
          >
            <h2 class="text-lg font-bold text-secondary-800 mb-4">Line Items</h2>
            <div
              class="max-h-[min(32rem,calc(100dvh-20rem))] overflow-auto overscroll-contain"
              role="region"
              tabindex="0"
              aria-label="Purchase order line items"
            >
              <table class="min-w-[36rem] w-full text-sm">
                <thead class="sticky top-0 z-10 bg-white">
                  <tr class="border-b border-secondary-100">
                    <th class="py-2 text-left text-xs font-bold text-secondary-400">
                      #
                    </th>
                    <th class="py-2 text-left text-xs font-bold text-secondary-400">
                      Item
                    </th>
                    <th
                      class="py-2 text-right text-xs font-bold text-secondary-400"
                    >
                      Qty
                    </th>
                    <th
                      class="py-2 text-right text-xs font-bold text-secondary-400"
                    >
                      Unit Price
                    </th>
                    <th
                      class="py-2 text-right text-xs font-bold text-secondary-400"
                    >
                      Total
                    </th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="line in order.lines"
                    :key="line.lineNumber"
                    class="border-b border-secondary-50"
                  >
                    <td class="py-2.5 text-secondary-400">
                      {{ line.lineNumber }}
                    </td>
                    <td class="py-2.5">
                      <p class="font-medium text-secondary-800">
                        {{ line.itemName }}
                      </p>
                      <p v-if="line.description" class="text-xs text-secondary-400">
                        {{ line.description }}
                      </p>
                    </td>
                    <td class="py-2.5 text-right text-secondary-600">
                      {{ line.quantity }}
                      <span
                        v-if="line.unitOfMeasure"
                        class="text-xs text-secondary-400"
                      >
                        {{ line.unitOfMeasure }}
                      </span>
                    </td>
                    <td class="py-2.5 text-right text-secondary-600">
                      {{ formatCurrency(line.unitPrice) }}
                    </td>
                    <td class="py-2.5 text-right font-semibold text-secondary-800">
                      {{ formatCurrency(line.lineTotal) }}
                    </td>
                  </tr>
                </tbody>
                <tfoot class="sticky bottom-0 bg-white">
                  <tr class="border-t-2 border-secondary-200">
                    <td
                      colspan="4"
                      class="py-3 text-right font-bold text-secondary-600"
                    >
                      Total:
                    </td>
                    <td
                      class="py-3 text-right text-lg font-bold text-secondary-800"
                    >
                      {{ formatCurrency(order.totalAmount ?? 0) }}
                    </td>
                  </tr>
                </tfoot>
              </table>
            </div>
          </section>

          <!-- Documents -->
          <section
            class="rounded-2xl border border-secondary-100 bg-white p-6 shadow-soft"
          >
            <h2 class="text-lg font-bold text-secondary-800 mb-4">Documents</h2>

            <!-- Upload -->
            <div
              class="mb-4 rounded-xl border-2 border-dashed border-secondary-200 p-4"
            >
              <div class="flex flex-wrap items-end gap-3">
                <div class="flex-1 min-w-[180px]">
                  <label for="purchase-order-document" class="mb-1 block text-xs font-semibold text-secondary-500"
                    >File (PDF, PNG, JPEG, XLSX — max 10 MB)</label
                  >
                  <input
                    id="purchase-order-document"
                    type="file"
                    accept=".pdf,.png,.jpg,.jpeg,.xlsx"
                    class="min-h-11 w-full rounded-xl border border-secondary-200 bg-white px-3 py-2.5 text-sm file:mr-3 file:rounded-lg file:border-0 file:bg-secondary-100 file:px-3 file:py-1 file:text-xs file:font-semibold"
                    @change="onFileChange"
                  />
                </div>
                <div class="min-w-[120px]">
                  <label class="mb-1 block text-xs font-semibold text-secondary-500"
                    >Type</label
                  >
                  <NieSelect
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
                <NieButton
                  variant="primary"
                  size="sm"
                  :loading="isUploading"
                  :disabled="!uploadFile"
                  @click="uploadDocument"
                >
                  Upload
                </NieButton>
              </div>
            </div>

            <!-- Document List -->
            <div
              v-if="!order.documents?.length"
              class="py-8 text-center text-sm text-secondary-400"
            >
              No documents uploaded yet
            </div>
            <div v-else class="space-y-2">
              <div
                v-for="doc in order.documents"
                :key="doc.id"
                class="flex items-center gap-3 rounded-xl bg-secondary-50 p-3"
              >
                <span class="material-symbols-outlined text-xl text-secondary-400"
                  >description</span
                >
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium text-secondary-800 truncate">
                    {{ doc.userFileName }}
                  </p>
                  <p class="text-xs text-secondary-400">
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
                  class="inline-flex h-11 w-11 items-center justify-center rounded-lg transition-colors hover:bg-secondary-200"
                >
                  <span class="material-symbols-outlined text-lg text-secondary-500"
                    >download</span
                  >
                </a>
                <button
                  type="button"
                  class="inline-flex h-11 w-11 items-center justify-center rounded-lg transition-colors hover:bg-danger-50"
                  :disabled="isDeletingDocId === doc.id"
                  aria-label="Delete document"
                  @click="deleteDocument(doc.id)"
                >
                  <NieLoaderSymbol
                    v-if="isDeletingDocId === doc.id"
                    size="sm"
                    label="Deleting document"
                  />
                  <span v-else class="material-symbols-outlined text-lg text-danger-400"
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
            class="rounded-2xl border border-secondary-100 bg-white p-6 shadow-soft"
          >
            <h2 class="text-lg font-bold text-secondary-800 mb-4">Order Details</h2>
            <dl class="space-y-3 text-sm">
              <div class="flex justify-between">
                <dt class="text-secondary-400">Vendor</dt>
                <dd class="font-medium text-secondary-800">
                  {{ order.vendorName }}
                </dd>
              </div>
              <div class="flex justify-between">
                <dt class="text-secondary-400">Delivery Location</dt>
                <dd class="font-medium text-secondary-800">
                  {{ order.deliveryAddress ?? "—" }}
                </dd>
              </div>
              <div class="flex justify-between">
                <dt class="text-secondary-400">Expected Delivery</dt>
                <dd class="font-medium text-secondary-800">
                  {{ formatDate(order.expectedDeliveryDate) }}
                </dd>
              </div>
              <div v-if="order.notes" class="border-t border-secondary-100 pt-3">
                <dt class="text-secondary-400 mb-1">Notes</dt>
                <dd class="font-medium text-secondary-800">
                  {{ order.notes }}
                </dd>
              </div>
              <div
                v-if="order.rejectionReason"
                class="rounded-lg bg-danger-50 p-3 border border-danger-100"
              >
                <dt class="text-danger-600 text-xs font-bold mb-1">
                  Rejection Reason
                </dt>
                <dd class="text-danger-800">
                  {{ order.rejectionReason }}
                </dd>
              </div>
            </dl>
          </section>

          <!-- Approval Timeline -->
          <section
            class="rounded-2xl border border-secondary-100 bg-white p-6 shadow-soft"
          >
            <h2 class="text-lg font-bold text-secondary-800 mb-4">
              Approval Timeline
            </h2>
            <div
              v-if="!order.approvals?.length"
              class="py-6 text-center text-sm text-secondary-400"
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
                    class="mt-1 w-px flex-1 bg-secondary-200"
                  ></div>
                </div>
                <!-- Content -->
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-semibold text-secondary-800">
                    {{ approval.approvalStage }}
                  </p>
                  <p class="text-xs text-secondary-500">
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
                    class="mt-1 text-xs text-secondary-600 italic"
                  >
                    "{{ approval.comments }}"
                  </p>
                </div>
              </div>
            </div>
          </section>

          <!-- Audit Info -->
          <section
            class="rounded-2xl border border-secondary-100 bg-white p-6 shadow-soft"
          >
            <h2 class="text-sm font-bold text-secondary-400 mb-3">Audit Trail</h2>
            <dl class="space-y-2 text-xs">
              <div class="flex justify-between">
                <dt class="text-secondary-400">Created</dt>
                <dd class="text-secondary-600">
                  {{ formatDateTime(order.createdOn) }}
                  <span v-if="order.createdBy" class="text-secondary-400"
                    >by {{ order.createdBy }}</span
                  >
                </dd>
              </div>
              <div class="flex justify-between">
                <dt class="text-secondary-400">Updated</dt>
                <dd class="text-secondary-600">
                  {{ formatDateTime(order.updatedOn) }}
                  <span v-if="order.updatedBy" class="text-secondary-400"
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
