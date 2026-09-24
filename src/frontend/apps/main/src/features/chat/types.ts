export type MessageAuthor = "assistant" | "educator";

export interface ChatMessage {
  id: number;
  author: MessageAuthor;
  text: string;
}
