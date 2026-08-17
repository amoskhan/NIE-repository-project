# Architecture

> **Fill this in for your project.** This file ships as a stub. It is the project-specific source of truth for how _your_ system is put together — the template only tells you what shape it should take.
>
> Step-by-step help: [`templates/architecture-guide.md`](templates/architecture-guide.md).
> The template's own baseline (services, ports, data stores) is described in the [README](../README.md) and [`GETTING-STARTED.md`](GETTING-STARTED.md); start from that and add what your project changes.

## Minimum Contents

- System context diagram (who uses the system, what it talks to)
- Container-level diagram (services, packages, data stores)
- Key external integrations
- Deployment and runtime assumptions
- Architectural constraints and open questions

All diagrams use [Mermaid](https://mermaid.js.org/). Text diagrams can be read by reviewers, by diff tools, and by AI agents; a screenshot cannot.

## Suggested skeleton

```markdown
## System Context

<mermaid diagram: actors, this system, external systems>

## Containers

<mermaid diagram: frontends, APIs, database, cache, background jobs>

## External Integrations

| System | Direction | Protocol | Auth | Failure behaviour |
| ------ | --------- | -------- | ---- | ----------------- |

## Deployment

Where it runs, how it is built, what configuration it needs at runtime.

## Constraints and Open Questions

Anything you have decided not to solve yet, and why.
```

## Update When

- A new service, package, or runtime boundary is introduced
- An integration or deployment topology changes
- The system context or trust boundaries move
