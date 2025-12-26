# ZenECS Core — Sample 02: View→Data via MessageBus

A **console** sample that demonstrates the ZenECS philosophy where the **view never writes ECS data directly**.
Instead, the view publishes **messages**; simulation systems consume them and mutate ECS state; presentation reads state (read-only).

* Minimal component: `Health`
* Message: `DamageRequest : IMessage`
* Systems:
    * `DamageSystem : ISystemLifecycle` (FixedGroup — writes via messages)
    * `PrintHealthSystem : ISystem` (FrameViewGroup — read-only)
* Kernel loop:
    * `Kernel.CreateWorld()` creates the world
    * `world.AddSystems()` registers systems
    * `Kernel.PumpAndLateFrame()` integrates variable step + fixed step

---

## What this sample shows

1. **View → Message**
   The "view" layer (here, console key input) never touches the world. It only calls `world.Publish(new DamageRequest(...))`.

2. **Message → System → World**
   `DamageSystem` subscribes to `DamageRequest` in `Initialize()`, then updates `Health` components in Simulation.

3. **World → Presentation (read-only)**
   `PrintHealthSystem` queries `Health` and prints it during Late (no writes).

---

## TL;DR flow

```
[View/Input] → publish(DamageRequest) → [MessageBus]
      → [DamageSystem] (FixedGroup) → update Health
      → [PrintHealthSystem] (FrameViewGroup) → print HP (read-only)
```

All writes happen in **FixedGroup**; **FrameViewGroup** is read-only and runs in **Late**.

---

## File layout

```
Messages.cs
```

Key excerpts:

### Component

```csharp
public readonly struct Health
{
    public readonly int Value;
    public Health(int value) => Value = value;
    public override string ToString() => $"HP={Value}";
}
```

### Message

```csharp
public readonly struct DamageRequest : IMessage
{
    public readonly Entity Entity;
    public readonly int Amount;
    public DamageRequest(Entity entity, int amount)
    {
        Entity = entity;
        Amount = amount;
    }
}
```

### Systems

```csharp
[FixedGroup]
public sealed class DamageSystem : ISystemLifecycle
{
    private IDisposable? _sub;

    public void Initialize(IWorld w)
    {
        _sub = w.Subscribe<DamageRequest>(m =>
        {
            if (!w.IsAlive(m.Entity) || !w.HasComponent<Health>(m.Entity)) return;
            
            using var cmd = w.BeginWrite();
            var current = w.ReadComponent<Health>(m.Entity);
            var updated = new Health(Math.Max(0, current.Value - m.Amount));
            cmd.ReplaceComponent(m.Entity, updated);
            Console.WriteLine($"[Logic] e:{m.Entity} took {m.Amount} → {updated}");
        });
    }

    public void Shutdown()
    {
        _sub?.Dispose();
    }

    public void Run(IWorld w, float dt) { }
}

[FrameViewGroup]
public sealed class PrintHealthSystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        foreach (var (e, health) in w.Query<Health>())
        {
            Console.WriteLine($"Entity {e.Id,2}: {health}");
        }
    }
}
```

### Frame driver

```csharp
var kernel = new Kernel();
var world = kernel.CreateWorld(null);
kernel.SetCurrentWorld(world);

world.AddSystems([
    new DamageSystem(),
    new PrintHealthSystem()
]);

// View layer: publish messages only
world.Publish(new DamageRequest(e1, rand.Next(5, 15)));

kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame);
```

---

## Build & Run

**Prereqs:** .NET 8 SDK, and ZenECS Core assemblies referenced.

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project ZenEcsCoreSamples-02-Messages.csproj
```

Press **1** or **2** to publish `DamageRequest` to entity 1 or 2.
Press **ESC** to exit.

---

## Example output

```
=== ZenECS Core Sample - View→Data via MessageBus (Kernel) ===
Running... press [1]/[2] to deal damage, [ESC] to quit.
[View] Sent DamageRequest → e:1
[Logic] e:1 took 12 → HP=88
Entity  1: HP=88
Entity  2: HP=75
[View] Sent DamageRequest → e:2
[Logic] e:2 took 7 → HP=68
Entity  1: HP=88
Entity  2: HP=68
Shutting down...
Done.
```

---

## APIs highlighted

* **Kernel & loop:** `Kernel.CreateWorld()`, `Kernel.PumpAndLateFrame()`, `Kernel.Dispose()`
* **MessageBus:** `world.Publish<T>()`, `world.Subscribe<T>(handler)`
* **World:** `world.BeginWrite()`, `cmd.CreateEntity()`, `cmd.AddComponent()`, `world.ReadComponent<T>()`, `cmd.ReplaceComponent()`, `world.Query<T>()`
* **Systems:** `[FixedGroup]` (writes), `[FrameViewGroup]` (read-only), `ISystemLifecycle` for subscription management

---

## Notes & best practices

* UI/View code should **never** mutate ECS data directly—**always** publish messages.
* Keep message handlers small and focused (one message → one responsibility).
* Presentation is **FrameViewGroup** and **read-only** for determinism and clarity.
* Use a fixed timestep for stable simulation.
* Always unsubscribe in `Shutdown()` to prevent memory leaks.

---

## License

MIT © 2026 Pippapips Limited.
