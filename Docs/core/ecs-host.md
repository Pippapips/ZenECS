# EcsHost

> Docs / Core / EcsHost

Hosting scenarios and integration points for ZenECS in different environments.

## Overview

ZenECS can be hosted in various environments:

- **Unity**: Via Unity Adapter
- **Standalone .NET**: Console applications, servers
- **Other Engines**: Godot, MonoGame, etc. (with custom adapter)
- **Testing**: Unit tests and integration tests

## Hosting Scenarios

### Unity Hosting

Use `EcsDriver` for automatic lifecycle:

```csharp
// EcsDriver handles kernel lifecycle
var kernel = KernelLocator.Current;
var world = kernel.CreateWorld(null, "GameWorld");
```

### Standalone .NET Hosting

Manual kernel management:

```csharp
var kernel = new Kernel();
var world = kernel.CreateWorld(null, "GameWorld");

// Game loop
while (running)
{
    kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame: 4);
}

kernel.Dispose();
```

### Server Hosting

For game servers:

```csharp
var kernel = new Kernel();
var serverWorld = kernel.CreateWorld(null, "Server", tags: new[] { "server" });

// Server loop
while (serverRunning)
{
    // Process network messages
    ProcessNetworkMessages(serverWorld);
    
    // Run simulation
    kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame: 4);
}
```

### Testing Hosting

For unit tests:

```csharp
[Test]
public void TestSystem()
{
    var kernel = new Kernel();
    var world = kernel.CreateWorld(null, "Test");
    
    // Test setup
    world.AddSystems([new TestSystem()]);
    
    // Execute
    kernel.PumpAndLateFrame(1f, 1f / 60f, maxSubStepsPerFrame: 4);
    
    // Assertions
    Assert.IsTrue(/* ... */);
    
    kernel.Dispose();
}
```

## Integration Points

### Frame Bridge

Bridge engine frame callbacks:

```csharp
// Unity
void Update() => kernel.BeginFrame(Time.deltaTime);
void FixedUpdate() => kernel.FixedStep(Time.fixedDeltaTime);
void LateUpdate() => kernel.LateFrame(Time.deltaTime, alpha);

// Godot
public override void _Process(float delta) => kernel.BeginFrame(delta);
public override void _PhysicsProcess(float delta) => kernel.FixedStep(delta);
```

### Lifecycle Management

Manage kernel lifecycle:

```csharp
// Initialize
var kernel = new Kernel();
var world = kernel.CreateWorld(null, "GameWorld");

// Run
kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame: 4);

// Cleanup
kernel.Dispose();
```

## See Also

- [Unity Adapter](../adapter-unity/overview.md) - Unity integration
- [EcsKernel](./ecs-kernel.md) - Kernel management
- [Architecture](../overview/architecture.md) - System design
