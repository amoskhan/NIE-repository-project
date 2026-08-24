# NIE application agent contract

The canonical rules repository is `https://niegithub.nie.edu.sg/NIE/nie-template`.

Before material work, read `.ai/README.md`, `.ai/WORKFLOW.md`, `.ai/APPLICATION.md`, `.ai/GLOBAL-RULES.md`, `.ai/LIBRARIES.md`, and every affected `.ai/FEATURE-*.md` dossier.

For template alignment, fetch or clone the canonical repository into a temporary location, pin an exact commit, compare the canonical rules and affected source, preserve `.ai/APPLICATION.md`, and follow the classification and evidence workflow in `.ai/WORKFLOW.md`. Do not use whole-folder source replacement over customized application code.

AI agents implement approved changes, tests, migrations, configuration, and documentation. Standard linters, analyzers, builds, tests, coverage, dependency scanners, secret scanners, and CodeQL provide deterministic evidence but do not replace architectural review. Material changes require a separate AI verifier and the report format in `.ai/REPORT.md`.

For dependencies, follow `NIE-DEPS-*` in `.ai/GLOBAL-RULES.md`: prefer the .NET platform/BCL and official Microsoft packages for .NET platform capabilities, then the technology owner's official open-source package, then a mature widely adopted open-source package. Do not introduce proprietary coupling or a vendor-specific dependency without an application-owned abstraction, portability evidence, and explicit approval.

Procurement is the real-world reference vertical. Learn from it without creating a runtime dependency on its domain.

Keep canonical source identities stable: do not rename `Backend.sln`, the generic .NET projects/namespaces, `apps/main`, `apps/auth`, or `@nie/*` packages to the product name. Apply product identity only through typed configuration, branding, catalog metadata, observability, deployment values, and external labels. Merge canonical changes at these stable paths.

Use Context7 first for current external library, framework, SDK, API, CLI, and cloud-service behavior. Never reveal, inspect, copy, transform, or summarize secrets, tokens, credential files, or secret-bearing configuration.
