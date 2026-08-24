# @nie/contracts

Dependency-free TypeScript contracts shared across frontend applications and platform adapters.

Put only transport shapes, identity/role contracts, and other stable serializable types here. Do not add Vue, browser APIs, HTTP clients, observability, UI components, product navigation, or business workflow state. Export every public contract from `src/index.ts`.

Run `pnpm run build:contracts` from `src/frontend`, followed by the platform and consuming application type checks whenever a contract changes.
