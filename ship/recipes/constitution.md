# Constitution — LIMS Control Lab

> The inviolable rules every stage and every agent action must respect for this project. Complements
> (does not replace) any repo `CLAUDE.md`. A finding that violates this file is a **blocker**, regardless
> of severity elsewhere. Drafted by `/ship:charter` from the approved brief (`ship/v0.1.0/brief.md`) and
> the platform decisions in `ship/v0.1.0/charter.md` §2; confirmed by the operator before design.

## Security constraints (must)
- Every write is authorized **server-side** by role (Control Lab Analyst / Lab Coordinator) and site. The Angular UI's role/visibility checks are a UX convenience only — they are never the enforcement boundary.
- Only a Lab Coordinator may unlock or amend a locked/completed result. Every unlock requires a recorded justification and is fully audited (who, when, before/after values).
- No credentials, connection strings, or API keys are ever committed to source. Secrets live in configuration/secret storage. This includes the Kendo UI for Angular license key (`docs/telerik-license.txt`) — gitignored, supplied to build/CI as a secret, never committed.
- The interim username/password authentication (pending Entra ID SSO) stores credentials only via a proper salted password hash — never plaintext, never reversible encryption.
- Every consequential action — a data change, a status/lifecycle transition, an unlock, an exception accept/reject decision — is written to the audit trail (user, role, timestamp, before/after values). This logging must never be bypassable by any code path, including scripts or admin tooling.
- No direct writes to the shared `cane-db` database from outside the LIMS Control Lab API — no ad hoc scripts or manual database edits that bypass authorization or the audit trail.

## Architectural invariants (must)
- Layering is one direction only: Angular UI → .NET Web API → Service layer → Repository/EF Core → SQL Server (`cane-db`). The UI never talks to the database directly; only the API does.
- LIMS Control Lab is the system of record for laboratory execution data (readings, calculations, calibration, exceptions). Databank is the system of record for validated enterprise/production data. Validated results flow **one-way**, LIMS → Databank; Databank must never write back into LIMS Control Lab's data.
- The role/permission model (Control Lab Analyst vs Lab Coordinator, site segregation) is defined once, server-side. The Angular UI reads and reflects it; it never redefines or duplicates the rule set.
- Derived/calculated values (weighted averages, composite results, calibration-adjusted values) recompute automatically from their source readings while the parent analysis is unlocked. Once locked, they freeze; an unlock by a Lab Coordinator is what triggers recompute — nothing else silently changes a locked value.
- Every entity subject to concurrent edits carries a concurrency token. A losing concurrent write surfaces a conflict to the user; it never silently overwrites the winning write.
- External laboratory analysis results and Factory Data (SCADA/ProcessNet/Citect) are **not** captured in LIMS Control Lab — they remain direct Databank entry, per the approved brief (§4, §7). No integration path routes this data into LIMS Control Lab without a new decision recorded in a future release's brief.

## Off-limits (must not)
- No force-push to `release/*` or the integration branch; no skipping CI checks (lint, typecheck, the coverage gate) to merge urgently.
- No plaintext or reversible storage of the interim username/password credentials.
- No "fixing" a result directly in the database to route around the exception/unlock workflow.
- No new dependency or framework substitution for the decided stack (.NET 10 backend, Angular 21+ frontend, SQL Server) without a recorded charter amendment.

## Guidance (not mechanically testable)
- Prefer configuring new analyses/tests through the template system (brief R1–R4) over one-off hardcoded logic for a specific site — the whole point of the template model is that Lab Coordinators extend it without IT/engineering involvement.
- When Entra ID SSO integration becomes available, migrate away from the interim username/password mechanism rather than running both indefinitely.
- Treat granular per-site test-method names, tolerances, and exact task ownership (still incomplete in the source working data) as configuration to gather properly during design/build — never infer a specific tolerance or method name from the ambiguous source spreadsheet.
- C Molasses Exchange not integrating to Databank in this release (brief R57) is provisional — revisit with the Databank/business team rather than treating it as a permanent architectural rule.

## How it's used
`/ship:design-request` carries these constraints into what's handed to whoever builds the UI/backend. `/ship:audit` and `/ship:accept` flag any code found to violate a "must"/"must not" rule above as a **blocker**, regardless of other findings' severity. `/ship:shape` and `/ship:build` route any implementation approach that would breach a rule here back for a compliant alternative rather than building the violation and flagging it later. A suspected breach of a "Guidance" item is raised as a finding for the human to judge, not an automatic block.
