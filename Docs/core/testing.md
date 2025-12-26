# Testing

> Docs / Core / Testing

Unit and integration testing guidelines for ZenECS.

## Overview

ZenECS is designed for testability:

- **Pure Systems**: Systems are pure functions
- **Mockable APIs**: Interfaces enable mocking
- **Isolated Worlds**: Test worlds are isolated
- **Deterministic**: Reproducible simulations

## Unit Testing

### Testing Systems

```csharp
[Test]
public void MovementSystem_MovesEntities()
{
    // Arrange
    var kernel = new Kernel();
    var world = kernel.CreateWorld(null, "Test");
    var system = new MovementSystem();
    world.AddSystems([system]);
    
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
    
    kernel.Dispose();
}
```

### Testing Components

```csharp
[Test]
public void Component_AddRemove()
{
    var world = kernel.CreateWorld(null, "Test");
    Entity entity;
    using (var cmd = world.BeginWrite())
    {
        entity = cmd.CreateEntity();
        cmd.AddComponent(entity, new Position(0, 0));
    Assert.IsTrue(world.HasComponent<Position>(entity));
    
    // Remove component
    world.RemoveComponent<Position>(entity);
    Assert.IsFalse(world.HasComponent<Position>(entity));
}
```

## Integration Testing

### Testing World Scenarios

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
        cmd.AddComponent(player, new Health(100, 100));
    }
    
    // Execute
    kernel.PumpAndLateFrame(1f, 1f / 60f, maxSubStepsPerFrame: 4);
    
    // Verify
    Assert.IsTrue(world.IsAlive(player));
    
    kernel.Dispose();
}
```

## Best Practices

### ✅ Do

- **Test in isolation**: Use test worlds
- **Test determinism**: Verify reproducible results
- **Test edge cases**: Empty worlds, single entities
- **Clean up**: Dispose kernels and worlds

### ❌ Don't

- **Don't test implementation**: Test behavior
- **Don't rely on order**: Test independently
- **Don't skip cleanup**: Always dispose

## See Also

- [Advanced Topics](../guides/advanced-topics.md) - Advanced patterns
- [World](./world.md) - World management
- [Systems](./systems.md) - System design
