# One image serving both halves: the .NET API, with the built React app in its
# wwwroot. Same origin, so there is no CORS to configure and no second service
# to deploy or keep in step.

# Stage 1: the React client.
FROM oven/bun:1 AS client
WORKDIR /src/client
# Manifest first, so the dependency layer is only rebuilt when dependencies
# actually change rather than on every source edit.
COPY client/package.json client/bun.lock ./
RUN bun install --frozen-lockfile
COPY client/ ./
# Vite inlines VITE_* variables at build time, so this has to arrive here rather
# than as a Railway runtime variable — one set on the service would be read by
# nothing. Left unset, the client falls back to the client id committed in
# authConfig.ts, which is correct while every environment shares one WorkOS
# environment. Point production at a different one and this is the seam.
ARG VITE_WORKOS_CLIENT_ID
ENV VITE_WORKOS_CLIENT_ID=${VITE_WORKOS_CLIENT_ID}
RUN bun run build

# Stage 2: publish the API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
# Central package management means restore needs both props files, and it needs
# them before the source for the restore layer to cache.
COPY api-dotnet/Directory.Build.props api-dotnet/Directory.Packages.props ./api-dotnet/
COPY api-dotnet/src/Api/Api.csproj ./api-dotnet/src/Api/
COPY api-dotnet/src/ServiceDefaults/ServiceDefaults.csproj ./api-dotnet/src/ServiceDefaults/
RUN dotnet restore api-dotnet/src/Api/Api.csproj
COPY api-dotnet/ ./api-dotnet/
# AppHost is deliberately absent: it orchestrates local development and has no
# job inside a deployed container.
RUN dotnet publish api-dotnet/src/Api/Api.csproj -c Release -o /app/publish --no-restore

# Stage 3: runtime.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=api /app/publish ./
COPY --from=client /src/client/dist ./wwwroot

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Railway assigns the port at runtime through PORT, so it cannot be baked in.
# The 8080 default keeps `docker run` working locally without one.
CMD ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} dotnet Api.dll"]
