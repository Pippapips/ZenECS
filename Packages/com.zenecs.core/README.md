# ZenECS Core

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Standard](https://img.shields.io/badge/.NET-Standard%202.1-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black.svg)](https://unity.com/)

**ZenECS Core** is a pure C# **Entity-Component-System (ECS)** runtime focused on clarity, determinism, and zero dependencies. It operates independently of Unity and is suitable for both game engines and standalone .NET applications.

## ✨ Key Features

### 🏗️ Clean Architecture
- **Layered Separation** — Strict boundaries between data (components), simulation (systems), and presentation (binders). View layer never writes directly; messages flow unidirectionally from view → simulation → presentation
- **Multi-World Kernel** — Isolated simulation spaces with independent lifecycles. Perfect for split-screen, server/client separation, or modular game modes
- **Zero Dependencies** — Pure .NET Standard 2.1 with no external frameworks. Engine-agnostic design enables Unity, Godot, or standalone .NET applications
- **Testable by Design** — Minimal public API surface, sealed scopes, and clear lifecycle boundaries. Systems are pure functions over component data
- **Dependency Injection** — Lightweight internal DI container with per-world scopes. Extensible bootstrap allows custom service composition

### ⚡ Reactive Programming
- **Message Bus** — Struct-based pub/sub messaging with deterministic delivery. Enables event-driven architecture where systems react to messages rather than polling
- **ComponentDelta Bindings** — Automatic change detection and reactive view updates. Binders receive deltas (Added/Changed/Removed) and update views reactively
- **Unidirectional Data Flow** — View publishes messages → Systems consume and mutate state → Presentation reads (read-only). No circular dependencies or tight coupling
- **UniRx Integration** — Optional bridge to convert message streams to `IObservable<T>` for reactive composition with LINQ operators

### 🎯 Production Ready
- **Deterministic Stepping** — Explicit `BeginFrame → FixedStep×N → LateFrame` structure with topological system ordering. Reproducible simulations for networking and replays
- **Command Buffers** — Batch structural changes (entity spawn/despawn, component add/remove) and apply at safe boundaries. Thread-safe and deterministic
- **Snapshot I/O** — Pluggable serialization backends with post-load migrations. Save/load world state with version compatibility
- **Thread Safety** — Concurrent world indexing with lock-based snapshots. Safe multi-threaded access patterns built-in

> **Status:** Release Candidate — 1.0 APIs locked, documentation/samples being finalized

---

## Table of Contents

- [Installation](#-installation)
  - [Unity (UPM)](#unity-upm)
  - [.NET (non-Unity)](#net-non-unity)
- [Quick Start](#-quick-start)
- [Namespaces](#-namespaces)
- [Core Concepts](#-core-concepts)
  - [Kernel](#kernel)
  - [KernelOptions](#kerneloptions)
  - [World](#world)
  - [Entity](#entity)
  - [WorldId](#worldid)
  - [WorldHandle](#worldhandle)
  - [Component](#component)
  - [IWorldSingletonComponent](#iworldsingletoncomponent)
  - [Systems](#systems)
  - [Query](#query)
  - [Command Buffer](#command-buffer)
  - [Message Bus](#message-bus)
  - [Binding](#binding)
  - [Hooks & Validators](#hooks--validators)
  - [Snapshot I/O](#snapshot-io)
- [Configuration](#️-configuration)
- [Attributes](#️-attributes)
- [Events](#-events)
- [Samples](#-samples)
- [Architecture](#️-architecture)
- [Extensibility Points](#-extensibility-points)
- [ZenECS vs Other ECS Frameworks](#-zenecs-vs-other-ecs-frameworks)
- [API Index](#-api-index)
- [Versioning & Compatibility](#️-versioning--compatibility)

---

## 📦 Installation

### Unity (UPM)

#### Install via Git URL

1. Open **Package Manager** → **Add package from git URL…**
2. Enter the following URL:
   ```
   https://github.com/Pippapips/ZenECS.git?path=Packages/com.zenecs.core
   ```

#### Local Development

Place the repository under your project and reference via `file:` URL or add an entry in `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.zenecs.core": "file:../../ZenECS/Packages/com.zenecs.core"
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

### Complete Example

For a full example, see `Packages/com.zenecs.core/Samples~/01-Basic/Basic.cs`.

```csharp
using ZenECS.Core;
using ZenECS.Core.Systems;

// 1. Create kernel and world
var kernel = new Kernel();
var world = kernel.CreateWorld(null, "Game");
kernel.SetCurrentWorld(world);

// 2. Define components
public readonly struct Position 
{ 
    public readonly float X, Y; 
    public Position(float x, float y) { X = x; Y = y; }
}

public readonly struct Velocity 
{ 
    public readonly float X, Y; 
    public Velocity(float x, float y) { X = x; Y = y; }
}

// 3. Write system
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

// 4. Register systems
world.AddSystems([new MoveSystem()]);

// 5. Create entities and add components
using (var cmd = world.BeginWrite())
{
    var entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position(0, 0));
    cmd.AddComponent(entity, new Velocity(1, 0));
}

// 6. Game loop
const float fixedDelta = 1f / 60f;
while (running)
{
    float dt = GetDeltaTime();
    kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame: 4);
}

// 7. Cleanup
kernel.Dispose();
```

### Writing Systems

```csharp
using ZenECS.Core;
using ZenECS.Core.Systems;

[FixedGroup]
[OrderAfter(typeof(PhysicsSystem))]
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

---

## 📚 Namespaces

The package is organized into the following namespaces:

- `ZenECS.Core` — Core types, kernel, world API, primitives (Entity, WorldId), attributes
- `ZenECS.Core.Systems` — System interfaces, attributes, utilities
- `ZenECS.Core.Messaging` — Message bus contracts
- `ZenECS.Core.Serialization` — Snapshot I/O, formatters, migrations
- `ZenECS.Core.Binding` — Context and binder contracts for view integration
- `ZenECS.Core.Config` — Configuration interfaces and options
- `ZenECS.Core.Internal` — Internal implementation details (not part of public API)

---

## 🧭 Core Concepts

This section provides a comprehensive guide to the core concepts of ZenECS Core.

### Kernel

The **Kernel** is the top-level manager that manages multiple worlds and orchestrates frame ticks.

**Interface:** `ZenECS.Core.IKernel`  
**Implementation:** `ZenECS.Core.Kernel`

**Key Responsibilities:**
- Create/destroy multiple worlds and lookup by ID/name/tag
- Current world selection and change events
- Pause/resume support
- Thread-safe world indexing
- Deterministic frame stepping

**Key Properties:**
- `bool IsRunning` — Whether the kernel is currently running
- `bool IsPaused` — Whether the kernel is paused
- `IWorld? CurrentWorld` — Currently selected world
- `float SimulationAccumulatorSeconds` — Unconsumed delta time for fixed stepping
- `long FrameCount` — Total number of frames processed
- `long FixedFrameCount` — Total number of fixed steps processed
- `double TotalSimulatedSeconds` — Accumulated simulated time

**Key Methods:**
```csharp
// World management
IWorld CreateWorld(WorldConfig? cfg = null, string? name = null, 
                   IEnumerable<string>? tags = null, WorldId? presetId = null, 
                   bool setAsCurrent = false);
void DestroyWorld(IWorld world);
bool TryGet(WorldId id, out IWorld? world);
IEnumerable<IWorld> FindByName(string name);
IEnumerable<IWorld> FindByTag(string tag);
IEnumerable<IWorld> FindByAnyTag(params string[] tags);

// Current world management
void SetCurrentWorld(IWorld world);
void ClearCurrentWorld();

// Frame stepping
void BeginFrame(float dt);
void FixedStep(float fixedDelta);
void LateFrame(float alpha = 1.0f);
int PumpAndLateFrame(float dt, float fixedDelta, int maxSubSteps);

// Control
void Pause();
void Resume();
void TogglePause();
```

**Events:**
- `event Action<IWorld>? WorldCreated`
- `event Action<IWorld>? WorldDestroyed`
- `event Action<IWorld?, IWorld?>? CurrentWorldChanged`
- `event Action? Disposed`

**Basic Usage:**

```csharp
using ZenECS.Core;

// Create kernel
var kernel = new Kernel(new KernelOptions
{
    AutoSelectNewWorld = true,
    StepOnlyCurrentWhenSelected = false
});

// Create worlds
var world1 = kernel.CreateWorld(null, "World1");
var world2 = kernel.CreateWorld(null, "World2");

// Set current world
kernel.SetCurrentWorld(world1);

// Game loop
kernel.BeginFrame(dt: 1f / 60f);
kernel.FixedStep(fixedDelta: 1f / 60f);
kernel.LateFrame(alpha: 1f);

// Or use convenience method
kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame);

// Cleanup
kernel.Dispose();
```

**Frame Structure:**

```
BeginFrame (variable timestep)
  ↓
FixedStep × N (fixed timestep, simulation)
  ↓
LateFrame (presentation, read-only)
```

### KernelOptions

Configuration options for the kernel.

**Type:** `ZenECS.Core.KernelOptions`

**Properties:**
- `bool AutoSelectNewWorld` — Automatically select newly created worlds as current
- `bool StepOnlyCurrentWhenSelected` — Only step the current world when one is selected
- `Func<WorldId> NewWorldId` — Factory function for generating new world IDs
- `string AutoNamePrefix` — Prefix for auto-generated world names

### World

**World** is the unified public API that integrates all ECS functionality. It represents a single simulation space.

**Interface:** `ZenECS.Core.IWorld`

**Key Properties:**
- `IKernel Kernel` — The kernel that owns this world
- `WorldId Id` — Stable identity of this world
- `string Name` — Human-readable name
- `IReadOnlyCollection<string> Tags` — Tags for discovery and grouping
- `long FrameCount` — Number of frames processed
- `long Tick` — World-local simulation tick counter
- `bool IsPaused` — Whether this world is paused
- `bool IsDisposing` — Whether this world is disposing

**Key Features:**
- **Entities**: Entity creation/destruction, lifecycle management
- **Components**: Component add/remove/query, type-segregated pooling
- **Query**: Fast entity queries with filtering support
- **CommandBuffer**: Buffer structural changes and apply at safe boundaries
- **Messages**: Struct-based Pub/Sub messaging
- **Contexts & Binders**: Binding system for view integration
- **Hooks**: Write permissions and validation hooks
- **Snapshot**: Save/load and migrations
- **Reset**: Fast world reset

**Basic Usage:**

```csharp
// Create world (via kernel)
var world = kernel.CreateWorld(null, "GameWorld");

// Create entity and add components using command buffer
using (var cmd = world.BeginWrite())
{
    var entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position { X = 0, Y = 0 });
    cmd.AddComponent(entity, new Velocity { X = 1, Y = 0 });
}

// Read components
var pos = world.ReadComponent<Position>(entity);  // Read by value
var vel = world.ReadComponent<Velocity>(entity);   // Read by value

// Check component
bool hasPos = world.HasComponent<Position>(entity);

// Destroy entity using command buffer
using (var cmd = world.BeginWrite())
{
    cmd.DestroyEntity(entity);
}
```

**Aggregated APIs:**

`IWorld` aggregates the following API surfaces:
- `IWorldQueryApi` — Entity queries
- `IWorldQuerySpanApi` — Query with span-based iteration
- `IWorldEntityApi` — Entity creation/destruction
- `IWorldComponentApi` — Component CRUD operations
- `IWorldContextApi` — Context management for binders
- `IWorldBinderApi` — Binder registration and management
- `IWorldSnapshotApi` — Snapshot save/load
- `IWorldMessagesApi` — Message bus operations
- `IWorldHookApi` — Write hooks and validators
- `IWorldCommandBufferApi` — Command buffer management
- `IWorldWorkerApi` — Worker API
- `IWorldSystemsApi` — System registration and management
- `IWorldResetApi` — World reset operations

### Entity

**Entity** is a container for components. It is represented as a packed 64-bit handle containing a generation and an ID.

**Type:** `ZenECS.Core.Entity` (struct)

**Structure:**
```
[ Gen (32 bits) | Id (32 bits) ]
```

**Key Properties:**
- `ulong Handle` — Raw 64-bit handle value
- `int Id` — Entity ID (lower 32 bits)
- `int Gen` — Generation (upper 32 bits)
- `bool IsNone` — Whether this is Entity.None (zero handle)
- `bool IsValid` — Whether handle is non-zero (does not guarantee liveness)

**Static Methods:**
```csharp
static ulong Pack(int id, int gen);
static (int id, int gen) Unpack(ulong handle);
```

**Usage Example:**

```csharp
// Create entities using command buffer
Entity player, enemy;
using (var cmd = world.BeginWrite())
{
    player = cmd.CreateEntity();
    enemy = cmd.CreateEntity();
}

// Check if entity is alive
if (world.IsAlive(player))
{
    // Entity is alive
}

// Access entity ID
int id = player.Id;
```

### WorldId

Stable, value-type identifier for a `IWorld`. Wraps a `Guid` to provide strong typing.

**Type:** `ZenECS.Core.WorldId` (struct)

**Properties:**
- `Guid Value` — The underlying globally unique identifier

**Usage:**
```csharp
var worldId = new WorldId(Guid.NewGuid());
var world = kernel.CreateWorld(null, presetId: worldId);
```

### WorldHandle

A safe handle that resolves to an `IWorld` instance.

**Type:** `ZenECS.Core.WorldHandle`

**Methods:**
```csharp
IWorld ResolveOrThrow();
bool TryResolve(out IWorld? world);
```

### Component

**Component** is a struct that stores data. It is a value type and stored in type-segregated pools.

**Characteristics:**
- Components must be `struct` value types
- Immutability pattern is recommended (`readonly struct`)
- Stored in type-segregated pools for memory efficiency
- Components can be added/removed from entities

**Component Definition Example:**

```csharp
// Position component
public readonly struct Position
{
    public readonly float X, Y;
    public Position(float x, float y)
    {
        X = x;
        Y = y;
    }
}

// Velocity component
public readonly struct Velocity
{
    public readonly float X, Y;
    public Velocity(float x, float y)
    {
        X = x;
        Y = y;
    }
}

// Tag component (no data)
public readonly struct Paused { }
```

**Component Operations:**

```csharp
// Add (using command buffer)
using (var cmd = world.BeginWrite())
{
    cmd.AddComponent(entity, new Position(0, 0));
}

// Read (by value)
var pos = world.ReadComponent<Position>(entity);

// Replace (using command buffer)
using (var cmd = world.BeginWrite())
{
    cmd.ReplaceComponent(entity, new Position(10, 20));
}

// Remove (using command buffer)
using (var cmd = world.BeginWrite())
{
    cmd.RemoveComponent<Velocity>(entity);
}

// Check
bool hasPos = world.HasComponent<Position>(entity);
```

### IWorldSingletonComponent

Components that implement this interface are treated as world-level singletons (at most one entity per world).

```csharp
public readonly struct Gravity : IWorldSingletonComponent
{
    public readonly float Value;
    public Gravity(float value) { Value = value; }
}

// Set singleton
world.SetSingleton(new Gravity(-9.8f));

// Get singleton
if (world.TryGetSingleton<Gravity>(out var gravity))
{
    // Use gravity.Value
}
```

### Systems

**System** is a class that encapsulates game logic. It queries and modifies components to update game state.

**System Interface:**

All systems implement the base `ISystem` interface:

```csharp
public interface ISystem
{
    void Run(IWorld w, float dt);
}
```

**Optional Interfaces:**
- **`ISystemLifecycle`**: Lifecycle management (Initialize/Shutdown)
- **`ISystemEnabledFlag`**: Enable/disable flag for systems

**System Groups:**

Systems are categorized into groups:

**Enum:** `ZenECS.Core.Systems.SystemGroup`

- `Unknown` — Unknown or not specified
- `FixedInput` — Fixed-step input phase
- `FixedDecision` — Fixed-step decision phase
- `FixedSimulation` — Fixed-step simulation phase
- `FixedPost` — Fixed-step post-simulation phase
- `FrameInput` — Per-frame input phase
- `FrameSync` — Per-frame sync phase
- `FrameView` — Per-frame view phase
- `FrameUI` — Per-frame UI phase

**Attributes:**
- **`[FixedGroup]`**: Maps to FixedSimulation group
- **`[FixedInputGroup]`**: Maps to FixedInput group
- **`[FixedDecisionGroup]`**: Maps to FixedDecision group
- **`[FixedPostGroup]`**: Maps to FixedPost group
- **`[FrameViewGroup]`**: Maps to FrameView group
- **`[FrameInputGroup]`**: Maps to FrameInput group
- **`[FrameSyncGroup]`**: Maps to FrameSync group
- **`[FrameUIGroup]`**: Maps to FrameUI group
- **`[OrderBefore(typeof(OtherSystem))]`**: Run before another system
- **`[OrderAfter(typeof(OtherSystem))]`**: Run after another system
- **`[Order(int priority)]`**: Order by priority value

**System Ordering:**

```csharp
[FixedGroup]
[OrderAfter(typeof(PhysicsSystem))]  // Run after PhysicsSystem
[OrderBefore(typeof(RenderSystem))]   // Run before RenderSystem
public sealed class MoveSystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        // Movement logic
    }
}
```

**System Writing Example:**

```csharp
using ZenECS.Core;
using ZenECS.Core.Systems;

[FixedGroup]
public sealed class MoveSystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        using var cmd = w.BeginWrite();
        // Query returns tuples: (entity, component1, component2, ...)
        foreach (var (e, pos, vel) in w.Query<Position, Velocity>())
        {
            // Update position using command buffer
            cmd.ReplaceComponent(e, new Position(
                pos.X + vel.X * dt,
                pos.Y + vel.Y * dt
            ));
        }
    }
}

[FrameViewGroup]
public sealed class PrintPositionsSystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        // Read-only query (no command buffer needed)
        foreach (var (e, pos) in w.Query<Position>())
        {
            Console.WriteLine($"Entity {e.Id}: {pos}");
        }
    }
}
```

**System Registration:**

```csharp
world.AddSystems([
    new MoveSystem(),
    new PrintPositionsSystem()
]);
```

### Query

**Query** is an efficient way to find entities that have specific components.

**Interface:** `ZenECS.Core.IWorldQueryApi`

**Basic Query:**

```csharp
// Single component query (returns tuples)
foreach (var (entity, pos) in world.Query<Position>())
{
    // Use pos directly
    Console.WriteLine($"Position: {pos.X}, {pos.Y}");
}

// Multiple component query
foreach (var (entity, pos, vel) in world.Query<Position, Velocity>())
{
    // Use pos and vel directly
    // Note: To modify components, use command buffer with ReplaceComponent
}
```

**Query Overloads:**

The API provides overloads for queries with 1 to 8 component types:

```csharp
QueryEnumerable<T1> Query<T1>(Filter f = default);
QueryEnumerable<T1, T2> Query<T1, T2>(Filter f = default);
QueryEnumerable<T1, T2, T3> Query<T1, T2, T3>(Filter f = default);
// ... up to T8
```

**Filtering:**

```csharp
using ZenECS.Core.Filters;

// Create filter
var filter = Filter.New
    .With<Position>()      // Requires Position component
    .With<Velocity>()      // Requires Velocity component
    .Without<Paused>()     // Must not have Paused component
    .Build();

// Use filter
foreach (var entity in world.Query<Position, Velocity>(filter))
{
    // Only process entities that satisfy filter conditions
}
```

**Performance Characteristics:**
- **Zero-allocation queries**: Enumerable queries allocate no heap memory
- **Optimized indexing**: Queries are seeded by the smallest component pool
- **BitSet-based**: Dense bitset for component presence tracking

### Command Buffer

**Command Buffer** is a mechanism that buffers structural changes (entity creation/destruction, component add/remove) and applies them in batches at safe boundaries.

**Interface:** `ZenECS.Core.ICommandBuffer`

**Use Cases:**
- Safely handle structural changes during system execution
- Buffer changes in multi-threaded environments
- Delay changes for deterministic execution

**Key Methods:**
```csharp
// Entity lifecycle
Entity CreateEntity();
void DestroyEntity(Entity e);
void DestroyAllEntities();

// Component operations
void AddComponent<T>(Entity e, in T value);
void ReplaceComponent<T>(Entity e, in T value);
void RemoveComponent<T>(Entity e);

// Singleton operations
void SetSingleton<T>(in T value) where T : struct, IWorldSingletonComponent;
void RemoveSingleton<T>();

// Control
void EndWrite();
```

**Basic Usage:**

```csharp
// Begin command buffer
using (var cmd = world.BeginWrite())
{
    // Buffer structural changes
    cmd.AddComponent(entity, new Health { Value = 100 });
    cmd.RemoveComponent<Stunned>(entity);
    cmd.DestroyEntity(entity);
    
    // ReplaceComponent is also supported
    cmd.ReplaceComponent(entity, new Health { Value = 75 });
} // Automatically applied when disposed

// Explicit application
world.RunScheduledJobs();
```

**Immediate Application:**

```csharp
using (var cmd = world.BeginWrite())
{
    cmd.AddComponent(entity, new Health { Value = 100 });
}
// EndWrite is automatically called and applied immediately
```

### Message Bus

**Message Bus** is a struct-based Pub/Sub messaging system. It implements unidirectional data flow from the view layer to the data layer.

**Base Interface:** `ZenECS.Core.Messaging.IMessage`

**Message Definition:**

```csharp
public readonly struct Damage : IMessage
{
    public readonly Entity Target;
    public readonly int Amount;
    
    public Damage(Entity target, int amount)
    {
        Target = target;
        Amount = amount;
    }
}
```

**IWorldMessagesApi Methods:**
```csharp
IDisposable Subscribe<T>(Action<T> handler) where T : struct, IMessage;
void Publish<T>(in T msg) where T : struct, IMessage;
int PumpAll();
void Clear();
```

**Subscribe and Publish:**

```csharp
// Subscribe
var subscription = world.Subscribe<Damage>(damage =>
{
    // Handle damage
    if (world.IsAlive(damage.Target) && world.HasComponent<Health>(damage.Target))
    {
        var health = world.ReadComponent<Health>(damage.Target);
        var newHealth = new Health(health.Value - damage.Amount);
        using (var cmd = world.BeginWrite())
        {
            cmd.ReplaceComponent(damage.Target, newHealth);
        }
    }
});

// Publish
world.Publish(new Damage { Target = entity, Amount = 10 });

// Unsubscribe
subscription.Dispose();

// Pump messages (typically called once per frame)
world.PumpMessages();
```

**Usage Pattern:**

The message bus implements **View → Data** unidirectional flow:

- **View Layer**: Publishes messages without directly modifying World
- **Simulation Layer**: Subscribes to messages and updates components
- **Presentation Layer**: Displays data in read-only mode

### Binding

**Binding** is a system for view integration. It connects ECS data with views (Unity, UI, audio, etc.) through Contexts and Binders.

**Interfaces:**
- `ZenECS.Core.Binding.IContext` — Container for view-related data
- `ZenECS.Core.Binding.IBinder` — Detects component changes and updates views
- `ZenECS.Core.Binding.IBinds<T>` — Interface for binders that react to component changes
- `ZenECS.Core.Binding.IRequireContext<T>` — Interface for binders that require a context
- `ComponentDelta<T>` — Component change information (old value, new value)

**Key Concepts:**
- **Context**: Container for view-related data
- **Binder**: Detects component changes and updates views
- **ComponentDelta**: Component change information (old value, new value)

**Binder Writing Example:**

```csharp
using ZenECS.Core.Binding;

public sealed class SpriteBinder : BaseBinder, IBinds<Position>, IRequireContext<SpriteContext>
{
    public void OnDelta(in ComponentDelta<Position> delta)
    {
        // Update view when component changes
        var sprite = GetContext<SpriteContext>().Sprite;
        sprite.transform.position = new Vector3(
            delta.NewValue.X,
            delta.NewValue.Y,
            0
        );
    }
}
```

**Context Definition:**

```csharp
public class SpriteContext : IContext
{
    public SpriteRenderer Sprite { get; set; }
}
```

### Hooks & Validators

**Hooks** and **Validators** provide world-scoped permission checks and value validation.

**Write Permission Hook:**

Controls component write permissions:

```csharp
// Write permission check
world.Hooks.AddWritePermission((entity, componentType) =>
{
    // GodMode component is write-protected
    return componentType != typeof(GodMode);
});
```

**Validator:**

Validates component values:

```csharp
// Health value validation
world.Hooks.AddValidator<Health>(health =>
{
    // Health must be >= 0
    return health.Value >= 0;
});
```

**Usage Example:**

```csharp
// Attempt to set invalid value (validation fails)
using (var cmd = world.BeginWrite())
{
    cmd.ReplaceComponent(entity, new Health { Value = -10 }); // Throws exception on validation failure
}

// Attempt to set component without permission
using (var cmd = world.BeginWrite())
{
    cmd.AddComponent(entity, new GodMode()); // Throws exception on permission check failure
}
```

**IWorldHookApi:** Write hooks and validators for a world.

### Snapshot I/O

**Snapshot I/O** is a feature for saving and loading world state. It supports pluggable backends and formatters.

**Interfaces:**
- `ZenECS.Core.Serialization.IComponentFormatter` — Formats components for serialization
- `ZenECS.Core.Serialization.ISnapshotBackend` — Backend for snapshot storage
- `ZenECS.Core.Serialization.IPostLoadMigration` — Performs data migration after loading

**IWorldSnapshotApi Methods:**
```csharp
void Save(Stream stream, IComponentFormatter formatter);
void Load(Stream stream, IComponentFormatter formatter, 
          IEnumerable<IPostLoadMigration>? migrations = null);
```

**Save:**

```csharp
using (var stream = File.Create("save.dat"))
{
    world.Save(stream, new BinaryComponentFormatter());
}
```

**Load:**

```csharp
using (var stream = File.OpenRead("save.dat"))
{
    var migrations = new List<IPostLoadMigration> 
    { 
        new V1ToV2Migration() 
    };
    world.Load(stream, new BinaryComponentFormatter(), migrations);
}
```

**Post-Load Migration:**

You can perform data migration after loading:

```csharp
public class V1ToV2Migration : IPostLoadMigration
{
    public void Migrate(IWorld world)
    {
        // Convert old version data to new version
        using (var cmd = world.BeginWrite())
        {
            foreach (var (entity, old) in world.Query<OldComponent>())
            {
                cmd.RemoveComponent<OldComponent>(entity);
                cmd.AddComponent(entity, new NewComponent(old.Data));
            }
        }
    }
}
```

---

## ⚙️ Configuration

### WorldConfig

Configuration for a world instance.

**Type:** `ZenECS.Core.WorldConfig`

### EcsRuntimeOptions

Runtime options for ECS.

**Type:** `ZenECS.Core.Config.EcsRuntimeOptions`

**Properties:**
- `IEcsLogger Log` — Logger instance

### IEcsLogger

Logger interface for ECS runtime.

**Interface:** `ZenECS.Core.Config.IEcsLogger`

---

## 🏷️ Attributes

### ZenComponentAttribute

Marks a component type and provides metadata.

**Type:** `ZenECS.Core.ZenComponentAttribute`

### ZenFormatterForAttribute

Specifies a formatter for a component type.

**Type:** `ZenECS.Core.ZenFormatterForAttribute`

### ZenDefaults

Provides default values for components.

**Type:** `ZenECS.Core.ZenDefaults`

### ZenSystemWatchAttribute

Marks a system to watch specific component types.

**Type:** `ZenECS.Core.ZenSystemWatchAttribute`

---

## 📡 Events

### EntityEvents

Events for entity lifecycle.

**Type:** `ZenECS.Core.EntityEvents`

### ComponentEvents

Events for component lifecycle.

**Type:** `ZenECS.Core.ComponentEvents`

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

## 📋 API Index

### Core Namespace (`ZenECS.Core`)

- `IKernel` / `Kernel` — Multi-world kernel
- `IWorld` — World API surface
- `Entity` — Entity handle
- `WorldId` — World identifier
- `WorldHandle` — Safe world handle
- `WorldConfig` — World configuration
- `KernelOptions` — Kernel configuration
- `ICommandBuffer` — Command buffer interface
- `ExternalCommand` — External command type

### Systems Namespace (`ZenECS.Core.Systems`)

- `ISystem` — Base system interface
- `ISystemLifecycle` — System lifecycle hooks
- `ISystemEnabledFlag` — Enable/disable flag
- `SystemGroup` — System execution groups
- System attribute classes

### Messaging Namespace (`ZenECS.Core.Messaging`)

- `IMessage` — Message interface

### Serialization Namespace (`ZenECS.Core.Serialization`)

- `IComponentFormatter` — Component formatter
- `ISnapshotBackend` — Snapshot backend
- `IPostLoadMigration` — Post-load migration

### Binding Namespace (`ZenECS.Core.Binding`)

- `IContext` — Context interface
- `IBinder` — Binder interface
- `IBinds<T>` — Component binder interface
- `IRequireContext<T>` — Context requirement interface
- `ComponentDelta<T>` — Component change delta

### Config Namespace (`ZenECS.Core.Config`)

- `IEcsLogger` — Logger interface
- `EcsRuntimeOptions` — Runtime options

---

## 🧩 Versioning & Compatibility

- **Target Frameworks**: `.NET Standard 2.1` / `.NET 8` (samples)
- **Unity**: `2021.3+` recommended
- **Versioning**: Semantic Versioning (SemVer)
- **Compatibility**: RC builds may adjust internal details without breaking public contracts

---

For detailed documentation, contributing guidelines, license information, and support, please refer to the main repository documentation.

