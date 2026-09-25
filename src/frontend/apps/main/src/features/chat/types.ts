export type MessageAuthor = "assistant" | "educator";

export interface ChatMessage {
  id: number;
  author: MessageAuthor;
  text: string;
  citation?: string;
  references?: { pdfPage: number; printedPage: string | null; evidence: string }[];
  suggestions?: string[];
}

export interface SyllabusTopic { id: string; question: string }

export interface SyllabusPage {
  pageNumber: number;
  text: string;
}

export interface SyllabusSource {
  origin: "project";
  fileName: string;
  pageCount: number;
  pages: SyllabusPage[];
}
