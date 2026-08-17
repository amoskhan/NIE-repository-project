<script setup lang="ts">
interface Props {
  eyebrow?: string;
  title: string;
  description: string;
}

withDefaults(defineProps<Props>(), {
  eyebrow: "",
});
</script>

<template>
  <section
    class="relative flex h-full flex-col justify-between overflow-hidden bg-[linear-gradient(135deg,var(--theme-color-neutral-900)_0%,var(--theme-color-brand-950)_40%,var(--theme-color-brand-800)_72%,var(--theme-color-neutral-800)_100%)] p-10 text-white"
  >
    <div class="pointer-events-none absolute inset-0">
      <div
        class="absolute right-[-4rem] top-[-4rem] h-64 w-64 rounded-full bg-primary-400/20 blur-3xl"
      ></div>
      <div
        class="absolute bottom-[-4rem] left-[-3rem] h-56 w-56 rounded-full bg-sky-400/20 blur-3xl"
      ></div>
      <div
        class="absolute inset-x-0 bottom-0 h-[38%] bg-[linear-gradient(to_top,rgba(255,255,255,0.06),transparent)]"
      ></div>
    </div>

    <div class="relative z-10 space-y-5">
      <p
        v-if="eyebrow"
        class="text-xs font-semibold uppercase tracking-[0.24em] text-white/65"
      >
        {{ eyebrow }}
      </p>
      <div class="space-y-4">
        <h2 class="max-w-xl text-4xl font-black tracking-tight lg:text-5xl">
          {{ title }}
        </h2>
        <p class="max-w-xl text-base leading-7 text-slate-300/90">
          {{ description }}
        </p>
      </div>
    </div>

    <div class="relative z-10 grid gap-4 sm:grid-cols-2">
      <slot></slot>
    </div>
  </section>
</template>
