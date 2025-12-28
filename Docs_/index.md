---
_layout: landing
---

# Welcome to ZenECS Documentation

ZenECS is a pure C# Entity-Component-System (ECS) runtime designed for clarity, determinism, and zero dependencies. It offers a robust foundation for building scalable and maintainable game logic, with an optional Unity integration layer.

## What is ZenECS?

ZenECS provides a powerful, engine-agnostic ECS framework that promotes a clean architecture and reactive programming patterns. It separates your game logic into distinct layers:

- **Data (Components):** Pure data structures defining your game state.
- **Simulation (Systems):** Logic that transforms data, operating on queries of components.
- **Presentation (Binders):** Connects ECS data to your view layer (Unity GameObjects, UI, etc.) reactively.

This strict separation ensures high testability, maintainability, and scalability for complex projects.

## Why ZenECS?

### 🏗️ Clean Architecture
- **Layered Separation:** Achieve strict boundaries between data, simulation, and presentation.
- **Multi-World Kernel:** Manage multiple isolated simulation spaces for advanced scenarios.
- **Zero Dependencies:** A lightweight, pure .NET Standard 2.1 core, adaptable to any engine.
- **Testable by Design:** Systems are pure functions, making your game logic easy to test.
- **Dependency Injection:** Extensible internal DI for flexible service composition.

### ⚡ Reactive Programming
- **Message Bus:** Build event-driven architectures with struct-based pub/sub messaging.
- **ComponentDelta Bindings:** Automatically react to component changes and update views efficiently.
- **Unidirectional Data Flow:** Enforce clear data flow from view (messages) → simulation (state mutation) → presentation (read-only).
- **UniRx Integration:** Seamlessly integrate with UniRx for powerful reactive compositions.

### 🎯 Production Ready
- **Deterministic Stepping:** Ensure reproducible simulations with explicit frame structures and topological system ordering.
- **Command Buffers:** Safely batch and apply structural changes in multi-threaded or deterministic environments.
- **Snapshot I/O:** Save and load world states with pluggable backends and post-load migrations for version compatibility.
- **Thread Safety:** Built-in patterns for safe multi-threaded access to core ECS elements.

## Quick Start

### For Unity Developers
Integrate ZenECS into your Unity project with the `ZenECS Adapter for Unity` package.

1. **Install:** Add `com.zenecs.core` and `com.zenecs.adapter.unity` via Unity Package Manager (Git URL).
2. **EcsDriver:** Add an `EcsDriver` component to your scene for automatic kernel lifecycle management.
3. **EntityLink:** Use `EntityLink` to connect Unity GameObjects to ECS entities.
4. **EntityBlueprint:** Create `ScriptableObject` blueprints to define and spawn pre-configured entities.

[Learn more about ZenECS Adapter for Unity](../Packages/com.zenecs.adapter.unity/README.md)

### For .NET Standalone Applications
ZenECS Core is a pure .NET library, perfect for server-side logic, simulations, or custom engine development.

1. **Install:** Add `ZenECS.Core` via NuGet.
2. **Kernel & World:** Instantiate `Kernel` and `World` to set up your simulation environment.
3. **Systems & Components:** Define your `struct` components and `ISystem` logic.
4. **Game Loop:** Drive your simulation with `kernel.PumpAndLateFrame()`.

[Learn more about ZenECS Core](../Packages/com.zenecs.core/README.md)

## Documentation Structure

This documentation site includes:

- **[Getting Started](docs/getting-started/quickstart-basic.html)** — Installation, setup, and quick start guides
- **[Core Concepts](docs/core/world.html)** — Entities, components, systems, worlds, and messaging
- **[Unity Adapter](docs/adapter-unity/overview.html)** — Unity integration guides and tutorials
- **[Guides](docs/guides/advanced-topics.html)** — Advanced topics and best practices
- **[Samples](docs/samples/01-basic.html)** — Code examples and tutorials
- **[API Reference](api/index.html)** — Complete API documentation

## Packages Overview

- **ZenECS Core:** The foundational, engine-agnostic ECS runtime. Provides the core concepts of entities, components, systems, queries, message bus, and binding.
- **ZenECS Adapter for Unity:** An optional layer that seamlessly integrates ZenECS Core with the Unity engine, offering MonoBehaviour-based lifecycle management, editor tools, and Unity-specific data-driven workflows.

---
MIT © Pippapips Limited
