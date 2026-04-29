# Repository Guidelines

## Project Structure & Module Organization
- `src/FuturePrelude.Quartz/`: ASP.NET Core 8 Web API hosting the Quartz scheduler. `Controllers` expose HTTP endpoints, `Services` (notably `SchedulerService`) wrap Quartz operations, `Common` holds constants/enums/extensions/helpers, `DTOs` shape request/response payloads, `Entities` map persisted job data, and `Options` binds configuration such as `QuartzSettingsOptions`. Root config files (`appsettings*.json`, `Dockerfile`) live here.
- `tests/FuturePrelude.Quartz.Tests/`: xUnit test project registered in `FuturePrelude.Quartz.slnx`.
- `docs/`, `references/`, `scripts/`: placeholders for documentation, design notes, and automation. Prefer adding new guides or scripts here rather than inside source.

## Build, Test, and Development Commands
- `dotnet restore` — restore NuGet packages.
- `dotnet build FuturePrelude.Quartz.slnx -c Debug` — compile the solution; switch to `-c Release` for deployments.
- `dotnet run --project src/FuturePrelude.Quartz/FuturePrelude.Quartz.csproj` — start the API locally with dependency injection and controllers enabled.
- `dotnet test FuturePrelude.Quartz.slnx` — run all xUnit tests; append `/p:CollectCoverage=true` to emit coverlet coverage.
- Optional: `dotnet format` before commits to normalize style (install dotnet-format if missing).

## Coding Style & Naming Conventions
- Follow C# conventions: PascalCase for types/methods, camelCase for locals/fields, and `Async` suffix for async methods.
- Indentation: 4 spaces; keep braces on new lines per default C# style. Nullable reference types are enabled; avoid disabling unless justified.
- Use DI-friendly constructors, `ILogger<T>` for diagnostics, and keep mappings/strings in `Common` constants/enums to stay DRY.
- Place shared helpers in `Common/Extensions` or `Common/Helpers`; avoid duplicating trigger/job conversion logic across controllers and services.

## Testing Guidelines
- Framework: xUnit with `Microsoft.NET.Test.Sdk`; coverage via coverlet.collector.
- File naming: `{TypeUnderTest}Tests.cs`; test names in the form `Method_Scenario_ExpectedOutcome`.
- Prefer service-level tests against `SchedulerService`, mocking `ISchedulerFactory`/`IScheduler` to verify trigger state and job metadata. Add regression tests for cron validation, trigger detail mapping, and status transitions.

## Commit & Pull Request Guidelines
- Commits: concise, imperative messages (e.g., `Add trigger detail mapping`). Group logical changes; avoid unrelated churn.
- PRs: include a brief summary, linked issues/work items, and test evidence (command outputs or coverage notes). Call out configuration changes (e.g., new `appsettings` keys or `QuartzSettingsOptions` defaults) and schema impacts to downstream consumers.
- Keep diffs focused; prefer small, reviewable PRs aligned with KISS/YAGNI.

## Configuration & Security Tips
- Store environment-specific values in `appsettings.{Environment}.json`; never commit secrets. For local secrets, use `dotnet user-secrets` or environment variables.
- Database/provider selection is driven by `QuartzSettingsOptions.DBType` and `InternalConstants` mappings—validate new providers and connection strings via integration tests before rollout.
