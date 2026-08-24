<script setup lang="ts">
import { ref, onMounted, computed } from "vue";
import { toTypedSchema } from "@vee-validate/zod";
import { useForm } from "vee-validate";
import { z } from "zod";
import { getValidationFieldErrors } from "@nie/platform";
import {
  useToast,
  NieDataTable,
  NieButton,
  NieConfirmDialog,
  NieInput,
  NieModal,
  NieSelect,
  NieTextarea,
} from "@nie/ui";
import catalogItemService, {
  type CatalogItemDto,
} from "@/services/procurement/catalogItemService";
import vendorService, { type VendorDto } from "@/services/procurement/vendorService";
import { useCodeTableOptions } from "@/composables/codes/useCodeTableOptions";
import {
  CodeTableType,
  type CodeTableTypeValue,
} from "@/services/codes/codeTableService";
import { mergeFilterOptions } from "@/utils/listFilterOptions";
import { useServerDataTable } from "@/composables/data-tables/useServerDataTable";

const toast = useToast();

const catalogTable = useServerDataTable<CatalogItemDto>({
  search: catalogItemService.search,
  getFilterOptions: catalogItemService.getFilterOptions,
});
const {
  rows,
  totalItems,
  loading: isLoading,
  error,
  filterOptionPages,
  load: loadCatalogItems,
  loadFilterOptions,
  reload: reloadCatalogItems,
} = catalogTable;
const vendors = ref<VendorDto[]>([]);
const search = ref("");
const selectedFilters = ref<Record<string, Array<string | number | boolean>>>(
  {},
);
const { codeTableOptions, loadCodeTableOptions } = useCodeTableOptions([
  CodeTableType.CatalogCategory,
  CodeTableType.UnitOfMeasure,
] as readonly CodeTableTypeValue[]);

// Modal
const showModal = ref(false);
const isEditing = ref(false);
const isSaving = ref(false);

const emptyItem: CatalogItemDto = {
  name: "",
  sku: "",
  description: null,
  category: null,
  unitOfMeasure: null,
  unitPrice: 0,
  isActive: true,
  vendorId: "",
};
const catalogItemSchema = z.object({
  id: z.string().uuid("Catalog item ID must be a valid UUID").optional(),
  name: z
    .string()
    .trim()
    .min(1, "Item name is required")
    .max(200, "Item name must not exceed 200 characters"),
  sku: z
    .string()
    .trim()
    .min(1, "SKU is required")
    .max(100, "SKU must not exceed 100 characters"),
  description: z.string().max(2_000).nullable().optional(),
  category: z.string().max(100).nullable().optional(),
  unitOfMeasure: z.string().max(50).nullable().optional(),
  unitPrice: z.number().min(0, "Price cannot be negative").max(100_000_000),
  isActive: z.boolean(),
  vendorId: z.string().uuid("Please select a vendor"),
});
const { defineField, errors, handleSubmit, resetForm, setErrors } = useForm({
  validationSchema: toTypedSchema(catalogItemSchema),
  initialValues: { ...emptyItem },
});
const [itemName] = defineField("name");
const [sku] = defineField("sku");
const [description] = defineField("description");
const [category] = defineField("category");
const [unitOfMeasure] = defineField("unitOfMeasure");
const [unitPrice] = defineField("unitPrice");
const [isActive] = defineField("isActive");
const [vendorId] = defineField("vendorId");

// Delete
const showDelete = ref(false);
const isDeleting = ref(false);
const deleteTarget = ref<CatalogItemDto | null>(null);
const deleteDialogOptions = computed(() =>
  showDelete.value
    ? {
        title: "Delete Catalog Item",
        message: `Delete item '${deleteTarget.value?.name ?? ""}'? This cannot be undone.`,
        confirmText: "Delete",
        variant: "danger" as const,
      }
    : null,
);

const columns = [
  { key: "sku", label: "SKU" },
  { key: "name", label: "Name" },
  { key: "vendorName", label: "Vendor" },
  { key: "category", label: "Category" },
  { key: "unitOfMeasure", label: "Unit" },
  { key: "unitPrice", label: "Price" },
  {
    key: "isActive",
    label: "Status",
    chip: {
      toneMap: { true: "success", false: "default" },
      label: (value: unknown) => (value ? "Active" : "Inactive"),
      dot: true,
    },
  },
];

const fallbackCategoryOptions = [
  "Hardware",
  "Software",
  "Furniture",
  "Stationery",
  "Cleaning",
].map((label) => ({
  label,
  value: label,
  count: 0,
}));

const fallbackUnitOptions = ["Each", "Box", "Pack", "Set", "Hour"].map(
  (label) => ({
    label,
    value: label,
    count: 0,
  }),
);

const catalogCategoryOptions = computed(() =>
  mergeFilterOptions(
    codeTableOptions.value[CodeTableType.CatalogCategory] ?? [],
    fallbackCategoryOptions,
  ),
);

const unitOfMeasureOptions = computed(() =>
  mergeFilterOptions(
    codeTableOptions.value[CodeTableType.UnitOfMeasure] ?? [],
    fallbackUnitOptions,
  ),
);

const loadVendorLookup = async () => {
  try {
    vendors.value = await vendorService.getLookup();
  } catch {
    toast.error("Failed to load vendor options");
    vendors.value = [];
  }
};

onMounted(async () => {
  await Promise.all([
    loadVendorLookup(),
    loadCodeTableOptions(),
  ]);
});

const openCreate = () => {
  resetForm({
    values: { ...emptyItem, vendorId: vendors.value[0]?.id ?? "" },
  });
  isEditing.value = false;
  showModal.value = true;
};

const openEdit = (row: CatalogItemDto) => {
  resetForm({ values: { ...row } });
  isEditing.value = true;
  showModal.value = true;
};

const closeModal = () => {
  showModal.value = false;
};

const save = handleSubmit(
  async (values) => {
    isSaving.value = true;
    try {
      await catalogItemService.save(values);
      toast.success(isEditing.value ? "Item updated" : "Item created");
      showModal.value = false;
      await reloadCatalogItems();
    } catch (error) {
      setErrors(getValidationFieldErrors(error));
      toast.error("Failed to save catalog item");
    } finally {
      isSaving.value = false;
    }
  },
  () => toast.error("Please correct the highlighted fields"),
);

const requestDelete = (row: CatalogItemDto) => {
  deleteTarget.value = row;
  showDelete.value = true;
};

const confirmDelete = async () => {
  if (!deleteTarget.value?.id) return;
  isDeleting.value = true;
  try {
    await catalogItemService.delete(deleteTarget.value.id);
    toast.success("Catalog item deleted");
    showDelete.value = false;
    deleteTarget.value = null;
    await reloadCatalogItems();
  } catch {
    toast.error("Failed to delete catalog item");
  } finally {
    isDeleting.value = false;
  }
};

function formatCurrency(val: number): string {
  return new Intl.NumberFormat("en-SG", {
    style: "currency",
    currency: "SGD",
  }).format(val);
}

const vendorSelectOptions = computed(() =>
  vendors.value
    .filter((vendor: VendorDto) => vendor.id !== undefined)
    .map((vendor: VendorDto) => ({
      value: vendor.id as string,
      label: vendor.name,
    })),
);
const categorySelectOptions = computed(() =>
  catalogCategoryOptions.value.map((option) => ({
    value: String(option.value),
    label: option.label,
  })),
);
const unitSelectOptions = computed(() =>
  unitOfMeasureOptions.value.map((option) => ({
    value: String(option.value),
    label: option.label,
  })),
);
</script>

<template>
  <div class="space-y-4 flex flex-col flex-1 min-h-0">
    <NieDataTable
      preference-key="procurement.catalog-items"
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
      :loading="isLoading"
      :error="error"
      @create="openCreate"
      @edit="openEdit"
      @delete="requestDelete"
      @query-change="loadCatalogItems"
      @filter-options-request="loadFilterOptions"
      @retry="reloadCatalogItems"
    >
      <template #cell-unitPrice="{ value }">
        <span class="font-semibold">{{ formatCurrency(value) }}</span>
      </template>

    </NieDataTable>

    <NieModal
      v-model="showModal"
      size="xl"
      placement="mobile-sheet"
      :title="isEditing ? 'Edit Catalog Item' : 'New Catalog Item'"
    >
            <div class="space-y-4">
              <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <NieInput
                  v-model="itemName"
                  label="Item Name *"
                  placeholder="Enter item name"
                  :error="errors.name"
                />
                <NieInput
                  v-model="sku"
                  label="SKU *"
                  placeholder="e.g. ITM-001"
                  :disabled="isEditing"
                  :error="errors.sku"
                />
              </div>

              <NieTextarea
                v-model="description"
                label="Description"
                placeholder="Item description"
                :rows="3"
                :error="errors.description"
              />

              <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <NieSelect
                  v-model="vendorId"
                  label="Vendor *"
                  :options="vendorSelectOptions"
                  placeholder="Select vendor"
                  :searchable="true"
                  :error="errors.vendorId"
                />
                <NieSelect
                  v-model="category"
                  label="Category"
                  :options="categorySelectOptions"
                  placeholder="Select category"
                  :error="errors.category"
                />
              </div>

              <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <NieSelect
                  v-model="unitOfMeasure"
                  label="Unit of Measure"
                  :options="unitSelectOptions"
                  placeholder="Select unit"
                  :error="errors.unitOfMeasure"
                />
                <NieInput
                  v-model.number="unitPrice"
                  label="Unit Price (SGD) *"
                  type="number"
                  placeholder="0.00"
                  :error="errors.unitPrice"
                />
              </div>

              <div class="flex items-center gap-2">
                <input
                  id="item-active"
                  v-model="isActive"
                  type="checkbox"
                  class="size-4 rounded border-secondary-300"
                />
                <label
                  for="item-active"
                  class="text-sm text-secondary-600 dark:text-secondary-300"
                  >Active</label
                >
              </div>
            </div>

      <template #footer>
            <div class="flex flex-wrap justify-end gap-3">
              <NieButton variant="secondary" @click="closeModal"
                >Cancel</NieButton
              >
              <NieButton variant="primary" :loading="isSaving" @click="save">
                {{ isEditing ? "Update" : "Create" }}
              </NieButton>
            </div>
      </template>
    </NieModal>

    <NieConfirmDialog
      :options="deleteDialogOptions"
      :loading="isDeleting"
      @confirm="confirmDelete"
      @cancel="showDelete = false"
    />
  </div>
</template>
