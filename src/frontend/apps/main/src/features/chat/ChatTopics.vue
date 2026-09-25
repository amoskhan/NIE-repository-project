<script setup lang="ts">
import { computed, shallowRef } from "vue";
import type { SyllabusTopic } from "./types";

const props = defineProps<{ topics: SyllabusTopic[]; disabled: boolean }>();
const emit = defineEmits<{ ask: [question: string] }>();
const search = shallowRef("");
const visibleTopics = computed(() => props.topics.filter(topic => topic.question.toLowerCase().includes(search.value.toLowerCase().trim())));
</script>

<template>
  <details v-if="topics.length" class="topics">
    <summary>Browse {{ topics.length }} available questions</summary>
    <label class="search-label" for="topic-search">Find a question</label>
    <input id="topic-search" v-model="search" class="search" type="search" placeholder="e.g. swimming or movement" />
    <ul class="topic-list">
      <li v-for="topic in visibleTopics" :key="topic.id">
        <button class="topic-button" type="button" :disabled="disabled" @click="emit('ask', topic.question)">{{ topic.question }}</button>
      </li>
    </ul>
    <p v-if="!visibleTopics.length">No prepared questions match. Try a different topic or open the syllabus PDF.</p>
  </details>
</template>

<style scoped>
.topics { margin: 0.7rem 1.5rem; color: var(--ink); font-size: 0.85rem; }
.topics summary { cursor: pointer; color: var(--blue); font-weight: 700; padding: 0.4rem 0; }
.search-label { display: block; margin: 0.8rem 0 0.3rem; }
.search { box-sizing: border-box; width: 100%; padding: 0.65rem; border: 1px solid var(--border); border-radius: 0.4rem; font: inherit; }
.topic-list { padding: 0; list-style: none; max-height: 16rem; overflow-y: auto; }
.topic-button { width: 100%; padding: 0.65rem 0.3rem; text-align: left; border: 0; border-bottom: 1px solid var(--border); background: transparent; color: var(--blue); cursor: pointer; font: inherit; }
.topic-button:disabled { opacity: 0.5; cursor: wait; }
</style>
