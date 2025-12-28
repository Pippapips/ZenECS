# Message Bus

> Docs / Core / Message Bus

Struct-based pub/sub messaging system for event-driven architecture.

## Overview

The **Message Bus** enables:

- **Event-Driven Architecture**: Systems react to messages
- **Decoupling**: Systems don't know about each other
- **Deterministic Delivery**: Struct-based, predictable
- **Type-Safe**: Compile-time type checking

### Key Concepts

- **Messages**: Struct-based event data
- **Publish**: Send messages to the bus
- **Consume**: Systems consume messages
- **Pump**: Process messages during frame updates

## How It Works

### Message Flow

```
Publisher → Message Bus → Subscribers
```

1. **Publish**: `world.Publish(new Message())`
2. **Pump**: Messages processed in `BeginFrame`
3. **Consume**: Systems consume messages during execution

### Message Structure

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

## API Surface

### Publishing Messages

```csharp
// Publish message
world.Publish(new DamageMessage
{
    Target = entity,
    Amount = 10
});
```

### Consuming Messages

```csharp
// Consume messages in system
foreach (var msg in world.ConsumeMessages<DamageMessage>())
{
    // Process message
}
```

### Message Pumping

Messages are automatically pumped during `BeginFrame`:

```csharp
kernel.BeginFrame(dt);  // Messages pumped here
kernel.FixedStep(fixedDelta);  // Systems consume here
```

## Examples

### Basic Message Pattern

```csharp
using ZenECS.Core;
using ZenECS.Core.Systems;

// Message
public struct DamageMessage
{
    public Entity Target;
    public float Amount;
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
    cmd.AddComponent(entity, new Health(100, 100));
}

// Publish damage
world.Publish(new DamageMessage { Target = entity, Amount = 25 });

// Process (messages pumped in BeginFrame)
kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame: 4);
```

### Multiple Message Types

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

## Best Practices

### ✅ Do

- **Use structs**: Messages should be value types
- **Keep immutable**: Use readonly fields
- **Consume in systems**: Process messages during execution
- **Type carefully**: Choose appropriate message types

### ❌ Don't

- **Don't store state**: Messages are data only
- **Don't mutate messages**: Keep them immutable
- **Don't publish in systems**: Publish from external code
- **Don't overuse**: Use for events, not for data storage

## Common Patterns

### Event Pattern

```csharp
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

### Request-Response Pattern

```csharp
// Request
public struct GetHealthRequest
{
    public Entity Target;
}

// Response
public struct HealthResponse
{
    public Entity Target;
    public float Current;
    public float Max;
}

// Handler
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

## See Also

- [Systems](./systems.md) - System design
- [Input → Intent](../adapter-unity/input-intent.md) - Input pattern
- [World](./world.md) - World API
