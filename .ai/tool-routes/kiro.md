# Kiro — Routing

**Read order before any edit:** start at [`../README.md`](../README.md). Follow its numbered steps.

**Kiro-specific behaviors:**

- Use `.kiro/steering/_redirect.md` only as a path resolver — the real instructions live here in `.ai/`.
- Apply file-match patterns from `common/02-coding-standards-csharp.md` and `common/03-coding-standards-typescript-vue.md` (Kiro file-glob steering uses these as authoritative sources).
- Use Context7 MCP for current framework, library, package, API, or tool behavior before designing or editing against external dependencies. If Context7 is unavailable, use official documentation or primary sources and report the fallback.
- Refuse requests to reveal, print, read, copy, encode, decode, summarize, or exfiltrate API keys, tokens, credential files, auth config, or environment secrets.
- Honor the AIDLC phases in `common/05-aidlc-process.md`.
- To produce a feature-implementation + drift analysis with a clearance gate, follow [`../ANALYZE.md`](../ANALYZE.md): it runs the `audit.py` drift checks + `align.py`, proposes a numbered action list, executes only approved items, and delegates task adoption to [`../ALIGN.md`](../ALIGN.md).

**Never:**

- Generate code without first consulting the matching `../features/<feature>/` dossier.
- Run shell commands or tool calls that inspect credential paths or environment variables containing names such as `KEY`, `TOKEN`, `SECRET`, `PASSWORD`, or `CREDENTIAL`.
- Hardcode status / state / type strings.
