# ZenECS Core — Sample 04: Snapshot I/O + Post Migration (Kernel)

A **console** sample demonstrating how to use the ZenECS **Snapshot I/O system**
to save and load world data, and how to apply a **Post-Load Migration** when component versions change.

* Components: `PositionV1`, `PositionV2`
* Systems:
    * `PrintSummarySystem : ISystem` — reads and prints final migrated entities (FrameViewGroup)
* Kernel loop:
    * `Kernel.CreateWorld()` initializes world
    * `world.AddSystems()` registers systems
    * `Kernel.PumpAndLateFrame()` performs simulation steps

---

## What this sample shows

1. **Saving & loading snapshots**
   Serializes world state into a binary stream using `world.SaveFullSnapshotBinary(stream)`
   and loads it into a fresh world with `world.LoadFullSnapshotBinary(stream)`.

2. **Versioned component migration (PostLoadMigration)**
   Demonstrates converting legacy component data (`PositionV1`)
   to a new version (`PositionV2`) after snapshot load.

3. **Post-migration verification**
   After migration, the world contains only `PositionV2` components,
   which are logged in the Presentation phase.

---

## TL;DR flow

```
Create entity with PositionV1
→ SaveFullSnapshotBinary(stream)
→ LoadFullSnapshotBinary(stream) into new world
→ Register PostLoadMigration
→ Run migration (PositionV1 → PositionV2)
→ Verify result (PositionV2 only)
```

---

## File layout

```
SnapshotIO_PostMig.cs
```

Key excerpts:

### Versioned Components

```csharp
public readonly struct PositionV1
{
    public readonly float X, Y;
    public PositionV1(float x, float y) { X = x; Y = y; }
}

public readonly struct PositionV2
{
    public readonly float X, Y;
    public readonly int Layer;
    public PositionV2(float x, float y, int layer = 0)
    {
        X = x; Y = y; Layer = layer;
    }
}
```

### Binary Formatters

```csharp
public sealed class PositionV1Formatter : BinaryComponentFormatter<PositionV1>
{
    public override void Write(in PositionV1 v, ISnapshotBackend b)
    {
        b.WriteFloat(v.X);
        b.WriteFloat(v.Y);
    }

    public override PositionV1 ReadTyped(ISnapshotBackend b)
        => new PositionV1(b.ReadFloat(), b.ReadFloat());
}

public sealed class PositionV2Formatter : BinaryComponentFormatter<PositionV2>
{
    public override void Write(in PositionV2 v, ISnapshotBackend b)
    {
        b.WriteFloat(v.X);
        b.WriteFloat(v.Y);
        b.WriteInt(v.Layer);
    }

    public override PositionV2 ReadTyped(ISnapshotBackend b)
        => new PositionV2(b.ReadFloat(), b.ReadFloat(), b.ReadInt());
}
```

### Migration logic

```csharp
public sealed class DemoPostLoadMigration : IPostLoadMigration
{
    public int Order => 0;

    public void Run(IWorld world)
    {
        using var cmd = world.BeginWrite();
        foreach (var (e, posV1) in world.Query<PositionV1>())
        {
            cmd.AddComponent(e, new PositionV2(posV1.X, posV1.Y, layer: 1));
            cmd.RemoveComponent<PositionV1>(e);
        }
    }
}
```

### Systems

```csharp
[FrameViewGroup]
public sealed class PrintSummarySystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        foreach (var (e, posV1) in w.Query<PositionV1>())
        {
            Console.WriteLine($"Entity {e.Id}: PositionV1={posV1}");
        }

        foreach (var (e, posV2) in w.Query<PositionV2>())
        {
            Console.WriteLine($"Entity {e.Id}: PositionV2={posV2}");
        }
    }
}
```

### Usage

```csharp
// Register StableIds & formatters
ComponentRegistry.Register("com.zenecs.samples.position.v1", typeof(PositionV1));
ComponentRegistry.Register("com.zenecs.samples.position.v2", typeof(PositionV2));
ComponentRegistry.RegisterFormatter(new PositionV1Formatter(), "com.zenecs.samples.position.v1");
ComponentRegistry.RegisterFormatter(new PositionV2Formatter(), "com.zenecs.samples.position.v2");

// Create V1 data
using (var cmd = world.BeginWrite())
{
    var e = cmd.CreateEntity();
    cmd.AddComponent(e, new PositionV1(3, 7));
}

// Save snapshot
using var ms = new MemoryStream();
world.SaveFullSnapshotBinary(ms);

// Load snapshot into new world
var world2 = kernel.CreateWorld(new WorldConfig(initialEntityCapacity: 8));
PostLoadMigrationRegistry.Register(new DemoPostLoadMigration());

ms.Position = 0;
world2.LoadFullSnapshotBinary(ms);
kernel.PumpAndLateFrame(0, 0, 1); // Flush migration changes
```

---

## Build & Run

**Prereqs:** .NET 8 SDK, and ZenECS Core assemblies referenced.

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project ZenEcsCoreSamples-04-SnapshotIO-PostMig.csproj
```

Press **any key** to exit.

---

## Example output

```
=== ZenECS Core Sample - SnapshotIO + PostMigration (Kernel) ===
Saved snapshot bytes: 56
Migrated entity 1 → (3, 7, layer:1)
Entity 1: PositionV2=(3, 7, layer:1)
Running... press any key to exit.
Shutting down...
Done.
```

---

## APIs highlighted

* **Serialization**
    * `world.SaveFullSnapshotBinary(Stream)`
    * `world.LoadFullSnapshotBinary(Stream)`
* **Migration**
    * `IPostLoadMigration`
    * `PostLoadMigrationRegistry.Register()`
    * `world.BeginWrite()`, `cmd.AddComponent()`, `cmd.RemoveComponent()`
* **World & Registry**
    * `ComponentRegistry.Register()`, `ComponentRegistry.RegisterFormatter()`
* **Kernel**
    * `Kernel.CreateWorld()`, `Kernel.PumpAndLateFrame()`, `Kernel.Dispose()`

---

## Notes & best practices

* Maintain **StableId strings** for every component version.
  (`com.zenecs.samples.position.v1`, `...v2`, etc.)
* Register all formatters **before snapshot I/O**.
* Always perform **migration** after load to reconcile old formats.
* Keep migration idempotent — running it twice shouldn't corrupt data.
* Presentation systems remain **read-only** and display results only.
* Call `PumpAndLateFrame()` after migration to flush CommandBuffer changes.

---

## License

MIT © 2026 Pippapips Limited.
