# Command Buffer

> Docs / Core / Command Buffer

Batch structural changes (entity creation/destruction, component add/remove) and apply at safe boundaries.

## Overview

Command Buffers enable:

- **Safe Structural Changes**: Batch operations safely
- **Thread Safety**: Safe in multi-threaded environments
- **Determinism**: Applied at predictable points
- **Performance**: Efficient batch operations

### Key Concepts

- **Structural Changes**: Entity create/destroy, component add/remove
- **Batching**: Multiple operations in one buffer
- **Safe Boundaries**: Changes applied at buffer disposal
- **Thread Safety**: Safe concurrent access

## How It Works

### Command Buffer Lifecycle

```
BeginWrite() → Operations → Dispose() → Apply Changes
```

1. **Begin**: `using var cmd = world.BeginWrite()`
2. **Operations**: Create entities, add/remove components
3. **Dispose**: Changes applied automatically
4. **Apply**: All changes applied at once

### Why Command Buffers?

**Without Command Buffer:**
```csharp
// ❌ Unsafe: Modifying during iteration
foreach (var (entity, component) in world.Query<Component>())
{
    world.DestroyEntity(entity);  // Modifying during iteration!
}
```

**With Command Buffer:**
```csharp
// ✅ Safe: Buffer changes, apply later
using var cmd = world.BeginWrite();
foreach (var (entity, component) in world.Query<Component>())
{
    cmd.DestroyEntity(entity);  // Buffered, applied after loop
}
```

## API Surface

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

## Examples

### Basic Usage

```csharp
using ZenECS.Core;

var world = kernel.CreateWorld(null, "GameWorld");

// Create entities with command buffer
using (var cmd = world.BeginWrite())
{
    var player = cmd.CreateEntity();
    cmd.AddComponent(player, new Position(0, 0));
    cmd.AddComponent(player, new Velocity(1, 0));
    
    var enemy = cmd.CreateEntity();
    cmd.AddComponent(enemy, new Position(10, 0));
    cmd.AddComponent(enemy, new Velocity(-1, 0));
} // Changes applied here
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
        }
    }
}
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
- **Don't nest buffers** - One buffer per scope

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
            cmd.AddComponent(entity, new Position(req.X, req.Y));
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
        
        foreach (var (entity, health) in world.Query<Health>())
        {
            if (health.Current <= 0)
            {
                cmd.DestroyEntity(entity);
            }
        }
    }
}
```

## See Also

- [World](./world.md) - World API
- [Entities](./entities.md) - Entity management
- [Systems](./systems.md) - System design
