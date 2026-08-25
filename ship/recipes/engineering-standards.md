# Engineering standards — LIMS Control Lab *(project standard)*

> No prior engineering standard existed for this project — `/ship:charter` drafted this fresh from the
> approved brief and the platform decisions in `charter.md` §2, with the operator's confirmation. This
> file **becomes the project's baseline**; no cross-check ran against a pre-existing standard. Copy it
> forward into later releases of this project rather than rewriting it — a deviation from it is recorded
> in that release's `charter.md` §3a, not a fresh set of choices.
>
> **The stack: a .NET 10 layered Web API backend + an Angular 21+ (standalone components) frontend, built
> on Kendo UI for Angular (v23, licensed — see the license-handling note in §1), both extending the
> existing LIMS platform and sharing its SQL Server database (`cane-db`).** Authentication is an interim
> username/password mechanism, designed to be swapped for Entra ID SSO without changing the authorization
> model (claims-based) underneath it.

## Reference architecture — the standing instruction the build starts from

### Backend — .NET 10 Web API, strictly layered `Controller → Service → Repository → DbContext` (EF Core)
- **Controllers** are thin: one per aggregate root (Sample, Analysis, AnalysisTemplate, CalibrationCurve,
  Schedule, …), authorize + validate input, delegate to a service, map the result to a DTO. Controllers
  never return entities.
- **Services** hold business logic and are HTTP-agnostic. Writes return an `Outcome<T>`
  (`Ok / NotFound / Invalid / Forbidden / Conflict`) that the controller maps to the HTTP result —
  `Conflict` maps to 409 for the optimistic-concurrency case (charter §2).
- **Repositories** are the only types that touch the `cane-db` `AppDbContext`. Multi-aggregate writes go
  through a shared `IUnitOfWork`.
- **Validation:** structured request validation (e.g. FluentValidation) producing a consistent
  validation-problem error shape that names the expected range/rule, the actual value, and the reason for
  failure (brief R38).
- **Errors:** one consistent problem-details-style envelope across validation errors, service `Outcome`
  failures, and the unhandled-exception safety net.
- **Logging:** structured logging (e.g. Serilog), config-driven levels, one completion line per request.
- **Auth:** interim ASP.NET Identity-style username/password (properly hashed) issuing a claims-bearing
  token; role (Control Lab Analyst / Lab Coordinator) and site claims are checked server-side on every
  write. The authorization model is claims-based specifically so swapping the credential mechanism for
  Entra ID SSO later does not require redesigning authorization.
- **Concurrency:** an EF Core concurrency token (rowversion) on every entity subject to concurrent edits.
  A losing `SaveChangesAsync` returns `Outcome.Conflict` → HTTP 409 — never a silent overwrite.
- **Derived values:** calculated/derived fields (weighted averages, composite results, calibration-
  adjusted values) recompute in the service layer whenever a source reading changes, while the parent
  analysis is unlocked. Locking freezes them; a Lab Coordinator's unlock is what triggers recompute.
- **Time:** inject a clock abstraction rather than calling `DateTime.Now`/`UtcNow` directly, so
  shift/schedule logic (brief R10) is testable.
- **Data:** EF Core migrations only, no ad hoc DDL.

### Frontend — Angular 21+, standalone components, feature-based
- **Standalone components** (no NgModules), lazy-loaded feature routes.
- **Feature-based structure:** `features/<name>/` owns its components, services, and models; shared
  primitives live in `shared/`.
- **State:** component-local state via signals where possible; a lightweight store for genuinely
  cross-component state (current user, site, role context).
- **API access:** one typed service per feature/resource wrapping `HttpClient` — no raw `HttpClient`
  calls scattered through components.
- **UI:** **Kendo UI for Angular (v23, licensed)** as the base primitive set — Grid, Form, Scheduler,
  and input components in particular fit this domain's history-search screens, template/schedule
  configuration, and shift-based work views. Theme via Kendo's theming system rather than overriding
  component internals; extend a Kendo component before hand-rolling an equivalent. Kendo's accessible-by-
  default components are the starting point for the WCAG AA bar (charter §5), not a substitute for
  verifying it per screen.
- **Forms:** Angular Reactive Forms, validation rules mirroring the backend's — client-side validation is
  a UX convenience, never the authorization or business-rule boundary; the server always re-checks.
- **Accessibility:** WCAG AA on every user-facing screen — semantic HTML first, ARIA only where native
  semantics fall short, keyboard navigation and visible focus management on every interactive flow
  (exception review, unlock/amend, ad-hoc scheduling), automated a11y linting in CI.

## 0. Licensed dependency handling
- **Kendo UI for Angular license key** (`docs/telerik-license.txt`, a signed Telerik license JWT) is a
  credential, not a config value — it is **never committed to source**. It is added to `.gitignore` and
  supplied to build/CI environments the same way any other secret is (environment variable or secret
  store), per the constitution's "no committed secrets" rule.

## 1. Mechanical enforcement *(exists before build starts — charter §2a)*
- **Backend:** analyzers enabled (`EnableNETAnalyzers`, `AnalysisLevel=latest-recommended`),
  `TreatWarningsAsErrors`, a repo-root `.editorconfig` with any suppression individually justified.
- **Frontend:** Angular ESLint flat config as a **zero-warning CI gate**; strict TypeScript
  (`"strict": true`) with `tsc --noEmit` as a separate zero-error typecheck script.
- **Coverage:** collected in CI for both sides (e.g. Coverlet for .NET, Karma/Jest coverage for Angular),
  **starting floor: 60% line coverage**, ratcheted up as the codebase matures — never lowered to make a
  build pass.
- **Pre-commit:** lint + format on staged files.

## 2. Verification tiers
**Backend:**
- Service-layer tests mocking repository interfaces; pure domain/calculation helpers (calibration
  chains, weighted averages) tested directly.
- HTTP round-trip tests, at least one per controller, exercising the full pipeline.
- An authorization test pinning the right role+site check to the right action.
- A concurrency test driving two concurrent writers at a locked/derived value and asserting a 409, not a
  silent overwrite.

**Frontend:**
- Unit + integration tests exercising behaviour as a user would, with API calls mocked.
- Component isolation via a catalogue tool (e.g. Storybook or Angular's component test harness).

**Shared:**
- E2E smoke over the core loops — capture a reading, trigger and resolve an exception, unlock/amend a
  locked result, schedule an ad-hoc analysis — run at minimum before each release.
- Coverage floor enforced in CI per §1, not merely measured.

## 3. Data
- Real relationships (FKs / join tables) — never a CSV-as-relation column.
- Indexes on hot filter/sort columns, especially the history-search dimensions (product, site, date/time,
  test, instrument, sample point — brief R48).
- Unique constraints wherever business uniqueness is assumed (e.g. one active template per
  product + site + analysis type).
- Correct date/time typing throughout — never a string-typed date/time column.
- Concurrency token + conflict handling on every entity subject to concurrent edits (charter §2); a
  derived value recomputes on write per the calculation posture above rather than needing a separate
  drift-reconciliation job.
- Multi-aggregate writes (e.g. an exception decision that also changes an analysis' lock state) wrapped
  in one transaction.

## 4. Contracts
- OpenAPI emitted from the running API — the contract is generated, not hand-maintained.
- Angular's API-facing types generated from OpenAPI, diff-gated in CI so committed generated types can't
  silently drift from a fresh regeneration.
- Role/permission definitions are single-sourced server-side; the frontend consumes them (e.g. via a
  `/me` claims endpoint) rather than hand-mirroring a separate role list.

## 5. Observability
**Backend:** health checks with a real `cane-db` dependency probe; request/dependency telemetry
(latency, failure rate); application-audit logging for every consequential action (constitution) as a
distinct concern from general request telemetry.
**Frontend:** a global error handler/boundary so a render failure degrades to a recoverable screen, not a
blank page; production error tracking with source maps; Core Web Vitals (or equivalent) checked before
release.
**Shared:** a correlation id threaded from the Angular client through backend logs and the error
envelope, so one request is traceable end-to-end. A post-deploy smoke gate polls the health endpoint
before a deploy is declared good.

## 6. Reuse — the shared-helpers inventory
This project's domain is calculation-heavy (multi-step calibration chains, weighted averages, tolerance
rules that recur across many analysis types and sites) — exactly the kind of logic that gets silently
re-derived per vertical if the canonical implementation isn't visible. Consult a shared-helpers inventory
before writing any new calculation, validation rule, or shared component; add a row whenever one is
written or adopted. `/ship:verify` checks new code against it.

## 7. Delivery & git conventions
- Conventional commits (`feat(scope): …` / `fix:` / `docs:` / `ci:` …).
- PR-per-change into the release branch (`release/<target>`); squash-merge, delete-source-branch on
  completion. CI split by area (backend/frontend pipelines, path-filtered).
- Tag-driven releases, aligned to the existing LIMS platform's own release cadence (charter §2 deployment
  decision) since Control Lab shares its environments.
- Destructive dev endpoints/test-data resets are config-gated (tier flag + admin role), never
  environment-name-gated.
- The project status doc is structured state, not an append-only narrative — updates replace rows in
  place; history lives in git, PR descriptions, and this dossier.

## Concrete bar per DoD dimension
- **Security** — server-side RBAC (role + site) on every write; audit trail on every consequential
  action; interim credentials properly hashed with a designed migration path to Entra ID SSO.
- **Reliability** — problem-details-style errors everywhere; multi-aggregate writes transactional;
  optimistic concurrency surfaced as a 409, never a silent overwrite.
- **Performance at scale** — history/search endpoints server-side paged (charter §2), hot filter columns
  indexed; no numeric SLA yet (charter §5) — refine during design once real load data exists.
- **Observability** — per §5 above.
- **Verification** — per §2 above, all tiers named and present, not just service tests.
- **Accessibility** — WCAG AA on every user-facing screen, automated a11y checks in CI.

## Reference vertical
Build **vertical #1 — capture a manual reading against a scheduled analysis, validate it against
tolerance, and flag/resolve the resulting exception** (the smallest slice that touches template
configuration, scheduling, data capture, validation, and the audit trail end to end) correctly against
everything above, as the **copy template** every later vertical is built from. Concurrency safety (§3),
the layer boundaries, and the shared-helpers check (§6) apply from this first vertical onward.

## Definition of done, per slice
**Backend:** layered (Controller→Service→Repository) · DTO-mapped · validated with the standard error
shape · server-side RBAC (role + site) in lockstep with the UI · paged if it lists · indexed/constrained
where §3 applies · concurrency-safe if it touches a locked/derived value · structured logging + audit
entries.
**Frontend:** feature-scoped, standalone components · built on the component-library primitives (no
hand-rolled equivalents) · reactive forms mirroring backend validation · typed API service (no raw
`HttpClient` in components) · error/loading/empty/permission states handled · WCAG AA.
**Both:** shared-helpers inventory checked/updated · tests green across the tiers in §2.
