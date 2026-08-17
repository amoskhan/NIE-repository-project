<script setup lang="ts">
import { ref, onMounted, computed } from "vue";
import {
  useToast,
  AppDataTable,
  AppButton,
  AppInput,
  AppBadge,
  AppSelect,
} from "@apptemplate/ui";
import ConfirmDialog from "@/components/common/ConfirmDialog.vue";
import catalogItemService, {
  type CatalogItemDto,
} from "@/services/catalogItemService";
import vendorService, { type VendorDto } from "@/services/vendorService";
import { useCodeTableOptions } from "@/composables/useCodeTableOptions";
import {
  CodeTableType,
  type CodeTableTypeValue,
} from "@/services/codeTableService";
import {
  buildFilterOptions,
  mergeFilterOptions,
} from "@/utils/listFilterOptions";

const toast = useToast();

const isLoading = ref(true);
const rows = ref<CatalogItemDto[]>([]);
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
  vendorId: 0,
};
const form = ref<CatalogItemDto>({ ...emptyItem });

// Delete
const showDelete = ref(false);
const isDeleting = ref(false);
const deleteTarget = ref<CatalogItemDto | null>(null);

const columns = [
  { key: "sku", label: "SKU" },
  { key: "name", label: "Name" },
  { key: "vendorName", label: "Vendor" },
  { key: "category", label: "Category" },
  { key: "unitOfMeasure", label: "Unit" },
  { key: "unitPrice", label: "Price" },
  { key: "isActive", label: "Status" },
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
    buildFilterOptions<CatalogItemDto>(rows.value, (row) => row.category),
    fallbackCategoryOptions,
  ),
);

const unitOfMeasureOptions = computed(() =>
  mergeFilterOptions(
    codeTableOptions.value[CodeTableType.UnitOfMeasure] ?? [],
    buildFilterOptions<CatalogItemDto>(rows.value, (row) => row.unitOfMeasure),
    fallbackUnitOptions,
  ),
);

const filterGroups = computed(() => [
  {
    key: "vendorName",
    label: "Vendor",
    options: buildFilterOptions<CatalogItemDto>(
      rows.value,
      (row) => row.vendorName,
    ),
  },
  {
    key: "category",
    label: "Category",
    options: catalogCategoryOptions.value,
  },
  {
    key: "unitOfMeasure",
    label: "Unit",
    options: unitOfMeasureOptions.value,
  },
  {
    key: "isActive",
    label: "Status",
    options: buildFilterOptions<CatalogItemDto>(
      rows.value,
      (row) => row.isActive,
      (value) => ((value as boolean) ? "Active" : "Inactive"),
    ),
  },
]);

const fetchAll = async () => {
  isLoading.value = true;
  try {
    const [items, vendorList] = await Promise.all([
      catalogItemService.getAll(),
      vendorService.getAll(),
    ]);
    rows.value = items;
    vendors.value = vendorList;
  } catch {
    toast.error("Failed to load catalog items");
    rows.value = [];
  } finally {
    isLoading.value = false;
  }
};

onMounted(async () => {
  await Promise.all([fetchAll(), loadCodeTableOptions()]);
});

const openCreate = () => {
  form.value = { ...emptyItem, vendorId: vendors.value[0]?.id ?? 0 };
  isEditing.value = false;
  showModal.value = true;
};

const openEdit = (row: CatalogItemDto) => {
  form.value = { ...row };
  isEditing.value = true;
  showModal.value = true;
};

const closeModal = () => {
  showModal.value = false;
};

const validate = (): string | null => {
  if (!form.value.name?.trim()) return "Item name is required";
  if (!form.value.sku?.trim()) return "SKU is required";
  if (!form.value.vendorId) return "Please select a vendor";
  if (form.value.unitPrice < 0) return "Price cannot be negative";
  return null;
};

const save = async () => {
  const msg = validate();
  if (msg) {
    toast.error(msg);
    return;
  }
  isSaving.value = true;
  try {
    await catalogItemService.save(form.value);
    toast.success(isEditing.value ? "Item updated" : "Item created");
    showModal.value = false;
    await fetchAll();
  } catch {
    toast.error("Failed to save catalog item");
  } finally {
    isSaving.value = false;
  }
};

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
    await fetchAll();
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
      value: vendor.id as number,
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
    <AppDataTable
      class="flex-1 min-h-0"
      v-model:search="search"
      v-model:selected-filters="selectedFilters"
      :columns="columns"
      :data="rows"
      row-key="id"
      :loading="isLoading"
      :filter-groups="filterGroups"
      @create="openCreate"
      @edit="openEdit"
      @delete="requestDelete"
      @retry="fetchAll"
    >
      <template #cell-unitPrice="{ value }">
        <span class="font-semibold">{{ formatCurrency(value) }}</span>
      </template>

      <template #cell-isActive="{ value }">
        <AppBadge :variant="value ? 'success' : 'default'" rounded>
          {{ value ? "Active" : "Inactive" }}
        </AppBadge>
      </template>
    </AppDataTable>

    <!-- Catalog Item Modal -->
    <Teleport to="body">
      <Transition name="fade">
        <div
          v-if="showModal"
          class="fixed inset-0 z-50 flex items-center justify-center bg-black/40"
          @click.self="closeModal"
        >
          <div
            class="mx-4 w-full max-w-xl rounded-2xl bg-white p-6 shadow-xl dark:bg-slate-800"
          >
            <div class="flex items-center justify-between mb-6">
              <h2 class="text-lg font-bold text-slate-800 dark:text-slate-100">
                {{ isEditing ? "Edit Catalog Item" : "New Catalog Item" }}
              </h2>
              <button
                class="rounded-lg p-1.5 hover:bg-slate-100 dark:hover:bg-slate-700"
                @click="closeModal"
              >
                <span class="material-symbols-outlined text-xl text-slate-400"
                  >close</span
                >
              </button>
            </div>

            <div class="space-y-4">
              <div class="grid grid-cols-2 gap-4">
                <AppInput
                  v-model="form.name"
                  label="Item Name *"
                  placeholder="Enter item name"
                />
                <AppInput
                  v-model="form.sku"
                  label="SKU *"
                  placeholder="e.g. ITM-001"
                  :disabled="isEditing"
                />
              </div>

              <AppInput
                v-model="form.description"
                label="Description"
                placeholder="Item description"
              />

              <div class="grid grid-cols-2 gap-4">
                <AppSelect
                  v-model="form.vendorId"
                  label="Vendor *"
                  :options="vendorSelectOptions"
                  placeholder="Select vendor"
                  :searchable="true"
                />
                <AppSelect
                  v-model="form.category"
                  label="Category"
                  :options="categorySelectOptions"
                  placeholder="Select category"
                />
              </div>

              <div class="grid grid-cols-2 gap-4">
                <AppSelect
                  v-model="form.unitOfMeasure"
                  label="Unit of Measure"
                  :options="unitSelectOptions"
                  placeholder="Select unit"
                />
                <AppInput
                  v-model.number="form.unitPrice"
                  label="Unit Price (SGD) *"
                  type="number"
                  placeholder="0.00"
                />
              </div>

              <div class="flex items-center gap-2">
                <input
                  id="item-active"
                  v-model="form.isActive"
                  type="checkbox"
                  class="size-4 rounded border-slate-300"
                />
                <label
                  for="item-active"
                  class="text-sm text-slate-600 dark:text-slate-300"
                  >Active</label
                >
              </div>
            </div>

            <div class="mt-6 flex justify-end gap-3">
              <AppButton variant="secondary" @click="closeModal"
                >Cancel</AppButton
              >
              <AppButton variant="primary" :disabled="isSaving" @click="save">
                {{ isSaving ? "Saving..." : isEditing ? "Update" : "Create" }}
              </AppButton>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <ConfirmDialog
      :show="showDelete"
      title="Delete Catalog Item"
      :message="`Delete item '${deleteTarget?.name ?? ''}'? This cannot be undone.`"
      confirm-text="Delete"
      variant="danger"
      :loading="isDeleting"
      @confirm="confirmDelete"
      @close="showDelete = false"
    />
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
