# Entities

> Docs / Core / Entities

**Entities** are unique identifiers that represent game objects. They have no data or behavior—they're just IDs that can have components attached.

## Overview

Entities in ZenECS:

- **Unique Identifiers**: Each entity has a unique ID
- **No Data**: Entities don't store data themselves
- **Component Containers**: Components are attached to entities
- **Value Types**: Entities are structs, not references

### Key Concepts

- **Entity ID**: Unique numeric identifier
- **Generation**: Prevents stale entity references
- **Lifecycle**: Create → Use → Destroy
- **Recycling**: Entity IDs are recycled

## How It Works

### Entity Structure

```csharp
public readonly struct Entity
{
    public int Id { get; }
    public int Generation { get; }
}
```

**Components:**
- `Id`: Unique identifier (recycled)
- `Generation`: Prevents stale references

### Entity Lifecycle

```
Create → Use → (Recycle) → Destroy
```

1. **Create**: `var entity = cmd.CreateEntity()` (using command buffer)
2. **Use**: Attach components, query in systems
3. **Destroy**: `cmd.DestroyEntity(entity)` (using command buffer)
4. **Recycle**: ID becomes available for reuse

## API Surface

### Creating Entities

```csharp
// Create entity using command buffer
Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();
}
```

### Checking Entity State

```csharp
// Check if entity is alive
bool isAlive = world.IsAlive(entity);

// Verify entity is valid
if (world.IsAlive(entity))
{
    // Entity is valid
}
```

### Destroying Entities

```csharp
// Destroy entity using command buffer
using (var cmd = world.BeginWrite())
{
    cmd.DestroyEntity(entity);
}
using (var cmd = world.BeginWrite())
{
    cmd.DestroyEntity(entity);
}
```

## Examples

### Basic Usage

```csharp
using ZenECS.Core;

var world = kernel.CreateWorld(null, "GameWorld");

// Create entity and add components using command buffer
Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position(0, 0));
    cmd.AddComponent(entity, new Velocity(1, 0));
}

// Check if alive
if (world.IsAlive(entity))
{
    var pos = world.ReadComponent<Position>(entity);
}

// Destroy entity using command buffer
using (var cmd = world.BeginWrite())
{
    cmd.DestroyEntity(entity);
}
```

### Entity Recycling

```csharp
// Create entity
Entity entity1;
using (var cmd = world.BeginWrite())
{
    entity1 = cmd.CreateEntity();  // ID: 0
    cmd.DestroyEntity(entity1);
}

// Create another (may reuse ID)
Entity entity2;
using (var cmd = world.BeginWrite())
{
    entity2 = cmd.CreateEntity();  // ID: 0 (recycled)
}

// Generation prevents confusion
if (entity1.Id == entity2.Id)
{
    // But generations differ, so entity1 is invalid
}
```

## Best Practices

### ✅ Do

- **Use command buffers** for entity creation/destruction
- **Check IsAlive** before accessing components
- **Store Entity values** (they're value types)
- **Reuse entities** when possible

### ❌ Don't

- **Don't store entity references** - Use EntityLink in Unity
- **Don't assume IDs are unique** - Use generation
- **Don't access destroyed entities** - Check IsAlive first
- **Don't create in queries** - Use command buffers

## Common Patterns

### Entity Creation Pattern

```csharp
using (var cmd = world.BeginWrite())
{
    var entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position(0, 0));
    cmd.AddComponent(entity, new Velocity(1, 0));
}
```

### Entity Validation Pattern

```csharp
if (world.IsAlive(entity))
{
    // Entity is valid, safe to use
    var component = world.Get<Component>(entity);
}
```

## See Also

- [World](./world.md) - World management
- [Components](./components.md) - Component system
- [Command Buffer](./command-buffer.md) - Structural changes
