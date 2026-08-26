# Frontend redesign brief — Kendo → Zard UI + Tailwind v4 (shape C18 / charter A1)

**Read this whole file before writing any code.** It is the single shared spec every screen is built to,
so the 7 feature areas come out as one coherent product, not 7 different-looking pages. Your per-screen
scope is in the dispatch that sent you here; this file is the design system + rules that apply to *all* of
them.

## 0. What we're doing and why
Replacing the Kendo UI component library with **Zard UI** (shadcn-style Angular primitives, already
generated into `src/app/shared/components/`) styled with **Tailwind CSS v4**, and modernising the current
"very simple" UI into a clean, professional, easy-to-use tool. Users are **Control Lab analysts and Lab
Coordinators at 8 sugar-mill sites, working rotating shifts** — many seasonal, needing minimal training.
The UI must be fast to scan, fast to enter data into, and unambiguous about state. It must work well on
**desktop and tablet** (BRD §6.12).

## 1. Hard rules (violating any of these = the work is rejected)
1. **No Kendo, anywhere in your files.** Remove every `@progress/kendo-*` import, every `kendo*`
   attribute/element (`kendoButton`, `kendoTextBox`, `kendo-grid`, `kendo-dropdownlist`,
   `kendo-datetimepicker`, `kendo-dialog`, etc.), and every Kendo module from `imports: [...]`. Do **not**
   uninstall the npm packages — the orchestrator does that last, once every screen is migrated.
2. **Do NOT touch the foundation files.** These are locked: `src/styles.css`, `angular.json`,
   `components.json`, `.postcssrc.json`, `tsconfig.json`, `src/app/app.config.ts`, and everything under
   `src/app/shared/components/`, `src/app/shared/core/`, `src/app/shared/utils/`. If you think one needs a
   change, STOP and say so in your report — do not edit it.
3. **Behaviour is frozen — this is a visual/UX rework only.** Every existing behavioural contract stays
   exactly as-is: `rowVersion` threading + 409 conflict handling, mandatory exception comment (R36),
   mandatory unlock justification (R46), the R38 validation display (expected range / actual value /
   reason), site-scoped visibility (R59), loading/error/empty/permission states, form validation rules,
   and every API call (same endpoints, same request/response shapes, same `LimsApiService`-derived
   service). Read the existing component's `.ts` and keep its logic; you are re-skinning the template and
   polishing the TS only where the view needs it.
4. **Use the Zard primitives, never hand-rolled or unstyled-native controls.** Import the specific Zard
   component classes from `@/shared/components/<name>` and add them to the component's `imports` array.
   Read the component's own `.ts` in `src/app/shared/components/<name>/` for its exact selector and inputs
   before using it (§4 lists the common ones, but the source is the truth).
5. **Only semantic tokens for colour** — Tailwind utilities backed by `src/styles.css`
   (`bg-background text-foreground border-border bg-primary text-muted-foreground bg-card`, and the status
   utilities `text-success bg-success/10 text-warning text-info text-destructive`, etc.). **Never** a raw
   hex, rgb, or oklch in a component, and never Tailwind's built-in named colours (`bg-blue-500`,
   `text-gray-700`) — those bypass the design system and dark mode.
6. **Vitest, not Jasmine.** Specs use `vi.fn()`, `vi.spyOn()`, `.mockReturnValue()`, `expect().toBe()`,
   `expect.objectContaining()` — never `jasmine.*`, `spyOn(...).and.returnValue`, `.toBeTrue()`,
   `fail()`. Update the existing spec so it still passes against the new template (query by role/text/
   label, not by Kendo DOM). Every screen must keep its `*.a11y.spec.ts` passing (axe-core, zero WCAG AA
   violations; `color-contrast` rule stays disabled per shape C12).
7. **Keep the file/naming conventions** already in the repo: `<kebab>.component.ts/.html/.scss`, class
   `<Pascal>Component`, `lims-` selector prefix, standalone components, signals + `computed()`, reactive
   forms, `httpResource`/`rxResource` for reads. `.scss` files should end up nearly empty — layout is
   Tailwind utility classes in the template, not bespoke SCSS.
8. **NEVER self-close a native non-void HTML element.** Angular's compiler rejects `<div … />`,
   `<span … />`, `<p … />`, `<textarea … />`, `<td … />`, `<label … />`, `<a … />`, etc. with error
   NG5002 ("Only void, custom and foreign elements can be self closed"), and `tsc --noEmit` does NOT catch
   it — only a real template compile does. Always write explicit closing tags: `<span …></span>`,
   `<div …></div>`, `<textarea …></textarea>`. ONLY these may self-close: void elements
   (`<input />`, `<img />`, `<br />`, `<hr />`) and custom/component elements (`<z-badge />`, `<ng-icon />`,
   `<lims-status-badge />`, `<router-outlet />`). When in doubt, use an explicit closing tag.
9. **Reuse the shared `<lims-status-badge [status]="…" />`** (`@/shared/ui/status-badge/status-badge.component.ts`)
   for any analysis/sample lifecycle status pill (NotStarted/InProgress/OnHold/Completed/Cancelled) — do
   NOT re-inline the status-pill recipe per screen. Import `StatusBadgeComponent` and add to `imports`.
   (For non-lifecycle pills like reading Valid/Invalid or exception Open/Resolved, use the §2 recipe inline.)
10. **Load `/taste-skill:taste-skill` first** and apply its anti-generic design judgment within the system
   below. The system here is the guardrail; taste is how you make it feel finished (spacing rhythm, hover/
   focus states, empty-state warmth, alignment). Do not invent a different colour system or layout shell.

## 2. The design language (build every screen from these)

### App shell (already built by the reference dispatch — match it exactly, don't rebuild it)
- **Left sidebar** (`w-60`, `bg-sidebar text-sidebar-foreground border-r border-sidebar-border`, fixed
  full height): brand block at top (logo + "LIMS Control Lab" wordmark), then nav items as icon + label
  rows (`lucide` icons via `@ng-icons/lucide`). Active item: `bg-sidebar-accent text-sidebar-primary
  font-medium` with a `2px` left primary accent bar. Collapses to an off-canvas drawer under `lg`.
- **Top bar** (`h-14 border-b border-border bg-background/80 backdrop-blur sticky top-0`): left = current
  page title (or breadcrumb); right = a site + shift context chip, a dark-mode toggle, and the user menu
  (avatar + name + role, with Logout).
- **Content region**: `mx-auto max-w-screen-2xl px-6 py-6`.

### Page structure (every feature screen)
- **Page header row**: `<h1 class="text-2xl font-semibold tracking-tight">` + a one-line
  `text-sm text-muted-foreground` description, and a right-aligned **primary action** button where the
  screen has a main action (e.g. "New template", "Schedule analysis"). `flex items-center justify-between`.
- **Body**: content in Zard **cards** (`z-card` with `z-card-header`/`z-card-content`), or a full-width
  table card for list screens. Group related form fields in a card with a `z-card-title`.
- Vertical rhythm: `space-y-6` between major sections; `gap-4` inside a form grid.

### Spacing, radius, type
- Spacing scale: prefer `2 / 3 / 4 / 6 / 8` (`gap-4`, `p-6`). Cards use `rounded-lg border bg-card`.
- Type: `text-2xl font-semibold` page titles; `text-sm font-medium` labels; `text-sm` body;
  `text-xs text-muted-foreground` meta. Inter is the font (set globally — don't re-declare it).
- Never a wall of unlabelled `<div class="detail-row">` — use description lists, cards, or a table.

### Status is the product's core language — render it with a badge recipe
Analysis/sample lifecycle and tolerance state must be instantly scannable. Use a small pill. Map:
| State | Recipe (classes on `<z-badge>` or a `<span>`) |
|-------|-----------------------------------------------|
| Completed / In-tolerance / Valid | `bg-success/12 text-success border border-success/25` |
| In progress | `bg-info/12 text-info border border-info/25` |
| Not started / Draft | `bg-muted text-muted-foreground border border-border` |
| On hold / Overdue / Due soon | `bg-warning/15 text-warning-foreground border border-warning/30` (warning text is dark; use `text-warning` on `bg-warning/12` only where contrast passes) |
| Exception / Out-of-tolerance / Cancelled / Error | `bg-destructive/12 text-destructive border border-destructive/25` |
Add a tiny leading dot (`h-1.5 w-1.5 rounded-full bg-current`) for extra scannability. Keep the pill
`inline-flex items-center gap-1.5 rounded-full px-2 py-0.5 text-xs font-medium`.

### Tables (list screens — replaces Kendo Grid)
- Native `<table z-table>` with `<thead z-table-header><tr z-table-row><th z-table-head>` and
  `<tbody z-table-body><tr z-table-row><td z-table-cell>`. Wrap in a `z-card` with
  `overflow-x-auto` so it scrolls on tablet, never the page body.
- Right-align numeric columns (`text-right tabular-nums`). Row hover `hover:bg-muted/50`. Make the primary
  cell a link where the row drills in. Keep pagination with the Zard `pagination` component where the
  data is paged (history search, work lists).
- Every table needs a real **empty state** (Zard `empty` or a centered `text-muted-foreground` block with
  a lucide icon) and a **loading state** (Zard `skeleton` rows or `spinner`).

### Forms (capture/config screens)
- `<label z-label for=..>` + the Zard control, one field per `<div class="space-y-1.5">`, laid out in a
  responsive grid (`grid gap-4 sm:grid-cols-2`). Inline validation message under the field:
  `text-xs text-destructive` with `role`/`aria-describedby` preserved from the existing template.
- Buttons: primary action `<button z-button>` (default = primary), secondary `zType="outline"`,
  destructive `zType="destructive"`, tertiary `zType="ghost"`. Show `zLoading` while submitting.
- Preserve every existing validator and the exact submit-guard logic.

### Loading / error / empty (never a blank screen)
- Loading: Zard `skeleton` (for content shape) or `z-spinner` for actions; keep `role="status"`.
- Error: a `z-card`/`z-alert` with `text-destructive`, the message, and a Retry `<button z-button
  zType="outline">`; keep `role="alert"`.
- Empty: Zard `empty` with a lucide icon, a short line, and (where relevant) the primary action.

### Responsive
- Everything must be usable at `768px` (tablet). Grids collapse to one column under `sm`; the sidebar
  becomes a drawer under `lg`; tables scroll inside their card; tap targets ≥ 40px high.

## 3. Definition of done for your screen
- `npx tsc --noEmit` clean · `npx ng lint` clean (0 warnings) · `npx ng build` succeeds · `npx ng test`
  green for your feature's specs including `*.a11y.spec.ts` · no `kendo`/`@progress` string anywhere in
  your files (`grep -rn "kendo\|@progress" <your files>` returns nothing) · every behavioural contract in
  §1.3 still works · the screen visibly matches this design language.
- **Report honestly**: paste the ACTUAL command output (test counts, build result). If you could not run
  something, say so plainly — do not claim a pass you didn't verify. The orchestrator re-runs everything.

## 4. Zard component quick reference (read the source for exact inputs)
All under `src/app/shared/components/<name>/`; import the class from `@/shared/components/<name>`.
- **Button**: `<button z-button zType="default|destructive|outline|secondary|ghost|link" zSize="sm|default|lg"
  [zLoading]="submitting()" [zDisabled]="form.invalid">` — `ZardButtonComponent`.
- **Input**: `<input z-input formControlName="x" placeholder=".." />` — `ZardInputComponent` (CVA, works
  with reactive forms). Numeric: keep `type="number"`. Also `input-group` for prefixed/suffixed inputs.
- **Textarea**: `<textarea z-textarea formControlName="comment">` — `ZardTextareaComponent`.
- **Select**: `<z-select formControlName="x" zPlaceholder="..">` with `<z-select-item [zValue]="'Modify'">Modify</z-select-item>`
  children — `ZardSelectComponent` + `ZardSelectItemComponent` (CVA). Read `select-item.component.ts` for
  the exact value input name.
- **Date picker**: `<z-date-picker formControlName="x" zFormat="MMM d, yyyy" />` — `ZardDatePickerComponent`
  (CVA). If a time-of-day is required (e.g. capturedAt), use a native `<input z-input type="datetime-local">`
  bound to the form control instead — note this choice in your report.
- **Table**: `<table z-table>` + `thead z-table-header` / `tbody z-table-body` / `tr z-table-row` /
  `th z-table-head` / `td z-table-cell` / `caption z-table-caption`.
- **Card**: `<z-card>` › `<z-card-header>` (`<z-card-title>`, `<z-card-description>`, `<z-card-action>`) ›
  `<z-card-content>` › `<z-card-footer>`.
- **Badge**: `<z-badge zType="default|secondary|destructive|outline">` — for status pills add the recipe
  classes from §2 via `class="..."` (badge only ships 4 variants; success/warning/info come from the
  token utilities).
- **Dialog** (replaces kendo-dialog): programmatic via `ZardDialogService` from `@/shared/components/dialog`
  — `inject(ZardDialogService).create({ zTitle, zContent, zData, ... })`. Read `dialog.service.ts`.
- Also available: `alert`, `tabs`, `separator`, `spinner`, `skeleton`, `empty`, `tooltip`, `breadcrumb`,
  `pagination`, `dropdown`, `avatar`, `checkbox`, `switch`, `popover`, `calendar`.

## 5. Domain vocabulary (use for realistic empty-state copy / examples, never fabricated API data)
Sites: PIONEER, INKERMAN, INVICTA, KALAMIA, VICTORIA, MACKNADE, PROSERPINE (+ 1). Products: Sugar, A/B/C
Massecuite, A/B/C Molasses, Syrup, Mud, ESJ, Bagasse, Pan Products. Tests/analyses: Pol, Brix, Water, RS,
Ash, Fibre, Sucrose, Dry Substance, Purity. Methods: BSES, NIRA, Quick 2-hour, Vacuum Oven. Instruments:
Refractometer, Polarimeter, Mettler balance, pH meter, HPLC. Roles: Control Lab Analyst, Lab Coordinator.
Sampling methods: Single (snap), Composite, Combined, Split, Exchange (molasses exchange). Lifecycle: Not
started, In progress, On hold, Completed, Cancelled.
