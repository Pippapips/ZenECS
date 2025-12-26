# Snapshot I/O

> Docs / Core / Snapshot I/O

Save and load world state with pluggable serialization backends.

## Overview

Snapshots enable:

- **Save/Load**: Persist world state to disk or memory
- **Checkpointing**: Create restore points
- **Replay**: Record and replay simulations
- **Migration**: Version compatibility with post-load migrations

## How It Works

### Save Process

```
World State
    ↓
Serialize Components
    ↓
Write to Stream
    ↓
Snapshot File
```

### Load Process

```
Snapshot File
    ↓
Read from Stream
    ↓
Deserialize Components
    ↓
Apply Migrations
    ↓
World State Restored
```

## API Surface

### Saving Snapshots

```csharp
// Save to file
using var stream = File.Create("save.dat");
world.Save(stream);

// Save with custom formatter
world.Save(stream, new JsonComponentFormatter());
```

### Loading Snapshots

```csharp
// Load from file
using var stream = File.OpenRead("save.dat");
world.Load(stream);

// Load with migrations
world.Load(stream, migrations: myMigrations);
```

## Examples

### Basic Save/Load

```csharp
using ZenECS.Core;

var world = kernel.CreateWorld(null, "GameWorld");

// Create entities using command buffer
Entity player;
using (var cmd = world.BeginWrite())
{
    player = cmd.CreateEntity();
    cmd.AddComponent(player, new Position(0, 0));
    cmd.AddComponent(player, new Health(100, 100));
}

// Save
using (var stream = File.Create("save.dat"))
{
    world.Save(stream);
}

// Load
using (var stream = File.OpenRead("save.dat"))
{
    world.Load(stream);
}
```

## See Also

- [Migration & PostMig](./migration-postmig.md) - Version migrations
- [World](./world.md) - World management
- [Advanced Topics](../guides/advanced-topics.md) - Advanced patterns
