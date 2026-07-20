# Interview Prep: Talking Through This Project

Likely questions if this API comes up in a technical interview, with answers grounded in what's actually in this repo (see [ARCHITECTURE.md](ARCHITECTURE.md) for the longer version of any of these).

## Architecture

**Q: Walk me through the layers and why you split them this way.**
Controller → BusinessLogic (BL) → Repository → DbContext → SQL Server, each connected through an interface (`IDashboardBusinessLogic`, `IDashboardRepository`). Controllers only translate HTTP in/out. BL owns validation and orchestration. Repositories own EF Core queries and nothing else. The point is a single place to change for each concern: business rule changes touch `BusinessLogic/`, storage changes touch `Repositories/`, and each layer can be unit tested by mocking the interface below it.

**Q: Isn't this over-engineered for 6 endpoints?**
Yes, honestly — a minimal API could implement this in a third of the files. The layering is there to demonstrate the pattern an enterprise codebase converges to once there's a second entity family, a second consumer, or a team big enough that "who owns this file" matters. For a single-developer dashboard demo, it's a deliberate choice, not a requirement of the domain — good to say this plainly if asked, rather than defending it as necessary.

**Q: Why entities (`Models/`) *and* DTOs, when several fields are identical today?**
To keep the wire contract decoupled from the database schema. If an EF entity later gets an internal-only column (audit fields, a soft-delete flag), returning entities directly from a controller would leak it to clients by accident. The `Mappings/` folder is the one place that decides what crosses that boundary — currently trivial 1:1 mapping, but the seam is what matters, not today's field list.

**Q: What would break first if this had to scale to 10x traffic?**
The single-instance LocalDB setup — there's no connection pooling tuning, no read replicas, no caching layer (dashboard cards/traffic sources are read far more than written, so they're the first candidate for a short-TTL cache). `EnableRetryOnFailure` helps with transient faults, not with an undersized database tier.

## Entity Framework Core

**Q: Why EF Core over Dapper (which the project apparently started with)?**
The README notes this started as a Dapper-based Minimal API. EF Core buys migrations (versioned schema history in `Migrations/`), LINQ query composition, and change tracking — at the cost of some raw-SQL control and a heavier abstraction. For a CRUD-shaped dashboard API, migrations and less hand-written mapping code outweighed Dapper's lower overhead.

**Q: Why `AsNoTracking()` on every read query?**
None of the read endpoints (`cards`, `revenue-cards`, `activities`, `orders`, `traffic`) mutate the entities they fetch in the same request. Change tracking exists to detect and persist mutations — paying that cost on data you're only going to serialize to JSON and discard is pure overhead.

**Q: How do you handle transient database failures?**
`options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(...))` in `Program.cs` — EF Core retries the operation automatically on transient SQL errors (deadlocks, brief network drops) instead of the first blip surfacing as a 500.

## Authentication (JWT)

**Q: Why JWT instead of cookie-based sessions?**
The client is a MAUI desktop/mobile app, not a browser — there's no natural cookie jar to lean on, and a bearer token in an `Authorization` header is the standard fit for a non-browser client talking to a stateless API. Stateless also means no shared session store if this API ever runs as more than one instance behind a load balancer.

**Q: Walk me through what happens on login.**
`POST /api/auth/login` hits `AuthController` → `AuthBusinessLogic.Login`, which compares the submitted username/password against the single `DemoUser` account from config using `CryptographicOperations.FixedTimeEquals` (constant-time, so failed attempts don't leak timing information about how much of the password matched). On success it builds a `JwtSecurityToken` signed with `HmacSha256` using `Jwt:Key`, and returns it with an expiry.

**Q: This only has one hardcoded user — how would you make this production-ready?**
Add a `User` entity + EF migration with a hashed password (e.g. `PasswordHasher<T>` from ASP.NET Core Identity, not raw SHA), an `IUserRepository`, and swap `AuthBusinessLogic`'s dependency from `DemoUserOptions` to that repository. Add `POST /api/auth/register` if self-service signup is needed. This repo intentionally stops short of that to keep the demo's surface area small — see the "Known limitations" list in ARCHITECTURE.md.

**Q: The signing key is sitting in `appsettings.json`, checked into git. Isn't that a problem?**
Yes, for anything beyond local dev. It's fine as a demo default because it's clearly documented as such — the fix is User Secrets locally, environment variables or a secrets manager (Key Vault, AWS Secrets Manager) in any real deployment. A real signing key should never be in source control regardless of environment.

**Q: No refresh tokens — what's the impact?**
The token just expires (`Jwt:ExpiryMinutes`) and the client re-authenticates from scratch — see `AuthTokenHandler` on the MAUI side, which re-logs in a minute before expiry. That means the password gets re-sent periodically instead of exchanging a longer-lived refresh token for a new access token. Fine for a single demo account; the standard fix for a real system is issuing both an access token and a refresh token at login, and only ever sending the password once.

**Q: How would you add role-based authorization?**
Add a `role` claim in `AuthBusinessLogic.Login` (pulled from the eventual `User` entity), then `[Authorize(Roles = "Admin")]` on the endpoints that need it. Today every valid token can hit every dashboard endpoint — there's exactly one user, so there's exactly one role's worth of behavior to distinguish.

## General API design

**Q: Why does `DeleteOrders` take IDs in the query string instead of a request body?**
It's a `DELETE`, and `DELETE` requests conventionally don't carry a body reliably across all HTTP clients/proxies — query parameters (`?ids=1&ids=2`) are the safe, idiomatic way to pass a small set of identifiers for a delete-by-id operation.

**Q: How are errors surfaced to the client?**
Two layers: expected validation failures (`ArgumentException` from BL, e.g. "Customer is required") are caught in the controller and returned as `400 BadRequest` with a message. Anything unexpected is caught by the centralized `UseExceptionHandler` in `Program.cs` and returned as a `500` with a generic message — the real exception detail only appears when `Environment.IsDevelopment()` is true, so production responses never leak stack traces.

**Q: Why split this into its own repo instead of keeping it inside the MAUI app's repo?**
It's a separately deployable, separately versioned concern — the MAUI app is *a* client of this API, not the only possible one. Splitting means the backend can ship on its own release cadence, independent of mobile app store review cycles, and could serve additional clients later through the same HTTP contract without sharing source.
