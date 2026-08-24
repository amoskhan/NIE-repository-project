<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import {
  NieButton,
  NieLoaderSymbol,
  NieResultState,
  NieTabs,
  useToast,
  type NieTabItem,
} from "@nie/ui";
import AccessAssignmentModal from "@/components/admin/access-control/AccessAssignmentModal.vue";
import AccessControlRolesPanel from "@/components/admin/access-control/AccessControlRolesPanel.vue";
import AccessControlUsersPanel from "@/components/admin/access-control/AccessControlUsersPanel.vue";
import AccessFunctionsPanel from "@/components/admin/access-control/AccessFunctionsPanel.vue";
import { AccessFunctionCode } from "@/app-config/accessFunctions";
import { usePermissions } from "@/composables/access-control/usePermissions";
import roleService from "@/services/access-control/roleService";
import { useServerDataTable } from "@/composables/data-tables/useServerDataTable";
import type {
  AccessControlOverview,
  AssignAccessRequest,
  StaffDetails,
  UserAccessSummary,
} from "@/types";

type AccessControlTab = "users" | "roles" | "access-functions";

const route = useRoute();
const router = useRouter();
const toast = useToast();
const { hasPermission } = usePermissions();

const canManageRoles = computed(() =>
  hasPermission(AccessFunctionCode.Api.AccessControlRolesManage),
);
const canManageAssignments = computed(() =>
  hasPermission(AccessFunctionCode.Api.AccessControlAssignmentsManage),
);
const canManageApplicationAccess = computed(() =>
  hasPermission(AccessFunctionCode.Api.ApplicationAccessManage),
);

const loading = ref(true);
const overviewError = ref<string | null>(null);
const saving = ref(false);
const overview = ref<AccessControlOverview>({
  users: [],
  roles: [],
  accessFunctions: [],
  applications: [],
});
const userTable = useServerDataTable<UserAccessSummary>({
  search: roleService.searchUsers,
  getFilterOptions: roleService.getUserFilterOptions,
});
const {
  rows: users,
  totalItems: userTotal,
  loading: usersLoading,
  error: usersError,
  filterOptionPages: userFilterOptionPages,
  load: loadUsers,
  loadFilterOptions: loadUserFilterOptions,
  reload: reloadUsers,
} = userTable;

const showAssignmentModal = ref(false);
const resolvedStaff = ref<StaffDetails | null>(null);
const lookupLoading = ref(false);
const lookupError = ref<string | null>(null);

const validTabs: AccessControlTab[] = ["users", "roles", "access-functions"];
const activeTab = computed<AccessControlTab>({
  get: () => {
    const value = String(route.query.tab ?? "users") as AccessControlTab;
    return validTabs.includes(value) ? value : "users";
  },
  set: (tab) => {
    void router.replace({
      name: "access-control",
      query: tab === "users" ? {} : { tab },
    });
  },
});

const tabs = computed<NieTabItem<AccessControlTab>[]>(() => [
  {
    id: "users",
    label: "Users",
    icon: "group",
    count: userTotal.value,
    panelId: "access-control-users-panel",
  },
  {
    id: "roles",
    label: "Roles",
    icon: "shield_person",
    count: overview.value.roles.length,
    panelId: "access-control-roles-panel",
  },
  {
    id: "access-functions",
    label: "Access Functions",
    icon: "key",
    count: overview.value.accessFunctions.length,
    panelId: "access-control-functions-panel",
  },
]);

onMounted(loadOverview);

watch(
  () => route.query.tab,
  (tab) => {
    if (tab && !validTabs.includes(String(tab) as AccessControlTab)) {
      void router.replace({ name: "access-control" });
    }
  },
  { immediate: true },
);

function apiError(error: unknown, fallback: string): string {
  const axiosError = error as {
    response?: { data?: { message?: string; error?: string } | string };
  };
  const data = axiosError.response?.data;
  if (typeof data === "string" && data.trim()) return data;
  return (typeof data === "object" && (data.message || data.error)) || fallback;
}

async function loadOverview(): Promise<void> {
  loading.value = true;
  overviewError.value = null;
  try {
    overview.value = await roleService.getOverview();
  } catch (error) {
    overviewError.value = apiError(error, "Failed to load access control");
    toast.error(overviewError.value);
  } finally {
    loading.value = false;
  }
}

function staffFromUser(user: UserAccessSummary): StaffDetails {
  return {
    workerId: "",
    userId: user.userId,
    name: user.displayName?.trim() || user.userId,
    email: user.email?.trim() || "",
    department: user.department?.trim() || "",
    departmentDescription: user.departmentDescription?.trim() || "",
    designation: user.designation?.trim() || "",
    title: user.title?.trim() || "",
  };
}

function openAssignment(user: UserAccessSummary | null): void {
  if (!canManageAssignments.value && !canManageApplicationAccess.value) return;
  resolvedStaff.value = user ? staffFromUser(user) : null;
  lookupError.value = null;
  showAssignmentModal.value = true;
}

async function lookupStaff(email: string): Promise<void> {
  lookupLoading.value = true;
  lookupError.value = null;
  try {
    resolvedStaff.value = await roleService.lookupStaff(email);
  } catch (error) {
    lookupError.value = apiError(error, "Staff member could not be resolved");
  } finally {
    lookupLoading.value = false;
  }
}

async function assignAccess(request: AssignAccessRequest): Promise<void> {
  saving.value = true;
  try {
    await roleService.assignAccess(request);
    await Promise.all([loadOverview(), reloadUsers()]);
    showAssignmentModal.value = false;
    toast.success(
      request.scope === "application"
        ? "Application access assigned"
        : "Global roles assigned",
    );
  } catch (error) {
    toast.error(apiError(error, "Failed to assign access"));
  } finally {
    saving.value = false;
  }
}

async function removeGlobalAssignment(id: string): Promise<void> {
  saving.value = true;
  try {
    await roleService.removeGlobalAssignment(id);
    await Promise.all([loadOverview(), reloadUsers()]);
    toast.success("Global role removed");
  } catch (error) {
    toast.error(apiError(error, "Failed to remove global role"));
  } finally {
    saving.value = false;
  }
}

async function removeApplicationAssignment(id: string): Promise<void> {
  saving.value = true;
  try {
    await roleService.removeApplicationAssignment(id);
    await Promise.all([loadOverview(), reloadUsers()]);
    toast.success("Application access removed");
  } catch (error) {
    toast.error(apiError(error, "Failed to remove application access"));
  } finally {
    saving.value = false;
  }
}

async function saveRoleMetadata(value: {
  roleId?: string;
  code: string;
  name: string;
  description: string | null;
  isActive: boolean;
}): Promise<void> {
  const existing = overview.value.roles.find(
    (role) => role.id === value.roleId,
  );
  saving.value = true;
  try {
    await roleService.saveRole({
      ...value,
      id: value.roleId,
      accessFunctionIds: existing?.accessFunctionIds ?? [],
    });
    await loadOverview();
    toast.success(existing ? "Role updated" : "Role created");
  } catch (error) {
    toast.error(apiError(error, "Failed to save role"));
  } finally {
    saving.value = false;
  }
}

async function saveRoleAccess(value: {
  roleId: string;
  accessFunctionIds: string[];
}): Promise<void> {
  const role = overview.value.roles.find((item) => item.id === value.roleId);
  if (!role) return;
  saving.value = true;
  try {
    await roleService.saveRole({
      id: role.id,
      code: role.code,
      name: role.name,
      description: role.description,
      isActive: role.isActive,
      accessFunctionIds: value.accessFunctionIds,
    });
    await loadOverview();
    toast.success("Role access functions updated");
  } catch (error) {
    toast.error(apiError(error, "Failed to update role access functions"));
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <div class="flex min-h-0 flex-1 flex-col gap-6">
    <NieTabs
      v-model="activeTab"
      :items="tabs"
      aria-label="Access control"
      id-prefix="access-control-tabs"
    />

    <div v-if="loading" class="flex justify-center py-16">
      <NieLoaderSymbol size="lg" variant="brand" label="Loading access control" />
    </div>

    <NieResultState
      v-else-if="overviewError"
      variant="error"
      title="Unable to load access control"
      :description="overviewError"
    >
      <template #actions>
        <NieButton variant="outline" @click="loadOverview">Try again</NieButton>
      </template>
    </NieResultState>

    <template v-else>
      <section
        v-if="activeTab === 'users'"
        id="access-control-users-panel"
        role="tabpanel"
        aria-labelledby="access-control-tabs-users"
      >
        <AccessControlUsersPanel
          :users="users"
          :total-items="userTotal"
          :loading="usersLoading"
          :error="usersError"
          :filter-option-pages="userFilterOptionPages"
          :saving="saving"
          :can-manage="canManageAssignments || canManageApplicationAccess"
          @assign="openAssignment"
          @remove-global="removeGlobalAssignment"
          @remove-application="removeApplicationAssignment"
          @query-change="loadUsers"
          @filter-options-request="loadUserFilterOptions"
          @retry="reloadUsers"
        />
      </section>

      <section
        v-else-if="activeTab === 'roles'"
        id="access-control-roles-panel"
        role="tabpanel"
        aria-labelledby="access-control-tabs-roles"
      >
        <AccessControlRolesPanel
          :roles="overview.roles"
          :access-functions="overview.accessFunctions"
          :saving="saving"
          :can-manage="canManageRoles"
          @save-role-access="saveRoleAccess"
          @save-role-metadata="saveRoleMetadata"
        />
      </section>

      <section
        v-else
        id="access-control-functions-panel"
        role="tabpanel"
        aria-labelledby="access-control-tabs-access-functions"
      >
        <AccessFunctionsPanel :access-functions="overview.accessFunctions" />
      </section>
    </template>

    <AccessAssignmentModal
      v-model="showAssignmentModal"
      :roles="overview.roles"
      :applications="overview.applications"
      :resolved-staff="resolvedStaff"
      :saving="saving"
      :lookup-loading="lookupLoading"
      :lookup-error="lookupError"
      :can-assign-global="canManageAssignments"
      :can-assign-application="canManageApplicationAccess"
      @lookup="lookupStaff"
      @assign="assignAccess"
    />
  </div>
</template>
