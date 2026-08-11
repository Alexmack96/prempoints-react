# PremPoints

## A trading game for the Premier League built with .NET 10 and Vertical Slice Architecture
	
## Architecture
	This React project uses Vertical Slice Architecture to mirror the C# backend. I chose this to keep features isolated and maintainable.
	Vertical Slice Architecture: https://www.youtube.com/watch?v=oAoaMlS1PWo

## Tech Stack & Decisions
	1) React+TypeScript+Vite project initiated from the vite CLI
  2) TanStack Query is used to hit a C# AspNetCore backend for most operations
  3) React Compiler is enabled in this project.
  4) React router is used to replace the basic routing from NextJS.
  5) Ag-grid for the grid capabilities

## Testing Strategy
	1) No Current front-end tests. Plan to use Playwright for end to end of golden endpoints.

## Getting Started
	1) Ensure correct back end is configured in config.json
	2) npm run dev

## React Compiler

The React Compiler is enabled on this template. See [this documentation](https://react.dev/learn/react-compiler) for more information.

## This game is an independent fan-made project and is not affiliated with, endorsed by, or connected to the Premier League or any of its clubs.