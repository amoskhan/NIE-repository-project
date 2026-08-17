<script setup lang="ts">
import { AppFeedbackHub } from "@apptemplate/ui";
import { RouterView } from "vue-router";
</script>

<template>
  <RouterView v-slot="{ Component }">
    <transition name="fade" mode="out-in">
      <component :is="Component" />
    </transition>
  </RouterView>
  <AppFeedbackHub />
</template>
