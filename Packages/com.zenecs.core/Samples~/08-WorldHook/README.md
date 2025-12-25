# ZenECS Core — Sample 08: World Hook

A **console** sample demonstrating world-level hooks for entity and component lifecycle events.

* Components: `Position`, `Health`
* Systems:
    * `HookSubscriberSystem : ISystemLifecycle` — subscribes to EntityEvents and ComponentEvents (FixedGroup)
    * `EntityLifecycleDemoSystem : ISystem` — creates/destroys entities to trigger hooks (FixedGroup)
    * `PrintStateSystem : ISystem` — prints current world state (FrameViewGroup)
* Kernel loop:
    * `Kernel.PumpAndLateFrame()` — steps simulation and presentation

---

## What this sample shows

1. **Entity lifecycle hooks**
   Subscribe to `EntityEvents.EntityCreated`, `EntityEvents.EntityDestroyRequested`, and `EntityEvents.EntityDestroy` to track entity lifetime.

2. **Component lifecycle hooks**
   Subscribe to `ComponentEvents.ComponentAdded` and `ComponentEvents.ComponentRemoved` to track component changes.

3. **Event subscription and cleanup**
   Properly subscribe in `Initialize()` and unsubscribe in `Shutdown()` to prevent memory leaks.

4. **Hook usage patterns**
   Hooks are useful for tooling, debugging, and cross-cutting concerns (not core gameplay logic).

---

## TL;DR flow

```
EntityEvents.EntityCreated += OnEntityCreated;
EntityEvents.EntityDestroy += OnEntityDestroyed;
ComponentEvents.ComponentAdded += OnComponentAdded;
ComponentEvents.ComponentRemoved += OnComponentRemoved;

// Events fire automatically when entities/components change
// Clean up in Shutdown()
```

---

## File layout

```
WorldHook.cs
```

Key excerpts:

### Event Subscription

```csharp
[FixedGroup]
public sealed class HookSubscriberSystem : ISystemLifecycle
{
    public void Initialize(IWorld w)
    {
        EntityEvents.EntityCreated += OnEntityCreated;
        EntityEvents.EntityDestroyRequested += OnEntityDestroyRequested;
        EntityEvents.EntityDestroy += OnEntityDestroyed;
        ComponentEvents.ComponentAdded += OnComponentAdded;
        ComponentEvents.ComponentRemoved += OnComponentRemoved;
    }

    public void Shutdown()
    {
        EntityEvents.EntityCreated -= OnEntityCreated;
        EntityEvents.EntityDestroyRequested -= OnEntityDestroyRequested;
        EntityEvents.EntityDestroy -= OnEntityDestroyed;
        ComponentEvents.ComponentAdded -= OnComponentAdded;
        ComponentEvents.ComponentRemoved -= OnComponentRemoved;
    }

    public void Run(IWorld w, float dt) { }
}
```

### Entity Lifecycle Events

```csharp
private void OnEntityCreated(IWorld world, Entity entity)
{
    Console.WriteLine($"[Hook] EntityCreated: e={entity.Id}");
}

private void OnEntityDestroyed(IWorld world, Entity entity)
{
    Console.WriteLine($"[Hook] EntityDestroy: e={entity.Id}");
}
```

### Component Lifecycle Events

```csharp
private void OnComponentAdded(IWorld world, Entity entity, Type componentType, object value)
{
    Console.WriteLine($"[Hook] ComponentAdded: e={entity.Id}, type={componentType.Name}");
}

private void OnComponentRemoved(IWorld world, Entity entity, Type componentType)
{
    Console.WriteLine($"[Hook] ComponentRemoved: e={entity.Id}, type={componentType.Name}");
}
```

### CommandBuffer Usage

```csharp
using (var cmd = w.BeginWrite())
{
    var e = cmd.CreateEntity();
    cmd.AddComponent(e, new Position(0, 0));
    cmd.DestroyEntity(e); // Triggers destroy hooks
}
```

---

## Build & Run

**Prereqs:** .NET 8 SDK and ZenECS Core assemblies referenced.

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project ZenEcsCoreSamples-08-WorldHook.csproj
```

Press **any key** to exit.

---

## Example output

```
=== ZenECS Core Sample - World Hook (Kernel) ===
[Hook] EntityCreated: e=1 (total created: 1)
[Hook] ComponentAdded: e=1, type=Position, value=(0, 0) (total added: 1)
[Hook] ComponentAdded: e=1, type=Health, value=HP=100 (total added: 2)
[Demo] Created entity 1 with Position and Health

=== Frame 60 ===
[Hook] ComponentAdded: e=1, type=Health, value=HP=150 (total added: 3)
[Demo] Added second Health component to entity 1

=== Frame 120 ===
[Hook] ComponentRemoved: e=1, type=Health (total removed: 1)
[Demo] Removed Health component from entity 1

=== Frame 180 ===
[Hook] EntityDestroyRequested: e=1
[Hook] EntityDestroy: e=1 (total destroyed: 1)
[Demo] Destroyed entity 1
...
```

---

## APIs highlighted

* **Entity Events:**
    * `EntityEvents.EntityCreated` — fired when entity is created
    * `EntityEvents.EntityDestroyRequested` — fired when destroy is requested
    * `EntityEvents.EntityDestroy` — fired when entity is fully destroyed
* **Component Events:**
    * `ComponentEvents.ComponentAdded` — fired when component is added
    * `ComponentEvents.ComponentRemoved` — fired when component is removed
* **System Lifecycle:**
    * `ISystemLifecycle.Initialize()` — called once on system registration
    * `ISystemLifecycle.Shutdown()` — called when system is removed
* **CommandBuffer:**
    * `world.BeginWrite()`, `cmd.CreateEntity()`, `cmd.DestroyEntity()`, `cmd.AddComponent()`, `cmd.RemoveComponent()`

---

## Notes & best practices

* **Hooks are for tooling/debugging**, not core gameplay logic. Use systems and message buses for gameplay.
* **Always unsubscribe** in `Shutdown()` to prevent memory leaks.
* Events fire **per-world** and carry the `IWorld` instance.
* **Exception-safe handlers** — exceptions propagate to the caller.
* Use hooks for cross-cutting concerns like logging, profiling, or editor tooling.
* Entity events fire in order: `EntityCreated` → `EntityDestroyRequested` → `EntityDestroy`.

---

## License

MIT © 2026 Pippapips Limited.
