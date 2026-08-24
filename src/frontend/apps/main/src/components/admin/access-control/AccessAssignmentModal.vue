<script setup lang="ts">
import { toTypedSchema } from "@vee-validate/zod";
import { NieButton, NieInput, NieModal } from "@nie/ui";
import { useForm } from "vee-validate";
import { computed, ref, watch } from "vue";
import { z } from "zod";
import type {
  ApplicationSummary,
  AssignAccessRequest,
  Role,
  StaffDetails,
} from "@/types";

const props = withDefaults(
  defineProps<{
    modelValue: boolean;
    roles: Role[];
    applications: ApplicationSummary[];
    resolvedStaff?: StaffDetails | null;
    saving?: boolean;
    lookupLoading?: boolean;
    lookupError?: string | null;
    canAssignGlobal?: boolean;
    canAssignApplication?: boolean;
  }>(),
  { canAssignGlobal: true, canAssignApplication: true },
);

const emit = defineEmits<{
  "update:modelValue": [value: boolean];
  lookup: [email: string];
  assign: [value: AssignAccessRequest];
}>();

const email = ref("");
const validationSummary = ref("");
const submitAttempted = ref(false);

const schema = toTypedSchema(
  z
    .object({
      userId: z.string().trim().min(1, "Resolve a staff member first"),
      scope: z.enum(["global", "application"]),
      roleIds: z.array(z.string().uuid()).min(1, "Select at least one role"),
      applicationIds: z.array(z.string().uuid()),
    })
    .superRefine((value, context) => {
      if (value.scope === "application" && value.applicationIds.length === 0) {
        context.addIssue({
          code: "custom",
          path: ["applicationIds"],
          message: "Select at least one application",
        });
      }
    }),
);

const { errors, resetForm, setFieldValue, validate, values } = useForm({
  validationSchema: schema,
  initialValues: {
    userId: "",
    scope: "global" as const,
    roleIds: [] as string[],
    applicationIds: [] as string[],
  },
});

const activeRoles = computed(() => props.roles.filter((role) => role.isActive));
const activeApplications = computed(() =>
  props.applications.filter((application) => application.isActive),
);
const scopeOptions = computed(() =>
  [
    {
      value: "global" as const,
      title: "All applications",
      description: "Roles apply globally across the platform.",
      allowed: props.canAssignGlobal,
    },
    {
      value: "application" as const,
      title: "Specific applications",
      description: "Roles apply only to selected applications.",
      allowed: props.canAssignApplication,
    },
  ].filter((option) => option.allowed),
);

watch(
  () => [props.modelValue, props.resolvedStaff] as const,
  ([isOpen, staff]) => {
    if (!isOpen) return;
    email.value = staff?.email ?? "";
    submitAttempted.value = false;
    validationSummary.value = "";
    resetForm({
      values: {
        userId: staff?.userId ?? "",
        scope: scopeOptions.value[0]?.value ?? "global",
        roleIds: [],
        applicationIds: [],
      },
    });
  },
  { immediate: true },
);

function toggleRole(roleId: string, checked: boolean): void {
  const selected = new Set(values.roleIds);
  if (checked) {
    selected.add(roleId);
  } else {
    selected.delete(roleId);
  }
  setFieldValue("roleIds", [...selected]);
}

function toggleApplication(applicationId: string, checked: boolean): void {
  const selected = new Set(values.applicationIds);
  if (checked) {
    selected.add(applicationId);
  } else {
    selected.delete(applicationId);
  }
  setFieldValue("applicationIds", [...selected]);
}

function setScope(scope: "global" | "application"): void {
  if (!scopeOptions.value.some((option) => option.value === scope)) return;
  setFieldValue("scope", scope);
  if (scope === "global") {
    setFieldValue("applicationIds", []);
  }
}

function lookup(): void {
  const normalizedEmail = email.value.trim();
  if (/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(normalizedEmail)) {
    emit("lookup", normalizedEmail);
  }
}

async function submit(): Promise<void> {
  submitAttempted.value = true;
  if (!scopeOptions.value.some((option) => option.value === values.scope)) {
    validationSummary.value =
      "You are not permitted to assign this access scope.";
    return;
  }
  const result = await validate();
  if (!result.valid) {
    validationSummary.value = Object.values(result.errors).join(" ");
    return;
  }
  validationSummary.value = "";

  emit("assign", {
    scope: values.scope,
    userId: values.userId,
    roleIds: [...values.roleIds],
    applicationIds:
      values.scope === "application" ? [...values.applicationIds] : [],
  });
}

defineExpose({ submit });
</script>

<template>
  <NieModal
    :model-value="modelValue"
    title="Assign access"
    size="xl"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <form
      id="access-assignment-form"
      class="space-y-6"
      @submit.prevent="submit"
    >
      <fieldset class="space-y-2">
        <legend
          class="text-sm font-bold text-secondary-700 dark:text-secondary-300"
        >
          Access scope
        </legend>
        <div class="grid gap-3 sm:grid-cols-2">
          <label
            v-for="option in scopeOptions"
            :key="option.value"
            class="flex cursor-pointer items-start gap-3 rounded-2xl border p-4 transition"
            :class="
              values.scope === option.value
                ? 'border-primary-500 bg-primary-50 dark:bg-primary-950/30'
                : 'border-secondary-200 hover:bg-secondary-50 dark:border-secondary-700 dark:hover:bg-secondary-800'
            "
          >
            <input
              type="radio"
              name="assignment-scope"
              :value="option.value"
              :checked="values.scope === option.value"
              class="mt-1 border-secondary-300 text-primary-600 focus:ring-primary-500"
              @change="setScope(option.value)"
            />
            <span>
              <span
                class="block font-semibold text-secondary-900 dark:text-white"
              >
                {{ option.title }}
              </span>
              <span class="mt-1 block text-xs text-secondary-500">
                {{ option.description }}
              </span>
            </span>
          </label>
        </div>
      </fieldset>

      <div v-if="!resolvedStaff" class="space-y-2">
        <label
          class="text-sm font-bold text-secondary-700 dark:text-secondary-300"
        >
          Staff email
        </label>
        <div class="flex gap-2">
          <NieInput
            v-model="email"
            class="flex-1"
            type="email"
            placeholder="name@nie.edu.sg"
            @keyup.enter.prevent="lookup"
          />
          <NieButton
            variant="outline"
            :loading="lookupLoading"
            :disabled="!email.trim()"
            @click="lookup"
          >
            Lookup
          </NieButton>
        </div>
        <p v-if="lookupError" class="text-sm text-danger-600" role="alert">
          {{ lookupError }}
        </p>
      </div>

      <div
        v-if="resolvedStaff"
        class="rounded-2xl border border-success-200 bg-success-50 p-4 dark:border-success-900 dark:bg-success-950/30"
      >
        <p class="font-bold text-secondary-900 dark:text-white">
          {{ resolvedStaff.name }}
        </p>
        <p class="mt-1 text-sm text-secondary-600 dark:text-secondary-300">
          {{ resolvedStaff.userId }} · {{ resolvedStaff.email }}
        </p>
        <p class="mt-1 text-xs text-secondary-500">
          {{ resolvedStaff.departmentDescription || resolvedStaff.department }}
          <template v-if="resolvedStaff.designation">
            · {{ resolvedStaff.designation }}
          </template>
        </p>
      </div>
      <p
        v-if="submitAttempted && errors.userId"
        class="text-sm text-danger-600"
        role="alert"
      >
        {{ errors.userId }}
      </p>
      <p
        v-if="validationSummary"
        data-testid="assignment-validation-summary"
        class="text-sm text-danger-600"
        role="alert"
      >
        {{ validationSummary }}
      </p>

      <fieldset class="space-y-3">
        <legend
          class="text-sm font-bold text-secondary-700 dark:text-secondary-300"
        >
          Roles
        </legend>
        <div class="grid gap-2 sm:grid-cols-2">
          <label
            v-for="role in activeRoles"
            :key="role.id"
            class="flex cursor-pointer items-start gap-3 rounded-xl border border-secondary-200 p-3 hover:bg-secondary-50 dark:border-secondary-700 dark:hover:bg-secondary-800"
          >
            <input
              data-testid="assignment-role"
              type="checkbox"
              class="mt-0.5 size-5 rounded border-secondary-300 text-primary-600 focus:ring-primary-500"
              :checked="values.roleIds.includes(role.id)"
              @change="
                toggleRole(role.id, ($event.target as HTMLInputElement).checked)
              "
            />
            <span>
              <span class="block text-sm font-semibold">{{ role.name }}</span>
              <span class="block text-xs text-secondary-500">{{
                role.code
              }}</span>
            </span>
          </label>
        </div>
        <p
          v-if="submitAttempted && errors.roleIds"
          class="text-sm text-danger-600"
          role="alert"
        >
          {{ errors.roleIds }}
        </p>
      </fieldset>

      <fieldset v-if="values.scope === 'application'" class="space-y-3">
        <legend
          class="text-sm font-bold text-secondary-700 dark:text-secondary-300"
        >
          Applications
        </legend>
        <div class="grid gap-2 sm:grid-cols-2">
          <label
            v-for="application in activeApplications"
            :key="application.id"
            class="flex cursor-pointer items-start gap-3 rounded-xl border border-secondary-200 p-3 hover:bg-secondary-50 dark:border-secondary-700 dark:hover:bg-secondary-800"
          >
            <input
              data-testid="assignment-application"
              type="checkbox"
              class="mt-0.5 size-5 rounded border-secondary-300 text-primary-600 focus:ring-primary-500"
              :checked="values.applicationIds.includes(application.id)"
              @change="
                toggleApplication(
                  application.id,
                  ($event.target as HTMLInputElement).checked,
                )
              "
            />
            <span>
              <span class="block text-sm font-semibold">{{
                application.name
              }}</span>
              <span class="block text-xs text-secondary-500">
                {{ application.projectKey }}
              </span>
            </span>
          </label>
        </div>
        <p
          v-if="submitAttempted && errors.applicationIds"
          class="text-sm text-danger-600"
          role="alert"
        >
          {{ errors.applicationIds }}
        </p>
      </fieldset>
    </form>

    <template #footer>
      <div class="flex justify-end gap-3">
        <NieButton variant="outline" @click="emit('update:modelValue', false)">
          Cancel
        </NieButton>
        <button
          type="submit"
          form="access-assignment-form"
          data-testid="submit-assignment"
          class="inline-flex min-h-11 items-center justify-center rounded-xl bg-primary-600 px-4 py-2.5 text-sm font-medium text-on-brand transition hover:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-50"
          :disabled="saving"
        >
          {{ saving ? "Assigning..." : "Assign access" }}
        </button>
      </div>
    </template>
  </NieModal>
</template>
