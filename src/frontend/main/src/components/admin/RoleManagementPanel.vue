<script setup lang="ts">
import { computed, ref } from "vue";
import { useRouter } from "vue-router";
import { useToast } from "@/composables/useToast";
import { AppSelect } from "@apptemplate/ui";
import roleService from "@/services/roleService";
import type {
  AccessFunction,
  Role,
  SaveRoleRequest,
  StaffUser,
  UserRoleAssignment,
} from "@/types";

const props = defineProps<{
  users: StaffUser[];
  roles: Role[];
  assignments: UserRoleAssignment[];
  accessFunctions: AccessFunction[];
}>();

const emit = defineEmits<{
  (event: "refresh"): void;
}>();

const router = useRouter();
const toast = useToast();
const saving = ref(false);

const showAssignUserModal = ref(false);
const selectedRole = ref<Role | null>(null);
const selectedStaffId = ref<number | null>(null);

const showRoleModal = ref(false);
const editingRole = ref<Role | null>(null);
const roleForm = ref<SaveRoleRequest>({
  code: "",
  name: "",
  description: null,
  isActive: true,
  displayOrder: 0,
  accessFunctionIds: [],
});

const roleDescriptions: Record<string, string> = {
  AdmissionOfficer:
    "Manages the application pipeline, searches and views applications, sends offers and messages, and accesses reports.",
  Assessor:
    "Reviews and assesses applications, manages interview questions, views applicant personas, and communicates with applicants.",
  Approver:
    "Has full assessment authority: approves or rejects applications, sends offers, performs bulk approvals, and accesses reports.",
  ProgrammeAdmin:
    "Administers programmes and workflows, configures form rules and interview questions, assesses applications, and views reports.",
  SystemAdmin:
    "Full access across admissions, enrolment, users and roles, configuration, audit logs, and reporting.",
};

const roleColorMap: Record<string, string> = {
  SystemAdmin: "bg-purple-100 text-purple-700",
  ProgrammeAdmin: "bg-blue-100 text-blue-700",
  Approver: "bg-emerald-100 text-emerald-700",
  Assessor: "bg-amber-100 text-amber-700",
  AdmissionOfficer: "bg-cyan-100 text-cyan-700",
};

const staffAccessFunctions = computed(() =>
  [...props.accessFunctions].sort(
    (left, right) =>
      left.module.localeCompare(right.module) ||
      left.displayOrder - right.displayOrder ||
      left.name.localeCompare(right.name),
  ),
);

const accessFunctionGroups = computed(() => {
  const groups = new Map<string, AccessFunction[]>();

  for (const accessFunction of staffAccessFunctions.value) {
    const existingGroup = groups.get(accessFunction.module) ?? [];
    existingGroup.push(accessFunction);
    groups.set(accessFunction.module, existingGroup);
  }

  return Array.from(groups.entries());
});

const availableUsersForRole = computed(() => {
  if (!selectedRole.value) return props.users;

  const assignedUserIds = props.assignments
    .filter(
      (assignment) =>
        assignment.roleId === selectedRole.value?.id && assignment.isActive,
    )
    .map((assignment) => assignment.staffUserId);

  return props.users.filter((user) => !assignedUserIds.includes(user.id));
});

const selectedAccessFunctionCount = computed(
  () => roleForm.value.accessFunctionIds.length,
);

// Counted on the login name: every assignment has one, whereas staffUserId is null for a
// login with no local account row.
const assignedUserCount = computed(
  () =>
    new Set(
      props.assignments
        .filter((assignment) => assignment.isActive)
        .map((assignment) => assignment.userId.toLowerCase()),
    ).size,
);

const roleCodePreview = computed(() => {
  const explicitCode = roleForm.value.code.trim();
  return explicitCode || buildCode(roleForm.value.name);
});

function getApiErrorMessage(error: unknown, fallback: string): string {
  const axiosError = error as {
    response?: { data?: { message?: string; error?: string } };
  };

  return (
    axiosError.response?.data?.message ||
    axiosError.response?.data?.error ||
    fallback
  );
}

function buildCode(value: string): string {
  return value
    .trim()
    .replace(/[^A-Za-z0-9]+/g, " ")
    .split(" ")
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join("");
}

function getInitials(name: string | null | undefined): string {
  if (!name) return "?";

  return name
    .split(" ")
    .map((part) => part[0])
    .join("")
    .toUpperCase()
    .slice(0, 2);
}

function getRoleColor(roleCode: string): string {
  return roleColorMap[roleCode] || "bg-slate-100 text-slate-600";
}

function getModuleClass(module: string): string {
  if (module.toLowerCase().includes("enrolment")) {
    return "bg-emerald-100 text-emerald-700";
  }

  if (module.toLowerCase().includes("system")) {
    return "bg-purple-100 text-purple-700";
  }

  return "bg-blue-100 text-blue-700";
}

function getRoleUsers(roleId: number): StaffUser[] {
  const assignedUserIds = props.assignments
    .filter((assignment) => assignment.roleId === roleId && assignment.isActive)
    .map((assignment) => assignment.staffUserId);

  return props.users.filter((user) => assignedUserIds.includes(user.id));
}

function getVisibleRoleAccessFunctions(role: Role): AccessFunction[] {
  return role.accessFunctions ?? [];
}

function openAssignUserModal(role: Role): void {
  selectedRole.value = role;
  selectedStaffId.value = null;
  showAssignUserModal.value = true;
}

function resetRoleForm(): void {
  editingRole.value = null;
  roleForm.value = {
    code: "",
    name: "",
    description: null,
    isActive: true,
    displayOrder: props.roles.length + 1,
    accessFunctionIds: [],
  };
}

function openCreateRole(): void {
  resetRoleForm();
  showRoleModal.value = true;
}

function openEditRole(role: Role): void {
  editingRole.value = role;
  roleForm.value = {
    id: role.id,
    code: role.code,
    name: role.name,
    description: role.description ?? null,
    isActive: role.isActive,
    displayOrder: role.displayOrder,
    accessFunctionIds: getVisibleRoleAccessFunctions(role).map(
      (accessFunction) => accessFunction.id,
    ),
  };
  showRoleModal.value = true;
}

async function saveUserAssignment(): Promise<void> {
  if (!selectedRole.value || !selectedStaffId.value) return;

  saving.value = true;

  try {
    const staffUser = props.users.find(
      (user) => user.id === selectedStaffId.value,
    );

    if (!staffUser) {
      toast.error("That user account is no longer available");
      return;
    }

    await roleService.saveAssignment({
      // AssignRoleDto.UserId is the login name, not the numeric account id.
      userId: staffUser.username,
      roleId: selectedRole.value.id,
    });

    emit("refresh");
    toast.success("User assigned successfully");
    showAssignUserModal.value = false;
  } catch (error: unknown) {
    toast.error(getApiErrorMessage(error, "Failed to assign user"));
  } finally {
    saving.value = false;
  }
}

async function saveRole(): Promise<void> {
  const roleName = roleForm.value.name.trim();
  const roleCode = roleCodePreview.value;

  if (!roleName) {
    toast.error("Role name is required");
    return;
  }

  if (!roleCode) {
    toast.error("Role code is required");
    return;
  }

  if (roleForm.value.accessFunctionIds.length === 0) {
    toast.error("Select at least one access function");
    return;
  }

  saving.value = true;

  try {
    await roleService.saveRole({
      id: roleForm.value.id,
      code: roleCode,
      name: roleName,
      description: roleForm.value.description?.trim() || null,
      isActive: roleForm.value.isActive,
      displayOrder: roleForm.value.displayOrder,
      accessFunctionIds: [...roleForm.value.accessFunctionIds],
    });

    emit("refresh");
    toast.success(editingRole.value ? "Role updated" : "Role created");
    showRoleModal.value = false;
  } catch (error: unknown) {
    toast.error(getApiErrorMessage(error, "Failed to save role"));
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <div class="flex flex-col gap-5">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h2 class="text-xl font-bold text-slate-800">Role Catalogue</h2>
        <p class="mt-1 text-sm text-slate-500">
          Combine system-defined access functions into one shared role, then
          assign that role to staff members.
        </p>
      </div>
      <div class="flex flex-wrap gap-3">
        <button
          class="flex h-11 items-center gap-2 rounded-xl border border-slate-200 bg-white px-5 text-sm font-bold text-slate-700 shadow-soft transition-all hover:border-accent/30 hover:text-accent"
          @click="router.push({ name: 'access-functions' })"
        >
          <span class="material-symbols-outlined text-[20px]">key</span>
          Open Access Functions
        </button>
        <button
          class="flex h-11 items-center gap-2 rounded-xl bg-accent px-5 text-sm font-bold text-white shadow-soft transition-all hover:bg-accent/90"
          @click="openCreateRole"
        >
          <span class="material-symbols-outlined text-[20px]">shield</span>
          Create Role
        </button>
      </div>
    </div>

    <div class="grid gap-4 md:grid-cols-3">
      <div class="rounded-2xl border border-slate-100 bg-white p-5 shadow-soft">
        <p class="text-xs font-bold uppercase tracking-wide text-slate-400">
          Roles
        </p>
        <p class="mt-2 text-3xl font-extrabold text-slate-800">
          {{ roles.length }}
        </p>
      </div>
      <div class="rounded-2xl border border-slate-100 bg-white p-5 shadow-soft">
        <p class="text-xs font-bold uppercase tracking-wide text-slate-400">
          Access Functions
        </p>
        <p class="mt-2 text-3xl font-extrabold text-slate-800">
          {{ staffAccessFunctions.length }}
        </p>
      </div>
      <div class="rounded-2xl border border-slate-100 bg-white p-5 shadow-soft">
        <p class="text-xs font-bold uppercase tracking-wide text-slate-400">
          Assigned Staff
        </p>
        <p class="mt-2 text-3xl font-extrabold text-slate-800">
          {{ assignedUserCount }}
        </p>
      </div>
    </div>

    <section
      class="rounded-2xl border border-slate-100 bg-white p-6 shadow-soft"
    >
      <div class="mb-4 flex items-center justify-between gap-3">
        <div>
          <h3 class="text-lg font-bold text-slate-800">
            System Access Functions
          </h3>
          <p class="mt-1 text-sm text-slate-500">
            These permissions are defined in code and are available when
            building shared staff roles.
          </p>
        </div>
        <span
          class="inline-flex items-center rounded-full bg-accent/10 px-3 py-1 text-xs font-bold text-accent"
        >
          {{ staffAccessFunctions.length }} functions
        </span>
      </div>

      <div class="flex flex-col gap-4">
        <div
          v-for="[module, moduleFunctions] in accessFunctionGroups"
          :key="module"
          class="rounded-2xl border border-slate-100 bg-slate-50/70 p-4"
        >
          <div class="mb-3 flex items-center justify-between gap-3">
            <span
              class="inline-flex rounded-full px-2.5 py-1 text-xs font-bold"
              :class="getModuleClass(module)"
            >
              {{ module }}
            </span>
            <span class="text-xs font-semibold text-slate-400">
              {{ moduleFunctions.length }} options
            </span>
          </div>

          <div class="flex flex-wrap gap-2">
            <div
              v-for="accessFunction in moduleFunctions"
              :key="accessFunction.id"
              class="rounded-xl border border-slate-200 bg-white px-3 py-2"
            >
              <p class="text-xs font-bold text-slate-700">
                {{ accessFunction.name }}
              </p>
              <p class="mt-0.5 text-[11px] font-mono text-slate-400">
                {{ accessFunction.code }}
              </p>
            </div>
          </div>
        </div>
      </div>
    </section>

    <section class="grid grid-cols-1 gap-5 lg:grid-cols-2">
      <article
        v-for="role in roles"
        :key="role.id"
        class="flex flex-col rounded-2xl border border-slate-100 bg-white p-6 shadow-soft"
      >
        <div class="mb-4 flex items-start justify-between gap-4">
          <div class="flex items-center gap-3">
            <div
              class="flex size-11 items-center justify-center rounded-xl"
              :class="getRoleColor(role.code)"
            >
              <span class="material-symbols-outlined text-[22px]">shield</span>
            </div>
            <div>
              <h3 class="text-lg font-bold text-slate-800">{{ role.name }}</h3>
              <p class="text-xs font-mono text-slate-500">{{ role.code }}</p>
            </div>
          </div>

          <span
            class="inline-flex items-center rounded-full px-2.5 py-1 text-xs font-bold"
            :class="
              role.isActive
                ? 'bg-emerald-100 text-emerald-700'
                : 'bg-slate-100 text-slate-500'
            "
          >
            {{ role.isActive ? "Active" : "Inactive" }}
          </span>
        </div>

        <p class="mb-4 text-sm leading-relaxed text-slate-500">
          {{
            roleDescriptions[role.code] ||
            role.description ||
            "No description provided."
          }}
        </p>

        <div class="mb-4">
          <div class="mb-2 flex items-center justify-between gap-3">
            <p
              class="text-xs font-bold uppercase tracking-wider text-slate-400"
            >
              Access Functions
            </p>
            <span class="text-xs font-semibold text-slate-400">
              {{ getVisibleRoleAccessFunctions(role).length }} selected
            </span>
          </div>

          <div class="flex flex-wrap gap-1.5">
            <span
              v-for="accessFunction in getVisibleRoleAccessFunctions(role)"
              :key="`${role.id}-${accessFunction.id}`"
              class="inline-flex rounded-full px-2 py-0.5 text-[10px] font-bold"
              :class="getModuleClass(accessFunction.module)"
            >
              {{ accessFunction.name }}
            </span>
            <span
              v-if="getVisibleRoleAccessFunctions(role).length === 0"
              class="text-xs italic text-slate-400"
            >
              No access functions configured
            </span>
          </div>
        </div>

        <div class="mt-auto border-t border-slate-100 pt-4">
          <div class="mb-3 flex items-center justify-between gap-3">
            <p
              class="text-xs font-bold uppercase tracking-wider text-slate-400"
            >
              Assigned Users
            </p>
            <span class="text-xs font-semibold text-slate-400">
              {{ getRoleUsers(role.id).length }}
            </span>
          </div>

          <div
            v-if="getRoleUsers(role.id).length > 0"
            class="mb-4 flex flex-wrap gap-2"
          >
            <div
              v-for="user in getRoleUsers(role.id)"
              :key="`${role.id}-${user.id}`"
              class="flex items-center gap-2 rounded-lg border border-slate-100 bg-slate-50 px-3 py-1.5"
            >
              <div
                class="flex size-6 items-center justify-center rounded-full bg-accent/10 text-[10px] font-bold text-accent"
              >
                {{ getInitials(user.fullName) }}
              </div>
              <span class="text-xs font-medium text-slate-700">
                {{ user.fullName || user.username }}
              </span>
            </div>
          </div>
          <p v-else class="mb-4 text-xs italic text-slate-400">
            No users assigned yet.
          </p>

          <div class="flex flex-wrap items-center gap-3">
            <button
              class="inline-flex items-center gap-1 rounded-lg px-3 py-1.5 text-xs font-bold text-accent transition-colors hover:bg-accent/10"
              @click="openAssignUserModal(role)"
            >
              <span class="material-symbols-outlined text-[16px]"
                >person_add</span
              >
              Assign User
            </button>
            <button
              class="inline-flex items-center gap-1 rounded-lg px-3 py-1.5 text-xs font-bold text-slate-600 transition-colors hover:bg-slate-100"
              @click="openEditRole(role)"
            >
              <span class="material-symbols-outlined text-[16px]">edit</span>
              Edit Role
            </button>
          </div>
        </div>
      </article>
    </section>

    <Teleport to="body">
      <div
        v-if="showAssignUserModal"
        class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/20 p-4 backdrop-blur-sm"
      >
        <div
          class="flex w-full max-w-lg flex-col overflow-hidden rounded-3xl bg-white shadow-2xl"
        >
          <div
            class="flex items-center justify-between border-b border-slate-100 p-6"
          >
            <h2 class="text-xl font-bold text-slate-800">Assign User</h2>
            <button
              class="rounded-full p-2 text-slate-400 hover:bg-slate-100"
              @click="showAssignUserModal = false"
            >
              <span class="material-symbols-outlined">close</span>
            </button>
          </div>

          <div class="flex flex-col gap-5 p-6">
            <div>
              <p class="mb-1 text-sm font-bold text-slate-500">Role</p>
              <span
                class="inline-flex rounded-full px-3 py-1 text-sm font-bold"
                :class="getRoleColor(selectedRole?.code ?? '')"
              >
                {{ selectedRole?.name }}
              </span>
            </div>

            <div class="flex flex-col gap-2">
              <AppSelect
                v-model="selectedStaffId"
                label="Select User"
                :options="
                  availableUsersForRole.map((user) => ({
                    value: user.id,
                    label: `${user.fullName || user.username} (${user.username})`,
                  }))
                "
                placeholder="Choose a user..."
                :searchable="true"
              />
            </div>
          </div>

          <div
            class="flex items-center justify-end gap-3 border-t border-slate-100 bg-slate-50 p-6"
          >
            <button
              class="rounded-xl px-6 py-2.5 font-bold text-slate-500 transition-colors hover:bg-slate-200"
              @click="showAssignUserModal = false"
            >
              Cancel
            </button>
            <button
              class="rounded-xl bg-accent px-6 py-2.5 font-bold text-white shadow-soft transition-all hover:bg-accent/90 disabled:opacity-50"
              :disabled="!selectedStaffId || saving"
              @click="saveUserAssignment"
            >
              {{ saving ? "Saving..." : "Assign User" }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <Teleport to="body">
      <div
        v-if="showRoleModal"
        class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/20 p-4 backdrop-blur-sm"
      >
        <div
          class="flex max-h-[90vh] w-full max-w-5xl flex-col overflow-hidden rounded-3xl bg-white shadow-2xl"
        >
          <div
            class="flex items-center justify-between border-b border-slate-100 p-6"
          >
            <div>
              <h2 class="text-xl font-bold text-slate-800">
                {{ editingRole ? "Edit Role" : "Create Role" }}
              </h2>
              <p class="mt-1 text-sm text-slate-500">
                Build one shared role with code-defined access functions.
              </p>
            </div>
            <button
              class="rounded-full p-2 text-slate-400 hover:bg-slate-100"
              @click="showRoleModal = false"
            >
              <span class="material-symbols-outlined">close</span>
            </button>
          </div>

          <div
            class="grid gap-6 overflow-y-auto p-6 lg:grid-cols-[360px_minmax(0,1fr)]"
          >
            <section class="flex flex-col gap-5">
              <div
                class="rounded-2xl border border-slate-100 bg-slate-50/70 p-5"
              >
                <h3
                  class="text-sm font-bold uppercase tracking-wide text-slate-400"
                >
                  Role Details
                </h3>

                <div class="mt-4 flex flex-col gap-4">
                  <div class="flex flex-col gap-2">
                    <label class="text-sm font-bold text-slate-500">
                      Name <span class="text-red-500">*</span>
                    </label>
                    <input
                      v-model="roleForm.name"
                      type="text"
                      class="h-12 rounded-xl border border-slate-200 bg-white px-4 font-medium text-slate-800 focus:ring-2 focus:ring-accent/20"
                      placeholder="e.g. Admissions & Enrolment Manager"
                    />
                  </div>

                  <div class="flex flex-col gap-2">
                    <label class="text-sm font-bold text-slate-500">
                      Code
                    </label>
                    <input
                      v-model="roleForm.code"
                      type="text"
                      class="h-12 rounded-xl border border-slate-200 bg-white px-4 font-medium text-slate-800 focus:ring-2 focus:ring-accent/20"
                      placeholder="Leave blank to auto-generate"
                    />
                    <p class="text-xs text-slate-400">
                      Preview:
                      <span class="font-mono">{{ roleCodePreview }}</span>
                    </p>
                  </div>

                  <div class="flex flex-col gap-2">
                    <label class="text-sm font-bold text-slate-500">
                      Description
                    </label>
                    <textarea
                      v-model="roleForm.description"
                      rows="4"
                      class="rounded-xl border border-slate-200 bg-white px-4 py-3 font-medium text-slate-800 focus:ring-2 focus:ring-accent/20"
                      placeholder="Describe who should use this role."
                    />
                  </div>

                  <div class="grid grid-cols-2 gap-4">
                    <div class="flex flex-col gap-2">
                      <label class="text-sm font-bold text-slate-500">
                        Display Order
                      </label>
                      <input
                        v-model.number="roleForm.displayOrder"
                        type="number"
                        min="0"
                        class="h-12 rounded-xl border border-slate-200 bg-white px-4 font-medium text-slate-800 focus:ring-2 focus:ring-accent/20"
                      />
                    </div>

                    <label
                      class="flex items-center gap-3 rounded-2xl border border-slate-200 bg-white px-4 py-3"
                    >
                      <input
                        v-model="roleForm.isActive"
                        type="checkbox"
                        class="size-4 rounded border-slate-300 text-accent focus:ring-accent/20"
                      />
                      <div>
                        <p class="text-sm font-bold text-slate-700">
                          Active role
                        </p>
                        <p class="text-xs text-slate-400">
                          Inactive roles stay in history but cannot be assigned.
                        </p>
                      </div>
                    </label>
                  </div>
                </div>
              </div>

              <div class="rounded-2xl border border-accent/10 bg-accent/5 p-5">
                <p
                  class="text-xs font-bold uppercase tracking-wide text-accent/80"
                >
                  Selected Access Functions
                </p>
                <p class="mt-2 text-3xl font-extrabold text-slate-800">
                  {{ selectedAccessFunctionCount }}
                </p>
                <p class="mt-1 text-sm text-slate-500">
                  Roles can combine screen and API permissions from the
                  code-managed access-function catalog.
                </p>
              </div>
            </section>

            <section class="flex flex-col gap-4">
              <div class="flex items-center justify-between gap-3">
                <div>
                  <h3 class="text-lg font-bold text-slate-800">
                    Access Function Selection
                  </h3>
                  <p class="mt-1 text-sm text-slate-500">
                    Choose the exact capabilities included in this role.
                  </p>
                </div>
                <span
                  class="inline-flex items-center rounded-full bg-slate-100 px-3 py-1 text-xs font-bold text-slate-500"
                >
                  {{ staffAccessFunctions.length }} available
                </span>
              </div>

              <div
                class="flex max-h-[56vh] flex-col gap-4 overflow-y-auto pr-1"
              >
                <div
                  v-for="[module, moduleFunctions] in accessFunctionGroups"
                  :key="`picker-${module}`"
                  class="rounded-2xl border border-slate-100 bg-slate-50/70 p-4"
                >
                  <div class="mb-3 flex items-center justify-between gap-3">
                    <span
                      class="inline-flex rounded-full px-2.5 py-1 text-xs font-bold"
                      :class="getModuleClass(module)"
                    >
                      {{ module }}
                    </span>
                    <span class="text-xs font-semibold text-slate-400">
                      {{ moduleFunctions.length }} options
                    </span>
                  </div>

                  <div class="grid gap-3 md:grid-cols-2">
                    <label
                      v-for="accessFunction in moduleFunctions"
                      :key="`role-form-${accessFunction.id}`"
                      class="flex cursor-pointer items-start gap-3 rounded-2xl border border-slate-200 bg-white px-4 py-3 transition-colors hover:border-accent/30"
                    >
                      <input
                        v-model="roleForm.accessFunctionIds"
                        :value="accessFunction.id"
                        type="checkbox"
                        class="mt-1 size-4 rounded border-slate-300 text-accent focus:ring-accent/20"
                      />
                      <div class="min-w-0">
                        <p class="text-sm font-bold text-slate-700">
                          {{ accessFunction.name }}
                        </p>
                        <p class="mt-1 text-[11px] font-mono text-slate-400">
                          {{ accessFunction.code }}
                        </p>
                        <p
                          v-if="accessFunction.description"
                          class="mt-1 text-xs leading-relaxed text-slate-500"
                        >
                          {{ accessFunction.description }}
                        </p>
                      </div>
                    </label>
                  </div>
                </div>
              </div>
            </section>
          </div>

          <div
            class="flex items-center justify-end gap-3 border-t border-slate-100 bg-slate-50 p-6"
          >
            <button
              class="rounded-xl px-6 py-2.5 font-bold text-slate-500 transition-colors hover:bg-slate-200"
              @click="showRoleModal = false"
            >
              Cancel
            </button>
            <button
              class="rounded-xl bg-accent px-6 py-2.5 font-bold text-white shadow-soft transition-all hover:bg-accent/90 disabled:opacity-50"
              :disabled="saving"
              @click="saveRole"
            >
              {{
                saving
                  ? "Saving..."
                  : editingRole
                    ? "Update Role"
                    : "Create Role"
              }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!--
      There is deliberately no "Add Access Function" modal here. Access functions are a
      code-owned catalogue (backend Shared/Security/AccessFunctionCatalog.cs) seeded on
      startup and referenced by [RequireAccessFunction] attributes — they are not
      runtime-editable data, and AccessControlController exposes no endpoint to write
      them. Roles are the runtime-composable thing; this panel builds those.
    -->
  </div>
</template>
