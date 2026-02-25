# AGENTS.md

## Cursor Cloud specific instructions

### Project overview

ZenECS is a pure C# Entity-Component-System framework. It is a **library**, not a running application — there are no web servers, databases, or Docker services.

- **Core library** (`src/ZenECS.Core.csproj`): targets .NET Standard 2.1, zero external dependencies
- **Unity adapter** (`src/ZenECS.Adapter.Unity.csproj`): optional Unity integration (requires Unity stubs, not buildable in CI)
- **Tests** (`Packages/com.zenecs.core/Tests~/ZenECS.Core.Tests/`): xUnit 2.9.0, targets .NET 8.0
- **Samples** (`Packages/com.zenecs.core/Samples~/01-Basic/` through `12-Binding/`): console apps, targets .NET 8.0

### Prerequisites

- **.NET SDK 8.0+** must be installed. The update script handles NuGet restore only; SDK installation is done once during initial VM setup.

### Common commands

| Task | Command |
|------|---------|
| Restore | `dotnet restore src/ZenECS.Core.sln` |
| Build (all) | `dotnet build src/ZenECS.Core.sln` |
| Test | `dotnet test src/ZenECS.Core.sln` |
| Run a sample | `dotnet run --project Packages/com.zenecs.core/Samples~/01-Basic/ZenEcsCoreSamples-01-Basic.csproj` |
| Benchmarks | `dotnet run --project Packages/com.zenecs.core/Benchmarks~/ZenECS.Core.Benchmarks.csproj -c Release` |

### Caveats

- The sample console apps use `Console.KeyAvailable` for interactive loops. In headless/CI environments they will crash after initialization — this is expected and does not indicate a framework bug.
- The Unity adapter project (`ZenECS.Adapter.Unity.csproj`) requires Unity assembly stubs and is **not included in the solution file**. It cannot be built in this environment. Focus on `ZenECS.Core.sln` for all development work.
- There is no linter configured (no `.editorconfig` enforcement, no `dotnet format` CI step). Code style is enforced by convention per `Docs/community/contributing.md`.
