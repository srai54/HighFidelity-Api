@echo off
setlocal
rem ------------------------------------------------------------------
rem  HighFidelity.Api launcher — starts the backend and keeps it
rem  attached to this terminal (Ctrl+C to stop).
rem ------------------------------------------------------------------
echo.
echo  Starting HighFidelity.Api on http://localhost:5199 ...
echo  Swagger UI will open automatically (see Properties/launchSettings.json).
echo.
dotnet run --project HighFidelity.Api
