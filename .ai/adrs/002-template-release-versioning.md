# Template Release Versioning

## Metadata

- **Date:** 2026-04-13
- **Status:** Accepted
- **Amended by:** [`004-configurable-regional-defaults.md`](./004-configurable-regional-defaults.md) — the release timezone is now configuration with `Asia/Singapore` as the default; the `YYYY.MM.DD.N` format below is unchanged.
- **Deciders:** App Template maintainers
- **AI Model Used:** GPT-5

## Context

App Template needed a formal release versioning system that works for both humans and AI agents. Template changes were landing without a machine-readable way to explain what changed, whether it was breaking, and how existing production repositories should adopt the change in order.

## Options Considered

### Option A: Git tags only

**Description:** Use only Git tags and commit history to describe template changes.

- **Pros:** Minimal repository surface area
- **Cons:** Not decision-complete for AI agents, no stable release manifest, and hard to consume from downstream repos

### Option B: Date-based file-backed releases

**Description:** Keep a root version marker, changelog, release index, and one machine-readable manifest plus one markdown note per release.

- **Pros:** Easy for maintainers to cut, easy for AI agents to read, and supports downstream adoption tracking
- **Cons:** Adds process overhead and requires validation tooling

### Option C: Semantic versioning with manual release batches

**Description:** Cut larger named releases manually and version them with SemVer.

- **Pros:** Familiar release scheme and fewer version bumps
- **Cons:** The team requested every canonical template change to produce a release, and batch releases hide fine-grained upgrade steps from AI agents

## Decision

Adopt Option B. App Template now uses date-based release versions in Singapore local time (`YYYY.MM.DD.N`) backed by repository files rather than Git tags. Each canonical template change updates the root version marker, changelog, release index, and release manifest pair together. AI agents consume those files directly when upgrading downstream repositories.

## Consequences

- **Positive:** Template releases become explicit, ordered, and readable by both humans and AI agents.
- **Positive:** Downstream repos can record which template release they have adopted.
- **Negative:** Template maintainers must update release metadata for every canonical change.
- **Risks:** If validation is bypassed, repo changes could land without synchronized release metadata.

## AI Reasoning Chain

> The team needs release metadata that AI agents can consume deterministically. Git history alone is too implicit. A date-based version plus a machine-readable release manifest keeps the release process lightweight while still making downstream upgrades decision-complete. Using Singapore local time avoids timezone drift for the maintainer team.
