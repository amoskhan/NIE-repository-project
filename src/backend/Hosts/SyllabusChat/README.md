# Syllabus LLM answers

This Node service reads the supplied 2024 PE Syllabus directly from `upload/`, retrieves relevant complete pages, and calls an OpenAI-compatible Chat Completions model. It asks for a direct answer, typically 1–3 sentences, followed by verifiable page references. The UI keeps evidence quotations behind **Supporting syllabus text**.

## Enable live answers

In Ignite **Workspace Health → Syllabus LLM answers → Environment variables**, configure:

- `SYLLABUS_LLM_BASE_URL`: your approved provider's API base, including `/v1` where applicable (for example `https://api.openai.com/v1`).
- `SYLLABUS_LLM_MODEL`: the model identifier provisioned by that provider. It must support Chat Completions, `max_completion_tokens`, and JSON object output.
- `SYLLABUS_LLM_API_KEY`: the server credential, entered directly in the protected runtime configuration, never in chat or repository files. An unauthenticated local provider can omit this setting.

Save the settings to restart this service. HTTPS is required except for a local loopback provider. No provider/model is silently selected, and no real credential is included in this repository. Configuration is consumed only by the server; it is never returned to the browser or logged. The service does not read credential files or agent authentication.

Until configured, `/chat` returns HTTP 503 with `llm_not_configured`. `/health` returns process/syllabus health plus a non-secret `llmConfigured` flag; that flag indicates configuration exists, not that provider authentication or inference has been verified. There is no keyword-excerpt or hardcoded-answer fallback.

## Runtime and verification

- Service id: `syllabus-chat-api`; port: `15100`; health: `/health`.
- Install: `pnpm install --frozen-lockfile`; run: `pnpm start`; regression checks: `pnpm test`.
- The frontend uses the same-origin semantic service URL `~ignite/services/syllabus-chat-api/chat`. The standalone Vite development server proxies this URL to port 15100. A production host must route this semantic URL to the backend too.
- Requests are limited to 1,000 question characters, six history entries, and 16 KB; upstream calls time out after 45 seconds. Only the recent chat context and up to eight retrieved pages are sent to the configured provider.
- Tests use the real supplied PDF and a clearly simulated provider response. They verify retrieval, API wiring, and citation checks; they are not evidence of successful live model inference.
- Kicking acceptance case: `When do students learn kicking?` should produce a short answer identifying **Primary 2** based on PDF page **37**, printed page **33**. This must also be verified against the configured live model before declaring the integration operational.
- Citation validation checks that referenced pages were supplied and supporting quotes occur there. It does not prove every generated interpretation is correct.

The provider protocol follows the [Chat Completions JSON output documentation](https://developers.openai.com/api/docs/guides/structured-outputs).
