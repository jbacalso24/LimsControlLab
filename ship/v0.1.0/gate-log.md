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
