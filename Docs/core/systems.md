# Systems

> Docs / Core / Systems

**Systems** are pure logic that operate on entities with specific component combinations. They query entities and transform their components.

## Overview

Systems in ZenECS:

- **Pure Logic**: No state, no side effects (except component mutations)
- **Query-Based**: Find entities with specific components
- **Grouped Execution**: Run in specific phases (Fixed, Frame, etc.)
- **Ordered**: Control execution order with attributes

### Key Concepts

- **ISystem Interface**: Base interface for all systems
- **System Groups**: Execution phases (FixedGroup, FrameGroup)
- **Execution Order**: Control when systems run
- **Query Pattern**: Find entities to process

## How It Works

### System Interface

All systems implement `ISystem`:

```csharp
public interface ISystem
{
    void Run(IWorld world, float deltaTime);
}
```

### System Groups

Systems run in specific phases:

- **FixedGroup**: Deterministic simulation (physics, gameplay)
- **FrameGroup**: Variable-timestep updates (camera, UI)

```csharp
[FixedGroup]
public class MovementSystem : ISystem
{
    public void Run(IWorld world, float deltaTime) { }
}

[FrameGroup]
public class CameraSystem : ISystem
{
    public void Run(IWorld world, float deltaTime) { }
}
```

### Execution Order

Control system execution order:

```csharp
[FixedGroup]
[OrderAfter(typeof(PhysicsSystem))]
public class MovementSystem : ISystem
{
    public void Run(IWorld world, float deltaTime) { }
}
```

## API Surface

### System Registration

```csharp
// Add single system
world.AddSystems([new MovementSystem()]);

// Add multiple systems
world.AddSystems([
    new MovementSystem(),
    new HealthSystem(),
    new RenderSystem()
]);

// Remove system
world.RemoveSystem<MovementSystem>();

// Get system
var system = world.GetSystem<MovementSystem>();
```

### System Attributes

```csharp
// Fixed-step simulation
[FixedGroup]
public class PhysicsSystem : ISystem { }

// Variable-timestep updates
[FrameGroup]
public class CameraSystem : ISystem { }

// Execution order
[OrderAfter(typeof(PhysicsSystem))]
[OrderBefore(typeof(RenderSystem))]
public class MovementSystem : ISystem { }
```

## Examples

### Basic System

```csharp
using ZenECS.Core;
using ZenECS.Core.Systems;

[FixedGroup]
public sealed class MovementSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        using var cmd = world.BeginWrite();
        
        foreach (var (entity, pos, vel) in world.Query<Position, Velocity>())
        {
            cmd.ReplaceComponent(entity, new Position(
                pos.X + vel.X * deltaTime,
                pos.Y + vel.Y * deltaTime
            ));
        }
    }
}
```

### System with Messages

```csharp
[FixedGroup]
public sealed class HealthSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        using var cmd = world.BeginWrite();
        
        // Process damage messages
        foreach (var msg in world.ConsumeMessages<DamageMessage>())
        {
            if (world.HasComponent<Health>(msg.Target))
            {
                ref var health = ref world.Ref<Health>(msg.Target);
                health.Current -= msg.Amount;
                
                if (health.Current <= 0)
                {
                    cmd.DestroyEntity(msg.Target);
                }
            }
        }
    }
}
```

### System with Ordering

```csharp
[FixedGroup]
[OrderAfter(typeof(InputSystem))]
[OrderBefore(typeof(RenderSystem))]
public sealed class GameplaySystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        // Runs after InputSystem, before RenderSystem
    }
}
```

## Best Practices

### ✅ Do

- **Use command buffers** for writes
- **Query efficiently** - filter early
- **Keep systems focused** - one responsibility
- **Use appropriate groups** - FixedGroup for simulation, FrameGroup for presentation

### ❌ Don't

- **Don't store state** - systems should be stateless
- **Don't mutate in queries** - use command buffers
- **Don't create entities** - use external commands
- **Don't access view layer** - use binders instead

## Common Patterns

### Query Pattern

```csharp
foreach (var (entity, component) in world.Query<Component>())
{
    // Process entity
}
```

### Message Pattern

```csharp
// Publish messages
world.Publish(new EventMessage());

// Consume in system
foreach (var msg in world.ConsumeMessages<EventMessage>())
{
    // React to message
}
```

### Command Buffer Pattern

```csharp
using var cmd = world.BeginWrite();
foreach (var (entity, component) in world.Query<Component>())
{
    cmd.ReplaceComponent(entity, newComponent);
}
```

## System Groups Explained

### FixedGroup

For deterministic simulation:

- **Physics**: Collision detection, movement
- **Gameplay**: Combat, AI, state machines
- **Networking**: Deterministic state updates

```csharp
[FixedGroup]
public class PhysicsSystem : ISystem { }
```

### FrameGroup

For variable-timestep updates:

- **Camera**: Smooth camera movement
- **UI**: Interface updates
- **Visual Effects**: Particle systems, animations

```csharp
[FrameGroup]
public class CameraSystem : ISystem { }
```

## Execution Flow

Systems run in this order:

1. **FixedGroup Systems** (deterministic)
   - FixedInput → FixedDecision → FixedSimulation → FixedPost
2. **FrameGroup Systems** (variable-timestep)
   - FrameInput → FrameSync → FrameView → FrameUI

## See Also

- [World](./world.md) - World management
- [System Runner](./system-runner.md) - Execution details
- [Query API](./world.md#query-api) - Advanced querying
- [Message Bus](./message-bus.md) - Event-driven architecture
