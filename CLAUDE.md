# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Switchly is a multi-tenant feature-flag platform built as a single .NET 9 ASP.NET Core Web API (`Switchly-2.0.WebApi`) backed by PostgreSQL. There is no client/SPA in this repo — only the backend.

## Common commands

Build / run the API locally (requires .NET 9 SDK and a Postgres instance reachable at `localhost:5432`):

```bash
dotnet restore
dotnet build
dotnet run --project Switchly-2.0.WebApi
```

Bring up the full stack (Postgres + API + Jaeger) via Docker Compose — this is the easiest way to run end-to-end:

```bash
docker compose up -d --build
# API:    http://localhost:8080
# Jaeger: http://localhost:16686
# Postgres exposed on 5432 (test_user / test_user / db: switchly)
```

EF Core migrations (run from repo root; the design-time host is the WebApi project):

```bash
dotnet ef migrations add <Name> --project Switchly-2.0.WebApi
dotnet ef database update   --project Switchly-2.0.WebApi
```

Note: there is no test project and no linter configured. `dotnet build` is the only correctness gate.

Deployment is automated via `.github/workflows/deploy.yml`: pushing to the `prod` branch SSHes into a Hetzner host and runs `git reset --hard origin/prod && docker compose up -d --build`. Do not push to `prod` casually.

## Architecture

### Vertical slices (Carter + MediatR)

Each use case lives in `Switchly-2.0.WebApi/Features/<Domain>/<UseCase>/` as a pair of files:

- `*Endpoint.cs` — a `CarterModule` that maps an HTTP route, parses the request DTO, builds the MediatR command/query, dispatches it, and returns `Results.Ok(res)` / `Results.BadRequest(res)` based on `Response<T>.Success`.
- `*Handler.cs` — declares the `IRequest<Response<T>>` record (the command/query), the response DTO, optionally a `AbstractValidator<T>`, and the `IRequestHandler` implementation.

Endpoints are auto-discovered via `app.MapCarter()` and handlers/validators are auto-registered by scanning the executing assembly in `Program.cs`. To add a new use case, just drop a new folder under `Features/<Domain>/` following this pattern — no manual wiring needed.

Naming quirks to preserve: the existing tree uses `IRequest`-typed records named `*CommandHandler` / `*Request` interchangeably (e.g. `CreateFlagCommandHandler` is the *command record*, not the handler). The actual handler class sometimes has a copy-pasted name (e.g. `CreateOrganizationHandler` lives inside `CreateFlagCommandHandler.cs` and handles flag creation). Match local conventions when extending; don't rename existing types just to "fix" them.

### Cross-cutting pipeline

- `Behaviors/ValidationBehavior.cs` is registered as an open-generic `IPipelineBehavior<,>` and runs every FluentValidation `AbstractValidator<TRequest>` it can find before the handler. Validation failures throw `ValidationException` — there is currently no exception-to-HTTP mapper, so handlers also defensively return `Response<T>.Fail(...)` for business errors and throw `UnauthorizedAccessException` / `InvalidOperationException` for auth/not-found cases.
- `Models/Common/Response.cs` (`Response<T>` with `Success`/`Message`/`Data`/`Errors`) is the standard envelope returned by every handler. New endpoints should return it.

### Data layer

- `Context/SwitchlyDbContext.cs` is the single `DbContext`. All `IEntityTypeConfiguration<T>` classes live in `Context/Configurations/` and are applied via `ApplyConfigurationsFromAssembly`. Add new entity config there rather than using fluent calls inline.
- `Context/Seed/` provides demo data wired in through `modelBuilder.ApplySeed()` inside `OnModelCreating` — seed changes flow through migrations, not runtime seeding.
- `Context/Migrations/` holds the EF migration history. Schema changes require a new migration; do not hand-edit existing ones.

### Domain model

The core hierarchy is: `Organization` → `Project` → `ProjectEnvironment` (and `ProjectSetting` / `ProjectSettingValue`) → `FeatureFlag` → `FeatureFlagEnvironment` → `FeatureFlagSegmentTargeting` → `SegmentGroup` → `SegmentRule` (+ `Variant` for multivariant flags). `OrganizationMember` is the join table that drives authorization. When creating a flag, an entry is fan-out-created in `FeatureFlagEnvironments` for every environment of the project (see `CreateFlag` for the canonical pattern).

Public flag evaluation (`POST /api/flag/evaluate`) is the read-path used by SDKs: it resolves `PublicKey` (organization) → `ProjectKey` → `EnvironmentKey` → `FlagKey`, then evaluates segment-targeting rules against caller-supplied `Traits`, falling back to the env-level `IsEnabled` flag. This endpoint is unauthenticated by design — everything else uses `.RequireAuthorization()`.

### Auth

`Extensions/ServiceCollectionAuthExtensions.AddAuth` configures JWT bearer auth using the `Jwt` config section. `Auth/JwtTokenGenerator` issues tokens carrying `sub` (userId) and one `org` claim per organization in the form `"<orgId>:<role>"`. `Auth/UserContext` (scoped, injected into handlers as `IUserContext`) parses those claims at request time and exposes `UserId`, `Organizations`, `IsInRole`, `IsOwner`. Membership/role checks in handlers should query `OrganizationMembers` (authoritative) or use `IUserContext` (claim-cached).

JWT signing key, issuer, audience, and `AccessTokenMinutes` come from `appsettings.Development.json` (the committed dev secret is fine for local; never reuse in prod). The connection string lives under `ConnectionStrings:Database` and is overridden in compose via `ConnectionStrings__Database`.

### Observability

OpenTelemetry tracing is configured in `Program.cs` with both Console and OTLP exporters; the Jaeger container in `compose.yaml` listens on the default OTLP ports (4317/4318) and serves the UI on 16686. No metrics/logs pipeline yet.

## Conventions to follow

- Keep handler files self-contained: command record, response DTO, validator, and handler all in the same `*Handler.cs`.
- Use `Response<T>.Ok(...)` / `Response<T>.Fail(...)` rather than throwing for expected business errors; reserve exceptions for auth and "should never happen" cases.
- Prefer `AsNoTracking()` for read-only queries (the existing read paths do this).
- Comments and validation messages in the codebase mix Turkish and English — don't churn existing strings; match the surrounding language when editing nearby code.
