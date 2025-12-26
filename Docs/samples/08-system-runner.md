# 08 - System Runner

> Docs / Samples / 08 - System Runner

Understand system execution order, groups, and the system runner pipeline.

## Prerequisites

- **.NET SDK 8.0+** or **Unity 2021.3+**
- **ZenECS Core** package installed
- Understanding of [System Runner](../core/system-runner.md)

## Run It

### .NET Standalone

```bash
cd Packages/com.zenecs.core/Samples~/08-SystemRunner
dotnet run
```

### Unity

1. Open Unity project
2. Open scene: `Packages/com.zenecs.core/Samples~/08-SystemRunner/Scene.unity`
3. Press Play

## Code Walkthrough

### System Groups

Systems run in specific groups:

```csharp
[FixedGroup]  // Deterministic simulation
public class PhysicsSystem : ISystem
{
    public void Run(IWorld world, float deltaTime) { }
}

[FrameGroup]  // Variable-timestep updates
public class CameraSystem : ISystem
{
    public void Run(IWorld world, float deltaTime) { }
}
```

### Execution Order

Control system execution order:

```csharp
[FixedGroup]
[OrderAfter(typeof(InputSystem))]
[OrderBefore(typeof(RenderSystem))]
public class GameplaySystem : ISystem
{
    public void Run(IWorld world, float deltaTime) { }
}
```

## Complete Example

```csharp
using ZenECS.Core;
using ZenECS.Core.Systems;

// Input system (runs first)
[FixedGroup]
public sealed class InputSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        // Process input
    }
}

// Gameplay system (runs after input)
[FixedGroup]
[OrderAfter(typeof(InputSystem))]
public sealed class GameplaySystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        // Game logic
    }
}

// Render system (runs last)
[FixedGroup]
[OrderAfter(typeof(GameplaySystem))]
public sealed class RenderSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        // Rendering
    }
}

// Register systems (order doesn't matter, attributes control execution)
var world = kernel.CreateWorld(null, "GameWorld");
world.AddSystems([
    new RenderSystem(),    // Registered first
    new InputSystem(),      // Registered second
    new GameplaySystem()    // Registered third
]);

// Execution order (controlled by attributes):
// 1. InputSystem
// 2. GameplaySystem
// 3. RenderSystem
```

## Execution Flow

### Frame Structure

```
BeginFrame (Update)
  ├─ FrameInput Systems
  └─ FrameSync Systems

FixedStep (FixedUpdate) × N
  ├─ FixedInput Systems
  ├─ FixedDecision Systems
  ├─ FixedSimulation Systems
  └─ FixedPost Systems

LateFrame (LateUpdate)
  ├─ FrameView Systems
  └─ FrameUI Systems
```

## See Also

- [System Runner](../core/system-runner.md) - Detailed documentation
- [Systems Guide](../core/systems.md) - System design
- [Architecture](../overview/architecture.md) - Execution model
