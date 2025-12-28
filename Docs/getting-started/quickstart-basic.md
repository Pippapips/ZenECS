# Quickstart (Basic)

> Docs / Getting started / Quickstart (Basic)

Get started with ZenECS in 5 minutes. This guide walks you through creating a world, adding components, and running systems.

## Prerequisites

- Unity 2021.3+ or .NET Standard 2.1+
- ZenECS Core package installed
- Basic C# knowledge

## Create World

A **World** is a container for entities, components, and systems. Create one via the Kernel:

```csharp
using ZenECS.Core;

// Create kernel (manages multiple worlds)
var kernel = new Kernel();

// Create a world
var world = kernel.CreateWorld(null, "GameWorld");
```

**In Unity**, use `EcsDriver`:

```csharp
using ZenECS.Adapter.Unity;
using ZenECS.Core;

// Kernel is automatically created by EcsDriver
var kernel = KernelLocator.Current;
var world = kernel.CreateWorld(null, "GameWorld", setAsCurrent: true);
```

## Define Components

Components are pure data structures (structs):

```csharp
public struct Position
{
    public float X, Y;
    
    public Position(float x, float y)
    {
        X = x;
        Y = y;
    }
}

public struct Velocity
{
    public float X, Y;
    
    public Velocity(float x, float y)
    {
        X = x;
        Y = y;
    }
}
```

**Key Points:**
- Components are `struct` (value types)
- No methods or behavior, just data
- Immutable by convention (readonly fields)

## Create Entities and Add Components

Entities are unique IDs. Attach components to give them data:

```csharp
// Create entity and add components using command buffer
using (var cmd = world.BeginWrite())
{
    var entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position(0, 0));
    cmd.AddComponent(entity, new Velocity(1, 0));
}
```

**Using Command Buffer** (recommended for structural changes):

```csharp
using (var cmd = world.BeginWrite())
{
    var entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position(0, 0));
    cmd.AddComponent(entity, new Velocity(1, 0));
}
```

## Write a System

Systems contain logic that processes entities:

```csharp
using ZenECS.Core;
using ZenECS.Core.Systems;

[FixedGroup]
public sealed class MovementSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        using var cmd = world.BeginWrite();
        
        // Query entities with Position AND Velocity
        foreach (var (entity, pos, vel) in world.Query<Position, Velocity>())
        {
            // Update position based on velocity
            cmd.ReplaceComponent(entity, new Position(
                pos.X + vel.X * deltaTime,
                pos.Y + vel.Y * deltaTime
            ));
        }
    }
}
```

**Key Points:**
- Systems implement `ISystem`
- Use `[FixedGroup]` for deterministic simulation
- Query entities with `world.Query<T1, T2>()`
- Use command buffer for writes: `world.BeginWrite()`

## Register Systems

Add systems to the world:

```csharp
world.AddSystems([new MovementSystem()]);
```

Systems run automatically during frame updates.

## Run the Simulation

Drive the simulation with the kernel:

```csharp
const float fixedDelta = 1f / 60f;  // 60 FPS

while (running)
{
    float dt = GetDeltaTime();  // Your delta time source
    
    // Run simulation
    kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame: 4);
}
```

**In Unity**, `EcsDriver` handles this automatically via `Update()` and `FixedUpdate()`.

## Complete Example

```csharp
using ZenECS.Core;
using ZenECS.Core.Systems;

// 1. Create kernel and world
var kernel = new Kernel();
var world = kernel.CreateWorld(null, "GameWorld");

// 2. Define components
public struct Position { public float X, Y; }
public struct Velocity { public float X, Y; }

// 3. Create system
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

// 4. Register system
world.AddSystems([new MovementSystem()]);

// 5. Create entities
using (var cmd = world.BeginWrite())
{
    var player = cmd.CreateEntity();
    cmd.AddComponent(player, new Position(0, 0));
    cmd.AddComponent(player, new Velocity(1, 0));
}

// 6. Game loop
const float fixedDelta = 1f / 60f;
while (running)
{
    float dt = GetDeltaTime();
    kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame: 4);
}

// 7. Cleanup
kernel.Dispose();
```

## Reset/Dispose

### Reset World

Clear all entities and components, keep systems:

```csharp
world.Reset();  // Fast reset, keeps systems
```

### Dispose

Clean up resources:

```csharp
kernel.Dispose();  // Disposes all worlds
```

## Next Steps

Now that you understand the basics:

1. **[Explore Samples](../samples/01-basic.md)** - See working examples
2. **[Learn About Systems](../core/systems.md)** - Deep dive into system design
3. **[Understand Queries](../core/world.md#query-api)** - Advanced querying
4. **[Try Unity Integration](../adapter-unity/overview.md)** - Unity-specific features

### Learning Path

**Beginner:**
- ✅ This guide (you're here!)
- [What is ECS?](../overview/what-is-ecs.md)
- [Basic Sample](../samples/01-basic.md)

**Intermediate:**
- [Systems Guide](../core/systems.md)
- [Message Bus](../core/message-bus.md)
- [Binding System](../core/binding.md)

**Advanced:**
- [Architecture](../overview/architecture.md)
- [Advanced Topics](../guides/advanced-topics.md)
- [Performance Guide](../core/performance.md)

## Common Patterns

### Query Pattern

Find entities with specific components:

```csharp
foreach (var (entity, component) in world.Query<Component>())
{
    // Process entity
}
```

### Multiple Components

Query entities with multiple components:

```csharp
foreach (var (entity, pos, vel, health) in world.Query<Position, Velocity, Health>())
{
    // Entity has all three components
}
```

### Read-Only Access

Read components without command buffer:

```csharp
foreach (var (entity, pos) in world.Query<Position>())
{
    var position = pos;  // Read by value
    // No writes needed
}
```

## Troubleshooting

### System Not Running

- Check system is registered: `world.AddSystems([new MySystem()])`
- Verify system has `[FixedGroup]` or `[FrameGroup]` attribute
- Ensure world is being stepped: `kernel.PumpAndLateFrame(...)`

### Entities Not Found

- Verify entities have required components
- Check query matches component types
- Ensure entities are alive: `world.IsAlive(entity)`

### Components Not Updating

- Use command buffer for writes: `using var cmd = world.BeginWrite()`
- Replace components: `cmd.ReplaceComponent(...)`
- Check system is running and registered

## See Also

- [Installation Guide](install-upm.md) - How to install ZenECS
- [World Guide](../core/world.md) - Deep dive into worlds
- [Systems Guide](../core/systems.md) - System design patterns
- [Basic Sample](../samples/01-basic.md) - Complete working example
