# ZenECS.Core.Tests

A comprehensive test suite for **ZenECS Core** runtime, validating all major APIs and features using the custom TestFramework.

## Overview

This test suite provides complete coverage of ZenECS Core functionality without Unity dependencies. Tests use a lightweight test framework (`ZenECS.Core.TestFramework`) that provides deterministic simulation steps and helper methods for common test scenarios.

## Test Framework

The test suite uses a custom framework located in `ZenECS.Core.TestFramework`:

- **TestWorldHost**: Creates a Core-only world and provides deterministic ticking methods
  - `TickFrame(dt, lateAlpha)`: Executes a single frame
  - `TickFixed(fixedDelta)`: Executes a fixed step
  - `TickFullFrame(dt, fixedDelta, fixedSteps, lateAlpha)`: Executes a complete frame with fixed steps

- **WorldTestExtensions**: Helper extension methods for `IWorld`
  - `Apply(Action<ICommandBuffer>)`: Records commands and immediately flushes scheduled jobs
  - `CreateEntity(Action<ICommandBuffer, Entity>?)`: Creates an entity with optional command chaining
  - `FlushJobs()`: Flushes any scheduled jobs and returns the count

## Test Coverage

### Core APIs Tested

1. **System Lifecycle API**
   - System add/remove
   - System enable/disable

2. **Component API** (Basic & Advanced)
   - Add/Replace/Remove components
   - HasComponent, TryReadComponent, ReadComponent
   - SnapshotComponent variants (typed, boxed)
   - GetAllComponents

3. **Entity API**
   - CreateEntity, DestroyEntity
   - IsAlive (by Entity and by ID/Generation)
   - GetAllEntities
   - ID reuse and generation bump

4. **Query API** (IWorldQueryApi)
   - Single and multi-component queries (T1 through T8)
   - Filter combinations (With, Without, WithAny, WithoutAny)
   - Complex filter combinations

5. **QuerySpan API** (IWorldQuerySpanApi)
   - QueryToSpan for entity collection
   - Process for in-place component modification
   - Filter support

6. **Messages API** (IWorldMessagesApi)
   - Subscribe/Publish
   - Message isolation between worlds
   - FIFO message ordering

7. **Singleton Components API** (IWorldSingletonComponent)
   - Set/Get/Remove singleton
   - Singleton violation detection
   - Command buffer support

8. **Snapshot API** (IWorldSnapshotApi)
   - Save/Load full world snapshots
   - Entity and component preservation
   - Post-load migrations

9. **Context API** (IWorldContextApi)
   - Context registration/removal
   - Context lookup
   - Context reinitialization

10. **Binder API** (IWorldBinderApi)
    - Binder attachment/detachment
    - Binder lookup
    - Automatic cleanup on entity destruction

11. **Hook API** (IWorldHookApi)
    - Read/Write permission hooks
    - Type-specific and object-level validators

12. **Worker API** (IWorldWorkerApi)
    - Scheduled job execution

13. **Command Buffer API**
    - Basic operations (CreateEntity, DestroyEntity, AddComponent, etc.)
    - Advanced operations (DestroyAllEntities, SetSingleton, RemoveSingleton)

## Running Tests

### Via Command Line
```bash
dotnet test
```

### Via Visual Studio
- Open Test Explorer (Test → Test Explorer)
- Run all tests or select specific tests

### Via Rider
- Right-click on the test project → Run Tests
- Or use the Unit Tests tool window

## Test Structure

Tests are organized by API surface:
- `*Tests.cs`: Basic API tests
- `*AdvancedTests.cs`: Advanced features and edge cases

Each test file focuses on a specific API or feature area, making it easy to locate and maintain tests.

## Notes

- **No Unity Dependencies**: All tests run without Unity, using only the Core runtime
- **Deterministic**: Tests use deterministic simulation steps for reproducible results
- **Isolated**: Each test creates its own world instance via `TestWorldHost`
- **Comprehensive**: Tests cover both happy paths and edge cases (empty worlds, destroyed entities, missing components, etc.)

## Documentation

- **[docs/TEST_COVERAGE.md](./docs/TEST_COVERAGE.md)** - Complete test coverage analysis
- **[docs/TESTING_GUIDE.md](./docs/TESTING_GUIDE.md)** - Comprehensive guide for writing tests, including patterns and examples
- **[docs/BEST_PRACTICES.md](./docs/BEST_PRACTICES.md)** - Best practices for writing and maintaining tests
- **[TestFramework README](../ZenECS.Core.TestFramework/README.md)** - TestFramework API reference

## Contributing

When adding new tests:
1. Follow the existing test structure and naming conventions
2. Use `TestWorldHost` for world creation
3. Use `WorldTestExtensions` helpers to reduce boilerplate
4. Test both success and failure cases
5. Include edge cases (empty collections, null values, destroyed entities, etc.)

For detailed guidance, see:
- [docs/TESTING_GUIDE.md](./docs/TESTING_GUIDE.md) - How to write tests
- [docs/BEST_PRACTICES.md](./docs/BEST_PRACTICES.md) - Best practices and common pitfalls
