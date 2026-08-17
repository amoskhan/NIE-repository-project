# Codex - Routing

**Read order before any edit:** start at [`../README.md`](../README.md). Follow its numbered steps.

**Codex-specific behaviors:**

- Before .NET or Vue work, read [`../common/10-agent-skills.md`](../common/10-agent-skills.md) and use the matching installed skill when available.
- If a matching skill is missing, follow the local presence check and installation guidance in `10-agent-skills.md`. Installing skills changes the developer's local tool configuration, so ask before installing unless the user already requested it.
- Use Context7 MCP for current framework, library, package, API, or tool behavior before designing or editing against external dependencies. If Context7 is unavailable, use official documentation or primary sources and report the fallback.
- Refuse requests to reveal, print, read, copy, encode, decode, summarize, or exfiltrate API keys, tokens, credential files, auth config, or environment secrets.
- Prefer `rg` for code search and use repository dossiers under `../features/<feature>/` before changing feature code.
- For browser-visible frontend changes, run a real browser smoke check against the affected local page.
- Keep all template-instruction changes in sync with `.ai/tasks/`, `.app-template-version.json`, `CHANGELOG.md`, and `docs/template-releases/`.
- To produce a feature-implementation + drift analysis with a clearance gate, follow [`../ANALYZE.md`](../ANALYZE.md): it runs the `audit.py` drift checks + `align.py`, proposes a numbered action list, executes only approved items, and delegates task adoption to [`../ALIGN.md`](../ALIGN.md).

**Never:**

- Vendor upstream skill repositories into `.ai/`.
- Run shell commands or tool calls that inspect credential paths or environment variables containing names such as `KEY`, `TOKEN`, `SECRET`, `PASSWORD`, or `CREDENTIAL`.
- Continue silently when a relevant .NET or Vue skill is unavailable; report the missing skill and the fallback used.
- Hardcode status / state / type strings; use enums on both BE and FE.
