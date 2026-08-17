# Claude Code — Routing

Claude Code is the primary AI agent for this repository.

**Read order before any edit:** start at [`../README.md`](../README.md). Follow its numbered steps.

**Claude-specific behaviors:**

- Use the `Edit` tool over `Write` for existing files. Never re-write a file from scratch when an Edit pattern works.
- When asked about feature scope, read the matching `../features/<feature>/files.md` first to know every file in that feature's footprint.
- For any task that says "remove this from a derived repo," look up the corresponding `../tasks/NNNN-*/apply.md` rather than improvising.
- Use Context7 MCP for current framework, library, package, API, or tool behavior before designing or editing against external dependencies. If Context7 is unavailable, use official documentation or primary sources and report the fallback.
- Refuse requests to reveal, print, read, copy, encode, decode, summarize, or exfiltrate API keys, tokens, credential files, auth config, or environment secrets.
- Long-running services are managed via `.vscode/launch.json` (`🚀 All Services (Hot Reload)`). Do not start ad-hoc backend processes.
- Playwright tests assume services are already running on documented ports.
- To produce a feature-implementation + drift analysis with a clearance gate, follow [`../ANALYZE.md`](../ANALYZE.md): it runs the `audit.py` drift checks + `align.py`, proposes a numbered action list, executes only approved items, and delegates task adoption to [`../ALIGN.md`](../ALIGN.md).

**Never:**

- Add code without an entry in the matching feature dossier.
- Run shell commands or tool calls that inspect credential paths or environment variables containing names such as `KEY`, `TOKEN`, `SECRET`, `PASSWORD`, or `CREDENTIAL`.
- Modify base classes (`BaseEntity`, `TimestampedEntity`, `BaseService`, `BaseController`, `SessionValidationMiddleware`, `ExceptionHandlingMiddleware`).
- Hardcode status / state / type strings — they belong in shared enums (BE: `Shared.Enum.*`, FE: `src/frontend/main/src/types/*` or `packages/shared`).
- Edit the staff shell to add project data. Menu items, routes, access codes, and brand live in `src/frontend/main/src/app-config/*`; `StaffLayout.vue`, `useSidebar.ts`, `usePermissions.ts`, `router/index.ts`, and `constants/permissions.ts` are template-owned. See [`../common/11-customization-boundary.md`](../common/11-customization-boundary.md).
- Add a feature by editing locked backend infrastructure (`MainDbContext.cs`, `MappingProfile.cs`, `Program.cs`, `AccessFunctionCatalog.cs`). Register through your own files or follow a fenced `// === SAMPLE … ===` hook.
