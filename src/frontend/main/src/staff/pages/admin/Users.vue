<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import RoleManagementPanel from "@/components/admin/RoleManagementPanel.vue";
import { useToast } from "@/composables/useToast";
import { AppDataTable, AppSelect } from "@apptemplate/ui";
import roleService from "@/services/roleService";
import staffUserService from "@/services/staffUserService";
import { buildFilterOptions } from "@/utils/listFilterOptions";
import type {
  AccessFunction,
  RegisterStaffRequest,
  Role,
  StaffUser,
  UserRoleAssignment,
} from "@/types";

const toast = useToast();
const loading = ref(true);
const saving = ref(false);
const route = useRoute();
const router = useRouter();

const users = ref<StaffUser[]>([]);
const roles = ref<Role[]>([]);
const assignments = ref<UserRoleAssignment[]>([]);
const accessFunctions = ref<AccessFunction[]>([]);

const showAssignModal = ref(false);
const selectedUser = ref<StaffUser | null>(null);
const selectedRoleId = ref<number | null>(null);

// Accounts live in this application's own identity provider, so an administrator types
// the details in directly. (There is no external staff directory to search.)
const showAddUserModal = ref(false);
const emptyNewUser = (): RegisterStaffRequest => ({
  userId: "",
  initialPassword: "",
  email: "",
  fullName: "",
  department: "",
});
const newUser = ref<RegisterStaffRequest>(emptyNewUser());
// RegisterUserAccountDto requires both of these; the request 400s without a password.
const canSubmitNewUser = computed(
  () =>
    newUser.value.userId.trim().length > 0 &&
    newUser.value.initialPassword.length > 0,
);

const userSearch = ref("");
const selectedFilters = ref<Record<string, Array<string | number | boolean>>>(
  {},
);

// Every local account is listed, including deactivated ones — hiding them would hide the
// Approve button that is the only way to bring an account back.
const userRows = computed(() =>
  users.value.map((user) => ({
    ...user,
    statusLabel: user.accountStatus,
    assignedRoleNames: getUserAssignments(user.id).map((assignment) =>
      getRoleName(assignment.roleId),
    ),
  })),
);

const userColumns = [
  { key: "fullName", label: "User" },
  { key: "assignedRoleNames", label: "Assigned Roles" },
  { key: "statusLabel", label: "Status" },
  { key: "department", label: "Department" },
];

const userFilterGroups = computed(() => [
  {
    key: "statusLabel",
    label: "Status",
    options: buildFilterOptions(userRows.value, (user) => user.statusLabel),
  },
  {
    key: "department",
    label: "Department",
    options: buildFilterOptions(userRows.value, (user) => user.department),
  },
  {
    key: "assignedRoleNames",
    label: "Roles",
    options: buildFilterOptions(
      userRows.value,
      (user) => user.assignedRoleNames,
    ),
  },
]);

const availableRoles = computed(() => {
  if (!selectedUser.value) {
    return roles.value.filter((role) => role.isActive);
  }

  const assignedRoleIds = getUserAssignments(selectedUser.value.id).map(
    (assignment) => assignment.roleId,
  );

  return roles.value.filter(
    (role) => role.isActive && !assignedRoleIds.includes(role.id),
  );
});

const activeTab = computed<"users" | "roles">(() =>
  route.name === "role-management" ? "roles" : "users",
);

onMounted(async () => {
  await loadPageData();
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

async function loadUsers(): Promise<void> {
  users.value = await staffUserService.getAll();
}

async function loadRbac(): Promise<void> {
  const [allRoles, allAssignments, allAccessFunctions] = await Promise.all([
    roleService.getAllRolesWithAccessFunctions(),
    roleService.getAllAssignments(),
    roleService.getAllAccessFunctions(),
  ]);

  roles.value = allRoles;
  assignments.value = allAssignments;
  accessFunctions.value = allAccessFunctions;
}

async function loadPageData(): Promise<void> {
  loading.value = true;

  try {
    await Promise.all([loadUsers(), loadRbac()]);
  } catch {
    toast.error("Failed to load users and roles");
  } finally {
    loading.value = false;
  }
}

async function handleRoleRefresh(): Promise<void> {
  try {
    await Promise.all([loadUsers(), loadRbac()]);
  } catch {
    toast.error("Failed to refresh users and roles");
  }
}

function getUserAssignments(userId: number): UserRoleAssignment[] {
  return assignments.value.filter(
    (assignment) => assignment.staffUserId === userId && assignment.isActive,
  );
}

function getRoleName(roleId: number): string {
  return roles.value.find((role) => role.id === roleId)?.name ?? "Unknown";
}

function getRoleCodeById(roleId: number): string {
  return roles.value.find((role) => role.id === roleId)?.code ?? "";
}

function openAssignModal(user: StaffUser): void {
  selectedUser.value = user;
  selectedRoleId.value = null;
  showAssignModal.value = true;
}

async function saveAssignment(): Promise<void> {
  if (!selectedUser.value || !selectedRoleId.value) return;

  saving.value = true;

  try {
    await roleService.saveAssignment({
      // AssignRoleDto.UserId is the login name.
      userId: selectedUser.value.username,
      roleId: selectedRoleId.value,
    });

    await loadRbac();
    toast.success("Role assigned successfully");
    showAssignModal.value = false;
  } catch (error: unknown) {
    toast.error(getApiErrorMessage(error, "Failed to assign role"));
  } finally {
    saving.value = false;
  }
}

async function removeAssignment(id: number): Promise<void> {
  try {
    const result = await roleService.deleteAssignment(id);

    await Promise.all([loadUsers(), loadRbac()]);

    if (result.userDeactivated) {
      toast.success(
        "Role removed. User deactivated because no active roles remain.",
      );
      return;
    }

    toast.success("Role assignment removed");
  } catch (error: unknown) {
    toast.error(getApiErrorMessage(error, "Failed to remove role assignment"));
  }
}

function openAddUserModal(): void {
  newUser.value = emptyNewUser();
  showAddUserModal.value = true;
}

async function addUser(): Promise<void> {
  if (!canSubmitNewUser.value) return;

  saving.value = true;

  try {
    const trimmed = (value: string | null | undefined) =>
      value?.trim() ? value.trim() : null;

    await staffUserService.register({
      userId: newUser.value.userId.trim(),
      initialPassword: newUser.value.initialPassword,
      email: trimmed(newUser.value.email),
      fullName: trimmed(newUser.value.fullName),
      department: trimmed(newUser.value.department),
    });

    await loadUsers();
    toast.success(
      "User added. They must change the initial password at first sign-in.",
    );
    showAddUserModal.value = false;
  } catch (error: unknown) {
    toast.error(getApiErrorMessage(error, "Failed to add user"));
  } finally {
    saving.value = false;
  }
}

function getDisplayName(user: StaffUser): string {
  return user.fullName || user.username;
}

async function approveUser(user: StaffUser): Promise<void> {
  try {
    // ApproveUser takes the real UserAccount.Id on the query string.
    await staffUserService.approve(user.id);
    await loadUsers();
    toast.success(`${getDisplayName(user)} approved successfully`);
  } catch (error: unknown) {
    toast.error(getApiErrorMessage(error, "Failed to approve user"));
  }
}

async function deactivateUser(user: StaffUser): Promise<void> {
  try {
    const result = await staffUserService.deactivate(user.id);
    await loadUsers();

    // A null sessionsRevoked means the session store could not be reached, so the user
    // may still be signed in somewhere. Say so instead of claiming a clean deactivation.
    if (result.warning) {
      toast.warning(`${getDisplayName(user)}: ${result.warning}`);
      return;
    }

    toast.success(
      `${getDisplayName(user)} deactivated (${result.sessionsRevoked ?? 0} session(s) revoked)`,
    );
  } catch (error: unknown) {
    toast.error(getApiErrorMessage(error, "Failed to deactivate user"));
  }
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

const roleColorMap: Record<string, string> = {
  SystemAdmin: "bg-purple-100 text-purple-700",
  ProgrammeAdmin: "bg-blue-100 text-blue-700",
  Approver: "bg-emerald-100 text-emerald-700",
  Assessor: "bg-amber-100 text-amber-700",
  AdmissionOfficer: "bg-cyan-100 text-cyan-700",
};

function getRoleColor(roleCode: string): string {
  return roleColorMap[roleCode] || "bg-slate-100 text-slate-600";
}

function userSearchAccessor(user: StaffUser & { assignedRoleNames: string[] }) {
  return [
    user.username,
    user.fullName,
    user.email,
    user.department,
    user.statusLabel,
    ...user.assignedRoleNames,
  ];
}

function openTab(tab: "users" | "roles") {
  const targetRoute = tab === "roles" ? "role-management" : "users";

  if (route.name !== targetRoute) {
    void router.push({ name: targetRoute });
  }
}
</script>

<template>
  <div class="flex flex-col gap-8 flex-1 min-h-0">
    <!-- Tabs -->
    <div class="portal-tabbar" role="tablist" aria-label="Users and roles tabs">
      <button
        role="tab"
        :aria-selected="activeTab === 'users'"
        class="portal-tab flex items-center gap-2"
        :class="
          activeTab === 'users'
            ? 'bg-accent text-white shadow-soft'
            : 'text-slate-500 hover:bg-accent-light hover:text-accent'
        "
        @click="openTab('users')"
      >
        <span class="material-symbols-outlined text-[18px]">group</span>
        Users
        <span
          class="ml-1 px-2 py-0.5 rounded-full text-[11px] font-bold"
          :class="
            activeTab === 'users'
              ? 'bg-white/15 text-white'
              : 'bg-slate-200 text-slate-500'
          "
          >{{ users.length }}</span
        >
      </button>
      <button
        role="tab"
        :aria-selected="activeTab === 'roles'"
        class="portal-tab flex items-center gap-2"
        :class="
          activeTab === 'roles'
            ? 'bg-accent text-white shadow-soft'
            : 'text-slate-500 hover:bg-accent-light hover:text-accent'
        "
        @click="openTab('roles')"
      >
        <span class="material-symbols-outlined text-[18px]">shield</span>
        Roles
        <span
          class="ml-1 px-2 py-0.5 rounded-full text-[11px] font-bold"
          :class="
            activeTab === 'roles'
              ? 'bg-white/15 text-white'
              : 'bg-slate-200 text-slate-500'
          "
          >{{ roles.length }}</span
        >
      </button>
    </div>

    <div v-if="loading" class="flex justify-center py-16">
      <div
        class="size-10 border-4 border-accent/30 border-t-accent rounded-full animate-spin"
      ></div>
    </div>

    <template v-else>
      <!-- =================== USERS TAB =================== -->
      <template v-if="activeTab === 'users'">
        <AppDataTable
          class="flex-1 min-h-0"
          v-model:search="userSearch"
          v-model:selected-filters="selectedFilters"
          :columns="userColumns"
          :data="userRows"
          row-key="id"
          :filter-groups="userFilterGroups"
          search-placeholder="Search all users"
          create-label="Add User"
          hide-edit
          hide-delete
          :search-accessor="userSearchAccessor"
          @create="openAddUserModal"
        >
          <template #cell-fullName="{ row }">
            <div class="flex items-center gap-3">
              <div
                class="flex size-10 items-center justify-center rounded-full bg-accent/10 text-sm font-bold text-accent"
              >
                {{ getInitials(row.fullName) }}
              </div>
              <div>
                <p class="text-sm font-bold text-slate-800">
                  {{ row.fullName || row.username }}
                </p>
                <p class="text-xs text-slate-500">
                  {{ row.username }}
                  <span
                    v-if="row.mustChangePassword"
                    class="ml-1 rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-bold text-amber-700"
                    title="The administrator-issued password has not been changed yet."
                  >
                    Temp password
                  </span>
                </p>
              </div>
            </div>
          </template>

          <template #cell-assignedRoleNames="{ row }">
            <div class="flex flex-wrap gap-1.5">
              <span
                v-for="assignment in getUserAssignments(row.id)"
                :key="assignment.id"
                class="group inline-flex items-center gap-1 rounded-full px-2.5 py-1 text-xs font-bold"
                :class="getRoleColor(getRoleCodeById(assignment.roleId))"
              >
                {{ getRoleName(assignment.roleId) }}
                <button
                  class="-mr-2 inline-flex h-10 w-10 items-center justify-center opacity-0 transition-opacity hover:text-red-500 group-hover:opacity-100"
                  title="Remove"
                  @click.stop="removeAssignment(assignment.id)"
                >
                  <span class="material-symbols-outlined text-[14px]"
                    >close</span
                  >
                </button>
              </span>
              <span
                v-if="getUserAssignments(row.id).length === 0"
                class="text-xs italic text-slate-400"
              >
                No roles
              </span>
            </div>
          </template>

          <template #cell-statusLabel="{ row }">
            <div class="flex items-center gap-2">
              <span
                v-if="row.statusLabel === 'Active'"
                class="flex items-center gap-2 text-sm font-medium text-emerald-600"
              >
                <span class="size-2 rounded-full bg-emerald-500"></span>
                Active
              </span>
              <span
                v-else-if="row.statusLabel === 'Locked'"
                class="flex items-center gap-2 text-sm font-medium text-amber-600"
                :title="`Locked out until ${row.lockoutEndOn}`"
              >
                <span class="size-2 rounded-full bg-amber-500"></span>
                Locked
              </span>
              <span
                v-else
                class="flex items-center gap-2 text-sm font-medium text-slate-500"
              >
                <span class="size-2 rounded-full bg-slate-400"></span>
                Inactive
              </span>
              <!-- ApproveUser both activates the account and clears any lockout. -->
              <button
                v-if="row.statusLabel !== 'Active'"
                class="ml-2 inline-flex min-h-10 items-center gap-1 rounded-lg bg-emerald-500 px-3 py-2 text-xs font-bold text-white transition-colors hover:bg-emerald-600"
                @click.stop="approveUser(row)"
              >
                <span class="material-symbols-outlined text-[14px]">check</span>
                {{ row.statusLabel === "Locked" ? "Unlock" : "Approve" }}
              </button>
            </div>
          </template>

          <template #cell-department="{ value }">
            {{ value || "-" }}
          </template>

          <template #extra-actions="{ row }">
            <button
              class="inline-flex min-h-10 items-center gap-1 rounded-lg px-3 py-2 text-xs font-bold text-accent transition-colors hover:bg-accent/10"
              @click.stop="openAssignModal(row)"
            >
              <span class="material-symbols-outlined text-[16px]">add</span>
              Assign Role
            </button>
            <button
              v-if="row.isApproved"
              class="inline-flex min-h-10 items-center gap-1 rounded-lg px-3 py-2 text-xs font-bold text-red-500 transition-colors hover:bg-red-50"
              title="Deactivate user and revoke live sessions"
              @click.stop="deactivateUser(row)"
            >
              <span class="material-symbols-outlined text-[16px]"
                >person_off</span
              >
              Deactivate
            </button>
          </template>
        </AppDataTable>
      </template>

      <!-- =================== ROLES TAB =================== -->
      <template v-if="activeTab === 'roles'">
        <RoleManagementPanel
          :users="users"
          :roles="roles"
          :assignments="assignments"
          :access-functions="accessFunctions"
          @refresh="handleRoleRefresh"
        />
      </template>
    </template>

    <!-- =================== ASSIGN ROLE MODAL (from Users tab) =================== -->
    <Teleport to="body">
      <div
        v-if="showAssignModal"
        class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/20 backdrop-blur-sm p-4"
      >
        <div
          class="bg-white rounded-3xl w-full max-w-lg shadow-2xl flex flex-col overflow-hidden"
        >
          <div
            class="flex items-center justify-between p-6 border-b border-slate-100"
          >
            <h2 class="text-xl font-bold text-slate-800">Assign Role</h2>
            <button
              class="p-2 hover:bg-slate-100 rounded-full text-slate-400"
              @click="showAssignModal = false"
            >
              <span class="material-symbols-outlined">close</span>
            </button>
          </div>
          <div class="p-6 flex flex-col gap-5">
            <div>
              <p class="text-sm font-bold text-slate-500 mb-1">User</p>
              <p class="text-lg font-bold text-slate-800">
                {{ selectedUser?.fullName || selectedUser?.username }}
              </p>
              <p class="text-sm text-slate-500">{{ selectedUser?.username }}</p>
            </div>
            <div class="flex flex-col gap-2">
              <AppSelect
                v-model="selectedRoleId"
                label="Select Role"
                :options="
                  availableRoles.map((r) => ({ value: r.id, label: r.name }))
                "
                placeholder="Choose a role..."
              />
            </div>
          </div>
          <div
            class="p-6 border-t border-slate-100 bg-slate-50 flex items-center justify-end gap-3"
          >
            <button
              class="px-6 py-2.5 rounded-xl font-bold text-slate-500 hover:bg-slate-200 transition-colors"
              @click="showAssignModal = false"
            >
              Cancel
            </button>
            <button
              class="px-6 py-2.5 rounded-xl bg-accent text-white font-bold shadow-soft hover:bg-accent/90 transition-all disabled:opacity-50"
              :disabled="!selectedRoleId || saving"
              @click="saveAssignment"
            >
              {{ saving ? "Saving..." : "Assign Role" }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- =================== ADD USER MODAL =================== -->
    <Teleport to="body">
      <div
        v-if="showAddUserModal"
        class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/20 backdrop-blur-sm p-4"
      >
        <div
          class="bg-white rounded-3xl w-full max-w-lg shadow-2xl flex flex-col overflow-hidden"
        >
          <div
            class="flex items-center justify-between p-6 border-b border-slate-100"
          >
            <h2 class="text-xl font-bold text-slate-800">Add New User</h2>
            <button
              class="p-2 hover:bg-slate-100 rounded-full text-slate-400"
              @click="showAddUserModal = false"
            >
              <span class="material-symbols-outlined">close</span>
            </button>
          </div>
          <div class="p-6 flex flex-col gap-5">
            <p class="text-sm text-slate-500">
              Accounts belong to this application. Most people create their own
              through the sign-up page — add one here only when you need to
              onboard someone directly.
            </p>

            <div class="flex flex-col gap-2">
              <label for="new-user-id" class="text-sm font-bold text-slate-500">
                Username <span class="text-red-500">*</span>
              </label>
              <input
                id="new-user-id"
                v-model="newUser.userId"
                type="text"
                class="h-12 px-4 rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-accent/20 text-slate-800 font-medium"
                placeholder="e.g. jane"
                autocomplete="off"
              />
              <p class="text-xs text-slate-400">
                The login name. It must match the account the person signs in
                with.
              </p>
            </div>

            <div class="flex flex-col gap-2">
              <label
                for="new-user-full-name"
                class="text-sm font-bold text-slate-500"
              >
                Full name
              </label>
              <input
                id="new-user-full-name"
                v-model="newUser.fullName"
                type="text"
                class="h-12 px-4 rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-accent/20 text-slate-800 font-medium"
                placeholder="e.g. Jane Tan"
              />
            </div>

            <div class="flex flex-col gap-2">
              <label
                for="new-user-email"
                class="text-sm font-bold text-slate-500"
              >
                Email
              </label>
              <input
                id="new-user-email"
                v-model="newUser.email"
                type="email"
                class="h-12 px-4 rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-accent/20 text-slate-800 font-medium"
                placeholder="e.g. jane@example.edu"
              />
            </div>

            <div class="flex flex-col gap-2">
              <label
                for="new-user-department"
                class="text-sm font-bold text-slate-500"
              >
                Department
              </label>
              <input
                id="new-user-department"
                v-model="newUser.department"
                type="text"
                class="h-12 px-4 rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-accent/20 text-slate-800 font-medium"
                placeholder="e.g. Registry"
              />
            </div>

            <div class="flex flex-col gap-2">
              <label
                for="new-user-password"
                class="text-sm font-bold text-slate-500"
              >
                Initial password <span class="text-red-500">*</span>
              </label>
              <input
                id="new-user-password"
                v-model="newUser.initialPassword"
                type="password"
                class="h-12 px-4 rounded-xl border border-slate-200 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-accent/20 text-slate-800 font-medium"
                placeholder="Password to hand over"
                autocomplete="new-password"
                @keyup.enter="addUser"
              />
              <p class="text-xs text-slate-400">
                A handover value only — the account is created with "must change
                password" set, so the user is required to replace it the first
                time they sign in.
              </p>
            </div>
          </div>
          <div
            class="p-6 border-t border-slate-100 bg-slate-50 flex items-center justify-end gap-3"
          >
            <button
              class="px-6 py-2.5 rounded-xl font-bold text-slate-500 hover:bg-slate-200 transition-colors"
              @click="showAddUserModal = false"
            >
              Cancel
            </button>
            <button
              class="px-6 py-2.5 rounded-xl bg-accent text-white font-bold shadow-soft hover:bg-accent/90 transition-all disabled:opacity-50"
              :disabled="!canSubmitNewUser || saving"
              @click="addUser"
            >
              {{ saving ? "Adding..." : "Add User" }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
