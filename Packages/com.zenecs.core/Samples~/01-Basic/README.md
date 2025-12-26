# ZenECS Core — Sample 01: Basic (Kernel)

A **console** sample demonstrating the ZenECS **Kernel loop** with a minimal simulation and presentation setup.

* Minimal components: `Position`, `Velocity`
* Systems:
    * `MoveSystem : ISystem` — integrates `Position += Velocity * dt` (FixedGroup)
    * `PrintPositionsSystem : ISystem` — prints entity positions (read-only, FrameViewGroup)
* Kernel loop:
    * `Kernel.CreateWorld()` creates the world
    * `world.AddSystems()` registers systems
    * `Kernel.PumpAndLateFrame()` performs variable-step + fixed-step integration

---

## What this sample shows

1. **World creation and system registration**
   The ECS world is created via `Kernel.CreateWorld()` and systems are registered with `world.AddSystems()`.

2. **Simulation → Presentation flow**
   `MoveSystem` updates `Position` each tick (write phase in FixedGroup), and
   `PrintPositionsSystem` reads and prints entity positions in the Late phase (FrameViewGroup).

3. **Variable + fixed timestep integration**
   The frame loop combines variable delta and fixed simulation updates using `Kernel.PumpAndLateFrame()`, ensuring smooth deterministic results.

---

## TL;DR flow

```
[FixedGroup] MoveSystem
    → integrates Position += Velocity * dt

[FrameViewGroup] PrintPositionsSystem
    → reads Position and prints results

Kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame)
```

Simulation writes; Presentation reads.
Presentation always runs in **FrameViewGroup** and is **read-only**.

---

## File layout

```
Basic.cs
```

Key excerpts:

### Components

```csharp
public readonly struct Position
{
    public readonly float X, Y;
    public Position(float x, float y) { X = x; Y = y; }
    public override string ToString() => $"({X:0.###}, {Y:0.###})";
}

public readonly struct Velocity
{
    public readonly float X, Y;
    public Velocity(float x, float y) { X = x; Y = y; }
}
```

### Systems

```csharp
[FixedGroup]
public sealed class MoveSystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        using var cmd = w.BeginWrite();
        foreach (var (e, pos, vel) in w.Query<Position, Velocity>())
        {
            cmd.ReplaceComponent(e, new Position(pos.X + vel.X * dt, pos.Y + vel.Y * dt));
        }
    }
}

[FrameViewGroup]
public sealed class PrintPositionsSystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        foreach (var (e, pos) in w.Query<Position>())
        {
            Console.WriteLine($"Entity {e.Id,3}: pos={pos}");
        }
    }
}
```

### Frame driver

```csharp
var kernel = new Kernel(null, logger: new EcsLogger());
var world = kernel.CreateWorld(null);
kernel.SetCurrentWorld(world);

world.AddSystems([
    new MoveSystem(),
    new PrintPositionsSystem()
]);

const float fixedDelta = 1f / 60f; // 60Hz simulation
const int maxSubStepsPerFrame = 4;

while (true)
{
    float dt = CalculateDeltaTime();
    kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame);
}
```

---

## Build & Run

**Prereqs:** .NET 8 SDK and ZenECS Core assemblies referenced.

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project ZenEcsCoreSamples-01-Basic.csproj
```

Press **any key** to exit.

---

## Example output

```
=== ZenECS Core Sample - Basic (Kernel) ===
Hello World!
Running... press any key to exit.
Entity   1: pos=(0.017, 0)
Entity   2: pos=(2, 0.99)
Entity   1: pos=(0.033, 0)
Entity   2: pos=(2, 0.98)
...
Shutting down...
Done.
```

---

## APIs highlighted

* **Kernel & loop:** `Kernel.CreateWorld()`, `Kernel.SetCurrentWorld()`, `Kernel.PumpAndLateFrame()`, `Kernel.Dispose()`
* **World:** `world.BeginWrite()`, `cmd.CreateEntity()`, `cmd.AddComponent()`, `cmd.ReplaceComponent()`, `world.Query<T>()`
* **Systems:** `[FixedGroup]` for simulation writes, `[FrameViewGroup]` for read-only display
* **Timing:** Fixed timestep (`fixedDelta`) + variable delta for interpolation

---

## Notes & best practices

* Separate systems into **FixedGroup** (write) and **FrameViewGroup** (read-only) groups.
* Use **fixed timestep** for deterministic logic and physics.
* Keep presentation code stateless; it should reflect the data, not modify it.
* Add a small sleep (e.g., `Thread.Sleep(1)`) in console loops to reduce CPU load.
* Always use `CommandBuffer` (via `BeginWrite()`) for entity and component modifications.

---

## License

MIT © 2026 Pippapips Limited.
