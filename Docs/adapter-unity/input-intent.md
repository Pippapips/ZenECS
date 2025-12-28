# Input → Intent

> Docs / Adapter (Unity) / Input → Intent

Convert Unity Input to ECS intent components using the message bus pattern.

## Overview

The **Input → Intent** pattern separates input handling from game logic:

1. **Input Layer**: Unity Input System captures input
2. **Intent Messages**: Convert input to ECS messages
3. **Intent Systems**: Process messages and create intent components
4. **Gameplay Systems**: React to intent components

This pattern enables:
- **Clean separation**: Input code doesn't know about game logic
- **Testability**: Test gameplay without Unity Input
- **Flexibility**: Swap input sources easily

## How It Works

### Flow

```
Unity Input
    ↓
Input Handler (MonoBehaviour)
    ↓
Publish Intent Message
    ↓
Intent System (ECS)
    ↓
Create Intent Component
    ↓
Gameplay System (ECS)
```

### Example

```csharp
// 1. Input Handler (Unity)
public class InputHandler : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            var world = KernelLocator.CurrentWorld;
            world.Publish(new MoveIntentMessage { Direction = Vector2.up });
        }
    }
}

// 2. Intent Component
public struct MoveIntent
{
    public Vector2 Direction;
}

// 3. Intent System
[FixedGroup]
public class IntentSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        using var cmd = world.BeginWrite();
        
        foreach (var msg in world.ConsumeMessages<MoveIntentMessage>())
        {
            if (world.HasComponent<Player>(msg.Entity))
            {
                cmd.AddOrReplaceComponent(msg.Entity, new MoveIntent
                {
                    Direction = msg.Direction
                });
            }
        }
    }
}

// 4. Gameplay System
[FixedGroup]
[OrderAfter(typeof(IntentSystem))]
public class MovementSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        using var cmd = world.BeginWrite();
        
        foreach (var (entity, pos, intent) in world.Query<Position, MoveIntent>())
        {
            cmd.ReplaceComponent(entity, new Position(
                pos.X + intent.Direction.X * deltaTime,
                pos.Y + intent.Direction.Y * deltaTime
            ));
            
            // Clear intent after processing
            cmd.RemoveComponent<MoveIntent>(entity);
        }
    }
}
```

## Best Practices

### ✅ Do

- **Use messages**: Convert input to messages immediately
- **Create intent components**: Systems create intent from messages
- **Clear intents**: Remove intent components after processing
- **Separate concerns**: Input handler doesn't know about gameplay

### ❌ Don't

- **Don't access GameObjects in systems**: Use messages instead
- **Don't store input state**: Convert to messages immediately
- **Don't mix input and gameplay**: Keep layers separate

## See Also

- [Message Bus](../core/message-bus.md) - Message system details
- [Unity Adapter Overview](./overview.md) - Unity integration
- [Systems Guide](../core/systems.md) - System design
