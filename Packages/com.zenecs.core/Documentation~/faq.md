# FAQ

> Docs / Overview / FAQ

Frequently asked questions about ZenECS.

## General Questions

### What is ZenECS?

ZenECS is a pure C# Entity-Component-System (ECS) runtime designed for clarity, determinism, and zero dependencies. It provides a clean architecture for game development with optional Unity integration.

### Why should I use ZenECS?

- **Clean Architecture**: Strict separation of data, simulation, and presentation
- **Testability**: Systems are pure functions, easy to test
- **Engine Independence**: Works with Unity, Godot, or standalone .NET
- **Deterministic**: Reproducible simulations for networking and replays
- **Well Documented**: Comprehensive documentation and examples

### Is ZenECS production-ready?

Yes! ZenECS 1.0 is a Release Candidate with locked APIs. It's being used in production projects and is ready for serious game development.

### How does ZenECS compare to Unity DOTS?

| Aspect | ZenECS | Unity DOTS |
|--------|--------|------------|
| **Performance** | Good | Excellent |
| **Clarity** | High | Medium |
| **Learning Curve** | Gentle | Steep |
| **Engine Dependency** | None | Unity only |
| **.NET Support** | Full | Unity only |

ZenECS prioritizes clarity and maintainability over maximum performance.

## Installation & Setup

### How do I install ZenECS?

**Unity (UPM):**
```
https://github.com/Pippapips/ZenECS.git?path=Packages/com.zenecs.core
```

**NuGet:**
```bash
dotnet add package ZenECS.Core
```

See [Installation Guide](../getting-started/install-upm.md) for details.

### What are the requirements?

- **.NET Standard 2.1** or higher
- **Unity 2021.3+** (for Unity adapter)
- No external dependencies (Core package)

### Do I need both Core and Unity Adapter?

- **Core**: Required for all projects
- **Unity Adapter**: Optional, only needed for Unity integration

## Usage Questions

### How do I create an entity?

```csharp
Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();
}
```

Or with command buffer:

```csharp
using (var cmd = world.BeginWrite())
{
    var entity = cmd.CreateEntity();
}
```

### How do I add components?

```csharp
using (var cmd = world.BeginWrite())
{
    cmd.AddComponent(entity, new Position(0, 0));
}
```

Or with command buffer:

```csharp
using (var cmd = world.BeginWrite())
{
    cmd.AddComponent(entity, new Position(0, 0));
}
```

### How do I query entities?

```csharp
foreach (var (entity, pos, vel) in world.Query<Position, Velocity>())
{
    // Process entities with both Position and Velocity
}
```

### How do I write a system?

```csharp
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
```

### When should I use Command Buffers?

Use command buffers when:
- Creating or destroying entities
- Adding or removing components
- Making structural changes during system execution

Command buffers ensure changes are applied at safe boundaries.

## Architecture Questions

### Why struct components?

- **Performance**: Value types, no heap allocation
- **Cache-friendly**: Contiguous memory layout
- **Immutability**: Encourages functional programming

### Why multiple worlds?

Multiple worlds enable:
- **Split-screen**: Separate worlds for each player
- **Server/Client**: Isolated simulation spaces
- **Game modes**: Different worlds for different modes
- **Testing**: Isolated test worlds

### How does the message bus work?

Messages are struct-based pub/sub:

```csharp
// Publish
world.Publish(new DamageMessage { Target = entity, Amount = 10 });

// Subscribe (in system)
foreach (var msg in world.ConsumeMessages<DamageMessage>())
{
    // React to message
}
```

Messages are processed deterministically during frame updates.

### What is binding?

Binding connects ECS data to view layer reactively:

```csharp
public class PositionBinder : IBinder<Position>
{
    public void OnDelta(ComponentDelta<Position> delta)
    {
        transform.position = delta.NewValue;
    }
}
```

Binders automatically receive updates when components change.

## Performance Questions

### Is ZenECS fast?

ZenECS is designed for good performance with a focus on clarity:
- **Cache-friendly**: Components in contiguous arrays
- **Zero allocation**: Queries use struct enumerators
- **Efficient**: Optimized for common use cases

For maximum performance, consider Unity DOTS.

### Can I use ZenECS for large-scale games?

Yes! ZenECS scales well:
- **Thousands of entities**: Handled efficiently
- **Many systems**: Parallel execution possible
- **Memory efficient**: Component pooling and recycling

### How does performance compare to Unity DOTS?

Unity DOTS is faster for very large-scale simulations (millions of entities). ZenECS prioritizes clarity and maintainability over maximum performance.

## Unity Integration

### How do I integrate with Unity?

Use the Unity Adapter package:

1. Install `com.zenecs.adapter.unity`
2. Add `EcsDriver` to your scene
3. Use `EntityLink` to connect GameObjects
4. Create systems and register them

See [Unity Adapter Overview](../adapter-unity/overview.md) for details.

### Can I use ZenECS without Unity?

Yes! ZenECS Core has zero dependencies and works in any .NET environment:
- Standalone .NET applications
- Game servers
- Simulations
- Other game engines (with custom adapter)

### How do EntityLink and GameObjects work?

`EntityLink` connects Unity GameObjects to ECS entities:

```csharp
var link = gameObject.AddComponent<EntityLink>();
link.Attach(world, entity);
```

The link maintains the connection and can be queried to find the GameObject for an entity.

## Advanced Questions

### How do I save/load game state?

Use snapshots:

```csharp
// Save
using var stream = File.Create("save.dat");
world.Save(stream);

// Load
using var stream = File.OpenRead("save.dat");
world.Load(stream);
```

See [Snapshot I/O](../core/snapshot-io.md) for details.

### How do I handle version migrations?

Register migration functions:

```csharp
world.RegisterMigration<OldComponent, NewComponent>((old) => new NewComponent
{
    // Convert old to new
});
```

See [Migration & PostMig](../core/migration-postmig.md) for details.

### Can I extend ZenECS?

Yes! ZenECS is designed for extensibility:
- **Custom systems**: Implement `ISystem`
- **Custom binders**: Implement `IBinder<T>`
- **Custom hooks**: Add write permissions and validators
- **Custom formatters**: Implement `IComponentFormatter`

See [Extending ZenECS](../guides/extending-zenecs.md) for details.

## Troubleshooting

### My system isn't running

Check:
1. System is registered: `world.AddSystems([new MySystem()])`
2. System has correct attribute: `[FixedGroup]` or `[FrameGroup]`
3. World is being stepped: `kernel.PumpAndLateFrame(...)`
4. Entities match query: Entities have required components

### Entities aren't being found

Check:
1. Entities have required components
2. Query matches component types
3. Entities are alive: `world.IsAlive(entity)`
4. World is correct: Using the right world instance

### Components aren't updating

Check:
1. Using command buffer for writes: `using var cmd = world.BeginWrite()`
2. Replacing components: `cmd.ReplaceComponent(...)`
3. System is running: Check system registration and execution
4. World is being stepped: `kernel.PumpAndLateFrame(...)`

## See Also

- [Quick Start](../getting-started/quickstart-basic.md) - Get started quickly
- [Troubleshooting](../adapter-unity/troubleshooting.md) - Common issues and solutions
- [Architecture](./architecture.md) - Technical details
- [Glossary](./glossary.md) - Terminology reference
