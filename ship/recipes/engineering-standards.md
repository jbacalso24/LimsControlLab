# Engineering standards — LIMS Control Lab *(project standard)*

> No prior engineering standard existed for this project — `/ship:charter` drafted this fresh from the
> approved brief and the platform decisions in `charter.md` §2, with the operator's confirmation. This
> file **becomes the project's baseline**; no cross-check ran against a pre-existing standard. Copy it
> forward into later releases of this project rather than rewriting it — a deviation from it is recorded
> in that release's `charter.md` §3a, not a fresh set of choices.
>
> **The stack: a .NET 10 layered Web API backend + an Angular 21+ (standalone components) frontend, built
> on Zard UI (shadcn-style Angular primitives copied into the repo) styled with Tailwind CSS v4** —
> *amended 2026-08-26, charter §3b/A1; was Kendo UI for Angular v23 (licensed)* — **both extending the
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

### Frontend — Angular 21.1+, extracted from the organisation's real Databank.WebApp codebase
> **Provenance.** No written frontend standard existed for this project, but a real sibling codebase does
> — `Databank.WebApp` (the organisation's Angular app for the enterprise Databank system), inspected
> 2026-08-25. It runs the same stack this project targets (Angular 21.1.x, Kendo UI v23.0.1, the same
> Telerik license mechanism) and is the actual convention this section is extracted from, not a generic
> Angular default. **Note (2026-08-26, charter §3b/A1):** Databank.WebApp uses Kendo UI v23; LIMS Control
> Lab **deliberately diverges on the UI library only** — Zard UI + Tailwind v4 instead of Kendo — while
> keeping every other convention extracted here (naming, layout-first routing, `BaseApiService` chain,
> Vitest, four-tier environments). The Kendo-specific lines below have been amended accordingly.
> Where that codebase itself has unresolved debt (raw `fetch`/`Promise` calls its own
> comments flag for removal, `any`-typed models, no ESLint/Prettier/Husky installed, an inconsistent
> component selector prefix), this standard states the **intended** pattern and does not carry the debt
> forward — LIMS Control Lab does the mechanical gates (§1) and the clean pattern from its first commit.

- **Standalone components by default**, each with its own `imports: [...]` array. Zard components are
  themselves standalone Angular components (generated into `shared/components/<name>/`); a feature imports
  the specific Zard component classes it uses (`import { ZardButtonComponent } from '@/shared/components/button'`
  etc.) directly in its own `imports` array — there are no NgModules to import and no per-environment
  license to activate. Reusable shell chrome (layouts, nav, header/footer) lives in `shared/`; feature
  components import the Zard primitives they need directly rather than through a catch-all module.
- **Naming:** no `Component` suffix on class names and no `.component.` infix in filenames — `analysis-execution.ts` / `.html` / `.scss`, class `AnalysisExecution` (the modern Angular style; Databank.WebApp's own root-level files already follow it, even though some older feature files under it still use the `.component.ts` legacy form — LIMS is consistent from the start, not mixed). **One selector prefix for the whole app** (e.g. `lims-`), applied uniformly — Databank.WebApp splits between `app-` and `db-` inconsistently; don't repeat that.
- **Feature-based structure:** `features/<name>/` owns its own components, services, and models;
  reusable chrome and cross-cutting primitives live in `shared/{components,services,models,layouts,
  directives,routes}/`, mirroring Databank.WebApp's real layout.
- **Routing — layout-first:** top-level routes each mount exactly one layout component (e.g. a default
  authenticated shell, an auth-flow layout) with no path segment of their own; the *actual* feature routes
  nest as that layout's `children`, declared in one `<area>.routes.ts` file per area and lazy-loaded. Do
  not hang feature routes directly off the root route array.
- **API access — a `BaseApiService` inheritance chain, not ad hoc `HttpClient` calls:**
  1. `shared/services/api/base-api.service.ts` — an abstract `BaseApiService` with an abstract
     `apiBase: Signal<string>`, protected `get/post/put/delete` methods wrapping `HttpClient` (returning
     `Observable`), and protected `getResource`/`rxGet` helpers wrapping Angular's `httpResource` /
     `rxResource` (signal-driven, auto-refetch-on-param-change reads).
  2. `shared/services/api/lims/lims-api.service.ts` — an abstract `LimsApiService extends BaseApiService`
     fixing `apiBase` to `environment.limsControlLabApiUrl`.
  3. Each feature's concrete service (`@Injectable({ providedIn: 'root' })`) extends `LimsApiService` and
     exposes typed methods only — e.g. `getAnalysisResource(id: number)` returning
     `HttpResourceRef<AnalysisDetailDto | undefined>`.
  - **Prefer `httpResource`/`rxResource` for reads** — they're reactive to signal inputs and expose
    `.value()`, `.isLoading()`, `.error()` directly, which is what feeds `computed()` state in components.
    Traditional `Observable`-returning methods remain for mutations (post/put/delete).
  - **No raw `fetch()` and no `Promise`-returning API methods** — Databank.WebApp's own code marks its
    remaining `fetch`-based methods with `// TODO: Refactor... we DON'T want promises. That's pure
    javascript stuff we don't do anymore.` Treat that as the standard stated plainly, not just a comment
    in someone else's repo: LIMS never writes a new `fetch`/`Promise` API call.
- **State:** signals + `computed()` for derived state, `effect()` for reacting to signal changes
  (matching Databank.WebApp's real components) — no global store (NgRx or otherwise) unless a genuine
  cross-feature state need emerges; cross-cutting context (current user/role/site) is one injectable
  signal-based service, following the same DI pattern as `BaseApiService`.
- **Environments — four tiers, file-replacement based:** `environment.model.ts` (a typed interface),
  `environment.ts` (left blank/placeholder — never edited directly), and `environment.{local,dev,uat,
  prod}.ts`, selected via `angular.json` `fileReplacements` per build configuration — matching
  Databank.WebApp's real convention and charter §2's "same environments as the existing platform"
  decision. Add `limsControlLabApiUrl` to the environment interface alongside whatever Databank's own
  variant already defines.
- **UI:** **Zard UI (shadcn-style Angular primitives) on Tailwind CSS v4** as the base primitive set
  *(amended 2026-08-26, charter §3b/A1; was Kendo UI v23)*. Components are generated into the repo via the
  Zard CLI (`npx zard-cli add <component>`) and **owned as project source** under
  `shared/components/<name>/` — extend or restyle the generated component in place before hand-rolling a
  new equivalent. The set covers this domain directly: **Table** (history search, template/schedule/work
  lists — replaces the Kendo Grid), **Select / Dropdown / Date Picker / Input / Textarea / Button /
  Dialog** for capture and configuration forms, plus **Badge / Card / Tabs** for status and shift-based
  work views. Theme via the Tailwind v4 CSS-variable token layer in `src/styles.css` (a single design
  system — semantic colour/spacing/typography tokens, light + dark), never by editing a component's
  internals ad hoc. Zard components are accessible-by-default (built on Angular CDK) — the starting point
  for the WCAG AA bar (charter §5), not a substitute for verifying it per screen (axe-core, shape.md C12).
  Runtime deps the CLI adds: `@angular/cdk`, `class-variance-authority`, `clsx`, `tailwind-merge`,
  `@ng-icons/core`, `@ng-icons/lucide`; dev: `tailwindcss`, `@tailwindcss/postcss`, `postcss`,
  `tailwindcss-animate`.
- **Forms:** Angular Reactive Forms (`FormBuilder`, `FormGroup`), validation rules mirroring the
  backend's — client-side validation is a UX convenience, never the authorization or business-rule
  boundary; the server always re-checks.
- **Accessibility:** WCAG AA on every user-facing screen — semantic HTML first, ARIA only where native
  semantics fall short, keyboard navigation and visible focus management on every interactive flow
  (exception review, unlock/amend, ad-hoc scheduling), automated a11y linting in CI.
- **Testing runner: Vitest** (via `@angular/build:unit-test`), not Jasmine/Karma — matching
  Databank.WebApp's actual `package.json`/`angular.json` configuration for Angular 21.

## 0. Licensed dependency handling
- **None for the frontend UI library** *(amended 2026-08-26, charter §3b/A1)*. The move from Kendo UI to
  **Zard UI + Tailwind CSS v4** removes the only proprietary, per-environment-licensed dependency — Zard's
  components are MIT-licensed source copied into the repo, and Tailwind is open source. There is no license
  key to gitignore, no secret to provision to developer/CI/deploy environments, and `docs/telerik-license.txt`
  is removed. The constitution's "no committed secrets" rule still governs the backend (JWT signing secret,
  `cane-db` connection string) exactly as before.

## 1. Mechanical enforcement *(exists before build starts — charter §2a)*
- **Backend:** analyzers enabled (`EnableNETAnalyzers`, `AnalysisLevel=latest-recommended`),
  `TreatWarningsAsErrors`, a repo-root `.editorconfig` with any suppression individually justified.
- **Frontend:** Angular ESLint flat config as a **zero-warning CI gate**; strict TypeScript
  (`"strict": true`) with `tsc --noEmit` as a separate zero-error typecheck script.
- **Coverage:** collected in CI for both sides (Coverlet for .NET, Vitest's built-in coverage for
  Angular), **starting floor: 60% line coverage**, ratcheted up as the codebase matures — never lowered
  to make a build pass.
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
- Unit + integration tests via Vitest, exercising behaviour as a user would, with `HttpClient` calls
  mocked (`HttpClientTestingModule`/`provideHttpClientTesting`) — including a case per `httpResource`
  state (`isLoading`, populated `value`, `error`).
- Component isolation via Angular's own component test harness (`@angular/cdk/testing` harnesses).

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
**Frontend:** feature-scoped, standalone components (no `Component` suffix / `.component.` infix,
consistent `lims-` selector prefix) · built on Zard UI primitives + Tailwind v4 (extend the generated Zard component before hand-rolling an equivalent) ·
reactive forms mirroring backend validation · API access only via a `LimsApiService`-derived service
using `httpResource`/`rxResource` for reads (no raw `HttpClient`, no `fetch`, no `Promise`-returning API
calls) · error/loading/empty/permission states handled · WCAG AA.
**Both:** shared-helpers inventory checked/updated · tests green across the tiers in §2.
