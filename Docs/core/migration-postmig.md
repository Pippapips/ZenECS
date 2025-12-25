# Migration & PostMig

> Docs / Core / Migration & PostMig

Version compatibility system for snapshots with post-load migrations.

## Overview

Migrations enable:

- **Version Compatibility**: Load old snapshot formats
- **Data Transformation**: Convert between component versions
- **Post-Load Processing**: Apply migrations after loading
- **Stable IDs**: Maintain entity relationships

## How It Works

### Migration Process

```
Load Snapshot
    ↓
Detect Version
    ↓
Apply Migrations
    ↓
Transform Components
    ↓
World State Restored
```

### Migration Registration

```csharp
// Register migration
world.RegisterMigration<OldComponent, NewComponent>((old) => new NewComponent
{
    // Convert old to new
    Field = old.OldField,
    NewField = CalculateNewField(old)
});
```

## Examples

### Component Migration

```csharp
// Old component
public struct OldHealth
{
    public int CurrentHP;
    public int MaxHP;
}

// New component
public struct Health
{
    public float Current;
    public float Max;
}

// Register migration
world.RegisterMigration<OldHealth, Health>((old) => new Health
{
    Current = old.CurrentHP,
    Max = old.MaxHP
});

// Load snapshot (migration applied automatically)
world.Load(stream);
```

## See Also

- [Snapshot I/O](./snapshot-io.md) - Save/load system
- [Advanced Topics](../guides/advanced-topics.md) - Advanced patterns
- [Surrogate Components](../guides/surrogate-components.md) - Component conversion
