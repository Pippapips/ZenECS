# 03 - Messages

> Docs / Samples / 03 - Messages

Use the message bus for event-driven architecture. View publishes messages, systems consume them.

## Prerequisites

- **.NET SDK 8.0+** or **Unity 2021.3+**
- **ZenECS Core** package installed
- Understanding of [Message Bus](../core/message-bus.md)

## Run It

### .NET Standalone

```bash
cd Packages/com.zenecs.core/Samples~/03-Messages
dotnet run
```

### Unity

1. Open Unity project
2. Open scene: `Packages/com.zenecs.core/Samples~/03-Messages/Scene.unity`
3. Press Play

## Code Walkthrough

### Step 1: Define Message

```csharp
public struct DamageMessage
{
    public Entity Target;
    public float Amount;
}
```

**Key Points:**
- Messages are `struct` (value types)
- No methods, just data
- Immutable by convention

### Step 2: Publish Message

```csharp
// Publish from anywhere
var world = KernelLocator.CurrentWorld;
world.Publish(new DamageMessage
{
    Target = entity,
    Amount = 10
});
```

### Step 3: Consume Messages in System

```csharp
[FixedGroup]
public class HealthSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        using var cmd = world.BeginWrite();
        
        // Consume all damage messages
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

## Complete Example

```csharp
using ZenECS.Core;
using ZenECS.Core.Systems;

// Message
public struct DamageMessage
{
    public Entity Target;
    public float Amount;
}

// Component
public struct Health
{
    public float Current;
    public float Max;
}

// System
[FixedGroup]
public sealed class HealthSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        using var cmd = world.BeginWrite();
        
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

// Usage
var world = kernel.CreateWorld(null, "GameWorld");
world.AddSystems([new HealthSystem()]);

Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Health { Current = 100, Max = 100 });
}

// Publish damage
world.Publish(new DamageMessage { Target = entity, Amount = 25 });

// Process messages (happens during frame update)
kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame: 4);
```

## Message Pump Timing

Messages are processed during frame updates:

```csharp
// Frame structure
kernel.BeginFrame(dt);        // Messages pumped here
kernel.FixedStep(fixedDelta);  // Systems run here
kernel.LateFrame(dt, alpha);  // Presentation here
```

**Key Points:**
- Messages are **pumped** in `BeginFrame`
- Systems **consume** messages during `FixedStep` or `LateFrame`
- Messages are **deterministic** (struct-based)

## Patterns

### Request-Response Pattern

```csharp
// Request message
public struct GetHealthRequest
{
    public Entity Target;
}

// Response message
public struct HealthResponse
{
    public Entity Target;
    public float Current;
    public float Max;
}

// System handles request
[FixedGroup]
public class HealthQuerySystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        foreach (var req in world.ConsumeMessages<GetHealthRequest>())
        {
            if (world.HasComponent<Health>(req.Target))
            {
                var health = world.Get<Health>(req.Target);
                world.Publish(new HealthResponse
                {
                    Target = req.Target,
                    Current = health.Current,
                    Max = health.Max
                });
            }
        }
    }
}
```

### Event Pattern

```csharp
// Event message
public struct EntityDestroyedEvent
{
    public Entity Entity;
    public string Reason;
}

// Publish event
world.Publish(new EntityDestroyedEvent
{
    Entity = entity,
    Reason = "Health depleted"
});

// Subscribe to event
[FixedGroup]
public class EventHandlerSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        foreach (var evt in world.ConsumeMessages<EntityDestroyedEvent>())
        {
            Debug.Log($"Entity {evt.Entity.Id} destroyed: {evt.Reason}");
        }
    }
}
```

## What to Try Next

### Experiment 1: Multiple Message Types

Handle multiple message types:

```csharp
[FixedGroup]
public class MultiMessageSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        // Process damage
        foreach (var msg in world.ConsumeMessages<DamageMessage>())
        {
            // Handle damage
        }
        
        // Process healing
        foreach (var msg in world.ConsumeMessages<HealMessage>())
        {
            // Handle healing
        }
    }
}
```

### Experiment 2: Message Chains

Chain messages together:

```csharp
// First message triggers second
world.Publish(new DamageMessage { Target = entity, Amount = 10 });

// System processes and publishes result
[FixedGroup]
public class DamageSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        foreach (var msg in world.ConsumeMessages<DamageMessage>())
        {
            // Process damage
            world.Publish(new HealthChangedEvent { Entity = msg.Target });
        }
    }
}
```

## Next Samples

- **[04 - Command Buffer](../samples/04-command-buffer.md)** - Structural changes
- **[05 - World Reset](../samples/05-world-reset.md)** - State management
- **[Message Bus Guide](../core/message-bus.md)** - Detailed message system

## See Also

- [Message Bus](../core/message-bus.md) - Message system details
- [Systems Guide](../core/systems.md) - System design
- [Input → Intent](../adapter-unity/input-intent.md) - Input pattern
