# Backend Architecture

What this API does, why it's structured this way, and what to change first if you extend it.

## Request flow

```
HTTP request
    │
    ▼
Controller            (Controllers/)      — HTTP in/out only: parse request, call BL, shape response
    │
    ▼
Business Logic (BL)    (BusinessLogic/)    — validation, orchestration, DTO mapping
    │
    ▼
Repository             (Repositories/)     — EF Core queries only, no business rules
    │
    ▼
DbContext              (Data/)             — EF Core model configuration
    │
    ▼
SQL Server (LocalDB)
```

Every arrow is an interface (`IDashboardBusinessLogic`, `IDashboardRepository`, `IAuthBusinessLogic`), registered in `Program.cs` via constructor injection. That's what makes each layer independently unit-testable — a controller test can mock `IDashboardBusinessLogic` without touching EF Core or SQL at all.

## Why Controller / BL / Repository instead of one big service

This is a small API, so it would be *possible* to put validation and EF queries directly in the controller. Three layers instead of one buys:

- **A single seam for business rules.** Everything under "would this order be allowed" lives in `BusinessLogic/`, not scattered across controller actions and query code.
- **A single seam for storage.** `Repositories/` is the only place that knows about `DbContext`. Swapping SQL Server for another provider, or mocking data access in a test, touches one layer.
- **Thin controllers.** `DashboardController` and `AuthController` only translate HTTP ↔ calls to BL — no `try/catch` around EF exceptions, no manual validation duplicated across endpoints.

The trade-off is more files for a small project. For a dashboard with one entity family (orders, cards, activities, traffic), that's a deliberate choice to demonstrate the pattern cleanly rather than a requirement of the domain — worth knowing if you're asked "isn't this overkill for 6 endpoints?" in a review: yes, a minimal API could do this in a third of the files, but the layering is what an enterprise codebase would converge to once a second entity family or a second consumer shows up.

## Folder-by-folder

| Folder | Owns | Never contains |
|---|---|---|
| `Controllers/` | Route attributes, `[Authorize]`, HTTP status codes | EF Core calls, validation logic |
| `BusinessLogic/` | Input validation, business rules, entity → DTO mapping calls | `DbContext`, SQL, HTTP concerns |
| `Repositories/` | `DbContext` queries (`AsNoTracking()` for reads), CRUD | Validation, DTOs |
| `Data/` | `AppDbContext`, EF model configuration (keys, relationships) | Query logic |
| `Models/` | EF Core entities — mirror the SQL tables exactly | Wire-format concerns (camelCase, nullability contracts) |
| `DTOs/` | Wire contracts returned to/from clients | EF Core attributes, navigation properties |
| `Mappings/` | Manual `Entity → Dto` extension methods | Business rules |
| `Configuration/` | Strongly-typed `IOptions<T>` classes bound from `appsettings.json` | Values themselves (those live in config) |
| `Migrations/` | EF Core schema history | Hand-edited SQL |

`Models` and `DTOs` are deliberately separate types even though several of them look identical field-for-field today. Returning entities directly from a controller is a common shortcut that becomes a liability the moment the EF entity needs a field the client shouldn't see (audit columns, internal flags) — the DTO boundary is what prevents that leak later, at the cost of writing (currently trivial) mapping code now.

## Authentication

### What's implemented

`POST /api/auth/login` exchanges a username/password for a JWT (HMAC-SHA256, signed with `Jwt:Key` from `appsettings.json`). All `DashboardController` endpoints carry `[Authorize]`, so every request needs `Authorization: Bearer <token>`. `AuthController` and `HealthController` stay anonymous — you need to be able to log in and to health-check before you have a token.

`AuthBusinessLogic.Login` checks credentials against **one hardcoded account** (`DemoUser` config section), not a database table. `CryptographicOperations.FixedTimeEquals` is used for the comparison instead of `==`/`string.Equals` so a bad guess doesn't return measurably faster/slower depending on how many characters matched (a timing side-channel) — cheap to do, worth knowing why it's there if asked.

### Why JWT and not cookies/sessions

- The API has no server-side session state — it's called by a MAUI desktop/mobile client, not a browser with cookie storage, so bearer tokens in a header are the natural fit.
```
Authorization: Bearer eyJhbGciOi...
```
- Stateless: any instance can validate a token without shared session storage, which matters the moment this API runs behind a load balancer.

### Known limitations (by design, for a demo — not oversights)

1. **`Jwt:Key` lives in `appsettings.json`, checked into git.** Fine for local dev; in a real deployment this must move to User Secrets (dev), environment variables, or Azure Key Vault/AWS Secrets Manager (prod). Never ship a real signing key in source control.
2. **One hardcoded user, plaintext password in config.** There's no `Users` table yet. To make this production-grade:
   - Add a `User` entity/migration (`Id`, `Username`, `PasswordHash`, `PasswordSalt` or use `Microsoft.AspNetCore.Identity.PasswordHasher<T>`)
   - Add `IUserRepository` + wire it into `AuthBusinessLogic` instead of `DemoUserOptions`
   - Add a `POST /api/auth/register` endpoint if self-service signup is needed
3. **No refresh tokens.** The token just expires (`Jwt:ExpiryMinutes`, default 60) and the client re-logs in — see `AuthTokenHandler` on the MAUI side. A refresh-token flow would avoid re-sending the password, at the cost of a second token type and a revocation story.
4. **No role/claim-based authorization.** Everything with a valid token can hit every dashboard endpoint. `[Authorize(Roles = "Admin")]` plus a `role` claim in `AuthBusinessLogic.Login` is the natural next step once there's more than one type of user.

### Client side (MAUI app)

The MAUI app doesn't have a login screen — `Services/AuthTokenHandler.cs` is an `HttpMessageHandler` that logs in against the demo account on first use, caches the token, and re-logs in a minute before it expires. This keeps the "demo, single hardcoded account" shape consistent end-to-end: the moment the backend grows real user accounts, the client's login call is the only thing that needs a UI in front of it.

## Reliability features

- `AsNoTracking()` on all read-only repository queries — skips EF's change-tracking overhead for data that's never updated in the same request.
- `EnableRetryOnFailure()` on the `DbContext` — transient SQL faults (network blip, deadlock) retry automatically instead of surfacing as a 500 on the first hiccup.
- Centralized `UseExceptionHandler` in `Program.cs` — any exception that escapes a controller becomes a consistent JSON error shape, and only leaks the exception message in `Development`.
- `/health` calls `Database.CanConnectAsync()` — a green health check means the database is actually reachable, not just that the process is alive.

## Why this is a separate repo from the MAUI app

This backend started inside the MAUI app's repo as a Dapper-based Minimal API, then got rewritten into the current layered architecture with EF Core, then was split out entirely (`git subtree split`, history preserved) once it became clear it's a separately deployable, separately versioned concern — the MAUI app is *a* client, not *the* app. Splitting means this API can be deployed, scaled, and given its own release cadence without coupling to mobile app store review cycles, and could serve a web or additional mobile client later without any shared source, only the HTTP contract documented in the root `README.md`.
