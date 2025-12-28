# ZenECS Core — Sample 12: Binding

A **console** sample demonstrating view binding using Contexts and Binders.

* Components: `Position`, `Velocity`, `Health`
* Contexts: `ConsoleViewContext` — view-related data container
* Binders: `PositionBinder`, `HealthBinder` — component change handlers
* Systems:
    * `MoveSystem : ISystem` — moves entities (FixedGroup)
    * `DamageSystem : ISystem` — damages entities (FixedGroup)
    * `PrintSummarySystem : ISystem` — prints summary (FrameViewGroup)
* Kernel loop:
    * `Kernel.PumpAndLateFrame()` — steps simulation and presentation

---

## What this sample shows

1. **Contexts**
   Attach view-related data to entities using `IContext` implementations.

2. **Binders**
   Implement `IBinder` (or derive from `BaseBinder`) to react to component changes.

3. **ComponentDelta**
   Receive component change notifications via `IBind<T>.OnDelta()`.

4. **View integration pattern**
   Decouple view updates from simulation logic using the binding system.

---

## TL;DR flow

```
// Attach context (view data)
var context = new ConsoleViewContext { DisplayName = "Entity1" };
world.RegisterContext(entity, context);

// Attach binder (view updates)
world.AttachBinder(entity, new PositionBinder());

// Binder receives deltas when components change
public void OnDelta(in ComponentDelta<Position> delta)
{
    // Update view based on component change
}
```

---

## File layout

```
Binding.cs
```

Key excerpts:

### Context Definition

```csharp
public class ConsoleViewContext : IContext
{
    public string DisplayName { get; set; } = "";
    public ConsoleColor Color { get; set; } = ConsoleColor.White;
    public long LastUpdateFrame { get; set; }
}
```

### Binder Implementation

```csharp
public sealed class PositionBinder : BaseBinder, IBind<Position>
{
    public void OnDelta(in ComponentDelta<Position> delta)
    {
        if (Contexts == null || World == null) return;

        var context = Contexts.Get<ConsoleViewContext>(World, Entity);
        if (context != null)
        {
            context.LastUpdateFrame = World.FrameCount;
            var kind = delta.Kind == ComponentDeltaKind.Added ? "added" :
                      delta.Kind == ComponentDeltaKind.Changed ? "changed" :
                      delta.Kind == ComponentDeltaKind.Removed ? "removed" : "snapshot";
            Console.WriteLine($"[Binder] {context.DisplayName}: Position {kind} to {delta.Value}");
        }
    }

    protected override void OnApply(IWorld w, Entity e)
    {
        // Called at end of presentation phase
        // Can perform final view updates here if needed
    }
}
```

### Attaching Contexts and Binders

```csharp
using (var cmd = world.BeginWrite())
{
    var e = cmd.CreateEntity();
    cmd.AddComponent(e, new Position(0, 0));
    cmd.AddComponent(e, new Health(100));

    var context = new ConsoleViewContext { DisplayName = "Entity1", Color = ConsoleColor.Green };
    world.RegisterContext(e, context);
    world.AttachBinder(e, new PositionBinder());
    world.AttachBinder(e, new HealthBinder());
}
```

---

## Build & Run

**Prereqs:** .NET 8 SDK and ZenECS Core assemblies referenced.

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project ZenEcsCoreSamples-12-Binding.csproj
```

Press **any key** to exit.

---

## Example output

```
=== ZenECS Core Sample - Binding (Kernel) ===
[Binder] Entity1: Position added to (0, 0)
[Binder] Entity1: Health added to HP=100
[Binder] Entity2: Position added to (5, 5)
[Binder] Entity2: Health added to HP=150

=== Frame 60 Summary ===
Total entities: 2

[Binder] Entity1: Position changed to (1.00, 0)
[Binder] Entity2: Position changed to (5, 4.50)

=== Frame 120 Summary ===
[Binder] Entity1: Health changed to HP=90
[Binder] Entity2: Health changed to HP=140
...
```

---

## APIs highlighted

* **Contexts:**
    * `world.RegisterContext(entity, context)` — attach context to entity
    * `world.DetachContext<T>(entity)` — detach context from entity
    * `contexts.Get<T>(world, entity)` — get context from binder
* **Binders:**
    * `world.AttachBinder(entity, binder)` — attach binder to entity
    * `world.DetachBinder<T>(entity)` — detach binder from entity
    * `BaseBinder` — convenience base class
    * `IBind<T>` — interface for component-specific binders
* **ComponentDelta:**
    * `ComponentDelta<T>.Kind` — Added, Changed, Removed, Snapshot
    * `ComponentDelta<T>.Value` — component value at time of delta
    * `ComponentDelta<T>.Entity` — entity where change occurred

---

## Notes & best practices

* **Contexts hold view data** — separate from ECS components (e.g., Unity GameObjects, UI elements).
* **Binders react to changes** — implement `IBind<T>` for each component type you want to track.
* **BaseBinder** provides common lifecycle — cache `World` and `Contexts` on bind.
* **OnDelta** is called when components change — use this to update views.
* **OnApply** is called at end of presentation — use for final view updates if needed.
* **Binding is decoupled** — view layer doesn't directly modify ECS data.
* **Use for Unity integration** — binders update Unity GameObjects, UI, audio, etc.

---

## License

MIT © 2026 Pippapips Limited.
