# What is ECS?

> Docs / Overview / What is ECS

Entity-Component-System (ECS) is a data-oriented architecture that separates data (components) from behavior (systems). This pattern enables scalable, maintainable, and performant game logic.

## Overview

Traditional object-oriented programming uses classes that combine data and behavior:

```csharp
// OOP approach
class Player
{
    public Vector3 Position { get; set; }
    public float Health { get; set; }
    
    public void Update(float deltaTime)
    {
        // Behavior mixed with data
        Position += Velocity * deltaTime;
    }
}
```

ECS separates these concerns:

```csharp
// ECS approach
// Component: Pure data
public struct Position { public float X, Y, Z; }
public struct Velocity { public float X, Y, Z; }
public struct Health { public float Current, Max; }

// System: Pure behavior
public class MovementSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        foreach (var (entity, pos, vel) in world.Query<Position, Velocity>())
        {
            // Transform data
            world.ReplaceComponent(entity, new Position(
                pos.X + vel.X * deltaTime,
                pos.Y + vel.Y * deltaTime,
                pos.Z + vel.Z * deltaTime
            ));
        }
    }
}
```

## How It Works

### The Three Pillars

#### 1. Entities

**Entities** are unique identifiers that represent game objects. They have no data or behavior—they're just IDs.

```csharp
// Create entity using command buffer
Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();  // Just an ID
}
```

#### 2. Components

**Components** are pure data structures (structs) that define properties. They have no methods or behavior.

```csharp
public struct Position
{
    public float X, Y, Z;
}

public struct Velocity
{
    public float X, Y, Z;
}

// Attach components to entities using command buffer
using (var cmd = world.BeginWrite())
{
    cmd.AddComponent(entity, new Position(0, 0, 0));
    cmd.AddComponent(entity, new Velocity(1, 0, 0));
}
```

#### 3. Systems

**Systems** are pure logic that operate on entities with specific component combinations. They query entities and transform their components.

```csharp
public class MovementSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        // Query all entities with Position AND Velocity
        foreach (var (entity, pos, vel) in world.Query<Position, Velocity>())
        {
            // Transform data
            var newPos = new Position(
                pos.X + vel.X * deltaTime,
                pos.Y + vel.Y * deltaTime,
                pos.Z + vel.Z * deltaTime
            );
            world.ReplaceComponent(entity, newPos);
        }
    }
}
```

### Data Flow

```
┌─────────────┐
│  Component  │  Data structure (struct)
│  Definition │
└──────┬──────┘
       │
       ↓
┌─────────────┐
│   Entity    │  ID + Component references
│             │
└──────┬──────┘
       │
       ↓
┌─────────────┐
│   System    │  Query entities → Transform components
│             │
└─────────────┘
```

## API Surface

### Core Concepts

**World**: A container for entities, components, and systems
```csharp
var world = kernel.CreateWorld(null, "GameWorld");
```

**Entity**: A unique identifier
```csharp
// Create entity using command buffer
Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();
}
```

**Component**: Pure data structure
```csharp
public struct Position { public float X, Y, Z; }
using (var cmd = world.BeginWrite())
{
    cmd.AddComponent(entity, new Position(0, 0, 0));
}
```

**System**: Logic that processes entities
```csharp
public class MovementSystem : ISystem
{
    public void Run(IWorld world, float deltaTime) { }
}
world.AddSystems([new MovementSystem()]);
```

**Query**: Find entities with specific components
```csharp
foreach (var (entity, pos, vel) in world.Query<Position, Velocity>())
{
    // Process entities with both Position and Velocity
}
```

## Examples

### Basic Example: Moving Entities

```csharp
using ZenECS.Core;

// 1. Create world
var kernel = new Kernel();
var world = kernel.CreateWorld(null, "GameWorld");

// 2. Define components
public struct Position { public float X, Y; }
public struct Velocity { public float X, Y; }

// 3. Create system
[FixedGroup]
public class MovementSystem : ISystem
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

// 6. Run simulation
const float fixedDelta = 1f / 60f;
kernel.PumpAndLateFrame(Time.deltaTime, fixedDelta, maxSubStepsPerFrame: 4);
```

### Advanced Example: Health System

```csharp
public struct Health
{
    public float Current;
    public float Max;
}

public struct DamageMessage
{
    public Entity Target;
    public float Amount;
}

[FixedGroup]
public class HealthSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        // Process damage messages
        foreach (var msg in world.ConsumeMessages<DamageMessage>())
        {
            if (world.HasComponent<Health>(msg.Target))
            {
                var health = world.ReadComponent<Health>(msg.Target);
                var newHealth = new Health(health.Current - msg.Amount, health.Max);
                
                using (var cmd = world.BeginWrite())
                {
                    if (newHealth.Current <= 0)
                    {
                        cmd.DestroyEntity(msg.Target);
                    }
                    else
                    {
                        cmd.ReplaceComponent(msg.Target, newHealth);
                    }
                }
            }
        }
    }
}
```

## Benefits of ECS

### 1. Performance

- **Cache-friendly**: Components stored in contiguous arrays
- **Parallel processing**: Systems can run independently
- **Zero allocation**: Queries use struct enumerators

### 2. Scalability

- **Add components dynamically**: Entities gain capabilities by adding components
- **Composition over inheritance**: Mix and match components freely
- **System isolation**: Systems don't depend on each other

### 3. Maintainability

- **Separation of concerns**: Data, logic, and presentation are separate
- **Testability**: Systems are pure functions, easy to test
- **Flexibility**: Change behavior by adding/removing systems

### 4. Clarity

- **Explicit data flow**: Easy to see what data systems use
- **No hidden state**: Components are explicit
- **Clear dependencies**: Systems declare what they need

## ECS vs OOP

| Aspect | OOP | ECS |
|--------|-----|-----|
| **Data & Behavior** | Combined in classes | Separated (components vs systems) |
| **Inheritance** | Class hierarchies | Composition (add components) |
| **Performance** | Cache misses common | Cache-friendly arrays |
| **Flexibility** | Rigid hierarchies | Dynamic composition |
| **Testing** | Mock dependencies | Pure functions |

## FAQ

### Q: Do I need to use ECS for everything?

**A**: No. ECS is great for game logic, but UI, file I/O, and other tasks can use traditional patterns.

### Q: Is ECS harder to learn?

**A**: Initially yes, but the separation of concerns makes complex systems easier to manage long-term.

### Q: Can I mix ECS with OOP?

**A**: Yes! ZenECS integrates with Unity's GameObject system and other OOP code.

### Q: What about performance?

**A**: ECS is designed for performance, but ZenECS prioritizes clarity and maintainability. For maximum performance, consider Unity DOTS.

## See Also

- [ZenECS at a Glance](./zenecs-at-a-glance.md) - Overview of ZenECS
- [Architecture](./architecture.md) - ZenECS architecture details
- [Quick Start](../getting-started/quickstart-basic.md) - Get started with ZenECS
- [Core Concepts](../core/world.md) - Deep dive into core concepts
