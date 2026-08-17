# Requirements

> **Fill this in for your project.** This folder ships empty on purpose. Put your business requirements here — what the application should do, who uses it, and why it exists — before you design or build.
>
> Step-by-step help: [`../templates/requirements-guide.md`](../templates/requirements-guide.md).

## Recommended contents

- **Personas and actors** — every kind of user, and what each one is trying to get done
- **Use cases and workflows** — the journeys through the system, end to end
- **Business rules** — the constraints that hold regardless of which screen you are on
- **Screen inventory** — every screen, its purpose, and who may open it
- **Reporting and compliance requirements** — what has to be extractable, and for whom

## How to organise it

Start with a single `README.md` describing the project in a page or two, then split as it grows:

```text
docs/requirements/
|-- README.md          # purpose, scope, users, key entities, key workflows
|-- personas.md        # one section per persona, with permissions
|-- use-cases.md       # one section per use case, with preconditions and outcomes
|-- business-rules.md
`-- screens.md
```

One document per concern beats one document that tries to hold everything.

## Why bother

Two practical reasons, beyond it usually being part of the grade:

1. It is the cheapest place to discover that two people on the team pictured different products.
2. An AI agent working in this repository reads these files. Given a real requirements folder it will propose entities, endpoints, and screens that fit your project; given an empty one it will guess.

Write it in plain language. Requirements that only make sense to their author are not requirements.
