# ZenECS Core — Sample 11: System Ordering

A **console** sample demonstrating system execution ordering with `[OrderBefore]` and `[OrderAfter]` attributes.

* Components: `Position`, `Velocity`, `Acceleration`
* Systems:
    * `UpdateVelocitySystem : ISystem` — updates velocity from acceleration (runs first)
    * `MoveSystem : ISystem` — moves position using velocity (runs second)
    * `DampingSystem : ISystem` — applies damping to velocity (runs third)
    * `PrintOrderSystem : ISystem` — prints execution order (FrameViewGroup)
* Kernel loop:
    * `Kernel.PumpAndLateFrame()` — steps simulation and presentation

---

## What this sample shows

1. **OrderBefore attribute**
   Declare that this system must run before another system within the same group.

2. **OrderAfter attribute**
   Declare that this system must run after another system within the same group.

3. **System group ordering**
   Ordering attributes only affect systems within the same `SystemGroup` (e.g., FixedGroup).

4. **Deterministic execution**
   The system planner resolves ordering constraints to ensure deterministic execution.

---

## TL;DR flow

```
[FixedGroup]
[OrderBefore(typeof(MoveSystem))]
public sealed class UpdateVelocitySystem : ISystem { ... }

[FixedGroup]
[OrderAfter(typeof(UpdateVelocitySystem))]
public sealed class MoveSystem : ISystem { ... }

[FixedGroup]
[OrderAfter(typeof(MoveSystem))]
public sealed class DampingSystem : ISystem { ... }
```

Execution order: UpdateVelocitySystem → MoveSystem → DampingSystem

---

## File layout

```
SystemOrdering.cs
```

Key excerpts:

### OrderBefore

```csharp
[FixedGroup]
[OrderBefore(typeof(MoveSystem))]
public sealed class UpdateVelocitySystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        using var cmd = w.BeginWrite();
        foreach (var (e, vel, acc) in w.Query<Velocity, Acceleration>())
        {
            cmd.ReplaceComponent(e, new Velocity(vel.X + acc.X * dt, vel.Y + acc.Y * dt));
        }
        Console.WriteLine($"[System] UpdateVelocitySystem ran (dt={dt:0.000})");
    }
}
```

### OrderAfter

```csharp
[FixedGroup]
[OrderAfter(typeof(UpdateVelocitySystem))]
public sealed class MoveSystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        using var cmd = w.BeginWrite();
        foreach (var (e, pos, vel) in w.Query<Position, Velocity>())
        {
            cmd.ReplaceComponent(e, new Position(pos.X + vel.X * dt, pos.Y + vel.Y * dt));
        }
        Console.WriteLine($"[System] MoveSystem ran (dt={dt:0.000})");
    }
}
```

### Multiple Constraints

```csharp
[FixedGroup]
[OrderAfter(typeof(UpdateVelocitySystem))]
[OrderBefore(typeof(DampingSystem))]
public sealed class MoveSystem : ISystem { ... }
```

---

## Build & Run

**Prereqs:** .NET 8 SDK and ZenECS Core assemblies referenced.

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project ZenEcsCoreSamples-11-SystemOrdering.csproj
```

Press **any key** to exit.

---

## Example output

```
=== ZenECS Core Sample - System Ordering (Kernel) ===
System execution order (within FixedGroup):
  1. UpdateVelocitySystem
  2. MoveSystem
  3. DampingSystem

Running... press any key to exit.
[System] UpdateVelocitySystem ran (dt=0.017)
[System] MoveSystem ran (dt=0.017)
[System] DampingSystem ran (dt=0.017)

=== Frame 60 ===
Entity states:
  Entity   1: pos=(1.02, 0), vel=(1.01, 0), acc=(0.10, 0)
  Entity   2: pos=(5, 4.98), vel=(0, -0.48), acc=(0, 0.05)
...
```

---

## APIs highlighted

* **System Ordering:**
    * `[OrderBefore(typeof(TargetSystem))]` — run before target
    * `[OrderAfter(typeof(TargetSystem))]` — run after target
    * Multiple attributes can be applied to a single system
* **System Groups:**
    * `[FixedGroup]` — fixed timestep simulation
    * `[FrameViewGroup]` — variable timestep presentation
* **CommandBuffer:**
    * `World.BeginWrite()`, `cmd.CreateEntity()`, `cmd.AddComponent()`

---

## Notes & best practices

* **Ordering is within groups** — `OrderBefore/After` only affects systems in the same `SystemGroup`.
* **Cross-group ordering** is handled by the runner's pipeline (FixedInput → FixedDecision → FixedSimulation → FixedPost).
* **Multiple constraints** can be applied — the planner resolves them using topological sorting.
* **Circular dependencies** will cause an exception — avoid cycles in ordering constraints.
* **Registration order doesn't matter** — attributes control execution, not registration order.
* Use ordering for **data dependencies** (e.g., velocity must be updated before position).

---

## License

MIT © 2026 Pippapips Limited.
