# HighFidelity.Api

Backend API for the [HighFidelity MAUI Dashboard](https://github.com/srai54/HighFidelity-Ui) — a layered ASP.NET Core Web API backed by SQL Server via EF Core.

> **This is the `demo-in-memory` branch.** Same Controllers/BusinessLogic/JWT/Swagger as `highfidelity-backend` — only the `Repository` layer differs. Data is hardcoded in `InMemoryDashboardRepository`/`InMemoryUserRepository` (mirrors `database/seed.sql` exactly), so there's **no SQL Server, no LocalDB, no seed step** — clone and run. Good for demoing to people who don't want to set up a database first, or when you're offline. Switch to `highfidelity-backend` for the SQL-backed version. See [docs/ARCHITECTURE.md#demo-in-memory-branch](docs/ARCHITECTURE.md#demo-in-memory-branch) for why this is a one-file difference instead of a fork.

This repo was split out of the MAUI app's monorepo once the backend became its own deployable concern; see [History](#history) below for why and how.

## Architecture

```
Controllers/    → HTTP in/out only. Parses requests, calls BusinessLogic, formats responses.
BusinessLogic/  → BL layer: validation, orchestration (Controller → BusinessLogic).
Repositories/   → EF Core data access only (BusinessLogic → Repository).
Data/           → AppDbContext — EF Core model configuration.
Models/         → EF Core entities, mirror the SQL tables — returned directly by dashboard endpoints.
DTOs/           → Wire shapes that genuinely differ from an entity (NewOrderRequest, LoginRequestDto/LoginResponseDto) — not one per entity, see docs/ARCHITECTURE.md.
Configuration/  → Strongly-typed options bound from appsettings.json (Jwt).
Migrations/     → EF Core schema history.
```

Each arrow is an interface (`IDashboardBusinessLogic`, `IDashboardRepository`), so every layer is independently testable and swappable. See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the full why/what, [docs/API_TESTING.md](docs/API_TESTING.md) for how to actually exercise the API (get a token, call protected endpoints), and [docs/INTERVIEW_QUESTIONS.md](docs/INTERVIEW_QUESTIONS.md) for a Q&A walkthrough of the design decisions.

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

`POST /api/auth/login` with the demo account (a seeded row in `dbo.Users`, default `admin` / `ChangeMe123!` — see `database/seed.sql`) returns a JWT. Send it as `Authorization: Bearer <token>` on every `/api/dashboard/*` call. Full design rationale and known limitations are in [docs/ARCHITECTURE.md#authentication](docs/ARCHITECTURE.md#authentication) — for the click-by-click Swagger/curl/Postman steps to actually get and use a token, see [docs/API_TESTING.md](docs/API_TESTING.md).

## Running locally

No database setup on this branch — just:

```powershell
dotnet run --project HighFidelity.Api
```

Listens on port `5199`. (The SQL-backed version's walkthrough — seeding the DB, Visual Studio, JWT login — is [docs/RUNNING_IN_VISUAL_STUDIO.md](docs/RUNNING_IN_VISUAL_STUDIO.md), written for the `highfidelity-backend` branch; the seeding step doesn't apply here, everything else does.)

## Reliability features

- `AsNoTracking()` on all read-only queries
- `EnableRetryOnFailure()` for transient SQL faults
- Centralized exception handling → consistent JSON error responses
- `/health` genuinely checks `Database.CanConnectAsync()`, not just process liveness

## Consumers

The [MAUI dashboard app](https://github.com/srai54/HighFidelity-Ui) is the current client, talking to this API over HTTP/JSON via `IDashboardDataService` — no shared source, only the HTTP contract above.

## History

This backend started as a Dapper-based Minimal API inside the MAUI app's repo, was rewritten into this layered Controller/BusinessLogic/Repository architecture with EF Core, and was then split into its own repo so it can be versioned, deployed, and (eventually) consumed by more than one client independently of the mobile app's release cycle. Git history from the original monorepo commits is preserved here via `git subtree split`.
