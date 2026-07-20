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
Business Logic (BL)    (BusinessLogic/)    — validation, orchestration
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
| `BusinessLogic/` | Input validation, business rules | `DbContext`, SQL, HTTP concerns |
| `Repositories/` | `DbContext` queries (`AsNoTracking()` for reads), CRUD | Validation |
| `Data/` | `AppDbContext`, EF model configuration (keys, relationships) | Query logic |
| `Models/` | EF Core entities — mirror the SQL tables exactly, and are what the dashboard endpoints return directly | Sensitive fields returned to clients (see `User.PasswordHash` below) |
| `DTOs/` | Wire contracts that are genuinely *not* shaped like an entity: `NewOrderRequest` (excludes server-assigned `Id`/`Invoice`), `LoginRequestDto`/`LoginResponseDto` (auth has no matching entity to reuse) | A DTO for every entity "just in case" |
| `Configuration/` | Strongly-typed `IOptions<T>` classes bound from `appsettings.json` | Values themselves (those live in config) |
| `Migrations/` | EF Core schema history | Hand-edited SQL |

### Why entities are returned directly instead of DTOs everywhere

Every read endpoint (`cards`, `revenue-cards`, `activities`, `orders`, `traffic`) used to map its entity to an identically-shaped DTO before returning it — same properties, same types, a mapping method that copied one into the other. That bought nothing: there was no divergence to protect against, so it was ceremony, not a safety boundary. It's been removed; `DashboardController` now returns `Models` (`Order`, `DashboardCard`, etc.) straight from the business logic layer.

This isn't "DTOs are pointless" — it's "a DTO earns its place by actually differing from the entity, not by existing on principle." Two things in this codebase still do:

- **`NewOrderRequest`** — deliberately *not* `Order`. It omits `Id` and `Invoice` because those are server-assigned; if the create-order endpoint accepted an `Order` directly, a client could set its own `Id`/`Invoice` and nothing would stop it.
- **`LoginRequestDto`/`LoginResponseDto`** — there's no `Order`-style entity these could piggyback on; a JWT + expiry isn't a database row.

The general rule this leaves behind: reach for a DTO when the wire shape needs to *differ* from storage (hide a field, reshape a request, represent something that isn't a row at all) — not by default for every entity. If a dashboard entity later grows an internal-only column (an audit flag, a soft-delete bit), that's the point at which it gets a DTO, not before. `Models/User.cs` is the one entity where this matters today even without a dedicated DTO: it holds `PasswordHash`, and no endpoint returns a `User` — `AuthBusinessLogic` returns `LoginResponseDto` instead, so the hash can't leak by accident. That's the actual boundary worth protecting; duplicating five identical read-only shapes wasn't.

## Authentication

### What's implemented

`POST /api/auth/login` exchanges a username/password for a JWT (HMAC-SHA256, signed with `Jwt:Key` from `appsettings.json`). All `DashboardController` endpoints carry `[Authorize]`, so every request needs `Authorization: Bearer <token>`. `AuthController` and `HealthController` stay anonymous — you need to be able to log in and to health-check before you have a token.

`AuthBusinessLogic.LoginAsync` checks credentials against the `dbo.Users` table (`Models/User.cs`, via `IUserRepository`) — no third-party auth library involved. Passwords are hashed with `Microsoft.AspNetCore.Identity`'s `PasswordHasher<T>` (PBKDF2, salted; part of the `Microsoft.Extensions.Identity.Core` package, which is just the hashing primitive — not the full ASP.NET Core Identity framework with `UserManager`/`SignInManager`/EF stores, which would be a lot of machinery for one login endpoint). `PasswordHasher.VerifyHashedPassword` does the comparison in constant time internally.

If the username doesn't exist, `AuthBusinessLogic` still runs the hasher against a fixed dummy hash before returning "invalid" — otherwise a real lookup failing fast (no user found → skip hashing) makes response time itself leak which usernames are registered.

### Why JWT and not cookies/sessions

- The API has no server-side session state — it's called by a MAUI desktop/mobile client, not a browser with cookie storage, so bearer tokens in a header are the natural fit.
```
Authorization: Bearer eyJhbGciOi...
```
- Stateless: any instance can validate a token without shared session storage, which matters the moment this API runs behind a load balancer.

### Known limitations (by design, for a demo — not oversights)

1. **`Jwt:Key` lives in `appsettings.json`, checked into git.** Fine for local dev; in a real deployment this must move to User Secrets (dev), environment variables, or Azure Key Vault/AWS Secrets Manager (prod). Never ship a real signing key in source control.
2. **No self-service registration.** There's exactly one seeded row in `dbo.Users` (`database/seed.sql`, username `admin`) — real accounts still have to be inserted by hand. Adding a `POST /api/auth/register` endpoint that calls `PasswordHasher<T>.HashPassword` and inserts through `IUserRepository` is a small, natural addition on top of what's here now.
3. **No refresh tokens.** The token just expires (`Jwt:ExpiryMinutes`, default 60) and the client re-logs in — see `AuthTokenHandler` on the MAUI side. A refresh-token flow would avoid re-sending the password, at the cost of a second token type and a revocation story.
4. **No role/claim-based authorization.** Everything with a valid token can hit every dashboard endpoint. `[Authorize(Roles = "Admin")]` plus a `role` claim (and column) on `User` is the natural next step once there's more than one type of user.

### Client side (MAUI app)

The MAUI app doesn't have a login screen — `Services/AuthTokenHandler.cs` is an `HttpMessageHandler` that logs in against the seeded `admin` account on first use, caches the token, and re-logs in a minute before it expires. The moment there's a real registration flow, the client's login call is the only thing that needs a UI in front of it.

## Reliability features

- `AsNoTracking()` on all read-only repository queries — skips EF's change-tracking overhead for data that's never updated in the same request.
- `EnableRetryOnFailure()` on the `DbContext` — transient SQL faults (network blip, deadlock) retry automatically instead of surfacing as a 500 on the first hiccup.
- Centralized `UseExceptionHandler` in `Program.cs` — any exception that escapes a controller becomes a consistent JSON error shape, and only leaks the exception message in `Development`.
- `/health` calls `Database.CanConnectAsync()` — a green health check means the database is actually reachable, not just that the process is alive.

## Why this is a separate repo from the MAUI app

This backend started inside the MAUI app's repo as a Dapper-based Minimal API, then got rewritten into the current layered architecture with EF Core, then was split out entirely (`git subtree split`, history preserved) once it became clear it's a separately deployable, separately versioned concern — the MAUI app is *a* client, not *the* app. Splitting means this API can be deployed, scaled, and given its own release cadence without coupling to mobile app store review cycles, and could serve a web or additional mobile client later without any shared source, only the HTTP contract documented in the root `README.md`.
