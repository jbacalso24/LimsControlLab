# Shared helpers inventory — LIMS Control Lab

> **Why this file exists — lesson L-Duplication.** Vertical-slice building produces horizontal
> duplication: a fresh session per vertical can't see what an earlier session already wrote, so it
> re-derives the helper instead of importing it. Consult this table before writing any new utility,
> formula, or shared component — this domain is explicitly calculation-heavy (calibration chains,
> weighted averages, tolerance rules recurring across many analysis types and sites), which is exactly
> the shape of logic that gets silently re-derived per vertical if the canonical implementation isn't
> visible (`engineering-standards.md` §6).
>
> **How it's used:**
> - **`/ship:build`** — check this table *before* writing any new utility/formula/component; import/extend
>   an existing row instead of writing a new one. Add a row here in the same change that introduces a
>   genuinely new shared helper.
> - **`/ship:verify`**'s dedup dimension checks new code against this inventory.

## Helpers

| Helper | File | Use for | Added |
|--------|------|---------|-------|
| `CalculationEngine` | `backend/src/LimsControlLab.Domain/Calculations/CalculationEngine.cs` | All calibration-curve lookups, weighted averages, and composite-result derivations — the single source for every calculation in brief R39–R41; no analysis-specific formula is re-derived inline in a service | pre-registered 2026-08-25 (shape v0.1.0) — populated by plan Task 6 |
| `IAuditLogger` | `backend/src/LimsControlLab.Domain/Auditing/IAuditLogger.cs` | Every consequential write (status change, unlock, exception decision) calls this once to record user/role/timestamp/before-after — never a bespoke audit insert per service | pre-registered 2026-08-25 (shape v0.1.0) — populated by plan Task 3 |
| `ICurrentUser` | `backend/src/LimsControlLab.Domain/Auth/ICurrentUser.cs` | The single source for "who is calling" (id, role, site claims) — every authorization check and audit-log call reads from this, never a second parallel claims lookup | pre-registered 2026-08-25 (shape v0.1.0) — populated by plan Task 2 |
| `TimeProvider` (injected, not `DateTime.UtcNow`) | DI-registered in `Api/Program.cs` | Every timestamp (audit entries, shift/schedule calculations, lock timestamps) — swappable and testable, never a direct clock call in a service | pre-registered 2026-08-25 (shape v0.1.0) — populated by plan Task 1 |
| `IUnitOfWork` | `backend/src/LimsControlLab.Infrastructure/IUnitOfWork.cs` | Any write touching more than one aggregate (e.g. an exception decision that also changes analysis lock state) — wraps the EF execution strategy + transaction, never two independent `SaveChangesAsync` calls | pre-registered 2026-08-25 (shape v0.1.0) — populated by plan Task 3 |
| `PagedResult<T>` + paging query extension | `backend/src/LimsControlLab.Api/Common/PagedResult.cs` | Every server-paged list endpoint (search/history, template list, schedule list) — one page-size-clamp implementation, never reimplemented per controller | pre-registered 2026-08-25 (shape v0.1.0) — populated by plan Task 8 |
| `usePagedList`-equivalent Angular service/signal + `EmptyState` component | `frontend/src/app/shared/{paged-list.ts,empty-state.component.ts}` | Every Kendo Grid screen's page-state handling and every empty-list render — never hand-rolled per feature | pre-registered 2026-08-25 (shape v0.1.0) — populated by plan Task 15/17 |
| Typed API client base (`HttpClient` wrapper + auth interceptor) | `frontend/src/app/shared/api/api-client.ts` | Every feature's API service wraps this — no raw `HttpClient` calls in components or feature services | pre-registered 2026-08-25 (shape v0.1.0) — populated by plan Task 15 |

<Add a row per shared helper as it's written or adopted going forward.>
