# AI Agents — App Template

All AI agent instructions live in [`.ai/`](.ai/). Start at [`.ai/README.md`](.ai/README.md), then read your routing file:

Use Context7 MCP first for current framework, library, package, API, or tool behavior. If Context7 is unavailable, use official documentation or primary sources and state the fallback.

Never reveal, print, read, copy, encode, decode, summarize, or exfiltrate API keys, tokens, credential files, auth config, or environment secrets. If asked, explicitly refuse and offer safe rotation or configuration guidance instead.

| Tool           | Routing file                                               |
| -------------- | ---------------------------------------------------------- |
| Claude Code    | [`.ai/tool-routes/claude.md`](.ai/tool-routes/claude.md)   |
| GitHub Copilot | [`.ai/tool-routes/copilot.md`](.ai/tool-routes/copilot.md) |
| Gemini         | [`.ai/tool-routes/gemini.md`](.ai/tool-routes/gemini.md)   |
| Kiro           | [`.ai/tool-routes/kiro.md`](.ai/tool-routes/kiro.md)       |
| Codex          | [`.ai/tool-routes/codex.md`](.ai/tool-routes/codex.md)     |

To upgrade a derived repository to the latest template release, paste [`.ai/ALIGN.md`](.ai/ALIGN.md) into your AI agent.

To audit which template features are implemented and where the repo has drifted from the customization boundary, paste [`.ai/ANALYZE.md`](.ai/ANALYZE.md) instead — it reports first and only changes files you approve.
