# Charter — `v0.1.0`

- **Release:** v0.1.0  ·  **Written:** 2026-08-25
- **Built from:** `ship/v0.1.0/brief.md@a58f960` + `assumption-ledger.md@a58f960` + `question-ledger.md@a58f960`

## 1. Application archetype & build class
- **Archetype:** `workflow` — the brief's core is lifecycle state (not started/in progress/on hold/completed/cancelled), exceptions, locking/unlocking, and reassignment, not simple record CRUD or a pure calculation engine.
- **What it pulls forward:** every status field (sample status, analysis status, exception status, lock status) owes an explicit transition table — who can move it from which state to which, and what's audited on the way. The calculation/calibration engine (brief §6.6) is real but secondary: it is triggered *by* lifecycle events (a reading captured, an analysis unlocked), not the organising concern.
- **Delivery surface:** `backend` (codebase adapter — no external design bundle). Confirmed with the operator 2026-08-25: this is a full-stack build (Angular UI + .NET API), built directly by the coding agent with no separate Claude Design/Figma handoff. `bundle.platform` in `ship/config.md` is set to `codebase` to match.
- **Build class:** `production` — this is a real system going live across 8 mill sites, targeting 01 June 2027. The engineering standard set below is binding; a deviation requires an explicit, recorded exception (§3a).

## 2. Platform decisions *(named up front — specify OR "build decides")*

| Decision | Value / "build decides" / n/a | Decided by |
|----------|-------------------------------|------------|
| Persistence + data contract | SQL Server, the existing LIMS platform's shared database (`cane-db`), via EF Core migrations. Control Lab is a new schema/module within it, not a new database. | Judison Bacalso, 2026-08-25 — resolves question-ledger Q1/Q2 |
| Authentication / identity | Interim: local username/password (properly hashed, per constitution). Target: Entra ID (Azure AD) SSO, matching the existing LIMS platform's enterprise identity provider — migration path designed in via claims-based authorization from day one, not bolted on later. | Judison Bacalso, 2026-08-25 |
| Server-side authorization | Role-based (Control Lab Analyst / Lab Coordinator per brief §5) plus site segregation, enforced server-side on every write. The Angular UI's role checks are a convenience only. | Judison Bacalso, 2026-08-25 |
| Pagination strategy | Server-side paged list/search endpoints (history search, brief R48/R49), page size clamped to a validated maximum. | Judison Bacalso, 2026-08-25 |
| Validation / error / logging posture | Structured request validation with a consistent error-response shape naming the expected range, actual value, and reason for failure (brief R38); structured logging; every write captured in the audit trail (user, role, timestamp, before/after values — brief R23, R75). | Judison Bacalso, 2026-08-25 |
| Deployment / environments | Same Dev/Test/Prod environments and release pipeline as the existing LIMS platform (PaaS, Cloud-ANZ) — consistent with extending rather than decoupling. | Judison Bacalso, 2026-08-25 |
| Invariants & concurrency — what can two users do at once? | Optimistic concurrency: a concurrency token (rowversion) detects a clash between, e.g., an Analyst editing a result and a Lab Coordinator unlocking/amending it. The losing write gets a clear conflict (HTTP 409), never a silent overwrite. | Judison Bacalso, 2026-08-25 |
| Which values are denormalised/derived, and what recomputes them? | Calculated/derived results (weighted averages, composite results, calibration-adjusted values — brief R39/R40) recompute automatically whenever their underlying readings change, while the parent analysis is unlocked. Once locked (brief R45–R47), they freeze; a Lab Coordinator's unlock triggers recompute. | Judison Bacalso, 2026-08-25 |

### 2a. Pre-build gate

| Gate item | In place? | If not, when? |
|-----------|-----------|----------------|
| Backend analyzers + warnings-as-errors + `.editorconfig` | Not yet | Before the first vertical is built (confirmed 2026-08-25) |
| Frontend lint + typecheck gate (Angular ESLint + strict TypeScript) | Not yet | Before the first vertical is built (confirmed 2026-08-25) |
| Coverage collection + floor | Not yet | Before the first vertical is built; starting floor **60% line coverage** (backend and frontend), ratcheted up over time — set by charter as a reasonable starting point, no prior project standard to inherit from |

## 3. Reference architecture & standards
- **Standard checked against:** none — no `ship/recipes/engineering-standards.md` existed for this project prior to this charter. This charter's decisions, drafted into `ship/recipes/engineering-standards.md`, **become the baseline** for this and future releases of this project.
- **Frontend UI library:** Kendo UI for Angular, v23, licensed (`docs/telerik-license.txt` — a Telerik license key confirmed to cover the `KENDOUIANGULAR` product; gitignored, never committed, supplied to build/CI as a secret). Confirmed by the operator 2026-08-25.

### 3a. Deviations from the standard
*(none — this release establishes the standard rather than deviating from one)*

| # | Requirement asks for | Standard requires | Chosen | Why | Decided by |
|---|---------------------|-------------------|--------|-----|------------|
| — | n/a | n/a | n/a | n/a | n/a |

## 4. Constitution
Drafted fresh at `ship/recipes/constitution.md` (no prior constitution existed). Inviolables, summarised:
- **Security:** all writes authorized server-side (role + site); only a Lab Coordinator may unlock/amend a locked result, always with justification and full audit; no committed secrets; interim credentials properly hashed; every consequential action audited and unbypassable; no direct writes to `cane-db` outside the LIMS Control Lab API.
- **Architecture:** one-way layering (Angular → API → Service → Repository → SQL Server); LIMS Control Lab is system of record for lab execution, Databank for validated enterprise data, one-way LIMS→Databank only; role/permission model defined once, server-side; derived values recompute-until-locked; concurrency tokens on every concurrently-edited entity; external lab results and Factory Data stay out of LIMS Control Lab per the brief.
- **Off-limits:** no force-push/skipped CI on `release/*`; no plaintext credentials; no bypassing the exception/unlock workflow via direct database edits; no stack substitution without a recorded charter amendment.
- **Guidance:** configure new analyses via the template system rather than hardcoding; migrate off the interim auth once Entra ID is available; don't infer ambiguous per-site method/tolerance detail from the source spreadsheet; treat C Molasses Exchange's Databank exclusion as provisional.

## 5. Non-functional posture
- **Performance:** no numeric SLA — the brief itself only states a qualitative expectation ("responsive under expected operational load," BRD §6.12) and explicitly defers detail to solution design. Confirmed 2026-08-25 to carry that forward as-is rather than inventing a number now.
- **Accessibility:** WCAG AA required on every user-facing screen, confirmed 2026-08-25 — checked via automated a11y linting in CI (per `engineering-standards.md`).
- **Internationalisation:** none for this release — English only (Wilmar Sugar's Australian mill sites), confirmed 2026-08-25.
- **Security:** per the constitution (§4 above) — server-side RBAC by role and site, full audit trail, no PII beyond staff identity in the audit log (brief §6).
- **Retention:** current + previous operational crush season retained locally; older data accessible via Databank/Data Lakehouse (brief R50, §6) — no separate export/download rule beyond the existing downstream integration paths.

## 6. Confirmation gate
- **Human confirms** the platform decisions (§2) and the constitution (§4) before design begins.
- **Question ledger:** both rows from `ship/v0.1.0/question-ledger.md` are resolved by this charter —
  - **Q1** (existing-platform facts: PaaS, Cloud-ANZ, shared infra, SQL Server, REST/OPC) → resolved into §2 (Persistence, Deployment) above.
  - **Q2** (build within existing LIMS vs. decouple) → resolved: extend the existing platform (§1 delivery surface, §2 Persistence).
  `question-ledger.md` is updated to `resolved` for both rows, closing DEFINITION's exit gate with no `open` rows remaining.
