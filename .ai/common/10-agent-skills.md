# 10 - External Agent Skills

Use upstream Agent Skills as companion guidance for framework-specific work. The template keeps pointers and bootstrap instructions here; do not vendor upstream skill repositories into `.ai/`.

## Skill sources

| Area                                                | Source                               | Install unit                                                                                                                  |
| --------------------------------------------------- | ------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------- |
| .NET / ASP.NET Core / EF Core / MSBuild / tests     | <https://github.com/dotnet/skills>   | Tool plugin marketplace `dotnet/skills`, or individual skills under `plugins/<plugin>/skills/<skill-name>`                    |
| Vue 3 / TypeScript / Pinia / Vue Router / Vue tests | <https://github.com/vuejs-ai/skills> | Tool plugin marketplace `vuejs-ai/skills`, `npx skills add vuejs-ai/skills`, or individual skills under `skills/<skill-name>` |

## When to use them

- Before using external framework, library, package, API, or tool behavior, use Context7 MCP for current documentation when it is available. If Context7 is unavailable, use official documentation or primary sources and report that fallback.
- For `src/backend/**`, `.csproj`, `.sln`, `.props`, `.targets`, migrations, tests, diagnostics, or package-management work, use the relevant .NET skill before designing or editing.
- For `src/frontend/**`, `.vue`, frontend `.ts`, Pinia, Vue Router, Vue Test Utils, Vitest, or Playwright frontend work, use the relevant Vue skill before designing or editing.
- If a feature touches both stacks, use both families of skills and then apply the stricter rule from `.ai/common/04-do-and-dont.md` — this repo's rules win over an upstream skill's advice.
- If the local AI tool cannot load Agent Skills, continue with the `.ai/common/` docs in this repo and report which skill was missing in the final response.

## Local presence check

Installing skills changes the developer's local AI-tool configuration, not this repository. Ask before installing unless the user has explicitly requested local skill installation.

For Codex-style local skills, check `%CODEX_HOME%\skills` when `CODEX_HOME` is set, otherwise `%USERPROFILE%\.codex\skills`:

```powershell
$SkillRoot = if ($env:CODEX_HOME) { Join-Path $env:CODEX_HOME "skills" } else { Join-Path $HOME ".codex\skills" }

Test-Path (Join-Path $SkillRoot "dotnet-webapi\SKILL.md")
Test-Path (Join-Path $SkillRoot "optimizing-ef-core-queries\SKILL.md")
Test-Path (Join-Path $SkillRoot "run-tests\SKILL.md")
Test-Path (Join-Path $SkillRoot "vue-best-practices\SKILL.md")
Test-Path (Join-Path $SkillRoot "vue-router-best-practices\SKILL.md")
Test-Path (Join-Path $SkillRoot "vue-testing-best-practices\SKILL.md")
```

For plugin-based tools, use the tool's `/skills`, `/agents`, or plugin panel to confirm the marketplace entries are installed.

For Kiro CLI, skills are discovered from `~/.kiro/skills/<skill-name>/SKILL.md` or the workspace `.kiro/skills/<skill-name>/SKILL.md`.

For Gemini CLI, enable Agent Skills in `~/.gemini/settings.json` when needed and install skills through the Gemini skills command or under `~/.gemini/skills/<skill-name>/SKILL.md`.

## Install when missing

### Claude Code / Copilot CLI plugin flow

```text
/plugin marketplace add dotnet/skills
/plugin install dotnet@dotnet-agent-skills dotnet-aspnet@dotnet-agent-skills dotnet-data@dotnet-agent-skills dotnet-msbuild@dotnet-agent-skills dotnet-test@dotnet-agent-skills dotnet-upgrade@dotnet-agent-skills dotnet11@dotnet-agent-skills

/plugin marketplace add vuejs-ai/skills
/plugin install vue-skills-bundle@vue-skills
```

Add task-specific .NET plugins as needed:

```text
/plugin install dotnet-ai@dotnet-agent-skills dotnet-template-engine@dotnet-agent-skills dotnet-nuget@dotnet-agent-skills
/plugin install dotnet-diag@dotnet-agent-skills
/plugin install dotnet-maui@dotnet-agent-skills
```

Use `dotnet-experimental@dotnet-agent-skills` only with explicit user approval.

### VS Code Copilot plugin preview

Add the marketplace in user or workspace settings, then browse `/plugins` in Copilot Chat:

```json
{
  "chat.plugins.enabled": true,
  "chat.plugins.marketplaces": ["dotnet/skills", "vuejs-ai/skills"]
}
```

### Vue skills manager

For tools that support the `skills` CLI:

```bash
npx skills add vuejs-ai/skills
```

### Gemini CLI

Gemini CLI can install skills from a Git URL, local path, or file:

```bash
gemini skills install https://github.com/vuejs-ai/skills/tree/main/skills/vue-best-practices
gemini skills install https://github.com/vuejs-ai/skills/tree/main/skills/vue-router-best-practices
gemini skills install https://github.com/vuejs-ai/skills/tree/main/skills/vue-testing-best-practices
```

If the `gemini skills` command is unavailable, copy the skill folder so the final layout is `~/.gemini/skills/<skill-name>/SKILL.md`.

### Kiro CLI

Kiro CLI discovers standard `SKILL.md` packages from global or workspace skill directories. Copy the required skill folders so one of these layouts exists:

```text
~/.kiro/skills/<skill-name>/SKILL.md
.kiro/skills/<skill-name>/SKILL.md
```

### Codex individual-skill flow

Install only missing skills. Examples:

```bash
skill-installer install https://github.com/dotnet/skills/tree/main/plugins/dotnet-aspnet/skills/dotnet-webapi
skill-installer install https://github.com/dotnet/skills/tree/main/plugins/dotnet-data/skills/optimizing-ef-core-queries
skill-installer install https://github.com/dotnet/skills/tree/main/plugins/dotnet-test/skills/run-tests

skill-installer install https://github.com/vuejs-ai/skills/tree/main/skills/vue-best-practices
skill-installer install https://github.com/vuejs-ai/skills/tree/main/skills/vue-router-best-practices
skill-installer install https://github.com/vuejs-ai/skills/tree/main/skills/vue-testing-best-practices
```

For other .NET skills, use this URL shape:

```text
https://github.com/dotnet/skills/tree/main/plugins/<plugin>/skills/<skill-name>
```

For other Vue skills, use this URL shape:

```text
https://github.com/vuejs-ai/skills/tree/main/skills/<skill-name>
```

Restart the AI tool after installing skills so the tool can discover them.

## Baseline skill map

| Work area                                                  | Start with these skills                                                                                           |
| ---------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| ASP.NET Core endpoints, middleware, uploads, OpenTelemetry | `dotnet-webapi`, `minimal-api-file-upload`, `configuring-opentelemetry-dotnet`                                    |
| EF Core queries and data access                            | `optimizing-ef-core-queries`                                                                                      |
| Builds, project files, MSBuild targets                     | `binlog-failure-analysis`, `binlog-generation`, `msbuild-modernization`, `target-authoring`                       |
| Tests and test migrations                                  | `run-tests`, `filter-syntax`, `platform-detection`, `writing-mstest-tests`, `migrate-vstest-to-mtp`               |
| .NET upgrades and compatibility                            | `migrate-dotnet9-to-dotnet10`, `migrate-dotnet10-to-dotnet11`, `dotnet-aot-compat`, `migrate-nullable-references` |
| Vue 3 Composition API                                      | `vue-best-practices`                                                                                              |
| Vue Router                                                 | `vue-router-best-practices`                                                                                       |
| Pinia                                                      | `vue-pinia-best-practices`                                                                                        |
| Vue tests                                                  | `vue-testing-best-practices`                                                                                      |
| Vue JSX                                                    | `vue-jsx-best-practices`                                                                                          |
| Vue debugging                                              | `vue-debug-guides`                                                                                                |
| Reusable composables                                       | `create-adaptable-composable`                                                                                     |
