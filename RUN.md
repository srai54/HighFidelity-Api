# Running this repo (quick reference)

This is the `demo-in-memory` branch — data is hardcoded in `Repositories/InMemoryDashboardRepository.cs`/`InMemoryUserRepository.cs`, mirroring `database/seed.sql`. **No database setup, no LocalDB, no seed step.** (On `highfidelity-backend`, the SQL-backed branch, see [docs/RUNNING_IN_VISUAL_STUDIO.md](docs/RUNNING_IN_VISUAL_STUDIO.md) for the seeding step this branch skips.)

## Every time — from a terminal

From this folder:

```powershell
run.cmd
```

or, from the parent folder that contains both `HighFidelity-Api` and the frontend repo as siblings:

```powershell
run be
```

Both do the same thing: `dotnet run --project HighFidelity.Api`, kept attached to the terminal (Ctrl+C to stop). It listens on `http://localhost:5199` and opens straight to Swagger (`/swagger`) if the launch profile's browser-launch applies; otherwise open that URL yourself.

## From an editor instead

- **Visual Studio**: open `HighFidelity.Api.slnx`, F5.
- **VS Code**: `.vscode/launch.json` has a "Run HighFidelity.Api" configuration (Run and Debug panel) that also opens the browser to Swagger automatically. This file isn't tracked by git in this repo (`.vscode/` is in `.gitignore`) — it exists locally only.

## Only run one at a time

Whichever way you launch it — terminal, Visual Studio, or VS Code — it binds port 5199. A second instance (from a different tool) will fail with "address already in use"; that's not a bug, it's just the OS refusing two processes on the same port. Stop whichever instance is already running before starting another.
