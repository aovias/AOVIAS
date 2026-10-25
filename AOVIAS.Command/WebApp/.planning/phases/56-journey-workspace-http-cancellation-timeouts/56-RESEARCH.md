# Phase 56 — Research: Journey workspace HTTP cancellation & timeouts

## RESEARCH COMPLETE

### Summary

The WebApp does **not** apply `rxjs` `timeout()` to Journey feature calls (`grep` on `src/app/features/journey` is clean). Primary cancellation drivers are **Angular `HttpClient` subscription teardown** (unsubscribe → `AbortController` aborts the fetch) and **logical races**, not a short RxJS deadline.

### Current architecture (verified in repo)

1. **`JourneyWorkspaceStore` is provided multiple times**
   - `JourneyComponent` (`/app/journey`) — `providers: [JourneyWorkspaceStore]`
   - `JourneyExecutionShellComponent` (`/app/journey/execution`) — `providers: [JourneyWorkspaceStore]`
   - Legacy `JourneyExecutionComponent` — also provides its own store (deprecated; not in `routes.ts`)

   Navigating **dashboard → execution** destroys the dashboard route component and its store instance. Any **in-flight** `forkJoin` or `getChapterStandards` tied to `takeUntilDestroyed(this.destroyRef)` on that instance **unsubscribes** and **aborts** HTTP — this matches backend “canceled task” logs when the user (or test) navigates before completion.

2. **`loadWorkspaceFromApi`**
   - Uses `forkJoin({ session, dashboard, chapters })` with `takeUntilDestroyed(this.destroyRef)`.
   - Successful load calls `ensureChapterStandardsLoaded(firstChapterId)` for the first chapter only.

3. **`ensureChapterStandardsLoaded`**
   - No **request supersession** (no `switchMap`, no generation id): rapid `selectChapter` / mode reload can leave **multiple** concurrent GETs; **completion order** can theoretically merge **stale** chapter standards if responses arrive out of order (violates “no mixed/stale data”).

4. **`BaseApiService`**
   - Plain `http.get/post/...` with **no** per-request `HttpContext` or timeout — good baseline; any long-read policy must be added **explicitly** (e.g. `HttpContext` token + interceptor) so short global deadlines **do not** apply to Journey reads unless intended.

5. **Interceptors**
   - `loadingInterceptor` — `finalize` only; no timeout.
   - `apiResponseInterceptor` — unwrap envelope; no timeout.

6. **Reference docs**
   - `docs/backend/journey-screen-api-spec.md` lists split GETs including `GET .../chapters/{chapterId}/standards` but does **not** state a numeric SLA; **product/Gateway** must supply the agreed **maximum** wait (or link to Gateway host/Kestrel limits).

### Implications for planning

- **Single `JourneyWorkspaceStore` scope** for both Journey dashboard and execution (parent route shell) removes spurious abort on **internal** navigation while keeping **one** load lifecycle — aligns with “user stays in journey area” not canceling work.
- **Explicit load generation / `switchMap`** (or equivalent) for workspace init and chapter-standards loads ensures **latest-only** application of results and intentional cancellation of **superseded** requests (distinct from “user still waiting on same screen”).
- **HttpContext + documented `environment` constant** for long-read timeout (≥ Gateway chain) avoids introducing a **new** stricter client bottleneck.
- **Dev logging**: distinguish `HttpErrorResponse` with status **0** (often network/abort), **504/408**, and closed `error` from `TimeoutError` if a timeout operator is used anywhere in the chain.

### Out of scope (per product prompt)

- Backend query optimization, SQL, or API shape changes.

### Open inputs (blockers for “documented maximum wait”)

- Exact **Gateway + reverse proxy** timeout values for `GET /api/v1/journeys/...` (confirm `v1` vs `v1.0` path segment matches deployment).

---

## RESEARCH COMPLETE
