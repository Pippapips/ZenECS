# ZenECS.Core.TestFramework

A lightweight **helper library** for writing tests against **ZenECS Core** without Unity or other hosting dependencies.

## Overview

The TestFramework provides essential utilities for testing ZenECS Core in isolation. It creates a minimal test environment with a `Kernel` and `World`, allowing you to write deterministic tests that validate Core behavior without external dependencies.

## Key Components

### TestWorldHost

A lightweight test harness that creates a Core-only world and provides deterministic simulation stepping.

**Features:**
- Creates a `Kernel` and `World` instance for testing
- Provides deterministic frame stepping methods
- Automatically disposes resources when done

**Usage:**
```csharp
using var host = new TestWorldHost();

// Access the world
var world = host.World;

// Step simulation deterministically
host.TickFrame(dt: 0.016f);        // Single frame
host.TickFixed(fixedDelta: 0.02f); // Single fixed step
host.TickFullFrame(dt: 0.016f, fixedDelta: 0.02f, fixedSteps: 1); // Full frame
```

**Constructor Options:**
- `kernelOptions`: Optional `KernelOptions` for kernel configuration
- `worldConfig`: Optional `WorldConfig` for world configuration
- `defaultFixedDelta`: Default fixed delta time (default: 1/60 seconds)

**Methods:**
- `TickFrame(float dt = 0f, float lateAlpha = 1f)`: Executes `BeginFrame` (which pumps messages) followed by `LateFrame`
- `TickFixed(float? fixedDelta = null)`: Executes a single fixed step simulation
- `TickFullFrame(float dt = 0f, float? fixedDelta = null, int fixedSteps = 1, float lateAlpha = 1f)`: Executes a complete frame: `BeginFrame` → N×`FixedStep` → `LateFrame`

### WorldTestExtensions

Helper extension methods for `IWorld` that reduce boilerplate in test code.

#### Apply

Records commands into a buffer and immediately flushes scheduled jobs. Sets `WorldWritePhase` to `Simulation` to allow structural changes during tests.

```csharp
host.World.Apply(cmd =>
{
    cmd.AddComponent(entity, new Position { X = 10, Y = 20 });
    cmd.RemoveComponent<Health>(entity);
});
```

**What it does:**
1. Sets write phase to `Simulation` (allows structural changes)
2. Creates a command buffer
3. Executes the recording action
4. Flushes scheduled jobs
5. Clears the write phase

#### CreateEntity

Creates an entity via command buffer with optional command chaining, then flushes.

```csharp
// Simple entity creation
Entity e = host.World.CreateEntity();

// Entity with components
Entity e = host.World.CreateEntity((cmd, entity) =>
{
    cmd.AddComponent(entity, new Position { X = 1, Y = 2 });
    cmd.AddComponent(entity, new Health { Value = 100 });
});
```

**Returns:** The created `Entity` handle

#### FlushJobs

Flushes any scheduled jobs for the world and returns how many ran.

```csharp
int jobsRun = host.World.FlushJobs();
```

## Example Test

```csharp
using Xunit;
using ZenECS.Core;
using ZenECS.Core.TestFramework;

public class MyComponentTests
{
    private struct Position
    {
        public int X;
        public int Y;
    }

    [Fact]
    public void CreateEntity_WithComponents_Succeeds()
    {
        using var host = new TestWorldHost();

        Entity e = host.World.CreateEntity((cmd, entity) =>
        {
            cmd.AddComponent(entity, new Position { X = 10, Y = 20 });
        });

        Assert.True(host.World.HasComponent<Position>(e));
        var pos = host.World.ReadComponent<Position>(e);
        Assert.Equal(10, pos.X);
        Assert.Equal(20, pos.Y);
    }

    [Fact]
    public void System_Runs_OnTickFrame()
    {
        using var host = new TestWorldHost();
        var system = new MySystem();
        
        host.World.AddSystem(system);
        
        host.TickFrame(dt: 0.016f);
        
        Assert.True(system.WasExecuted);
    }
}
```

## Design Principles

1. **Minimal Surface**: Keep the API surface small to maintain stability as Core evolves
2. **No External Dependencies**: Tests run without Unity, Godot, or other hosting frameworks
3. **Deterministic**: All simulation steps are deterministic and reproducible
4. **Isolated**: Each test creates its own world instance

## Integration with Test Projects

1. Reference this project from your test project(s)
2. Create `TestWorldHost` instances in your tests
3. Use `WorldTestExtensions` helpers to reduce boilerplate
4. Write assertions against the world state

## Notes

- The framework automatically manages `WorldWritePhase` during `Apply()` calls to allow structural changes
- All command buffer operations are immediately flushed, so tests can assert immediately after operations
- The `TestWorldHost` implements `IDisposable` and should be used with `using` statements for proper cleanup
