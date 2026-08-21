# PremPoints

## A trading game for the Premier League built with .NET 10 and Vertical Slice Architecture
	
## Architecture
	This Minimal API project uses Vertical Slice Architecture as described by Jimmy Bogard and Chris Sainty. I chose this to keep features isolated and maintainable.
	Vertical Slice Architecture: https://www.youtube.com/watch?v=oAoaMlS1PWo
	Designing For Change: https://www.youtube.com/watch?v=_1rjo2l17kI&list=RQd2ypmEbUhovjQVoX7dsE_IyuOgc&t=1928s

## Tech Stack & Decisions
	1) Code-First Entity Framework as an ORM, with Sql Server database.
	2) EndpointFilters to add processing to endpoints. Used for cross cutting such as logging, metrics, traces and validation
	3) Errors as values using the Result pattern via Ardalis.Result package, with automatic statuscode mapping via Ardalis.Result.AspNetCore.
	4) MediatR as a message bus and business-logic handler
	5) OpenTelemtry for logging, metrics and traces
	6) DotNet Aspire for local developer experience
	7) Health checks for both aliveness and readiness, for all external dependencies e.g. Sql Server
	8) No CRUD open to players. Reference data has a full CRUD surface, but every
	   write sits behind the Admin policy; the endpoints a player reaches are
	   shaped by what the game needs.

## Testing Strategy
	1) Integration: every endpoint behaviour, through the real HTTP stack, against a
	   throwaway LocalDB database per test. Not Testcontainers — there is no Docker
	   on the dev machine, and CREATE DATABASE on a running LocalDB is far cheaper
	   than starting a SQL Server container per test.
	2) Unit (Validation): Fast tests for FluentValidation logic (covering both success and failure states).
	3) Unit (Domain): Isolated tests for complex business logic (e.g., the PnL Calculator).
	4) Snapshot: integration tests verify the whole HTTP response — status line,
	   headers and body — as one Verify snapshot, so a change to any part of the
	   contract shows up as a reviewable diff instead of slipping past a handful
	   of hand-written assertions. Snapshots live in tests/IntegrationTests/
	   Features/Snapshots.

	   They only work because the suite is deterministic: the clock is a pinned
	   FakeTimeProvider, seeded ids come from TestIds rather than
	   Guid.CreateVersion7, and the only scrubbed values are traceId and the
	   genuinely random ids the API mints at runtime. To accept a deliberate
	   change, review the .received.txt and rename it over the .verified.txt.
	5) Conventions: EndpointConventionTests and ConventionRulesTests read the
	   endpoints the application actually mapped and assert the conventions below.
	   Between them they found twenty-odd violations the day they were written,
	   including four endpoints whose OpenAPI named the wrong response type.
	   EndpointInventoryTests snapshots the whole surface — method, route, policy
	   and declared statuses — so any change to the contract has to be reviewed
	   and accepted rather than merely noticed.

## REST conventions

	Teams is the reference slice. Anything new should look like it.

	The Enforced column is not decoration. These rules were prose only until they
	had already rotted — ten of eighteen endpoints had no rate limiter, four
	declared `TeamDto` as the response type of something that was not a team, and
	eight writes were reachable without authentication. A row marked "prose" is
	one where that can happen again.

	Enforcement levels: **test** — a convention test fails the build.
	**construction** — the API makes the wrong thing hard to write.
	**config** — one global setting, no per-endpoint choice.
	**slice** — covered by that resource's own snapshot tests, which a new slice
	copies. **prose** — nothing checks it.

	| Concern | Convention | Enforced |
	|---|---|---|
	| Versioning | One URL segment, applied once in Program.cs: `/api/v1`. The whole API versions as a unit — shipping `/v2/teams` alone would need endpoints to declare their own version. | test |
	| Rate limiting | Applied to the route group, not per endpoint. An endpoint needing a different budget overrides it. | test |
	| Identity | The opaque id, always, constrained `{id:guid}`. Names are filters, never routes — a name is mutable, so a URL built from one breaks on the first rename. | test |
	| Paging | `PagedResponse<T>` envelope, `?page=&pageSize=`, capped at 100. Requests implement `IPagedRequest`; validators inherit `PagedRequestValidator`. A page size above the cap is refused, never clamped. | test (a collection read may not return a bare array) |
	| Create | 201 with a `Location` header built from the item route via `LinkGenerator`, so the version prefix stays in one place. | test |
	| Delete | 204. Real deletes, no soft-delete flag. 409 if a foreign key still points at the row, with a detail naming what blocked it. | test (204 + 404 declared) |
	| AuthZ | Every write requires authorization. A named policy per endpoint; `Policies.Admin` for reference-data writes. Roles live on the user row, not the WorkOS token, so they are projected onto the principal during token validation. | test (that a policy exists, and that 401/403 are declared — not *which* policy) |
	| OpenAPI | `WithName` and `WithTags` on every endpoint, names unique, at least one 2xx declared, 404 declared on every item route. | test |
	| DTOs | Never expose audit columns or raw foreign keys. That is why `TeamDto` is two fields. | test |
	| Validation | 422, via `.WithValidation<TRequest>()`, which adds the filter and declares the status together so they cannot drift. A malformed body is still a framework 400; that is a different failure. | construction |
	| Errors | RFC 9457 ProblemDetails for every failure, including bare 401/403/404 — see `ResultExtensions` and `UseStatusCodePages`. | construction (if you use `ToApiResult`) |
	| Sorting | `?sort=field` / `?sort=-field` against a `SortMap` allow-list, always with an id tiebreaker. Without it, rows sharing a sort key move between pages — and every row seeded in one `SaveChanges` shares a `CreatedAtUtc`, so ties are the normal case. | construction (if you use `SortMap`) |
	| Enums | Cross the wire as strings, never ints. | config |
	| Update | PUT, full replacement. The uniqueness check excludes the row being updated, so a no-op PUT stays idempotent. | slice |
	| Empty results | 200 with an empty collection. Never 404, never an error. Cannot distinguish "no season covers that date" from "season has no teams" — accepted, since 404 on a collection is worse. | slice |
	| Collections | One read-collection per resource. "Active", "by name" and the rest are query parameters on it, not routes of their own. | prose (the Identity rule blocks name-keyed routes, but nothing counts collections) |
	| Cancellation | Every endpoint, handler and query takes a `CancellationToken` and passes it down. | prose |
	| Dates | `DateOnly` as ISO `yyyy-mm-dd`; instants UTC. The clock is an injected `TimeProvider`, never `DateTime.UtcNow` — including anything derived from it, such as v7 ids. | prose |
	| Concurrency | None, deliberately. Last write wins. One admin edits reference data; reinstate a rowversion and `If-Match` the day that stops being true. | n/a — a decision, not a rule |

### Convention debt

	The rules above are switched on today because the endpoints that break them
	are named in `tests/IntegrationTests/Features/ConventionDebt.cs`, each with a
	reason. Every entry is debt, not permission — a new endpoint cannot join a
	list without someone deliberately adding it, and a test fails if an entry
	outlives the route it excused.

	The largest list is `AnonymousWrite`. Eight endpoints that change game state
	— including trade submission and reseeding a season — are reachable without
	authentication. Unlike the others this is a security decision rather than a
	shape one: closing an entry means choosing which policy guards it.

## Getting Started
	1) Set the database connection string in user-secrets (it holds a password, so it is not committed):
		dotnet user-secrets set "ConnectionStrings:PremPoints" "<string>" --project src/AppHost
		dotnet user-secrets set "ConnectionStrings:PremPoints" "<string>" --project src/Api
	2) Run `aspire run`, or `dotnet run --project src/Api` for the API on its own.

	Every environment shares one Azure SQL server and one credential, and differs only
	in which database it targets. The database name is not a secret and is committed per
	environment as `Database:Name` — PremPointsDev in Development, PremPoints in
	Production — and is swapped into the connection string's Initial Catalog at startup.

	The integration tests do not use any of this. They create a throwaway LocalDB
	database per test and supply their own connection string.

## TODO
	- [ ] Replace the server admin login with a least-privilege SQL user per environment.
	  The API currently connects as the admin account, which can drop the database;
	  it only needs read/write on its own tables. Jira ticket raised.

## This game is an independent fan-made project and is not affiliated with, endorsed by, or connected to the Premier League or any of its clubs.
