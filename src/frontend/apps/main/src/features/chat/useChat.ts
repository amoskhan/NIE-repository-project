import { computed, ref } from "vue";
import type { ChatMessage } from "./types";

const welcomeMessage: ChatMessage = {
  id: 1,
  author: "assistant",
  text:
    "Hello! I am the PE Syllabus Assistant. Ask a question about the 2024 MOE PE Syllabus and I will help you find the right guidance.",
};

function createUnavailableSourceResponse(): string {
  return "This prototype is not connected to your school’s approved 2024 PE Syllabus source yet, so I cannot provide a verified syllabus answer or citation. Connect the approved source library to enable grounded answers with section references.";
}

export function useChat() {
  const messages = ref<ChatMessage[]>([welcomeMessage]);
  const nextMessageId = ref(2);

  const messageCountLabel = computed(() =>
    `${messages.value.length} ${messages.value.length === 1 ? "message" : "messages"}`,
  );

  function sendMessage(question: string): void {
    const trimmedQuestion = question.trim();

    if (!trimmedQuestion) {
      return;
    }

    messages.value.push({
      id: nextMessageId.value++,
      author: "educator",
      text: trimmedQuestion,
    });
    messages.value.push({
      id: nextMessageId.value++,
      author: "assistant",
      text: createUnavailableSourceResponse(),
    });
  }

  return { messageCountLabel, messages, sendMessage };
}
