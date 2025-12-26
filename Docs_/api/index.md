# API Reference

Complete API reference for ZenECS, auto-generated from XML documentation comments.

## Overview

ZenECS API documentation is automatically generated from XML comments in the source code using DocFX. This ensures documentation stays in sync with the codebase.

## Quick Navigation

### Core Namespaces

- **[ZenECS.Core](xref:ZenECS.Core)** — Core ECS runtime
  - `IKernel`, `IWorld`, `Entity`, `Component`
  - `ICommandBuffer`, `IWorldQueryApi`, `IWorldComponentApi`
  - `IWorldSystemsApi`, `IWorldMessagesApi`, `IWorldBinderApi`

- **[ZenECS.Core.Systems](xref:ZenECS.Core.Systems)** — System interfaces
  - `ISystem`, system groups, ordering attributes

- **[ZenECS.Core.Messaging](xref:ZenECS.Core.Messaging)** — Message bus
  - `IMessage`, message publishing and subscription

- **[ZenECS.Core.Binding](xref:ZenECS.Core.Binding)** — Binding system
  - `IBinder<T>`, `ComponentDelta<T>`, `IContext`, `IRequireContext<T>`

- **[ZenECS.Core.Serialization](xref:ZenECS.Core.Serialization)** — Snapshot I/O
  - `IComponentFormatter`, `ISnapshotBackend`, `IPostLoadMigration`

### Unity Adapter Namespaces

- **[ZenECS.Adapter.Unity](xref:ZenECS.Adapter.Unity)** — Unity integration
  - `EcsDriver`, `KernelLocator`, `ZenEcsUnityBridge`

- **[ZenECS.Adapter.Unity.Linking](xref:ZenECS.Adapter.Unity.Linking)** — Entity linking
  - `EntityLink`, `EntityViewRegistry`, `EntityLinkExtensions`

- **[ZenECS.Adapter.Unity.Blueprints](xref:ZenECS.Adapter.Unity.Blueprints)** — Blueprint system
  - `EntityBlueprint`, `EntityBlueprintData`

- **[ZenECS.Adapter.Unity.SystemPresets](xref:ZenECS.Adapter.Unity.SystemPresets)** — System presets
  - `SystemPreset`, `ISystemPresetResolver`

## Key Types

### Core Types

- **[IKernel](xref:ZenECS.Core.IKernel)** — Multi-world kernel management
- **[IWorld](xref:ZenECS.Core.IWorld)** — World API surface
- **[Entity](xref:ZenECS.Core.Entity)** — Entity handle
- **[ICommandBuffer](xref:ZenECS.Core.ICommandBuffer)** — Command buffer for structural changes

### World APIs

- **[IWorldQueryApi](xref:ZenECS.Core.IWorldQueryApi)** — Entity queries
- **[IWorldComponentApi](xref:ZenECS.Core.IWorldComponentApi)** — Component operations
- **[IWorldSystemsApi](xref:ZenECS.Core.IWorldSystemsApi)** — System management
- **[IWorldMessagesApi](xref:ZenECS.Core.IWorldMessagesApi)** — Message bus
- **[IWorldBinderApi](xref:ZenECS.Core.IWorldBinderApi)** — Binding system
- **[IWorldCommandBufferApi](xref:ZenECS.Core.IWorldCommandBufferApi)** — Command buffers

## How to Use

### Finding API Documentation

1. **Browse by namespace** — Use the table of contents to navigate to namespaces
2. **Search** — Use the search box to find specific types or members
3. **Cross-references** — Click on type names to navigate to their documentation

### Examples

**Find a method:**
- Navigate to namespace (e.g., `ZenECS.Core`)
- Find type (e.g., `IWorld`)
- Locate method (e.g., `BeginWrite`)

**Browse by topic:**
- **Entities**: `Entity`, `IWorldEntityApi`
- **Components**: `IWorldComponentApi`, `IWorldSingletonComponent`
- **Systems**: `ISystem`, `IWorldSystemsApi`
- **Messages**: `IMessage`, `IWorldMessagesApi`
- **Binding**: `IBinder<T>`, `ComponentDelta<T>`

## Related Documentation

- [Core Concepts](../docs/core/world.html) — Entities, components, systems, worlds
- [API Reference Guide](../docs/core/api-reference.html) — Detailed API documentation guide
- [Getting Started](../docs/getting-started/quickstart-basic.html) — Quick start tutorial

---

**Note:** This API documentation is auto-generated from XML comments in the source code. For conceptual documentation and tutorials, see the [Core Concepts](../docs/core/world.html) section.

