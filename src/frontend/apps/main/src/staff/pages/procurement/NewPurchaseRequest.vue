<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { toTypedSchema } from "@vee-validate/zod";
import { useRouter } from "vue-router";
import { useForm } from "vee-validate";
import { z } from "zod";
import { getValidationFieldErrors } from "@nie/platform";
import {
  useToast,
  NieButton,
  NieInput,
  NieLoaderSymbol,
  NieResultState,
  NieSelect,
  NieTextarea,
} from "@nie/ui";
import vendorService, { type Vendor } from "@/services/procurement/vendorService";
import catalogItemService, {
  type CatalogItem,
} from "@/services/procurement/catalogItemService";
import purchaseOrderService from "@/services/procurement/purchaseOrderService";
import type { PurchaseOrderDto } from "@/services/procurement/purchaseOrderService";

const router = useRouter();
const toast = useToast();

// Steps
const currentStep = ref(0);
const steps = [
  { label: "Select Vendor", icon: "storefront" },
  { label: "Add Line Items", icon: "list_alt" },
  { label: "Delivery Details", icon: "local_shipping" },
  { label: "Review & Submit", icon: "check_circle" },
];

// Data
const vendors = ref<Vendor[]>([]);
const catalogItems = ref<CatalogItem[]>([]);
const loadingVendors = ref(true);
const loadingItems = ref(false);
const vendorLoadError = ref<string | null>(null);
const catalogLoadError = ref<string | null>(null);
const isSaving = ref(false);
const isSubmitting = ref(false);

// Form state
const purchaseOrderLineSchema = z.object({
  id: z.string().uuid("Line ID must be a valid UUID").optional(),
  lineNumber: z.number().int().positive("Line number is required"),
  itemName: z
    .string()
    .trim()
    .min(1, "Item name is required")
    .max(200, "Item name must not exceed 200 characters"),
  description: z.string().max(2_000).nullable().optional(),
  unitOfMeasure: z.string().max(50).nullable().optional(),
  quantity: z
    .number()
    .int("Quantity must be a whole number")
    .positive("Quantity must be greater than zero")
    .max(1_000_000),
  unitPrice: z
    .number()
    .min(0, "Unit price cannot be negative")
    .max(100_000_000),
  lineTotal: z.number().min(0),
  catalogItemId: z
    .string()
    .uuid("Catalog item ID must be a valid UUID")
    .nullable()
    .optional(),
});
const purchaseOrderFormSchema = z.object({
  vendorId: z.string().uuid("Vendor is required"),
  lines: z
    .array(purchaseOrderLineSchema)
    .min(1, "At least one line item is required")
    .max(100, "A purchase order cannot contain more than 100 line items"),
  deliveryAddress: z
    .string()
    .trim()
    .min(1, "Delivery location is required")
    .max(500),
  expectedDeliveryDate: z
    .string()
    .max(10)
    .refine(
      (value) => !value || /^\d{4}-\d{2}-\d{2}$/.test(value),
      "Expected delivery date must be a valid date",
    ),
  notes: z.string().max(2_000, "Notes must not exceed 2,000 characters"),
});
type PurchaseOrderForm = z.infer<typeof purchaseOrderFormSchema>;

const { defineField, errors, handleSubmit, setErrors } =
  useForm<PurchaseOrderForm>({
    validationSchema: toTypedSchema(purchaseOrderFormSchema),
    initialValues: {
      vendorId: "",
      lines: [],
      deliveryAddress: "",
      expectedDeliveryDate: "",
      notes: "",
    },
  });
const [selectedVendorId] = defineField("vendorId");
const [lines] = defineField("lines");
const [deliveryAddress] = defineField("deliveryAddress");
const [expectedDeliveryDate] = defineField("expectedDeliveryDate");
const [notes] = defineField("notes");

const deliveryOptions = ["Main Office", "Warehouse", "Branch Office"];

const selectedVendor = computed(() =>
  vendors.value.find((v) => v.id === selectedVendorId.value),
);

const totalAmount = computed(() =>
  lines.value.reduce((s, l) => s + l.lineTotal, 0),
);

const loadVendors = async () => {
  loadingVendors.value = true;
  vendorLoadError.value = null;
  try {
    vendors.value = await vendorService.getAll();
  } catch {
    vendorLoadError.value = "Vendors could not be loaded.";
    toast.error(vendorLoadError.value);
  } finally {
    loadingVendors.value = false;
  }
};

onMounted(loadVendors);

// Step 1: Vendor selection
const selectVendor = async (vendorId: string) => {
  selectedVendorId.value = vendorId;
  loadingItems.value = true;
  catalogLoadError.value = null;
  try {
    catalogItems.value = await catalogItemService.getByVendor(vendorId);
  } catch {
    catalogLoadError.value = "Catalog items could not be loaded for this vendor.";
    toast.error(catalogLoadError.value);
  } finally {
    loadingItems.value = false;
  }
};

// Step 2: Line items
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

// Navigation
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

// Submit
const saveAsDraft = handleSubmit(async (values) => {
  isSaving.value = true;
  try {
    const payload = buildPayload(values);
    await purchaseOrderService.save(payload);
    toast.success("Purchase order saved as draft");
    router.push({ name: "order-history" });
  } catch (error) {
    applyServerValidationErrors(error);
    toast.error("Failed to save purchase order");
  } finally {
    isSaving.value = false;
  }
}, showValidationSummary);

const submitOrder = handleSubmit(async (values) => {
  isSubmitting.value = true;
  try {
    const payload = buildPayload(values);
    const saved = await purchaseOrderService.save(payload);
    if (saved.id) {
      await purchaseOrderService.submit(saved.id);
    }
    toast.success("Purchase order submitted for approval");
    router.push({ name: "order-history" });
  } catch (error) {
    applyServerValidationErrors(error);
    toast.error("Failed to submit purchase order");
  } finally {
    isSubmitting.value = false;
  }
}, showValidationSummary);

function showValidationSummary() {
  const invalidFields = Object.keys(errors.value);
  if (invalidFields.some((field) => field === "vendorId")) {
    currentStep.value = 0;
  } else if (invalidFields.some((field) => field.startsWith("lines"))) {
    currentStep.value = 1;
  } else if (
    invalidFields.some((field) =>
      ["deliveryAddress", "expectedDeliveryDate", "notes"].includes(field),
    )
  ) {
    currentStep.value = 2;
  }

  toast.error("Please correct the highlighted fields");
}

function applyServerValidationErrors(error: unknown) {
  const serverErrors = getValidationFieldErrors(error);
  if (Object.keys(serverErrors).length === 0) return;
  setErrors(serverErrors);
  showValidationSummary();
}

function getLineError(
  index: number,
  field: "itemName" | "quantity" | "unitPrice",
): string | undefined {
  return (errors.value as Record<string, string | undefined>)[
    `lines[${index}].${field}`
  ];
}

function buildPayload(values: PurchaseOrderForm): PurchaseOrderDto {
  return {
    vendorId: values.vendorId,
    deliveryAddress: values.deliveryAddress,
    expectedDeliveryDate: values.expectedDeliveryDate || null,
    notes: values.notes || null,
    lines: values.lines,
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
        class="inline-flex min-h-10 items-center rounded-lg px-1 text-info-600 hover:underline"
        >Orders</router-link
      >
      <span class="text-secondary-400 dark:text-secondary-400">/</span>
      <span class="text-secondary-600 dark:text-secondary-300">New Purchase Request</span>
    </nav>

    <!-- Step Indicator -->
    <div class="rounded-2xl border border-secondary-100 bg-white p-5 shadow-soft dark:border-secondary-700 dark:bg-secondary-800">
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
                  ? 'bg-success-500 text-on-success'
                  : idx === currentStep
                    ? 'text-on-brand'
                    : 'bg-secondary-100 text-secondary-400 dark:text-secondary-400'
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
              :class="idx <= currentStep ? 'text-secondary-800 dark:text-secondary-100' : 'text-secondary-400 dark:text-secondary-400'"
              >{{ step.label }}</span
            >
          </div>
          <div
            v-if="idx < steps.length - 1"
            class="mx-4 h-px flex-1"
            :class="idx < currentStep ? 'bg-success-300' : 'bg-secondary-200'"
          ></div>
        </div>
      </div>
    </div>

    <!-- Step Content -->
    <div class="rounded-2xl border border-secondary-100 bg-white p-6 shadow-soft dark:border-secondary-700 dark:bg-secondary-800">
      <!-- Step 1: Select Vendor -->
      <div v-if="currentStep === 0">
        <h2 class="text-lg font-bold text-secondary-800 dark:text-secondary-100 mb-1">Select Vendor</h2>
        <p class="text-sm text-secondary-500 dark:text-secondary-300 mb-6">
          Choose a vendor to create a purchase order for
        </p>

        <div v-if="loadingVendors" class="flex justify-center py-12">
          <NieLoaderSymbol size="md" label="Loading vendors" />
        </div>

        <NieResultState
          v-else-if="vendorLoadError"
          compact
          variant="error"
          title="Unable to load vendors"
          :description="vendorLoadError"
        >
          <template #actions>
            <NieButton variant="outline" @click="loadVendors">Try again</NieButton>
          </template>
        </NieResultState>

        <div v-else class="grid gap-3 sm:grid-cols-2">
          <button
            v-for="vendor in vendors.filter((v) => v.isActive)"
            :key="vendor.id"
            class="flex items-start gap-4 rounded-xl border-2 p-4 text-left transition-all hover:shadow-[var(--theme-shadow-card)]"
            :class="
              selectedVendorId === vendor.id
                ? 'border-info-500 bg-info-50'
                : 'border-secondary-100 hover:border-secondary-200 dark:border-secondary-700'
            "
            @click="selectVendor(vendor.id!)"
          >
            <div
              class="flex size-10 shrink-0 items-center justify-center rounded-xl text-on-brand font-bold"
              style="background-color: var(--color-primary)"
            >
              {{ vendor.name.charAt(0) }}
            </div>
            <div class="min-w-0">
              <p class="font-semibold text-secondary-800 dark:text-secondary-100 truncate">
                {{ vendor.name }}
              </p>
              <p class="text-xs text-secondary-500 dark:text-secondary-300">{{ vendor.code }}</p>
              <p v-if="vendor.category" class="mt-1 text-xs text-secondary-400 dark:text-secondary-400">
                {{ vendor.category }}
              </p>
            </div>
            <span
              v-if="selectedVendorId === vendor.id"
              class="material-symbols-outlined ml-auto text-info-500"
              >check_circle</span
            >
          </button>
        </div>
        <p
          v-if="errors.vendorId"
          role="alert"
          class="mt-3 text-sm font-medium text-danger-600"
        >
          {{ errors.vendorId }}
        </p>
      </div>

      <!-- Step 2: Add Line Items -->
      <div v-if="currentStep === 1">
        <h2 class="text-lg font-bold text-secondary-800 dark:text-secondary-100 mb-1">Add Line Items</h2>
        <p class="text-sm text-secondary-500 dark:text-secondary-300 mb-6">
          Add items to this purchase order. Pick from the catalog or add custom
          items.
        </p>

        <div v-if="loadingItems" class="flex justify-center py-8">
          <NieLoaderSymbol size="md" label="Loading catalog items" />
        </div>
        <NieResultState
          v-else-if="catalogLoadError"
          compact
          variant="error"
          title="Unable to load catalog items"
          :description="catalogLoadError"
        >
          <template #actions>
            <NieButton
              variant="outline"
              @click="selectVendor(selectedVendorId)"
            >
              Try again
            </NieButton>
          </template>
        </NieResultState>

        <!-- Quick-add from catalog -->
        <div
          v-else-if="catalogItems.length > 0"
          class="mb-6 rounded-xl bg-secondary-50 p-4"
        >
          <p
            class="text-xs font-bold uppercase tracking-wide text-secondary-400 dark:text-secondary-400 mb-3"
          >
            Quick Add from Catalog
          </p>
          <div class="flex flex-wrap gap-2">
            <button
              v-for="item in catalogItems.filter((i) => i.isActive)"
              :key="item.id"
              class="min-h-10 rounded-lg border border-secondary-200 dark:border-secondary-700 bg-white dark:border-secondary-600 dark:bg-secondary-900 px-3 py-2 text-xs font-medium text-secondary-700 dark:text-secondary-200 transition-colors hover:border-info-300 hover:bg-info-50"
              @click="addLine(item)"
            >
              {{ item.name }}
              <span class="text-caption text-secondary-400 dark:text-secondary-400 ml-1">{{
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
            class="rounded-xl border border-secondary-100 dark:border-secondary-700 p-4"
          >
            <div class="flex items-start gap-4">
              <span
                class="flex size-7 shrink-0 items-center justify-center rounded-full bg-secondary-100 text-xs font-bold text-secondary-500 dark:text-secondary-300"
                >{{ line.lineNumber }}</span
              >
              <div class="flex-1 grid grid-cols-1 gap-3 sm:grid-cols-4">
                <NieInput
                  v-model="line.itemName"
                  label="Item Name *"
                  placeholder="Item name"
                  class="sm:col-span-2"
                  :error="getLineError(idx, 'itemName')"
                />
                <NieInput
                  v-model.number="line.quantity"
                  label="Qty *"
                  type="number"
                  placeholder="1"
                  :error="getLineError(idx, 'quantity')"
                  @update:model-value="updateLineTotal(idx)"
                />
                <NieInput
                  v-model.number="line.unitPrice"
                  label="Unit Price *"
                  type="number"
                  placeholder="0.00"
                  :error="getLineError(idx, 'unitPrice')"
                  @update:model-value="updateLineTotal(idx)"
                />
              </div>
              <div class="flex flex-col items-end gap-1 pt-6">
                <span class="text-sm font-bold text-secondary-700 dark:text-secondary-200">{{
                  formatCurrency(line.lineTotal)
                }}</span>
                <button
                  class="inline-flex h-11 w-11 items-center justify-center rounded-lg transition-colors hover:bg-danger-50"
                  @click="removeLine(idx)"
                >
                  <span class="material-symbols-outlined text-lg text-danger-400"
                    >delete</span
                  >
                </button>
              </div>
            </div>
          </div>
        </div>
        <p
          v-if="errors.lines"
          role="alert"
          class="mt-3 text-sm font-medium text-danger-600"
        >
          {{ errors.lines }}
        </p>

        <button
          class="mt-4 flex items-center gap-2 rounded-xl border-2 border-dashed border-secondary-200 dark:border-secondary-700 px-4 py-3 text-sm font-semibold text-secondary-500 dark:text-secondary-300 hover:border-info-300 hover:text-info-600 transition-colors w-full justify-center"
          @click="addLine()"
        >
          <span class="material-symbols-outlined text-lg">add</span>
          Add Custom Item
        </button>

        <div
          v-if="lines.length > 0"
          class="mt-4 flex justify-end border-t border-secondary-100 dark:border-secondary-700 pt-4"
        >
          <div class="text-right">
            <p class="text-xs text-secondary-400 dark:text-secondary-400">Total Amount</p>
            <p class="text-2xl font-bold text-secondary-800 dark:text-secondary-100">
              {{ formatCurrency(totalAmount) }}
            </p>
          </div>
        </div>
      </div>

      <!-- Step 3: Delivery Details -->
      <div v-if="currentStep === 2">
        <h2 class="text-lg font-bold text-secondary-800 dark:text-secondary-100 mb-1">Delivery Details</h2>
        <p class="text-sm text-secondary-500 dark:text-secondary-300 mb-6">
          Specify where and when items should be delivered
        </p>

        <div class="space-y-4 max-w-lg">
          <NieSelect
            v-model="deliveryAddress"
            label="Delivery Location *"
            :options="
              deliveryOptions.map((loc) => ({ value: loc, label: loc }))
            "
            placeholder="Select delivery location"
            :error="errors.deliveryAddress"
          />

          <NieInput
            v-model="expectedDeliveryDate"
            label="Expected Delivery Date"
            type="date"
            :error="errors.expectedDeliveryDate"
          />

          <NieTextarea
            v-model="notes"
            label="Notes"
            :rows="3"
            placeholder="Any special instructions..."
            :error="errors.notes"
          />
        </div>
      </div>

      <!-- Step 4: Review & Submit -->
      <div v-if="currentStep === 3">
        <h2 class="text-lg font-bold text-secondary-800 dark:text-secondary-100 mb-1">Review & Submit</h2>
        <p class="text-sm text-secondary-500 dark:text-secondary-300 mb-6">
          Review your purchase order before submitting
        </p>

        <div class="space-y-5">
          <!-- Vendor -->
          <div class="rounded-xl bg-secondary-50 p-4">
            <p
              class="text-xs font-bold uppercase tracking-wide text-secondary-400 dark:text-secondary-400 mb-2"
            >
              Vendor
            </p>
            <p class="font-semibold text-secondary-800 dark:text-secondary-100">
              {{ selectedVendor?.name }}
            </p>
            <p class="text-sm text-secondary-500 dark:text-secondary-300">
              {{ selectedVendor?.code }}
            </p>
          </div>

          <!-- Line Items -->
          <div class="rounded-xl bg-secondary-50 p-4">
            <p
              class="text-xs font-bold uppercase tracking-wide text-secondary-400 dark:text-secondary-400 mb-3"
            >
              Line Items ({{ lines.length }})
            </p>
            <div
              class="max-h-[min(28rem,calc(100dvh-22rem))] overflow-auto overscroll-contain"
              role="region"
              tabindex="0"
              aria-label="Purchase request line items"
            >
              <table class="min-w-[36rem] w-full text-sm">
                <thead class="sticky top-0 z-10 bg-secondary-50">
                  <tr class="border-b border-secondary-200 dark:border-secondary-700">
                    <th class="text-left py-2 text-xs font-bold text-secondary-400 dark:text-secondary-400">
                      #
                    </th>
                    <th class="text-left py-2 text-xs font-bold text-secondary-400 dark:text-secondary-400">
                      Item
                    </th>
                    <th
                      class="text-right py-2 text-xs font-bold text-secondary-400 dark:text-secondary-400"
                    >
                      Qty
                    </th>
                    <th
                      class="text-right py-2 text-xs font-bold text-secondary-400 dark:text-secondary-400"
                    >
                      Price
                    </th>
                    <th
                      class="text-right py-2 text-xs font-bold text-secondary-400 dark:text-secondary-400"
                    >
                      Total
                    </th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="line in lines"
                    :key="line.lineNumber"
                    class="border-b border-secondary-100 dark:border-secondary-700"
                  >
                    <td class="py-2 text-secondary-500 dark:text-secondary-300">{{ line.lineNumber }}</td>
                    <td class="py-2 font-medium text-secondary-800 dark:text-secondary-100">
                      {{ line.itemName }}
                    </td>
                    <td class="py-2 text-right text-secondary-600 dark:text-secondary-300">
                      {{ line.quantity }}
                    </td>
                    <td class="py-2 text-right text-secondary-600 dark:text-secondary-300">
                      {{ formatCurrency(line.unitPrice) }}
                    </td>
                    <td class="py-2 text-right font-semibold text-secondary-800 dark:text-secondary-100">
                      {{ formatCurrency(line.lineTotal) }}
                    </td>
                  </tr>
                </tbody>
                <tfoot class="sticky bottom-0 bg-secondary-50">
                  <tr class="border-t-2 border-secondary-200 dark:border-secondary-700">
                    <td
                      colspan="4"
                      class="py-3 text-right font-bold text-secondary-600 dark:text-secondary-300"
                    >
                      Total:
                    </td>
                    <td
                      class="py-3 text-right text-xl font-bold text-secondary-800 dark:text-secondary-100"
                    >
                      {{ formatCurrency(totalAmount) }}
                    </td>
                  </tr>
                </tfoot>
              </table>
            </div>
          </div>

          <!-- Delivery -->
          <div class="rounded-xl bg-secondary-50 p-4">
            <p
              class="text-xs font-bold uppercase tracking-wide text-secondary-400 dark:text-secondary-400 mb-2"
            >
              Delivery
            </p>
            <div class="grid grid-cols-1 gap-4 text-sm sm:grid-cols-2">
              <div>
                <p class="text-secondary-400 dark:text-secondary-400">Location</p>
                <p class="font-medium text-secondary-800 dark:text-secondary-100">
                  {{ deliveryAddress }}
                </p>
              </div>
              <div>
                <p class="text-secondary-400 dark:text-secondary-400">Expected Date</p>
                <p class="font-medium text-secondary-800 dark:text-secondary-100">
                  {{ expectedDeliveryDate || "Not specified" }}
                </p>
              </div>
            </div>
            <div v-if="notes" class="mt-3 text-sm">
              <p class="text-secondary-400 dark:text-secondary-400">Notes</p>
              <p class="font-medium text-secondary-800 dark:text-secondary-100">{{ notes }}</p>
            </div>
          </div>

          <!-- Approval Chain Info -->
          <div class="rounded-xl border border-warning-200 bg-warning-50 p-4">
            <div class="flex items-start gap-3">
              <span class="material-symbols-outlined text-warning-600">info</span>
              <div>
                <p class="text-sm font-semibold text-warning-800">
                  Approval Chain
                </p>
                <p class="text-sm text-warning-700 mt-1">
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
      <NieButton v-if="currentStep > 0" variant="secondary" @click="prev">
        <span class="material-symbols-outlined text-lg mr-1">arrow_back</span>
        Back
      </NieButton>
      <div v-else></div>

      <div class="flex gap-3">
        <NieButton
          v-if="currentStep === steps.length - 1"
          variant="secondary"
          :loading="isSaving"
          :disabled="isSubmitting"
          @click="saveAsDraft"
        >
          Save as Draft
        </NieButton>
        <NieButton
          v-if="currentStep < steps.length - 1"
          variant="primary"
          :disabled="!canNext"
          @click="next"
        >
          Next
          <span class="material-symbols-outlined text-lg ml-1"
            >arrow_forward</span
          >
        </NieButton>
        <NieButton
          v-if="currentStep === steps.length - 1"
          variant="primary"
          :loading="isSubmitting"
          :disabled="isSaving"
          @click="submitOrder"
        >
          Submit for Approval
        </NieButton>
      </div>
    </div>
  </div>
</template>
