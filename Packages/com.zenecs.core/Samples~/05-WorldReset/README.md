# ZenECS Core — Sample 05: World Reset (Kernel)

A **console** sample demonstrating **`World.Reset(keepCapacity)`** behaviors:

* `Reset(keepCapacity: true)`  : **fast clear** — removes all entities/components but **preserves** internal arrays/pools
* `Reset(keepCapacity: false)` : **hard reset** — rebuilds internal structures from the **initial config**

* Component: `Health`
* Systems: None (direct demonstration in Main)
* Kernel loop:
    * `Kernel.CreateWorld()` creates world
    * `Kernel.PumpAndLateFrame()` integrates variable + fixed steps

---

## What this sample shows

1. **Fast reset (keep capacity)**
   Clear all data while preserving memory pools and internal arrays for quick reuse.

2. **Hard reset (reinitialize)**
   Rebuild internal storage to the initial configuration for a fully fresh world.

3. **Entity lifecycle after reset**
   Entities created before reset become invalid; new entities can be created after reset.

---

## TL;DR flow

```
Seed world (e1,e2 with Health)
→ Reset(keepCapacity:true)
→ Re-seed (e3 with Health)
→ Reset(keepCapacity:false)
```

---

## File layout

```
WorldReset.cs
```

Key excerpts:

```csharp
var kernel = new Kernel();
var world = kernel.CreateWorld(null);
kernel.SetCurrentWorld(world);

// Seed initial entities
Entity e1, e2;
using (var cmd = world.BeginWrite())
{
    e1 = cmd.CreateEntity();
    e2 = cmd.CreateEntity();
    cmd.AddComponent(e1, new Health(100));
    cmd.AddComponent(e2, new Health(50));
}
kernel.PumpAndLateFrame(0, 0, 1);
Console.WriteLine($"Before reset: alive={world.AliveCount}, e1.Has(Health)={world.HasComponent<Health>(e1)}");

// Option A: Keep capacity (fast clear). Preserves internal arrays/pools.
world.Reset(keepCapacity: true);
Console.WriteLine($"After Reset(keepCapacity:true): alive={world.AliveCount}");
// Note: e1 and e2 are now invalid after reset

// Re-seed to verify the world still works and reuses capacity
Entity e3;
using (var cmd = world.BeginWrite())
{
    e3 = cmd.CreateEntity();
    cmd.AddComponent(e3, new Health(77));
}
kernel.PumpAndLateFrame(0, 0, 1);
Console.WriteLine($"Re-seed: alive={world.AliveCount}, e3.Has(Health)={world.HasComponent<Health>(e3)}");

// Option B: Hard reset — rebuild internal structures from initial config
world.Reset(keepCapacity: false);
Console.WriteLine($"After Reset(keepCapacity:false): alive={world.AliveCount}");
// Note: e3 is now invalid after reset
```

---

## Build & Run

**Prereqs:** .NET 8 SDK and ZenECS Core assemblies referenced.

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project ZenEcsCoreSamples-05-WorldReset.csproj
```

Press **any key** to exit.

---

## Example output

```
=== ZenECS Core Sample - World Reset (Kernel) ===
=== World.Reset demo (keepCapacity vs hard reset) ===
Before reset: alive=2, e1.Has(Health)=True
After Reset(keepCapacity:true): alive=0
Re-seed: alive=1, e3.Has(Health)=True
After Reset(keepCapacity:false): alive=0
Running... press any key to exit.
Shutting down...
Done.
```

---

## APIs highlighted

* **World reset:** `world.Reset(bool keepCapacity)`
* **World ops:** `world.BeginWrite()`, `cmd.CreateEntity()`, `cmd.AddComponent()`, `world.HasComponent<T>()`, `world.AliveCount`
* **Kernel loop:** `Kernel.CreateWorld()`, `Kernel.PumpAndLateFrame()`, `Kernel.Dispose()`

---

## Notes & best practices

* Prefer **`Reset(true)`** for scene/level transitions to reuse memory and reduce GC churn.
* Use **`Reset(false)`** when you need a fully reinitialized world (e.g., config changes).
* Entities created before reset become **invalid** after reset — do not use them.
* Consider exposing reset options in your game's state manager (menus, editor tooling, etc.).
* Always use `CommandBuffer` (via `BeginWrite()`) for entity creation and component modifications.

---

## License

MIT © 2026 Pippapips Limited.
