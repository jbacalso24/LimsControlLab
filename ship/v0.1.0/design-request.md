# Design request — `v0.1.0`

- **Release:** v0.1.0  ·  **Written:** 2026-08-25
- **Delivery surface:** backend (codebase) — from charter §1 / `bundle.platform: codebase`
- **For:** the coding agent (Claude Code) — no external designer, no design bundle
- **Built from:** `ship/v0.1.0/brief.md@66271df` + `ship/v0.1.0/charter.md@66271df`
- **Return to:** n/a — audited in place at HEAD, nothing is returned or staged

> **There is no external designer and no design bundle for this release.** `/ship:audit` measures the
> repository at HEAD against this document via the `codebase` adapter. §7 "design these" is `n/a`, and
> §11 is `n/a` — no separate design-decisions phase exists to hand off to.
>
> **One clarification specific to this project:** "backend (codebase)" here means *no external design
> tool was used* (per the operator's explicit choice — Claude Design/Figma is skipped, the coding agent
> builds directly), **not** that there is no UI. The system does include a real Angular + Kendo UI
> (charter §2/§3), built in the same codebase as the .NET API and audited as one repository. The
> `codebase` adapter's own documentation notes it normally expects "no visual screens" on a backend
> release — that expectation does **not** hold here, since UI code will live in this same repo. This is
> flagged so `/ship:audit` doesn't mistake an empty/partial frontend for an out-of-scope concern, and so
> nobody downstream assumes this is a headless/API-only service.

## 1. What we're building and why
Wilmar Sugar Australia is introducing a fit-for-purpose LIMS Control Lab capability across 8 mill sites, replacing paper, spreadsheets, and manual Databank Shift Entry as the way analysts capture, calculate, validate, and manage laboratory results during cane crushing. It becomes the system of record for laboratory execution — readings, calculations, calibration, exceptions, and results — feeding validated data downstream to Databank, SCADA, and the Data Lakehouse, without introducing formal multi-step approval overhead for routine work.

## 2. Who uses it, and what they need to do

| User type | What they need to do | Volume / frequency |
|-----------|----------------------|--------------------|
| **Control Lab Analyst** (covers all local job titles — Control Analyst, Day Analyst, Juice Analyst, Boiler Operator, Chemical Attendant, Fugal Operator, Evaps Operator, Day Fibre Chemist, Control Chemist) | Perform scheduled, non-scheduled, and ad-hoc analyses across products (sugar, juice family, mud/filter, bagasse, pan products, boiler/feed water, ponds); capture manual and instrument readings; start/pause/resume/cancel their own analysis work; save and resume partially completed work; respond to their own out-of-tolerance exceptions (modify, retest, or accept with comment); search and compare historical results; transfer samples and continue analyses across sites. | 8 mill sites; hundreds of samples per day per site during crush, multiple tests per sample, concurrent users across shifts. |
| **Lab Coordinator** | Everything a Control Lab Analyst can do, plus: create/modify/retire analysis templates and validation rules/tolerances; review and accept or reject exception results with explanatory notes; unlock and amend a locked completed result (with mandatory justification and full audit); assign and reassign analysis work between users/roles; review shift reports. | One or more per site/shift. |

## 3. Behaviour the design must accommodate
All 78 requirements from `brief.md` §3 are in scope for implementation; below is the full set grouped by API/behavioural surface, in EARS form with source requirement IDs so the audit can trace back. General non-functional posture (responsiveness, availability, backup/DR, documentation — R65, R66, R70, R71) and abstract data-governance statements (R76–R78) are carried as posture (charter §5) rather than repeated per-surface here, since they apply system-wide rather than to one surface.

| Req | Behaviour | Surfaces it touches |
|-----|-----------|---------------------|
| R1–R7 | Configure analysis templates (tests, readings, calculations, validation rules/tolerances), reusable across sites/products/roles; site-specific variants; retire without affecting in-flight work; sampling-method definitions (snap/composite/combined/split/exchange); multi-analysis-per-sample and multi-sample-per-analysis support. | Analysis Template API |
| R8–R18 | Scheduled/non-scheduled/ad-hoc analyses; schedule configuration by site/product/type/shift/role; three fixed 8-hour shifts (08:00–16:00, 16:00–00:00, 00:00–08:00); complex recurrence/exclusion patterns; work-queue visibility (not started/in progress/completed); suspend schedules during shutdowns; assignment/reassignment with audit; overdue/delayed visibility and notification; ad-hoc substitution of methods/instruments. | Scheduling & Work Allocation API |
| R19–R25 | Sample/analysis lifecycle states (not started/in progress/on hold/completed/cancelled) with start/pause/resume/cancel; multi-shift progression across users; timestamped, attributed status changes; full audit trail; formal identifiers for Pan Products/Sugar/Mud/Bagasse samples; split/combine/traceability. | Sample & Analysis Lifecycle API |
| R26–R32 | Manual and instrument-based reading capture; instrument selection/association/secure transmission; manual fallback; default + runtime-alternate instrument assignment; multiple/duplicate/variant readings; save-and-resume partial work; test sequencing and time-based step constraints. | Data Capture & Instrumentation API |
| R33–R38 | Tolerance/limit validation; exception flagging; exception resolution (modify/retest/accept-with-comment); mandatory commentary on override; single-field, cross-field, and contextual validation; validation messages naming expected range, actual value, and reason. | Validation & Exceptions API |
| R39–R41 | Automatic calculation from readings (averages, weighted averages, composite results, calibration-based values); calibration curve/table CRUD, lookup, and auto-derivation; rules for how invalid/rejected results propagate. | Calculation & Calibration API |
| R42–R47 | Exception review queue for Lab Coordinators; accept/reject with notes; no mandatory approval on non-exception results; locking of completed analyses; Lab-Coordinator-only unlock/amend with mandatory justification, full audit, and optional reclassification flag. | Result Management (Review/Locking/Audit) API |
| R48–R50 | Historical search by product/analysis type/site/date/test/instrument/sample point; comparison across samples/tests/repeats/historical-vs-current; local retention of current + previous crush season, older data via Databank/Data Lakehouse. | Search & History API |
| R51–R57 | Integration with Databank, SCADA (IP21), and Data Lakehouse; only valid/complete data released downstream; integration-failure visibility and reprocessing; near-real-time/post-validation/batch transfer tiers; configurable alerting (Could); external lab results and Factory Data explicitly **excluded** from LIMS Control Lab (Databank direct, as today); C Molasses Exchange captured in LIMS but **not** transmitted to Databank this release (provisional). | Integration & Data Flow API |
| R58–R59 | Cross-site sample transfer; transferring/receiving sites can view/continue/access-prior-results; analysing site can edit. | Cross-Site & Sample Transfer API |
| R60–R64 | Work-view clarity (current shift/day, in progress, completed); efficient navigation between samples/analyses/tests/sites; rapid multi-reading entry; state/exception visibility; concurrent multi-sample/analysis work. | UI/UX (realized in the Angular frontend within this same codebase) |
| R72–R75 | Server-side RBAC by role (§2) and site; enterprise-standard authentication; configuration/validation-rule access restricted to Lab Coordinator; full auditability of access and changes. | Security / Auth API (cross-cutting) |

## 4. Data that appears on screen

| Data | Owned by | Editable here? | Notes |
|------|----------|----------------|-------|
| Control Lab analysis results (readings, calculations, calibration, exceptions) | LIMS Control Lab | Yes, by Control Lab Analyst / Lab Coordinator per §5 | System of record for lab execution |
| Validated enterprise/production data | Databank | No — read-only downstream consumer | LIMS Control Lab never receives writes back from Databank |
| Analysis templates, validation rules/tolerances, calibration tables | LIMS Control Lab | Yes, by Lab Coordinator only | — |
| External laboratory analysis results (e.g. Gateway Lab) associated with a Control Sample | Databank | Not editable here — **not captured in LIMS Control Lab at all** | Decided 2026-08-25; do not build a capture path for this |
| Factory Data from site SCADA/ProcessNet/Citect | Databank | Not editable here — **not captured in LIMS Control Lab at all** | Decided 2026-08-25; no change to today's manual Databank entry |
| "C Molasses Exchange" results | LIMS Control Lab | Yes | Provisional: not transmitted to Databank this release |
| SCADA display data (e.g. Pol results) | LIMS Control Lab (source) | No — derived/pushed, not directly edited | — |
| Historical lab data beyond current + previous crush season | Data Lakehouse (via Databank) | No | Batch feed only |

## 5. Roles — what each may do, and what each must NOT

| Role | Can | Cannot | What that means on screen |
|------|-----|--------|---------------------------|
| Control Lab Analyst | Perform scheduled/ad-hoc analyses; capture readings; start/pause/resume/cancel own work; save/resume partial work; respond to own exceptions; search/compare history; transfer samples between sites | Configure/retire templates or validation rules; unlock/amend a locked result; approve/reject someone else's exception; reassign work to others | Configuration and unlock/review actions are simply absent from this role's UI, not merely disabled — and absence is a UX decision, not the security boundary (enforcement is server-side, per charter §2) |
| Lab Coordinator | Everything an Analyst can, plus: configure/retire templates/rules; review/accept/reject exceptions with notes; unlock/amend locked results (justification required, fully audited); assign/reassign work | Bypass the audit trail; unlock/amend without a recorded justification | The unlock/amend flow must force a justification field before the action can complete — this is a business rule, not just a UI nicety |

**Enforcement is server-side** on every write (charter §2) — the Angular UI's role-based visibility is a usability convenience, never the actual permission boundary. This must be verifiable by inspecting the API layer directly (attributes/policy handlers), not inferred from what the UI shows or hides.

## 6. Privacy constraints the design must respect
No personal or customer data is involved (brief §6). The only privacy-relevant element is staff identity captured against every action for the audit trail (who entered/approved/unlocked what, and when), under role-based access control and enterprise-standard authentication. No masking, export-permission, or additional confirmation requirements beyond the unlock/amend justification (§5) are stated anywhere in the source.

## 7. Scope — what to design, and what not to
- **Design these:** `n/a — no external design surface; see §3 for the full in-scope behavioural requirement set the codebase (API + Angular UI) must implement.`
- **Do not design / build these:** Execution of one-off/non-routine external lab testing services (e.g. asbestos testing); full replacement of Databank's operational reporting; non-Control-Lab spreadsheet processes; capture of external laboratory analysis results in LIMS Control Lab (stays Databank-direct); capture of Factory Data (SCADA/ProcessNet/Citect) in LIMS Control Lab (stays Databank-direct); Databank's own downstream validation/SAP push; bioethanol laboratory equipment/analyses (different business unit). Future considerations (not decided against, just deferred): reagent/consumables tracking, external-certificate attachment/storage, extended in-LIMS reporting, equipment service reports/certifications.

## 8. Application archetype — what it makes non-negotiable
**Workflow** (charter §1). Every status field — sample status, analysis status, exception status, lock status — owes an explicit transition table: who can move it from which state to which, what triggers the move, and what gets audited on the way. The calculation/calibration engine (R39–R41) is real but secondary: it activates *in response to* lifecycle events (a reading captured, an analysis unlocked), it is not the organising concern. `/ship:audit` should expect and check for transition tables (bundle-spec S5) as the highest-value thing to verify here.

## 9. Platform decisions that shape the interface
- **Pagination:** server-side paged list/search endpoints (history search, R48/R49), page size clamped to a validated maximum.
- **Validation / error posture:** structured validation errors naming the expected range, the actual value entered, and the reason for failure (R38); a consistent error-response shape across the API.
- **Authentication:** interim local username/password login (no SSO redirect yet); designed for a later swap to Entra ID SSO without changing the authorization model underneath.
- **Authorization:** server-side, by role + site, on every write — never a UI-only check (constitution, §5 above).
- **Concurrency:** optimistic concurrency — a conflicting write is rejected with a clear conflict signal (HTTP 409), never silently overwritten. This has a real interface consequence: the UI must have a way to show the user their save was rejected due to a concurrent change and let them reload/retry.

## 10. Non-functional expectations
- **Devices:** desktop and mobile/tablet (brief R67) — no further viewport detail stated.
- **Accessibility:** WCAG AA on every user-facing screen (charter §5).
- **i18n:** none — English only (charter §5).
- **Performance:** no numeric SLA stated; qualitative "responsive under expected operational load" only, refined during build (charter §5).
- **Retention:** current + previous crush season locally; older data via Databank/Data Lakehouse (brief §6, R50).

## 11. Decisions this design must make
`n/a — no external design decisions phase.` This is a backend (codebase) release with no separate design-decisions handoff; any implementation-level decision not already settled by the brief/charter is `/ship:shape`'s to make (during Construction), or escalates to the human per `/ship:shape`'s own process — it does not get recorded here.

## 12. What to send back — the acceptance contract
Since nothing is commissioned or returned, this section states what `/ship:audit` will check against the repository at HEAD, per the `codebase` adapter and `bundle-spec.md`:

| # | Section | What the codebase audit checks |
|---|---------|-----------------------------------|
| S1 | Overview & IA | Module/project layout, solution manifest, README — reflects the archetype (§8) |
| S2 | Surfaces | The actual endpoint set (controllers/routes) **and** the Angular UI's screens/components — every §3 requirement should have a corresponding surface, or it's a GAP |
| S3 | Design system | n/a on a pure backend, but since a real UI lives in this repo: Kendo UI theming/tokens should exist once frontend work starts — absence early is expected (empty/greenfield), not a defect, until build begins |
| S4 | Data model | Entities, EF Core models, migrations — should reflect §4's data table and the roles/permissions model |
| S5 | Business rules & state machines | **Per lifecycle, a transition table in code** (service/domain logic) for sample status, analysis status, exception status, lock status — the single most important thing this audit checks, per §8 |
| S6 | Permissions & policy | Server-side role+site authorization attributes/policy handlers/middleware — must be directly readable in code, not inferred from the UI |
| S7 | Decisions log | Expected absent unless ADRs exist — decisions live in `brief.md`/`charter.md` instead; not a gap |
| S8 | Out-of-scope / deferred | Expected absent from code — already recorded in `brief.md` §7 and §7 above; not a gap |
| S9 | Platform decisions | Observed from actual wiring (DI config, `appsettings`, auth middleware) against charter §2 — e.g. does the persistence layer actually use EF Core against `cane-db`, is authorization actually server-side |
| S10 | Sample data | Seed/fixture data, if any, checked for reconciliation with stated volumes (brief §2) or labelled illustrative |
| S11 | Non-functional | Logging, health checks, middleware visible in code; WCAG AA and other stated targets (§10) not directly code-verifiable without running the app — the audit should note this limit rather than claim false confidence |
| S12 | Provenance | The git commit sha at audit time — exact, no separate manifest needed |

**This is a greenfield repository** — at first audit, the inventory will be empty and every requirement in §3 will report as a GAP ("not yet built"). That is the expected, honest starting state, not an adapter error (per `codebase.md`'s own guidance) — the resulting `coverage.md` **is** the build backlog `/ship:plan` sequences.

## 13. Audit remit — which requirements each audit owns
`n/a — all requirements are backend (codebase); one audit covers the entire repository, including the Angular UI code that lives within it.`
