# Repository Instructions

## Project Scope

- The current implementation is backend-only. Do not add frontend work unless the user explicitly requests it.
- The backend targets .NET 8 and is organized into Domain, Application, Infrastructure, API, and test projects.
- Link data is intentionally stored in memory. A process restart clears all links.

## Project Layout

- `backend/UrlShortener.Domain`: domain entities and domain behavior
- `backend/UrlShortener.Application`: use cases and dependency interfaces
- `backend/UrlShortener.Infrastructure`: in-memory repository and code generation adapters
- `backend/UrlShortener.Api`: ASP.NET Core controllers, configuration, and dependency injection
- `backend/UrlShortener.Api.Tests`: backend tests
- `README.md`: reviewer setup and endpoint usage

## Verification Commands

Run these commands from the repository root:

```powershell
dotnet restore backend\UrlShortener.sln
dotnet build backend\UrlShortener.sln
dotnet test backend\UrlShortener.sln
```

Run the API locally with:

```powershell
dotnet run --project backend\UrlShortener.Api\UrlShortener.Api.csproj --urls http://localhost:5000
```

Read `README.md` for the endpoint contract and PowerShell examples before changing the API behavior.

## Implementation Rules

- Keep business rules in `UrlShortener.Application` or `UrlShortener.Domain`, not in controllers.
- Keep storage and external adapters in `UrlShortener.Infrastructure` behind Application interfaces.
- Keep configuration in `appsettings.json` and bind it through typed options when it crosses into application code.
- Use `ILogger` for operational logging; keep structured logs on the configured provider and do not expose exception details in HTTP responses.
- Preserve case-sensitive custom aliases and soft-delete behavior unless the user changes the requirements.
- Add or update focused tests for business behavior when changing link creation, validation, redirection, lifecycle, or analytics.
