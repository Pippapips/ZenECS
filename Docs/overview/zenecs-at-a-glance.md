# ZenECS at a Glance

> Docs / Overview / ZenECS at a glance

**ZenECS** is a pure C# Entity-Component-System (ECS) runtime designed for clarity, determinism, and zero dependencies. It provides a robust foundation for building scalable and maintainable game logic with an optional Unity integration layer.

## Key Features

### 🏗️ Clean Architecture

- **Layered Separation**: Strict boundaries between data (components), simulation (systems), and presentation (binders)
- **Multi-World Kernel**: Manage multiple isolated simulation spaces for advanced scenarios
- **Zero Dependencies**: Pure .NET Standard 2.1 core, adaptable to any engine
- **Testable by Design**: Systems are pure functions, making game logic easy to test
- **Dependency Injection**: Extensible internal DI for flexible service composition

### ⚡ Reactive Programming

- **Message Bus**: Build event-driven architectures with struct-based pub/sub messaging
- **ComponentDelta Bindings**: Automatically react to component changes and update views efficiently
- **Unidirectional Data Flow**: Enforce clear data flow from view (messages) → simulation (state mutation) → presentation (read-only)
- **UniRx Integration**: Seamlessly integrate with UniRx for powerful reactive compositions

### 🎯 Production Ready

- **Deterministic Stepping**: Ensure reproducible simulations with explicit frame structures
- **Command Buffers**: Safely batch and apply structural changes in multi-threaded environments
- **Snapshot I/O**: Save and load world states with pluggable backends and post-load migrations
- **Thread Safety**: Built-in patterns for safe multi-threaded access to core ECS elements

## Philosophy

ZenECS is built on three core principles:

### 1. Clarity Over Cleverness

- **Explicit over implicit**: Clear API surface with minimal magic
- **Readable code**: Self-documenting APIs that express intent
- **Predictable behavior**: Deterministic execution with clear lifecycle

### 2. Separation of Concerns

- **Data (Components)**: Pure data structures, no behavior
- **Simulation (Systems)**: Pure logic that transforms data
- **Presentation (Binders)**: View layer that reads data reactively

### 3. Engine Independence

- **Zero engine dependencies**: Core runs anywhere .NET Standard 2.1 is supported
- **Optional adapters**: Unity integration is a separate, optional layer
- **Flexible deployment**: Use in Unity, Godot, standalone .NET, or server applications

## When to Use

ZenECS is ideal for:

- **Game Development**: Unity games requiring clean architecture and testability
- **Simulations**: Deterministic simulations for networking or replays
- **Server Applications**: Game servers needing ECS architecture
- **Educational Projects**: Learning ECS patterns with a clear, well-documented framework
- **Prototyping**: Rapid iteration with data-driven entity configuration

### When NOT to Use

ZenECS may not be suitable if:

- You need maximum performance at all costs (consider DOTS/ECS for Unity)
- You prefer object-oriented patterns over data-oriented design
- You require extensive third-party integrations out of the box

## Compatibility

### .NET Support

- **.NET Standard 2.1**: Core runtime
- **.NET Framework 4.8+**: Compatible via .NET Standard
- **.NET Core 3.1+**: Full support
- **.NET 5+**: Full support

### Unity Support

- **Unity 2021.3+**: Recommended minimum version
- **Unity 2022.1+**: Full feature support
- **Unity 2023.1+**: Latest features and optimizations

### Platform Support

- **Windows**: Full support
- **macOS**: Full support
- **Linux**: Full support
- **WebGL**: Limited (no threading support)
- **Mobile**: iOS and Android supported

## Next Steps

Ready to get started? Follow these steps:

1. **[Install ZenECS](../getting-started/install-upm.md)** - Add the package to your project
2. **[Quick Start Guide](../getting-started/quickstart-basic.md)** - Build your first ECS system
3. **[Explore Samples](../samples/01-basic.md)** - Learn from working examples
4. **[Read Architecture Guide](./architecture.md)** - Understand the design

### Learning Path

**Beginner:**
- Start with [Quick Start](../getting-started/quickstart-basic.md)
- Read [What is ECS?](./what-is-ecs.md)
- Try [Basic Sample](../samples/01-basic.md)

**Intermediate:**
- Study [Core Concepts](../core/world.md)
- Learn [Systems](../core/systems.md)
- Explore [Message Bus](../core/message-bus.md)

**Advanced:**
- Master [Binding System](../core/binding.md)
- Understand [Architecture](./architecture.md)
- Read [Advanced Topics](../guides/advanced-topics.md)

> **Tip**: Start with Quick Start, then open the Basic sample. The best way to learn ZenECS is by building something!

## See Also

- [What is ECS?](./what-is-ecs.md) - Introduction to Entity-Component-System architecture
- [Architecture](./architecture.md) - Deep dive into ZenECS design
- [Design Philosophy](./design-philosophy.md) - Core principles and decisions
- [FAQ](./faq.md) - Frequently asked questions
- [Glossary](./glossary.md) - Terminology reference
