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

## REST conventions

	Teams is the reference slice. Anything new should look like it.

	| Concern | Convention |
	|---|---|
	| Versioning | One URL segment, applied once in Program.cs: `/api/v1`. |
	| Identity | The opaque id, always. Names are filters, never routes — a name is mutable, so a URL built from one breaks on the first rename. |
	| Collections | One read-collection per resource. "Active", "by name" and the rest are query parameters on it, not routes of their own. |
	| Paging | `PagedResponse<T>` envelope, `?page=&pageSize=`, capped at 100. A page size above the cap is refused, not clamped, so a caller cannot mispage silently. |
	| Sorting | `?sort=field` / `?sort=-field`, from a per-resource allow-list, always with an id tiebreaker so paging is stable. |
	| Empty results | 200 with an empty collection. Never 404, never an error. |
	| Create | 201 with a `Location` header built from the item route via LinkGenerator. |
	| Delete | 204. Real deletes, no soft-delete flag. 409 if a foreign key still points at the row, with a detail naming what blocked it. |
	| Update | PUT, full replacement. The uniqueness check excludes the row being updated, so a no-op PUT stays idempotent. |
	| Errors | RFC 9457 ProblemDetails for every failure, including bare 401/403/404 — see ResultExtensions and UseStatusCodePages. |
	| Validation | 422, from the FluentValidation endpoint filter. |
	| AuthZ | A named policy per endpoint. `Policies.Admin` for reference-data writes. |

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