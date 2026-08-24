<script setup lang="ts">
import { onMounted, onUnmounted, ref } from "vue";
import myInfoService, {
  type MyInfoPersonData,
  type TestProfile,
} from "@/services/myinfo/myInfoService";
import myInfoLoginBtn from "@/assets/myinfo-login-red.svg";
import {
  NieButton,
  NieDataTable,
  NieLoaderSymbol,
  NieResultState,
  NieTabs,
  useToast,
  type NieTabItem,
} from "@nie/ui";
import { useServerDataTable } from "@/composables/data-tables/useServerDataTable";

const toast = useToast();
type MyInfoTabId = "myinfo" | "test-accounts";

const activeTab = ref<MyInfoTabId>("myinfo");
const myInfoTabs: NieTabItem<MyInfoTabId>[] = [
  {
    id: "myinfo",
    label: "MyInfo Login",
    icon: "fingerprint",
    panelId: "myinfo-login-panel",
  },
  {
    id: "test-accounts",
    label: "Test Accounts",
    icon: "groups",
    panelId: "myinfo-test-accounts-panel",
  },
];
const loading = ref(true);
const configured = ref(false);
const configurationError = ref<string | null>(null);
const actionError = ref<string | null>(null);
const authenticating = ref(false);
const personData = ref<MyInfoPersonData | null>(null);

const testProfileTable = useServerDataTable<TestProfile>({
  search: myInfoService.searchTestProfiles,
  getFilterOptions: myInfoService.getTestProfileFilterOptions,
});
const {
  rows: testProfiles,
  totalItems: testProfileTotal,
  loading: testProfilesLoading,
  error: testProfilesError,
  filterOptionPages: testProfileFilterOptionPages,
  load: loadTestProfiles,
  loadFilterOptions: loadTestProfileFilterOptions,
  reload: reloadTestProfiles,
} = testProfileTable;

// Column definitions for the shared DataTable (sorting + search + paging +
// mobile cards are all handled by the component itself — see DataTable.vue).
const testProfileColumns = [
  { key: "uinfin", label: "NRIC/FIN" },
  { key: "name", label: "Name" },
  { key: "sex", label: "Sex" },
  { key: "race", label: "Race" },
  { key: "dob", label: "DOB" },
  { key: "nationality", label: "Nationality" },
  { key: "email", label: "Email" },
  { key: "mobile", label: "Mobile" },
  {
    key: "passType",
    label: "Pass Type",
    chip: { tone: "info", dot: true },
  },
  { key: "postalCode", label: "Postal Code" },
];

async function loadConfiguration() {
  loading.value = true;
  configurationError.value = null;
  try {
    configured.value = await myInfoService.isConfigured();
  } catch {
    configured.value = false;
    configurationError.value = "The MyInfo configuration status could not be loaded.";
  } finally {
    loading.value = false;
  }
}

// Check if MyInfo is configured
onMounted(loadConfiguration);

// Listen for SingPass callback postMessage
function handleMessage(event: MessageEvent) {
  if (event.origin !== window.location.origin) return;
  if (event.data?.type !== "application-auth-callback") return;

  if (event.data.error) {
    actionError.value = `SingPass error: ${event.data.error}`;
    toast.error(actionError.value);
    authenticating.value = false;
    return;
  }

  if (event.data.code && event.data.state) {
    exchangeCode(event.data.code, event.data.state);
  }
}

onMounted(() => window.addEventListener("message", handleMessage));
onUnmounted(() => window.removeEventListener("message", handleMessage));

async function loginWithSingPass() {
  actionError.value = null;
  if (!configured.value) {
    toast.error(
      "MyInfo/SingPass is not configured. Please contact your administrator to set up the MyInfo integration.",
    );
    return;
  }
  authenticating.value = true;
  try {
    const url = await myInfoService.getAuthorizeUrl();
    window.open(url, "singpass-login", "width=600,height=700");
  } catch (err: unknown) {
    const message =
      err instanceof Error && err.message
        ? err.message
        : "Failed to initiate SingPass login. MyInfo may not be configured on the server.";
    actionError.value = message;
    toast.error(message);
    authenticating.value = false;
  }
}

async function exchangeCode(code: string, state: string) {
  actionError.value = null;
  try {
    personData.value = await myInfoService.callback(code, state);
    toast.success("MyInfo data retrieved successfully");
  } catch {
    actionError.value = "MyInfo data could not be retrieved.";
    toast.error(actionError.value);
  } finally {
    authenticating.value = false;
  }
}

function clearData() {
  personData.value = null;
}

function getPersonFieldValue(key: keyof MyInfoPersonData): string {
  return personData.value?.[key] || "—";
}

// Person data display cards
const infoCards = [
  {
    title: "Personal Information",
    icon: "person",
    fields: [
      { label: "Full Name", key: "name" },
      { label: "NRIC/FIN", key: "nricFin" },
      { label: "Sex", key: "sex" },
      { label: "Race", key: "race" },
      { label: "Date of Birth", key: "dateOfBirth" },
      { label: "Nationality", key: "nationality" },
      { label: "Birth Country", key: "birthCountry" },
      { label: "Residential Status", key: "residentialStatus" },
      { label: "Marital Status", key: "maritalStatus" },
    ],
  },
  {
    title: "Contact Details",
    icon: "contact_mail",
    fields: [
      { label: "Email", key: "email" },
      { label: "Mobile", key: "mobileNumber" },
    ],
  },
  {
    title: "Address",
    icon: "home",
    fields: [
      { label: "Block", key: "blockNumber" },
      { label: "Street", key: "streetName" },
      { label: "Floor", key: "floorNumber" },
      { label: "Unit", key: "unitNumber" },
      { label: "Postal Code", key: "postalCode" },
      { label: "Full Address", key: "registeredAddress" },
    ],
  },
  {
    title: "Education & Employment",
    icon: "school",
    fields: [
      { label: "Highest Qualification", key: "highestQualification" },
      { label: "Occupation", key: "occupation" },
      { label: "Employer", key: "employerName" },
      { label: "Subject", key: "subject" },
    ],
  },
];
</script>

<template>
  <div class="space-y-6">
    <NieTabs
      v-model="activeTab"
      :items="myInfoTabs"
      aria-label="MyInfo"
      id-prefix="myinfo-tabs"
    />

    <!-- Tab 1: MyInfo Login -->
    <div
      v-if="activeTab === 'myinfo'"
      id="myinfo-login-panel"
      role="tabpanel"
      aria-labelledby="myinfo-tabs-myinfo"
    >
      <!-- Loading state -->
      <div
        v-if="loading"
        class="flex items-center justify-center py-16 text-secondary-400"
      >
        <NieLoaderSymbol size="sm" class="mr-2" label="Checking configuration" />
        <span>Checking configuration...</span>
      </div>

      <NieResultState
        v-else-if="configurationError"
        variant="error"
        title="Unable to check MyInfo"
        :description="configurationError"
      >
        <template #actions>
          <NieButton variant="outline" @click="loadConfiguration">Try again</NieButton>
        </template>
      </NieResultState>

      <NieResultState
        v-else-if="actionError"
        variant="error"
        title="Unable to retrieve MyInfo data"
        :description="actionError"
      >
        <template #actions>
          <NieButton variant="outline" @click="loginWithSingPass">Try again</NieButton>
        </template>
      </NieResultState>

      <!-- Not configured -->
      <div
        v-else-if="!configured"
        class="rounded-xl border border-warning-200 bg-warning-50 p-6 text-center"
      >
        <span class="material-symbols-outlined text-warning-500 text-4xl mb-2"
          >warning</span
        >
        <p class="text-warning-800 font-medium">MyInfo is not configured</p>
        <p class="text-warning-600 text-sm mt-1">
          Please configure MyInfo settings in appsettings.json to enable
          SingPass integration.
        </p>
      </div>

      <!-- Login prompt (no data yet) -->
      <div
        v-else-if="!personData"
        class="max-w-lg mx-auto text-center space-y-6 py-8"
      >
        <div
          class="mx-auto size-16 rounded-full bg-accent/10 text-accent flex items-center justify-center"
        >
          <span class="material-symbols-outlined text-hero">fingerprint</span>
        </div>
        <div>
          <h2 class="text-xl font-bold text-secondary-900">Retrieve MyInfo Data</h2>
          <p class="mt-2 text-secondary-500 text-sm">
            Login with SingPass to retrieve your MyInfo personal data. This uses
            the MyInfo FAPI v5 integration with SingPass staging environment.
          </p>
        </div>
        <button
          class="inline-flex items-center gap-2 disabled:opacity-50"
          :disabled="authenticating"
          @click="loginWithSingPass"
        >
          <img
            :src="myInfoLoginBtn"
            alt="Login with MyInfo"
            class="h-12 cursor-pointer hover:opacity-90 transition-opacity"
          />
        </button>
        <p v-if="authenticating" class="text-sm text-secondary-400 animate-pulse">
          Waiting for SingPass authentication...
        </p>
      </div>

      <!-- Person data display -->
      <div v-else class="space-y-6">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-3">
            <div
              class="size-10 rounded-full bg-success-50 text-success-600 flex items-center justify-center"
            >
              <span class="material-symbols-outlined">verified</span>
            </div>
            <div>
              <h2 class="text-lg font-bold text-secondary-900">
                {{ personData.name || "MyInfo Data" }}
              </h2>
              <p class="text-sm text-secondary-500">
                Verified at
                {{
                  personData.verifiedAtUtc
                    ? new Date(personData.verifiedAtUtc).toLocaleString()
                    : "just now"
                }}
              </p>
            </div>
          </div>
          <button
            class="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-secondary-600 bg-secondary-100 rounded-lg hover:bg-secondary-200 transition-colors"
            @click="clearData"
          >
            <span class="material-symbols-outlined text-body-lg">close</span>
            Clear
          </button>
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div
            v-for="card in infoCards"
            :key="card.title"
            class="rounded-xl border border-secondary-200 bg-white p-5"
          >
            <div class="flex items-center gap-2 mb-4">
              <span class="material-symbols-outlined text-accent text-section-title">{{
                card.icon
              }}</span>
              <h3 class="font-semibold text-secondary-900 text-sm">
                {{ card.title }}
              </h3>
            </div>
            <dl class="space-y-2.5">
              <div
                v-for="field in card.fields"
                :key="field.key"
                class="flex justify-between text-sm"
              >
                <dt class="text-secondary-500">{{ field.label }}</dt>
                <dd class="text-secondary-900 font-medium text-right">
                  {{ getPersonFieldValue(field.key) }}
                </dd>
              </div>
            </dl>
          </div>
        </div>
      </div>
    </div>

    <!-- Tab 2: Test Accounts — uses the shared DataTable so it matches the
         look-and-feel of every other admin/data page in the app. -->
    <div
      v-if="activeTab === 'test-accounts'"
      id="myinfo-test-accounts-panel"
      role="tabpanel"
      aria-labelledby="myinfo-tabs-test-accounts"
    >
      <NieDataTable
        preference-key="myinfo.test-profiles"
        :definition-version="1"
        :columns="testProfileColumns"
        :data="testProfiles"
        server-side
        :total-items="testProfileTotal"
        :filter-option-pages="testProfileFilterOptionPages"
        row-key="uinfin"
        :loading="testProfilesLoading"
        :error="testProfilesError"
        hide-create
        hide-actions
        search-placeholder="Search test accounts"
        empty-state-title="No test accounts"
        empty-state-message="No SingPass staging profiles are available."
        @query-change="loadTestProfiles"
        @filter-options-request="loadTestProfileFilterOptions"
        @retry="reloadTestProfiles"
      />
    </div>
  </div>
</template>
