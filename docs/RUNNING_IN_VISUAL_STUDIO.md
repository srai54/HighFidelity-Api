# Running HighFidelity.Api from Visual Studio

## Prerequisites

- Visual Studio 2022 (17.12+) with the **ASP.NET and web development** workload
- SQL Server LocalDB (installed automatically with the workload above, or via SQL Server Express)
- .NET 10 SDK (preview) — `dotnet --version` should report `10.0.x`

## 1. Open the solution

Open `HighFidelity.Api.slnx` in Visual Studio (double-click it, or **File → Open → Project/Solution**).

## 2. Seed the database

The API expects a `HighFidelity` database on `(localdb)\MSSQLLocalDB`. Seed it once before the first run:

- **Visual Studio**: open **View → SQL Server Object Explorer**, connect to `(localdb)\MSSQLLocalDB`, then open `database/seed.sql` and click **Execute**.
- **Or from a terminal** (Tools → Command Line → Developer PowerShell in VS also works):
  ```powershell
  sqlcmd -S "(localdb)\MSSQLLocalDB" -d HighFidelity -i database\seed.sql
  ```

This creates the five tables (`DashboardCards`, `RevenueCards`, `Activities`, `Orders`, `TrafficSources`) and inserts demo rows. It's idempotent — re-running it just drops and recreates the tables.

## 3. Set the startup project

Right-click **HighFidelity.Api** in Solution Explorer → **Set as Startup Project** (there's only one project, so this is usually already the case).

## 4. Run

Press **F5** (or **Ctrl+F5** to run without the debugger attached). Visual Studio will:

1. Restore NuGet packages
2. Build the project
3. Launch Kestrel on `http://localhost:5199` (see `appsettings.json` → `Urls`)
4. Open a browser to the Swagger/`/health` endpoint if the launch profile is configured to do so

You should see console output confirming the app is listening on port 5199. Hit `http://localhost:5199/health` to confirm it can reach the database — it should return `200 OK` with a JSON body, not just process liveness.

## 5. Get a JWT and call a protected endpoint

Dashboard endpoints require a Bearer token (see [ARCHITECTURE.md](ARCHITECTURE.md#authentication)). Log in with the demo account first:

```http
POST http://localhost:5199/api/auth/login
Content-Type: application/json

{ "username": "admin", "password": "ChangeMe123!" }
```

The response contains a `token` — pass it as `Authorization: Bearer <token>` on subsequent requests, e.g. `GET /api/dashboard/cards`.

Demo credentials live in `appsettings.json` under `DemoUser` — change them there for your own environment.

## 6. Running the MAUI dashboard against this API

The [MauiHighFidelityDashboard](https://github.com/srai54/-MauiHighFidelityDashboard) app is a separate repo/solution and is the actual consumer of this API:

1. Clone it alongside this repo.
2. Make sure `HighFidelity.Api` is already running (steps above) — the MAUI app has no embedded data and shows nothing until the API responds.
3. Open `MauiHighFidelityDashboard.sln` in Visual Studio, set the MAUI project as startup, pick a target (Windows Machine is the simplest for local dev), and press F5.
4. The app logs in automatically against the same demo account on startup (see `Services/AuthTokenHandler.cs`) and attaches the JWT to every API call — no manual login step needed on the client.

## Troubleshooting

| Symptom | Fix |
|---|---|
| `Connection string 'HighFidelity' is missing` on startup | Check `appsettings.json` → `ConnectionStrings:HighFidelity` |
| `/health` returns 500 | LocalDB instance isn't running or the `HighFidelity` database doesn't exist yet — re-run `seed.sql` |
| `401 Unauthorized` on dashboard endpoints | You're missing the `Authorization: Bearer <token>` header — log in via `/api/auth/login` first |
| MAUI app shows empty lists / "Failed to load..." errors | The API isn't running, or is running on a different port than `Services/ApiSettings.cs` expects |
