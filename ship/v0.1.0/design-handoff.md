# Design handoff — `v0.1.0`

- **Release:** v0.1.0  ·  **Written:** 2026-08-25  ·  **Delivery surface:** backend (codebase)
- **Built from** *(pointers, commit-pinned — not copied):*
  `brief.md@66271df` + `charter.md@66271df` + `design-request.md@79a43d0` +
  `audits/codebase-2026-08-25/@3990b17` + `decisions-log.md@d285764`

> **To Construction:** this is the frozen, decided state of the design. `/ship:plan` sequences from it;
> `/ship:build`, `/ship:accept` and `/ship:docs` read it as the record of what this release is. **No
> clarify decision changed anything here** — zero rows were routed to clarify, so the decided state below
> is identical to `design-request.md`'s stated behaviour. See §4 for the audit-verdict note.

## 1. What we're building — decided behaviour

| Behaviour | Decided state (post-clarify) | Traces to | Pointer / decision # |
|-----------|------------------------------|-----------|----------------------|
| Analysis Template configuration | Lab Coordinator configures/retires templates (tests, readings, calculations, validation rules/tolerances), reusable across sites/products/roles, with site-specific variants; retiring a template never affects in-flight analyses | brief R1–R7 | `design-request.md` §3 |
| Scheduling & Work Allocation | Scheduled/non-scheduled/ad-hoc analyses; three fixed 8-hour shifts (08:00–16:00, 16:00–00:00, 00:00–08:00); complex recurrence/exclusion patterns; work-queue visibility; suspend during shutdowns; assignment/reassignment with audit; overdue/delayed visibility | brief R8–R18 | `design-request.md` §3 |
| Sample & Analysis Lifecycle | States (not started/in progress/on hold/completed/cancelled) with start/pause/resume/cancel; multi-shift progression; timestamped attributed status changes; full audit trail; formal identifiers for Pan Products/Sugar/Mud/Bagasse samples; split/combine/traceability | brief R19–R25 | `design-request.md` §3 |
| Data Capture & Instrumentation | Manual + instrument reading capture; instrument selection/association/secure transmission; manual fallback; default + runtime-alternate instrument assignment; save-and-resume partial work; test sequencing and time-based step constraints | brief R26–R32 | `design-request.md` §3 |
| Validation & Exceptions | Tolerance/limit validation; exception flagging; exception resolution (modify/retest/accept-with-comment); mandatory commentary on override; validation messages naming expected range/actual value/reason | brief R33–R38 | `design-request.md` §3 |
| Calculation & Calibration | Automatic calculation from readings; calibration curve/table CRUD, lookup, auto-derivation; rules for invalid/rejected-result propagation; derived values auto-recompute while unlocked, freeze once locked | brief R39–R41; charter §2 (concurrency/derived-values decision) | `design-request.md` §3, §9 |
| Result Management (Review/Locking/Audit) | Exception review queue for Lab Coordinators; accept/reject with notes; no mandatory approval on non-exception results; locking of completed analyses; Lab-Coordinator-only unlock/amend with mandatory justification, full audit, optional reclassification | brief R42–R47 | `design-request.md` §3, §5 |
| Search & History | Historical search by product/analysis type/site/date/test/instrument/sample point; comparison across samples/tests/repeats/historical-vs-current; local retention of current + previous crush season | brief R48–R50 | `design-request.md` §3 |
| Integration & Data Flow | Integration with Databank, SCADA (IP21), Data Lakehouse; only valid/complete data released downstream; near-real-time/post-validation/batch transfer tiers; **external lab results and Factory Data explicitly excluded from LIMS Control Lab**; C Molasses Exchange captured in LIMS but not transmitted to Databank this release (provisional) | brief R51–R57 | `design-request.md` §3, §4 |
| Cross-Site & Sample Transfer | Cross-site sample transfer; transferring/receiving sites can view/continue/access-prior-results; analysing site can edit | brief R58–R59 | `design-request.md` §3 |
| UI/UX (Angular + Kendo UI) | Work-view clarity, efficient navigation, rapid multi-reading entry, state/exception visibility, concurrent multi-sample work — realized in the Angular frontend within the same codebase as the .NET API (no external design bundle) | brief R60–R64 | `design-request.md` §3, note |
| Security / Auth (cross-cutting) | Server-side RBAC by role (Control Lab Analyst / Lab Coordinator) and site on every write; interim username/password auth (properly hashed) with a designed Entra ID SSO migration path; full auditability of access and changes | brief R72–R75; charter §2 | `design-request.md` §3, §9 |

## 2. Work to do — unsequenced

Every row below is a `routes-to: plan` finding from `audits/codebase-2026-08-25/coverage.md` — objective build work, not a decision (zero rows this cycle routed to clarify). There are no can't-be-judged-here criteria to add: the `codebase` adapter found none (see that audit's coverage.md).

| # | Item | Kind | From | Acceptance criteria (EARS) |
|---|------|------|------|----------------------------|
| W1 | Solution/project scaffolding & documented layout | gap | coverage A1 | WHEN the repository is inspected, the system SHALL present a documented solution/project layout (README, module structure) reflecting the workflow archetype (charter §1). |
| W2 | Every in-scope requirement has a real surface | gap | coverage A2 | WHEN any brief §3 requirement (R1–R64, R72–R78) is exercised, the system SHALL provide the corresponding API endpoint or UI surface implementing it. |
| W3 | Empty/loading/error/role-variant UI states | gap | coverage A3 | WHEN a Control Lab Analyst or Lab Coordinator views any list/detail screen, the system SHALL render empty, loading, error, and role-appropriate states, not only the populated happy path (brief R60–R64). |
| W4 | Business rules enforced at the point of action | gap | coverage B3 | WHEN a user-facing action is implemented, the system SHALL enforce its governing business rule (e.g. exception override requiring comment, R36) at the point of that action, not merely display it. |
| W5 | Stub/fidelity labelling convention during build | gap | coverage C1 | WHEN a surface or control is not yet wired to real behaviour during build, the system SHALL label it stub/mock-data/not-in-scope so partial progress is never presented as finished. |
| W6 | Seed/sample data flagged as illustrative | gap | coverage C2 | WHEN seed or sample data is used in a non-production environment, the system SHALL flag it as illustrative, distinguishing it from real captured results. |
| W7 | Lifecycle state machines (sample/analysis/exception/lock) | gap | coverage D1 | WHEN a sample, analysis, exception, or lock transitions state, the system SHALL enforce only the transitions defined in brief R19–R25, R33–R38, and R42–R47, each timestamped and attributed to a user and role (R22). |
| W8 | Derived-value recompute rule | gap | coverage D2 | WHEN a source reading changes while its parent analysis is unlocked, the system SHALL automatically recompute all dependent calculated/derived values (brief R39–R41); WHEN the analysis is locked, recompute SHALL NOT occur until a Lab Coordinator unlocks it (R57; charter §2). |
| W9 | Audit trail on every change | gap | coverage D3 | WHEN any data change or status update occurs, the system SHALL record it in the audit trail with date, time, user, role, and values changed (brief R22, R23, R75). |
| W10 | Cascade rules on unlock/reject | gap | coverage D4 | WHEN a result is unlocked and amended, the system SHALL cascade recompute to its dependent derived values and flag it for optional reclassification (brief R41, R46). |
| W11 | Auto vs manual transition distinction | gap | coverage D5 | WHILE an analysis is unlocked, derived values SHALL auto-recompute without user action (brief R39); sample/analysis lifecycle transitions (start/pause/resume/cancel) SHALL require an explicit authorised-user action and SHALL NOT auto-advance (brief R20). |
| W12 | Role model expressed in code | gap | coverage E1 | WHEN the system authenticates a user, the system SHALL assign them exactly one of the two ratified roles — Control Lab Analyst or Lab Coordinator (brief §5) — with no undefined role state. |
| W13 | Permission edge cases enforced | gap | coverage E2 | IF a Control Lab Analyst attempts to configure a template, unlock a result, or act outside their assigned site THEN the system SHALL deny the action server-side (brief §5; charter §2). |
| W14 | Server-side authorization (not UI-only) | gap | coverage E3 | WHEN any write request reaches the API, the system SHALL verify role and site authorization server-side before executing it, independent of what the Angular UI displays or hides (constitution; charter §2). |
| W15 | Data model & migrations | gap | coverage F1 | WHEN the database schema is created, the system SHALL implement the entities and relationships in brief §4 via EF Core migrations against the shared `cane-db`. |
| W16 | Normalisation posture applied | gap | coverage F2 | WHEN a value is both storable and derivable, the system SHALL follow `engineering-standards.md` §3's aggregate-on-read / don't-denormalise posture unless a documented exception states otherwise. |
| W17 | Seed data reconciles with declared counts | gap | coverage F3 | WHEN seed/fixture data is provided for testing, the system SHALL ensure declared record counts reconcile with the rows actually seeded. |
| W18 | One canonical taxonomy (sites/roles/statuses) | gap | coverage G2 | WHEN a site, role, or lifecycle status is referenced anywhere in the system, the system SHALL use exactly one canonical taxonomy (the 8 named sites, 2 roles, 5 lifecycle states) — never a second parallel naming. |
| W19 | Persistence wired to `cane-db` | gap | coverage H1 | WHEN the API persists data, the system SHALL do so via EF Core migrations against the shared SQL Server `cane-db` (charter §2) — no ad hoc DDL, no alternate store. |
| W20 | Authentication wired (interim + SSO path) | gap | coverage H2 | WHEN a user logs in, the system SHALL authenticate via the interim username/password mechanism (properly hashed) with a claims-based authorization model designed for a later swap to Entra ID SSO (charter §2). |
| W21 | Server-side authorization wired | gap | coverage H3 | WHEN any endpoint is called, the system SHALL check the caller's role+site claims server-side before processing (charter §2). |
| W22 | Server-side pagination wired | gap | coverage H4 | WHEN a history/search endpoint (brief R48/R49) returns a list, the system SHALL page it server-side with a clamped maximum page size. |
| W23 | Validation/error/logging posture wired | gap | coverage H5 | WHEN a request fails validation, the system SHALL return a structured error naming the expected range, actual value, and reason (brief R38); WHEN any request completes, the system SHALL emit one structured log line. |
| W24 | Integrations marked real-bridge vs honest-stub | gap | coverage H6 | WHEN the system integrates with Databank, SCADA/IP21, or the Data Lakehouse, the system SHALL mark each integration as a real bridge or an honest stub, and SHALL exclude external lab results and Factory Data from LIMS Control Lab entirely (brief R56). |
| W25 | Non-functional targets wired (WCAG AA, i18n) | gap | coverage I1 | WHEN any user-facing screen renders, the system SHALL meet WCAG AA (charter §5); the system SHALL support English only for this release. |
| W26 | Deployment aligned to existing platform | gap | coverage I2 | WHEN the release is deployed, the system SHALL use the same Dev/Test/Prod environments and pipeline as the existing LIMS platform (charter §2). |

## 3. Out of scope this release

No clarify row was deferred this cycle (zero rows were routed to clarify at all). Out-of-scope items below trace to `brief.md` §7 directly.

| Item | Why out | Target release |
|------|---------|----------------|
| External laboratory analysis results (e.g. Gateway Lab) captured in LIMS Control Lab | Decided at intake 2026-08-25 — stays Databank-direct, per the business's own operational note (brief R56) | Not planned |
| Factory Data (SCADA/ProcessNet/Citect) captured in LIMS Control Lab | Decided at intake 2026-08-25 — stays as direct manual Databank entry, as today | Not planned |
| Execution of one-off/non-routine external lab testing services (e.g. asbestos testing) | Brief §7, explicit BRD exclusion | Not planned |
| Full replacement of Databank's operational reporting | Brief §7, explicit BRD exclusion | Not planned |
| Non-Control-Lab spreadsheet-based processes | Brief §7, explicit BRD exclusion | Not planned |
| Databank's own downstream validation / SAP push | Brief §7 — Databank's internal concern, not a Control Lab LIMS requirement | Not planned |
| Bioethanol laboratory equipment/analyses (site codes YPP/Yarraville, SAR/Sarina) | Assumption-ledger A2 — different business unit entirely | Not planned |
| Reagent and consumables tracking | Brief §7 — BRD "Future Considerations," not decided against | vNext |
| Attachment/storage of external-lab certificates or reports | Brief §7 — BRD "Future Considerations" | vNext |
| Extended in-LIMS reporting capabilities | Brief §7 — BRD "Future Considerations" | vNext |
| Equipment service reports and certifications | Brief §7 — BRD "Future Considerations" | vNext |
| C Molasses Exchange results transmitted to Databank | Decided at intake 2026-08-25 — LIMS-only for now, provisional pending Databank/business confirmation (brief R57) | Revisit next release |

## 4. Provenance

- **Audit folder:** `audits/codebase-2026-08-25/` — verdict **NOT HANDOFF-READY**.
- **Decisions folded in:** 0 from `decisions-log.md` (the log holds 0 entries — nothing was routed to clarify).
- **Deferred to a later release:** 0 clarify rows deferred; 5 brief §7 items carried forward as vNext (table above).
- **Reconciliation note:** There is no audit-vs-clarify delta to reconcile — clarify made zero decisions, so §1 above is identical to `design-request.md`'s stated behaviour. The **NOT HANDOFF-READY** verdict is not being silently overridden: it reflects a genuinely empty repository (6 blocker + 20 gap findings, all "not yet built," 0 ambiguities). This is the expected state named explicitly in `phases.md`'s own audit-stage guidance for a backend release — **"the gaps are the work"** — and confirmed with the operator (2026-08-25) before running audit/clarify: no existing LIMS platform codebase is available to this project, so the first audit of a from-scratch backend release cannot mechanically reach HANDOFF-READY on code that doesn't exist yet. §2 above **is** that gap list, carried forward unsequenced for `/ship:plan` to sequence as this release's initial build plan.
