# ZenECS Core Test Coverage Analysis

## Current Test Files

1. **SystemLifecycleAndEnableTests.cs**
   - ✅ System add/remove
   - ✅ System enable/disable

2. **ExternalCommandTests.cs**
   - ✅ External command flush

3. **WorldComponentApiTests.cs**
   - ✅ Component add/replace/remove
   - ✅ HasComponent, TryReadComponent, ReadComponent

4. **WorldResetAndGenerationTests.cs**
   - ✅ Entity ID reuse
   - ✅ Generation bump
   - ✅ World.Reset()

5. **WorldAliveCountTests.cs**
   - ✅ AliveCount tracking
   - ✅ AliveCount after Reset

6. **WorldSnapshotTests.cs**
   - ✅ Snapshot save/load
   - ✅ Entity and component preservation
   - ✅ Generation preservation
   - ✅ FreeIds preservation
   - ✅ NextId preservation
   - ✅ Empty World snapshot
   - ✅ Exception handling when formatter is missing
   - ✅ Post-load migration

7. **WorldQueryTests.cs**
   - ✅ Basic queries (Query<T1>, Query<T1, T2>, Query<T1, T2, T3>, Query<T1, T2, T3, T4>)
   - ✅ Filter.With<T>() - additional component filter
   - ✅ Filter.Without<T>() - exclusion component filter
   - ✅ Filter.WithAny() - OR group filter
   - ✅ Filter.WithoutAny() - NOT-OR group filter
   - ✅ Complex filter combinations
   - ✅ Multi-component queries (T1..T8 all tested)
   - ✅ Empty result queries
   - ✅ Query updates after component changes
   - ✅ Multi-component queries + filter combinations

8. **WorldMessagesTests.cs**
   - ✅ Subscribe<T>() - message subscription
   - ✅ Publish<T>() - message publishing
   - ✅ Unsubscribe (IDisposable)
   - ✅ Multiple subscribers
   - ✅ Message isolation between Worlds
   - ✅ Message type isolation
   - ✅ Message FIFO order
   - ✅ Multiple pump cycles
   - ✅ Empty queue handling

9. **WorldSingletonTests.cs**
   - ✅ SetSingleton<T>() - singleton setup
   - ✅ GetSingleton<T>() - get singleton
   - ✅ TryGetSingleton<T>() - try get singleton
   - ✅ RemoveSingleton<T>() - remove singleton
   - ✅ HasSingleton(Entity) - check if entity owns singleton
   - ✅ GetAllSingletons() - get all singletons
   - ✅ Singleton violation detection (same singleton component on multiple entities)
   - ✅ Update via SetSingleton
   - ✅ SetSingleton/RemoveSingleton via CommandBuffer
   - ✅ Singleton index update on entity destruction

10. **WorldQuerySpanTests.cs**
   - ✅ QueryToSpan<T1>() - single component span collection
   - ✅ QueryToSpan<T1, T2>() - multi-component span collection
   - ✅ QueryToSpan<T1, T2, T3, T4>() - 4-component span collection
   - ✅ QueryToSpan with Filter combinations
   - ✅ Span capacity limit handling
   - ✅ Process<T>() - component processing by reference
   - ✅ Process skips dead entities
   - ✅ Process skips entities without component
   - ✅ QueryToSpan + Process workflow
   - ✅ Empty span handling

11. **WorldComponentApiAdvancedTests.cs**
   - ✅ SnapshotComponent<T>() - snapshot delta dispatch
   - ✅ SnapshotComponentBoxed() - snapshot with boxed value
   - ✅ SnapshotComponentTyped() - snapshot by type
   - ✅ HasComponentBoxed() - check component existence with boxed type
   - ✅ GetAllComponents() - get all components on entity

12. **WorldCommandBufferAdvancedTests.cs**
   - ✅ DestroyAllEntities() - destroy all entities
   - ✅ SetSingleton() via command buffer - set singleton via command buffer
   - ✅ RemoveSingleton() via command buffer - remove singleton via command buffer

13. **WorldEntityApiTests.cs**
   - ✅ GetAllEntities() - get all alive entities
   - ✅ IsAlive(Entity) - check if entity is alive
   - ✅ IsAlive(int id, int gen) - check by ID and Generation
   - ✅ GetAllEntities returns snapshot
   - ✅ Excludes destroyed entities

14. **WorldContextTests.cs**
   - ✅ RegisterContext() - context registration
   - ✅ HasContext<T>() - check context existence
   - ✅ HasContext(Entity, Type) - check context by type
   - ✅ GetAllContexts() - get all contexts
   - ✅ RemoveContext() - remove context
   - ✅ ReinitializeContext() - reinitialize context
   - ✅ Context cleanup on entity destruction

15. **WorldBinderTests.cs**
   - ✅ AttachBinder() - binder registration
   - ✅ HasBinder<T>() - check binder existence
   - ✅ DetachBinder() - remove specific binder
   - ✅ DetachAllBinders() - remove all binders
   - ✅ DetachBinder(Entity, Type) - remove binder by type
   - ✅ GetAllBinders() - get all binders
   - ✅ GetAllBinderList() - get binder list
   - ✅ Automatic binder detachment on entity destruction

16. **WorldHookTests.cs**
   - ✅ AddWritePermission() - add write permission hook
   - ✅ RemoveWritePermission() - remove write permission hook
   - ✅ ClearWritePermissions() - clear all write permission hooks
   - ✅ AddReadPermission() - add read permission hook
   - ✅ RemoveReadPermission() - remove read permission hook
   - ✅ ClearReadPermissions() - clear all read permission hooks
   - ✅ AddValidator<T>() - add typed validator
   - ✅ RemoveValidator<T>() - remove typed validator
   - ✅ ClearTypedValidators() - clear all typed validators
   - ✅ AddValidator(object) - add object-level validator
   - ✅ RemoveValidator(object) - remove object-level validator
   - ✅ ClearValidators() - clear all object-level validators

17. **WorldWorkerTests.cs**
   - ✅ RunScheduledJobs() - execute scheduled jobs
   - ✅ RunScheduledJobs() return value verification
   - ✅ Multiple job execution
   - ✅ Duplicate call handling

## Test Coverage Summary

- **Total Test Files**: 17
- **Completed Major APIs**:
  - ✅ Query API (T1-T8, including filters)
  - ✅ Messages API
  - ✅ Singleton Components API
  - ✅ QuerySpan API
  - ✅ Component API (basic + advanced)
  - ✅ Context API
  - ✅ Binder API
  - ✅ Hook API
  - ✅ Worker API
  - ✅ Command Buffer API (basic + advanced)
  - ✅ Entity API
  - ✅ Snapshot API
  - ✅ System Lifecycle API

## Test Framework

The test suite uses a custom test framework (`ZenECS.Core.TestFramework`) that provides:

- **TestWorldHost**: A lightweight harness for creating a Core-only world and ticking it deterministically
- **WorldTestExtensions**: Helper extension methods for `IWorld` to simplify test code
  - `Apply()`: Records commands into a buffer and immediately flushes scheduled jobs
  - `CreateEntity()`: Creates an entity via command buffer with optional chaining
  - `FlushJobs()`: Flushes any scheduled jobs for the world

## Test Execution

All tests use xUnit and can be run via:
- Visual Studio Test Explorer
- `dotnet test` command
- Any xUnit-compatible test runner

## Notes

- All tests are designed to run without Unity dependencies
- Tests use deterministic simulation steps via `TestWorldHost.TickFrame()`, `TickFixed()`, and `TickFullFrame()`
- The test framework sets `WorldWritePhase` to `Simulation` during test execution to allow structural changes
- Tests cover both happy paths and edge cases (empty worlds, destroyed entities, missing components, etc.)

