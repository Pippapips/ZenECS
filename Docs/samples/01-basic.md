# 01 - Basic

> Docs / Samples / 01 - Basic

Minimal console sample demonstrating world creation, entity management, and system execution.

## Prerequisites

- **.NET SDK 8.0+** or **Unity 2021.3+**
- **ZenECS Core** package installed
- Basic C# knowledge

## Run It

### .NET Standalone

```bash
cd Packages/com.zenecs.core/Samples~/01-Basic
dotnet run
```

### Unity

1. Open Unity project with ZenECS installed
2. Open scene: `Packages/com.zenecs.core/Samples~/01-Basic/Scene.unity`
3. Press Play

## Code Walkthrough

### Step 1: Define Components

```csharp
public struct Position
{
    public float X, Y;
    
    public Position(float x, float y)
    {
        X = x;
        Y = y;
    }
}

public struct Velocity
{
    public float X, Y;
    
    public Velocity(float x, float y)
    {
        X = x;
        Y = y;
    }
}
```

**Key Points:**
- Components are `struct` (value types)
- No methods, just data
- Immutable by convention

### Step 2: Create System

```csharp
using ZenECS.Core;
using ZenECS.Core.Systems;

[FixedGroup]
public sealed class MovementSystem : ISystem
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

**Key Points:**
- Implements `ISystem` interface
- Uses `[FixedGroup]` for deterministic simulation
- Queries entities with `Position` and `Velocity`
- Uses command buffer for writes

### Step 3: Setup World

```csharp
using ZenECS.Core;

// Create kernel
var kernel = new Kernel();

// Create world
var world = kernel.CreateWorld(null, "GameWorld");

// Register system
world.AddSystems([new MovementSystem()]);
```

### Step 4: Create Entities

```csharp
using (var cmd = world.BeginWrite())
{
    // Create player entity
    var player = cmd.CreateEntity();
    cmd.AddComponent(player, new Position(0, 0));
    cmd.AddComponent(player, new Velocity(1, 0));
    
    // Create enemy entity
    var enemy = cmd.CreateEntity();
    cmd.AddComponent(enemy, new Position(10, 0));
    cmd.AddComponent(enemy, new Velocity(-1, 0));
}
```

### Step 5: Run Simulation

```csharp
const float fixedDelta = 1f / 60f;  // 60 FPS
bool running = true;

while (running)
{
    float dt = GetDeltaTime();  // Your time source
    
    // Run one frame
    kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame: 4);
    
    // Check exit condition
    if (ShouldExit())
        running = false;
}

// Cleanup
kernel.Dispose();
```

## Complete Example

```csharp
using System;
using ZenECS.Core;
using ZenECS.Core.Systems;

// Components
public struct Position { public float X, Y; }
public struct Velocity { public float X, Y; }

// System
[FixedGroup]
public sealed class MovementSystem : ISystem
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

// Main
class Program
{
    static void Main()
    {
        // Setup
        var kernel = new Kernel();
        var world = kernel.CreateWorld(null, "GameWorld");
        world.AddSystems([new MovementSystem()]);
        
        // Create entities
        using (var cmd = world.BeginWrite())
        {
            var player = cmd.CreateEntity();
            cmd.AddComponent(player, new Position(0, 0));
            cmd.AddComponent(player, new Velocity(1, 0));
        }
        
        // Game loop
        const float fixedDelta = 1f / 60f;
        for (int i = 0; i < 60; i++)  // Run 1 second
        {
            kernel.PumpAndLateFrame(fixedDelta, fixedDelta, maxSubStepsPerFrame: 4);
            
            // Print position
            var pos = world.Get<Position>(player);
            Console.WriteLine($"Frame {i}: Position = ({pos.X:F2}, {pos.Y:F2})");
        }
        
        // Cleanup
        kernel.Dispose();
    }
}
```

## Expected Output

```
Frame 0: Position = (0.02, 0.00)
Frame 1: Position = (0.03, 0.00)
Frame 2: Position = (0.05, 0.00)
...
Frame 59: Position = (0.98, 0.00)
```

## What to Try Next

### Experiment 1: Add More Components

Add a `Health` component:

```csharp
public struct Health
{
    public float Current;
    public float Max;
}

// Add to entity
cmd.AddComponent(player, new Health(100, 100));
```

### Experiment 2: Add Another System

Create a `HealthSystem`:

```csharp
[FixedGroup]
public sealed class HealthSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        // Process health components
    }
}
```

### Experiment 3: Use Messages

Publish damage messages:

```csharp
world.Publish(new DamageMessage { Target = player, Amount = 10 });
```

## Next Samples

- **[02 - Binding](../samples/02-binding.md)** - Connect to Unity GameObjects
- **[03 - Messages](../samples/03-messages.md)** - Event-driven architecture
- **[04 - Command Buffer](../samples/04-command-buffer.md)** - Structural changes

## See Also

- [Quick Start Guide](../getting-started/quickstart-basic.md) - Getting started
- [World Guide](../core/world.md) - World management
- [Systems Guide](../core/systems.md) - System design
