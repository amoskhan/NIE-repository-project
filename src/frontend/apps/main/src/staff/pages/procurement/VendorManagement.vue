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
import { useCodeTableOptions } from "@/composables/codes/useCodeTableOptions";
import {
  CodeTableType,
  type CodeTableTypeValue,
} from "@/services/codes/codeTableService";
import vendorService, { type VendorDto } from "@/services/procurement/vendorService";
import { mergeFilterOptions } from "@/utils/listFilterOptions";
import { useServerDataTable } from "@/composables/data-tables/useServerDataTable";

const toast = useToast();

const vendorTable = useServerDataTable<VendorDto>({
  search: vendorService.search,
  getFilterOptions: vendorService.getFilterOptions,
});
const {
  rows,
  totalItems,
  loading: isLoading,
  error,
  filterOptionPages,
  load: loadVendors,
  loadFilterOptions,
  reload: reloadVendors,
} = vendorTable;
const search = ref("");
const selectedFilters = ref<Record<string, Array<string | number | boolean>>>(
  {},
);
const { codeTableOptions, loadCodeTableOptions } = useCodeTableOptions([
  CodeTableType.VendorCategory,
] as readonly CodeTableTypeValue[]);

// Modal state
const showModal = ref(false);
const isEditing = ref(false);
const isSaving = ref(false);

const emptyVendor: VendorDto = {
  name: "",
  code: "",
  contactPerson: null,
  email: null,
  phone: null,
  address: null,
  category: null,
  isActive: true,
  notes: null,
};
const optionalEmailSchema = z.union([
  z.string().trim().email("Enter a valid email address").max(320),
  z.literal(""),
  z.null(),
]);
const vendorSchema = z.object({
  id: z.string().uuid("Vendor ID must be a valid UUID").optional(),
  name: z
    .string()
    .trim()
    .min(1, "Vendor name is required")
    .max(200, "Vendor name must not exceed 200 characters"),
  code: z
    .string()
    .trim()
    .min(1, "Vendor code is required")
    .max(50, "Vendor code must not exceed 50 characters"),
  contactPerson: z.string().max(200).nullable().optional(),
  email: optionalEmailSchema.optional(),
  phone: z.string().max(50).nullable().optional(),
  address: z.string().max(1_000).nullable().optional(),
  category: z.string().max(100).nullable().optional(),
  isActive: z.boolean(),
  notes: z.string().max(2_000).nullable().optional(),
});
const { defineField, errors, handleSubmit, resetForm, setErrors } = useForm({
  validationSchema: toTypedSchema(vendorSchema),
  initialValues: { ...emptyVendor },
});
const [vendorName] = defineField("name");
const [vendorCode] = defineField("code");
const [contactPerson] = defineField("contactPerson");
const [email] = defineField("email");
const [phone] = defineField("phone");
const [address] = defineField("address");
const [category] = defineField("category");
const [isActive] = defineField("isActive");
const [notes] = defineField("notes");

// Delete
const showDelete = ref(false);
const isDeleting = ref(false);
const deleteTarget = ref<VendorDto | null>(null);
const deleteDialogOptions = computed(() =>
  showDelete.value
    ? {
        title: "Delete Vendor",
        message: `Delete vendor '${deleteTarget.value?.name ?? ""}'? This cannot be undone.`,
        confirmText: "Delete",
        variant: "danger" as const,
      }
    : null,
);

const columns = [
  { key: "code", label: "Code" },
  { key: "name", label: "Name" },
  { key: "contactPerson", label: "Contact" },
  { key: "email", label: "Email" },
  { key: "phone", label: "Phone" },
  { key: "category", label: "Category" },
  {
    key: "isActive",
    label: "Status",
    chip: {
      toneMap: { true: "success", false: "default" },
      label: (value: unknown) => (value ? "Active" : "Inactive"),
      dot: true,
    },
  },
  { key: "catalogItemCount", label: "Items" },
];

const fallbackCategoryOptions = [
  "IT Services",
  "Office Supplies",
  "Maintenance",
  "Consulting",
  "Logistics",
].map((label) => ({
  label,
  value: label,
  count: 0,
}));

const vendorCategoryOptions = computed(() =>
  mergeFilterOptions(
    codeTableOptions.value[CodeTableType.VendorCategory] ?? [],
    fallbackCategoryOptions,
  ),
);

onMounted(async () => {
  await loadCodeTableOptions();
});

const openCreate = () => {
  resetForm({ values: { ...emptyVendor } });
  isEditing.value = false;
  showModal.value = true;
};

const openEdit = (row: VendorDto) => {
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
      await vendorService.save(values);
      toast.success(isEditing.value ? "Vendor updated" : "Vendor created");
      showModal.value = false;
      await reloadVendors();
    } catch (error) {
      setErrors(getValidationFieldErrors(error));
      toast.error("Failed to save vendor");
    } finally {
      isSaving.value = false;
    }
  },
  () => toast.error("Please correct the highlighted fields"),
);

const requestDelete = (row: VendorDto) => {
  deleteTarget.value = row;
  showDelete.value = true;
};

const confirmDelete = async () => {
  if (!deleteTarget.value?.id) return;
  isDeleting.value = true;
  try {
    await vendorService.delete(deleteTarget.value.id);
    toast.success("Vendor deleted");
    showDelete.value = false;
    deleteTarget.value = null;
    await reloadVendors();
  } catch {
    toast.error("Failed to delete vendor");
  } finally {
    isDeleting.value = false;
  }
};

const categoryOptions = computed(() => vendorCategoryOptions.value);
const categorySelectOptions = computed(() =>
  categoryOptions.value.map((option) => ({
    value: String(option.value),
    label: option.label,
  })),
);
</script>

<template>
  <div class="space-y-4 flex flex-col flex-1 min-h-0">
    <NieDataTable
      preference-key="procurement.vendors"
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
      @query-change="loadVendors"
      @filter-options-request="loadFilterOptions"
      @retry="reloadVendors"
    >
      <template #cell-catalogItemCount="{ value }">
        <span class="font-semibold">{{ value ?? 0 }}</span>
      </template>
    </NieDataTable>

    <NieModal
      v-model="showModal"
      size="xl"
      placement="mobile-sheet"
      :title="isEditing ? 'Edit Vendor' : 'New Vendor'"
    >
            <div class="space-y-4">
              <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <NieInput
                  v-model="vendorName"
                  label="Vendor Name *"
                  placeholder="Enter vendor name"
                  :error="errors.name"
                />
                <NieInput
                  v-model="vendorCode"
                  label="Vendor Code *"
                  placeholder="e.g. VND-001"
                  :disabled="isEditing"
                  :error="errors.code"
                />
              </div>

              <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <NieInput
                  v-model="contactPerson"
                  label="Contact Person"
                  placeholder="Full name"
                  :error="errors.contactPerson"
                />
                <NieInput
                  v-model="email"
                  label="Email"
                  type="email"
                  placeholder="email@example.com"
                  :error="errors.email"
                />
              </div>

              <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <NieInput
                  v-model="phone"
                  label="Phone"
                  placeholder="+65 XXXX XXXX"
                  :error="errors.phone"
                />
                <NieSelect
                  v-model="category"
                  label="Category"
                  :options="categorySelectOptions"
                  placeholder="Select category"
                  :error="errors.category"
                />
              </div>

              <NieInput
                v-model="address"
                label="Address"
                placeholder="Vendor address"
                :error="errors.address"
              />

              <NieTextarea
                v-model="notes"
                label="Notes"
                placeholder="Additional notes"
                :rows="3"
                :error="errors.notes"
              />

              <div class="flex items-center gap-2">
                <input
                  id="vendor-active"
                  v-model="isActive"
                  type="checkbox"
                  class="size-4 rounded border-secondary-300"
                />
                <label
                  for="vendor-active"
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
