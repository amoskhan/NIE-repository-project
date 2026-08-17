# AI Chatbot - File Map

## Owned files

| Path                                                           | Layer            | Purpose                                                                   |
| -------------------------------------------------------------- | ---------------- | ------------------------------------------------------------------------- |
| `src/backend/Libraries/Domain/Models/ChatConversation.cs`      | Domain           | Conversation header entity.                                               |
| `src/backend/Libraries/Domain/Models/ChatMessage.cs`           | Domain           | Individual messages within a conversation.                                |
| `src/backend/Libraries/Domain/Models/ChatEmbedding.cs`         | Domain           | pgvector embedding rows used for retrieval.                               |
| `src/backend/Libraries/Services/Services/Chat/IChatService.cs` | Contract         | Chat conversation, message, streaming, and search service boundary.       |
| `src/backend/Libraries/Services/Services/Chat/ChatService.cs`  | Service          | Chat orchestration and placeholder LLM/vector behavior.                   |
| `src/backend/API/Controllers/ChatController.cs`                | API              | Conversation CRUD, SSE response streaming, and semantic search endpoints. |
| `src/frontend/main/src/pages/chat/ChatView.vue`                | Route            | Chat workspace page.                                                      |
| `src/frontend/main/src/components/chat/ChatSidebar.vue`        | Component        | Conversation list and source switching.                                   |
| `src/frontend/main/src/components/chat/ChatMessageBubble.vue`  | Component        | Message rendering.                                                        |
| `src/frontend/main/src/components/chat/ChatInputBox.vue`       | Component        | Prompt entry and send/stop controls.                                      |
| `src/frontend/main/src/services/chatService.ts`                | Frontend service | Typed API client for conversations, messages, streaming, and search.      |

## Touched files

| Path                                               | Why                                                                   |
| -------------------------------------------------- | --------------------------------------------------------------------- |
| `src/backend/API/Program.cs`                       | Registers optional chat service when the feature files are present.   |
| `src/backend/Libraries/Data/Data/MainDbContext.cs` | Adds chat DbSets and model configuration when the feature is applied. |
| `src/frontend/main/src/router/index.ts`            | Adds optional chat routes.                                            |
| `src/frontend/main/src/constants/permissions.ts`   | Defines `api.chat.use`.                                               |

## Migrations

Chat entities require an EF migration when enabled in a derived project. If pgvector is enabled, the target PostgreSQL database must also have the `vector` extension installed — the shipped `postgres:18-alpine` image does not provide it, so switch the Compose image to `pgvector/pgvector:pg18` first (see `README.md` § Prerequisite).
