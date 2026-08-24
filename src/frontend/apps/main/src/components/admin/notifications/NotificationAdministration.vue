<script setup lang="ts">
import { computed, onMounted, ref, shallowRef, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { toTypedSchema } from "@vee-validate/zod";
import { useForm } from "vee-validate";
import { z } from "zod";
import {
  NieButton,
  NieBadge,
  NieDataTable,
  NieInput,
  NieLoaderSymbol,
  NieResultState,
  NieSwitch,
  NieTabs,
  NieTextarea,
  useToast,
  type NieTabItem,
} from "@nie/ui";
import nieLogoUrl from "@/assets/nie-logo.svg";
import { buildNotificationEmailPreview } from "@/components/admin/notifications/notificationEmailPreview";
import {
  buildNotificationPolicyUpdatePayload,
  validateNotificationPolicyTiming,
} from "@/components/admin/notifications/notificationPolicyTiming";
import notificationAdministrationService, {
  type NotificationAdministrationOverview,
  type NotificationDelivery,
  type NotificationPolicy,
  type NotificationTemplate,
} from "@/services/notifications/notificationAdministrationService";
import { useServerDataTable } from "@/composables/data-tables/useServerDataTable";

type TabId = "policies" | "templates" | "delivery";

const toast = useToast();
const route = useRoute();
const router = useRouter();

interface ApiErrorShape {
  response?: {
    data?: {
      message?: string;
      errors?: unknown;
    };
  };
}

function apiError(error: unknown): ApiErrorShape {
  return typeof error === "object" && error !== null
    ? (error as ApiErrorShape)
    : {};
}
const loading = ref(true);
const overviewError = shallowRef<string | null>(null);
const routeTabIds: Readonly<Record<string, TabId>> = {
  policies: "policies",
  "email-templates": "templates",
  templates: "templates",
  delivery: "delivery",
};
const activeTab = computed<TabId>({
  get: () => routeTabIds[String(route.query.tab ?? "policies")] ?? "policies",
  set: (tab) => {
    const query = { ...route.query };
    if (tab === "policies") {
      delete query.tab;
    } else {
      query.tab = tab === "templates" ? "email-templates" : tab;
    }
    void router.replace({ name: "notification-administration", query });
  },
});
const overview = ref<NotificationAdministrationOverview | null>(null);
const savingPolicyKey = ref<string | null>(null);
const savingTemplate = ref(false);
const selectedEventKey = ref("");
const previewVisible = ref(true);
const deliveryTable = useServerDataTable<NotificationDelivery>({
  search: notificationAdministrationService.searchDeliveries,
  getFilterOptions:
    notificationAdministrationService.getDeliveryFilterOptions,
});
const {
  rows: deliveries,
  totalItems: deliveryTotal,
  loading: deliveriesLoading,
  error: deliveriesError,
  filterOptionPages: deliveryFilterOptionPages,
  load: loadDeliveries,
  loadFilterOptions: loadDeliveryFilterOptions,
  reload: reloadDeliveries,
} = deliveryTable;

const unsafeTemplateMarkup =
  /<\s*\/?\s*(script|iframe|object|embed|form|style|link|meta|img|svg|video|audio|source|picture)\b|\son[a-z]+\s*=|javascript:|url\(/i;
const templateFormSchema = toTypedSchema(
  z.object({
    subject: z
      .string()
      .trim()
      .min(1, "Subject is required")
      .max(240, "Subject cannot exceed 240 characters"),
    content: z
      .string()
      .trim()
      .min(1, "Content is required")
      .max(20_000, "Content cannot exceed 20,000 characters")
      .refine(
        (value) => !unsafeTemplateMarkup.test(value),
        "Content contains unsupported or unsafe HTML",
      ),
  }),
);
const {
  defineField,
  errors: templateErrors,
  validate: validateTemplateForm,
} = useForm({
  validationSchema: templateFormSchema,
  initialValues: { subject: "", content: "" },
});
const [templateSubject] = defineField("subject");
const [templateContent] = defineField("content");

const policies = computed(() => overview.value?.policies ?? []);
const templates = computed(() => overview.value?.templates ?? []);
const deliveryColumns = [
  { key: "eventKey", label: "Event", filter: true },
  { key: "recipientName", label: "Recipient", filter: true },
  {
    key: "channel",
    label: "Channel",
    filter: true,
    chip: { tone: "info" },
  },
  {
    key: "status",
    label: "Status",
    filter: true,
    chip: {
      toneMap: {
        Sent: "success",
        Pending: "warning",
        Retrying: "warning",
        Failed: "danger",
        Skipped: "default",
      },
      dot: true,
    },
  },
  { key: "attempts", label: "Attempts", type: "number" as const },
  { key: "createdOn", label: "Updated", type: "date" as const },
];
const placeholders = computed(() => overview.value?.allowedPlaceholders ?? []);
const tabs = computed<NieTabItem<TabId>[]>(() => [
  {
    id: "policies",
    label: "Policies",
    icon: "tune",
    panelId: "notification-policies-panel",
  },
  {
    id: "templates",
    label: "Email templates",
    icon: "mail",
    panelId: "notification-templates-panel",
  },
  {
    id: "delivery",
    label: "Delivery",
    icon: "outbox",
    count: deliveryTotal.value,
    panelId: "notification-delivery-panel",
  },
]);

const policyGroups = computed(() => {
  const groups = new Map<string, NotificationPolicy[]>();
  for (const policy of policies.value) {
    const current = groups.get(policy.category) ?? [];
    current.push(policy);
    groups.set(policy.category, current);
  }
  return Array.from(groups, ([category, items]) => ({ category, items }));
});

const eventOptions = computed(() =>
  policies.value.map((policy) => ({
    eventKey: policy.eventKey,
    label: policy.displayName,
  })),
);

const selectedVersions = computed(() =>
  templates.value
    .filter((template) => template.eventKey === selectedEventKey.value)
    .sort((left, right) => right.version - left.version),
);

const publishedTemplate = computed(() =>
  selectedVersions.value.find((template) => template.isPublished),
);

const previewDocument = computed(() =>
  buildNotificationEmailPreview({
    content: templateContent.value,
    logoUrl: nieLogoUrl,
    applicationName: "NIE Template",
  }),
);

const deliveryCounts = computed(() => {
  const counts = overview.value?.deliveryStatusCounts ?? {};
  return {
    sent: counts.Sent ?? counts.sent ?? 0,
    retry:
      (counts.Retry ?? counts.retry ?? 0) +
      (counts.Pending ?? counts.pending ?? 0),
    failed: counts.Failed ?? counts.failed ?? 0,
    skipped: counts.Skipped ?? counts.skipped ?? 0,
  };
});

async function loadOverview(showLoader = true) {
  if (showLoader) loading.value = true;
  overviewError.value = null;
  try {
    overview.value = await notificationAdministrationService.getOverview();
    if (!selectedEventKey.value && eventOptions.value.length > 0) {
      selectEvent(eventOptions.value[0].eventKey);
    } else if (selectedEventKey.value) {
      selectEvent(selectedEventKey.value);
    }
  } catch {
    overviewError.value =
      "Notification configuration could not be loaded. Try again.";
    toast.error("Notification configuration could not be loaded");
  } finally {
    if (showLoader) loading.value = false;
  }
}

function selectEvent(eventKey: string) {
  selectedEventKey.value = eventKey;
  const published = templates.value
    .filter((template) => template.eventKey === eventKey)
    .sort((left, right) => right.version - left.version)
    .find((template) => template.isPublished);
  templateSubject.value = published?.subject ?? "";
  templateContent.value = published?.content ?? "";
}

function loadVersion(template: NotificationTemplate) {
  templateSubject.value = template.subject;
  templateContent.value = template.content;
}

function appendPlaceholder(placeholder: string) {
  templateContent.value += `{${placeholder}}`;
}

function isMandatoryApprovalPolicy(policy: NotificationPolicy) {
  return policy.category === "Approval tasks";
}

async function savePolicy(policy: NotificationPolicy) {
  const timingError = validateNotificationPolicyTiming(policy);
  if (timingError) {
    toast.error(timingError);
    return;
  }
  savingPolicyKey.value = policy.eventKey;
  try {
    await notificationAdministrationService.updatePolicy(
      policy.eventKey,
      buildNotificationPolicyUpdatePayload(policy),
    );
    toast.success(`${policy.displayName} policy saved`);
  } catch (error: unknown) {
    toast.error(
      apiError(error).response?.data?.message ??
        "Notification policy could not be saved",
    );
    await loadOverview(false);
  } finally {
    savingPolicyKey.value = null;
  }
}

async function publishTemplate() {
  if (!selectedEventKey.value) return;
  const validation = await validateTemplateForm();
  if (!validation.valid) {
    toast.error(
      "Correct the email template validation errors before publishing",
    );
    return;
  }
  savingTemplate.value = true;
  try {
    await notificationAdministrationService.saveTemplate({
      eventKey: selectedEventKey.value,
      subject: templateSubject.value,
      content: templateContent.value,
      publish: true,
    });
    toast.success("A new email template version was published");
    await loadOverview(false);
  } catch (error: unknown) {
    const response = apiError(error).response?.data;
    const errors = response?.errors;
    toast.error(
      Array.isArray(errors)
        ? errors.join(" ")
        : (response?.message ?? "Template could not be published"),
    );
  } finally {
    savingTemplate.value = false;
  }
}

async function restoreVersion(template: NotificationTemplate) {
  try {
    await notificationAdministrationService.publishTemplate(template.id);
    toast.success(`Version ${template.version} is now published`);
    await loadOverview(false);
  } catch {
    toast.error("Template version could not be published");
  }
}

async function retryDelivery(delivery: NotificationDelivery) {
  try {
    await notificationAdministrationService.retryDelivery(delivery.id);
    toast.success("Delivery queued for retry");
    await Promise.all([loadOverview(false), reloadDeliveries()]);
  } catch {
    toast.error("Delivery could not be queued");
  }
}

function formatDate(value?: string | null): string {
  if (!value) return "—";
  return new Intl.DateTimeFormat("en-SG", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}

function eventLabel(eventKey: string): string {
  return (
    policies.value.find((policy) => policy.eventKey === eventKey)
      ?.displayName ?? eventKey
  );
}

onMounted(loadOverview);

watch(
  () => route.query.tab,
  (tab) => {
    if (tab && !routeTabIds[String(tab)]) {
      const query = { ...route.query };
      delete query.tab;
      void router.replace({ name: "notification-administration", query });
    }
  },
  { immediate: true },
);
</script>

<template>
  <div class="notification-admin">
    <NieTabs
      v-model="activeTab"
      :items="tabs"
      aria-label="Notification administration"
      id-prefix="notification-tabs"
    />

    <div v-if="loading" class="notification-admin__loading">
      <NieLoaderSymbol
        size="lg"
        variant="brand"
        label="Loading notification configuration"
      />
      Loading notification configuration…
    </div>

    <NieResultState
      v-else-if="overviewError"
      compact
      variant="error"
      title="Unable to load notification configuration"
      :description="overviewError"
    >
      <template #actions>
        <NieButton
          variant="outline"
          aria-label="Retry loading notification configuration"
          @click="loadOverview()"
        >
          Try again
        </NieButton>
      </template>
    </NieResultState>

    <template v-else-if="overview">
      <main
        v-if="activeTab === 'policies'"
        id="notification-policies-panel"
        class="policy-groups"
        role="tabpanel"
        aria-labelledby="notification-tabs-policies"
      >
        <section
          v-for="group in policyGroups"
          :key="group.category"
          class="policy-group"
        >
          <div class="policy-group__heading">
            <h2>{{ group.category }}</h2>
            <p>
              Recipient stages are fixed by the Procurement approval workflow;
              channels are configurable for every event, while reminder and
              escalation timing are optional only for reminder emails.
            </p>
          </div>

          <div class="policy-table">
            <article
              v-for="policy in group.items"
              :key="policy.eventKey"
              class="policy-row"
            >
              <div class="policy-row__identity">
                <div class="policy-row__title">
                  <span
                    class="policy-row__indicator"
                    :class="{ 'policy-row__indicator--off': !policy.isActive }"
                  />
                  <strong>{{ policy.displayName }}</strong>
                </div>
                <p>{{ policy.description }}</p>
                <code>{{ policy.eventKey }}</code>
              </div>

              <div class="policy-row__configuration">
                <div class="policy-row__channels">
                  <label>
                    <NieSwitch
                      :model-value="policy.inAppEnabled"
                      :disabled="true"
                    />
                    <span>In-app</span>
                  </label>
                  <label>
                    <NieSwitch
                      :model-value="policy.emailEnabled"
                      @update:model-value="
                        policy.emailEnabled = Boolean($event)
                      "
                    />
                    <span>Email</span>
                  </label>
                  <label>
                    <NieSwitch
                      :model-value="policy.pushEnabled"
                      @update:model-value="policy.pushEnabled = Boolean($event)"
                    />
                    <span>Push notifications</span>
                  </label>
                </div>

                <div
                  v-if="policy.supportsReminderConfiguration"
                  class="policy-row__timing"
                >
                  <div>
                    <NieInput
                      :model-value="policy.reminderAfterHours"
                      type="number"
                      label="Reminder after"
                      placeholder="Optional"
                      :min="1"
                      :max="720"
                      @update:model-value="
                        policy.reminderAfterHours =
                          $event === null ? null : Number($event)
                      "
                    />
                    <small>hours (optional)</small>
                  </div>
                  <div>
                    <NieInput
                      :model-value="policy.escalationAfterHours"
                      type="number"
                      label="Escalation after"
                      placeholder="Optional"
                      :min="1"
                      :max="2160"
                      @update:model-value="
                        policy.escalationAfterHours =
                          $event === null ? null : Number($event)
                      "
                    />
                    <small>hours (optional)</small>
                  </div>
                </div>
              </div>

              <div class="policy-row__actions">
                <label class="policy-row__active">
                  <NieSwitch
                    :model-value="policy.isActive"
                    :disabled="isMandatoryApprovalPolicy(policy)"
                    @update:model-value="policy.isActive = Boolean($event)"
                  />
                  <span>Active</span>
                </label>
                <NieButton
                  size="sm"
                  :loading="savingPolicyKey === policy.eventKey"
                  @click="savePolicy(policy)"
                >
                  Save
                </NieButton>
              </div>
            </article>
          </div>
        </section>
      </main>

      <main
        v-else-if="activeTab === 'templates'"
        id="notification-templates-panel"
        class="template-workspace"
        role="tabpanel"
        aria-labelledby="notification-tabs-templates"
      >
        <aside class="template-workspace__events">
          <h2>Email events</h2>
          <button
            v-for="event in eventOptions"
            :key="event.eventKey"
            type="button"
            :class="{ active: selectedEventKey === event.eventKey }"
            @click="selectEvent(event.eventKey)"
          >
            <span>{{ event.label }}</span>
            <span class="material-symbols-outlined">chevron_right</span>
          </button>
        </aside>

        <section class="template-editor">
          <div class="template-editor__heading">
            <div>
              <h2>{{ eventLabel(selectedEventKey) }}</h2>
              <p>
                Published
                {{
                  publishedTemplate
                    ? `version ${publishedTemplate.version}`
                    : "template unavailable"
                }}
              </p>
            </div>
            <NieButton :loading="savingTemplate" @click="publishTemplate">
              <span class="material-symbols-outlined">publish</span>
              Publish new version
            </NieButton>
          </div>

          <NieInput
            v-model="templateSubject"
            label="Subject"
            placeholder="Email subject"
            :maxlength="240"
            :error="templateErrors.subject"
          />

          <div class="placeholder-list">
            <span>Insert placeholder</span>
            <button
              v-for="placeholder in placeholders"
              :key="placeholder"
              type="button"
              class="font-mono"
              @click="appendPlaceholder(placeholder)"
            >
              {{ placeholder }}
            </button>
          </div>

          <NieTextarea
            v-model="templateContent"
            label="Content inside the NIE email wrapper"
            :rows="15"
            :maxlength="20000"
            :spellcheck="true"
            :error="templateErrors.content"
          />

          <div class="template-history">
            <div class="template-history__heading">
              <h3>Version history</h3>
              <NieButton
                type="button"
                size="sm"
                variant="ghost"
                @click="previewVisible = !previewVisible"
              >
                {{ previewVisible ? "Hide preview" : "Show preview" }}
              </NieButton>
            </div>
            <div class="template-history__versions">
              <button
                v-for="version in selectedVersions"
                :key="version.id"
                type="button"
                @click="loadVersion(version)"
              >
                <span>
                  Version {{ version.version }}
                  <small v-if="version.isPublished">Published</small>
                </span>
                <span>{{ formatDate(version.publishedOn) }}</span>
                <NieButton
                  v-if="!version.isPublished"
                  size="sm"
                  variant="outline"
                  @click.stop="restoreVersion(version)"
                >
                  Restore
                </NieButton>
              </button>
            </div>
          </div>
        </section>

        <aside v-if="previewVisible" class="template-preview">
          <div class="template-preview__chrome">
            <span>Preview</span>
            <span class="material-symbols-outlined">desktop_windows</span>
          </div>
          <div class="template-preview__email">
            <div class="template-preview__message-meta">
              <span>Subject</span>
              <strong>{{ templateSubject || "Email subject" }}</strong>
            </div>
            <iframe
              class="template-preview__content"
              title="Email content preview"
              sandbox="allow-same-origin"
              :srcdoc="previewDocument"
            />
          </div>
        </aside>
      </main>

      <main
        v-else
        id="notification-delivery-panel"
        class="delivery-panel"
        role="tabpanel"
        aria-labelledby="notification-tabs-delivery"
      >
        <section class="delivery-summary">
          <div>
            <span class="delivery-summary__value">{{
              deliveryCounts.sent
            }}</span>
            <span>Sent</span>
          </div>
          <div>
            <span class="delivery-summary__value">{{
              deliveryCounts.retry
            }}</span>
            <span>Pending / retrying</span>
          </div>
          <div>
            <span class="delivery-summary__value">{{
              deliveryCounts.failed
            }}</span>
            <span>Failed</span>
          </div>
          <div>
            <span class="delivery-summary__value">{{
              deliveryCounts.skipped
            }}</span>
            <span>Skipped</span>
          </div>
        </section>

        <NieDataTable
          preference-key="administration.notification-deliveries"
          :definition-version="1"
          :columns="deliveryColumns"
          :data="deliveries"
          server-side
          :total-items="deliveryTotal"
          :filter-option-pages="deliveryFilterOptionPages"
          row-key="id"
          :loading="deliveriesLoading"
          :error="deliveriesError"
          :show-toolbar="true"
          :hide-create="true"
          :hide-edit="true"
          :hide-delete="true"
          appearance="minimal"
          search-placeholder="Search delivery history"
          empty-state-title="No delivery history"
          empty-state-message="Delivery history will appear after the first workflow event."
          max-height="calc(100dvh - 22rem)"
          @query-change="loadDeliveries"
          @filter-options-request="loadDeliveryFilterOptions"
          @retry="reloadDeliveries"
        >
          <template #cell-eventKey="{ row }">
            <div class="delivery-cell">
              <strong>{{ eventLabel(row.eventKey) }}</strong>
              <code>{{ row.correlationKey }}</code>
            </div>
          </template>
          <template #cell-recipientName="{ row }">
            <div class="delivery-cell">
              <strong>{{ row.recipientName || row.recipientUserId }}</strong>
              <span>{{ row.recipientEmail || row.recipientUserId }}</span>
            </div>
          </template>
          <template #cell-status="{ row }">
            <div class="delivery-cell">
              <NieBadge
                :variant="
                  row.status === 'Sent'
                    ? 'success'
                    : row.status === 'Failed'
                      ? 'danger'
                      : row.status === 'Pending' || row.status === 'Retrying'
                        ? 'warning'
                        : 'default'
                "
                dot
                rounded
              >
                {{ row.status }}
              </NieBadge>
              <small v-if="row.lastError">{{ row.lastError }}</small>
            </div>
          </template>
          <template #cell-createdOn="{ row }">
            {{
              formatDate(row.sentOn || row.nextAttemptOn || row.createdOn)
            }}
          </template>
          <template #extra-actions="{ row }">
            <NieButton
              v-if="row.status === 'Failed' || row.status === 'Skipped'"
              size="sm"
              variant="ghost"
              class="min-w-11 px-0"
              title="Retry delivery"
              aria-label="Retry delivery"
              @click="retryDelivery(row)"
            >
              <span class="material-symbols-outlined">replay</span>
            </NieButton>
          </template>
        </NieDataTable>
      </main>
    </template>
  </div>
</template>

<style scoped>
.notification-admin {
  display: grid;
  gap: var(--theme-space-5);
  min-width: 0;
  color: var(--color-text);
}

.notification-admin__loading {
  display: flex;
  min-height: 20rem;
  align-items: center;
  justify-content: center;
  gap: var(--theme-space-3);
  color: var(--theme-color-text-soft);
}

.status {
  display: inline-flex;
  align-items: center;
  width: fit-content;
  border-radius: var(--theme-radius-pill);
  padding: var(--theme-space-1) var(--theme-space-2);
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-bold);
  white-space: nowrap;
}

.status--sent {
  background: var(--theme-color-success-surface);
  color: var(--theme-color-success-700);
}

.status--retry,
.status--pending {
  background: var(--theme-color-warning-100);
  color: var(--theme-color-warning-700);
}

.status--failed {
  background: var(--theme-color-danger-100);
  color: var(--theme-color-danger-700);
}

.status--skipped,
.status--not-configured {
  background: var(--theme-color-surface-subtle);
  color: var(--theme-color-text-muted);
}

.policy-groups {
  display: grid;
  grid-auto-rows: max-content;
  align-content: start;
  gap: var(--theme-space-5);
  max-height: calc(100dvh - 13.5rem);
  overflow-y: auto;
  overscroll-behavior: contain;
  padding-right: var(--theme-space-1);
}

.policy-group,
.delivery-panel {
  overflow: hidden;
  border: 1px solid var(--color-border);
  border-radius: var(--theme-radius-panel);
  background: var(--color-surface);
  box-shadow: var(--theme-shadow-soft);
}

.policy-group__heading {
  padding: var(--theme-space-4) var(--theme-space-5);
  border-bottom: 1px solid var(--color-border);
}

.policy-group__heading h2,
.template-workspace h2 {
  font-size: var(--theme-font-size-body-lg);
  font-weight: var(--theme-font-weight-bold);
}

.policy-group__heading p {
  margin-top: var(--theme-space-1);
  color: var(--color-text-muted);
  font-size: var(--theme-font-size-label);
}

.policy-row {
  display: grid;
  grid-template-columns: minmax(15rem, 1.2fr) minmax(28rem, 1.8fr) auto;
  align-items: center;
  gap: var(--theme-space-4);
  padding: var(--theme-space-4) var(--theme-space-5);
  border-bottom: 1px solid var(--color-border);
}

.policy-row:last-child {
  border-bottom: 0;
}

.policy-row__identity {
  min-width: 0;
}

.policy-row__title {
  display: flex;
  align-items: center;
  gap: var(--theme-space-2);
  font-size: var(--theme-font-size-body);
}

.policy-row__indicator {
  flex: 0 0 auto;
  width: 0.52rem;
  height: 0.52rem;
  border-radius: var(--theme-radius-circle);
  background: var(--theme-color-success-solid);
  box-shadow: 0 0 0 4px var(--theme-color-success-100);
}

.policy-row__indicator--off {
  background: var(--theme-color-neutral-400);
  box-shadow: 0 0 0 4px var(--theme-color-border-default);
}

.policy-row__identity p {
  margin: var(--theme-space-1) 0;
  color: var(--color-text-muted);
  font-size: var(--theme-font-size-caption);
  line-height: 1.35;
}

.policy-row__identity code,
.delivery-table code {
  display: block;
  overflow: hidden;
  color: var(--theme-color-text-muted);
  font-size: var(--theme-font-size-caption);
  text-overflow: ellipsis;
  white-space: nowrap;
}

.policy-row__configuration {
  display: grid;
  gap: var(--theme-space-3);
  min-width: 0;
}

.policy-row__channels {
  display: flex;
  flex-wrap: wrap;
  gap: var(--theme-space-3);
}

.policy-row__channels label,
.policy-row__active {
  display: flex;
  align-items: center;
  gap: var(--theme-space-1);
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-semibold);
}

.policy-row__timing {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: var(--theme-space-2);
}

.policy-row__timing label > span,
.field > span {
  display: block;
  margin-bottom: var(--theme-space-1);
  color: var(--color-text-muted);
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-bold);
}

.policy-row__timing label > div {
  display: flex;
  align-items: center;
  overflow: hidden;
  border: 1px solid var(--color-border);
  border-radius: var(--theme-radius-control);
}

.policy-row__timing input {
  min-width: 0;
  width: 4rem;
  border: 0;
  padding: var(--theme-space-2) var(--theme-space-2);
  font-size: var(--theme-font-size-caption);
}

.policy-row__timing small {
  padding-right: var(--theme-space-2);
  color: var(--color-text-muted);
  font-size: var(--theme-font-size-caption);
}

.policy-row__actions {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: var(--theme-space-3);
}

.template-workspace {
  display: grid;
  grid-template-columns: minmax(12rem, 0.65fr) minmax(24rem, 1.45fr) minmax(
      18rem,
      0.9fr
    );
  height: calc(100dvh - 13.5rem);
  min-height: 32rem;
  max-height: 56rem;
  overflow: hidden;
  border: 1px solid var(--color-border);
  border-radius: var(--theme-radius-panel);
  background: var(--color-surface);
  box-shadow: var(--theme-shadow-soft);
}

.template-workspace__events {
  min-height: 0;
  overflow-y: auto;
  overscroll-behavior: contain;
  padding: var(--theme-space-4) var(--theme-space-3);
  border-right: 1px solid var(--color-border);
  background: var(--color-surface-muted, var(--theme-color-surface-subtle));
}

.template-workspace__events h2 {
  padding: 0 var(--theme-space-2) var(--theme-space-3);
}

.template-workspace__events > button {
  display: flex;
  width: 100%;
  align-items: center;
  justify-content: space-between;
  gap: var(--theme-space-2);
  border-radius: var(--theme-radius-control);
  padding: var(--theme-space-3) var(--theme-space-3);
  color: var(--theme-color-text-soft);
  font-size: var(--theme-font-size-label);
  font-weight: var(--theme-font-weight-semibold);
  text-align: left;
}

.template-workspace__events > button.active {
  background: var(--color-surface);
  color: var(--theme-color-text-strong);
  box-shadow: var(--theme-shadow-soft);
}

.template-editor {
  display: grid;
  align-content: start;
  gap: var(--theme-space-4);
  min-width: 0;
  min-height: 0;
  overflow-y: auto;
  overscroll-behavior: contain;
  padding: var(--theme-space-5);
}

.template-editor__heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: var(--theme-space-4);
}

.template-editor__heading p {
  margin-top: var(--theme-space-1);
  color: var(--color-text-muted);
  font-size: var(--theme-font-size-caption);
}

.field input,
.field textarea {
  width: 100%;
  border: 1px solid var(--color-border);
  border-radius: var(--theme-radius-control);
  background: var(--color-surface);
  padding: var(--theme-space-3) var(--theme-space-3);
  color: var(--color-text);
  font-size: var(--theme-font-size-label);
}

.field-error {
  display: block;
  margin-top: var(--theme-space-1-5);
  color: var(--theme-color-danger-800);
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-semibold);
}

.field textarea {
  resize: vertical;
  font-family: var(--theme-font-mono);
  line-height: 1.55;
}

.placeholder-list {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--theme-space-2);
}

.placeholder-list > span {
  margin-right: var(--theme-space-1);
  color: var(--color-text-muted);
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-bold);
}

.placeholder-list button {
  border-radius: var(--theme-radius-pill);
  background: var(--color-sidebar-active);
  padding: var(--theme-space-1) var(--theme-space-2);
  color: var(--color-primary);
  font-family: var(--theme-font-mono);
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-bold);
}

.template-history {
  border-top: 1px solid var(--color-border);
  padding-top: var(--theme-space-4);
}

.template-history__heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.template-history__heading h3 {
  font-size: var(--theme-font-size-label);
  font-weight: var(--theme-font-weight-bold);
}

.template-history__heading button {
  color: var(--color-primary);
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-bold);
  min-height: var(--theme-control-height-md);
  padding-inline: var(--theme-space-2);
}

.template-history__versions {
  display: grid;
  gap: var(--theme-space-2);
  margin-top: var(--theme-space-2-5);
}

.template-history__versions > button {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto auto;
  align-items: center;
  gap: var(--theme-space-3);
  border: 1px solid var(--color-border);
  border-radius: var(--theme-radius-control);
  padding: var(--theme-space-2) var(--theme-space-3);
  font-size: var(--theme-font-size-caption);
  text-align: left;
}

.template-history__versions small {
  margin-left: var(--theme-space-1-5);
  border-radius: var(--theme-radius-pill);
  background: var(--theme-color-success-surface);
  padding: var(--theme-space-1) var(--theme-space-2);
  color: var(--theme-color-success-700);
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-bold);
}

.template-preview {
  min-width: 0;
  min-height: 0;
  overflow-y: auto;
  overscroll-behavior: contain;
  border-left: 1px solid var(--color-border);
  background: var(--theme-color-surface-subtle);
  padding: var(--theme-space-4);
}

.template-preview__chrome {
  display: flex;
  align-items: center;
  justify-content: space-between;
  color: var(--theme-color-text-soft);
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-bold);
}

.template-preview__email {
  margin-top: var(--theme-space-3);
  overflow: hidden;
  border-radius: var(--theme-radius-control);
  background: white;
  box-shadow: var(--theme-shadow-card);
}

.template-preview__message-meta {
  display: grid;
  gap: var(--theme-space-1);
  padding: var(--theme-space-3) var(--theme-space-4);
  border-bottom: 1px solid var(--theme-color-border-default);
  background-color: var(--theme-color-surface-panel);
}

.template-preview__message-meta span {
  color: var(--theme-color-text-muted);
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-bold);
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.template-preview__message-meta strong {
  color: var(--theme-color-text-strong);
  font-size: var(--theme-font-size-caption);
  line-height: 1.4;
}

.template-preview__content {
  display: block;
  width: 100%;
  min-height: 24rem;
  border: 0;
}

.delivery-summary {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  border-bottom: 1px solid var(--color-border);
  flex: 0 0 auto;
}

.delivery-summary > div {
  display: grid;
  gap: var(--theme-space-1);
  padding: var(--theme-space-4) var(--theme-space-5);
  border-right: 1px solid var(--color-border);
  color: var(--color-text-muted);
  font-size: var(--theme-font-size-caption);
}

.delivery-summary > div:last-child {
  border-right: 0;
}

.delivery-summary__value {
  color: var(--color-text);
  font-size: var(--theme-font-size-section-title);
  font-weight: var(--theme-font-weight-bold);
}

.delivery-table-wrap {
  min-height: 0;
  flex: 1 1 auto;
  overflow: auto;
  overscroll-behavior: contain;
}

.delivery-panel {
  display: flex;
  max-height: calc(100dvh - 13.5rem);
  min-height: 0;
  flex-direction: column;
}

.delivery-table {
  width: 100%;
  border-collapse: collapse;
  font-size: var(--theme-font-size-caption);
}

.delivery-table th {
  position: sticky;
  z-index: 2;
  top: 0;
  background: var(--color-surface-muted, var(--theme-color-surface-subtle));
  padding: var(--theme-space-3) var(--theme-space-3);
  color: var(--color-text-muted);
  font-size: var(--theme-font-size-caption);
  font-weight: var(--theme-font-weight-bold);
  letter-spacing: 0.05em;
  text-align: left;
  text-transform: uppercase;
}

.delivery-table td {
  max-width: 18rem;
  padding: var(--theme-space-3);
  border-top: 1px solid var(--color-border);
  vertical-align: top;
}

.delivery-table td strong,
.delivery-table td span {
  display: block;
}

.delivery-table td > span:not(.status),
.delivery-table td small {
  margin-top: var(--theme-space-1);
  color: var(--color-text-muted);
  font-size: var(--theme-font-size-caption);
  line-height: 1.35;
}

.delivery-table__retry {
  display: grid;
  width: 2rem;
  height: 2rem;
  place-items: center;
  border-radius: var(--theme-radius-control);
  color: var(--color-primary);
}

.delivery-table__retry:hover {
  background: var(--color-sidebar-active);
}

.delivery-table__empty {
  padding: var(--theme-space-12) !important;
  color: var(--color-text-muted);
  text-align: center;
}

@media (max-width: 1280px) {
  .policy-row {
    grid-template-columns: minmax(15rem, 1.5fr) minmax(14rem, 1fr);
  }

  .policy-row__actions {
    justify-content: flex-start;
  }

  .template-workspace {
    grid-template-columns: minmax(12rem, 0.6fr) minmax(24rem, 1.4fr);
    height: auto;
    min-height: 0;
    max-height: none;
    overflow: visible;
  }

  .template-preview {
    grid-column: 1 / -1;
    overflow: visible;
    border-top: 1px solid var(--color-border);
    border-left: 0;
  }
}

@media (max-width: 800px) {
  .policy-groups,
  .delivery-panel {
    max-height: none;
    overflow: visible;
  }

  .template-editor__heading {
    flex-direction: column;
  }

  .policy-row {
    grid-template-columns: 1fr;
  }

  .template-workspace {
    grid-template-columns: 1fr;
    height: auto;
    min-height: 0;
    max-height: none;
  }

  .template-workspace__events {
    max-height: 14rem;
    overflow-y: auto;
    border-right: 0;
    border-bottom: 1px solid var(--color-border);
  }

  .template-editor {
    overflow: visible;
  }

  .delivery-summary {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .delivery-summary > div:nth-child(2) {
    border-right: 0;
  }

  .delivery-summary > div:nth-child(-n + 2) {
    border-bottom: 1px solid var(--color-border);
  }
}

</style>
