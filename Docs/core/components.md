# Components

> Docs / Core / Components

**Components** are pure data structures (structs) that define properties. They have no methods or behavior—they're just data.

## Overview

Components in ZenECS:

- **Pure Data**: Structs with no methods
- **Value Types**: Stored as value types
- **Type-Segregated**: Each type in separate arrays
- **Immutable by Convention**: Readonly fields recommended

### Key Concepts

- **Struct Components**: Value types, no heap allocation
- **Type Segregation**: Components stored by type
- **Component Pools**: Automatic pool management
- **Immutability**: Encouraged for data integrity

## How It Works

### Component Definition

```csharp
public struct Position
{
    public float X, Y, Z;
    
    public Position(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
```

**Key Points:**
- Components are `struct` (value types)
- No methods or behavior
- Immutable by convention (readonly fields)

### Component Storage

Components are stored in type-specific pools:

```
World
├── ComponentPool<Position>
│   └── Position[] (indexed by Entity.Id)
├── ComponentPool<Velocity>
│   └── Velocity[] (indexed by Entity.Id)
└── ComponentPool<Health>
    └── Health[] (indexed by Entity.Id)
```

## API Surface

### Adding Components

```csharp
// Add component using command buffer
using (var cmd = world.BeginWrite())
{
    cmd.AddComponent(entity, new Position(0, 0, 0));
}
using (var cmd = world.BeginWrite())
{
    cmd.AddComponent(entity, new Position(0, 0, 0));
}
```

### Reading Components

```csharp
// Read by value
var pos = world.Get<Position>(entity);

// Read by reference (for modification)
ref var pos = ref world.Ref<Position>(entity);
pos.X = 10;  // Modify directly
```

### Modifying Components

```csharp
// Replace component
world.ReplaceComponent(entity, new Position(1, 1, 1));

// With command buffer (recommended)
using (var cmd = world.BeginWrite())
{
    cmd.ReplaceComponent(entity, new Position(1, 1, 1));
}
```

### Removing Components

```csharp
// Remove component
world.RemoveComponent<Position>(entity);

// With command buffer (recommended)
using (var cmd = world.BeginWrite())
{
    cmd.RemoveComponent<Position>(entity);
}
```

### Checking Components

```csharp
// Check if entity has component
bool hasPos = world.HasComponent<Position>(entity);

// Check multiple components
if (world.HasComponent<Position>(entity) && 
    world.HasComponent<Velocity>(entity))
{
    // Entity has both components
}
```

## Examples

### Basic Component Usage

```csharp
using ZenECS.Core;

var world = kernel.CreateWorld(null, "GameWorld");
Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position(0, 0, 0));
    cmd.AddComponent(entity, new Velocity(1, 0, 0));
    cmd.AddComponent(entity, new Health(100, 100));
}

// Read components
var pos = world.ReadComponent<Position>(entity);
var vel = world.ReadComponent<Velocity>(entity);
var health = world.ReadComponent<Health>(entity);

// Modify components
world.ReplaceComponent(entity, new Position(1, 0, 0));

// Remove component
world.RemoveComponent<Velocity>(entity);
```

### Component Patterns

#### Immutable Component

```csharp
public readonly struct Position
{
    public readonly float X, Y, Z;
    
    public Position(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
```

#### Mutable Component (by reference)

```csharp
public struct Health
{
    public float Current;
    public float Max;
    
    // Modify via reference
    ref var health = ref world.Ref<Health>(entity);
    health.Current -= 10;
}
```

## Best Practices

### ✅ Do

- **Use structs**: Components should be value types
- **Keep immutable**: Use readonly fields when possible
- **Use command buffers**: For structural changes
- **Type carefully**: Choose appropriate types

### ❌ Don't

- **Don't add methods**: Components are data only
- **Don't store references**: Use Entity IDs instead
- **Don't mutate in queries**: Use command buffers
- **Don't make too large**: Keep components focused

## Component Design

### Size Considerations

**Small Components** (recommended):
```csharp
public struct Position { public float X, Y, Z; }  // 12 bytes
```

**Medium Components** (acceptable):
```csharp
public struct Transform 
{ 
    public float X, Y, Z;
    public float RotX, RotY, RotZ;
    public float ScaleX, ScaleY, ScaleZ;
}  // 36 bytes
```

**Large Components** (avoid if possible):
```csharp
public struct LargeData
{
    public float[100] Values;  // 400 bytes - consider splitting
}
```

### Component Composition

Compose complex data from simple components:

```csharp
// ✅ Good: Separate components
public struct Position { public float X, Y, Z; }
public struct Rotation { public float X, Y, Z; }
public struct Scale { public float X, Y, Z; }

// ❌ Bad: One large component
public struct Transform
{
    public Position Pos;
    public Rotation Rot;
    public Scale Scale;
    // Too large, split into separate components
}
```

## See Also

- [Entities](./entities.md) - Entity management
- [World](./world.md) - World API
- [Query API](./world.md#query-api) - Finding entities
