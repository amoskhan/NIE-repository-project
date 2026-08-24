# AI Chatbot

Canonical NIE rules for the AI Chatbot feature.

Rules version: 2026.08.07.1
Feature key: ai-chatbot  
Adoption: **opt-in**

## Adoption and navigation

- Menu or entry point: required at **Primary > AI Assistant**.
- Visibility: Only when adopted and the user has the chat screen/API access functions.
- If the feature is not adopted, report not-applicable with the product or architecture reason; do not silently omit it.

## Required libraries and minimums

| Library | Package/runtime | Minimum | Ecosystem |
| --- | --- | --- | --- |
| .NET / ASP.NET Core | net | 10.0.0 | runtime |
| Entity Framework Core | Microsoft.EntityFrameworkCore | 10.0.5 | nuget |
| Npgsql EF Core Provider | Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 | nuget |
| PostgreSQL | postgres | 17.0.0 | service |
| Azure.AI.OpenAI | Azure.AI.OpenAI | 2.1.0 | nuget |
| Microsoft.Agents.AI | Microsoft.Agents.AI | 1.3.0 | nuget |
| Microsoft.Extensions.AI | Microsoft.Extensions.AI | 10.5.0 | nuget |
| Pgvector | Pgvector | 0.3.2 | nuget |
| Pgvector.EntityFrameworkCore | Pgvector.EntityFrameworkCore | 0.3.0 | nuget |
| Vue | vue | 3.5.30 | npm |
| Axios | axios | 1.18.0 | npm |

Versions are floors, not forced pins for derived applications. Stable upgrades are allowed when compatibility, build, tests, security, and deployment evidence pass. The template itself keeps exact tested pins and lockfiles.

## Rules

| Rule | Severity | Area | Requirement | Required evidence |
| --- | --- | --- | --- | --- |
| NIE-CHAT-001 | error | backend | Put LLM, embedding, and retrieval providers behind application-owned interfaces and DI; controllers must not call provider SDKs directly. | review |
| NIE-CHAT-002 | error | security | Guard every chat endpoint and route, enforce conversation ownership, rate/usage limits, input limits, and retrieval authorization. | tests |
| NIE-CHAT-003 | error | data | Store conversations/messages with PostgreSQL and vectors with pgvector; define retention and deletion behavior. | migration-and-tests |
| NIE-CHAT-004 | error | frontend | Use SSE cancellation/reconnect/error states and keep provider/model names out of the UI contract unless intentionally exposed. | browser-tests |
| NIE-CHAT-005 | error | privacy | Do not send secrets or unnecessary personal data to a model and do not log prompts/responses unless an approved retention policy permits it. | security-review |
| NIE-CHAT-006 | error | verification | Test ownership, prompt limits, cancellation, provider failure, vector retrieval authorization, and access denial. | tests |

## Canonical reference footprint

Use these files as working examples and comparison targets. Procurement is a reference vertical, not a runtime dependency.

- src/backend/Core/Domain/Models/ChatConversation.cs
- src/backend/Core/Domain/Models/ChatMessage.cs
- src/backend/Infrastructure/AI/Models/ChatEmbedding.cs
- src/backend/Core/Application/Features/Chat/IChatService.cs
- src/backend/Core/Application/Features/Chat/ChatService.cs
- src/backend/Hosts/Api/Controllers/ChatController.cs
- src/frontend/apps/main/src/pages/chat/ChatView.vue
- src/frontend/apps/main/src/components/chat/ChatSidebar.vue
- src/frontend/apps/main/src/components/chat/ChatMessageBubble.vue
- src/frontend/apps/main/src/components/chat/ChatInputBox.vue
- src/frontend/apps/main/src/services/chatService.ts

## AI implementation and verification

The implementing AI must:

1. Assess every rule above before editing and identify affected backend, frontend, data, security, navigation, and test surfaces.
2. Compare the application implementation with the pinned canonical template reference. Classify each shared file as identical, behind, customized, ahead, conflict, or not applicable.
3. Implement missing behavior directly. Preserve domain behavior and integrate shared updates through extension points rather than overwriting valid customization.
4. Add or update focused unit, architecture, integration, component, API, and browser tests according to the evidence type for each rule.
5. Run the standard repository gates and record exact commands, exit status, and meaningful test counts or artifacts.
6. Report each rule as pass, fail, not-applicable, manual-review, or approved-exception with file/line and test evidence.

A separate AI verifier must review material changes from a fresh context, inspect the diff and evidence, rerun risk-relevant gates, and issue an independent verdict. A claim without traceable evidence is not a pass.
