# 04 - Command Buffer

> Docs / Samples / 04 - Command Buffer

Use command buffers to safely batch structural changes (entity creation, component add/remove) and apply at safe boundaries.

## Prerequisites

- **.NET SDK 8.0+** or **Unity 2021.3+**
- **ZenECS Core** package installed
- Understanding of [Command Buffer](../core/command-buffer.md)

## Run It

### .NET Standalone

```bash
cd Packages/com.zenecs.core/Samples~/04-CommandBuffer
dotnet run
```

### Unity

1. Open Unity project
2. Open scene: `Packages/com.zenecs.core/Samples~/04-CommandBuffer/Scene.unity`
3. Press Play

## Code Walkthrough

### Why Command Buffers?

Command buffers ensure structural changes are applied at safe boundaries:

- **Thread Safety**: Safe in multi-threaded environments
- **Determinism**: Applied at predictable points
- **Batching**: Efficient batch operations
- **Safety**: Prevents modification during iteration

### Basic Usage

```csharp
using (var cmd = world.BeginWrite())
{
    // Create entity
    var entity = cmd.CreateEntity();
    
    // Add components
    cmd.AddComponent(entity, new Position(0, 0));
    cmd.AddComponent(entity, new Velocity(1, 0));
    
    // Remove component
    cmd.RemoveComponent<Velocity>(entity);
    
    // Destroy entity
    cmd.DestroyEntity(entity);
} // All changes applied here
```

### In Systems

```csharp
[FixedGroup]
public class SpawnSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        using var cmd = world.BeginWrite();
        
        // Safe to create entities during system execution
        foreach (var msg in world.ConsumeMessages<SpawnRequest>())
        {
            var entity = cmd.CreateEntity();
            cmd.AddComponent(entity, new Position(msg.X, msg.Y));
            cmd.AddComponent(entity, new Velocity(0, 0));
        }
    }
}
```

## Complete Example

```csharp
using ZenECS.Core;
using ZenECS.Core.Systems;

// Components
public struct Position { public float X, Y; }
public struct Velocity { public float X, Y; }
public struct Health { public float Current, Max; }

// Message
public struct SpawnRequest
{
    public float X, Y;
}

// System
[FixedGroup]
public sealed class SpawnSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        using var cmd = world.BeginWrite();
        
        // Spawn entities from requests
        foreach (var req in world.ConsumeMessages<SpawnRequest>())
        {
            var entity = cmd.CreateEntity();
            cmd.AddComponent(entity, new Position(req.X, req.Y));
            cmd.AddComponent(entity, new Velocity(0, 0));
            cmd.AddComponent(entity, new Health(100, 100));
        }
    }
}

[FixedGroup]
[OrderAfter(typeof(SpawnSystem))]
public sealed class CleanupSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        using var cmd = world.BeginWrite();
        
        // Destroy dead entities
        foreach (var (entity, health) in world.Query<Health>())
        {
            if (health.Current <= 0)
            {
                cmd.DestroyEntity(entity);
            }
        }
    }
}

// Usage
var world = kernel.CreateWorld(null, "GameWorld");
world.AddSystems([new SpawnSystem(), new CleanupSystem()]);

// Request spawn
world.Publish(new SpawnRequest { X = 0, Y = 0 });

// Process (spawn happens here)
kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame: 4);
```

## Command Buffer API

### Entity Operations

```csharp
using var cmd = world.BeginWrite();

// Create entity
var entity = cmd.CreateEntity();

// Destroy entity
cmd.DestroyEntity(entity);
```

### Component Operations

```csharp
// Add component
cmd.AddComponent(entity, new Position(0, 0));

// Remove component
cmd.RemoveComponent<Position>(entity);

// Replace component
cmd.ReplaceComponent(entity, new Position(1, 1));

// Add or replace
cmd.AddOrReplaceComponent(entity, new Position(2, 2));
```

## Best Practices

### ✅ Do

- **Always use command buffers** for structural changes
- **Use in systems** when creating/destroying entities
- **Batch operations** for efficiency
- **Dispose properly** with `using` statement

### ❌ Don't

- **Don't modify during queries** - Use command buffers
- **Don't create entities directly** - Use command buffers
- **Don't forget to dispose** - Use `using` statement

## Common Patterns

### Spawn Pattern

```csharp
[FixedGroup]
public class SpawnSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        using var cmd = world.BeginWrite();
        
        foreach (var req in world.ConsumeMessages<SpawnRequest>())
        {
            var entity = cmd.CreateEntity();
            // Configure entity
        }
    }
}
```

### Cleanup Pattern

```csharp
[FixedGroup]
public class CleanupSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        using var cmd = world.BeginWrite();
        
        foreach (var (entity, component) in world.Query<Component>())
        {
            if (ShouldDestroy(component))
            {
                cmd.DestroyEntity(entity);
            }
        }
    }
}
```

## What to Try Next

### Experiment 1: Batch Spawning

Spawn multiple entities efficiently:

```csharp
[FixedGroup]
public class BatchSpawnSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        using var cmd = world.BeginWrite();
        
        var requests = world.ConsumeMessages<SpawnRequest>().ToList();
        foreach (var req in requests)
        {
            var entity = cmd.CreateEntity();
            // Configure
        }
    }
}
```

### Experiment 2: Conditional Operations

Conditional structural changes:

```csharp
[FixedGroup]
public class ConditionalSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        using var cmd = world.BeginWrite();
        
        foreach (var (entity, health) in world.Query<Health>())
        {
            if (health.Current <= 0 && !world.HasComponent<Dead>(entity))
            {
                cmd.AddComponent(entity, new Dead());
                cmd.RemoveComponent<Health>(entity);
            }
        }
    }
}
```

## Next Samples

- **[05 - World Reset](../samples/05-world-reset.md)** - State management
- **[06 - World Hook](../samples/06-world-hook.md)** - Lifecycle hooks
- **[Command Buffer Guide](../core/command-buffer.md)** - Detailed guide

## See Also

- [Command Buffer](../core/command-buffer.md) - Detailed documentation
- [Systems Guide](../core/systems.md) - System design
- [World Guide](../core/world.md) - World management
