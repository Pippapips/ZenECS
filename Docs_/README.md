# Getting Started with ZenECS

Welcome to ZenECS! This guide will help you get started with ZenECS in just a few minutes.

## What is ZenECS?

ZenECS is a pure C# Entity-Component-System framework built for Clean Architecture and Reactive Programming. It helps you build maintainable, testable, and scalable game architectures.

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

## Next Steps

- **[Quick Start Tutorial](docs/getting-started/quickstart-basic.html)** — Build your first ECS system in 5 minutes
- **[Installation Guide](docs/getting-started/install-upm.html)** — Detailed installation instructions
- **[Core Concepts](docs/core/world.html)** — Learn about entities, components, and systems
- **[Unity Integration](docs/adapter-unity/overview.html)** — Unity-specific features and setup

## Learn More

- **[Core Documentation](../Packages/com.zenecs.core/README.md)** — Complete Core API reference
- **[Unity Adapter Documentation](../Packages/com.zenecs.adapter.unity/README.md)** — Unity integration guide
- **[Samples](docs/samples/01-basic.html)** — Working code examples

---

**Ready to dive deeper?** Start with the [Quick Start Tutorial](docs/getting-started/quickstart-basic.html) or explore the [Core Concepts](docs/core/world.html).
