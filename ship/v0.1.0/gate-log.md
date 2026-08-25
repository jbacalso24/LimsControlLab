# Gate log — `v0.1.0`

> **⚠️ PROVISIONAL.** The gate-log is a theory-adopted audit-posture artifact — the founding retro
> never evidenced its cost. It has not been exercised on a real release. Keep it as lightweight audit
> substrate, but if it proves to be make-work after a real run, it is a candidate to drop.

> **Append-only.** The immutable trail of who decided what, when, and against which version of the
> spec. Never edit a past row; a reversal or correction is a *new* row that references the old one.
> This is the release's audit substrate (alongside the git history of the dossier).

| # | When (UTC) | Stage / gate | Decision | Who | Against (spec version / commit) | Notes |
|---|-----------|--------------|----------|-----|--------------------------------|-------|
| 1 | 2026-08-25T00:00 | frame — brief approved | `Approve` | Judison Bacalso | `brief.md` (v0.1.0, pre-commit) | Assumption ledger has 0 needs-human rows; question ledger has 2 open out-of-remit rows owned by charter, non-blocking per phases.md |
| 2 | 2026-08-25T01:00 | charter — decisions + constitution confirmed | `Confirm` | Judison Bacalso | `charter.md@a58f960` (base brief), see also interim answers: "Mixed. Backend and Frontend... I will just go directly with claude code." (delivery surface); "It should be separated. Backend will use .NET while frontend will use Angular." (stack split); ".NET 10 will be good" / "Angular 21+ would be better." (versions); "Yes, same SQL Server \"cane-db\"" (persistence); "It will be Entra ID, but for now just do a fallback on user and password." (auth); "Optimistic concurrency, conflict surfaced" (concurrency); "Same as existing LIMS platform" (deployment); "WCAG AA required" (accessibility); "Also i want to use Kendo UI for the components... version should v23" (UI library) | Both question-ledger rows (Q1, Q2) resolved into charter.md §2; DEFINITION phase exit gate now clean (no open rows) |
| 6 | 2026-08-25T05:00 | design-handoff — written | `n/a — no human gate; handoff written` | Judison Bacalso | `design-handoff.md` built from `audits/codebase-2026-08-25/@3990b17` + `decisions-log.md@d285764` | 12 decided-behaviour rows, 26 unsequenced work items (W1–W26), 12 out-of-scope items (7 not-planned, 4 vNext, 1 revisit-next-release). Audit verdict carried as NOT HANDOFF-READY (greenfield, "gaps are the work" per phases.md) — not silently overridden. Cleared to `/ship:plan`. |
| 5 | 2026-08-25T04:00 | clarify — question resolved | `n/a — 0 clarify-routed rows found; nothing to resolve` | Judison Bacalso | `ship/v0.1.0/audits/codebase-2026-08-25/@3990b17` | Worklist (additions.md + coverage.md clarify-routed rows) was empty — Definition already closed every decision. `decisions-log.md` created with 0 entries, documenting why. Cleared to `/ship:design-handoff`. |
| 4 | 2026-08-25T03:00 | audit — verdict | `NOT HANDOFF-READY (expected — greenfield build inventory)` | Judison Bacalso | `ship/v0.1.0/audits/codebase-2026-08-25/` @ repo commit `79a43d0` | 6 blocker + 20 gap findings, all routed to `plan` (0 to `clarify` — Definition already closed every decision). `coverage.md`'s 26 rows are the build backlog. |
| 3 | 2026-08-25T02:00 | design-request — issued | `n/a — no human gate; request issued` | Judison Bacalso | `design-request.md` built from `brief.md@66271df` + `charter.md@66271df` | Backend (codebase) surface — no external design commissioned; nothing returned. Next: `/ship:audit` against the repo at HEAD (currently empty/greenfield). |
