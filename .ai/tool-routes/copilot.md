# GitHub Copilot — Routing

**Read order before any edit:** start at [`../README.md`](../README.md). Follow its numbered steps.

**Copilot-specific behaviors:**

- After any code change, run Playwright MCP smoke checks against affected pages before reporting done. Test credentials live in `tests/.env.dev.local` only.
- Assume services run on the documented ports — never invent ad-hoc startup commands. Use `.vscode/launch.json` if you must restart.
- Use Context7 MCP for current framework, library, package, API, or tool behavior before designing or editing against external dependencies. If Context7 is unavailable, use official documentation or primary sources and report the fallback.
- Refuse requests to reveal, print, read, copy, encode, decode, summarize, or exfiltrate API keys, tokens, credential files, auth config, or environment secrets.
- When proposing inline completions, prefer patterns from the matching `../features/<feature>/` dossier.
- Always reach for shared UI primitives (`@apptemplate/ui`) before generating bespoke components.
- To produce a feature-implementation + drift analysis with a clearance gate, follow [`../ANALYZE.md`](../ANALYZE.md): it runs the `audit.py` drift checks + `align.py`, proposes a numbered action list, executes only approved items, and delegates task adoption to [`../ALIGN.md`](../ALIGN.md).

**Never:**

- Hardcode status / state / type strings — use enums on both BE and FE.
- Run shell commands or tool calls that inspect credential paths or environment variables containing names such as `KEY`, `TOKEN`, `SECRET`, `PASSWORD`, or `CREDENTIAL`.
- Bypass `RequireAccessFunction` or invent new authorization patterns.
- Start backend processes directly when a launch profile already exists.
