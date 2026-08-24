<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { NieButton } from "@nie/ui";
import AccessFunctionGrantRow from "./AccessFunctionGrantRow.vue";
import RoleMetadataModal from "./RoleMetadataModal.vue";
import type { AccessFunction, Role } from "@/types";

const props = withDefaults(
  defineProps<{
    roles: Role[];
    accessFunctions: AccessFunction[];
    saving?: boolean;
    canManage?: boolean;
  }>(),
  { canManage: true },
);

const emit = defineEmits<{
  "save-role-access": [value: { roleId: string; accessFunctionIds: string[] }];
  "save-role-metadata": [
    value: {
      roleId?: string;
      code: string;
      name: string;
      description: string | null;
      isActive: boolean;
    },
  ];
}>();

const selectedRoleId = ref("");
const draftAccessFunctionIds = ref<string[]>([]);
const showRoleModal = ref(false);
const modalRole = ref<Role | null>(null);

const orderedRoles = computed(() =>
  [...props.roles].sort(
    (left, right) =>
      left.displayOrder - right.displayOrder ||
      left.name.localeCompare(right.name),
  ),
);
const selectedRole = computed(
  () =>
    props.roles.find((role) => role.id === selectedRoleId.value) ??
    orderedRoles.value[0] ??
    null,
);
const groups = computed(() => {
  const grouped = new Map<string, AccessFunction[]>();
  for (const accessFunction of props.accessFunctions.filter(
    (item) => item.isActive,
  )) {
    const list = grouped.get(accessFunction.module) ?? [];
    list.push(accessFunction);
    grouped.set(accessFunction.module, list);
  }

  return [...grouped.entries()]
    .map(([name, accessFunctions]) => ({
      name,
      accessFunctions: accessFunctions.sort(
        (left, right) => left.displayOrder - right.displayOrder,
      ),
    }))
    .sort((left, right) => left.name.localeCompare(right.name));
});

watch(
  orderedRoles,
  (roles) => {
    if (!roles.some((role) => role.id === selectedRoleId.value)) {
      selectedRoleId.value = roles[0]?.id ?? "";
    }
  },
  { immediate: true },
);
watch(
  selectedRole,
  (role) => {
    draftAccessFunctionIds.value = [
      ...(role?.accessFunctionIds ??
        role?.accessFunctions.map((item) => item.id) ??
        []),
    ];
  },
  { immediate: true },
);

function setGrant(id: string, selected: boolean): void {
  const values = new Set(draftAccessFunctionIds.value);
  if (selected) {
    values.add(id);
  } else {
    values.delete(id);
  }
  draftAccessFunctionIds.value = [...values];
}

function editRole(): void {
  if (!props.canManage) return;
  modalRole.value = selectedRole.value;
  showRoleModal.value = true;
}

function newRole(): void {
  if (!props.canManage) return;
  modalRole.value = null;
  showRoleModal.value = true;
}

function saveAccess(): void {
  if (!selectedRole.value || !props.canManage) return;
  emit("save-role-access", {
    roleId: selectedRole.value.id,
    accessFunctionIds: [...draftAccessFunctionIds.value],
  });
}

function saveMetadata(value: {
  roleId?: string;
  code: string;
  name: string;
  description: string | null;
  isActive: boolean;
}): void {
  if (!props.canManage) return;
  showRoleModal.value = false;
  emit("save-role-metadata", value);
}
</script>

<template>
  <div
    class="grid min-h-[32rem] gap-6 lg:h-[calc(100dvh-13.5rem)] lg:max-h-[56rem] lg:grid-cols-[18rem_minmax(0,1fr)]"
  >
    <aside
      class="flex min-h-0 flex-col overflow-hidden rounded-3xl border border-secondary-200 bg-white p-3 shadow-[var(--theme-shadow-soft)] dark:border-secondary-700 dark:bg-secondary-900"
    >
      <div class="mb-3 flex items-center justify-between gap-2 px-2">
        <div>
          <p
            class="text-xs font-bold uppercase tracking-wide text-secondary-400"
          >
            Roles
          </p>
          <p class="text-sm text-secondary-500">
            {{ roles.length }} configured
          </p>
        </div>
        <button
          v-if="canManage"
          type="button"
          class="inline-flex size-11 items-center justify-center rounded-xl text-primary-700 hover:bg-primary-50 dark:text-primary-300 dark:hover:bg-primary-950/40"
          aria-label="Create role"
          title="Create role"
          @click="newRole"
        >
          <span class="material-symbols-outlined" aria-hidden="true">add</span>
        </button>
      </div>

      <div
        class="min-h-0 flex-1 space-y-1 overflow-y-auto overscroll-contain pr-1"
        role="tablist"
        aria-label="Roles"
        aria-orientation="vertical"
      >
        <button
          v-for="role in orderedRoles"
          :key="role.id"
          type="button"
          role="tab"
          class="w-full rounded-2xl px-3 py-3 text-left transition"
          :class="
            role.id === selectedRole?.id
              ? 'bg-primary-600 text-on-brand shadow-[var(--theme-shadow-soft)]'
              : 'text-secondary-700 hover:bg-secondary-50 dark:text-secondary-200 dark:hover:bg-secondary-800'
          "
          :aria-selected="role.id === selectedRole?.id"
          @click="selectedRoleId = role.id"
        >
          <span class="block font-semibold">{{ role.name }}</span>
          <span
            class="mt-1 block truncate text-xs"
            :class="
              role.id === selectedRole?.id
                ? 'text-primary-100'
                : 'text-secondary-400'
            "
          >
            {{ role.code }} · {{ role.assignedUserCount ?? 0 }} users
          </span>
        </button>
      </div>
    </aside>

    <section
      v-if="selectedRole"
      class="flex min-h-0 min-w-0 flex-col overflow-hidden rounded-3xl border border-secondary-200 bg-white p-5 shadow-[var(--theme-shadow-soft)] dark:border-secondary-700 dark:bg-secondary-900"
      role="tabpanel"
    >
      <div
        class="mb-6 flex shrink-0 flex-wrap items-start justify-between gap-4 border-b border-secondary-100 pb-5 dark:border-secondary-800"
      >
        <div>
          <div class="flex flex-wrap items-center gap-2">
            <h2 class="text-xl font-bold text-secondary-900 dark:text-white">
              {{ selectedRole.name }}
            </h2>
            <span
              v-if="selectedRole.isSystemRole"
              class="rounded-full bg-primary-100 px-2 py-0.5 text-xs font-bold text-primary-700 dark:bg-primary-950/50 dark:text-primary-300"
            >
              System
            </span>
          </div>
          <p class="mt-1 max-w-2xl text-sm text-secondary-500">
            {{ selectedRole.description || "No description provided." }}
          </p>
        </div>
        <NieButton
          v-if="canManage"
          variant="outline"
          size="sm"
          :aria-label="`Edit ${selectedRole.name}`"
          @click="editRole"
        >
          <span class="material-symbols-outlined text-card-title">edit</span>
          Edit
        </NieButton>
      </div>

      <div
        class="min-h-0 flex-1 space-y-5 overflow-y-auto overscroll-contain pr-2"
        role="region"
        tabindex="0"
        :aria-label="`${selectedRole.name} access functions`"
      >
        <section
          v-for="group in groups"
          :key="group.name"
          class="rounded-2xl border border-secondary-100 bg-secondary-50/40 p-4 dark:border-secondary-800 dark:bg-secondary-950/30"
        >
          <div class="mb-3 flex items-center justify-between gap-3">
            <h3 class="font-bold text-secondary-900 dark:text-secondary-100">
              {{ group.name }}
            </h3>
            <span class="text-xs font-semibold text-secondary-400">
              {{
                group.accessFunctions.filter((item) =>
                  draftAccessFunctionIds.includes(item.id),
                ).length
              }}/{{ group.accessFunctions.length }} granted
            </span>
          </div>
          <div class="grid gap-3 xl:grid-cols-2">
            <AccessFunctionGrantRow
              v-for="accessFunction in group.accessFunctions"
              :key="accessFunction.id"
              :access-function="accessFunction"
              :selected="draftAccessFunctionIds.includes(accessFunction.id)"
              :disabled="saving || !canManage"
              @update:selected="setGrant(accessFunction.id, $event)"
            />
          </div>
        </section>
      </div>

      <div v-if="canManage" class="mt-6 flex shrink-0 justify-end">
        <NieButton
          data-testid="save-role-access"
          :disabled="draftAccessFunctionIds.length === 0"
          :loading="saving"
          @click="saveAccess"
        >
          Save access functions
        </NieButton>
      </div>
    </section>

    <div
      v-else
      class="flex min-h-80 items-center justify-center rounded-3xl border border-dashed border-secondary-300 text-secondary-500"
    >
      Create a role to configure access functions.
    </div>
  </div>

  <RoleMetadataModal
    v-model="showRoleModal"
    :role="modalRole"
    :saving="saving"
    @save="saveMetadata"
  />
</template>
