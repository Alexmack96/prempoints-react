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
	8) Not exposing any CRUD publicly, the API is simply going to expose what the front end needs, and some admin tools.

## Testing Strategy
	1) Integration: Happy path tests for all endpoints using TestContainers to spin up real database instances.
	2) Unit (Validation): Fast tests for FluentValidation logic (covering both success and failure states).
	3) Unit (Domain): Isolated tests for complex business logic (e.g., the PnL Calculator).

## Getting Started
	1) Ensure Docker is running (required for TestContainers/Database).
	2) Update appsettings.json with your connection string.
	3) Run dotnet run.

## This game is an independent fan-made project and is not affiliated with, endorsed by, or connected to the Premier League or any of its clubs.