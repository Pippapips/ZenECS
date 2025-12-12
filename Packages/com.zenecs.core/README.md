# ZenECS Core

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Standard](https://img.shields.io/badge/.NET-Standard%202.1-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black.svg)](https://unity.com/)

**ZenECS Core** is a pure C# **Entity-Component-System (ECS)** runtime focused on clarity, determinism, and zero dependencies. It operates independently of Unity and is suitable for both game engines and standalone .NET applications.

## ✨ Key Features

- **Multi-World Kernel** — Create and manage multiple worlds, select current world, and step all worlds deterministically
- **Clean World API** — Unified API integrating components, queries, command buffers, messages, hooks, and contexts/binders
- **Zero Dependencies** — No external frameworks; lightweight internal DI and worker queue
- **Deterministic Stepping** — `BeginFrame → FixedStep×N → LateFrame` structure with explicit ordering attributes
- **Thread Safety** — Safe multi-threaded access using concurrent collections and lock-based snapshots
- **Snapshot I/O** — Pluggable backend + `IComponentFormatter` + post-load migrations
- **Binding Layer** — Contexts + binders for view integration (Unity, UI, audio, etc.), separated from Core data flow
- **Testable** — Small public surface, sealed scopes, clear lifecycle; samples included

> **Status:** Release Candidate — 1.0 APIs locked, documentation/samples being finalized

---

## 📦 Installation

### Unity (UPM)

#### Install via Git URL

1. Open **Package Manager** → **Add package from git URL…**
2. Enter the following URL:
   ```
   https://github.com/Pippapips/ZenECS_deprecated.git?path=Packages/com.zenecs.core#v1.0.0
   ```

#### Local Development

Place the repository under your project and reference via `file:` URL or add an entry in `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.zenecs.core": "file:../../ZenECS_deprecated/Packages/com.zenecs.core"
  }
}
```

### .NET (non-Unity)

Add the Core folder as a project/solution reference, or use the NuGet package when available:

```bash
dotnet add package ZenECS.Core --version 1.0.0
```

---

## 🚀 Quick Start

### Complete Example

For a full example, see `Packages/com.zenecs.core/Samples~/01-Basic/Basic.cs`.

```csharp
using ZenECS.Core;
using ZenECS.Core.Systems;

// 1. Create kernel and world
var kernel = new Kernel();
var world = kernel.CreateWorld("Game");

// 2. Define components
public readonly struct Position 
{ 
    public readonly float X, Y; 
    public Position(float x, float y) { X = x; Y = y; }
}

public readonly struct Velocity 
{ 
    public readonly float X, Y; 
    public Velocity(float x, float y) { X = x; Y = y; }
}

// 3. Write system
[SimulationGroup]
public sealed class MoveSystem : IFixedRunSystem
{
    public void Run(IWorld world, float fixedDelta)
    {
        foreach (var entity in world.Query<Position, Velocity>())
        {
            ref var pos = ref world.Ref<Position>(entity);
            var vel = world.Get<Velocity>(entity);
            
            pos = new Position(
                pos.X + vel.X * fixedDelta,
                pos.Y + vel.Y * fixedDelta
            );
        }
    }
}

// 4. Create entity and add components
var entity = world.CreateEntity();
world.AddComponent(entity, new Position(0, 0));
world.AddComponent(entity, new Velocity(1, 0));

// 5. Register systems
world.AddSystems([new MoveSystem()]);

// 6. Game loop
const float fixedDelta = 1f / 60f;
while (running)
{
    float dt = GetDeltaTime();
    kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame: 4);
}

// 7. Cleanup
kernel.Dispose();
```

### Writing Systems

```csharp
using ZenECS.Core;
using ZenECS.Core.Systems;

[SimulationGroup]
[OrderAfter(typeof(PhysicsSystem))]
public sealed class MoveSystem : IFixedRunSystem
{
    public void Run(IWorld world, float fixedDelta)
    {
        var filter = Filter.New
            .With<Position>()
            .With<Velocity>()
            .Build();

        foreach (var entity in world.Query<Position, Velocity>(filter))
        {
            ref var pos = ref world.Ref<Position>(entity);
            var vel = world.Get<Velocity>(entity);
            
            pos = new Position(
                pos.X + vel.X * fixedDelta,
                pos.Y + vel.Y * fixedDelta
            );
        }
    }
}
```

---

## 🧭 Core Concepts

This section provides a comprehensive guide to the core concepts of ZenECS Core.

### Kernel

The **Kernel** is the top-level manager that manages multiple worlds and orchestrates frame ticks.

**Key Responsibilities:**
- Create/destroy multiple worlds and lookup by ID/name/tag
- Current world selection and change events
- Pause/resume support
- Thread-safe world indexing

**Basic Usage:**

```csharp
using ZenECS.Core;

// Create kernel
var kernel = new Kernel(new KernelOptions
{
    AutoSelectNewWorld = true,
    StepOnlyCurrentWhenSelected = false
});

// Create worlds
var world1 = kernel.CreateWorld("World1");
var world2 = kernel.CreateWorld("World2");

// Set current world
kernel.SetCurrentWorld(world1);

// Game loop
kernel.BeginFrame(dt: 1f / 60f);
kernel.FixedStep(fixedDelta: 1f / 60f);
kernel.LateFrame(alpha: 1f);

// Or use convenience method
kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame);

// Cleanup
kernel.Dispose();
```

**Frame Structure:**

```
BeginFrame (variable timestep)
  ↓
FixedStep × N (fixed timestep, simulation)
  ↓
LateFrame (presentation, read-only)
```

### World

**World** is the unified public API that integrates all ECS functionality. It represents a single simulation space.

**Key Features:**
- **Entities**: Entity creation/destruction, lifecycle management
- **Components**: Component add/remove/query, type-segregated pooling
- **Query**: Fast entity queries with filtering support
- **CommandBuffer**: Buffer structural changes and apply at safe boundaries
- **Messages**: Struct-based Pub/Sub messaging
- **Contexts & Binders**: Binding system for view integration
- **Hooks**: Write permissions and validation hooks
- **Snapshot**: Save/load and migrations
- **Reset**: Fast world reset

**Basic Usage:**

```csharp
// Create world (via kernel)
var world = kernel.CreateWorld("GameWorld");

// Create entity
var entity = world.CreateEntity();

// Add components
world.AddComponent(entity, new Position { X = 0, Y = 0 });
world.AddComponent(entity, new Velocity { X = 1, Y = 0 });

// Read/write components
ref var pos = ref world.Ref<Position>(entity);  // Modify by reference
var vel = world.Get<Velocity>(entity);          // Read by value

// Check component
bool hasPos = world.HasComponent<Position>(entity);

// Destroy entity
world.DestroyEntity(entity);
```

### Entity

**Entity** is a container for components. It is represented as a simple ID and contains no data itself.

**Characteristics:**
- Entities have unique IDs
- Entities themselves contain no data
- Data is stored through components
- Entities can be created/destroyed, and destroyed entities are not reused

**Usage Example:**

```csharp
// Create entities
var player = world.CreateEntity();
var enemy = world.CreateEntity();

// Check if entity is alive
if (world.IsAlive(player))
{
    // Entity is alive
}

// Access entity ID
int id = player.Id;
```

### Component

**Component** is a struct that stores data. It is a value type and stored in type-segregated pools.

**Characteristics:**
- Components must be `struct` value types
- Immutability pattern is recommended (`readonly struct`)
- Stored in type-segregated pools for memory efficiency
- Components can be added/removed from entities

**Component Definition Example:**

```csharp
// Position component
public readonly struct Position
{
    public readonly float X, Y;
    public Position(float x, float y)
    {
        X = x;
        Y = y;
    }
}

// Velocity component
public readonly struct Velocity
{
    public readonly float X, Y;
    public Velocity(float x, float y)
    {
        X = x;
        Y = y;
    }
}

// Tag component (no data)
public readonly struct Paused { }
```

**Component Operations:**

```csharp
// Add
world.AddComponent(entity, new Position(0, 0));

// Read (by value)
var pos = world.Get<Position>(entity);

// Read (read-only)
var pos = world.ReadComponent<Position>(entity);

// Modify (by reference)
ref var pos = ref world.Ref<Position>(entity);
pos = new Position(pos.X + 1, pos.Y);

// Set
world.SetComponent(entity, new Position(10, 20));

// Remove
world.RemoveComponent<Velocity>(entity);

// Check
bool hasPos = world.HasComponent<Position>(entity);
```

### Systems

**System** is a class that encapsulates game logic. It queries and modifies components to update game state.

**System Types:**

ZenECS provides the following system interfaces:

1. **`IFrameSetupSystem`**: One-time initialization per frame
2. **`IFixedSetupSystem`**: One-time initialization per fixed step
3. **`IVariableRunSystem`**: Variable timestep execution (BeginFrame)
4. **`IFixedRunSystem`**: Fixed timestep execution (SimulationGroup)
5. **`IPresentationSystem`**: Presentation phase (LateFrame)
6. **`ISystemLifecycle`**: Lifecycle management (Initialize/Shutdown)

**System Groups:**

Systems are categorized into groups:

- **`[SimulationGroup]`**: Simulation phase (FixedStep)
- **`[PresentationGroup]`**: Presentation phase (LateFrame)

**System Ordering:**

```csharp
[SimulationGroup]
[OrderAfter(typeof(PhysicsSystem))]  // Run after PhysicsSystem
[OrderBefore(typeof(RenderSystem))]   // Run before RenderSystem
public sealed class MoveSystem : IFixedRunSystem
{
    public void Run(IWorld world, float fixedDelta)
    {
        // Movement logic
    }
}
```

**System Writing Example:**

```csharp
using ZenECS.Core;
using ZenECS.Core.Systems;

[SimulationGroup]
public sealed class MoveSystem : IFixedRunSystem
{
    public void Run(IWorld world, float fixedDelta)
    {
        // Query all entities with Position and Velocity
        foreach (var entity in world.Query<Position, Velocity>())
        {
            ref var pos = ref world.Ref<Position>(entity);
            var vel = world.Get<Velocity>(entity);
            
            // Update position
            pos = new Position(
                pos.X + vel.X * fixedDelta,
                pos.Y + vel.Y * fixedDelta
            );
        }
    }
}

[PresentationGroup]
public sealed class PrintPositionsSystem : IPresentationSystem
{
    public void Run(IWorld world, float dt, float alpha)
    {
        // Read-only query
        foreach (var entity in world.Query<Position>())
        {
            var pos = world.ReadComponent<Position>(entity);
            Console.WriteLine($"Entity {entity.Id}: {pos}");
        }
    }
}
```

**System Registration:**

```csharp
world.AddSystems([
    new MoveSystem(),
    new PrintPositionsSystem()
]);
```

### Query

**Query** is an efficient way to find entities that have specific components.

**Basic Query:**

```csharp
// Single component query
foreach (var entity in world.Query<Position>())
{
    var pos = world.Get<Position>(entity);
}

// Multiple component query
foreach (var entity in world.Query<Position, Velocity>())
{
    ref var pos = ref world.Ref<Position>(entity);
    var vel = world.Get<Velocity>(entity);
}
```

**Filtering:**

```csharp
using ZenECS.Core.Filters;

// Create filter
var filter = Filter.New
    .With<Position>()      // Requires Position component
    .With<Velocity>()      // Requires Velocity component
    .Without<Paused>()     // Must not have Paused component
    .Build();

// Use filter
foreach (var entity in world.Query<Position, Velocity>(filter))
{
    // Only process entities that satisfy filter conditions
}
```

**Performance Characteristics:**
- **Zero-allocation queries**: Enumerable queries allocate no heap memory
- **Optimized indexing**: Queries are seeded by the smallest component pool
- **BitSet-based**: Dense bitset for component presence tracking

### Command Buffer

**Command Buffer** is a mechanism that buffers structural changes (entity creation/destruction, component add/remove) and applies them in batches at safe boundaries.

**Use Cases:**
- Safely handle structural changes during system execution
- Buffer changes in multi-threaded environments
- Delay changes for deterministic execution

**Basic Usage:**

```csharp
// Begin command buffer
using (var cmd = world.BeginWrite())
{
    // Buffer structural changes
    cmd.AddComponent(entity, new Health { Value = 100 });
    cmd.RemoveComponent<Stunned>(entity);
    cmd.DestroyEntity(entity);
    
    // ReplaceComponent is also supported
    cmd.ReplaceComponent(entity, new Health { Value = 75 });
} // Automatically applied

// Explicit application
world.RunScheduledJobs();
```

**Immediate Application:**

```csharp
using (var cmd = world.BeginWrite())
{
    cmd.AddComponent(entity, new Health { Value = 100 });
}
// EndWrite is automatically called and applied immediately
```

### Message Bus

**Message Bus** is a struct-based Pub/Sub messaging system. It implements unidirectional data flow from the view layer to the data layer.

**Message Definition:**

```csharp
public readonly struct Damage : IMessage
{
    public readonly Entity Target;
    public readonly int Amount;
    
    public Damage(Entity target, int amount)
    {
        Target = target;
        Amount = amount;
    }
}
```

**Subscribe and Publish:**

```csharp
// Subscribe
var subscription = world.Subscribe<Damage>(damage =>
{
    // Handle damage
    if (world.IsAlive(damage.Target) && world.HasComponent<Health>(damage.Target))
    {
        var health = world.Get<Health>(damage.Target);
        var newHealth = new Health(health.Value - damage.Amount);
        world.SetComponent(damage.Target, newHealth);
    }
});

// Publish
world.Publish(new Damage { Target = entity, Amount = 10 });

// Unsubscribe
subscription.Dispose();
```

**Usage Pattern:**

The message bus implements **View → Data** unidirectional flow:

- **View Layer**: Publishes messages without directly modifying World
- **Simulation Layer**: Subscribes to messages and updates components
- **Presentation Layer**: Displays data in read-only mode

### Binding

**Binding** is a system for view integration. It connects ECS data with views (Unity, UI, audio, etc.) through Contexts and Binders.

**Key Concepts:**
- **Context**: Container for view-related data
- **Binder**: Detects component changes and updates views
- **ComponentDelta**: Component change information (old value, new value)

**Binder Writing Example:**

```csharp
using ZenECS.Core.Binding;

public sealed class SpriteBinder : BaseBinder, IBinds<Position>, IRequireContext<SpriteContext>
{
    public void OnDelta(in ComponentDelta<Position> delta)
    {
        // Update view when component changes
        var sprite = GetContext<SpriteContext>().Sprite;
        sprite.transform.position = new Vector3(
            delta.NewValue.X,
            delta.NewValue.Y,
            0
        );
    }
}
```

**Context Definition:**

```csharp
public class SpriteContext : IContext
{
    public SpriteRenderer Sprite { get; set; }
}
```

### Hooks & Validators

**Hooks** and **Validators** provide world-scoped permission checks and value validation.

**Write Permission Hook:**

Controls component write permissions:

```csharp
// Write permission check
world.Hooks.AddWritePermission((entity, componentType) =>
{
    // GodMode component is write-protected
    return componentType != typeof(GodMode);
});
```

**Validator:**

Validates component values:

```csharp
// Health value validation
world.Hooks.AddValidator<Health>(health =>
{
    // Health must be >= 0
    return health.Value >= 0;
});
```

**Usage Example:**

```csharp
// Attempt to set invalid value (validation fails)
world.SetComponent(entity, new Health { Value = -10 }); // Throws exception on validation failure

// Attempt to set component without permission
world.AddComponent(entity, new GodMode()); // Throws exception on permission check failure
```

### Snapshot I/O

**Snapshot I/O** is a feature for saving and loading world state. It supports pluggable backends and formatters.

**Save:**

```csharp
using (var stream = File.Create("save.dat"))
{
    world.Save(stream, new BinaryComponentFormatter());
}
```

**Load:**

```csharp
using (var stream = File.OpenRead("save.dat"))
{
    var migrations = new List<IPostLoadMigration> 
    { 
        new V1ToV2Migration() 
    };
    world.Load(stream, new BinaryComponentFormatter(), migrations);
}
```

**Post-Load Migration:**

You can perform data migration after loading:

```csharp
public class V1ToV2Migration : IPostLoadMigration
{
    public void Migrate(IWorld world)
    {
        // Convert old version data to new version
        foreach (var entity in world.Query<OldComponent>())
        {
            var old = world.Get<OldComponent>(entity);
            world.RemoveComponent<OldComponent>(entity);
            world.AddComponent(entity, new NewComponent(old.Data));
        }
    }
}
```

---

## 📚 Samples

The project includes the following samples:

- **00-Start** — Getting started guide
- **01-Basic** — Basic usage, movement system
- **02-Messages** — Pub/Sub messaging
- **03-CommandBuffer** — Scheduled structural changes
- **04-SnapshotIO-PostMig** — Persistence and post-load migrations
- **05-WorldReset** — World teardown and rebuild patterns
- **06-WriteHooks-Validators** — Permissions and typed validators
- **07-ComponentChangeFeed** — Binder delta flow
- **08-SystemRunner** — Grouping and ordering
- **WorldHooks** — World-level hooks

In Unity, the `Samples~` folder appears in the package entry. In .NET, each sample can be built/run as an independent console project.

---

## 🏗️ Architecture

### Project Structure

```
Runtime/
  Core/            # Primitives, DI, logging, bootstrap
  Execution/
    Kernel/        # IKernel, Kernel, KernelOptions
    Systems/       # System contracts + runner/planner + ordering attributes
    Scheduling/    # Scheduling utilities
  World/           # Public IWorld* APIs and World implementation
  Serialization/   # Snapshot abstractions + binary formatter + stream backend
  Messaging/       # Message bus implementation
  Deterministic/   # Command buffer, external commands
  Binding/         # IBinder/IBinds/IContext/IRequireContext (contracts)
  Events/          # EntityEvents (lifecycle notifications)
  Attributes/      # Component/system attributes
```

### Thread Safety

- **Kernel**: Thread-safe world indexing using `ConcurrentDictionary` and lock-based snapshots
- **World**: Single-threaded usage recommended (use command buffer for structural changes during system execution)
- **Query**: Safe enumeration with read-only snapshots

### Performance Characteristics

- **Zero-allocation queries**: Enumerable queries allocate no heap memory
- **Component pooling**: Type-segregated struct pooling for memory efficiency
- **BitSet-based**: Dense bitset for component presence tracking
- **Optimized indexing**: Queries seeded by smallest component pool

---

## 🔧 Extensibility Points

ZenECS provides several extensibility points:

- **Logging** — Plug your logger via `EcsRuntimeOptions.Log`
- **DI/Services** — Swap world internals by composing your own `CoreBootstrap` child scope
- **Snapshot Backend** — Implement `ISnapshotBackend`
- **Serialization** — Implement `IComponentFormatter` (binary/JSON/custom) and `IPostLoadMigration`
- **Binding** — Provide custom contexts/binders and use the router to validate `IRequireContext<>`

---

## 🆚 ZenECS vs Other ECS Frameworks

| Feature | ZenECS | Other Frameworks |
|---------|--------|------------------|
| **Simplicity** | Few concepts, explicit lifecycle | Complex APIs, implicit behavior |
| **Engine Agnostic** | Unity optional, pure .NET | Unity-only or engine-dependent |
| **Deterministic Model** | Predictable ordering and barriers | Non-deterministic or unclear |
| **Batteries Included** | Messaging, snapshots, command buffers, validators included | Basic features only |
| **Thread Safety** | Multi-threaded environment considered | Single-threaded focused |
| **Dependencies** | Zero external dependencies | External frameworks required |

---

## 🧩 Versioning & Compatibility

- **Target Frameworks**: `.NET Standard 2.1` / `.NET 8` (samples)
- **Unity**: `2021.3+` recommended
- **Versioning**: Semantic Versioning (SemVer)
- **Compatibility**: RC builds may adjust internal details without breaking public contracts

---

## 📖 Documentation

For detailed documentation, see:

- **Sample Code**: See `Packages/com.zenecs.core/Samples~/` folder
- [API Reference](Docs/core/api-reference.md)
- [Quick Start Guide](Docs/getting-started/quickstart-basic.md)
- [Architecture Overview](Docs/overview/architecture.md)
- [FAQ](Docs/overview/faq.md)

---

## 🤝 Contributing

Contributions are welcome! Please check:

- [Contributing Guide](Docs/community/contributing.md)
- [Code of Conduct](Docs/community/code-of-conduct.md)

---

## ⚖️ License

MIT © Pippapips Limited

See the [LICENSE](../LICENSE) file for details.

---

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/Pippapips/ZenECS_deprecated/issues)
- **Email**: ck@pippapips.com
- **Website**: [pippapips.com](https://pippapips.com)

---

## 🧾 Acknowledgements

Built with love for data-driven games and tools. Feedback and PRs welcome!

---

**Made with ❤️ by Pippapips Limited**
