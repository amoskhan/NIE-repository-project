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
import { useCodeTableOptions } from "@/composables/useCodeTableOptions";
import {
  CodeTableType,
  type CodeTableTypeValue,
} from "@/services/codeTableService";
import vendorService, { type VendorDto } from "@/services/vendorService";
import {
  buildFilterOptions,
  mergeFilterOptions,
} from "@/utils/listFilterOptions";

const toast = useToast();

const isLoading = ref(true);
const rows = ref<VendorDto[]>([]);
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
const form = ref<VendorDto>({ ...emptyVendor });

// Delete
const showDelete = ref(false);
const isDeleting = ref(false);
const deleteTarget = ref<VendorDto | null>(null);

const columns = [
  { key: "code", label: "Code" },
  { key: "name", label: "Name" },
  { key: "contactPerson", label: "Contact" },
  { key: "email", label: "Email" },
  { key: "phone", label: "Phone" },
  { key: "category", label: "Category" },
  { key: "isActive", label: "Status" },
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
    buildFilterOptions<VendorDto>(rows.value, (row) => row.category),
    fallbackCategoryOptions,
  ),
);

const filterGroups = computed(() => [
  {
    key: "category",
    label: "Category",
    options: vendorCategoryOptions.value,
  },
  {
    key: "isActive",
    label: "Status",
    options: buildFilterOptions<VendorDto>(
      rows.value,
      (row) => row.isActive,
      (value) => ((value as boolean) ? "Active" : "Inactive"),
    ),
  },
]);

const fetchAll = async () => {
  isLoading.value = true;

  try {
    rows.value = await vendorService.getAll();
  } catch {
    toast.error("Failed to load vendors");
    rows.value = [];
  } finally {
    isLoading.value = false;
  }
};

onMounted(async () => {
  await Promise.all([fetchAll(), loadCodeTableOptions()]);
});

const openCreate = () => {
  form.value = { ...emptyVendor };
  isEditing.value = false;
  showModal.value = true;
};

const openEdit = (row: VendorDto) => {
  form.value = { ...row };
  isEditing.value = true;
  showModal.value = true;
};

const closeModal = () => {
  showModal.value = false;
};

const validate = (): string | null => {
  if (!form.value.name?.trim()) return "Vendor name is required";
  if (!form.value.code?.trim()) return "Vendor code is required";
  if (form.value.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.value.email))
    return "Invalid email address";
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
    await vendorService.save(form.value);
    toast.success(isEditing.value ? "Vendor updated" : "Vendor created");
    showModal.value = false;
    await fetchAll();
  } catch {
    toast.error("Failed to save vendor");
  } finally {
    isSaving.value = false;
  }
};

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
    await fetchAll();
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
      <template #cell-isActive="{ value }">
        <AppBadge :variant="value ? 'success' : 'default'" rounded>
          {{ value ? "Active" : "Inactive" }}
        </AppBadge>
      </template>

      <template #cell-catalogItemCount="{ value }">
        <span class="font-semibold">{{ value ?? 0 }}</span>
      </template>
    </AppDataTable>

    <!-- Vendor Modal -->
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
                {{ isEditing ? "Edit Vendor" : "New Vendor" }}
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
                  label="Vendor Name *"
                  placeholder="Enter vendor name"
                />
                <AppInput
                  v-model="form.code"
                  label="Vendor Code *"
                  placeholder="e.g. VND-001"
                  :disabled="isEditing"
                />
              </div>

              <div class="grid grid-cols-2 gap-4">
                <AppInput
                  v-model="form.contactPerson"
                  label="Contact Person"
                  placeholder="Full name"
                />
                <AppInput
                  v-model="form.email"
                  label="Email"
                  type="email"
                  placeholder="email@example.com"
                />
              </div>

              <div class="grid grid-cols-2 gap-4">
                <AppInput
                  v-model="form.phone"
                  label="Phone"
                  placeholder="+65 XXXX XXXX"
                />
                <AppSelect
                  v-model="form.category"
                  label="Category"
                  :options="categorySelectOptions"
                  placeholder="Select category"
                />
              </div>

              <AppInput
                v-model="form.address"
                label="Address"
                placeholder="Vendor address"
              />

              <AppInput
                v-model="form.notes"
                label="Notes"
                placeholder="Additional notes"
              />

              <div class="flex items-center gap-2">
                <input
                  id="vendor-active"
                  v-model="form.isActive"
                  type="checkbox"
                  class="size-4 rounded border-slate-300"
                />
                <label
                  for="vendor-active"
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
      title="Delete Vendor"
      :message="`Delete vendor '${deleteTarget?.name ?? ''}'? This cannot be undone.`"
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
