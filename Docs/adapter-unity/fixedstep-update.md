# FixedStep vs Update

> Docs / Adapter (Unity) / FixedStep vs Update

Understanding the difference between fixed-step simulation and variable-timestep updates in ZenECS.

## Overview

ZenECS uses a **3-phase frame structure**:

1. **BeginFrame** (Update): Variable-timestep input and sync
2. **FixedStep** (FixedUpdate): Deterministic simulation
3. **LateFrame** (LateUpdate): Variable-timestep presentation

## Frame Structure

### BeginFrame (Update)

Runs every frame with variable delta time:

- **FrameInput Systems**: Device input, window events
- **FrameSync Systems**: Camera, client prediction

**Use for:**
- Input sampling
- Camera movement
- UI updates
- Non-deterministic operations

### FixedStep (FixedUpdate)

Runs with fixed timestep (deterministic):

- **FixedInput Systems**: Player input sampling
- **FixedDecision Systems**: AI, pathfinding
- **FixedSimulation Systems**: Physics, gameplay
- **FixedPost Systems**: Cleanup, events

**Use for:**
- Gameplay logic
- Physics simulation
- Network synchronization
- Deterministic operations

### LateFrame (LateUpdate)

Runs every frame with variable delta time:

- **FrameView Systems**: Interpolation, transforms
- **FrameUI Systems**: UI, HUD updates
- **Binding Application**: View bindings applied

**Use for:**
- Visual presentation
- UI updates
- Interpolation
- View synchronization

## System Groups

### FixedGroup

For deterministic simulation:

```csharp
[FixedGroup]
public class MovementSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        // deltaTime is fixed (e.g., 1/60)
    }
}
```

### FrameGroup

For variable-timestep updates:

```csharp
[FrameGroup]
public class CameraSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        // deltaTime is variable (actual frame time)
    }
}
```

## When to Use Which

### Use FixedGroup For

- ✅ Physics simulation
- ✅ Gameplay logic
- ✅ AI systems
- ✅ Network synchronization
- ✅ Deterministic operations

### Use FrameGroup For

- ✅ Input handling
- ✅ Camera movement
- ✅ UI updates
- ✅ Visual effects
- ✅ Non-deterministic operations

## Examples

### FixedStep System

```csharp
[FixedGroup]
public class PhysicsSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        // deltaTime is always 1/60 (fixed)
        // Deterministic physics simulation
        foreach (var (entity, pos, vel) in world.Query<Position, Velocity>())
        {
            // Physics calculations
        }
    }
}
```

### Variable-Timestep System

```csharp
[FrameGroup]
public class CameraSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        // deltaTime is variable (actual frame time)
        // Smooth camera movement
        foreach (var (entity, pos) in world.Query<Position>())
        {
            // Camera interpolation
        }
    }
}
```

## Best Practices

### ✅ Do

- **Use FixedGroup for gameplay**: Deterministic simulation
- **Use FrameGroup for presentation**: Smooth visuals
- **Separate concerns**: Don't mix fixed and variable timestep
- **Test determinism**: Verify fixed-step systems are reproducible

### ❌ Don't

- **Don't mix timesteps**: Keep fixed and variable separate
- **Don't use variable time for gameplay**: Breaks determinism
- **Don't use fixed time for visuals**: Causes stuttering

## See Also

- [System Runner](../core/system-runner.md) - Execution details
- [Systems Guide](../core/systems.md) - System design
- [Architecture](../overview/architecture.md) - Frame structure
