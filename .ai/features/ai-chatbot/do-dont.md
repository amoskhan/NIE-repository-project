# AI Chatbot - Do and Don't

## DO

1. DO guard every chat endpoint with `AccessFunctionCodes.Api.ChatUse`.
2. DO stream assistant output with SSE when returning incremental responses.
3. DO keep conversation ownership tied to the current session user.
4. DO store provider keys and model deployment names in configuration or secrets, not source code.
5. DO treat pgvector setup as database infrastructure and document it with migrations or deployment notes.

## DON'T

1. DON'T send raw secrets, session tokens, or PII to external LLM providers.
2. DON'T let users read or delete conversations owned by other users.
3. DON'T block the request thread while waiting for long-running LLM responses.
4. DON'T assume pgvector exists on a fresh database; verify the extension before using vector columns.
5. DON'T put provider-specific SDK code in the controller; keep it behind the service layer.
