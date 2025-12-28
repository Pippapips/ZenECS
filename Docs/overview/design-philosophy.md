# Design Philosophy

> Docs / Overview / Design Philosophy

This document explains the core principles and design decisions behind ZenECS.

## Core Principles

### 1. Clarity Over Cleverness

ZenECS prioritizes readable, maintainable code over clever optimizations.

**Examples:**

- **Explicit APIs**: Methods have clear names that express intent
- **Minimal magic**: No hidden behavior or implicit conventions
- **Self-documenting**: Code structure reveals design intent

```csharp
// Clear and explicit
Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position(0, 0));
}

// Not: world << Position(0, 0)  // Too clever
```

### 2. Separation of Concerns

Strict boundaries between data, simulation, and presentation.

**Three Layers:**

1. **Data (Components)**: Pure structs, no behavior
2. **Simulation (Systems)**: Pure logic, no view dependencies
3. **Presentation (Binders)**: View updates, read-only data access

```csharp
// ✅ Good: Clear separation
public struct Position { public float X, Y; }  // Data

public class MovementSystem : ISystem { }      // Simulation

public class PositionBinder : IBinder { }      // Presentation

// ❌ Bad: Mixed concerns
public class Player : MonoBehaviour
{
    public Vector3 position;
    public void Update() { /* logic + view */ }
}
```

### 3. Engine Independence

Core runtime has zero dependencies on game engines.

**Benefits:**

- Testable without Unity
- Usable in any .NET environment
- Adapter pattern for engine integration

## Design Decisions

### Why .NET Standard 2.1?

- **Broad compatibility**: Works with Unity, .NET Core, .NET Framework
- **Modern features**: Value types, spans, async/await
- **Future-proof**: Upgrade path to .NET 6+

### Why Struct Components?

- **Performance**: Value types, no heap allocation
- **Cache-friendly**: Contiguous memory layout
- **Immutability**: Encourages functional programming

### Why Multi-World Kernel?

- **Isolation**: Separate simulation spaces
- **Flexibility**: Split-screen, server/client, game modes
- **Testability**: Isolated test worlds

### Why Message Bus?

- **Decoupling**: Systems don't know about each other
- **Event-driven**: React to events, not poll
- **Deterministic**: Struct-based, predictable delivery

### Why Command Buffers?

- **Safety**: Batch structural changes
- **Determinism**: Apply at safe boundaries
- **Thread-safety**: Safe multi-threaded access

## Architecture Choices

### Layered Architecture

```
Application Layer (Your Code)
    ↓
Public API Layer (Interfaces)
    ↓
Core Runtime Layer (Implementation)
    ↓
Infrastructure Layer (Utilities)
```

**Benefits:**

- Clear boundaries
- Testable layers
- Extensible design

### Dependency Injection

Lightweight internal DI container:

- **Hierarchical scopes**: Root → World
- **Service composition**: Extensible bootstrap
- **No external deps**: Pure C# implementation

### Deterministic Execution

Fixed-step simulation with accumulator:

- **Reproducible**: Same input = same output
- **Network-friendly**: Deterministic for multiplayer
- **Replay support**: Record and replay simulations

## Trade-offs

### What We Prioritize

✅ **Clarity**: Readable, maintainable code  
✅ **Testability**: Easy to test and mock  
✅ **Flexibility**: Extensible and adaptable  
✅ **Documentation**: Well-documented APIs

### What We Sacrifice

❌ **Maximum performance**: Not as fast as DOTS  
❌ **Complexity**: Simpler than some ECS frameworks  
❌ **Features**: Focused feature set

## Comparison with Other Approaches

### vs Unity DOTS

| Aspect | ZenECS | Unity DOTS |
|--------|--------|------------|
| **Performance** | Good | Excellent |
| **Clarity** | High | Medium |
| **Learning Curve** | Gentle | Steep |
| **Engine Dependency** | None | Unity only |

### vs Entitas

| Aspect | ZenECS | Entitas |
|--------|--------|---------|
| **Code Generation** | None | Extensive |
| **API Style** | Explicit | Fluent |
| **Unity Integration** | Optional | Required |
| **.NET Support** | Full | Unity only |

### vs Flecs

| Aspect | ZenECS | Flecs |
|--------|--------|-------|
| **Language** | C# | C |
| **Performance** | Good | Excellent |
| **Ease of Use** | High | Medium |
| **Platform Support** | .NET | Native |

## Future Considerations

### Potential Enhancements

- **Parallel systems**: Multi-threaded system execution
- **Hot reload**: Runtime system updates
- **Visual editor**: GUI for entity configuration
- **Performance profiler**: Built-in profiling tools

### Stability Promise

- **API stability**: 1.0 APIs are locked
- **Breaking changes**: Only in major versions
- **Migration guides**: Provided for upgrades

## See Also

- [Architecture](./architecture.md) - Technical architecture details
- [ZenECS at a Glance](./zenecs-at-a-glance.md) - Feature overview
- [FAQ](./faq.md) - Common questions
