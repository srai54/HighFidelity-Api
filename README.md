# HighFidelity.Api

Backend API for the [HighFidelity MAUI Dashboard](https://github.com/srai54/HighFidelity-Ui) — a layered ASP.NET Core Web API backed by SQL Server via EF Core.

This repo was split out of the MAUI app's monorepo once the backend became its own deployable concern; see [History](#history) below for why and how.

## Architecture

```
Controllers/    → HTTP in/out only. Parses requests, calls BusinessLogic, formats responses.
BusinessLogic/  → BL layer: validation, orchestration (Controller → BusinessLogic).
Repositories/   → EF Core data access only (BusinessLogic → Repository).
Data/           → AppDbContext — EF Core model configuration.
Models/         → EF Core entities, mirror the SQL tables.
DTOs/           → Wire contracts returned to clients (never the raw entities).
Mappings/       → Manual entity → DTO conversion.
Configuration/  → Strongly-typed options bound from appsettings.json (Jwt).
Migrations/     → EF Core schema history.
```

Each arrow is an interface (`IDashboardBusinessLogic`, `IDashboardRepository`), so every layer is independently testable and swappable. See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the full why/what, and [docs/INTERVIEW_QUESTIONS.md](docs/INTERVIEW_QUESTIONS.md) for a Q&A walkthrough of the design decisions.

## Endpoints

All `/api/dashboard/*` endpoints require a JWT — see [Authentication](#authentication) below.

| Endpoint | Description |
|---|---|
| `POST /api/auth/login` | Exchange username/password for a JWT |
| `GET /api/dashboard/cards` | Summary KPI cards |
| `GET /api/dashboard/revenue-cards` | Revenue analytics cards |
| `GET /api/dashboard/activities` | Activity timeline |
| `GET /api/dashboard/orders` | All orders |
| `POST /api/dashboard/orders` | Create an order (DB assigns Id + Invoice) |
| `DELETE /api/dashboard/orders?ids=1&ids=2` | Bulk delete by Id |
| `GET /api/dashboard/traffic` | Traffic source distribution |
| `GET /health` | Readiness probe — checks actual DB connectivity |

Interactive docs: `GET /swagger` (Swagger UI, on in every environment).

## Authentication

`POST /api/auth/login` with the demo account (a seeded row in `dbo.Users`, default `admin` / `ChangeMe123!` — see `database/seed.sql`) returns a JWT. Send it as `Authorization: Bearer <token>` on every `/api/dashboard/*` call. Full design rationale and known limitations are in [docs/ARCHITECTURE.md#authentication](docs/ARCHITECTURE.md#authentication).

## Running locally

See [docs/RUNNING_IN_VISUAL_STUDIO.md](docs/RUNNING_IN_VISUAL_STUDIO.md) for the full Visual Studio walkthrough (seeding the DB, running both this API and the MAUI client, logging in). Quick version:

```powershell
# 1. Seed the database (SQL Server / LocalDB)
sqlcmd -S "(localdb)\MSSQLLocalDB" -d HighFidelity -i database\seed.sql

# 2. Run the API
dotnet run --project HighFidelity.Api
```

Default connection string (`HighFidelity.Api/appsettings.json`) targets `(localdb)\MSSQLLocalDB`, database `HighFidelity`, port `5199`. Override via `ConnectionStrings:HighFidelity` for a different SQL Server instance.

## Reliability features

- `AsNoTracking()` on all read-only queries
- `EnableRetryOnFailure()` for transient SQL faults
- Centralized exception handling → consistent JSON error responses
- `/health` genuinely checks `Database.CanConnectAsync()`, not just process liveness

## Consumers

The [MAUI dashboard app](https://github.com/srai54/HighFidelity-Ui) is the current client, talking to this API over HTTP/JSON via `IDashboardDataService` — no shared source, only the HTTP contract above.

## History

This backend started as a Dapper-based Minimal API inside the MAUI app's repo, was rewritten into this layered Controller/BusinessLogic/Repository architecture with EF Core, and was then split into its own repo so it can be versioned, deployed, and (eventually) consumed by more than one client independently of the mobile app's release cycle. Git history from the original monorepo commits is preserved here via `git subtree split`.
