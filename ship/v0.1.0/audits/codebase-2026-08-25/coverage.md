# What the design covers — `v0.1.0` · codebase `2026-08-25`

This is a from-scratch repository: no solution/project files, no source directories, nothing beyond the `ship/` dossier and `docs/` source material. Every rubric item that depends on inspecting code is therefore a GAP — the honest reading is "not yet built," never "undecided." Four items are `N/A` because they were settled in Definition (`brief.md`/`charter.md`) rather than belonging in a repository at all, per the rubric's own per-platform table.

## What was reviewed, and what was not

- **Reviewed:** the full working tree at commit `79a43d0`, branch `release/v0.1.0`.
- **Not reviewed, and why:** nothing was excluded. `docs/` (source BRD/HLD/spreadsheet) and `ship/` (the dossier) were read as context, not as audit subject — the subject is application code, of which there is none.
- **No figures captured.** There is no rendered UI to capture; a figure can only show presence, and nothing is present yet.

## Coverage, by rubric section

### A. Screens, IA & visual coverage

| Requirement | Verdict | What is short | Severity | Routes to | Pointer |
|-------------|---------|----------------|----------|-----------|---------|
| A1 — IA / entity hierarchy stated explicitly in the repo (solution layout, README) | Not found | No solution file, project manifest, or README exists yet | gap | plan | absent — no repo structure yet |
| A2 — Every in-scope surface (API endpoint or UI screen) exists, with full composition | Not found | Zero endpoints, zero components implemented against brief §3's 78 requirements | gap | plan | absent |
| A3 — Empty/loading/error/role-variant states shown, not just happy path | Not found | No UI exists to show any state | gap | plan | absent |
| A4 — Global chrome (nav, account, theme, brand) settled & consistent | N/A | Not applicable on `codebase` — no chrome exists to be inconsistent; will apply once the Angular shell is built | n/a | — | — |

### B. Decisions & intent capture

| Requirement | Verdict | What is short | Severity | Routes to | Pointer |
|-------------|---------|----------------|----------|-----------|---------|
| B1 — A decisions log ships in the bundle | N/A | Not applicable on `codebase` — decisions live in `brief.md`/`charter.md`, which already exist and are complete; re-demanding this inside the repo would fail every backend audit for a document never meant to be there | n/a | — | `brief.md`, `charter.md` |
| B3 — Behavioural rules attached to affordances are written next to them (in code) | Not found | No code exists to attach rules to | gap | plan | absent |
| B4 — Every decision `design-request.md` §11 asked for has an explicit answer | Pass (vacuous) | §11 states `n/a — no external design decisions phase`; zero decisions were raised, so none are unanswered | — | — | `design-request.md` §11 |

### C. Stub & fidelity honesty

| Requirement | Verdict | What is short | Severity | Routes to | Pointer |
|-------------|---------|----------------|----------|-----------|---------|
| C1 — Every unwired surface/control labelled stub/mock-data/not-in-scope | Not found | No surfaces exist at all — there is nothing to mislabel, but the check requires a positive labelling convention to exist in the codebase once surfaces are added, and none does yet | blocker | plan | absent — establish a fidelity/stub-labelling convention when the first surfaces are built |
| C2 — Placeholder/known-fake data flagged as such | Not found | No seed/sample data exists | gap | plan | absent |

### D. Business rules & state machines *(heaviest section)*

| Requirement | Verdict | What is short | Severity | Routes to | Pointer |
|-------------|---------|----------------|----------|-----------|---------|
| D1 — Each lifecycle aggregate (Sample, Analysis, Exception, Lock) has a state machine in code (states, transitions, trigger, actor, guard) | Not found | Brief R19–R25 (lifecycle), R33–R38 (exceptions), R42–R47 (lock/unlock) fully state the required transitions in prose; **none is implemented in code** | blocker | plan | absent — implement per brief R19–R25, R33–R38, R42–R47 |
| D2 — Derived/roll-up values state their derivation rule, in code | Not found | Brief R39–R41 (calculated/derived results) and charter §2 (recompute-until-locked posture) are fully specified in the dossier; not implemented | blocker | plan | absent — implement per brief R39–R41, R57; charter §2 |
| D3 — Audit/history expectations stated where they matter, in code | Not found | Brief R22, R23, R75 (full audit trail) not implemented | gap | plan | absent |
| D4 — Cross-entity cascade rules specified (e.g. unlock → recompute, exception → lock state) | Not found | Brief R41, R46 (cascades on unlock/rejection) not implemented | gap | plan | absent |
| D5 — Auto-advance vs manual transitions distinguished per lifecycle | Not found | Brief §3 already distinguishes these in prose (e.g. analyses don't auto-advance without an analyst action; derived values do auto-recompute) but nothing is coded | gap | plan | absent |

### E. Permissions & policy

| Requirement | Verdict | What is short | Severity | Routes to | Pointer |
|-------------|---------|----------------|----------|-----------|---------|
| E1 — Role model defined and ratified (Control Lab Analyst, Lab Coordinator) | Not found (ratified elsewhere) | The role model itself **is** ratified — brief §5, charter confirmation 2026-08-25 — but nothing in code expresses it yet (no role/claim definitions) | gap | plan | absent — brief §5 is the ratified source; implement per it |
| E2 — Permission policies for edge cases decided (site segregation, unlock-only-by-Coordinator) | Not found | Brief §5, charter §2 (server-side RBAC by role + site) fully decided; not implemented | blocker | plan | absent — implement per brief §5, charter §2 |
| E3 — Enforcement stated as server-side, not UI-only | Not found (stated elsewhere) | Charter §2/constitution explicitly require server-side enforcement; nothing in code yet to verify it against | gap | plan | absent — constitution.md "Security constraints" is the ratified source |

### F. Data model & seed integrity

| Requirement | Verdict | What is short | Severity | Routes to | Pointer |
|-------------|---------|----------------|----------|-----------|---------|
| F1 — Entity/data model & relationships documented (in migrations/models) | Not found | Brief §4 states the data/source-of-truth table; no EF Core models or migrations exist | gap | plan | absent |
| F2 — Normalisation intent stated (aggregate-on-read vs denormalise) | Not found (stated in standard) | `engineering-standards.md` §3 already states this posture; not yet implemented | normal/gap | plan | `engineering-standards.md` §3 |
| F3 — Seed/sample data internally consistent with declared counts | Not found | No seed data exists; brief §2's qualitative volumes (hundreds of samples/day/site) have no corresponding fixtures yet | gap | plan | absent |

### G. Consistency & single source of truth

| Requirement | Verdict | What is short | Severity | Routes to | Pointer |
|-------------|---------|----------------|----------|-----------|---------|
| G1 — One canonical source, no duplicate/stale trees | Pass | Single git repository, single working tree, no duplicate or stale copies exist | — | — | `git rev-parse HEAD` = `79a43d0` |
| G2 — One taxonomy each for site/role/status, used everywhere | Not found | Brief already fixes the taxonomy (8 named sites, 2 roles, 5 lifecycle states) but no code exists to apply it consistently yet | gap | plan | absent |
| G3 — Licensed/unavailable assets flagged with a substitute | N/A | Not applicable on `codebase` — no design assets exist to license; Kendo UI's own license is tracked in charter §2/`engineering-standards.md` §0, not a design-asset concern | n/a | — | `engineering-standards.md` §0 |

### H. Platform decisions to name (even if "build decides")

| Requirement | Verdict | What is short | Severity | Routes to | Pointer |
|-------------|---------|----------------|----------|-----------|---------|
| H1 — Persistence + data contract named and wired | Not found (named, not wired) | Charter §2 names SQL Server `cane-db` via EF Core; nothing wired in code | blocker | plan | absent — charter.md §2 is the ratified source |
| H2 — Authentication / real identity named and wired | Not found (named, not wired) | Charter §2 names interim username/password with an Entra ID migration path; nothing wired | blocker | plan | absent — charter.md §2 is the ratified source |
| H3 — Server-side authorization wired | Not found (named, not wired) | Charter §2/constitution name role+site enforcement; nothing wired | gap | plan | absent |
| H4 — Pagination strategy wired | Not found (named, not wired) | Charter §2 names server-side paging with a clamped max; nothing wired | gap | plan | absent |
| H5 — Validation/error/logging posture wired | Not found (named, not wired) | Charter §2/`engineering-standards.md` name the posture; nothing wired | gap | plan | absent |
| H6 — Every integration marked real-bridge vs honest-stub | Not found | Brief R51–R57 name Databank/SCADA/Data Lakehouse integrations and their exclusions (external lab results, Factory Data); none implemented, so none yet marked | gap | plan | absent |

### I. Non-functional & operational flags

| Requirement | Verdict | What is short | Severity | Routes to | Pointer |
|-------------|---------|----------------|----------|-----------|---------|
| I1 — Perf/a11y/i18n/security/retention stated or explicitly "none for v1" | Not found (stated, not wired) | Charter §5 states all five explicitly (WCAG AA, English-only, no numeric perf SLA, security per constitution, ~2-season retention); nothing wired/verifiable in code yet | gap | plan | `charter.md` §5 is the ratified source |
| I2 — Deployment/environment expectations named | Not found (named, not wired) | Charter §2 names "same environments as the existing LIMS platform"; no deployment config exists yet | gap | plan | `charter.md` §2 is the ratified source |

## Can't be judged here

None. Unlike a design bundle, the `codebase` adapter can directly observe (or, when absent, correctly classify as N/A) every rubric item — there is nothing here that is structurally invisible to a repository the way visual chrome or a decisions log are.

## Withdrawn findings

None — this is the first audit of this release.

## What this review could not check

- Whether the *existing* LIMS platform (the one this release is meant to extend, per charter §1/§2) is actually compatible with the decisions recorded — that platform's source is not available to this project (confirmed with the operator), so nothing about its real schema, API surface, or auth wiring could be cross-checked. This is a standing risk to flag forward, not a gap in this audit's method.
- Whether Kendo UI v23 and .NET 10/Angular 21+ are mutually compatible in practice — no code exists yet to exercise that combination.

<details>
<summary>Provenance</summary>

Repository: this project, commit `79a43d0bd0b321005c8854d2a229aeee261a276f`, branch `release/v0.1.0`. Dossier inputs: `brief.md@66271df`, `charter.md@66271df`, `design-request.md@79a43d0`.

</details>
