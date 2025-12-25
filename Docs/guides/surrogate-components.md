# Surrogate Components

> Docs / Guides / Surrogate components

Use surrogate components to bridge between different component representations or handle legacy data.

## Overview

**Surrogate components** are temporary components used to:

- **Convert data**: Transform between component formats
- **Handle migrations**: Support legacy component structures
- **Bridge systems**: Connect incompatible component types
- **Temporary state**: Store intermediate computation results

## Use Cases

### Data Conversion

Convert between component formats:

```csharp
// Old component format
public struct OldPosition
{
    public float X, Y;
}

// New component format
public struct Position
{
    public float X, Y, Z;
}

// Surrogate for conversion
public struct PositionSurrogate
{
    public float X, Y, Z;
}

// Conversion system
[FixedGroup]
public class ConversionSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        using var cmd = world.BeginWrite();
        
        foreach (var (entity, oldPos) in world.Query<OldPosition>())
        {
            // Convert to new format
            cmd.AddComponent(entity, new Position
            {
                X = oldPos.X,
                Y = oldPos.Y,
                Z = 0
            });
            
            // Remove old component
            cmd.RemoveComponent<OldPosition>(entity);
        }
    }
}
```

### Legacy Support

Support legacy component structures:

```csharp
// Legacy component
public struct LegacyHealth
{
    public int CurrentHP;
    public int MaxHP;
}

// Modern component
public struct Health
{
    public float Current;
    public float Max;
}

// Surrogate for compatibility
public struct HealthSurrogate
{
    public float Current;
    public float Max;
    
    public static HealthSurrogate FromLegacy(LegacyHealth legacy)
    {
        return new HealthSurrogate
        {
            Current = legacy.CurrentHP,
            Max = legacy.MaxHP
        };
    }
}
```

## Patterns

### Migration Pattern

Use surrogates during migration:

```csharp
[FixedGroup]
public class MigrationSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        using var cmd = world.BeginWrite();
        
        // Find entities with old components
        foreach (var (entity, oldComponent) in world.Query<OldComponent>())
        {
            // Create surrogate
            var surrogate = ConvertToSurrogate(oldComponent);
            cmd.AddComponent(entity, surrogate);
            
            // Remove old component
            cmd.RemoveComponent<OldComponent>(entity);
        }
        
        // Convert surrogates to new format
        foreach (var (entity, surrogate) in world.Query<SurrogateComponent>())
        {
            var newComponent = ConvertFromSurrogate(surrogate);
            cmd.AddComponent(entity, newComponent);
            cmd.RemoveComponent<SurrogateComponent>(entity);
        }
    }
}
```

### Temporary State Pattern

Use surrogates for temporary computation:

```csharp
// Surrogate for intermediate calculation
public struct VelocitySurrogate
{
    public float X, Y;
}

[FixedGroup]
public class PhysicsSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        using var cmd = world.BeginWrite();
        
        // Calculate velocity in surrogate
        foreach (var (entity, pos, force) in world.Query<Position, Force>())
        {
            var velocity = CalculateVelocity(force);
            cmd.AddOrReplaceComponent(entity, new VelocitySurrogate
            {
                X = velocity.X,
                Y = velocity.Y
            });
        }
        
        // Apply surrogate to actual component
        foreach (var (entity, surrogate) in world.Query<VelocitySurrogate>())
        {
            cmd.AddOrReplaceComponent(entity, new Velocity
            {
                X = surrogate.X,
                Y = surrogate.Y
            });
            cmd.RemoveComponent<VelocitySurrogate>(entity);
        }
    }
}
```

## Best Practices

### ✅ Do

- **Use for migration**: Temporary during transitions
- **Clear after use**: Remove surrogates when done
- **Document purpose**: Explain why surrogates exist
- **Keep minimal**: Only when necessary

### ❌ Don't

- **Don't keep long-term**: Remove after migration
- **Don't overuse**: Prefer direct components
- **Don't break contracts**: Maintain component contracts

## See Also

- [Migration & PostMig](../core/migration-postmig.md) - Version migration
- [Components](../core/components.md) - Component system
- [Advanced Topics](./advanced-topics.md) - Advanced patterns
