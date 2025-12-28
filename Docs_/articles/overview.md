# ZenECS Overview

ZenECS is a pure C# Entity-Component-System framework built for Clean Architecture and Reactive Programming.

## What is ZenECS?

ZenECS separates your game logic into three distinct layers:

- **Data Layer** (Components) — Pure data structures, no logic
- **Simulation Layer** (Systems) — Pure functions that transform component data
- **Presentation Layer** (Binders) — Reactive view updates that respond to data changes

## Key Benefits

### Clean Architecture
- Strict boundaries between layers
- View never writes directly to data
- Systems are pure functions
- Multi-world isolation
- Zero dependencies

### Reactive Programming
- Message bus for event-driven architecture
- ComponentDelta bindings for automatic change detection
- Unidirectional data flow
- Optional UniRx integration

### Production Ready
- Deterministic stepping for networking
- Thread-safe command buffers
- Snapshot I/O with migrations
- Built-in thread safety

## Learn More

- [API Reference](../api/)
- [Core Documentation](../../Packages/com.zenecs.core/README.md)
- [Unity Adapter Documentation](../../Packages/com.zenecs.adapter.unity/README.md)
