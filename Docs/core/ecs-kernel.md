# EcsKernel

> Docs / Core / EcsKernel

The **Kernel** is the top-level manager that manages multiple worlds and coordinates the game loop.

## Overview

The Kernel provides:

- **Multi-World Management**: Create, destroy, and manage multiple worlds
- **Frame Coordination**: Coordinate `BeginFrame → FixedStep×N → LateFrame` structure
- **World Indexing**: Find worlds by ID, name, or tags
- **Time Tracking**: Frame counters and simulation time

### Key Concepts

- **World Management**: Create and destroy worlds
- **Frame Structure**: 3-phase frame execution
- **Current World**: Active world selection
- **Time Accumulator**: Fixed-step simulation

## How It Works

### Kernel Creation

```csharp
var kernel = new Kernel();
// Or with options
var options = new KernelOptions { /* ... */ };
var kernel = new Kernel(options);
```

### World Management

```csharp
// Create world
var world = kernel.CreateWorld(null, "GameWorld");

// Find world
var world = kernel.FindWorld("GameWorld");
var world = kernel.FindWorldById(worldId);
var worlds = kernel.FindWorldsByTag("gameplay");

// Set current world
kernel.SetCurrentWorld(world);

// Destroy world
kernel.DestroyWorld(worldId);
```

### Frame Execution

```csharp
// Manual frame control
kernel.BeginFrame(deltaTime);
kernel.FixedStep(fixedDeltaTime);
kernel.LateFrame(deltaTime, alpha);

// Or automatic (recommended)
kernel.PumpAndLateFrame(deltaTime, fixedDeltaTime, maxSubStepsPerFrame: 4);
```

## API Surface

### World Management

```csharp
// Create world
IWorld CreateWorld(string? name = null, IEnumerable<string>? tags = null, bool setAsCurrent = false);

// Find worlds
IWorld? FindWorld(string name);
IWorld? FindWorldById(WorldId id);
IEnumerable<IWorld> FindWorldsByTag(string tag);

// Current world
IWorld? CurrentWorld { get; }
void SetCurrentWorld(IWorld world);

// Destroy world
bool DestroyWorld(WorldId id);
```

### Frame Execution

```csharp
// Frame phases
void BeginFrame(float deltaTime);
void FixedStep(float fixedDeltaTime);
void LateFrame(float deltaTime, float alpha);

// Automatic (recommended)
void PumpAndLateFrame(float deltaTime, float fixedDeltaTime, int maxSubStepsPerFrame = 4);
```

### Time Tracking

```csharp
long FrameCount { get; }
long FixedFrameCount { get; }
float SimulationTimeSeconds { get; }
```

## Examples

### Basic Usage

```csharp
using ZenECS.Core;

// Create kernel
var kernel = new Kernel();

// Create world
var world = kernel.CreateWorld(null, "GameWorld", setAsCurrent: true);

// Register systems
world.AddSystems([new MovementSystem()]);

// Game loop
const float fixedDelta = 1f / 60f;
while (running)
{
    float dt = GetDeltaTime();
    kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame: 4);
}

// Cleanup
kernel.Dispose();
```

### Multi-World Setup

```csharp
var kernel = new Kernel();

// Create multiple worlds
var gameWorld = kernel.CreateWorld(null, "Game");
var uiWorld = kernel.CreateWorld(null, "UI");
var serverWorld = kernel.CreateWorld(null, "Server", tags: new[] { "server" });

// Step all worlds
kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame: 4);
```

## Best Practices

### ✅ Do

- **Use PumpAndLateFrame**: Automatic frame management
- **Set current world**: For convenience access
- **Use tags**: Organize worlds by purpose
- **Dispose properly**: Clean up resources

### ❌ Don't

- **Don't create too many worlds**: Keep it reasonable
- **Don't mix worlds**: Keep worlds isolated
- **Don't forget disposal**: Always dispose kernel

## See Also

- [World](./world.md) - World management
- [System Runner](./system-runner.md) - System execution
- [Architecture](../overview/architecture.md) - System design
