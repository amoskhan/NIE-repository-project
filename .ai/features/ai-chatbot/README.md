# AI Chatbot (pgvector + Streaming)

> **Status:** `optional`

## Overview

AI-powered chat interface with SSE streaming, conversation management, and pgvector-based semantic search. The only LLM backend wired up in this template is **Azure OpenAI** (`src/backend/Libraries/AI/`); `ChatService` otherwise falls back to a placeholder responder so the feature runs without credentials. Any other provider is something you add yourself.

## Prerequisite — swap the PostgreSQL image for a pgvector build

The template ships `postgres:18-alpine`, which does **not** include pgvector. Running `CREATE EXTENSION vector` against the stock image fails with `ERROR: extension "vector" is not available`. Before anything else in this feature:

1. Edit `.devcontainer/docker-compose.yml` (and `build/docker-compose.yml` if you deploy with it) and change the database image:

   ```yaml
   postgres:
     image: pgvector/pgvector:pg18 # was postgres:18-alpine
   ```

2. Recreate the container so the new image is actually pulled:

   ```bash
   docker compose -f .devcontainer/docker-compose.yml up -d --force-recreate postgres
   ```

3. Then create the extension (see [pgvector Setup](#pgvector-setup) below).

`pgvector/pgvector:pg18` is the same PostgreSQL 18 server with the extension preinstalled, so no data model or connection-string change is needed. If CI needs the extension too, change the `image:` under the `postgres` service in `.github/workflows/ci.yml` as well.

## Key Files

- `src/backend/Libraries/Domain/Models/ChatConversation.cs` — conversation header entity
- `src/backend/Libraries/Domain/Models/ChatMessage.cs` — individual messages in a conversation
- `src/backend/Libraries/Domain/Models/ChatEmbedding.cs` — pgvector embedding rows used for retrieval
- `src/backend/Libraries/Services/Services/Chat/IChatService.cs` — conversations, messages, streaming
- `src/backend/Libraries/Services/Services/Chat/ChatService.cs` — implementation with placeholder LLM
- `src/backend/Libraries/AI/Services/Chat/AzureOpenAIService.cs` — the Azure OpenAI client
- `src/backend/API/Controllers/ChatController.cs` — REST + SSE streaming endpoint
- `src/frontend/main/src/pages/chat/ChatView.vue` — full chat page with sidebar
- `src/frontend/main/src/components/chat/ChatSidebar.vue` — conversation list
- `src/frontend/main/src/components/chat/ChatMessageBubble.vue` — message bubble
- `src/frontend/main/src/components/chat/ChatInputBox.vue` — input with send/stop
- `build/appsettings.api.json` — AI config section

## pgvector Setup

Only after the image swap above:

```sql
CREATE EXTENSION IF NOT EXISTS vector;
```
