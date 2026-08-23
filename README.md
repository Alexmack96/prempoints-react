# 1. PremPoints

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

## 1.1. Prerequisites

| Tool | Notes |
|---|---|
| .NET SDK 10 | `dotnet --version` should report 10.x |
| SQL Server LocalDB | `sqllocaldb info` should list `MSSQLLocalDB` |
| Bun | the client's package manager — `bun --version` |
| Aspire CLI | `aspire --version` (13.x) — must be ≥ the `Aspire.AppHost.Sdk` version in `AppHost.csproj` |
| `dotnet-ef` | `dotnet tool install --global dotnet-ef --version 10.*` |
| VS Code extension | `microsoft-aspire.aspire-vscode`, plus the C# extension |

## 1.2. First run

```bash
bun install --cwd client
dotnet restore api-dotnet/PremPoints.slnx
dotnet ef database update --project api-dotnet/src/Api/Api.csproj
```

The last step creates the `PremPoints` database on LocalDB and applies all
migrations. It only needs repeating when new migrations are added.

## 1.3. Running

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

### 1.3.1. How the client reaches the API

The client calls `/api/...` on its own origin. The Vite dev server proxies that
to the API, using the `API_URL` the AppHost injects. Nothing is cross-origin
locally, so CORS is not involved. On the API side every feature endpoint is
mounted under a single `app.MapGroup("api")`.

## 1.4. Other debug configurations

- **.NET API only (no Aspire)** — the API alone on :5062, connection string from
  `appsettings.Development.json`. No dashboard, no client.
- **Attach to Api (fallback)** — for when the Aspire extension isn't available.

## 1.5. Tests

```bash
dotnet test api-dotnet/tests/UnitTests/UnitTests.csproj
dotnet test api-dotnet/tests/IntegrationTests/IntegrationTests.csproj
```

Integration tests need LocalDB, not Docker. Each test class creates its own
`PremPointsTests_<guid>` database, migrates and seeds it, then drops it on
teardown; databases older than an hour are swept at start-up in case a run was
killed mid-way.

## 1.6. Conventions

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

# 2. To Do

[] test onboarding gate for username and favouriote team 
[] Seed season start
[]also on mobile mode, i.e. i tried ctrl+shift+m in my broswer on computer, it should only show one card and a name dropdown selector that flips between themn , and an option to select a second one. quite different from how the web ui is!
[] Seed prices for the screenshot here, the prices table should have a bid and ask, and we should calculate in the c# model the average as being the price every time, never persist it, redundant. make a mapping where names dont match, seems like only man utd and man city
[] on mobile, there should just be one nice card and a team selector that flips between the badgfes/names etc. since not enough real estate to show all 20 
[] display a simple ag-grid with the badges, team name, and price, and current date e.g. today we will seed 20 prices for 21 aug
[] Create differents service account users per application so they can only access their db and cant drop db etc. only admin can.

# 3. Railway: get to prod

The image and `railway.toml` already exist. What follows is what stands between
them and a live deployment, worst first.

## 3.1. Blockers

The code-side blockers are closed. What is left needs a Railway dashboard, a
WorkOS dashboard or a Docker daemon, so none of it can be done from the repo.

[X] **Trust Railway's proxy in `UseForwardedHeaders`.** `KnownNetworks` and
   `KnownProxies` are now cleared in `Program.cs`. They default to loopback,
   which meant Railway's `X-Forwarded-Proto` was dropped, the app read every
   request as HTTP, and `UseHttpsRedirection` bounced it — forever.
[X] **Close `ConventionDebt.AnonymousWrite`.** All seven writes now require
   `Policies.Admin`: `seednewseason`, `seasons`, `seasonPeriods`,
   `teams/activate`, `users/activate`, `users/deactivate`, `trades/type`. The
   debt list is empty and kept that way, so the rule enforces with no
   exemptions.
[X] **Make `POST prices` Admin.** Was merely authenticated, so any signed-in
   player could move the market they were trading against.
[X] **Partition the rate limiter.** 120 a minute is now per caller — internal
   user id, then WorkOS subject, then IP — rather than one global bucket the
   whole league shared.
[] **Decide how Railway reaches the database.** Azure SQL is firewalled by IP
   and Railway's egress IPs are dynamic unless you pay for a static one. Either
   buy the static egress IP and allowlist it, or move the database to Railway
   and change the provider. This is the decision the rest of the deploy waits on.
[] **Set the Railway variables.** `ConnectionStrings__PremPoints` (with
   `Encrypt=True`), and confirm `ASPNETCORE_ENVIRONMENT=Production` from the
   image is what you want. `Database__Name` already comes from
   `appsettings.Production.json`.
[] **Register the Railway origin with WorkOS.** The client's redirect URI is
   its own origin, so the deployed URL has to be a registered redirect URI and
   allowed origin in the WorkOS dashboard, or sign-in fails before the login
   page. The build-arg half is done: the Dockerfile takes
   `VITE_WORKOS_CLIENT_ID` and Vite bakes it in, so pass it at build time if
   production points at a different WorkOS environment. A Railway runtime
   variable of that name would be read by nothing.
[] **Build the image once locally before pushing.** It has never been built —
   there is no Docker on this machine. A first failure inside Railway's build
   logs is a slow way to find a typo.

## 3.2. Before letting anyone else in

[] `/metrics` is mapped in every environment by `MapDefaultEndpoints`, so
   Prometheus scraping is public in prod. Gate it, or move it behind a
   Railway private network.
[] `ReactQueryDevtools` renders unconditionally in `main.tsx` and ships in the
   production bundle. Wrap it in `import.meta.env.DEV`.
[] `Microsoft.OpenApi` 2.3.0 carries a high-severity advisory (GHSA-v5pm-xwqc-g5wc).
   Bump it in `Directory.Packages.props`.
[] Pin Railway to one replica. Migrations run at startup, and two instances
   racing the same migration is how you corrupt a schema. Check migration time
   against `healthcheckTimeout = 60` too — the app does not listen until they
   finish.
[] `AllowedOrigins` in `appsettings.json` still lists `https://my-production-app.com`.
   Same-origin means CORS is barely exercised, which is exactly why a wrong
   value here will go unnoticed.
[] Production logging is `Warning` by default, so an incident leaves you with
   nothing to read. Decide `Information` for the API's own namespace, and set
   `OTEL_EXPORTER_OTLP_ENDPOINT` if traces should go anywhere.
[] Static files are served with no cache headers, so hashed assets are
   re-fetched every visit.

## 3.3. Cleanup

[] `bun run lint` has never run: `eslint.config.js` extends `reactX` and
   `reactDom`, neither imported nor installed.
[] No CI. Nothing runs `dotnet test` or the client build before a deploy.
[] Unused client dependencies: `ag-grid-community`, `ag-grid-react`,
   `@headlessui/react`, `postcss`, `autoprefixer`.
[] `AppSettings:TradingWindow` is read by nothing, and says `America/New_York`
   for a Premier League game.
[] Favicon is still `vite.svg`.
[] Custom domain, if it is not staying on `*.up.railway.app`.
