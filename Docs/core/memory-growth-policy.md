# Memory Growth Policy

> Docs / Core / Memory growth policy

How arrays and pools expand when capacity is exceeded.

## Overview

ZenECS uses configurable growth policies to manage memory allocation for entities, component pools, and internal data structures. The growth policy determines how arrays expand when their capacity is exceeded.

**Key Concepts:**

- **Growth Policy**: Strategy for expanding capacity (Doubling vs Step)
- **Initial Capacity**: Starting size of arrays and pools
- **Growth Step**: Fixed increment for Step policy
- **Automatic Growth**: Arrays expand automatically when needed

## Growth Policies

### `GrowthPolicy.Doubling` (Default)

Doubles capacity on each expansion, with a minimum step of 256.

**Characteristics:**
- **Pros**: Fewer resize operations, better for high-growth scenarios
- **Cons**: Larger memory jumps, may waste memory
- **Best For**: Dynamic entity counts, unpredictable growth

**Example:**
```
Initial: 256
After 1st expansion: 512 (256 * 2)
After 2nd expansion: 1024 (512 * 2)
After 3rd expansion: 2048 (1024 * 2)
```

**Minimum Step Guarantee:**
- Always grows by at least 256 slots
- Prevents tiny increments for large arrays

### `GrowthPolicy.Step`

Expands capacity by a fixed number of slots (`GrowthStep`) on each expansion.

**Characteristics:**
- **Pros**: Predictable memory growth, better memory efficiency
- **Cons**: More frequent resize operations
- **Best For**: Known entity counts, memory-constrained environments

**Example (GrowthStep = 256):**
```
Initial: 256
After 1st expansion: 512 (256 + 256)
After 2nd expansion: 768 (512 + 256)
After 3rd expansion: 1024 (768 + 256)
```

## Configuration

### World Configuration

Configure growth policy when creating a world:

```csharp
var config = new WorldConfig(
    initialEntityCapacity: 512,
    growthPolicy: GrowthPolicy.Doubling,
    growthStep: 256
);

var world = kernel.CreateWorld(config, "MyWorld");
```

### Configuration Parameters

#### `InitialEntityCapacity`

Initial number of entity slots (sizes `Alive`/`Generation` arrays).

- **Default**: 256
- **Minimum**: 16 (clamped automatically)
- **Impact**: Larger values reduce early resizes but use more memory

```csharp
var config = new WorldConfig(
    initialEntityCapacity: 1024  // Start with 1024 entity slots
);
```

#### `InitialPoolBuckets`

Initial bucket count for component pool dictionary (hash table).

- **Default**: 256
- **Minimum**: 16
- **Impact**: Higher values reduce hash collisions and rehash frequency

```csharp
var config = new WorldConfig(
    initialPoolBuckets: 512  // More buckets for better hash performance
);
```

#### `GrowthPolicy`

Expansion strategy: `Doubling` or `Step`.

- **Default**: `GrowthPolicy.Doubling`
- **Impact**: Affects resize frequency and memory usage patterns

```csharp
var config = new WorldConfig(
    growthPolicy: GrowthPolicy.Step  // Use step-based growth
);
```

#### `GrowthStep`

Number of slots added per expansion when using `Step` policy.

- **Default**: 256
- **Minimum**: 32 (clamped automatically)
- **Impact**: Smaller steps = more frequent resizes, better memory efficiency

```csharp
var config = new WorldConfig(
    growthPolicy: GrowthPolicy.Step,
    growthStep: 128  // Grow by 128 slots at a time
);
```

## How It Works

### Entity Storage Growth

Entity-related arrays (`_alive`, `_generation`) grow when new entities exceed current capacity:

```csharp
// Current capacity: 256
// New entity ID: 300
// → Triggers expansion to 512 (Doubling) or 512 (Step with step=256)
```

**Growth Algorithm (Doubling):**
```csharp
int cap = Math.Max(16, current);
while (cap < required)
{
    int next = cap * 2;
    if (next - cap < 256) next = cap + 256;  // Minimum step
    cap = next;
}
```

**Growth Algorithm (Step):**
```csharp
int step = growthStep;
int blocks = (required + step - 1) / step;
return Math.Max(required, blocks * step);
```

### Component Pool Growth

Component pools grow independently using power-of-two expansion:

```csharp
// Component pool for Position
// Current capacity: 128
// New entity ID: 200
// → Expands to 256 (next power of 2)
```

**Component Pool Growth:**
- Always uses power-of-two expansion (independent of `GrowthPolicy`)
- Ensures O(1) entity ID indexing
- Grows automatically via `EnsureCapacity()`

## Examples

### Example 1: High-Entity-Count Game

For games with many entities (10,000+):

```csharp
var config = new WorldConfig(
    initialEntityCapacity: 2048,      // Start large
    growthPolicy: GrowthPolicy.Doubling,  // Fewer resizes
    initialPoolBuckets: 512           // More hash buckets
);

var world = kernel.CreateWorld(config, "GameWorld");
```

**Benefits:**
- Fewer resize operations
- Better performance for large entity counts
- Acceptable memory overhead

### Example 2: Memory-Constrained Environment

For memory-constrained environments:

```csharp
var config = new WorldConfig(
    initialEntityCapacity: 128,        // Start small
    growthPolicy: GrowthPolicy.Step,  // Predictable growth
    growthStep: 64                     // Small increments
);

var world = kernel.CreateWorld(config, "MobileWorld");
```

**Benefits:**
- Predictable memory usage
- Better memory efficiency
- Smaller memory footprint

### Example 3: Known Entity Count

When entity count is known in advance:

```csharp
int expectedEntityCount = 5000;

var config = new WorldConfig(
    initialEntityCapacity: expectedEntityCount,  // Pre-allocate
    growthPolicy: GrowthPolicy.Step,            // Minimal growth
    growthStep: 256
);

var world = kernel.CreateWorld(config, "PreAllocatedWorld");
```

**Benefits:**
- Minimal resizing
- Optimal memory usage
- Better performance

## Performance Considerations

### Resize Frequency

**Doubling Policy:**
- Fewer resize operations
- O(log n) resizes for n entities
- Better for dynamic growth

**Step Policy:**
- More resize operations
- O(n/step) resizes for n entities
- Better for predictable growth

### Memory Overhead

**Doubling Policy:**
- May allocate up to 2x required capacity
- Average overhead: ~33% (geometric series)
- Acceptable for most scenarios

**Step Policy:**
- Allocates exactly required + step
- Average overhead: ~step/2
- Better memory efficiency

### Component Pool Growth

Component pools always use power-of-two growth:

- **Pros**: O(1) entity ID indexing, efficient memory layout
- **Cons**: May waste up to 50% capacity
- **Impact**: Minimal for typical use cases

## Best Practices

### 1. Choose Appropriate Initial Capacity

```csharp
// ✅ Good: Match expected entity count
var config = new WorldConfig(
    initialEntityCapacity: expectedEntityCount
);

// ❌ Bad: Too small (many resizes)
var config = new WorldConfig(
    initialEntityCapacity: 16  // Will resize frequently
);
```

### 2. Use Doubling for Dynamic Growth

```csharp
// ✅ Good: Dynamic entity creation
var config = new WorldConfig(
    growthPolicy: GrowthPolicy.Doubling
);
```

### 3. Use Step for Predictable Growth

```csharp
// ✅ Good: Known entity count range
var config = new WorldConfig(
    growthPolicy: GrowthPolicy.Step,
    growthStep: 256
);
```

### 4. Monitor Memory Usage

```csharp
// Check actual capacity vs used
var world = kernel.CreateWorld(config, "World");
// After creating entities...
// world.AliveCount vs world capacity
```

### 5. Pre-allocate for Known Counts

```csharp
// ✅ Good: Pre-allocate for known count
int maxEntities = 10000;
var config = new WorldConfig(
    initialEntityCapacity: maxEntities
);
```

## Memory Usage Estimation

### Entity Storage

**Per Entity:**
- Alive bit: 1 bit (in BitSet)
- Generation: 4 bytes (int)
- **Total**: ~4 bytes per entity slot

**Example (10,000 entities):**
- Capacity: 16,384 (next power of 2)
- Memory: ~64 KB (generation array) + ~2 KB (bitset)
- **Total**: ~66 KB

### Component Pools

**Per Component Type:**
- Component array: `sizeof(T) * capacity`
- Presence bitset: `capacity / 8` bytes

**Example (Position, 10,000 entities):**
- Position size: 12 bytes (3 floats)
- Capacity: 16,384
- Memory: 196 KB (data) + 2 KB (bitset)
- **Total**: ~198 KB per component type

## Troubleshooting

### High Memory Usage

**Symptoms:**
- Memory usage higher than expected
- Frequent GC collections

**Solutions:**
1. Use `Step` policy with smaller step size
2. Reduce initial capacity
3. Monitor actual vs allocated capacity
4. Use world reset to reclaim memory

### Frequent Resizes

**Symptoms:**
- Performance spikes during entity creation
- High allocation rate

**Solutions:**
1. Increase initial capacity
2. Use `Doubling` policy
3. Pre-allocate for known entity counts
4. Batch entity creation

### Memory Fragmentation

**Symptoms:**
- High memory usage despite low entity count
- GC pressure

**Solutions:**
1. Use world reset to compact memory
2. Reduce initial capacity
3. Use `Step` policy for predictable growth
4. Monitor and adjust configuration

## Configuration Examples

### Small Game (100-1000 entities)

```csharp
var config = new WorldConfig(
    initialEntityCapacity: 256,
    growthPolicy: GrowthPolicy.Doubling,
    growthStep: 128
);
```

### Medium Game (1000-10000 entities)

```csharp
var config = new WorldConfig(
    initialEntityCapacity: 1024,
    growthPolicy: GrowthPolicy.Doubling,
    growthStep: 256
);
```

### Large Game (10000+ entities)

```csharp
var config = new WorldConfig(
    initialEntityCapacity: 4096,
    growthPolicy: GrowthPolicy.Doubling,
    growthStep: 512
);
```

### Memory-Constrained (Mobile)

```csharp
var config = new WorldConfig(
    initialEntityCapacity: 128,
    growthPolicy: GrowthPolicy.Step,
    growthStep: 64
);
```

## FAQ

### How do I reduce memory usage?

1. Use `Step` policy with smaller step size
2. Reduce initial capacity
3. Use world reset periodically
4. Monitor and adjust based on actual usage

### How do I improve performance?

1. Increase initial capacity to reduce resizes
2. Use `Doubling` policy for dynamic growth
3. Pre-allocate for known entity counts
4. Monitor resize frequency

### Can I change growth policy at runtime?

No, growth policy is set at world creation and cannot be changed. Create a new world with different configuration if needed.

### How do I estimate memory usage?

```csharp
// Entity storage
int entitySlots = world.Capacity;  // Actual capacity
int entityMemory = entitySlots * 4;  // ~4 bytes per slot

// Component pools (per type)
int componentMemory = componentCount * sizeof(ComponentType);
```

### What happens when capacity is exceeded?

Arrays automatically expand according to the growth policy:
- **Doubling**: Capacity doubles (minimum +256)
- **Step**: Capacity increases by `GrowthStep`

## See Also

- [World Configuration](./world.md) - World setup
- [Performance](./performance.md) - Performance optimization
- [World Reset](./world-reset.md) - Memory reclamation
