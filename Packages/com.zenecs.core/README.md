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

### Basic Usage

```csharp
using ZenECS.Core;

// 1. Create kernel and world
var kernel = new Kernel();
var world = kernel.CreateWorld(name: "Game");

// 2. Define components
public struct Position { public float X, Y; }
public struct Velocity { public float X, Y; }

// 3. Create entity and add components
var entity = world.CreateEntity();
world.AddComponent(entity, new Position { X = 0, Y = 0 });
world.AddComponent(entity, new Velocity { X = 1, Y = 0 });

// 4. Game loop
kernel.BeginFrame(dt: 1f / 60f);
kernel.FixedStep(fixedDelta: 1f / 60f);
kernel.LateFrame(alpha: 1f);

// 5. Cleanup
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
            
            pos.X += vel.X * fixedDelta;
            pos.Y += vel.Y * fixedDelta;
        }
    }
}
```

---

## 🧭 Core Concepts

### Kernel

Manages multiple worlds and orchestrates frame ticks.

```csharp
var kernel = new Kernel(new KernelOptions
{
    AutoSelectNewWorld = true,
    StepOnlyCurrentWhenSelected = false
});

var world1 = kernel.CreateWorld("World1");
var world2 = kernel.CreateWorld("World2");

kernel.SetCurrentWorld(world1);

// Step all worlds or only current world
kernel.BeginFrame(dt);
kernel.FixedStep(fixedDelta);
kernel.LateFrame(alpha);
```

**Key Features:**
- World creation/destruction and lookup by ID/name/tag
- Current world selection and change events
- Pause/resume support
- Thread-safe world indexing

### World

Unified public API integrating all ECS functionality:

- **Entities** — Entity creation/destruction, lifecycle management
- **Components** — Component add/remove/query, type-segregated pooling
- **Query** — Fast entity queries with filtering support
- **CommandBuffer** — Buffer structural changes and apply at safe boundaries
- **Messages** — Struct-based Pub/Sub messaging
- **Contexts & Binders** — Binding system for view integration
- **Hooks** — Write permissions and validation hooks
- **Snapshot** — Save/load and migrations
- **Reset** — Fast world reset

### Systems

Systems encapsulate game logic:

- `IFrameSetupSystem` — One-time initialization per frame
- `IFixedSetupSystem` — One-time initialization per fixed step
- `IVariableRunSystem` — Variable timestep (BeginFrame)
- `IFixedRunSystem` — Fixed timestep (SimulationGroup)
- `IPresentationSystem` — Presentation phase (LateFrame)

**Ordering:**

```csharp
[SimulationGroup]
[OrderBefore(typeof(RenderSystem))]
[OrderAfter(typeof(PhysicsSystem))]
public sealed class MoveSystem : IFixedRunSystem
{
    // ...
}
```

### Components & Queries

Components are `struct` value types stored in type-segregated pools.

```csharp
// Create query
var filter = Filter.New
    .With<Position>()
    .With<Velocity>()
    .Without<Paused>()
    .Build();

// Iterate entities
foreach (var e in world.Query<Position, Velocity>(filter))
{
    ref var pos = ref world.Ref<Position>(e);  // Modify by reference
    var vel = world.Get<Velocity>(e);          // Read by value
    
    pos.X += vel.X * dt;
}
```

### Command Buffer

Buffer structural changes and apply at safe boundaries:

```csharp
using (var cmd = world.BeginWrite(CommandBufferApplyMode.Scheduled))
{
    cmd.AddComponent(entity, new Health { Value = 100 });
    cmd.RemoveComponent<Stunned>(entity);
    cmd.DestroyEntity(entity);
} // Scheduled for execution at worker barrier
```

### Messaging

Struct-based Pub/Sub messaging:

```csharp
public struct Damage : IMessage
{
    public Entity Target;
    public int Amount;
}

// Subscribe
var subscription = world.Subscribe<Damage>(damage =>
{
    // Handle damage
    var health = world.Get<Health>(damage.Target);
    health.Value -= damage.Amount;
    world.SetComponent(damage.Target, health);
});

// Publish
world.Publish(new Damage { Target = entity, Amount = 10 });

// Unsubscribe
subscription.Dispose();
```

### Hooks & Validators

World-scoped permission and validation hooks:

```csharp
// Write permission check
world.Hooks.AddWritePermission((entity, componentType) =>
    componentType != typeof(GodMode));

// Component value validation
world.Hooks.AddValidator<Health>(health => health.Value >= 0);
```

### Binding

Contexts and binders for view integration:

```csharp
public sealed class SpriteBinder : BaseBinder, IBinds<Position>, IRequireContext<SpriteContext>
{
    public void OnDelta(in ComponentDelta<Position> delta)
    {
        // Update sprite position
        var sprite = GetContext<SpriteContext>().Sprite;
        sprite.transform.position = new Vector3(delta.NewValue.X, delta.NewValue.Y, 0);
    }
}
```

### Snapshot I/O

Save/load with pluggable backend and formatters:

```csharp
// Save
using (var stream = File.Create("save.dat"))
{
    world.Save(stream, new BinaryComponentFormatter());
}

// Load
using (var stream = File.OpenRead("save.dat"))
{
    var migrations = new List<IPostLoadMigration> { new V1ToV2Migration() };
    world.Load(stream, new BinaryComponentFormatter(), migrations);
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
