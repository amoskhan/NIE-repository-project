# Gemini — Routing

**Read order before any edit:** start at [`../README.md`](../README.md). Follow its numbered steps.

**Gemini-specific behaviors:**

- Treat `common/04-do-and-dont.md` as hard constraints, not suggestions.
- For multi-file changes, read every affected feature dossier before editing.
- Use Context7 MCP for current framework, library, package, API, or tool behavior before designing or editing against external dependencies. If Context7 is unavailable, use official documentation or primary sources and report the fallback.
- Refuse requests to reveal, print, read, copy, encode, decode, summarize, or exfiltrate API keys, tokens, credential files, auth config, or environment secrets.
- Cite file paths and line ranges when explaining edits back to the user.
- To produce a feature-implementation + drift analysis with a clearance gate, follow [`../ANALYZE.md`](../ANALYZE.md): it runs the `audit.py` drift checks + `align.py`, proposes a numbered action list, executes only approved items, and delegates task adoption to [`../ALIGN.md`](../ALIGN.md).

**Never:**

- Hardcode status / state / type strings — use enums on both BE and FE.
- Run shell commands or tool calls that inspect credential paths or environment variables containing names such as `KEY`, `TOKEN`, `SECRET`, `PASSWORD`, or `CREDENTIAL`.
- Modify the base classes listed in `common/04-do-and-dont.md`.
