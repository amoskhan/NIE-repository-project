# Configurable Timezone and Locale, with Singapore Defaults

## Metadata

- **Date:** 2026-07-31
- **Status:** Accepted
- **Deciders:** App Template maintainers
- **AI Model Used:** Claude Opus 5

## Context

[`002-template-release-versioning.md`](./002-template-release-versioning.md) fixed release versions to `YYYY.MM.DD.N` in Singapore local time, and several other surfaces (release stamps, analysis report dates, date formatting) assumed the same zone and an `en-SG` locale.

The template is now used by teams who may not be in that region. A hardcoded zone is wrong for them in small but constant ways: a release minted "today" is dated yesterday, analysis reports collide, and displayed timestamps read as someone else's clock. At the same time, ripping the defaults out entirely would leave every new project with an empty required setting before it can cut its first release.

This ADR amends the timezone assumption in ADR 002. The date-based version format itself is unchanged.

## Options Considered

### Option A: Keep Asia/Singapore hardcoded

**Description:** Leave the zone as a constant in the release tooling and docs.

- **Pros:** Zero configuration; every release is trivially comparable.
- **Cons:** Wrong for any team outside the zone, and it reads as institution-specific leftovers in a template meant to be neutral.

### Option B: Require an explicit timezone with no default

**Description:** Make the zone a mandatory setting that a project must supply before its first release.

- **Pros:** No hidden regional assumption anywhere.
- **Cons:** Adds a required setup step and a failure mode on first use, for a value most teams do not care about on day one.

### Option C: Configurable with a shipped default

**Description:** Resolve the zone from CLI flag → environment variable → the `timezone` field in `.app-template-version.json` → `docs/template-releases/index.json` → the default `Asia/Singapore`. Locale defaults to `en-SG` and is likewise overridable.

- **Pros:** Works immediately with no setup, and a team changes it once in the version marker and every later release inherits it.
- **Cons:** A default that is invisible until it surprises someone; two projects in different zones can mint versions that sort by different local days.

## Decision

Adopt **Option C**. Timezone and locale are configuration with `Asia/Singapore` and `en-SG` as shipped defaults, not as rules.

- `tools/template-versioning/release.py` resolves the zone in this order: `--timezone`, `APP_TEMPLATE_TIMEZONE`, the `timezone` field in `.app-template-version.json`, the `timezone` field in `docs/template-releases/index.json`, then `Asia/Singapore`.
- Setting `timezone` once in `.app-template-version.json` is the supported way to change it. Every subsequent release inherits it.
- Agent playbooks that stamp dates (`ANALYZE.md` reports) use the repo's configured zone and record which zone they used in the output.
- Feature code never hardcodes a timezone or locale. Display formatting reads the configured locale.
- A wrong-looking regional default is a configuration fix, not a template defect — it must not be reported as a critical audit failure.

## Consequences

- **Positive:** Teams outside the default zone get correct dates by changing one field.
- **Positive:** The template still runs and releases with zero configuration.
- **Negative:** Release version strings are only totally ordered within a single project, because `YYYY.MM.DD.N` is local-day based.
- **Risks:** A project that never sets `timezone` silently inherits Singapore days. The IANA zone database may be missing on minimal containers; the release tool warns and falls back rather than failing.

## AI Reasoning Chain

> The requirement was to strip institution-specific assumptions without degrading first-run experience. Deleting the default (Option B) trades a wrong value for a required decision, which is worse for a template whose main promise is "clone it and it works." Keeping it hardcoded (Option A) is exactly the kind of embedded regional assumption this cleanup exists to remove. A resolution chain that ends in a documented default preserves zero-config startup while making the value a first-class, discoverable setting — and recording the resolved zone in generated artifacts means a reader can always tell which clock produced a date. The date-based version format from ADR 002 stays; only its "Singapore" clause is amended.
