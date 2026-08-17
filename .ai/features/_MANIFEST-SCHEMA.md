# `manifest.yaml` schema for feature dossiers

Every feature directory under `.ai/features/<feature-name>/` SHOULD have a `manifest.yaml` file that gives downstream tooling a structured view of the feature. The template audit reads these files to check dossier completeness, and any scaffolding tool you build on top of the template can read them to drive a module-selection UI.

`README.md`, `files.md`, `do-dont.md`, `customize.md`, `verify.md` are still authored as Markdown — `manifest.yaml` is just the _machine-readable summary_ of what the README says.

## Schema (v1)

```yaml
# Required
name: ai-chatbot # must equal the feature directory name
title: AI Chatbot # human-readable
category: backend # one of: backend / frontend / fullstack / infra / security / devex
status: scaffolded # one of: scaffolded / released / deprecated
description: |
  One paragraph (no more than 500 chars) describing what the feature
  does and when to use it. This is the blurb a scaffolding wizard or a
  feature index would show next to the feature name.

# Optional
copierFlag: include_chat # the boolean flag in copier.yml that toggles this feature; null if not Copier-gated
relatedTasks: ["0010"] # IDs of tasks (.ai/tasks/<NNNN>/) that ship or modify this feature
dependsOn: ["shared-utilities"] # feature names this one requires
removableInDerivedRepo: true # whether a derived repo can later remove this via a cleanup task
removalTaskId: null # if removable, the task ID that removes it (null if no such task yet)
files:
  - path: "src/backend/Libraries/Services/Services/Chat/IChatService.cs"
    role: interface
  - path: "src/backend/Libraries/Services/Services/Chat/ChatService.cs"
    role: implementation
  - path: "src/backend/API/Controllers/ChatController.cs"
    role: controller
  - path: "src/frontend/main/src/services/chatService.ts"
    role: client

# Tags for catalog filtering / search
tags: [llm, sse, pgvector]

# Owners (free-form strings; use "group:<team>" or "user:<handle>")
owners:
  - "group:platform"
```

## Field rules

- `name` MUST equal the directory name. Validation at seed time fails fast if not.
- `category` is a closed enum. Adding a new category is a template-versioning change (cut a release).
- `status: scaffolded` means the feature is in the template repo but no derived repo should auto-adopt it. `released` means it's stable and tasks targeting it are eligible for automated PRs. `deprecated` suppresses new auto-PRs.
- `copierFlag`, when set, must match a question name in `copier.yml`. A scaffolding wizard can derive its module-selection UI from this field.
- `files` is the canonical file list. `files.md` is the human-readable mirror. Keep them in sync (a future audit check will compare them).

## Validation

Run `python <template-root>/tools/template-audit/audit.py --repo <repo>` from the central App Template checkout. The audit's `features` category checks for the presence of `manifest.yaml`, validates required fields, and confirms `category` is in the closed enum.
