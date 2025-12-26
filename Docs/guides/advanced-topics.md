# Advanced Topics

> Docs / Guides / Advanced topics

Advanced patterns, optimizations, and architectural considerations for ZenECS.

## Overview

This guide covers advanced topics for experienced ZenECS users:

- **Performance Optimization**: Hot path optimizations and profiling
- **Multi-World Patterns**: Complex world management scenarios
- **Custom Extensions**: Extending ZenECS with custom functionality
- **Networking**: Deterministic simulations for multiplayer
- **Testing Strategies**: Unit testing and integration testing

## Performance Optimization

### Query Optimization

**Cache queries** when possible:

```csharp
public class MovementSystem : ISystem
{
    // Cache query result
    private List<(Entity, Position, Velocity)> _entities = new();
    
    public void Run(IWorld world, float deltaTime)
    {
        _entities.Clear();
        foreach (var item in world.Query<Position, Velocity>())
        {
            _entities.Add(item);
        }
        
        // Process cached list
        foreach (var (entity, pos, vel) in _entities)
        {
            // Process
        }
    }
}
```

**Filter early** in the pipeline:

```csharp
// ✅ Good: Filter early
foreach (var (entity, health) in world.Query<Health>()
    .Where(e => world.Get<Health>(e).Current > 0))
{
    // Only process alive entities
}

// ❌ Bad: Filter late
foreach (var (entity, health) in world.Query<Health>())
{
    if (health.Current <= 0) continue;  // Filtered too late
    // Process
}
```

### Component Pool Optimization

**Pre-allocate pools** for known component types:

```csharp
var world = kernel.CreateWorld(new WorldConfig
{
    InitialPoolBuckets = new Dictionary<Type, int>
    {
        { typeof(Position), 1000 },
        { typeof(Velocity), 1000 },
        { typeof(Health), 500 }
    }
});
```

### System Execution Order

**Optimize system order** to minimize cache misses:

```csharp
[FixedGroup]
[OrderBefore(typeof(RenderSystem))]  // Run before expensive systems
public class MovementSystem : ISystem { }
```

## Multi-World Patterns

### Split-Screen Games

```csharp
var kernel = new Kernel();

// Create separate worlds for each player
var player1World = kernel.CreateWorld(null, "Player1", tags: new[] { "player1" });
var player2World = kernel.CreateWorld(null, "Player2", tags: new[] { "player2" });

// Step worlds independently
kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame: 4);
```

### Server/Client Separation

```csharp
// Server world (authoritative)
var serverWorld = kernel.CreateWorld(null, "Server", tags: new[] { "server" });

// Client world (prediction)
var clientWorld = kernel.CreateWorld(null, "Client", tags: new[] { "client" });

// Sync server state to client
SyncWorlds(serverWorld, clientWorld);
```

### Game Mode Isolation

```csharp
// Menu world
var menuWorld = kernel.CreateWorld(null, "Menu");

// Gameplay world
var gameplayWorld = kernel.CreateWorld(null, "Gameplay");

// Switch between worlds
kernel.SetCurrentWorld(menuWorld);  // Show menu
kernel.SetCurrentWorld(gameplayWorld);  // Start game
```

## Custom Extensions

### Custom System Groups

Add new system execution phases:

```csharp
// 1. Extend SystemGroup enum (internal)
public enum SystemGroup
{
    // ... existing groups
    CustomPhase
}

// 2. Add execution logic to SystemRunner
public void CustomPhase(IWorld world, float deltaTime)
{
    RunGroup(SystemGroup.CustomPhase, world, deltaTime);
}
```

### Custom Component Formatters

Implement custom serialization:

```csharp
public class JsonComponentFormatter : IComponentFormatter
{
    public void WriteComponent(Stream stream, Type componentType, object component)
    {
        var json = JsonUtility.ToJson(component);
        var bytes = Encoding.UTF8.GetBytes(json);
        stream.Write(bytes, 0, bytes.Length);
    }
    
    public object ReadComponent(Stream stream, Type componentType)
    {
        var bytes = new byte[stream.Length];
        stream.Read(bytes, 0, bytes.Length);
        var json = Encoding.UTF8.GetString(bytes);
        return JsonUtility.FromJson(json, componentType);
    }
}

// Use custom formatter
world.Save(stream, new JsonComponentFormatter());
```

### Custom Hooks

Add custom validation and permissions:

```csharp
// Write permission hook
world.Hooks.AddWritePermission((entity, componentType) =>
{
    // Custom logic
    return HasPermission(entity, componentType);
});

// Value validator
world.Hooks.AddValidator<Health>(health =>
{
    // Validate health values
    return health.Current >= 0 && health.Current <= health.Max;
});
```

## Networking Patterns

### Deterministic Simulation

Ensure reproducible simulations:

```csharp
// Use fixed timestep
const float fixedDelta = 1f / 60f;

// Deterministic random seed
var random = new System.Random(seed);

// Deterministic system order
[FixedGroup]
[OrderAfter(typeof(InputSystem))]
public class GameplaySystem : ISystem { }
```

### State Synchronization

Sync world state between server and client:

```csharp
// Server: Create snapshot
var snapshot = world.CreateSnapshot();

// Client: Apply snapshot
world.LoadSnapshot(snapshot);
```

### Rollback and Prediction

Implement client-side prediction:

```csharp
// Save state before prediction
var savedState = world.CreateSnapshot();

// Run prediction
world.Step(fixedDelta);

// If server state differs, rollback
if (ServerStateDiffers(savedState, serverState))
{
    world.LoadSnapshot(savedState);
    world.LoadSnapshot(serverState);
}
```

## Testing Strategies

### Unit Testing Systems

Test systems in isolation:

```csharp
[Test]
public void MovementSystem_MovesEntities()
{
    // Arrange
    var world = new World(/* ... */);
    var system = new MovementSystem();
    Entity entity;
    using (var cmd = world.BeginWrite())
    {
        entity = cmd.CreateEntity();
        cmd.AddComponent(entity, new Position(0, 0));
        cmd.AddComponent(entity, new Velocity(1, 0));
    }
    
    // Act
    system.Run(world, 1f);
    
    // Assert
    var pos = world.Get<Position>(entity);
    Assert.AreEqual(1f, pos.X);
    Assert.AreEqual(0f, pos.Y);
}
```

### Integration Testing

Test full world scenarios:

```csharp
[Test]
public void World_CompleteGameplayLoop()
{
    var kernel = new Kernel();
    var world = kernel.CreateWorld(null, "Test");
    
    // Setup
    world.AddSystems([new MovementSystem(), new HealthSystem()]);
    Entity player;
    using (var cmd = world.BeginWrite())
    {
        player = cmd.CreateEntity();
        cmd.AddComponent(player, new Position(0, 0));
        cmd.AddComponent(player, new Health(100));
    }
    
    // Execute
    kernel.PumpAndLateFrame(1f, 1f / 60f, maxSubStepsPerFrame: 4);
    
    // Verify
    Assert.IsTrue(world.IsAlive(player));
}
```

## Best Practices

### ✅ Do

- **Profile first**: Identify bottlenecks before optimizing
- **Use appropriate data structures**: Choose components wisely
- **Minimize allocations**: Reuse collections, use structs
- **Test determinism**: Verify reproducible simulations

### ❌ Don't

- **Don't over-optimize**: Premature optimization is the root of all evil
- **Don't break determinism**: Avoid non-deterministic operations in fixed-step
- **Don't mix concerns**: Keep systems focused and isolated

## See Also

- [Performance Guide](../core/performance.md) - Performance optimization details
- [Architecture](../overview/architecture.md) - System architecture
- [Extending ZenECS](./extending-zenecs.md) - Extension patterns
