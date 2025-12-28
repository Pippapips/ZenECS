# 05 - World Reset

> Docs / Samples / 05 - World Reset

Reset world state while keeping systems registered. Useful for game restarts and state management.

## Prerequisites

- **.NET SDK 8.0+** or **Unity 2021.3+**
- **ZenECS Core** package installed
- Understanding of [World Reset](../core/world-reset.md)

## Run It

### .NET Standalone

```bash
cd Packages/com.zenecs.core/Samples~/05-WorldReset
dotnet run
```

### Unity

1. Open Unity project
2. Open scene: `Packages/com.zenecs.core/Samples~/05-WorldReset/Scene.unity`
3. Press Play

## Code Walkthrough

### World Reset

Reset clears all entities and components but keeps systems:

```csharp
// Setup world with systems
var world = kernel.CreateWorld(null, "GameWorld");
world.AddSystems([new MovementSystem(), new HealthSystem()]);

// Create entities
Entity player;
using (var cmd = world.BeginWrite())
{
    player = cmd.CreateEntity();
    cmd.AddComponent(player, new Position(0, 0));
}

// Reset world (entities destroyed, systems remain)
world.Reset();

// Systems are still registered
// Ready for new simulation
```

### Complete Example

```csharp
using ZenECS.Core;
using ZenECS.Core.Systems;

[FixedGroup]
public sealed class GameSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        // Game logic
    }
}

// Setup
var kernel = new Kernel();
var world = kernel.CreateWorld(null, "GameWorld");
world.AddSystems([new GameSystem()]);

// Game loop
bool gameRunning = true;
while (gameRunning)
{
    // Run simulation
    kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame: 4);
    
    // Check for reset
    if (ShouldReset())
    {
        world.Reset();  // Clear entities, keep systems
        InitializeGame(world);  // Recreate entities
    }
}

// Cleanup
kernel.Dispose();
```

## Use Cases

### Game Restart

```csharp
public void RestartGame()
{
    // Reset world (clears all entities)
    world.Reset();
    
    // Reinitialize game state
    Entity player;
    using (var cmd = world.BeginWrite())
    {
        player = cmd.CreateEntity();
        cmd.AddComponent(player, new Position(0, 0));
        cmd.AddComponent(player, new Health(100, 100));
    }
}
```

### Level Transition

```csharp
public void LoadLevel(int levelIndex)
{
    // Reset current level
    world.Reset();
    
    // Load new level entities
    LoadLevelEntities(world, levelIndex);
}
```

## Best Practices

### ✅ Do

- **Use for restarts**: Fast way to reset state
- **Keep systems**: Systems remain registered
- **Reinitialize**: Recreate entities after reset
- **Test thoroughly**: Verify reset works correctly

### ❌ Don't

- **Don't reset during iteration**: Reset at safe boundaries
- **Don't forget to reinitialize**: Entities are cleared
- **Don't reset unnecessarily**: Only when needed

## See Also

- [World Reset](../core/world-reset.md) - Detailed documentation
- [World Guide](../core/world.md) - World management
- [Advanced Topics](../guides/advanced-topics.md) - Advanced patterns
