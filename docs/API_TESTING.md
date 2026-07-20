# Testing the API (Swagger walkthrough)

Step-by-step for validating the API is working end-to-end: get a token, authorize with it, call a protected endpoint. Covers Swagger UI, curl, and Postman.

## Prerequisite

The API has to be running first — see [RUNNING_IN_VISUAL_STUDIO.md](RUNNING_IN_VISUAL_STUDIO.md) or `RUN.md` at the repo root. Confirm it's actually up before anything else:

```powershell
curl http://localhost:5199/health
# expect: {"status":"ok","database":"connected"}
```

If that fails, nothing below will work — fix that first (see the Troubleshooting table in `RUNNING_IN_VISUAL_STUDIO.md`).

## Option A — Swagger UI (easiest)

Open **http://localhost:5199/swagger** in a browser.

### 1. Get a token

1. Find `Auth` → `POST /api/auth/login` and click **Try it out**.
2. Replace the request body with the seeded demo account:
   ```json
   {
     "username": "admin",
     "password": "ChangeMe123!"
   }
   ```
3. Click **Execute**.
4. In the **Response body**, copy the value of `"token"` — the long `eyJhbGciOi...` string. Copy just the string content, not the surrounding quotes.

If this returns `401` instead of a token: the database probably wasn't seeded (or was seeded before the `Users` table existed) — re-run `database/seed.sql` (see `RUN.md`).

### 2. Authorize Swagger with the token

1. Scroll to the top of the page and click the green **Authorize** button (padlock icon, above all the endpoint groups — not the padlock on an individual endpoint).
2. Paste the token into the **Value** field under `Bearer`.
   - Paste only the raw token. Do **not** type `Bearer ` in front of it — Swagger adds that prefix itself. Pasting `Bearer eyJ...` (with the word "Bearer" included) will fail authorization.
3. Click **Authorize**, then **Close**.
4. Every endpoint's padlock icon should now show as locked, and every request Swagger sends from here on carries the header automatically.

### 3. Call a protected endpoint

Try `GET /api/dashboard/cards` → **Try it out** → **Execute**. Expect `200` with the seeded card data. If you skip step 2, this returns `401`.

### Token expiry

The token is valid for `Jwt:ExpiryMinutes` (60 by default, in `appsettings.json`). After it expires, protected calls go back to `401` — repeat step 1 to get a new one, then step 2 to re-authorize (the old **Authorize** entry doesn't refresh itself).

## Option B — curl

```powershell
# 1. Log in, capture the token
$token = (curl -s -X POST http://localhost:5199/api/auth/login `
  -H "Content-Type: application/json" `
  -d '{"username":"admin","password":"ChangeMe123!"}' | ConvertFrom-Json).token

# 2. Call a protected endpoint with it
curl -s http://localhost:5199/api/dashboard/cards -H "Authorization: Bearer $token"
```

## Option C — Postman / Insomnia

1. `POST http://localhost:5199/api/auth/login`, body (raw JSON): `{"username":"admin","password":"ChangeMe123!"}`. Copy `token` from the response.
2. On the request(s) you want to call: **Authorization** tab → type **Bearer Token** → paste the token (no `Bearer ` prefix needed here either — Postman adds it for you, same as Swagger).

## Troubleshooting

| Symptom | Cause |
|---|---|
| `401` on `/api/auth/login` itself | Wrong password, or the `Users` table wasn't seeded — re-run `database/seed.sql` |
| `401` on a dashboard endpoint after authorizing | Token expired (60 min default), or you pasted `Bearer <token>` instead of just `<token>` into Swagger's Authorize dialog |
| Swagger's Authorize dialog has no effect | Make sure you clicked the **top-level** Authorize button (padlock above the endpoint list), not a padlock on one specific endpoint |
| `/swagger` itself 404s | The API isn't running, or you're hitting the wrong port — confirm with `GET /health` first |
