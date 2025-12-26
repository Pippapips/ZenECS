# World Reset

> Docs / Core / World Reset

Reset world state while keeping systems registered.

## Overview

World reset:

- **Clears Entities**: All entities destroyed
- **Clears Components**: All components removed
- **Keeps Systems**: Systems remain registered
- **Fast Reset**: Efficient state clearing

## API Surface

### Reset World

```csharp
// Reset world (clears entities, keeps systems)
world.Reset();
```

## Examples

### Game Restart

```csharp
public void RestartGame()
{
    // Reset world
    world.Reset();
    
    // Reinitialize
    Entity player;
    using (var cmd = world.BeginWrite())
    {
        player = cmd.CreateEntity();
        cmd.AddComponent(player, new Position(0, 0));
    }
}
```

## See Also

- [World](./world.md) - World management
- [Samples](../samples/05-world-reset.md) - Sample project
- [Advanced Topics](../guides/advanced-topics.md) - Advanced patterns
