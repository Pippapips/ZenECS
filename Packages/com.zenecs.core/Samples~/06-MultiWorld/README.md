# ZenECS Core — Sample 06: Multi-World (Kernel)

A **console** sample demonstrating how to create and manage multiple independent worlds with the ZenECS Kernel.

* Components: `Position`, `Velocity`
* Systems:
    * `MoveSystem : ISystem` — integrates `Position += Velocity * dt` (FixedGroup)
    * `PrintAllWorldsSystem : ISystem` — prints positions from all worlds (FrameViewGroup)
* Kernel loop:
    * `Kernel.CreateWorld()` — creates multiple worlds with names and tags
    * `Kernel.SetCurrentWorld()` — switches between worlds
    * `Kernel.PumpAndLateFrame()` — steps all worlds (or only current if configured)

---

## What this sample shows

1. **Multiple world creation**
   Create multiple independent worlds with different names and tags using `Kernel.CreateWorld()`.

2. **World switching**
   Switch between worlds using `Kernel.SetCurrentWorld()` and manage which world is active.

3. **Independent execution**
   Each world maintains its own entities, components, and systems, running independently.

4. **World lookup**
   Access all worlds via `Kernel.GetAllWorlds()` and query by name or tags.

---

## TL;DR flow

```
Kernel.CreateWorld(name: "GameWorld", tags: ["game", "main"])
Kernel.CreateWorld(name: "UISimulation", tags: ["ui", "overlay"])
Kernel.SetCurrentWorld(world1)
Kernel.PumpAndLateFrame(dt, fixedDelta, maxSubSteps)  // steps all worlds
```

Each world runs independently with its own entities and systems.

---

## File layout

```
MultiWorld.cs
```

Key excerpts:

### World Creation

```csharp
var kernel = new Kernel(null, logger: new EcsLogger());
var world1 = kernel.CreateWorld(null, name: "GameWorld", tags: new[] { "game", "main" });
var world2 = kernel.CreateWorld(null, name: "UISimulation", tags: new[] { "ui", "overlay" });
var world3 = kernel.CreateWorld(null, name: "Background", tags: new[] { "background" });
kernel.SetCurrentWorld(world1);
```

### Systems per World

```csharp
world1.AddSystems([new MoveSystem()]);
world2.AddSystems([new MoveSystem()]);
world3.AddSystems([new MoveSystem()]);
world1.AddSystems([new PrintAllWorldsSystem(kernel)]);
```

### Entity Creation with CommandBuffer

```csharp
using (var cmd = world1.BeginWrite())
{
    var e = cmd.CreateEntity();
    cmd.AddComponent(e, new Position(0, 0));
    cmd.AddComponent(e, new Velocity(1, 0));
}
```

### Frame Loop

```csharp
kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame);
```

---

## Build & Run

**Prereqs:** .NET 8 SDK and ZenECS Core assemblies referenced.

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project ZenEcsCoreSamples-06-MultiWorld.csproj
```

Press **[1]/[2]/[3]** to switch between worlds.
Press **[ESC]** to exit.

---

## Example output

```
=== ZenECS Core Sample - Multi-World (Kernel) ===
Created 3 worlds:
  - GameWorld (ID: ...)
  - UISimulation (ID: ...)
  - Background (ID: ...)

=== Current World: GameWorld (ID: ...) ===
  Entity   1: pos=(0.02, 0)
  Entity   2: pos=(2, 0.99)

[All Worlds: 3]
  - GameWorld (ID: ...): 2 entities
  - UISimulation (ID: ...): 1 entities
  - Background (ID: ...): 1 entities

[Switched to: UISimulation]
...
```

---

## APIs highlighted

* **Kernel & World Management:**
    * `Kernel.CreateWorld(name, tags)`
    * `Kernel.SetCurrentWorld(world)`
    * `Kernel.GetAllWorlds()`
    * `Kernel.PumpAndLateFrame(dt, fixedDelta, maxSubSteps)`
* **CommandBuffer:**
    * `world.BeginWrite()`, `cmd.CreateEntity()`, `cmd.AddComponent()`
* **Systems:**
    * `[FixedGroup]` (simulation writes)
    * `[FrameViewGroup]` (read-only presentation)

---

## Notes & best practices

* Use **multiple worlds** for separate simulation spaces (game world, UI simulation, background processing).
* Each world maintains **independent state** — entities, components, and systems are isolated.
* **CommandBuffer** should be used for entity creation and component modifications.
* **Kernel.PumpAndLateFrame()** steps all worlds by default (or only current if `StepOnlyCurrentWhenSelected` is enabled).
* World switching is useful for scene management, level transitions, or multi-layer simulations.

---

## License

MIT © 2026 Pippapips Limited.
