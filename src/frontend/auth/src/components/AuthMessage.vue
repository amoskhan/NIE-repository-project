<script setup lang="ts">
// PROJECT-OWNED — safe to edit.
//
// One banner style for every auth screen, built on the shared AppAlert primitive from
// @apptemplate/ui so it inherits the design system's colours and dark-mode handling.
// Renders nothing when `text` is empty, so pages can bind it unconditionally.
//
// Spacing is an explicit `spaced` prop rather than a class passed from the parent:
// AppAlert declares `class` as a real prop, so relying on attribute fallthrough here
// would be ambiguous. Being explicit costs one prop and removes the guesswork.

import { computed } from "vue";
import { AppAlert } from "@apptemplate/ui";

type Tone = "error" | "success" | "info";

interface Props {
  /** Message to show. Empty string / undefined hides the banner entirely. */
  text?: string;
  tone?: Tone;
  /** Add bottom margin. Use outside a flex-gap container. */
  spaced?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  tone: "error",
  spaced: false,
});

const variant = computed(() =>
  props.tone === "error"
    ? ("danger" as const)
    : props.tone === "success"
      ? ("success" as const)
      : ("info" as const),
);

// AppAlert types its `class` prop as a plain string, so build one rather than an array.
const alertClass = computed(() =>
  [
    // Tone is in the class name so tests (and CSS) can target `[class*="error"]`.
    `auth-alert auth-alert--${props.tone}`,
    props.tone === "error" ? "auth-shake" : "",
    props.spaced ? "mb-6" : "",
  ]
    .filter(Boolean)
    .join(" "),
);
</script>

<template>
  <transition name="auth-fade">
    <AppAlert
      v-if="text"
      :variant="variant"
      :class="alertClass"
      :data-testid="`auth-message-${tone}`"
    >
      {{ text }}
    </AppAlert>
  </transition>
</template>
