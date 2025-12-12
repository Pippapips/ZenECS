# ZenECS Core — Sample 10: System Runner (Manual Loop)

A **console** sample demonstrating manual frame loop control using `Kernel.PumpAndLateFrame`.

* Components: `Position`, `Velocity`
* Systems:
    * `MoveSystem : ISystem` — moves entities (FixedGroup)
    * `PrintPositionsSystem : ISystem` — prints positions (FrameViewGroup)
* Kernel loop:
    * Manual frame loop with `Kernel.PumpAndLateFrame()`
    * Fixed timestep accumulation and sub-stepping
    * Alpha interpolation for presentation

---

## What this sample shows

1. **Manual frame loop**
   Control the game loop manually in a console application using `Kernel.PumpAndLateFrame()`.

2. **Fixed timestep accumulation**
   The kernel accumulates variable frame time into fixed timestep sub-steps for deterministic simulation.

3. **Alpha interpolation**
   The kernel calculates an interpolation factor (alpha) for smooth presentation between fixed steps.

4. **Console app pattern**
   Demonstrates the typical pattern for console applications that need direct loop control.

---

## TL;DR flow

```
while (running)
{
    float dt = CalculateDeltaTime();
    
    // Internally performs:
    // 1. BeginFrame(dt) - variable timestep
    // 2. FixedStep(fixedDelta) × N - fixed timestep (accumulated)
    // 3. LateFrame(alpha) - presentation (read-only)
    kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame);
}
```

---

## File layout

```
SystemRunner.cs
```

Key excerpts:

### Manual Frame Loop

```csharp
const float fixedDelta = 1f / 60f; // 60Hz fixed timestep
const int maxSubStepsPerFrame = 4; // Maximum fixed steps per frame

var sw = Stopwatch.StartNew();
double prev = sw.Elapsed.TotalSeconds;

while (true)
{
    double now = sw.Elapsed.TotalSeconds;
    float dt = (float)(now - prev);
    prev = now;

    kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame);
    
    Thread.Sleep(1); // Frame rate limiting
}
```

### What PumpAndLateFrame Does

Internally, `PumpAndLateFrame()` performs:

1. **BeginFrame(dt)** — Runs variable-timestep systems (FrameInput, FrameSync)
2. **FixedStep(fixedDelta) × N** — Accumulates `dt` into fixed timestep and runs fixed systems multiple times
3. **LateFrame(alpha)** — Runs presentation systems (FrameView, FrameUI) with interpolation factor

---

## Build & Run

**Prereqs:** .NET 8 SDK and ZenECS Core assemblies referenced.

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project <your-console-sample-csproj>
```

Press **any key** to exit.

---

## Example output

```
=== ZenECS Core Sample - System Runner (Manual Loop) ===
Running manual frame loop...
Fixed timestep: 0.017s (60Hz)
Max sub-steps per frame: 4

=== Frame 60 (dt=0.017) ===
  Entity   1: pos=(1.00, 0)
  Entity   2: pos=(2, 0.50)

=== Frame 120 (dt=0.017) ===
  Entity   1: pos=(2.00, 0)
  Entity   2: pos=(2, 0.00)
...
```

---

## APIs highlighted

* **Kernel Frame Loop:**
    * `Kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame)` — all-in-one frame step
    * `Kernel.BeginFrame(dt)` — variable timestep phase
    * `Kernel.FixedStep(fixedDelta)` — fixed timestep phase
    * `Kernel.LateFrame(alpha)` — presentation phase
* **CommandBuffer:**
    * `World.BeginWrite()`, `cmd.CreateEntity()`, `cmd.AddComponent()`
* **Systems:**
    * `[FixedGroup]` (simulation writes)
    * `[FrameViewGroup]` (read-only presentation)

---

## Notes & best practices

* **Use `PumpAndLateFrame()`** for console apps that need direct loop control.
* **Fixed timestep** ensures deterministic simulation — use 60Hz (1/60s) for games.
* **Max sub-steps** prevents frame drops from causing large time jumps — typically 4-8.
* **Alpha interpolation** provides smooth presentation between fixed steps.
* **Frame rate limiting** (Thread.Sleep) reduces CPU usage in console apps.
* The kernel handles all the complexity of fixed timestep accumulation internally.

---

## License

MIT © 2025 Pippapips Limited.
