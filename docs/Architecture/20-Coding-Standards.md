# Coding Standards

[← README](./README.md) · [Architecture Constitution](../PRAMAN-Architecture-Constitution.md)

---

## Backend (.NET)

| Rule | Detail |
|------|--------|
| Nullable reference types | Enabled in modern projects |
| API responses | Always `ApiResponse<T>` |
| Controllers | `[ApiVersion]`, `[Authorize]`, no DbContext |
| Async | `CancellationToken` on public service methods |
| Logging | Structured via Serilog |
| Validation | Filters + FluentValidation where used |

**Naming:** PascalCase public API; async methods suffix `Async`.

---

## Frontend (Angular)

| Rule | Detail |
|------|--------|
| Standalone components | Default — no new NgModules |
| Change detection | `OnPush` preferred |
| State | Signals; stores as `@Injectable` |
| Styling | CSS variables from `theme.tokens.css` — **no raw hex in features** |
| API | Extend `BaseApiService`; path relative to `/api/v1` |

See `PRAMAN_ANGULAR_ARCHITECTURE_RULES.md` for folder structure.

---

## Git & PR

- Focused PRs per sprint/feature.
- Migration files committed with designer/snapshot.
- No secrets in `appsettings` committed values.

---

## AI Assistant Rules

When using Cursor/AI on this repo:

1. Read Architecture Constitution first.
2. Do not invent APIs — verify controllers.
3. Do not mark planned work complete in docs.
4. Match existing naming in surrounding files.

---

## Forbidden (Constitution)

- Bypass service layer from controllers.
- Duplicate business logic in Angular components.
- Direct cross-service database access.
- Force-push to main without approval.
