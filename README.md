# ZenECS

[![ZenECS Core](https://img.shields.io/badge/ZenECS%20Core-1.1.0-orange.svg)](https://github.com/Pippapips/ZenECS/releases/tag/upm-core-1.1.0)
[![ZenECS Adapter Unity](https://img.shields.io/badge/ZenECS%20Adapter%20Unity-1.0.0-orange.svg)](https://github.com/Pippapips/ZenECS/releases/tag/upm-adapter-unity-1.0.0)
[![NuGet Package](https://img.shields.io/badge/NuGet-1.1.0-blue.svg?logo=nuget)](https://github.com/Pippapips/ZenECS/packages?package_type=nuget)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Standard](https://img.shields.io/badge/.NET-Standard%202.1-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black.svg)](https://unity.com/)

> **A pure C# Entity-Component-System framework built for Clean Architecture and Reactive Programming.**

ZenECS is a modern ECS runtime that helps you build maintainable, testable, and scalable game architectures. Whether you're building Unity games, standalone .NET applications, or server simulations, ZenECS provides the foundation for clean code that scales.

---

## 🎯 What is ZenECS?

ZenECS is an **Entity-Component-System (ECS)** framework that separates your game logic into three distinct layers:

- **Data Layer** (Components) — Pure data structures, no logic
- **Simulation Layer** (Systems) — Pure functions that transform component data
- **Presentation Layer** (Binders) — Reactive view updates that respond to data changes

Unlike traditional OOP architectures, ZenECS enforces **unidirectional data flow**: Views publish messages → Systems process and mutate state → Presentation reads (read-only). This eliminates circular dependencies and makes your codebase predictable and testable.

---

## ✨ Why ZenECS?

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

---

## 🚀 Quick Start

### Unity (Recommended)

1. **Install via Package Manager:**
   ```json
   {
     "dependencies": {
      "com.zenecs.core": "https://github.com/Pippapips/ZenECS.git?path=Packages/com.zenecs.core",
      "com.zenecs.adapter.unity": "https://github.com/Pippapips/ZenECS.git?path=Packages/com.zenecs.adapter.unity"
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

---

## 📦 Packages

This repository contains two packages:

### [ZenECS Core](Packages/com.zenecs.core/README.md)
The engine-agnostic ECS runtime. Works with Unity, Godot, or standalone .NET applications.

**Key Features:**
- Multi-world kernel with deterministic stepping
- Message bus for event-driven architecture
- ComponentDelta bindings for reactive views
- Command buffers for thread-safe batching
- Snapshot I/O with migrations
- Zero dependencies (.NET Standard 2.1)

**Samples:**
- Basic usage and movement system
- Message bus and pub/sub patterns
- Command buffer usage
- Snapshot I/O and migrations
- World reset patterns
- Write hooks and validators
- Component change bindings
- System runner and ordering

### [ZenECS Adapter Unity](Packages/com.zenecs.adapter.unity/README.md)
Optional Unity integration layer. Bridges ZenECS with Unity's lifecycle and editor tools.

**Key Features:**
- `EcsDriver` for automatic kernel lifecycle management
- `EntityLink` for GameObject ↔ Entity binding
- `EntityBlueprint` for ScriptableObject-based entity spawning
- Editor tools (ECS Explorer, custom inspectors)
- UniRx and Zenject integration
- Input-to-Intent pattern
- FixedStep vs Update separation

**Samples:**
- EcsDriver basic setup
- EntityLink (GameObject ↔ Entity)
- EntityBlueprint (entity spawning)
- System Presets
- Input → Intent pattern
- FixedStep vs Update comparison
- UniRx integration
- Zenject integration

---

## 📚 Documentation

**📖 [View Full Documentation →](https://pippapips.github.io/ZenECS)**

Complete documentation is available on GitHub Pages, including interactive API reference, tutorials, and guides.

- **[Getting Started Guide](Docs/getting-started/quickstart-basic.md)** — 5-minute quickstart tutorial
- **[Core Documentation](Packages/com.zenecs.core/README.md)** — Complete API reference and concepts
- **[Unity Adapter Documentation](Packages/com.zenecs.adapter.unity/README.md)** — Unity-specific integration guide
- **[Full Documentation Index](Docs/README.md)** — Comprehensive guides and tutorials
- **[API Reference](Docs/references/api-index.md)** — Auto-generated API documentation

---

## 🎓 Learn More

### Core Concepts

- **Entities** — Containers for components
- **Components** — Pure data structures (structs)
- **Systems** — Pure functions that transform component data
- **Messages** — Event-driven communication between layers
- **Binders** — Reactive view updates based on component changes

### Architecture Patterns

- **View → Message → System → State** — Unidirectional data flow
- **Multi-World** — Isolated simulation spaces
- **Command Buffers** — Batch structural changes
- **ComponentDelta** — Automatic change detection

---

## 🔗 Quick Links

**🌐 [Online Documentation](https://pippapips.github.io/ZenECS)** — Browse interactive docs with search and API reference

- **[Quick Start Guide](Docs/getting-started/quickstart-basic.md)** — Get started in 5 minutes
- **[Core README](Packages/com.zenecs.core/README.md)** — Detailed Core documentation
- **[Adapter Unity README](Packages/com.zenecs.adapter.unity/README.md)** — Unity integration guide
- **[Documentation Index](Docs/README.md)** — Full documentation
- **[Core Samples](Packages/com.zenecs.core/Samples~)** — Core example projects
- **[Unity Samples](Packages/com.zenecs.adapter.unity/Samples~)** — Unity integration examples
- **[FAQ](Docs/overview/faq.md)** — Frequently asked questions

---

## 📄 License

MIT © Pippapips Limited

---

## 🤝 Contributing

We welcome contributions! Please see:
- **[Contributing Guidelines](Docs/community/contributing.md)** — How to contribute
- **[Code of Conduct](Docs/community/code-of-conduct.md)** — Community standards
- **[Governance](Docs/community/governance.md)** — Project governance

## 📞 Support

- **[FAQ](Docs/overview/faq.md)** — Common questions and answers
- **[Support Guide](Docs/community/support.md)** — How to get help
- **[GitHub Issues](https://github.com/Pippapips/ZenECS/issues)** — Report bugs or request features

---

**Ready to build better architectures?** 

📖 **[Browse the full documentation online](https://pippapips.github.io/ZenECS)** or start with the [Quick Start Guide](Docs/getting-started/quickstart-basic.md). Explore the [Core README](Packages/com.zenecs.core/README.md) and [Unity Adapter README](Packages/com.zenecs.adapter.unity/README.md) for detailed guides.
