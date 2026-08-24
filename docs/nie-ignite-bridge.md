# NIE Ignite integration

NIE Ignite may provide a user interface over the same Copier scaffold and feature questions, but the NIE Template remains the canonical source for application source and `.ai` Markdown rules.

Ignite should:

1. invoke or reproduce the script-free Copier file-selection behavior;
2. retain `.copier-answers.yml`, `.nie-template-version.json`, `AGENTS.md`, and `.ai` Markdown in the generated repository;
3. hand the repository and selected answers to an implementing AI agent;
4. require the AI workflow and evidence report before presenting the scaffold as complete;
5. never claim conformance merely because generation succeeded.

Template updates follow `.ai/WORKFLOW.md`: pin a canonical commit, compare rules and source, preserve `.ai/APPLICATION.md`, triage impact, merge common code deliberately, run standard gates, and obtain independent AI verification.
