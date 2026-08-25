# Question ledger — `v0.1.0`

> Everything raised during Definition that is not yet settled. A row exists because an answer is
> *missing*, not because it was hard to ask. **The phase gate requires this file empty** — every row
> carries an owner, so "unresolved" always names someone rather than drifting.

| # | Question / noted item | Kind | Why it isn't settled here | Owner | Affects | Status | Resolution |
|---|----------------------|------|---------------------------|-------|---------|--------|------------|
| Q1 | The HLD states several existing-platform facts: Solution Type `PaaS`; Data Residency `Cloud - ANZ`; Control Lab is expected to be built as a module within the **existing LIMS platform** shared with the Juice Payment module (Windows Server 2025 hosts for the LIMS Web/Control API/Databank API hosts; SQL Server database `cane-db` over TCP 1433; direct REST/HTTPS API to Databank; direct OPC UA to the IP21 Historian; internal domain `sucrogen.com`, external `wilmar.com.au`). | `out-of-remit` | Frame captures WHAT and FOR WHOM; persistence, hosting, and integration-protocol choices are platform decisions charter owns. | `charter` | `charter.md` §2 platform decisions | `open` | `` |
| Q2 | The HLD's own "Option Analysis" section states an explicitly unresolved architecture choice: *"whether to use the existing LIMS codebase, APIs, and database, or to develop a solution that is decoupled from the parent application."* | `out-of-remit` | This is a stack/architecture decision named in the source itself, not a requirement — charter's call, informed by Q1's platform facts. | `charter` | `charter.md` §2/§3a, and downstream `shape.md` | `open` | `` |

## Notes
- **`out-of-remit` is not a complaint — it is a handoff.** Frame recording these HLD-stated platform
  facts is frame doing its job: capturing the evidence without deciding on it.
- **Every row names an `Owner`.** A row owned by nobody is a row nobody closes.
- **A resolved row keeps its history.** Set `Status: resolved` and fill `Resolution` — don't delete it.
- Every other open item the source documents raised (role model, external-lab-result scope, Factory Data
  scope, C Molasses Exchange → Databank routing, the shift-model contradiction, sample identifiers,
  privacy scope, and the unrelated "Sheet3" boilerplate content) was resolved directly with the operator
  during the 2026-08-25 intake interview and is recorded in `brief.md`, not here. Granular per-site
  test-method and task-assignment detail still visible in the source working spreadsheet (e.g. exact
  method-name variants, which individual tests Feed Water on a given site) is not listed here as
  `unanswered` — the HLD's own stated assumption defers that detail to solution-design-time template
  configuration (see `brief.md` §3 and §7), so it does not block this phase.
