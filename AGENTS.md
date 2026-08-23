# Repository Instructions

## Application Scope

- This is a full-stack URL shortener with a React dashboard and an ASP.NET Core API.
- The frontend uses React 19, TypeScript, Vite, and Tailwind CSS 4.
- The backend targets .NET 8 and is organized into Domain, Application, Infrastructure, API, and test projects.
- Link data is intentionally stored in memory. Restarting the backend clears all links.
- The frontend and backend run as separate local processes. Keep their request and response contracts synchronized.

## Sources Of Truth

- Read `README.md` before changing setup, configuration, or HTTP behavior; it defines the API contract and local workflows.
- Read `CONTEXT.md` when changing domain terminology, destination selection, link lifecycle, or access statistics.
- Treat `frontend/package.json`, the .NET project files, and configuration files as the source of truth for tool versions and scripts.

## Project Layout

- `frontend/src/App.tsx`: dashboard UI and user workflows
- `frontend/src/api.ts`: API types, HTTP requests, and API error handling
- `frontend/src/validation.ts`: client-side link and destination validation
- `frontend/src/*.test.ts(x)`: Vitest and Testing Library tests
- `backend/UrlShortener.Domain`: domain entities and lifecycle behavior
- `backend/UrlShortener.Application`: use cases and dependency interfaces
- `backend/UrlShortener.Infrastructure`: in-memory storage, code generation, and registrable-domain adapters
- `backend/UrlShortener.Api`: ASP.NET Core controllers, contracts, configuration, and dependency injection
- `backend/UrlShortener.Api.Tests`: xUnit service and HTTP integration tests

## Backend Rules

- Keep business rules in `UrlShortener.Application` or `UrlShortener.Domain`; controllers translate application outcomes into HTTP responses.
- Keep storage and external adapters in `UrlShortener.Infrastructure` behind Application interfaces.
- Keep configuration in `appsettings.json` and bind values through typed options when they cross into application code.
- Use `ILogger` for operational logging. Return sanitized HTTP errors without exception details.
- Preserve case-sensitive custom aliases, reserved aliases, soft deletion, platform routing, and access-count behavior unless requirements change.
- Platform destinations must remain on the same registrable domain as the default destination. Production validation uses the Public Suffix List and may perform network access when initialized.
- Keep API contracts in `UrlShortener.Api/Contracts` and update frontend types and requests when those contracts change.

## Frontend Rules

- Keep endpoint paths, transport types, JSON handling, and `ApiError` behavior in `frontend/src/api.ts`.
- Keep reusable URL, alias, and registrable-domain validation in `frontend/src/validation.ts`; keep it aligned with backend validation.
- Preserve accessible labels, keyboard interaction, loading states, success/error feedback, and responsive behavior when changing the dashboard.
- Follow the existing visual language and Tailwind/CSS setup rather than introducing an unrelated design system.
- Use `VITE_API_BASE_URL` for environment-specific API locations. Update backend `ShortLink:AllowedOrigins` when supporting a new frontend origin.

## Testing Rules

- Add focused xUnit tests for Domain/Application behavior involving creation, validation, redirects, lifecycle, concurrency, or analytics.
- Add `WebApplicationFactory` integration tests for routes, model binding, status codes, headers, serialization, and multi-endpoint lifecycle behavior.
- Keep API integration tests deterministic by replacing external network adapters and process-wide state with test-owned implementations.
- Add Vitest tests for frontend validation and Testing Library tests for behavior visible to users. Prefer accessible queries and user interactions over implementation details.
- Update both backend integration tests and frontend tests when an API request or response contract changes.
- Cover success, validation, server-error, loading, and empty-state paths that are affected by a change.

## Verification

Run backend commands from the repository root:

```powershell
dotnet restore backend\UrlShortener.sln
dotnet build backend\UrlShortener.sln
dotnet test backend\UrlShortener.sln
```

Show every backend test name and result with:

```powershell
dotnet test backend\UrlShortener.sln --logger "console;verbosity=detailed"
```

Run frontend commands from `frontend/`:

```powershell
npm install
npm run lint
npm test
npm run build
```

For full-stack or API-contract changes, run both backend and frontend verification. For a focused change, run the nearest relevant tests during development and the complete affected suite before finishing.

## Local Development

Run the API from the repository root:

```powershell
dotnet run --project backend\UrlShortener.Api\UrlShortener.Api.csproj --launch-profile dev
```

Run the dashboard from `frontend/`:

```powershell
npm run dev
```
