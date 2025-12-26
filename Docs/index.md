---
_layout: landing
---

# Welcome to ZenECS

> **A pure C# Entity-Component-System framework built for Clean Architecture and Reactive Programming.**

ZenECS is a modern ECS runtime that helps you build maintainable, testable, and scalable game architectures. Whether you're building Unity games, standalone .NET applications, or server simulations, ZenECS provides the foundation for clean code that scales.

## What is ZenECS?

ZenECS is an **Entity-Component-System (ECS)** framework that separates your game logic into three distinct layers:

- **Data Layer** (Components) — Pure data structures, no logic
- **Simulation Layer** (Systems) — Pure functions that transform component data
- **Presentation Layer** (Binders) — Reactive view updates that respond to data changes

Unlike traditional OOP architectures, ZenECS enforces **unidirectional data flow**: Views publish messages → Systems process and mutate state → Presentation reads (read-only). This eliminates circular dependencies and makes your codebase predictable and testable.

## Quick Start

### Unity (Recommended)

1. **Install via Package Manager:**
   ```json
   {
     "dependencies": {
      "com.zenecs.core": "https://github.com/Pippapips/ZenECS.git?path=Packages/com.zenecs.core#v1.0.0",
      "com.zenecs.adapter.unity": "https://github.com/Pippapips/ZenECS.git?path=Packages/com.zenecs.adapter.unity#v1.0.0"
     }
   }
   ```

2. **Add EcsDriver to your scene:**
   ```csharp
   using ZenECS.Adapter.Unity;
   using ZenECS.Core;
   
   // Kernel is automatically created
   var kernel = KernelLocator.Current;
   var world = kernel.CreateWorld(null, "GameWorld", setAsCurrent: true);
   ```

3. **Create a system:**
   ```csharp
   [FixedGroup]
   public sealed class MoveSystem : ISystem
   {
       public void Run(IWorld w, float dt)
       {
           using var cmd = w.BeginWrite();
           foreach (var (e, pos, vel) in w.Query<Position, Velocity>())
           {
               cmd.ReplaceComponent(e, new Position(
                   pos.X + vel.X * dt,
                   pos.Y + vel.Y * dt
               ));
           }
       }
   }
   ```

4. **Register and run:**
   ```csharp
   world.AddSystems([new MoveSystem()]);
   // EcsDriver automatically calls kernel.BeginFrame/FixedStep/LateFrame
   ```

### .NET (Standalone)

```bash
dotnet add package ZenECS.Core --version 1.0.0
```

```csharp
using ZenECS.Core;

// Create kernel and world
var kernel = new Kernel();
var world = kernel.CreateWorld(null, "Game");

// Register systems
world.AddSystems([new MoveSystem()]);

// Game loop
while (running)
{
    float dt = GetDeltaTime();
    kernel.PumpAndLateFrame(dt, fixedDelta: 1f/60f, maxSubStepsPerFrame: 4);
}
```

## Why ZenECS?

### 🏗️ Clean Architecture by Design

**Stop fighting your codebase.** ZenECS enforces clear boundaries between layers:

- ✅ **View never writes directly** — All mutations happen through messages and systems
- ✅ **Systems are pure functions** — Easy to test, reason about, and debug
- ✅ **Multi-world isolation** — Perfect for split-screen, networking, or modular game modes
- ✅ **Zero dependencies** — Pure .NET Standard 2.1, works with Unity, Godot, or standalone .NET
- ✅ **Testable by default** — Minimal public API, sealed scopes, clear lifecycle

### ⚡ Reactive Programming Built-In

**Build event-driven architectures** without the complexity:

- ✅ **Message Bus** — Struct-based pub/sub with deterministic delivery
- ✅ **ComponentDelta Bindings** — Automatic change detection and reactive view updates
- ✅ **Unidirectional Flow** — View → Message → System → State → Presentation
- ✅ **UniRx Integration** — Optional bridge to `IObservable<T>` for reactive composition

### 🎯 Production Ready

**Built for real projects:**

- ✅ **Deterministic Stepping** — Reproducible simulations for networking and replays
- ✅ **Command Buffers** — Thread-safe batching of structural changes
- ✅ **Snapshot I/O** — Save/load with version migrations
- ✅ **Thread Safety** — Concurrent world indexing built-in

## Next Steps

Ready to dive deeper? Follow these steps:

1. **[Installation Guide](getting-started/install-upm.md)** — Add ZenECS to your project
2. **[Quick Start Tutorial](getting-started/quickstart-basic.md)** — Build your first ECS system in 5 minutes
3. **[Explore Samples](samples/01-basic.md)** — Learn from working examples
4. **[Read Architecture Guide](overview/architecture.md)** — Understand the design

### Learning Path

**Beginner:**
- Start with [Quick Start](getting-started/quickstart-basic.md)
- Read [What is ECS?](overview/what-is-ecs.md)
- Try [Basic Sample](samples/01-basic.md)

**Intermediate:**
- Study [Core Concepts](core/world.md)
- Learn [Systems](core/systems.md)
- Explore [Message Bus](core/message-bus.md)

**Advanced:**
- Master [Binding System](core/binding.md)
- Understand [Architecture](overview/architecture.md)
- Read [Advanced Topics](guides/advanced-topics.md)

## Documentation Structure

- **[Getting Started](getting-started/quickstart-basic.html)** — Installation, setup, and quick start guides
- **[Core Concepts](core/world.html)** — Entities, components, systems, worlds, and messaging
- **[Unity Adapter](adapter-unity/overview.html)** — Unity integration guides and tutorials
- **[Guides](guides/advanced-topics.html)** — Advanced topics and best practices
- **[Samples](samples/01-basic.html)** — Code examples and tutorials
- **[Overview](overview/zenecs-at-a-glance.html)** — Architecture, design philosophy, and FAQ

## Packages

This repository contains two packages:

- **[ZenECS Core](../Packages/com.zenecs.core/README.md)** — The engine-agnostic ECS runtime
- **[ZenECS Adapter Unity](../Packages/com.zenecs.adapter.unity/README.md)** — Optional Unity integration layer

---

**Ready to build better architectures?** Start with the [Quick Start Guide](getting-started/quickstart-basic.md) or explore the [Core README](../Packages/com.zenecs.core/README.md) and [Unity Adapter README](../Packages/com.zenecs.adapter.unity/README.md) for detailed guides.

---

MIT © Pippapips Limited
