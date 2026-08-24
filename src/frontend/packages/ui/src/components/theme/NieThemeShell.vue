<script setup lang="ts">
import { computed } from "vue";
import type { LayoutVariant } from "../../theme";
import { cn } from "../../lib/utils";

interface Props {
  variant?: LayoutVariant;
  contentClass?: string;
  class?: string;
}

const props = withDefaults(defineProps<Props>(), {
  variant: "bare-content",
  contentClass: "",
});

const shellClasses = computed(() =>
  cn(
    "theme-shell w-full",
    props.variant === "split-auth"
      ? "grid lg:grid-cols-[minmax(0,1.1fr)_minmax(24rem,0.9fr)]"
      : props.variant === "sidebar-admin" || props.variant === "portal-shell"
        ? "flex"
        : "flex flex-col",
    props.class,
  ),
);

const contentClasses = computed(() =>
  cn(
    "theme-shell__content min-w-0 flex-1",
    props.variant === "split-auth"
      ? "flex min-h-full items-stretch"
      : "flex min-h-full flex-1 flex-col",
    props.contentClass,
  ),
);
</script>

<template>
  <div :class="shellClasses" :data-layout-variant="variant">
    <aside
      v-if="$slots.sidebar && (variant === 'sidebar-admin' || variant === 'portal-shell')"
      class="theme-shell__sidebar"
    >
      <slot name="sidebar"></slot>
    </aside>

    <section
      v-if="$slots.hero && variant === 'split-auth'"
      class="theme-shell__hero relative hidden overflow-hidden lg:flex"
    >
      <slot name="hero"></slot>
    </section>

    <div :class="contentClasses">
      <header v-if="$slots.topbar" class="theme-shell__topbar">
        <slot name="topbar"></slot>
      </header>

      <main class="theme-shell__main flex-1">
        <slot></slot>
      </main>
    </div>
  </div>
</template>
