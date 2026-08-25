# Audit log — `v0.1.0`

> **Append-only activity trace.** One row per `/ship:*` invocation, written by the plugin's
> `UserPromptSubmit` hook — not by the stages, so it cannot be forgotten. Never edit or delete a
> row; this is the record of what ran.
>
> Gate **decisions** (approvals, verdicts, sign-offs, route-backs) live in `gate-log.md`, not here.
> This file answers "what happened, when, by whom"; that one answers "who decided what".

| Timestamp | Operator | Invocation |
| --- | --- | --- |
| 2026-08-25 13:48:29 +10:00 | Judison Bacalso | /ship:charter v0.1.0 |
| 2026-08-25 14:16:56 +10:00 | Judison Bacalso | /ship:audit v0.1.0 |
