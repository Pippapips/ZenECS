# System Runner

> Docs / Core / System Runner

The **System Runner** orchestrates system execution, managing groups, order, and frame phases.

## Overview

The System Runner:

- **Executes Systems**: Runs systems in correct order
- **Manages Groups**: Organizes systems by execution phase
- **Controls Order**: Ensures systems run in correct sequence
- **Handles Lifecycle**: Initialize and shutdown systems

### Key Concepts

- **System Groups**: Execution phases (FixedGroup, FrameGroup)
- **Execution Order**: Control when systems run
- **Frame Phases**: BeginFrame, FixedStep, LateFrame
- **System Plan**: Execution plan for systems

## How It Works

### Execution Flow

```
BeginFrame
  ├─ FrameInput Systems
  └─ FrameSync Systems

FixedStep × N
  ├─ FixedInput Systems
  ├─ FixedDecision Systems
  ├─ FixedSimulation Systems
  └─ FixedPost Systems

LateFrame
  ├─ FrameView Systems
  └─ FrameUI Systems
```

### System Groups

Systems are organized into groups:

- **FixedGroup**: Deterministic simulation
- **FrameGroup**: Variable-timestep updates

### Execution Order

Control order with attributes:

```csharp
[FixedGroup]
[OrderAfter(typeof(InputSystem))]
[OrderBefore(typeof(RenderSystem))]
public class GameplaySystem : ISystem { }
```

## API Surface

### System Registration

```csharp
// Add systems
world.AddSystems([new MovementSystem(), new HealthSystem()]);

// Remove system
world.RemoveSystem<MovementSystem>();

// Get system
var system = world.GetSystem<MovementSystem>();
```

### System Attributes

```csharp
// Fixed-step simulation
[FixedGroup]
public class PhysicsSystem : ISystem { }

// Variable-timestep updates
[FrameGroup]
public class CameraSystem : ISystem { }

// Execution order
[OrderAfter(typeof(InputSystem))]
[OrderBefore(typeof(RenderSystem))]
public class GameplaySystem : ISystem { }
```

## Examples

### Basic System Execution

```csharp
var world = kernel.CreateWorld(null, "GameWorld");

// Register systems
world.AddSystems([
    new InputSystem(),
    new GameplaySystem(),
    new RenderSystem()
]);

// Systems run automatically during frame updates
kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame: 4);
```

### Ordered Systems

```csharp
[FixedGroup]
public class InputSystem : ISystem
{
    public void Run(IWorld world, float deltaTime) { }
}

[FixedGroup]
[OrderAfter(typeof(InputSystem))]
public class GameplaySystem : ISystem
{
    public void Run(IWorld world, float deltaTime) { }
}

[FixedGroup]
[OrderAfter(typeof(GameplaySystem))]
public class RenderSystem : ISystem
{
    public void Run(IWorld world, float deltaTime) { }
}

// Execution order:
// 1. InputSystem
// 2. GameplaySystem
// 3. RenderSystem
```

## Best Practices

### ✅ Do

- **Use appropriate groups**: FixedGroup for simulation, FrameGroup for presentation
- **Control order**: Use OrderAfter/OrderBefore when needed
- **Keep systems focused**: One responsibility per system
- **Test order**: Verify systems run in correct sequence

### ❌ Don't

- **Don't mix groups**: Keep fixed and frame separate
- **Don't create circular dependencies**: Avoid order conflicts
- **Don't over-order**: Only order when necessary

## See Also

- [Systems](./systems.md) - System design
- [World](./world.md) - World management
- [Architecture](../overview/architecture.md) - Execution model
