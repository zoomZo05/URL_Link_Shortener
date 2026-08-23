# URL Link Shortener

URL shortener dashboard and API built with React, TypeScript, Vite, Tailwind CSS, .NET 8, and ASP.NET Core. The application runs locally as two processes:

- Frontend: React/Vite dashboard at `http://localhost:5173`
- Backend: ASP.NET Core API at [https://gul.fy:5001](https://gul.fy:5001) or http://localhost:5000 (we use gul.fy to match the requirements).

Links are stored in memory. Restarting the backend removes all links.

## Requirements

- .NET 8 SDK, or a newer SDK that can target `net8.0`
- Node.js `20.19+` and npm
- Frontend dependencies, including Tailwind CSS, installed with `npm install`
- Network access on the first backend link operation, because the API loads the Public Suffix List for registrable-domain validation

## Install

Run these commands from the repository root.

```powershell
dotnet restore backend\UrlShortener.sln

Set-Location frontend (cd frontend)
npm install
```

## Run Frontend And Backend

Use two terminals, both opened at the repository root.

### Terminal 1: Backend

```powershell
dotnet run --project backend\UrlShortener.Api\UrlShortener.Api.csproj --launch-profile dev
```

Keep this terminal running. The API is available at `https://gul.fy:5001`.

### Terminal 2: Frontend

```powershell
Set-Location frontend (cd frontend)
npm run dev
```

Open `http://localhost:5173` in a browser. The frontend uses `https://gul.fy:5001` as its default API URL. To use a different API URL, set `VITE_API_BASE_URL` before starting Vite:

```powershell
Set-Location frontend (cd frontend)
$env:VITE_API_BASE_URL = "https://gul.fy:5001"
npm run dev
```

If the frontend runs on a different origin, add that origin to `ShortLink:AllowedOrigins` in `backend/UrlShortener.Api/appsettings.json`.

## Verify And Test

Run backend commands from the repository root:

```powershell
dotnet build backend\UrlShortener.sln
dotnet test backend\UrlShortener.sln
```

Run frontend commands from `frontend/`:

```powershell
npm run lint
npm test
npm run build
```

The frontend tests use Vitest and jsdom. The backend tests use xUnit.

## API Contract

API endpoints use JSON for request and response bodies. `{code}` is a generated short code or a case-sensitive custom alias.

### Create a link

`POST /api/links`

```json
{
  "originalUrl": "https://example.com/main",
  "customAlias": "example-main",
  "platformOverrides": {
    "ios": "https://download.example.com/ios",
    "android": "https://download.example.com/android"
  }
}
```

`customAlias` and `platformOverrides` are optional. Without an alias, the API generates a secure Base62 code. Platform destinations must use the same registrable domain as `originalUrl`; sibling subdomains are allowed.

Response: `201 Created`

```json
{
  "id": "guid",
  "shortCode": "example-main",
  "shortUrl": "http://localhost:5000/example-main",
  "originalUrl": "https://example.com/main",
  "platformOverrides": {
    "iosUrl": "https://download.example.com/ios",
    "androidUrl": "https://download.example.com/android"
  },
  "clickCount": 0,
  "isActive": true,
  "createdAtUtc": "2026-08-21T11:37:37Z",
  "lastAccessedAtUtc": null
}
```

Validation and errors:

- `originalUrl` must be an absolute `http` or `https` URL.
- Aliases may contain letters, numbers, hyphens, and underscores.
- Aliases are case-sensitive and cannot be `api`, `swagger`, `health`, or `stats`.
- `400 Bad Request`: invalid URL, alias, or platform destination
- `409 Conflict`: custom alias is already in use
- `503 Service Unavailable`: a unique generated code could not be created

### List links

`GET /api/links`

Returns `200 OK` with an array of link objects. Deleted links are excluded.

### Get statistics

`GET /api/links/{code}/stats`

Returns `200 OK` with the link object, including `clickCount`, `createdAtUtc`, and nullable `lastAccessedAtUtc`. Returns `404 Not Found` for an unknown or deleted code.

### Enable or disable a link

`PATCH /api/links/{code}/status`

```json
{
  "isActive": false
}
```

Returns `204 No Content` on success or `404 Not Found` for an unknown/deleted code. A disabled link is retained and can be re-enabled.

### Delete a link

`DELETE /api/links/{code}`

Returns `204 No Content` on success or `404 Not Found` for an unknown code. Deletion is soft: the record remains in memory for historical purposes, is excluded from listing, and its redirect returns `404 Not Found`.

### Redirect

`GET /{code}`

Returns `302 Found` with a `Location` header. The API examines the request `User-Agent` and selects an iOS destination, Android destination, or the default `originalUrl`. A successful redirect increments `clickCount` and updates `lastAccessedAtUtc`.

- Unknown or deleted code: `404 Not Found`
- Disabled link: `410 Gone`, without incrementing statistics

Example API call after creating a link:

```powershell
$body = @{ originalUrl = "https://example.com" } | ConvertTo-Json
$link = Invoke-RestMethod -Method Post -Uri "http://localhost:5000/api/links" `
  -ContentType "application/json" -Body $body

curl.exe -i "http://localhost:5000/$($link.shortCode)"
```

## Configuration

Backend configuration is in `backend/UrlShortener.Api/appsettings.json`:

```json
{
  "ShortLink": {
    "BaseUrl": "http://localhost:5000",
    "CodeLength": 8,
    "AllowedOrigins": ["http://localhost:5173"]
  }
}
```

- `BaseUrl` controls the `shortUrl` returned by the API.
- `CodeLength` controls generated short-code length.
- `AllowedOrigins` controls backend CORS access.

## Project Layout

- `backend/UrlShortener.Domain`: entities and domain behavior
- `backend/UrlShortener.Application`: use cases and interfaces
- `backend/UrlShortener.Infrastructure`: in-memory repository, code generation, and domain policy
- `backend/UrlShortener.Api`: HTTP controllers, configuration, and dependency injection
- `backend/UrlShortener.Api.Tests`: xUnit backend tests
- `frontend/src`: React dashboard, API client, validation, and Vitest tests

## Persistence Decision

If the application needs durable data, multi-instance deployment, or production use, replace the infrastructure implementation with a database-backed `IUrlRepository` and add migrations, connection-string configuration, and persistence/integration tests. The application and API layers are already separated from the storage implementation for that change.

AI-log link - https://drive.google.com/file/d/1GBRyyGQ-vFwMUmzc0A_anPZH-IBUjYvD/view?usp=drive_link
