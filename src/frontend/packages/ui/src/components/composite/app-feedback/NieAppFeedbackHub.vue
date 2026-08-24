<script setup lang="ts">
import { computed } from "vue";
import { useConfirm } from "../../../composables/useConfirm";
import { useToast } from "../../../composables/useToast";
import { NieConfirmDialog } from "../confirm";
import { NieToastContainer } from "../toast";

const toast = useToast();
const confirm = useConfirm();

const toastItems = computed(() => toast.toasts.value);
const confirmOptions = computed(() => confirm.state.value.options);
</script>

<template>
  <NieToastContainer :toasts="toastItems" @dismiss="toast.remove" />
  <NieConfirmDialog
    :options="confirmOptions"
    @confirm="confirm.handleConfirm"
    @cancel="confirm.handleCancel"
  />
</template>
