# Assumption ledger — `v0.1.0`

> The record of everything the intake *inferred* rather than was told. It exists so a reader can tell
> what was decided vs. assumed, and so the risky assumptions get a human call instead of silently
> shaping the spec. `needs-human` rows block the gate until answered.

- **Entries:** 2  ·  **safe-to-infer (applied):** 2  ·  **needs-human (surface at gate):** 0

| # | Assumption | Why inferred | Confidence | Downstream decision it affects | Class | Status |
|---|-----------|--------------|-----------|--------------------------------|-------|--------|
| A1 | Site code `PCK` in the source spreadsheet maps to the mill named **Plane Creek** | The "Analysis Required" sheet explicitly names 7 of the 8 mills in its own column headers (Inkerman, Invicta, Kalamia, Victoria, Macknade, Proserpine, Pioneer) matching the corresponding site codes exactly; Plane Creek is the only one of Wilmar Sugar's known 8 mills left unmatched, and `PCK` is the only site code left unnamed anywhere in the workbook. Confirmed by elimination, not by an explicit label. | high | Any place a site name (rather than the raw code) is shown to a user, e.g. schedule/template configuration by site (R4, R9), reporting, integration payloads | safe-to-infer | applied |
| A2 | Equipment rows tagged with site codes `YPP` (Yarraville) and `SAR` (Sarina) in the "equipment analysis" sheet belong to **bioethanol laboratories**, a different business unit, and are out of scope for this Control Lab LIMS | These two codes do not correspond to any of Wilmar Sugar's 8 mill sites named throughout the rest of the workbook, and their associated equipment/analysis types (GC/GCMS, fermentation-related testing) are unrelated to sugar-mill Control Lab testing | high | §7 scope — excludes these rows from the in-scope equipment/analysis inventory | safe-to-infer | applied |

## Notes
- **Confidence + downstream** together set the class: low-confidence or a high-blast-radius downstream
  → `needs-human`, regardless of how reasonable the guess seems.
- A `needs-human` row resolved at the gate becomes a decision — record the answer and the acceptance
  criteria that make it testable in the row itself, and fold the decision into the brief section it
  affects. Don't just flip the status.
- **Not the same file as `question-ledger.md`.** This one records what frame *inferred*; that one
  records what frame *asked and didn't get*, plus what it noted but may not decide. An inference is
  something you have to disprove; a question is something nobody answered yet.
- Every other ambiguity or contradiction the source documents raised (role model, external-lab-result
  scope, Factory Data scope, C Molasses Exchange routing, shift model, sample identifiers, privacy
  scope, and the unrelated "Sheet3" boilerplate content) was resolved by a direct human decision during
  the intake interview on 2026-08-25, not inferred — those decisions are recorded directly in `brief.md`,
  not here.
