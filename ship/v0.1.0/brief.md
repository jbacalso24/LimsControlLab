# Brief — `v0.1.0`

- **Written:** `2026-08-25`  ·  **Input classification:** `sufficient`  ·  **Assumptions:** `assumption-ledger.md` (2 entries, 0 need a human call)  ·  **Open items:** `question-ledger.md` (2 open)
- **Evidence:** `docs/UPDATED BRD Control  Lab.docx` (Business Requirements Document v0.2), `docs/HLD - LIMS Control Lab DRAFT.docx` (High Level Design, incl. embedded architecture/process diagrams), `docs/Samples required from each site_Updated2024.xlsx` (operational working data — equipment inventory, per-site analysis schedules, prior stakeholder Q&A), plus a guided intake interview held 2026-08-25 to resolve contradictions and gaps the sources left open.

> Wilmar Sugar Australia is introducing a fit-for-purpose Laboratory Information Management System (LIMS) capability for **Control Lab** operations across its 8 mill sites (Inkerman, Invicta, Kalamia, Victoria, Macknade, Proserpine, Plane Creek, Pioneer), to replace paper, spreadsheets, and manual Databank Shift Entry as the way Control Lab analysts capture, calculate, validate, and manage laboratory results during cane crushing. The input was **sufficient** — the BRD already states ~80 testable requirements and the HLD covers the integration architecture — so this brief is largely transcription into testable form, plus resolution of contradictions the sources raised themselves (an operational note excluding external lab results from LIMS scope, conflicting shift-length assumptions, an unresolved product-to-Databank routing question) via a guided interview with the operator. Indicative go-live is 01 June 2027, aligned with the separate Databank Re-write and Data Lakehouse initiatives; this brief does not depend on that date.

## 1. Problem & goal

**Current process.** Control Lab analysts test samples (juice, mud, bagasse, boiler/feed water, pan products, sugar, molasses) using paper records and site-managed spreadsheets. Selected final results are then re-keyed into Databank via Shift Data Entry screens. Databank was built as the enterprise repository for validated production data — it does not capture analytical steps, method detail, raw readings, or intermediate calculation values, and its shift-based data model restricts recording multiple results for the same analysis type within a single shift.

**What this costs today:**
- Data capture is duplicated across paper, spreadsheets, and Databank re-entry, with no single system-based record linking raw readings, intermediate values, final results, and analyst activity.
- Test methods, calculations, and validation rules live outside any system, so how an analysis is performed varies by site and by individual practice.
- Databank's Shift Entry UI is complex for new and seasonal staff, increasing onboarding time and error/data-loss risk.
- Data completeness and timeliness vary by analysis, site, and local practice, delaying downstream visibility.
- The organisation is dependent on key individuals and local tools, limiting its ability to standardise or automate.

**Why now.** A parallel Databank Re-write initiative is modernising the enterprise data platform but explicitly excludes laboratory execution from its scope — Databank is intended only to consume validated Control Lab results going forward. A Data Lakehouse initiative is also in demand phase and expects Control Lab data as an input. Both depend on Control Lab having a proper system of record first. A sibling LIMS already supports Juice Lab (Juice Payment) analysis and has run in production for ~2 years, demonstrating the approach is feasible for this factory environment — Control Lab is a materially different, more operationally flexible process (ad-hoc and operations-initiated analysis at any time, not the tightly controlled audit/payment model Juice Lab follows).

**What changes when it ships.** Control Lab becomes a proper system of record for laboratory execution: readings, calculations, calibration, exceptions, and results are captured once, validated, and made available in near-real-time or batch to Databank, SCADA, and the Data Lakehouse — without introducing formal multi-step approval overhead for routine work.

## 2. Users & their tasks

Two user types cover every distinct system permission evidenced in the requirements. The source working data uses many local job titles (Control Analyst, Day Analyst, Juice Analyst, Boiler Operator, Chemical Attendant, Fugal Operator, Evaps Operator, Day Fibre Chemist, Control Chemist) that vary by site — per the BRD's own requirements text, these are rostering/assignment distinctions, not different system permissions, so they are modelled as one role below. "Engineers" and "External party," which appear incidentally in the source working data, are not named anywhere in the BRD's actual requirements as having a task or permission, so no separate user type is defined for them in this release.

| User type | What they need to do | Volume / frequency |
|-----------|----------------------|--------------------|
| **Control Lab Analyst** (covers all local job titles — Control Analyst, Day Analyst, Juice Analyst, Boiler Operator, Chemical Attendant, Fugal Operator, Evaps Operator, Day Fibre Chemist, Control Chemist) | Perform scheduled, non-scheduled, and ad-hoc analyses across products (sugar, juice family, mud/filter, bagasse, pan products, boiler/feed water, ponds); capture manual and instrument readings; start/pause/resume/cancel their own analysis work; save and resume partially completed work; respond to their own out-of-tolerance exceptions (modify, retest, or accept with comment); search and compare historical results; transfer samples and continue analyses across sites. | 8 mill sites; hundreds of samples per day per site during crush, multiple tests per sample, concurrent users across shifts (indicative planning assumption, BRD §6.16 — no precise headcount is stated anywhere in the source). |
| **Lab Coordinator** | Everything a Control Lab Analyst can do, plus: create/modify/retire analysis templates and validation rules/tolerances; review and accept or reject exception results with explanatory notes; unlock and amend a locked completed result (with mandatory justification and full audit); assign and reassign analysis work between users/roles; review shift reports. | One or more per site/shift, per the BRD's as-is process diagram (Figure 1), which shows this as a distinct reviewing function separate from the analyst who captures readings. |

## 3. Requirements — testable acceptance criteria (EARS)

Requirements are grouped by the BRD's own categories (§6.1–§6.15) for traceability. All are `stated: BRD §6.<n>` unless marked otherwise. Priority (Must/Should/Could) is carried from the BRD. Per the HLD's own stated assumption ("the detailed structure of individual analyses, data fields, and formulas will be finalised during solution design"), the specific per-site test methods, tolerances, and exact task ownership visible in the source working spreadsheet (e.g. method-name variants like BSES vs NIRA, which named individual performs Feed Water testing) are configuration data for the Lab Coordinator to set up via R1–R4 below, not top-level acceptance criteria in this brief.

### Analysis Configuration & Templates

| # | Acceptance criterion (EARS) | User (§2) | Source | Assumption? |
|---|---|---|---|---|
| R1 | WHEN a Lab Coordinator defines an analysis template, the system SHALL allow configuration of: the tests required, the readings captured per test, the calculations derived from those readings, and validation rules/tolerances where applicable. (Must) | Lab Coordinator | stated: BRD §6.1 | |
| R2 | Analysis templates SHALL be reusable across sites, products/analysis types, roles, and scheduled/ad-hoc analyses. (Must) | Lab Coordinator | stated: BRD §6.1 | |
| R3 | WHEN operational requirements change, a Lab Coordinator SHALL be able to create, modify, and retire analysis templates. (Must) | Lab Coordinator | stated: BRD §6.1 | |
| R4 | The system SHALL support site-specific variations of an analysis template. (Must) | Lab Coordinator | stated: BRD §6.1 | |
| R5 | WHEN an analysis template is changed, the system SHALL NOT affect analyses already in progress or completed. (Must) | Lab Coordinator | stated: BRD §6.1 | |
| R6 | The system SHALL support definitions of sampling methods, including single/snap, composite, combined, split, and exchange samples between sites (e.g. molasses exchange). (Must) | Control Lab Analyst | stated: BRD §6.1 | |
| R7 | The system SHALL support multiple analyses per sample, multiple samples contributing to a single analysis, and reuse of analysis definitions across products. (Must) | Control Lab Analyst | stated: BRD §6.1 | |

### Scheduling & Work Allocation

| # | Acceptance criterion (EARS) | User (§2) | Source | Assumption? |
|---|---|---|---|---|
| R8 | The system SHALL support scheduled, non-scheduled, and ad-hoc analysis requests. (Must) | Control Lab Analyst | stated: BRD §6.2 | |
| R9 | WHEN a Lab Coordinator defines a schedule, the system SHALL allow configuration by site, product, analysis type, shift/day/week or other defined period, and role. (Must) | Lab Coordinator | stated: BRD §6.2 | |
| R10 | WHEN a schedule is defined by shift, the system SHALL use three 8-hour shifts (08:00–16:00, 16:00–00:00, 00:00–08:00), aligned to Databank's existing shift model. (Must) | Lab Coordinator | stated: intake interview decision, 2026-08-25 — resolves a shift-definition inconsistency left open in the source working data (competing Day/Afternoon/Night clock times vs. Databank's fixed tri-shift model) | |
| R11 | The system SHALL support complex scheduling patterns: recurring intervals within a shift, multi-day recurrence, shift-based rules, and exclusion rules. (Must) | Lab Coordinator | stated: BRD §6.2 | |
| R12 | A Control Lab Analyst SHALL be able to view work relevant to their role and site: not started, in progress, completed. (Must) | Control Lab Analyst | stated: BRD §6.2 | |
| R13 | WHILE a factory shutdown or extended stoppage is in effect, the system SHOULD support temporary suspension of scheduled analyses without deleting the schedule. (Should) | Lab Coordinator | stated: BRD §6.2 | |
| R14 | Scheduled analysis SHALL generally be available to any suitably authorised analyst; WHERE required, a Lab Coordinator MAY assign a task to a specific individual, and all assignment changes SHALL be audited. (Must) | Both | stated: BRD §6.2 | |
| R15 | The system SHALL support reassignment of analysis work between roles or users (e.g. shift change, workload balancing). (Must) | Lab Coordinator | stated: BRD §6.2 | |
| R16 | The system SHALL provide visibility of overdue, delayed, or missed analyses. (Must) | Both | stated: BRD §6.2 | |
| R17 | The system SHOULD provide notifications for upcoming or delayed/overdue analyses. (Should) | Both | stated: BRD §6.2 | |
| R18 | The system SHALL support temporary or ad-hoc modification of analysis execution: substitution of methods/instruments, temporary changes to steps, and short-term/one-off requests. (Must) | Control Lab Analyst | stated: BRD §6.2 | |

### Sample & Analysis Lifecycle Management

| # | Acceptance criterion (EARS) | User (§2) | Source | Assumption? |
|---|---|---|---|---|
| R19 | The system SHALL track the lifecycle status of samples and analyses: not started, in progress, on hold, completed, cancelled. (Must) | Control Lab Analyst | stated: BRD §6.3 | |
| R20 | A Control Lab Analyst SHALL be able to start, pause/hold, resume, and cancel an analysis. (Must) | Control Lab Analyst | stated: BRD §6.3 | |
| R21 | An analysis or test MAY progress over multiple shifts, with different authorised users entering or updating results at different points in time. (Must) | Control Lab Analyst | stated: BRD §6.3 | |
| R22 | Status changes SHALL be timestamped and attributed to a user and role. (Must) | Both | stated: BRD §6.3 | |
| R23 | The system SHALL maintain a complete audit trail of all data changes and status updates (date, time, user, values changed). (Must) | Both | stated: BRD §6.3 | |
| R24 | The system SHALL support association of samples with identifiers (e.g. container, batch, or collection reference). Pan Products, Sugar, Mud, and Bagasse samples SHALL have a formal identifier/numbering scheme, replacing today's ad hoc labelling. (Must) | Control Lab Analyst | stated: BRD §6.3, applicability to these sample types confirmed via intake interview 2026-08-25 | |
| R25 | The system SHALL support splitting, combining, and traceability of samples and associated results. (Must) | Control Lab Analyst | stated: BRD §6.3 | |

### Data Capture & Instrumentation Interaction

| # | Acceptance criterion (EARS) | User (§2) | Source | Assumption? |
|---|---|---|---|---|
| R26 | The system SHALL support capture of manual data entry and instrument-generated readings. (Must) | Control Lab Analyst | stated: BRD §6.4 | |
| R27 | The system SHALL support interaction with laboratory instruments: selection/re-selection of instruments, association of readings to the correct test, and secure/reliable transmission of data. (Must) | Control Lab Analyst | stated: BRD §6.4 | |
| R28 | WHERE instrument integration is unavailable, the system SHALL allow manual entry. (Must) | Control Lab Analyst | stated: BRD §6.4 | |
| R29 | The system SHALL support default instrument assignment per test, user selection of an alternate instrument at runtime, on-demand retrieval of readings, and association of readings with the selected instrument. (Must) | Control Lab Analyst | stated: BRD §6.4 | |
| R30 | The system SHALL support multiple readings for a single test, duplicate/repeat tests, and test variations (e.g. temperature, time, chemical variations). (Must) | Control Lab Analyst | stated: BRD §6.4 | |
| R31 | A Control Lab Analyst SHALL be able to save partially completed work and resume/update it later. (Must) | Control Lab Analyst | stated: BRD §6.4 | |
| R32 | The system SHALL support sequencing rules where dependencies exist between tests (e.g. Brix recorded before Temp) and time-based constraints between analysis steps (e.g. oven time). (Must) | Control Lab Analyst | stated: BRD §6.4 | |

### Validation, Tolerances & Exceptions

| # | Acceptance criterion (EARS) | User (§2) | Source | Assumption? |
|---|---|---|---|---|
| R33 | The system SHALL validate entered readings and calculated results against predefined tolerances or limits where applicable. (Must) | Control Lab Analyst | stated: BRD §6.5 | |
| R34 | Results outside of tolerance SHALL be clearly flagged as exceptions. (Must) | Control Lab Analyst | stated: BRD §6.5 | |
| R35 | WHEN an exception occurs, the system SHALL allow the Control Lab Analyst to modify data, perform a retest, or accept the result with comments. (Must) | Control Lab Analyst | stated: BRD §6.5 | |
| R36 | The system SHALL require commentary WHEN a user overrides or accepts an exception. (Must) | Control Lab Analyst | stated: BRD §6.5 | |
| R37 | Validation rules SHALL support single-field tolerances, cross-field validation (e.g. relationships between readings), and contextual validation based on analysis type. (Must) | Lab Coordinator | stated: BRD §6.5 | |
| R38 | Validation messages SHALL clearly indicate the expected range/rule, the actual value entered, and the reason for failure. (Must) | Control Lab Analyst | stated: BRD §6.5 | |

### Calculations & Calibration

| # | Acceptance criterion (EARS) | User (§2) | Source | Assumption? |
|---|---|---|---|---|
| R39 | The system SHALL automatically calculate results from captured readings using defined formulas, including averages, weighted averages, composite results, and calibration-based values. (Must) | Control Lab Analyst | stated: BRD §6.6 | |
| R40 | The system SHALL support creation, maintenance, and application of calibration curves/tables, including lookup of values and automatic derivation of calibrated values from entered readings. (Must) | Lab Coordinator | stated: BRD §6.6 | |
| R41 | The system SHALL support defined rules governing how invalid or rejected results impact related data. (Must) | Control Lab Analyst | stated: BRD §6.6 | |

### Result Management (Review, Locking & Audit)

| # | Acceptance criterion (EARS) | User (§2) | Source | Assumption? |
|---|---|---|---|---|
| R42 | The system SHALL provide visibility of exception results for review by a Lab Coordinator. (Must) | Lab Coordinator | stated: BRD §6.7 | |
| R43 | A Lab Coordinator SHALL be able to accept or reject an exception result and provide explanatory notes. (Must) | Lab Coordinator | stated: BRD §6.7 | |
| R44 | The system SHALL NOT require mandatory approval for all results — only exception results require Lab Coordinator review. (Must) | Lab Coordinator | stated: BRD §6.7, confirmed via source Q&A ("Exception report only") | |
| R45 | The system SHOULD support locking of completed analyses to prevent further modification. (Should) | Both | stated: BRD §6.7 | |
| R46 | WHERE necessary, a Lab Coordinator MAY unlock and amend a completed/locked result, with mandatory justification, a full audit trail of the change, and an optional reclassification flag for reporting. (Must) | Lab Coordinator | stated: BRD §6.7 | |
| R47 | Unlocking a result SHALL be restricted to a Lab Coordinator, SHALL require justification, and SHALL be fully auditable. (Must) | Lab Coordinator | stated: BRD §6.7 | |

### Search, Comparisons and History

| # | Acceptance criterion (EARS) | User (§2) | Source | Assumption? |
|---|---|---|---|---|
| R48 | Users SHALL be able to search historical analysis results by product, analysis type, site, date/time, test, instrument, and sample point. (Must) | Both | stated: BRD §6.8 | |
| R49 | The system SHALL support comparison of results across samples, across tests, repeat/duplicate tests, and historical vs. current results. (Must) | Both | stated: BRD §6.8 | |
| R50 | The system SHALL retain at least the current and previous operational crush season's data locally, with longer-term historical data accessible via Databank/Data Lakehouse. (Must) | Both | stated: BRD §6.8 note; HLD §5.1 ("approximately two years (or seasons)") | |

### Integration & Data Flow

| # | Acceptance criterion (EARS) | User (§2) | Source | Assumption? |
|---|---|---|---|---|
| R51 | The system SHALL support integration with Databank, SCADA (via IP21), and data platforms (e.g. Data Lakehouse). (Must) | — (system) | stated: BRD §6.9 | |
| R52 | The system SHALL ensure only valid and complete data is made available to downstream systems. (Must) | — (system) | stated: BRD §6.9 | |
| R53 | Integration failures SHALL be visible to authorised users and SHALL support reprocessing or recovery. (Must) | Lab Coordinator | stated: BRD §6.9 | |
| R54 | The system SHALL support near-real-time transfer for operational systems (e.g. SCADA), post-validation transfer for downstream calculations, and batch feeds for analytics platforms. (Must) | — (system) | stated: BRD §6.9 | |
| R55 | The system SHOULD support configurable alerting/notification for defined events (e.g. out-of-tolerance results, critical gateway analyses); delivery mechanism to be confirmed during solution design. (Could) | Both | stated: BRD §6.9 | |
| R56 | External laboratory analysis results (e.g. Gateway Lab sugar quality results) associated with a Control Sample SHALL NOT be captured within LIMS Control Lab; they SHALL continue to be recorded directly in Databank, as today. (Must) | — (out of LIMS) | stated: intake interview decision, 2026-08-25 — supersedes the BRD's original in-scope listing of external-result capture, per the business's own operational note ("External Analysis should go into Databank. No need to be included in the LIMS.") | |
| R57 | "C Molasses Exchange" results SHALL be captured and retained within LIMS Control Lab, but SHALL NOT be transmitted to Databank in this release. | Control Lab Analyst | stated: intake interview decision, 2026-08-25. **Comment: provisional** — the source data flags Databank routing for this product as an open "Databank question"; revisit with the Databank/business team in a future release. | |

### Cross-Site & Sample Transfer

| # | Acceptance criterion (EARS) | User (§2) | Source | Assumption? |
|---|---|---|---|---|
| R58 | The system SHALL support transfer of samples and associated data between sites. (Must) | Control Lab Analyst | stated: BRD §6.10 | |
| R59 | Transferring and receiving sites SHALL be able to view incoming samples, continue analyses, and access prior results; the current (analysing) site SHALL be able to edit. (Must) | Control Lab Analyst | stated: BRD §6.10 | |

### UI/UX Requirements

| # | Acceptance criterion (EARS) | User (§2) | Source | Assumption? |
|---|---|---|---|---|
| R60 | The system SHALL provide users with a clear view of work for the current shift/day, work in progress, and completed work. (Must) | Control Lab Analyst | stated: BRD §6.11 | |
| R61 | The system SHALL support efficient navigation between samples, analyses, tests, and sites, without requiring excessive navigation or data re-entry. (Must) | Control Lab Analyst | stated: BRD §6.11 | |
| R62 | The system SHALL support rapid entry of multiple similar readings across samples or tests. (Must) | Control Lab Analyst | stated: BRD §6.11 | |
| R63 | Users SHALL be able to easily identify current analysis state, outstanding steps, and exceptions requiring attention. (Must) | Both | stated: BRD §6.11 | |
| R64 | The system SHALL support working across multiple samples and analyses concurrently. (Must) | Control Lab Analyst | stated: BRD §6.11 | |

### Non-Functional, Operational & Security

| # | Acceptance criterion (EARS) | User (§2) | Source | Assumption? |
|---|---|---|---|---|
| R65 | The system SHALL support responsive data entry and retrieval under expected operational load. | Both | stated: BRD §6.12 | |
| R66 | The system SHALL be available to support continuous, shift-based laboratory operations. | Both | stated: BRD §6.12 | |
| R67 | The system SHALL support efficient data entry in a laboratory environment and SHALL be accessible across relevant device types (e.g. desktop, mobile/tablet). | Both | stated: BRD §6.12 | |
| R68 | The system SHALL ensure data integrity during normal and interrupted operation and SHALL support recovery from failures without data loss. | — (system) | stated: BRD §6.12 | |
| R69 | WHERE required, the system SHALL support offline data capture and subsequent synchronisation. | Control Lab Analyst | stated: BRD §6.12 | |
| R70 | Backup and recovery processes SHALL be defined to meet business-continuity needs, and disaster-recovery capability SHALL support restoration within acceptable timeframes. | — (system) | stated: BRD §6.13 | |
| R71 | Appropriate system and user documentation SHALL be provided. | Both | stated: BRD §6.13 | |
| R72 | The system SHALL enforce role-based access control aligned to the laboratory roles in §2, with segregation of access by site, role, and responsibility. (Must) | — (system) | stated: BRD §6.14 | |
| R73 | Authentication SHALL align with enterprise standards (e.g. single sign-on where applicable). (Must) | Both | stated: BRD §6.14 | |
| R74 | Access to analysis-template configuration, validation rules, and critical data SHALL be controlled and restricted to the Lab Coordinator role. (Must) | Lab Coordinator | stated: BRD §6.14 | |
| R75 | The system SHALL provide full auditability of data access and changes. (Must) | — (system) | stated: BRD §6.14 | |
| R76 | Clear ownership of Control Lab data SHALL be defined: LIMS Control Lab is the system of record for laboratory execution data; Databank is the system of record for validated enterprise/production data. | — (system) | stated: BRD §6.15 | |
| R77 | Data definitions (e.g. products, analysis types, units) SHALL align with enterprise standards. | — (system) | stated: BRD §6.15 | |
| R78 | Data quality rules and validation expectations SHALL be established, and data lineage from capture through to downstream systems SHALL be maintained. | — (system) | stated: BRD §6.15 | |

## 4. Data & source of truth

| Data | Source of truth | Imported / entered / derived | On conflict |
|------|-----------------|------------------------------|-------------|
| Control Lab analysis results (readings, calculations, calibration, exceptions) | LIMS Control Lab | Entered manually or captured from instruments | LIMS Control Lab is authoritative for lab execution detail |
| Validated enterprise/production data | Databank | Imported one-way, post-validation, from LIMS Control Lab | Databank never edits LIMS-origin data; it is a downstream consumer, not the system of record for lab execution |
| Analysis templates, validation rules/tolerances, calibration tables | LIMS Control Lab | Configured by a Lab Coordinator | N/A — Lab Coordinator-owned configuration |
| External laboratory analysis results (e.g. Gateway Lab sugar quality) associated with a Control Sample | Databank | Entered directly into Databank | Out of LIMS Control Lab scope — decided 2026-08-25 (R56) |
| Factory Data from site SCADA/ProcessNet/Citect systems (e.g. boiler emissions, maceration readings) | Databank | Manually keyed by Control Lab staff into Databank, as today | Out of LIMS Control Lab scope — decided 2026-08-25; no change to the current process |
| "C Molasses Exchange" results | LIMS Control Lab | Entered manually or via instrument | Not transmitted to Databank in this release (R57) — provisional, revisit in a future release |
| SCADA display data (e.g. Pol results shown on factory e-screens) | LIMS Control Lab (source); pushed to SCADA/IP21 | Derived from validated LIMS Control Lab results | LIMS Control Lab is authoritative |
| Historical lab data older than the current + previous crush season | Data Lakehouse (via Databank) | Batch feed from LIMS Control Lab | LIMS Control Lab retains only ~2 seasons locally (R50) |

## 5. Roles & permissions

| Role | Can | Cannot | Notes |
|------|-----|--------|-------|
| Control Lab Analyst | Perform scheduled/ad-hoc analyses; capture manual/instrument readings; start, pause, resume, cancel their own analysis work; save and resume partial work; respond to their own exceptions (modify, retest, accept-with-comment); search/compare historical data; transfer samples between sites | Configure or retire analysis templates or validation rules; unlock or amend a locked/completed result; approve or reject an exception on someone else's behalf; reassign work to other users | Segregation of access applies by site and role — an Analyst only sees and acts on work for their own site/role (R72) |
| Lab Coordinator | Everything a Control Lab Analyst can do, plus: create/modify/retire analysis templates and validation rules/tolerances; review and accept/reject exception results with notes; unlock and amend a locked/completed result (with mandatory justification and full audit); assign/reassign analysis work between users or roles | Bypass the audit trail; unlock or amend a result without recording a justification | Confirmed as a distinct permission tier via intake interview 2026-08-25, matching the BRD's as-is process diagram |

## 6. Privacy, sensitive data & retention

No personal or customer data is involved — the HLD explicitly states this (Personally Identifiable Information: unchecked), and no source document names any HR-, payroll-, or customer-linked data. The only privacy-relevant element is **staff identity captured against actions for the audit trail** — every status change, data entry, exception decision, and unlock/amend action is attributed to a user and role (R22, R23, R75) under role-based access control (R72) and enterprise-standard authentication such as SSO (R73). Confirmed via intake interview 2026-08-25.

**Retention:** the system retains at least the current and previous operational crush season locally; older data is accessible via Databank/Data Lakehouse (R50). No separate export/download rule beyond the existing downstream integration paths (R51–R54) is stated anywhere in the source.

## 7. Scope

- **In scope:** Laboratory data capture (manual and instrument-based — Polarimeter, Balance, HPLC and others as defined in solution design); analysis configuration, execution, and lifecycle management; scheduled and ad-hoc analysis handling; automated calculations and validation; calibration curves/tables; result management and traceability; exception reporting; integration of validated results to Databank, operational systems (e.g. SCADA), and data platforms; recording of routine and ad-hoc instrument calibration.
- **Out of scope / later:**
  - Execution of one-off or non-routine external laboratory testing services (e.g. asbestos identification, ad-hoc investigative testing) — BRD §4.2.
  - Full replacement of Databank's operational reporting capabilities — BRD §4.2.
  - Non-Control-Lab spreadsheet-based processes — BRD §4.2.
  - **Capture of external laboratory analysis results** (e.g. Gateway Lab sugar quality results) associated with Control Samples — the BRD originally listed this as in-scope; **superseded at intake, 2026-08-25**: these results continue to be recorded directly in Databank, not LIMS Control Lab, per the business's own operational note (R56).
  - **Factory Data** captured from site SCADA/ProcessNet/Citect systems (e.g. boiler emissions, maceration) — stays as direct manual Databank entry, as today; not captured via LIMS Control Lab (R56 area, decided 2026-08-25).
  - Databank's own downstream data validation and its onward push to SAP — this is Databank's internal concern, not a Control Lab LIMS requirement.
  - Bioethanol laboratory equipment/analyses (site codes YPP/Yarraville, SAR/Sarina appearing in the equipment inventory) — a different business unit, not a sugar-mill Control Lab (see assumption ledger A2).
  - **Future considerations** (BRD §4.3, explicitly deferred, not decided against): reagent and consumables tracking; attachment/storage of certificates or reports from external laboratory analyses; extended reporting capabilities within LIMS; service reports and certifications for equipment.
  - Granular per-site test-method naming, exact tolerance values, and precise task ownership below the role level (e.g. which named individual tests Feed Water on a given site) are not settled in this brief — per the HLD's own stated assumption, this detail is finalised during solution design as Lab Coordinator-configured template data (R1–R4), not as top-level requirements.

## 8. Gate

- **Approve:** the requirements (§3) **and** the assumption ledger (`assumption-ledger.md`) — 2 entries, both `safe-to-infer` and already applied, 0 `needs-human`.
- **Open items:** `question-ledger.md` carries 2 `open` rows, both `out-of-remit` and owned by `/ship:charter` — they do not block this approval; the Definition phase does not close until charter resolves them.
- **Not approvable while** any section is blank, any §3 row has an empty Source, or placeholder text remains — verified clean at time of writing.
