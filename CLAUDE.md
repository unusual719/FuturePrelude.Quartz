# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FuturePrelude.Quartz is an ASP.NET Core 10 Web API hosting the Quartz distributed job scheduler with JWT authentication. It provides HTTP endpoints to manage job scheduling with support for multiple database providers (MySQL, SQL Server, PostgreSQL, SQLite) and Redis for distributed caching/locking.

## Build Commands

- `dotnet restore` — restore NuGet packages
- `dotnet build FuturePrelude.Quartz.slnx -c Debug` — compile solution (use `-c Release` for deployments)
- `dotnet run --project src/FuturePrelude.Quartz/FuturePrelude.Quartz.csproj` — run locally
- `dotnet test FuturePrelude.Quartz.slnx` — run all xUnit tests
- `dotnet test FuturePrelude.Quartz.slnx /p:CollectCoverage=true` — run with coverage

## Architecture

**Middleware Pipeline (in order):**
1. `RequestBodyCaptureMiddleware` — captures request body for logging/exception handling
2. `NLogRequestIdMiddleware` — adds trace/request ID to log context
3. `RequestTimingMiddleware` — logs request duration

**Global Exception Handling:**
- `GlobalExceptionFilter` — catches unhandled exceptions in Controllers, returns unified `ApiResponse`
- Exception-to-status mapping: `ArgumentNullException` → 400, `UnauthorizedAccessException` → 401, `KeyNotFoundException` → 404, etc.

**Authentication/Authorization:**
- JWT authentication via `AddJwt()` extension
- Custom `JWTAuthorizeHandler` implements `IAuthorizationHandler` for authorization
- `[AllowAnonymous]` on endpoints bypasses authorization

**Database Strategy:**
- **Quartz internal data**: uses Quartz's built-in AdoJobStore (configured via `QuartzStoreOptions.DBStorage`)
- **Business data tables**: FreeSql ORM with `AddFreeSqlDbContext()`
- **Distributed locking/caching**: StackExchange.Redis

**Scheduler:**
- Quartz registered via `AddQuartzScheduler(quartzOptions)`
- `SchedulerService` wraps `IScheduler` (singleton)
- `JobManagementService` handles job lifecycle (scoped)
- JSON serialization for job data via `Quartz.Serialization.Json`

**Response Format:**
All API responses use `ApiResponse<T>` with fields: `code` (0=success), `message`, `data`, `timestamp`, `traceId`, `errors`

## Key Files

| File | Purpose |
|------|---------|
| `Program.cs` | Application startup, service registration, middleware pipeline |
| `Core/ApiResponse.cs` | Unified API response model |
| `Core/JWTAuthentications/` | JWT configuration, options, handler |
| `Core/Quartz/Extensions/QuartzRegistrationServiceCollectionExtensions.cs` | Quartz DI registration |
| `Filters/GlobalExceptionFilter.cs` | Controller exception handling |
| `DependencyInjection/ServiceCollectionExtensions.cs` | Options and FreeSql registration |
| `Services/SchedulerService.cs` | Quartz scheduler wrapper |
| `JOBs/Runtimes/` | Job execution runtimes (HTTP, Plugin) |

## Configuration

- `appsettings.json` → `appsettings.{Environment}.json` → environment variables
- `QuartzStoreOptions` bound from `appsettings.json` section
- `JWTOptions` bound from `appsettings.json` section
- Never commit secrets — use `dotnet user-secrets` or environment variables

## Important Patterns

- Jobs registered with `AddJob<T>()` and triggered via service triggers
- Helper methods for trigger/job conversion should live in `Common/Extensions` or `Common/Helpers`
- `ISchedulerFactory` / `IScheduler` injected via DI
- Use `context.Items["RequestBody"]` to access captured request body in filters/middleware
- Custom authorization requirements should inherit from `IAuthorizationRequirement`

## Solution Structure

```
FuturePrelude.Quartz.slnx
├── src/FuturePrelude.Quartz/     (main API project)
└── tests/FuturePrelude.Quartz.Tests/  (xUnit tests)
```