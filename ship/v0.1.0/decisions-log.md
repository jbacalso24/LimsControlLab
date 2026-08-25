# Decisions log — `v0.1.0`

> Every decision made while resolving the audit's open questions: what was chosen, what was rejected,
> and **why**. Reversals get a new row (never edit history) so "deliberately dropped" stays
> distinguishable from "accidentally missing." A decision of *"build decides"* is a valid, explicit
> delegation — recorded here, not left silent.

- **Release:** v0.1.0  ·  **Decided:** 0
- **Built from:** `ship/v0.1.0/audits/codebase-2026-08-25/additions.md@3990b17` + `coverage.md@3990b17`

| # | Date | Question (OQ #) | Decision | Acceptance criteria (EARS) | Rejected alternative | Why | Owner |
|---|------|-----------------|----------|----------------------------|----------------------|-----|-------|
| — | — | — | — | — | — | — | — |

## Why this is empty

The `codebase-2026-08-25` audit routed **zero** rows to `clarify` — `additions.md` found nothing added
(the repository is empty), and every `coverage.md` finding routed to `plan` as a build task, not a
decision, because the Definition phase (`brief.md`, `charter.md`) had already closed every requirement-
and role-level question before the audit ran. There is nothing for this stage to decide this cycle. This
file exists (rather than being absent) so `/ship:design-handoff`'s gate — which requires
`decisions-log.md` to exist — has something real to point at, honestly stating why it holds no rows.
