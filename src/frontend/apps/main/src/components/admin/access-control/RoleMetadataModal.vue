<script setup lang="ts">
import { toTypedSchema } from "@vee-validate/zod";
import { NieButton, NieInput, NieModal, NieSwitch, NieTextarea } from "@nie/ui";
import { useForm } from "vee-validate";
import { watch } from "vue";
import { z } from "zod";
import type { Role } from "@/types";

const props = defineProps<{
  modelValue: boolean;
  role: Role | null;
  saving?: boolean;
}>();

const emit = defineEmits<{
  "update:modelValue": [value: boolean];
  save: [
    value: {
      roleId?: string;
      code: string;
      name: string;
      description: string | null;
      isActive: boolean;
    },
  ];
}>();

const schema = toTypedSchema(
  z.object({
    code: z
      .string()
      .trim()
      .min(1, "Role code is required")
      .max(50)
      .regex(
        /^[A-Za-z][A-Za-z0-9._-]*$/,
        "Use letters, numbers, dots, hyphens, or underscores",
      ),
    name: z.string().trim().min(1, "Role name is required").max(100),
    description: z.string().trim().max(500).optional(),
    isActive: z.boolean(),
  }),
);

const { defineField, errors, handleSubmit, resetForm } = useForm({
  validationSchema: schema,
});
const [code] = defineField("code");
const [name] = defineField("name");
const [description] = defineField("description");
const [isActive] = defineField("isActive");

watch(
  () => [props.modelValue, props.role] as const,
  ([isOpen, role]) => {
    if (!isOpen) return;
    resetForm({
      values: {
        code: role?.code ?? "",
        name: role?.name ?? "",
        description: role?.description ?? "",
        isActive: role?.isActive ?? true,
      },
    });
  },
  { immediate: true },
);

const submit = handleSubmit((values) => {
  emit("save", {
    roleId: props.role?.id,
    code: values.code.trim(),
    name: values.name.trim(),
    description: values.description?.trim() || null,
    isActive: values.isActive,
  });
});
</script>

<template>
  <NieModal
    :model-value="modelValue"
    :title="role ? 'Edit role' : 'New role'"
    size="lg"
    @update:model-value="emit('update:modelValue', $event)"
  >
    <form class="space-y-5" @submit.prevent="submit">
      <NieInput
        v-model="code"
        label="Role code"
        :readonly="Boolean(role)"
        :error="errors.code"
      />
      <NieInput v-model="name" label="Role name" :error="errors.name" />
      <NieTextarea
        id="role-description"
        v-model="description"
        label="Description"
        :rows="4"
        :error="errors.description"
      />
      <NieSwitch v-model="isActive" label="Active role" />
    </form>

    <template #footer>
      <div class="flex justify-end gap-3">
        <NieButton variant="outline" @click="emit('update:modelValue', false)">
          Cancel
        </NieButton>
        <NieButton :loading="saving" @click="submit">Save role</NieButton>
      </div>
    </template>
  </NieModal>
</template>
