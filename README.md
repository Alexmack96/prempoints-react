# PremPoints

Monorepo: a .NET 10 API and a React client, orchestrated locally by .NET Aspire.

```
├─ client/          React 19 + Vite + Tailwind, TanStack Query, AG Grid
├─ api-dotnet/      .NET 10 API (vertical slices, MediatR, EF Core, SQL Server)
│  ├─ src/Api                 endpoints, domain, infrastructure, migrations
│  ├─ src/AppHost             Aspire orchestration — the F5 entry point
│  ├─ src/ServiceDefaults     OpenTelemetry, health checks, service discovery
│  └─ tests/                  UnitTests, IntegrationTests
├─ aspire.config.json         points the Aspire tooling at the AppHost
└─ .vscode/                   launch + task definitions
```

## Prerequisites

| Tool | Notes |
|---|---|
| .NET SDK 10 | `dotnet --version` should report 10.x |
| SQL Server LocalDB | `sqllocaldb info` should list `MSSQLLocalDB` |
| Bun | the client's package manager — `bun --version` |
| Aspire CLI | `aspire --version` (13.x) — must be ≥ the `Aspire.AppHost.Sdk` version in `AppHost.csproj` |
| `dotnet-ef` | `dotnet tool install --global dotnet-ef --version 10.*` |
| VS Code extension | `microsoft-aspire.aspire-vscode`, plus the C# extension |

## First run

```bash
bun install --cwd client
dotnet restore api-dotnet/PremPoints.slnx
dotnet ef database update --project api-dotnet/src/Api/Api.csproj
```

The last step creates the `PremPoints` database on LocalDB and applies all
migrations. It only needs repeating when new migrations are added.

## Running

Press **F5** in VS Code and pick *Aspire: full stack (API + React + dashboard)*.
That starts the AppHost, which starts the API and the Vite dev server, opens the
Aspire dashboard, and attaches debuggers to both — breakpoints bind in C#
endpoints and in `.tsx` components.

From a terminal instead:

```bash
aspire run
```

| Resource | Address |
|---|---|
| Aspire dashboard | http://localhost:15227 |
| API | http://localhost:5062 |
| Swagger | http://localhost:5062/swagger |
| React client | port assigned by Aspire — see the dashboard |

The API port is pinned; the client's is not. `AddViteApp` allocates it and passes
it as `PORT`, which `client/vite.config.ts` reads.

### How the client reaches the API

The client calls `/api/...` on its own origin. The Vite dev server proxies that
to the API, using the `API_URL` the AppHost injects. Nothing is cross-origin
locally, so CORS is not involved. On the API side every feature endpoint is
mounted under a single `app.MapGroup("api")`.

## Other debug configurations

- **.NET API only (no Aspire)** — the API alone on :5062, connection string from
  `appsettings.Development.json`. No dashboard, no client.
- **Attach to Api (fallback)** — for when the Aspire extension isn't available.

## Tests

```bash
dotnet test api-dotnet/tests/UnitTests/UnitTests.csproj
dotnet test api-dotnet/tests/IntegrationTests/IntegrationTests.csproj
```

Integration tests need LocalDB, not Docker. Each test class creates its own
`PremPointsTests_<guid>` database, migrates and seeds it, then drops it on
teardown; databases older than an hour are swept at start-up in case a run was
killed mid-way.

## Conventions

- **Central package management.** Every version lives in
  `api-dotnet/Directory.Packages.props`; `.csproj` files carry bare
  `<PackageReference Include="..." />` with no `Version`. Shared MSBuild
  properties (target framework, nullability) are in `Directory.Build.props`.
- **Vertical slices.** One folder per use case under `src/Api/Features/<Area>/`,
  holding the request, validator, endpoint and handler for that operation.
- **HTTP locally.** The API only enables HSTS and HTTPS redirection outside
  Development; TLS is the hosting platform's job.
- **Deployment connection strings** go in user secrets
  (`dotnet user-secrets set "ConnectionStrings:PremPoints" "..."` from
  `api-dotnet/src/AppHost`), never in `appsettings.json`.
