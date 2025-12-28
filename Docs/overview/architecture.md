# ZenECS Core Architecture Overview

> Comprehensive guide to ZenECS Core's internal structure and design principles

This document is for developers who want to deeply understand ZenECS Core's architecture. It covers internal structure, data flow, execution model, and extensibility.

## Table of Contents

1. [Overall Overview](#overall-overview)
2. [Architecture Layers](#architecture-layers)
3. [Core Components](#core-components)
4. [Data Flow](#data-flow)
5. [Execution Loop](#execution-loop)
6. [Threading Model](#threading-model)
7. [Dependency Injection and Service Composition](#dependency-injection-and-service-composition)
8. [Extensibility and Plugins](#extensibility-and-plugins)

---

## Overall Overview

ZenECS Core adopts a **layered architecture** to provide clear separation of concerns and high modularity.

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
│            (Unity Adapter, Game Logic, Systems)             │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                      Public API Layer                       │
│          (IWorld, IKernel, IMessageBus, etc.)               │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                     Core Runtime Layer                      │
│   (World, Kernel, SystemRunner, ComponentPoolRepository)    │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                  Infrastructure Layer                       │
│    (ServiceContainer, BitSet, Worker, Scheduling)           │
└─────────────────────────────────────────────────────────────┘
```

### Design Principles

1. **Single Responsibility Principle**: Each module has one clear role

2. **Dependency Inversion**: Depend on interfaces rather than concrete implementations

3. **Zero Allocation**: Queries and enumeration operations minimize heap allocations

4. **Deterministic Execution**: Fixed-step simulation ensures reproducible results

5. **Engine Independence**: No dependency on Unity or specific game engines

---

## Architecture Layers

### 1. Application Layer

The topmost layer where user code and game engine adapters reside.

**Responsibilities:**

- Implement game logic (systems, component definitions)
- Integration with Unity/other engines
- Game loop and frame management

**Example:**

```csharp
// Code written in the application layer
[SimulationGroup]
public sealed class MoveSystem : IFixedRunSystem
{
    public void Run(IWorld world, float fixedDelta)
    {
        foreach (var entity in world.Query<Position, Velocity>())
        {
            // Game logic
        }
    }
}
```

### 2. Public API Layer

The layer that defines interfaces exposed to external code.

**Key Interfaces:**

- `IKernel`: Multi-world management and frame tick coordination
- `IWorld`: Unified ECS API (entities, components, systems, queries, messages, etc.)
- `IMessageBus`: Pub/Sub messaging
- `ISystemRunner`: System execution orchestration

**Characteristics:**

- Hides implementation details (encapsulation)
- Enhanced testability (mockable)
- Version compatibility maintenance

### 3. Core Runtime Layer

The layer that implements the core ECS logic.

**Key Components:**

#### Kernel
- Multi-world lifecycle management
- Frame tick coordination (BeginFrame → FixedStep×N → LateFrame)
- World indexing (lookup by ID/name/tag)

#### World
- Entity storage management (BitSet-based)
- Component pool repository
- System execution pipeline
- Message bus
- Binding router
- Permission hooks and validators

#### SystemRunner
- System group execution planning
- Execution order guarantee
- Lifecycle management (Initialize/Shutdown)

#### ComponentPoolRepository
- Type-specific component pool management
- Lazy initialization
- Pool cleanup on entity removal

### 4. Infrastructure Layer

The bottommost layer providing basic utilities and services.

**Key Components:**

#### ServiceContainer
- Lightweight DI container
- Hierarchical scope support (Root → World)
- Singleton and factory patterns

#### BitSet
- Entity alive flag management
- Memory-efficient set operations

#### Worker
- Asynchronous job scheduling
- Job queue management

---

## Core Components

### Kernel

**Role:** Top-level manager that manages multiple worlds and coordinates the game loop

**Key Features:**

```csharp
public sealed class Kernel : IKernel
{
    // World indexes
    private readonly ConcurrentDictionary<WorldId, IWorld> _byId;
    private readonly ConcurrentDictionary<string, HashSet<WorldId>> _byName;
    private readonly ConcurrentDictionary<string, HashSet<WorldId>> _byTag;

    // Time tracking
    private float _simulationAccumulatorSeconds;
    private long _frameCount;
    private long _fixedFrameCount;

    // Frame ticks
    public void BeginFrame(float dt);
    public void FixedStep(float fixedDelta);
    public void LateFrame(float alpha);
    public void PumpAndLateFrame(float dt, float fixedDelta, int maxSubStepsPerFrame);
}
```

**Lifecycle:**

1. `Kernel` creation → Root ServiceContainer setup
2. `CreateWorld()` → Per-world ServiceContainer creation
3. Game loop: `BeginFrame` → `FixedStep×N` → `LateFrame`
4. `Dispose()` → Cleanup all worlds

### World

**Role:** ECS host representing a single simulation space

**Internal Structure:**

```csharp
internal sealed partial class World : IWorld
{
    private readonly IKernel _kernel;
    private readonly ServiceContainer _scope;  // DI scope

    // Services (resolved from DI)
    private readonly ISystemRunner _runner;
    private readonly IComponentPoolRepository _componentPoolRepository;
    private readonly IMessageBus _bus;
    private readonly IBindingRouter _bindingRouter;
    private readonly IPermissionHook _permissionHook;

    // Entity storage
    private BitSet _alive;           // Alive flags
    private int[] _generation;       // Generation counter (prevents stale handles)
    private Stack<int> _freeIds;     // Recyclable IDs
    private int _nextId;             // Next ID to issue
}
```

**Partial Class Separation:**

The `World` class is split into 15 partial files by functionality:

- `World.cs`: Basic structure and constructor
- `WorldEntityApi.cs`: Entity creation/deletion
- `WorldComponentApi.cs`: Component add/remove/query
- `WorldQueryApi.cs`: Query and filtering
- `WorldSystemsApi.cs`: System registration/removal
- `WorldMessagesApi.cs`: Message bus
- `WorldBinderApi.cs`: Binding
- `WorldSnapshot.cs`: Save/load
- Others...

### ComponentPoolRepository

**Role:** Repository that manages type-specific component pools

**Structure:**

```csharp
internal sealed class ComponentPoolRepository : IComponentPoolRepository
{
    // Type-specific pool storage
    private Dictionary<Type, IComponentPool> _pools;

    // Factory cache (minimizes reflection)
    private static readonly ConcurrentDictionary<Type, Func<IComponentPool>> _poolFactories;

    public IComponentPool GetPool<T>() where T : struct
    {
        // Lazy initialization: create pool on first use
        if (!_pools.TryGetValue(typeof(T), out var pool))
        {
            pool = new ComponentPool<T>();
            _pools.Add(typeof(T), pool);
        }
        return pool;
    }
}
```

**Characteristics:**

- **Type-specific separation**: Each component type stored in independent arrays
- **Lazy initialization**: Pool created on first use (memory efficient)
- **Cached factories**: Minimizes reflection overhead

### SystemRunner

**Role:** Orchestrates system execution pipeline

**Execution Flow:**

```csharp
internal sealed class SystemRunner : ISystemRunner
{
    private SystemPlan _plan;  // Execution plan

    public void BeginFrame(IWorld w, float dt)
    {
        // 1. Pump messages
        _bus.PumpAll();

        // 2. Execute FrameInput systems
        RunGroup(SystemGroup.FrameInput, w, dt);
        _worker.RunScheduledJobs(w);

        // 3. Execute FrameSync systems
        RunGroup(SystemGroup.FrameSync, w, dt);
        _worker.RunScheduledJobs(w);
    }

    public void FixedStep(IWorld w, float fixedDelta)
    {
        // Fixed-step pipeline:
        // FixedInput → FixedDecision → FixedSimulation → FixedPost
        RunFixedGroup(SystemGroup.FixedInput, w, fixedDelta);
        RunFixedGroup(SystemGroup.FixedDecision, w, fixedDelta);
        RunFixedGroup(SystemGroup.FixedSimulation, w, fixedDelta);
        RunFixedGroup(SystemGroup.FixedPost, w, fixedDelta);
    }

    public void LateFrame(IWorld w, float dt, float alpha)
    {
        // Presentation pipeline:
        // FrameView → FrameUI → Apply binders
        RunLateGroup(SystemGroup.FrameView, w, dt, alpha);
        RunLateGroup(SystemGroup.FrameUI, w, dt, alpha);
        _router.ApplyAll(w);  // View binding
    }
}
```

**System Groups:**

| Group | Execution Point | Purpose |
|-------|----------------|---------|
| `FixedInput` | FixedStep | Player input sampling |
| `FixedDecision` | FixedStep | AI, pathfinding, decisions |
| `FixedSimulation` | FixedStep | Physics, gameplay state updates |
| `FixedPost` | FixedStep | Cleanup, events, bookkeeping |
| `FrameInput` | BeginFrame | Device input, window events |
| `FrameSync` | BeginFrame | Camera, client prediction |
| `FrameView` | LateFrame | Interpolation, transforms, animation |
| `FrameUI` | LateFrame | UI, HUD, debug overlays |

---

## Data Flow

### Component Data Flow

```
┌─────────────┐
│  Component  │  struct Position { float X, Y; }
│  Definition │
└──────┬──────┘
       │
       ↓
┌──────────────────────────┐
│  ComponentPool<T>        │  Stored as type-specific arrays
│  - Array: T[]            │
│  - Index: Entity.Id      │
└──────┬───────────────────┘
       │
       ↓
┌──────────────────────────┐
│  World                   │  Unified API
│  - Get<T>(entity)        │
│  - Ref<T>(entity)        │
│  - Query<T...>()         │
└──────┬───────────────────┘
       │
       ↓
┌──────────────────────────┐
│  System                  │  Query and modify
│  foreach (var e in       │
│    world.Query<T...>())  │
└──────────────────────────┘
```

### Message Flow

```
┌──────────────┐
│   Publisher  │  world.Publish(new DamageMessage(...))
└──────┬───────┘
       │
       ↓
┌──────────────────┐
│   MessageBus     │  Stored in message queue
│   - Queue<T>     │
└──────┬───────────┘
       │
       ↓ (On PumpAll call)
┌──────────────────┐
│   Subscribers    │  Execute registered handlers
│   - Action<T>    │
└──────────────────┘
```

### Binding Flow (View ↔ Data)

```
┌──────────────────┐
│   World          │  ECS data
│   - Components   │
└──────┬───────────┘
       │ Delta occurs
       ↓
┌──────────────────┐
│ BindingRouter    │  Track changes
│ - DeltaTracker   │
└──────┬───────────┘
       │ On ApplyAll
       ↓
┌──────────────────┐
│   Binder         │  Update view
│   OnDelta()      │  sprite.transform.position = ...
└──────────────────┘
```

---

## Execution Loop

### Frame Structure

ZenECS uses a **3-phase frame structure**:

```
┌─────────────────────────────────────────────────────────┐
│                    BeginFrame(dt)                       │
│  ┌───────────────────────────────────────────────────┐  │
│  │ FrameInput Systems                                │  │
│  │ - Device input, window events                     │  │
│  └───────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────┐  │
│  │ FrameSync Systems                                 │  │
│  │ - Camera, client prediction                       │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│              FixedStep(fixedDelta) × N                  │
│  (Convert dt to fixed steps using accumulator)          │
│  ┌───────────────────────────────────────────────────┐  │
│  │ FixedInput Systems                                │  │
│  │ - Player input sampling                           │  │
│  └───────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────┐  │
│  │ FixedDecision Systems                             │  │
│  │ - AI, pathfinding, decisions                      │  │
│  └───────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────┐  │
│  │ FixedSimulation Systems                           │  │
│  │ - Physics, gameplay state updates                 │  │
│  └───────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────┐  │
│  │ FixedPost Systems                                 │  │
│  │ - Cleanup, events, bookkeeping                    │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────┐
│                LateFrame(dt, alpha)                     │
│  ┌───────────────────────────────────────────────────┐  │
│  │ FrameView Systems                                 │  │
│  │ - Interpolation, transforms, animation            │  │
│  └───────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────┐  │
│  │ FrameUI Systems                                   │  │
│  │ - UI, HUD, debug overlays                         │  │
│  └───────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────┐  │
│  │ BindingRouter.ApplyAll()                          │  │
│  │ - Apply view bindings                             │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### Accumulator-Based Fixed-Step

To ensure fixed steps, ZenECS uses the **Time Accumulator** pattern:

```csharp
public void PumpAndLateFrame(float dt, float fixedDelta, int maxSubStepsPerFrame)
{
    BeginFrame(dt);
    
    // Add dt to accumulator
    _simulationAccumulatorSeconds += dt;
    
    // Execute fixed steps (up to maxSubStepsPerFrame times)
    int subSteps = 0;
    while (_simulationAccumulatorSeconds >= fixedDelta && subSteps < maxSubStepsPerFrame)
    {
        FixedStep(fixedDelta);
        _simulationAccumulatorSeconds -= fixedDelta;
        subSteps++;
    }
    
    // Calculate interpolation factor (predict next frame)
    float alpha = _simulationAccumulatorSeconds / fixedDelta;
    LateFrame(dt, alpha);
}
```

**Advantages:**

- ✅ Deterministic simulation (fixed step)
- ✅ Frame drop handling (accumulated time processing)
- ✅ Spike protection (maxSubStepsPerFrame limit)

---

## Threading Model

### Single-Threaded Default Model

By default, ZenECS assumes a **single-threaded** model:

- System execution is sequential
- Component access is synchronous
- Message processing is single-threaded

### Thread Safety Guarantees

Designed to be safely used in multi-threaded environments:

**1. World Indexing (Kernel)**

```csharp
// Use ConcurrentDictionary
private readonly ConcurrentDictionary<WorldId, IWorld> _byId;
private readonly ConcurrentDictionary<string, HashSet<WorldId>> _byName;
```

**2. Snapshots and Locks**

- Thread-safe snapshots provided on world lookup
- Internal lock usage (read performance optimized)

**3. Command Buffer**

```csharp
// Buffer structural changes during system execution
using (var cmd = world.BeginWrite())
{
    cmd.CreateEntity();
    cmd.AddComponent(entity, component);
} // Applied at safe boundaries
```

### Worker and Job Scheduling

Jobs can be scheduled via the `IWorker` interface:

```csharp
public interface IWorker
{
    void ScheduleJob(IWorld world, Action job);
    void RunScheduledJobs(IWorld world);
}
```

**Usage Example:**

```csharp
// Schedule job from system
_worker.ScheduleJob(world, () =>
{
    // Background work
});

// Execute jobs at end of frame
_worker.RunScheduledJobs(world);
```

---

## Dependency Injection and Service Composition

### Hierarchical DI Scopes

ZenECS uses **hierarchical DI scopes**:

```
Root ServiceContainer (App lifetime)
  └─ KernelOptions
  └─ Other app-global services

World ServiceContainer (World lifetime)
  ├─ IWorker
  ├─ IMessageBus
  ├─ IComponentPoolRepository
  ├─ IBindingRouter
  ├─ IContextRegistry
  ├─ IPermissionHook
  └─ ISystemRunner
```

### Bootstrap Process

```csharp
// 1. Create root scope
var root = CoreBootstrap.BuildRoot(kernelOptions);

// 2. Create Kernel (using root scope)
var kernel = new Kernel(options, root);

// 3. Create world scope
var worldScope = CoreBootstrap.BuildWorldScope(worldConfig, root);

// 4. Create World
var world = new World(cfg, id, name, tags, kernel, worldScope);
```

### ServiceContainer Characteristics

**Lightweight DI Container:**

- No external dependencies (pure C#)
- Singleton and factory pattern support
- Hierarchical scopes (Parent-Child)
- Seal functionality (immutability after composition)
- Reverse Dispose (child → parent)

**Example:**

```csharp
internal static class CoreBootstrap
{
    internal static ServiceContainer BuildWorldScope(WorldConfig cfg, ServiceContainer root)
    {
        var world = root.CreateChildScope();

        // Register services
        world.RegisterFactory<IWorker>(_ => new Worker(), asSingleton: true);
        world.RegisterFactory<IMessageBus>(_ => new MessageBus(), asSingleton: true);
        world.RegisterFactory<IComponentPoolRepository>(
            _ => new ComponentPoolRepository(cfg.InitialPoolBuckets),
            asSingleton: true);

        world.Seal();  // No further registration
        return world;
    }
}
```

---

## Extensibility and Plugins

### Adapter Extension

ZenECS integrates with game engines through the **adapter pattern**:

**Unity Adapter Example:**

```csharp
// Unity adapter extends World scope
var unityScope = worldScope.CreateChildScope();

unityScope.RegisterFactory<IUnityViewService>(
    _ => new UnityViewService(),
    asSingleton: true);

// Add Unity-specific services
```

### Custom System Groups

To add a new system group:

1. Add to `SystemGroup` enum
2. Add group processing logic to `SystemPlanner`
3. Add execution method to `SystemRunner`

### Custom Component Formatters

Custom formatters for snapshot save/load:

```csharp
public interface IComponentFormatter
{
    void WriteComponent(Stream stream, Type componentType, object component);
    object ReadComponent(Stream stream, Type componentType);
}

// Use Binary formatter
world.Save(stream, new BinaryComponentFormatter());

// Or implement custom JSON formatter
world.Save(stream, new JsonComponentFormatter());
```

### Hook and Validator Extensions

Add custom hooks to control component writes:

```csharp
// Write permission hook
world.Hooks.AddWritePermission((entity, componentType) =>
{
    // Custom logic
    return true;
});

// Value validator
world.Hooks.AddValidator<Health>(health =>
{
    // Custom validation logic
    return health.Current >= 0 && health.Current <= health.Max;
});
```

---

## Performance Optimization Strategies

### 1. Component Pooling

- **Type-specific separation**: Each component type in independent arrays
- **Lazy initialization**: Pool created on first use
- **Capacity management**: Auto-expansion when needed

### 2. Zero-Allocation Queries

```csharp
// Struct-based enumerator (no heap allocation)
public struct QueryEnumerator<T> : IEnumerator<Entity>
{
    // Value type enumerator
}
```

### 3. BitSet Optimization

Dense bitset for entity alive tracking:

- Fast lookup (O(1))
- Efficient memory usage
- Vectorizable operations

### 4. Filter Caching

```csharp
// Cache filter interpretation results
private static readonly ConcurrentDictionary<FilterSignature, Filter> _cachedFilters;
```

### 5. System Plan Caching

```csharp
// Rebuild plan only when systems are added/removed
private SystemPlan _plan;
private bool _dirty;

public void AddSystem(ISystem system)
{
    _pendingAdd.Add(system);
    _dirty = true;  // Replan next frame
}
```

---

## Architecture Diagrams

### Overall Structure

```
┌────────────────────────────────────────────────────────────┐
│                         Application                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   Systems    │  │  Components  │  │   Adapters   │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└────────────────────────────────────────────────────────────┘
                           ↓ uses
┌────────────────────────────────────────────────────────────┐
│                          Public API                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │    IKernel   │  │    IWorld    │  │  IMessageBus │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└────────────────────────────────────────────────────────────┘
                           ↓ implements
┌────────────────────────────────────────────────────────────┐
│                        Core Runtime                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │    Kernel    │  │    World     │  │ SystemRunner │      │
│  │              │  │              │  │              │      │
│  │ - World mgmt │  │ - Entities   │  │ - Exec plan  │      │
│  │ - Frame ticks│  │ - Components │  │ - Group exec │      │
│  │ - Indexing   │  │ - Systems    │  │ - Lifecycle  │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  ┌──────────────────────────────────────────────────────┐  │
│  │         ComponentPoolRepository                      │  │
│  │         - Type-specific pool mgmt                    │  │
│  └──────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────┘
                           ↓ uses
┌────────────────────────────────────────────────────────────┐
│                    Infrastructure                          │
│  ┌────────────────┐  ┌──────────────┐  ┌──────────────┐    │
│  │ServiceContainer│  │    BitSet    │  │    Worker    │    │
│  └────────────────┘  └──────────────┘  └──────────────┘    │
└────────────────────────────────────────────────────────────┘
```

### World Internal Structure

```
┌────────────────────────────────────────────────────────────┐
│                          World                             │
│  ┌──────────────────────────────────────────────────────┐  │
│  │              Entity Storage                          │  │
│  │  - BitSet _alive          (Alive flags)              │  │
│  │  - int[] _generation      (Generation counter)       │  │
│  │  - Stack<int> _freeIds    (Recyclable IDs)           │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │      ComponentPoolRepository                         │  │
│  │  Dictionary<Type, IComponentPool>                    │  │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐            │  │
│  │  │ Pool<T1> │  │ Pool<T2> │  │ Pool<T3> │  ...       │  │
│  │  └──────────┘  └──────────┘  └──────────┘            │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │            SystemRunner                              │  │
│  │  - SystemPlan _plan                                  │  │
│  │  - Group execution                                   │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │            MessageBus                                │  │
│  │  Dictionary<Type, Queue<IMessage>>                   │  │
│  └──────────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────────┐  │
│  │            BindingRouter                             │  │
│  │  - DeltaTracker                                      │  │
│  │  - Binder management                                 │  │
│  └──────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────┘
```

---

## Summary

ZenECS Core has the following architectural characteristics:

✅ **Layered Architecture**: Clear separation of concerns

✅ **DI-based Composition**: Hierarchical service scopes

✅ **Deterministic Execution**: Fixed-step simulation

✅ **Performance Optimization**: Zero-allocation queries, component pooling

✅ **Extensible**: Adapter pattern, custom hooks

✅ **Engine Independence**: No dependency on Unity or specific game engines

This design enables ZenECS to be used flexibly and efficiently in various game projects.

---

**Made with ❤️ by Pippapips Limited**
