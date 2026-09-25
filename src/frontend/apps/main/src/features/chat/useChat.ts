import { computed, ref } from "vue";
import type { Ref } from "vue";
import type { ChatMessage, SyllabusPage, SyllabusSource } from "./types";

const welcomeMessage: ChatMessage = {
  id: 1,
  author: "assistant",
  text:
    "Hello! I am the PE Syllabus Assistant. Ask a question about the 2024 MOE PE Syllabus and I will help you find the right guidance.",
};

const ignoredSearchTerms = new Set([
  "about",
  "after",
  "again",
  "also",
  "are",
  "can",
  "does",
  "find",
  "for",
  "from",
  "how",
  "into",
  "its",
  "learn",
  "should",
  "the",
  "their",
  "these",
  "this",
  "what",
  "when",
  "where",
  "which",
  "with",
  "would",
]);

function createUnavailableSourceResponse(): string {
  return "This prototype is not connected to your school’s approved 2024 PE Syllabus source yet, so I cannot provide a verified syllabus answer or citation. Connect the approved source library to enable grounded answers with section references.";
}

function searchTerms(question: string): string[] {
  return Array.from(
    new Set(
      question
        .toLowerCase()
        .match(/[a-z0-9][a-z0-9-]*/g)
        ?.filter((term) => term.length > 2 && !ignoredSearchTerms.has(term)) ?? [],
    ),
  );
}

function countOccurrences(text: string, term: string): number {
  return text.split(term).length - 1;
}

function excerptFromPage(page: SyllabusPage, terms: string[]): string {
  const normalizedText = page.text.toLowerCase();
  const matchIndex = terms
    .map((term) => normalizedText.indexOf(term))
    .find((index) => index >= 0);
  const start = Math.max(0, (matchIndex ?? 0) - 180);
  const end = Math.min(page.text.length, (matchIndex ?? 0) + 360);
  const excerpt = page.text.slice(start, end).trim();

  return `${start > 0 ? "…" : ""}${excerpt}${end < page.text.length ? "…" : ""}`;
}

function findMatches(question: string, source: SyllabusSource): SyllabusPage[] {
  const terms = searchTerms(question);

  if (terms.length === 0) {
    return [];
  }

  return source.pages
    .map((page) => ({
      page,
      score: terms.reduce(
        (total, term) => total + Math.min(countOccurrences(page.text.toLowerCase(), term), 3),
        0,
      ),
    }))
    .filter(({ score }) => score > 0)
    .sort((left, right) => right.score - left.score)
    .slice(0, 2)
    .map(({ page }) => page);
}

function createSourceResponse(question: string, source: SyllabusSource): Pick<ChatMessage, "text" | "citation"> {
  const terms = searchTerms(question);
  const matches = findMatches(question, source);

  if (matches.length === 0) {
    return {
      text: "I could not find a clear matching passage in the uploaded syllabus. Try a more specific curriculum term, learning outcome, or movement skill.",
    };
  }

  return {
    text: `I found these passages in the uploaded syllabus:\n\n${matches
      .map((page) => `“${excerptFromPage(page, terms)}”`)
      .join("\n\n")}`,
    citation: `${source.fileName} · ${matches.map((page) => `page ${page.pageNumber}`).join(" and ")}`,
  };
}

export function useChat(source: Readonly<Ref<SyllabusSource | null>>) {
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
    const response = source.value
      ? createSourceResponse(trimmedQuestion, source.value)
      : { text: createUnavailableSourceResponse() };

    messages.value.push({
      id: nextMessageId.value++,
      author: "assistant",
      ...response,
    });
  }

  return { messageCountLabel, messages, sendMessage };
}
