# World

> Docs / Core / World

A **World** is a container for entities, components, and systems. It represents an isolated simulation space with its own lifecycle.

## Overview

A World in ZenECS:

- **Isolated Space**: Entities, components, and systems are scoped to a world
- **Independent Lifecycle**: Each world can be created, reset, and disposed independently
- **Multi-World Support**: Kernel manages multiple worlds simultaneously
- **Unified API**: Single interface for all ECS operations

### Key Concepts

- **Entities**: Unique identifiers in the world
- **Components**: Data attached to entities
- **Systems**: Logic that processes entities
- **Queries**: Find entities with specific components
- **Messages**: Event-driven communication
- **Binding**: Connect to view layer

## How It Works

### World Creation

Worlds are created via the Kernel:

```csharp
var kernel = new Kernel();
var world = kernel.CreateWorld(null, "GameWorld");
```

**Parameters:**
- `name`: World identifier (optional)
- `tags`: Tags for categorization (optional)
- `setAsCurrent`: Set as current world (optional)

### World Lifecycle

```
Create → Use → (Reset) → Dispose
```

1. **Create**: `kernel.CreateWorld(...)`
2. **Use**: Add systems, create entities, run simulation
3. **Reset** (optional): Clear entities, keep systems
4. **Dispose**: Clean up resources

## API Surface

### Entity Management

```csharp
// Create entity using command buffer
Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();
}

// Check if alive
bool isAlive = world.IsAlive(entity);

// Destroy entity using command buffer
using (var cmd = world.BeginWrite())
{
    cmd.DestroyEntity(entity);
}
```

### Component Management

```csharp
// Add component using command buffer
using (var cmd = world.BeginWrite())
{
    cmd.AddComponent(entity, new Position(0, 0));
}

// Get component (by value)
var pos = world.Get<Position>(entity);

// Get component (by reference)
ref var pos = ref world.Ref<Position>(entity);

// Check component
bool hasPos = world.HasComponent<Position>(entity);

// Remove component
world.RemoveComponent<Position>(entity);

// Replace component
world.ReplaceComponent(entity, new Position(1, 1));
```

### Query API

```csharp
// Query entities with single component
foreach (var (entity, pos) in world.Query<Position>())
{
    // Process entity
}

// Query entities with multiple components
foreach (var (entity, pos, vel) in world.Query<Position, Velocity>())
{
    // Entity has both Position and Velocity
}

// Query with filter
foreach (var (entity, health) in world.Query<Health>()
    .Where(e => world.Get<Health>(e).Current > 0))
{
    // Only entities with health > 0
}
```

### System Management

```csharp
// Add system
world.AddSystems([new MovementSystem()]);

// Remove system
world.RemoveSystem<MovementSystem>();

// Get system
var system = world.GetSystem<MovementSystem>();
```

### Message Bus

```csharp
// Publish message
world.Publish(new DamageMessage { Target = entity, Amount = 10 });

// Subscribe to messages (in system)
foreach (var msg in world.ConsumeMessages<DamageMessage>())
{
    // React to message
}
```

### Command Buffer

```csharp
// Buffer structural changes
using (var cmd = world.BeginWrite())
{
    var entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position(0, 0));
    cmd.RemoveComponent<Velocity>(entity);
} // Changes applied here
```

## Examples

### Basic World Usage

```csharp
using ZenECS.Core;

// Create kernel and world
var kernel = new Kernel();
var world = kernel.CreateWorld(null, "GameWorld");

// Create entity with components using command buffer
Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position(0, 0));
    cmd.AddComponent(entity, new Velocity(1, 0));
}

// Query and process
foreach (var (e, pos, vel) in world.Query<Position, Velocity>())
{
    world.ReplaceComponent(e, new Position(
        pos.X + vel.X * 0.016f,
        pos.Y + vel.Y * 0.016f
    ));
}

// Cleanup
kernel.Dispose();
```

### Multi-World Setup

```csharp
var kernel = new Kernel();

// Create multiple worlds
var gameWorld = kernel.CreateWorld(null, "Game");
var uiWorld = kernel.CreateWorld(null, "UI");
var serverWorld = kernel.CreateWorld(null, "Server");

// Each world is isolated
gameWorld.AddSystems([new GameSystem()]);
uiWorld.AddSystems([new UISystem()]);
serverWorld.AddSystems([new ServerSystem()]);

// Step all worlds
kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame: 4);
```

### World Reset

```csharp
// Reset clears entities but keeps systems
world.Reset();

// All entities are destroyed
// Systems remain registered
// Ready for new simulation
```

## Best Practices

### ✅ Do

- **Use command buffers** for structural changes
- **Query efficiently** - filter early in pipeline
- **Isolate worlds** - separate concerns (game, UI, server)
- **Reset worlds** - reuse worlds instead of recreating

### ❌ Don't

- **Don't store entity references** - entities are value types
- **Don't mutate in queries** - use command buffers
- **Don't create entities in systems** - use external commands
- **Don't mix worlds** - keep worlds isolated

## Common Patterns

### Entity Creation Pattern

```csharp
using (var cmd = world.BeginWrite())
{
    var entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position(0, 0));
    cmd.AddComponent(entity, new Velocity(1, 0));
    cmd.AddComponent(entity, new Health(100));
}
```

### Query Pattern

```csharp
foreach (var (entity, component) in world.Query<Component>())
{
    // Process entity
    ProcessEntity(entity, component);
}
```

### Message Pattern

```csharp
// Publish
world.Publish(new EventMessage { Data = value });

// Subscribe (in system)
foreach (var msg in world.ConsumeMessages<EventMessage>())
{
    HandleMessage(msg);
}
```

## Performance Considerations

### Query Performance

- Queries are **zero-allocation** (struct enumerators)
- **Cache-friendly** - components in contiguous arrays
- **Fast filtering** - efficient component matching

### Memory Management

- **Component pooling** - automatic pool management
- **Entity recycling** - IDs are recycled
- **Capacity policies** - configurable growth strategies

## See Also

- [Entities](./entities.md) - Entity management details
- [Components](./components.md) - Component system
- [Systems](./systems.md) - System execution
- [Query API](../core/world.md#query-api) - Advanced querying
- [Command Buffer](./command-buffer.md) - Structural changes
