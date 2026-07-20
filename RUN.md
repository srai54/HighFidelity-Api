# Running this repo (quick reference)

For the full walkthrough (seeding the DB, Visual Studio, JWT login) see [docs/RUNNING_IN_VISUAL_STUDIO.md](docs/RUNNING_IN_VISUAL_STUDIO.md). This file is just the fast path.

## One-time setup

Seed the database (only needed once, or after pulling a schema change):

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d HighFidelity -i database\seed.sql
```

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
