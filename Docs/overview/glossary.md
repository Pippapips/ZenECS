# Glossary

> Docs / Overview / Glossary

Terminology reference for ZenECS concepts and APIs.

## Core Concepts

### Entity

A unique identifier representing a game object. Entities have no data or behavior—they're just IDs that can have components attached.

```csharp
Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();  // Just an ID
}
```

**Related**: [Entities](../core/entities.md)

### Component

A pure data structure (struct) that defines properties. Components have no methods or behavior—they're just data.

```csharp
public struct Position { public float X, Y, Z; }
```

**Related**: [Components](../core/components.md)

### System

Pure logic that operates on entities with specific component combinations. Systems query entities and transform their components.

```csharp
public class MovementSystem : ISystem
{
    public void Run(IWorld world, float deltaTime) { }
}
```

**Related**: [Systems](../core/systems.md)

### World

A container for entities, components, and systems. Represents an isolated simulation space.

```csharp
var world = kernel.CreateWorld(null, "GameWorld");
```

**Related**: [World](../core/world.md)

### Kernel

Top-level manager that manages multiple worlds and coordinates the game loop.

```csharp
var kernel = new Kernel();
```

**Related**: [EcsKernel](../core/ecs-kernel.md)

## API Terms

### Query

Find entities that have specific component combinations.

```csharp
foreach (var (entity, pos, vel) in world.Query<Position, Velocity>())
{
    // Entities with both Position and Velocity
}
```

**Related**: [Query API](../core/world.md#query-api)

### Command Buffer

Batch structural changes (entity creation/destruction, component add/remove) and apply at safe boundaries.

```csharp
using (var cmd = world.BeginWrite())
{
    var entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position(0, 0));
}
```

**Related**: [Command Buffer](../core/command-buffer.md)

### Message Bus

Pub/sub messaging system for event-driven architecture.

```csharp
world.Publish(new DamageMessage { Target = entity, Amount = 10 });
```

**Related**: [Message Bus](../core/message-bus.md)

### Binding

Connect ECS data to view layer (Unity GameObjects, UI, etc.) reactively.

```csharp
public class PositionBinder : IBinder<Position>
{
    public void OnDelta(ComponentDelta<Position> delta) { }
}
```

**Related**: [Binding](../core/binding.md)

## System Groups

### FixedGroup

Systems that run during fixed-step simulation. Use for deterministic gameplay logic.

```csharp
[FixedGroup]
public class PhysicsSystem : ISystem { }
```

**Related**: [System Runner](../core/system-runner.md)

### FrameGroup

Systems that run during variable-timestep frame updates. Use for presentation and UI.

```csharp
[FrameGroup]
public class CameraSystem : ISystem { }
```

**Related**: [System Runner](../core/system-runner.md)

## Unity-Specific Terms

### EcsDriver

Unity MonoBehaviour that manages kernel lifecycle and bridges Unity's frame callbacks.

```csharp
[RequireComponent(typeof(EcsDriver))]
public class GameBootstrap : MonoBehaviour { }
```

**Related**: [Unity Adapter Overview](../adapter-unity/overview.md)

### EntityLink

Unity MonoBehaviour that links a GameObject to an ECS entity.

```csharp
var link = gameObject.AddComponent<EntityLink>();
link.Attach(world, entity);
```

**Related**: [View Binder](../adapter-unity/view-binder.md)

### EntityBlueprint

ScriptableObject asset that defines entity configuration (components, contexts, binders).

```csharp
[CreateAssetMenu]
public class PlayerBlueprint : EntityBlueprint { }
```

**Related**: [Blueprint Components](../guides/blueprint-components.md)

## Advanced Concepts

### Snapshot

Serialized world state for save/load functionality.

```csharp
world.Save(stream);  // Create snapshot
world.Load(stream);  // Restore from snapshot
```

**Related**: [Snapshot I/O](../core/snapshot-io.md)

### Migration

Version compatibility system for snapshots.

```csharp
world.RegisterMigration<OldComponent, NewComponent>(migrate);
```

**Related**: [Migration & PostMig](../core/migration-postmig.md)

### Hook

Callback system for intercepting component operations.

```csharp
world.Hooks.AddWritePermission((entity, type) => true);
```

**Related**: [World Hook](../core/world-hook.md)

### Validator

Validation function for component values.

```csharp
world.Hooks.AddValidator<Health>(health => health.Current >= 0);
```

**Related**: [Write Hooks & Validators](../core/write-hooks-validators.md)

## Common Patterns

### Query Pattern

Find and process entities with specific components.

```csharp
foreach (var (entity, component) in world.Query<Component>())
{
    // Process entity
}
```

### Message Pattern

Publish events and react in systems.

```csharp
// Publish
world.Publish(new EventMessage());

// Subscribe (in system)
foreach (var msg in world.ConsumeMessages<EventMessage>())
{
    // React to event
}
```

### Binding Pattern

Reactively update views when components change.

```csharp
public class ViewBinder : IBinder<Position>
{
    public void OnDelta(ComponentDelta<Position> delta)
    {
        if (delta.IsAdded || delta.IsChanged)
        {
            transform.position = delta.NewValue;
        }
    }
}
```

## See Also

- [ZenECS at a Glance](./zenecs-at-a-glance.md) - Feature overview
- [What is ECS?](./what-is-ecs.md) - ECS introduction
- [Architecture](./architecture.md) - Technical details
