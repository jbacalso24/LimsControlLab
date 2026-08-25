# Shape — `v0.1.0`

- **Release:** v0.1.0  ·  **Written:** 2026-08-25  ·  **Delivery surface:** backend (codebase)
- **Built from** *(pointers, commit-pinned):* `plan.md@fa5f2fe` + `design-handoff.md@f65e295` + `engineering-standards.md@66271df`

> **To build:** §1 is the skeleton you copy — do not re-invent contracts or layering per slice, copy them
> from here. §2 is the running record of every decision Construction made; when you hit something
> undecided, route it back to `/ship:shape`, which appends here — never guess, and never reopen Design.

## 1. Technical shape — the copy template

This is a **greenfield vertical build** (backend/codebase, no existing LIMS platform codebase available to
this project — confirmed at audit time), so the shape below is the full copy template for plan Task 3
(the reference vertical) and every backend task that follows it, not a modification pattern.

### Contracts

- **API base:** REST, JSON, URL-versioned — `/api/v1/...`. OpenAPI emitted from the running API
  (`engineering-standards.md` §4); the Angular client (plan Task 15) generates from it, never hand-mirrored.
- **Concurrency contract:** every entity exposed via the API carries a `rowVersion` field (the EF Core
  concurrency token, charter §2). Writes echo the `rowVersion` they read; a stale write returns
  **409 Conflict** with a body naming the current `rowVersion` so the client can reload rather than
  silently overwrite.
- **Error contract:** RFC-7807 `application/problem+json` for every failure — 400 validation (naming the
  expected range, actual value, and reason per brief R38), 403 forbidden, 404 not found, 409 conflict,
  500 safety net. Every response carries a `correlationId` (`engineering-standards.md` §5).
- **Reference vertical endpoints (plan Task 3):**
  - `GET /api/v1/analyses/{analysisId}` → `AnalysisDetailDto { id, sampleId, templateId, status, readings[], exceptions[], rowVersion }`
  - `POST /api/v1/analyses/{analysisId}/readings` — body `{ testId, value, unit, capturedAtUtc, instrumentId? }` → 201 `ReadingDto { id, testId, value, unit, capturedAtUtc, capturedBy, validationResult }`
  - `POST /api/v1/analyses/{analysisId}/exceptions/{exceptionId}/decision` — body `{ decision: "Modify" | "Retest" | "AcceptWithComment", comment, rowVersion }` (comment mandatory — brief R36) → 200 updated `ExceptionDto`
  - `PATCH /api/v1/analyses/{analysisId}/status` — body `{ action: "Start" | "Pause" | "Resume" | "Cancel", rowVersion }` → 200 updated status, or 409 on stale `rowVersion`
- **What must NOT change:** the `rowVersion` + 409 concurrency contract and the RFC-7807 error envelope are established here for every subsequent backend task (4–13) to reuse verbatim — a later task introducing a different error shape or skipping `rowVersion` on a concurrently-editable entity is a shape violation, not a valid local choice.

### Layering

`Controller → Service → Repository → DbContext` throughout (`engineering-standards.md`):

- **`AnalysesController`** (and every later controller) is thin: authorizes via `[Authorize]` policy, validates via the global FluentValidation-equivalent filter, delegates to its service, maps the returned `Outcome<T>` to the HTTP result via one shared extension method set. Controllers never return entities, only DTOs.
- **`AnalysisExecutionService`** (and every later service) holds the business logic, is HTTP-agnostic, returns `Outcome<T>` (`Ok / NotFound / Invalid / Forbidden / Conflict`). Calculation logic is never inlined here — it calls `CalculationEngine` (shared-helpers-inventory).
- **Repositories** (`IAnalysisRepository`, `ISampleRepository`, …) are the only types touching `LimsDbContext`. A write spanning more than one aggregate (e.g. an exception decision that also changes the analysis's lock state) goes through `IUnitOfWork`.
- **Cross-cutting services** — `ICurrentUser` (who's calling), `IAuditLogger` (writes the audit trail), `TimeProvider` (injected clock) — are constructor-injected into services, never instantiated ad hoc or read from `HttpContext` directly inside a service.
- **Angular:** one feature folder per vertical (`features/analysis-execution/`, …), standalone components throughout (no `Component` suffix / `.component.` infix — `analysis-execution.ts`, class `AnalysisExecution`), consistent `lims-` selector prefix. Each feature's API access goes through a concrete service extending `LimsApiService` (itself extending `BaseApiService`) — no raw `HttpClient`, no `fetch`, no `Promise`-returning API calls (per `engineering-standards.md`'s extracted `Databank.WebApp` convention). Reads prefer `httpResource`/`rxResource`; mutations use the `Observable`-returning `post`/`put`/`delete`. Smart/container components own state (signals) and API calls; presentational components stay pure.

### Shared helpers — resolved up front

Checked against `ship/shared-helpers-inventory.md` (created this run — first release, so every row is `new`, pre-registered so build reuses rather than re-derives across Tasks 2–15):

| Helper | Reuse / extend / new | Inventory row |
|--------|----------------------|---------------|
| `CalculationEngine` | new | `backend/src/LimsControlLab.Domain/Calculations/CalculationEngine.cs` |
| `IAuditLogger` | new | `backend/src/LimsControlLab.Domain/Auditing/IAuditLogger.cs` |
| `ICurrentUser` | new | `backend/src/LimsControlLab.Domain/Auth/ICurrentUser.cs` |
| `TimeProvider` (DI-registered) | new | `Api/Program.cs` registration |
| `IUnitOfWork` | new | `backend/src/LimsControlLab.Infrastructure/IUnitOfWork.cs` |
| `PagedResult<T>` + paging extension | new | `backend/src/LimsControlLab.Api/Common/PagedResult.cs` |
| Angular paged-list helper + `EmptyState` | new | `frontend/src/app/shared/{paged-list.ts,empty-state.ts}` |
| `BaseApiService` + `LimsApiService` (abstract API base classes) | new | `frontend/src/app/shared/services/api/{base-api.service.ts,lims/lims-api.service.ts}` |

## 2. Construction decision log

| # | Date | Type | Decision | Acceptance criteria (EARS) | Why | Routes to | Decided by |
|---|------|------|----------|----------------------------|-----|-----------|------------|
| C1 | 2026-08-25 | technical | API is URL-versioned (`/api/v1/...`), REST + JSON | n/a — structural | Neither brief nor charter names a versioning scheme; a structural call the requirements never needed to state | shape | shape |
| C2 | 2026-08-25 | technical | DTO naming: `<Entity>Dto` for detail, `<Entity>ListItemDto` for list rows; controllers never return entities | n/a — structural | `engineering-standards.md` implies this convention but doesn't spell out the exact naming; fixed once so every task follows the same pattern | shape | shape |
| C3 | 2026-08-25 | technical | Concurrency is mechanically implemented as a `rowVersion` field on every concurrently-editable DTO, echoed on writes, with 409 responses naming the current `rowVersion` | n/a — structural | Charter §2 decided *that* optimistic concurrency applies; it did not (and shouldn't have) specified the field name or 409 body shape — that's this stage's job | shape | shape |
| C4 | 2026-08-25 | technical | Audit logging is centralized in one `IAuditLogger` service called from each Service method after a successful write, never a bespoke audit insert per controller/service | n/a — structural | Brief R22/R23/R75 require a full audit trail but don't (and shouldn't) dictate the implementation pattern; centralizing it now prevents 13 independent audit-write implementations later (lesson L-Duplication) | shape | shape |
| C5 | 2026-08-25 | technical | Reference vertical's minimal entity set (plan Task 3) is `Sample`, `Analysis`, `AnalysisTemplate` (minimal fields only — full template configurability is plan Task 4), `ExceptionRecord` | n/a — structural | Scoping Task 3 tightly to the lifecycle+exception+audit+concurrency pattern (not full template richness) is what makes it a clean copy template rather than a partial slice of Task 4's work | shape | shape |

| C6 | 2026-08-25 | technical | Frontend conventions corrected to match the organisation's real `Databank.WebApp` codebase (Angular 21.1, Kendo v23, Vitest, `BaseApiService`/`httpResource` pattern, layout-first routing, four-tier environments) rather than generic Angular assumptions; `engineering-standards.md`, this file, `plan.md`, and `shared-helpers-inventory.md` updated accordingly | n/a — structural | The operator provided access to a real sibling Angular codebase (`C:\Users\j.bacalso\Documents\Databank\Databank.WebApp`) after the initial shape run; extracting its actual conventions is more reliable than an inferred default and corrects several specifics (test runner, API-layering pattern, naming) the first pass got only approximately right | shape | shape |

- **Additions this release:** 0  ·  **Technical decisions:** 6

No `addition` rows this run — every open structural question for the reference vertical was decidable from `brief.md`, `charter.md`, and `engineering-standards.md` without a requirements gap. **Can shape decide everything in front of it? Yes.** Cleared to `/ship:build`.
