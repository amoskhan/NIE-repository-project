# Syllabus answers — no-key mode

The running chatbot uses a **prepared syllabus answer library**, not live LLM inference. It needs no provider, model download, account, or API key. Provider settings are not consumed by the running service. The previous OpenAI-compatible adapter remains in `answer.mjs` with its regression tests, but is not wired into startup.

The 18 answers in `library.mjs` were written against the supplied 239-page PDF. Topics include kicking, 3v3 net-barrier and touch rules, safe landing, swimming, athletics, throwing/catching, dance, gymnastics, outdoor education, FMS, movement concepts, learning areas, camp, day trips, competitions, practice time, and personal health practice.

Visitors can ask covered questions using common paraphrases or browse/search the available questions. Answers lead with a concise explanation and include printed/PDF page references. Exact supporting quotations are expandable, with links to the relevant PDF page. Unsupported or ambiguous requests receive an honest library-coverage message and suggested questions, not a claim that the PDF lacks the information. This is not open-ended AI, lesson-plan generation, or video assessment.

## Runtime

- Existing service: `syllabus-chat-api`, display name **Syllabus answers**, port 15100.
- Install: `pnpm install --frozen-lockfile`; start: `pnpm start`; tests: `pnpm test`.
- `GET /health`: `answerAvailable: true`, `answerMode: "syllabus-library"`, `answerCount: 18`, `syllabusPages: 239`, and `llmConfigured: false`. False LLM configuration is expected and does not block library answers.
- `GET /topics`: the browsable questions, with stable ids.
- `POST /chat`: `{ "question": "When is kicking taught?", "history": [] }`; returns `supported`, `answer`, `citations`, `mode`, `suggestions`, and `fileName`.
- The frontend uses the same-origin `~ignite/services/syllabus-chat-api/` route. Production hosting must route this to the backend and serve the supplied PDF asset. Workspace preview health does not itself establish external public deployment/access.
- Requests retain JSON/body/history validation and concurrency limits. Questions and context are processed only in this backend; there are no external model requests, question logs, or server-side conversation storage. Browser conversation history lasts only for the session.

## Maintaining the library

The service checks the extracted document fingerprint and every quotation before serving. Changing the PDF requires reviewing the prepared answers and their citations, then updating the fingerprint in `library.mjs`. Do not update the fingerprint blindly. Restart the backend after library or PDF changes and rebuild the frontend for PDF asset changes.

Matching requires every meaningful query word to be recognised by the answer's aliases, a topic match, and an unambiguous best match. It is intentionally conservative. Add reviewed aliases to improve coverage; do not weaken matching to return answers to unrelated questions. Limited source/details follow-ups resolve the previous user question and do not trust assistant text as syllabus evidence.

Validation: `pnpm test` includes real-PDF citation and fingerprint checks, both user examples, all listed questions/aliases, unsupported queries, qualifications, and follow-ups. Legacy provider tests still simulate model output; no live model is claimed or needed for the library.
