import { computed, onMounted, onScopeDispose, ref, shallowRef } from "vue";
import type { ChatMessage, SyllabusSource } from "./types";

const apiBase = new URL("~ignite/services/syllabus-chat-api/", new URL(import.meta.env.BASE_URL, window.location.origin));

export function useChat(source: SyllabusSource) {
  const messages = ref<ChatMessage[]>([{
    id: 1, author: "assistant",
    text: "Your 2024 PE Syllabus is ready. Ask a specific question, such as ‘When do students learn kicking?’, for a short answer with syllabus references.",
  }]);
  let nextId = 2;
  const isAnswering = shallowRef(false);
  const error = shallowRef("");
  const failedQuestion = shallowRef("");
  const connection = shallowRef<"checking" | "ready" | "not-configured" | "unavailable">("checking");
  const controller = new AbortController();
  onScopeDispose(() => controller.abort());

  const messageCountLabel = computed(() => `${messages.value.length} messages`);

  async function checkConnection(): Promise<void> {
    try {
      const response = await fetch(new URL("health", apiBase), { signal: AbortSignal.any([controller.signal, AbortSignal.timeout(5000)]) });
      if (!response.ok) throw new Error();
      const health = await response.json();
      connection.value = health.llmConfigured ? "ready" : "not-configured";
    } catch {
      connection.value = "unavailable";
    }
  }
  onMounted(checkConnection);

  async function requestAnswer(question: string, append: boolean): Promise<void> {
    const text = question.trim();
    if (!text || text.length > 1000 || isAnswering.value) return;
    const priorMessages = append ? messages.value : messages.value.slice(0, -1);
    const history = priorMessages.filter(message => message.id !== 1).slice(-4)
      .map(message => ({ role: message.author === "educator" ? "user" : "assistant", content: message.text.slice(0, 1500) }));
    if (append) messages.value.push({ id: nextId++, author: "educator", text });
    error.value = "";
    failedQuestion.value = "";
    isAnswering.value = true;
    try {
      const response = await fetch(new URL("chat", apiBase), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ question: text, history }),
        signal: AbortSignal.any([controller.signal, AbortSignal.timeout(55000)]),
      });
      const result = await response.json();
      if (!response.ok) {
        if (result.code === "llm_not_configured") {
          connection.value = "not-configured";
          throw new Error("The LLM connection needs to be configured before I can generate an answer from your syllabus.");
        }
        throw new Error(response.status === 429 ? "The assistant is busy. Please try again shortly." : "The answer service could not produce an answer with valid syllabus references. Please try again.");
      }
      if (typeof result.answer !== "string" || !Array.isArray(result.citations)) throw new Error("The answer service returned an incomplete answer. Please retry.");
      connection.value = "ready";
      messages.value.push({
        id: nextId++, author: "assistant", text: result.answer,
        citation: result.citations.length ? `${source.fileName} · ${result.citations.map((citation: {pdfPage: number; printedPage: string | null}) =>
          `PDF page ${citation.pdfPage}${citation.printedPage ? ` (printed page ${citation.printedPage})` : ""}`).join("; ")}` : undefined,
        references: result.citations,
      });
    } catch (reason) {
      if (controller.signal.aborted) return;
      failedQuestion.value = text;
      error.value = reason instanceof Error && reason.name === "TimeoutError"
        ? "The answer took too long. Please try again."
        : reason instanceof Error && reason.name === "Error" ? reason.message : "Unable to reach the answer service. Please try again.";
    } finally {
      isAnswering.value = false;
    }
  }

  return {
    messages, messageCountLabel, isAnswering, error, failedQuestion, connection,
    sendMessage: (question: string) => requestAnswer(question, true),
    retry: () => requestAnswer(failedQuestion.value, false),
  };
}
