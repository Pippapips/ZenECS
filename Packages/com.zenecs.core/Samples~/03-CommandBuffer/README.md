# ZenECS Core — Sample 03: CommandBuffer (Kernel)

A **console** sample demonstrating the ZenECS **CommandBuffer API**,
which enables thread-safe, batched component modifications — applied either **deferred** or **immediately**.

* Components: `Health`, `Stunned`
* Systems:
    * `CommandBufferDemoSystem : ISystem` — demonstrates deferred & immediate CommandBuffer use
    * `PrintStatusSystem : ISystem` — read-only view of current entity states (FrameViewGroup)
* Kernel loop:
    * `Kernel.CreateWorld()` bootstraps world
    * `world.AddSystems()` registers systems
    * `Kernel.PumpAndLateFrame()` performs variable + fixed-step updates

---

## What this sample shows

1. **Deferred execution (using BeginWrite)**
   Command operations (`AddComponent`, `ReplaceComponent`, `RemoveComponent`) are collected in a buffer via `BeginWrite()`,
   and applied when the buffer is disposed or when `RunScheduledJobs()` is called.

2. **Immediate execution**
   A write scope created with `BeginWrite()` applies changes when disposed (implicit barrier).

3. **Thread-safe batching**
   CommandBuffers allow multithreaded collection of component changes, safely synchronized at frame boundaries.

---

## TL;DR flow

```
using (var cmd = world.BeginWrite())
{
    cmd.AddComponent(e, new Health(100));
    cmd.ReplaceComponent(e, new Health(75));
    cmd.RemoveComponent<Stunned>(e);
} // Applied at disposal

world.RunScheduledJobs(); // Apply any scheduled jobs
```

* **Deferred** = batched safely, applied at disposal or via `RunScheduledJobs()`
* **Immediate** = applied instantly when buffer is disposed

---

## File layout

```
CommandBuffer.cs
```

Key excerpts:

### Components

```csharp
public readonly struct Health
{
    public readonly int Value;
    public Health(int value) => Value = value;
    public override string ToString() => Value.ToString();
}

public readonly struct Stunned
{
    public readonly float Seconds;
    public Stunned(float seconds) => Seconds = seconds;
    public override string ToString() => $"{Seconds:0.##}s";
}
```

### Systems

```csharp
[FixedGroup]
public sealed class CommandBufferDemoSystem : ISystem
{
    private bool _done;

    public void Run(IWorld w, float dt)
    {
        if (_done) return;

        using var cmd = w.BeginWrite();
        var e1 = cmd.CreateEntity();
        var e2 = cmd.CreateEntity();

        // Deferred apply
        using (var cb = w.BeginWrite())
        {
            cb.AddComponent(e1, new Health(100));
            cb.AddComponent(e2, new Health(80));
            cb.AddComponent(e2, new Stunned(1.5f));
            cb.ReplaceComponent(e2, new Health(75));
            cb.RemoveComponent<Stunned>(e2);
        } // Applied at disposal

        w.RunScheduledJobs();

        // Immediate apply
        using (var cb2 = w.BeginWrite())
        {
            cb2.ReplaceComponent(e1, new Health(42));
        } // Applied immediately

        _done = true;
    }
}

[FrameViewGroup]
public sealed class PrintStatusSystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        foreach (var (e, health) in w.Query<Health>())
        {
            var stunned = w.HasComponent<Stunned>(e) ? w.ReadComponent<Stunned>(e).ToString() : "no";
            Console.WriteLine($"Entity {e.Id,2}: Health={health.Value}, Stunned={stunned}");
        }
    }
}
```

### Frame driver

```csharp
const float fixedDelta = 1f / 60f; // 60Hz
const int maxSubStepsPerFrame = 4;

kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame);
```

---

## Build & Run

**Prereqs:** .NET 8 SDK, and ZenECS Core assemblies referenced.

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project ZenEcsCoreSamples-03-CommandBuffer.csproj
```

Press **any key** to exit.

---

## Example output

```
=== ZenECS Core Sample - CommandBuffer (Kernel) ===
=== CommandBuffer demo (deferred + immediate) ===
After apply (deferred): e1 Health=100, e2 Health=75, Has<Stunned>(e2)=False
After immediate EndWrite: e1 Health=42
Entity  1: Health=42, Stunned=no
Entity  2: Health=75, Stunned=no
Running... press any key to exit.
Shutting down...
Done.
```

---

## APIs highlighted

* **CommandBuffer API**
    * `world.BeginWrite()`, `cmd.AddComponent()`, `cmd.ReplaceComponent()`, `cmd.RemoveComponent()`
    * `world.RunScheduledJobs()` — apply scheduled jobs
* **World**
    * `cmd.CreateEntity()`, `world.HasComponent<T>()`, `world.ReadComponent<T>()`
* **Systems**
    * `[FixedGroup]` (writes)
    * `[FrameViewGroup]` (read-only)
* **Kernel loop**
    * `Kernel.CreateWorld()`, `Kernel.PumpAndLateFrame()`, `Kernel.Dispose()`

---

## Notes & best practices

* Use **CommandBuffer** (via `BeginWrite()`) for all entity and component modifications.
* CommandBuffers are automatically applied when disposed (using pattern).
* Use `RunScheduledJobs()` to explicitly flush scheduled operations.
* Presentation systems must remain **read-only**.
* CommandBuffers are automatically pooled; avoid long-term retention.

---

## License

MIT © 2026 Pippapips Limited.
