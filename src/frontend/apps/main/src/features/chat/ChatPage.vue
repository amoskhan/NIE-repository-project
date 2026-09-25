<script setup lang="ts">
import ChatComposer from "./ChatComposer.vue";
import ChatHeader from "./ChatHeader.vue";
import ChatMessageList from "./ChatMessageList.vue";
import { useChat } from "./useChat";
import { projectSyllabus } from "./projectSyllabus";

const prompts = [
  "What are the PE learning outcomes?",
  "How can I teach safe landing?",
  "Where can I find FMS guidance?",
];

const { messageCountLabel, messages, sendMessage } = useChat(projectSyllabus);
</script>

<template>
  <main class="page-shell">
    <section class="chat-card" aria-labelledby="chat-title">
      <ChatHeader />

      <div class="intro">
        <p id="chat-title" class="intro-title">Your 2024 PE Syllabus is ready</p>
        <p class="intro-copy">
          All {{ projectSyllabus.pageCount }} pages of the syllabus you provided are available.
          Ask a question below to find relevant passages and page references.
        </p>
        <a class="source-link" :href="projectSyllabus.url" target="_blank" rel="noopener noreferrer">
          View the 2024 PE Syllabus (PDF)
        </a>
      </div>

      <div class="prompt-section" data-tour="syllabus-prompts">
        <p class="prompt-label">Try a question</p>
        <div class="prompt-list">
          <button
            v-for="prompt in prompts"
            :key="prompt"
            class="prompt-button"
            type="button"
            @click="sendMessage(prompt)"
          >
            {{ prompt }}
          </button>
        </div>
      </div>

      <ChatMessageList :messages="messages" />
      <p class="message-count">{{ messageCountLabel }}</p>
      <ChatComposer @send="sendMessage" />
    </section>
  </main>
</template>

<style scoped>
.page-shell {
  box-sizing: border-box;
  display: grid;
  min-height: 100dvh;
  place-items: center;
  padding: 1.25rem;
  background: linear-gradient(135deg, #e9f2f6 0%, #f9f5ec 100%);
}

.chat-card {
  display: flex;
  width: min(100%, 52rem);
  min-height: min(46rem, calc(100dvh - 2.5rem));
  flex-direction: column;
  overflow: hidden;
  border: 1px solid rgb(18 59 93 / 12%);
  border-radius: 1rem;
  background: var(--surface);
  box-shadow: 0 1.25rem 3.75rem rgb(18 59 93 / 13%);
}

.intro {
  padding: 1.4rem 1.5rem 0.45rem;
}

.intro-title {
  margin: 0;
  color: var(--navy);
  font-size: 1.1rem;
  font-weight: 800;
}

.intro-copy {
  margin: 0.4rem 0 0;
  color: var(--muted);
  font-size: 0.9rem;
  line-height: 1.45;
}

.source-link {
  display: inline-block;
  margin-top: 0.65rem;
  color: var(--blue);
  font-size: 0.85rem;
  text-underline-offset: 0.2em;
}

.prompt-section {
  padding: 0.6rem 1.5rem 0.3rem;
}

.prompt-label {
  margin: 0 0 0.45rem;
  color: var(--muted);
  font-size: 0.75rem;
  font-weight: 750;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.prompt-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.45rem;
}

.prompt-button {
  padding: 0.45rem 0.65rem;
  border: 1px solid #b8cfe0;
  border-radius: 999px;
  background: #f7fbfd;
  color: #155988;
  cursor: pointer;
  font: inherit;
  font-size: 0.8rem;
  font-weight: 650;
}

.prompt-button:hover,
.prompt-button:focus-visible {
  border-color: var(--blue);
  background: #e8f4fb;
}

.prompt-button:focus-visible {
  outline: 3px solid rgb(24 102 163 / 22%);
  outline-offset: 2px;
}

.message-count {
  margin: 0;
  padding: 0 1.5rem 0.65rem;
  color: var(--muted);
  font-size: 0.75rem;
  text-align: right;
}
</style>
