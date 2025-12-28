# ZenECS Core — Sample 07: Query Filtering

A **console** sample demonstrating advanced query filtering with `Filter.New.With<T>().Without<T>()` patterns.

* Components: `Position`, `Velocity`, `Health`, `Paused`, `Enemy`, `Player`
* Systems:
    * `MoveSystem : ISystem` — moves only non-paused entities (FixedGroup)
    * `DamageEnemySystem : ISystem` — damages enemies using WithAny filter (FixedGroup)
    * `PrintFilteredSystem : ISystem` — prints filtered results (FrameViewGroup)
* Kernel loop:
    * `Kernel.PumpAndLateFrame()` — steps simulation and presentation

---

## What this sample shows

1. **Basic filtering (With/Without)**
   Filter entities that have specific components (`With<T>`) and exclude others (`Without<T>`).

2. **OR-group filtering (WithAny)**
   Use `WithAny` to match entities that have at least one of the specified component types.

3. **Complex filter combinations**
   Combine multiple constraints to create precise queries.

4. **Performance considerations**
   Filters are cached and optimized for efficient iteration.

---

## TL;DR flow

```
var filter = Filter.New
    .With<Position>()
    .With<Velocity>()
    .Without<Paused>()
    .Build();

foreach (var (e, pos, vel) in world.Query<Position, Velocity>(filter))
{
    // Process only non-paused moving entities
}
```

---

## File layout

```
QueryFiltering.cs
```

Key excerpts:

### Basic Filter

```csharp
[FixedGroup]
public sealed class MoveSystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        var filter = Filter.New
            .With<Position>()
            .With<Velocity>()
            .Without<Paused>()
            .Build();

        using var cmd = w.BeginWrite();
        foreach (var (e, pos, vel) in w.Query<Position, Velocity>(filter))
        {
            cmd.ReplaceComponent(e, new Position(pos.X + vel.X * dt, pos.Y + vel.Y * dt));
        }
    }
}
```

### WithAny Filter (OR-group)

```csharp
[FixedGroup]
public sealed class DamageEnemySystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        var filter = Filter.New
            .With<Health>()
            .WithAny(typeof(Enemy))
            .Without<Player>()
            .Build();

        using var cmd = w.BeginWrite();
        foreach (var (e, health) in w.Query<Health>(filter))
        {
            var updated = new Health(health.Value - 1);
            cmd.ReplaceComponent(e, updated);
        }
    }
}
```

### CommandBuffer Usage

```csharp
using (var cmd = world.BeginWrite())
{
    var e = cmd.CreateEntity();
    cmd.AddComponent(e, new Position(0, 0));
    cmd.AddComponent(e, new Velocity(1, 0));
    cmd.AddComponent(e, new Enemy());
}
```

---

## Build & Run

**Prereqs:** .NET 8 SDK and ZenECS Core assemblies referenced.

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project ZenEcsCoreSamples-07-QueryFiltering.csproj
```

Press **any key** to exit.

---

## Example output

```
=== ZenECS Core Sample - Query Filtering (Kernel) ===

=== Frame 1 ===
Moving entities (Position + Velocity, not Paused):
  Entity   1: pos=(0.02, 0)
  Entity   2: pos=(5, 4.99)
  Entity   4: pos=(14.99, 15.01)

Entities with Health:
  Entity   1: HP=100 [Enemy]
  Entity   2: HP=150 [Player]
  Entity   3: HP=80 [Enemy]

Paused entities: 1

[Damage] Entity 1: 100 → 99
[Damage] Entity 3: 80 → 79
...
```

---

## APIs highlighted

* **Query Filtering:**
    * `Filter.New.With<T>()` — require component
    * `Filter.New.Without<T>()` — exclude component
    * `Filter.New.WithAny(types)` — OR-group (any one of)
    * `Filter.New.WithoutAny(types)` — NOT-OR-group
    * `Filter.Build()` — finalize filter
* **Query:**
    * `world.Query<T1, T2>(filter)` — query with filter
* **CommandBuffer:**
    * `world.BeginWrite()`, `cmd.CreateEntity()`, `cmd.AddComponent()`
* **Systems:**
    * `[FixedGroup]` (simulation writes)
    * `[FrameViewGroup]` (read-only presentation)

---

## Notes & best practices

* Use **filters** to efficiently query specific entity subsets without iterating all entities.
* **With/Without** are AND conditions — all `With` must be present, all `Without` must be absent.
* **WithAny/WithoutAny** are OR conditions — at least one type in the group must match.
* Filters are **immutable** and can be cached/reused across frames.
* Combine filters for complex queries (e.g., "moving enemies that are not paused").
* Filters are optimized internally — use them liberally for better performance.

---

## License

MIT © 2026 Pippapips Limited.
