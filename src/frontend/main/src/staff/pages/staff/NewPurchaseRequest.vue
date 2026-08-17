<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { useRouter } from "vue-router";
import { useToast, AppButton, AppInput, AppSelect } from "@apptemplate/ui";
import vendorService, { type Vendor } from "@/services/vendorService";
import catalogItemService, {
  type CatalogItem,
} from "@/services/catalogItemService";
import purchaseOrderService from "@/services/purchaseOrderService";
import type {
  PurchaseOrderDto,
  PurchaseOrderLineDto,
} from "@/services/purchaseOrderService";

const router = useRouter();
const toast = useToast();

// ─── Steps ────────────────────────────
const currentStep = ref(0);
const steps = [
  { label: "Select Vendor", icon: "storefront" },
  { label: "Add Line Items", icon: "list_alt" },
  { label: "Delivery Details", icon: "local_shipping" },
  { label: "Review & Submit", icon: "check_circle" },
];

// ─── Data ─────────────────────────────
const vendors = ref<Vendor[]>([]);
const catalogItems = ref<CatalogItem[]>([]);
const loadingVendors = ref(true);
const loadingItems = ref(false);
const isSaving = ref(false);
const isSubmitting = ref(false);

// ─── Form State ───────────────────────
const selectedVendorId = ref<number | null>(null);
const lines = ref<PurchaseOrderLineDto[]>([]);
const deliveryAddress = ref("");
const expectedDeliveryDate = ref("");
const notes = ref("");

const deliveryOptions = ["Main Office", "Warehouse", "Branch Office"];

const selectedVendor = computed(() =>
  vendors.value.find((v) => v.id === selectedVendorId.value),
);

const totalAmount = computed(() =>
  lines.value.reduce((s, l) => s + l.lineTotal, 0),
);

onMounted(async () => {
  try {
    vendors.value = await vendorService.getAll();
  } catch {
    toast.error("Failed to load vendors");
  } finally {
    loadingVendors.value = false;
  }
});

// ─── Step 1: Vendor Selection ─────────
const selectVendor = async (vendorId: number) => {
  selectedVendorId.value = vendorId;
  loadingItems.value = true;
  try {
    catalogItems.value = await catalogItemService.getByVendor(vendorId);
  } catch {
    toast.error("Failed to load catalog items");
  } finally {
    loadingItems.value = false;
  }
};

// ─── Step 2: Line Items ───────────────
const addLine = (item?: CatalogItem) => {
  const lineNumber = lines.value.length + 1;
  lines.value.push({
    lineNumber,
    itemName: item?.name ?? "",
    description: item?.description ?? "",
    unitOfMeasure: item?.unitOfMeasure ?? "Each",
    quantity: 1,
    unitPrice: item?.unitPrice ?? 0,
    lineTotal: item?.unitPrice ?? 0,
    catalogItemId: item?.id ?? null,
  });
};

const updateLineTotal = (idx: number) => {
  const line = lines.value[idx];
  line.lineTotal = Math.round(line.quantity * line.unitPrice * 100) / 100;
};

const removeLine = (idx: number) => {
  lines.value.splice(idx, 1);
  lines.value.forEach((l, i) => (l.lineNumber = i + 1));
};

// ─── Navigation ───────────────────────
const canNext = computed(() => {
  switch (currentStep.value) {
    case 0:
      return !!selectedVendorId.value;
    case 1:
      return (
        lines.value.length > 0 &&
        lines.value.every(
          (l) => l.itemName.trim() && l.quantity > 0 && l.unitPrice >= 0,
        )
      );
    case 2:
      return !!deliveryAddress.value.trim();
    default:
      return true;
  }
});

const next = () => {
  if (currentStep.value < steps.length - 1) currentStep.value++;
};
const prev = () => {
  if (currentStep.value > 0) currentStep.value--;
};

// ─── Submit ───────────────────────────
const saveAsDraft = async () => {
  isSaving.value = true;
  try {
    const payload = buildPayload();
    await purchaseOrderService.save(payload);
    toast.success("Purchase order saved as draft");
    router.push({ name: "order-history" });
  } catch {
    toast.error("Failed to save purchase order");
  } finally {
    isSaving.value = false;
  }
};

const submitOrder = async () => {
  isSubmitting.value = true;
  try {
    const payload = buildPayload();
    const saved = await purchaseOrderService.save(payload);
    if (saved.id) {
      await purchaseOrderService.submit(saved.id);
    }
    toast.success("Purchase order submitted for approval");
    router.push({ name: "order-history" });
  } catch {
    toast.error("Failed to submit purchase order");
  } finally {
    isSubmitting.value = false;
  }
};

function buildPayload(): PurchaseOrderDto {
  return {
    vendorId: selectedVendorId.value!,
    deliveryAddress: deliveryAddress.value,
    expectedDeliveryDate: expectedDeliveryDate.value || null,
    notes: notes.value || null,
    lines: lines.value,
  };
}

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat("en-SG", {
    style: "currency",
    currency: "SGD",
  }).format(amount);
}
</script>

<template>
  <div class="mx-auto max-w-4xl space-y-6">
    <!-- Breadcrumbs -->
    <nav class="flex items-center gap-2 text-sm">
      <router-link
        :to="{ name: 'order-history' }"
        class="inline-flex min-h-10 items-center rounded-lg px-1 text-blue-600 hover:underline"
        >Orders</router-link
      >
      <span class="text-slate-400">/</span>
      <span class="text-slate-600">New Purchase Request</span>
    </nav>

    <!-- Step Indicator -->
    <div class="rounded-2xl border border-slate-100 bg-white p-5 shadow-soft">
      <div class="flex items-center justify-between">
        <div
          v-for="(step, idx) in steps"
          :key="idx"
          class="flex flex-1 items-center"
        >
          <div class="flex items-center gap-3">
            <div
              class="flex size-10 items-center justify-center rounded-full text-sm font-bold transition-all"
              :class="
                idx < currentStep
                  ? 'bg-emerald-500 text-white'
                  : idx === currentStep
                    ? 'text-white'
                    : 'bg-slate-100 text-slate-400'
              "
              :style="
                idx === currentStep
                  ? 'background-color: var(--color-primary)'
                  : ''
              "
            >
              <span
                v-if="idx < currentStep"
                class="material-symbols-outlined text-lg"
                >check</span
              >
              <span v-else class="material-symbols-outlined text-lg">{{
                step.icon
              }}</span>
            </div>
            <span
              class="hidden text-sm font-semibold sm:block"
              :class="idx <= currentStep ? 'text-slate-800' : 'text-slate-400'"
              >{{ step.label }}</span
            >
          </div>
          <div
            v-if="idx < steps.length - 1"
            class="mx-4 h-px flex-1"
            :class="idx < currentStep ? 'bg-emerald-300' : 'bg-slate-200'"
          ></div>
        </div>
      </div>
    </div>

    <!-- Step Content -->
    <div class="rounded-2xl border border-slate-100 bg-white p-6 shadow-soft">
      <!-- Step 1: Select Vendor -->
      <div v-if="currentStep === 0">
        <h2 class="text-lg font-bold text-slate-800 mb-1">Select Vendor</h2>
        <p class="text-sm text-slate-500 mb-6">
          Choose a vendor to create a purchase order for
        </p>

        <div v-if="loadingVendors" class="flex justify-center py-12">
          <div
            class="size-8 animate-spin rounded-full border-4 border-accent/30 border-t-accent"
          ></div>
        </div>

        <div v-else class="grid gap-3 sm:grid-cols-2">
          <button
            v-for="vendor in vendors.filter((v) => v.isActive)"
            :key="vendor.id"
            class="flex items-start gap-4 rounded-xl border-2 p-4 text-left transition-all hover:shadow-md"
            :class="
              selectedVendorId === vendor.id
                ? 'border-blue-500 bg-blue-50'
                : 'border-slate-100 hover:border-slate-200'
            "
            @click="selectVendor(vendor.id!)"
          >
            <div
              class="flex size-10 shrink-0 items-center justify-center rounded-xl text-white font-bold"
              style="background-color: var(--color-primary)"
            >
              {{ vendor.name.charAt(0) }}
            </div>
            <div class="min-w-0">
              <p class="font-semibold text-slate-800 truncate">
                {{ vendor.name }}
              </p>
              <p class="text-xs text-slate-500">{{ vendor.code }}</p>
              <p v-if="vendor.category" class="mt-1 text-xs text-slate-400">
                {{ vendor.category }}
              </p>
            </div>
            <span
              v-if="selectedVendorId === vendor.id"
              class="material-symbols-outlined ml-auto text-blue-500"
              >check_circle</span
            >
          </button>
        </div>
      </div>

      <!-- Step 2: Add Line Items -->
      <div v-if="currentStep === 1">
        <h2 class="text-lg font-bold text-slate-800 mb-1">Add Line Items</h2>
        <p class="text-sm text-slate-500 mb-6">
          Add items to this purchase order. Pick from the catalog or add custom
          items.
        </p>

        <!-- Quick-add from catalog -->
        <div
          v-if="catalogItems.length > 0"
          class="mb-6 rounded-xl bg-slate-50 p-4"
        >
          <p
            class="text-xs font-bold uppercase tracking-wider text-slate-400 mb-3"
          >
            Quick Add from Catalog
          </p>
          <div class="flex flex-wrap gap-2">
            <button
              v-for="item in catalogItems.filter((i) => i.isActive)"
              :key="item.id"
              class="min-h-10 rounded-lg border border-slate-200 bg-white px-3 py-2 text-xs font-medium text-slate-700 transition-colors hover:border-blue-300 hover:bg-blue-50"
              @click="addLine(item)"
            >
              {{ item.name }}
              <span class="text-[10px] text-slate-400 ml-1">{{
                formatCurrency(item.unitPrice)
              }}</span>
            </button>
          </div>
        </div>

        <!-- Line Items Table -->
        <div class="space-y-3">
          <div
            v-for="(line, idx) in lines"
            :key="idx"
            class="rounded-xl border border-slate-100 p-4"
          >
            <div class="flex items-start gap-4">
              <span
                class="flex size-7 shrink-0 items-center justify-center rounded-full bg-slate-100 text-xs font-bold text-slate-500"
                >{{ line.lineNumber }}</span
              >
              <div class="flex-1 grid grid-cols-1 gap-3 sm:grid-cols-4">
                <AppInput
                  v-model="line.itemName"
                  label="Item Name *"
                  placeholder="Item name"
                  class="sm:col-span-2"
                />
                <AppInput
                  v-model.number="line.quantity"
                  label="Qty *"
                  type="number"
                  placeholder="1"
                  @update:model-value="updateLineTotal(idx)"
                />
                <AppInput
                  v-model.number="line.unitPrice"
                  label="Unit Price *"
                  type="number"
                  placeholder="0.00"
                  @update:model-value="updateLineTotal(idx)"
                />
              </div>
              <div class="flex flex-col items-end gap-1 pt-6">
                <span class="text-sm font-bold text-slate-700">{{
                  formatCurrency(line.lineTotal)
                }}</span>
                <button
                  class="inline-flex h-11 w-11 items-center justify-center rounded-lg transition-colors hover:bg-red-50"
                  @click="removeLine(idx)"
                >
                  <span class="material-symbols-outlined text-lg text-red-400"
                    >delete</span
                  >
                </button>
              </div>
            </div>
          </div>
        </div>

        <button
          class="mt-4 flex items-center gap-2 rounded-xl border-2 border-dashed border-slate-200 px-4 py-3 text-sm font-semibold text-slate-500 hover:border-blue-300 hover:text-blue-600 transition-colors w-full justify-center"
          @click="addLine()"
        >
          <span class="material-symbols-outlined text-lg">add</span>
          Add Custom Item
        </button>

        <div
          v-if="lines.length > 0"
          class="mt-4 flex justify-end border-t border-slate-100 pt-4"
        >
          <div class="text-right">
            <p class="text-xs text-slate-400">Total Amount</p>
            <p class="text-2xl font-extrabold text-slate-800">
              {{ formatCurrency(totalAmount) }}
            </p>
          </div>
        </div>
      </div>

      <!-- Step 3: Delivery Details -->
      <div v-if="currentStep === 2">
        <h2 class="text-lg font-bold text-slate-800 mb-1">Delivery Details</h2>
        <p class="text-sm text-slate-500 mb-6">
          Specify where and when items should be delivered
        </p>

        <div class="space-y-4 max-w-lg">
          <AppSelect
            v-model="deliveryAddress"
            label="Delivery Location *"
            :options="
              deliveryOptions.map((loc) => ({ value: loc, label: loc }))
            "
            placeholder="Select delivery location"
          />

          <AppInput
            v-model="expectedDeliveryDate"
            label="Expected Delivery Date"
            type="date"
          />

          <div>
            <label class="mb-1 block text-xs font-semibold text-slate-600"
              >Notes</label
            >
            <textarea
              v-model="notes"
              rows="3"
              class="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm focus:border-blue-400 focus:ring-1 focus:ring-blue-400"
              placeholder="Any special instructions..."
            ></textarea>
          </div>
        </div>
      </div>

      <!-- Step 4: Review & Submit -->
      <div v-if="currentStep === 3">
        <h2 class="text-lg font-bold text-slate-800 mb-1">Review & Submit</h2>
        <p class="text-sm text-slate-500 mb-6">
          Review your purchase order before submitting
        </p>

        <div class="space-y-5">
          <!-- Vendor -->
          <div class="rounded-xl bg-slate-50 p-4">
            <p
              class="text-xs font-bold uppercase tracking-wider text-slate-400 mb-2"
            >
              Vendor
            </p>
            <p class="font-semibold text-slate-800">
              {{ selectedVendor?.name }}
            </p>
            <p class="text-sm text-slate-500">
              {{ selectedVendor?.code }}
            </p>
          </div>

          <!-- Line Items -->
          <div class="rounded-xl bg-slate-50 p-4">
            <p
              class="text-xs font-bold uppercase tracking-wider text-slate-400 mb-3"
            >
              Line Items ({{ lines.length }})
            </p>
            <table class="w-full text-sm">
              <thead>
                <tr class="border-b border-slate-200">
                  <th class="text-left py-2 text-xs font-bold text-slate-400">
                    #
                  </th>
                  <th class="text-left py-2 text-xs font-bold text-slate-400">
                    Item
                  </th>
                  <th class="text-right py-2 text-xs font-bold text-slate-400">
                    Qty
                  </th>
                  <th class="text-right py-2 text-xs font-bold text-slate-400">
                    Price
                  </th>
                  <th class="text-right py-2 text-xs font-bold text-slate-400">
                    Total
                  </th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="line in lines"
                  :key="line.lineNumber"
                  class="border-b border-slate-100"
                >
                  <td class="py-2 text-slate-500">{{ line.lineNumber }}</td>
                  <td class="py-2 font-medium text-slate-800">
                    {{ line.itemName }}
                  </td>
                  <td class="py-2 text-right text-slate-600">
                    {{ line.quantity }}
                  </td>
                  <td class="py-2 text-right text-slate-600">
                    {{ formatCurrency(line.unitPrice) }}
                  </td>
                  <td class="py-2 text-right font-semibold text-slate-800">
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
                    class="py-3 text-right text-xl font-extrabold text-slate-800"
                  >
                    {{ formatCurrency(totalAmount) }}
                  </td>
                </tr>
              </tfoot>
            </table>
          </div>

          <!-- Delivery -->
          <div class="rounded-xl bg-slate-50 p-4">
            <p
              class="text-xs font-bold uppercase tracking-wider text-slate-400 mb-2"
            >
              Delivery
            </p>
            <div class="grid grid-cols-2 gap-4 text-sm">
              <div>
                <p class="text-slate-400">Location</p>
                <p class="font-medium text-slate-800">
                  {{ deliveryAddress }}
                </p>
              </div>
              <div>
                <p class="text-slate-400">Expected Date</p>
                <p class="font-medium text-slate-800">
                  {{ expectedDeliveryDate || "Not specified" }}
                </p>
              </div>
            </div>
            <div v-if="notes" class="mt-3 text-sm">
              <p class="text-slate-400">Notes</p>
              <p class="font-medium text-slate-800">{{ notes }}</p>
            </div>
          </div>

          <!-- Approval Chain Info -->
          <div class="rounded-xl border border-amber-200 bg-amber-50 p-4">
            <div class="flex items-start gap-3">
              <span class="material-symbols-outlined text-amber-600">info</span>
              <div>
                <p class="text-sm font-semibold text-amber-800">
                  Approval Chain
                </p>
                <p class="text-sm text-amber-700 mt-1">
                  After submission, this order will go through:
                  <strong>Manager</strong> &rarr;
                  <strong>Finance</strong> &rarr;
                  <strong>Procurement</strong> for approval.
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Navigation Buttons -->
    <div class="flex items-center justify-between">
      <AppButton v-if="currentStep > 0" variant="secondary" @click="prev">
        <span class="material-symbols-outlined text-lg mr-1">arrow_back</span>
        Back
      </AppButton>
      <div v-else></div>

      <div class="flex gap-3">
        <AppButton
          v-if="currentStep === steps.length - 1"
          variant="secondary"
          :disabled="isSaving"
          @click="saveAsDraft"
        >
          {{ isSaving ? "Saving..." : "Save as Draft" }}
        </AppButton>
        <AppButton
          v-if="currentStep < steps.length - 1"
          variant="primary"
          :disabled="!canNext"
          @click="next"
        >
          Next
          <span class="material-symbols-outlined text-lg ml-1"
            >arrow_forward</span
          >
        </AppButton>
        <AppButton
          v-if="currentStep === steps.length - 1"
          variant="primary"
          :disabled="isSubmitting"
          @click="submitOrder"
        >
          {{ isSubmitting ? "Submitting..." : "Submit for Approval" }}
        </AppButton>
      </div>
    </div>
  </div>
</template>
