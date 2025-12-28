# Performance

> Docs / Core / Performance

Performance considerations and optimization strategies for ZenECS.

## Overview

ZenECS is designed for good performance:

- **Cache-Friendly**: Components in contiguous arrays
- **Zero Allocation**: Queries use struct enumerators
- **Efficient Storage**: BitSet for entity tracking
- **Component Pooling**: Automatic pool management

## Optimization Strategies

### Query Optimization

**Cache queries** when possible:

```csharp
// Cache query result
private List<(Entity, Position, Velocity)> _entities = new();

public void Run(IWorld world, float deltaTime)
{
    _entities.Clear();
    foreach (var item in world.Query<Position, Velocity>())
    {
        _entities.Add(item);
    }
    // Process cached list
}
```

**Filter early** in pipeline:

```csharp
// ✅ Good: Filter early
foreach (var (entity, health) in world.Query<Health>()
    .Where(e => world.Get<Health>(e).Current > 0))
{
    // Only process alive entities
}
```

### Component Pool Optimization

**Pre-allocate pools**:

```csharp
var world = kernel.CreateWorld(new WorldConfig
{
    InitialPoolBuckets = new Dictionary<Type, int>
    {
        { typeof(Position), 1000 },
        { typeof(Velocity), 1000 }
    }
});
```

### System Order Optimization

**Optimize system order**:

```csharp
[FixedGroup]
[OrderBefore(typeof(ExpensiveSystem))]  // Run before expensive systems
public class FastSystem : ISystem { }
```

## Performance Characteristics

### Memory

- **Component Storage**: Type-segregated arrays
- **Entity Tracking**: BitSet (efficient)
- **Pool Management**: Automatic, lazy initialization

### CPU

- **Query Performance**: O(n) where n = entity count
- **Component Access**: O(1) by entity ID
- **System Execution**: Sequential, ordered

## Best Practices

### ✅ Do

- **Profile first**: Identify bottlenecks
- **Use appropriate data structures**: Choose components wisely
- **Minimize allocations**: Reuse collections
- **Test performance**: Measure before optimizing

### ❌ Don't

- **Don't over-optimize**: Premature optimization
- **Don't break determinism**: For performance
- **Don't ignore allocations**: Profile memory usage

## See Also

- [Advanced Topics](../guides/advanced-topics.md) - Advanced patterns
- [Architecture](../overview/architecture.md) - System design
- [World](./world.md) - World management
