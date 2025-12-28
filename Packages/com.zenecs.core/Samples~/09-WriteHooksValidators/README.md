# ZenECS Core — Sample 09: Write Hooks & Validators

A **console** sample demonstrating write permission hooks and component value validators.

* Components: `Position`, `Health`, `Locked`
* Systems:
    * `HookSetupSystem : ISystemLifecycle` — sets up write hooks and validators (FixedGroup)
    * `WriteAttemptSystem : ISystem` — attempts various writes to demonstrate hooks (FixedGroup)
    * `PrintStateSystem : ISystem` — prints current world state (FrameViewGroup)
* Kernel loop:
    * `Kernel.PumpAndLateFrame()` — steps simulation and presentation

---

## What this sample shows

1. **Write permission hooks**
   Use `AddWritePermission()` to control which entities/components can be written to.

2. **Read permission hooks**
   Use `AddReadPermission()` to control which entities/components can be read from.

3. **Typed validators**
   Use `AddValidator<T>()` to validate component values before writes.

4. **Object-level validators**
   Use `AddValidator()` for generic validation across all component types.

---

## TL;DR flow

```
// Write permission: prevent writes to locked entities
world.AddWritePermission((entity, componentType) =>
    !world.HasComponent<Locked>(entity));

// Typed validator: Health must be 0-200
world.AddValidator<Health>(health => 
    health.Value >= 0 && health.Value <= 200);

// Object validator: Position must be finite
world.AddValidator((object obj) =>
    obj is Position pos && 
    !float.IsNaN(pos.X) && !float.IsInfinity(pos.X));
```

---

## File layout

```
WriteHooksValidators.cs
```

Key excerpts:

### Write Permission Hook

```csharp
[FixedGroup]
public sealed class HookSetupSystem : ISystemLifecycle
{
    private Func<Entity, Type, bool>? _writePermissionHook;

    public void Initialize(IWorld w)
    {
        _writePermissionHook = (entity, componentType) =>
        {
            if (w.HasComponent<Locked>(entity))
            {
                Console.WriteLine($"[Permission] Write denied: Entity {entity.Id} is Locked, cannot modify {componentType.Name}");
                return false;
            }
            return true;
        };
        w.AddWritePermission(_writePermissionHook);
    }

    public void Shutdown()
    {
        // Cleanup if needed
    }

    public void Run(IWorld w, float dt) { }
}
```

### Typed Validator

```csharp
public void Initialize(IWorld w)
{
    var healthValidator = (Health health) =>
    {
        if (health.Value < 0 || health.Value > 200)
        {
            Console.WriteLine($"[Validator] Health validation failed: {health.Value} (must be 0-200)");
            return false;
        }
        return true;
    };
    w.AddValidator(healthValidator);
}
```

### Object-Level Validator

```csharp
public void Initialize(IWorld w)
{
    w.AddValidator((object obj) =>
    {
        if (obj is Position pos)
        {
            if (float.IsNaN(pos.X) || float.IsNaN(pos.Y) || 
                float.IsInfinity(pos.X) || float.IsInfinity(pos.Y))
            {
                Console.WriteLine($"[Validator] Position validation failed: ({pos.X}, {pos.Y}) (must be finite)");
                return false;
            }
        }
        return true;
    });
}
```

---

## Build & Run

**Prereqs:** .NET 8 SDK and ZenECS Core assemblies referenced.

```bash
dotnet restore
dotnet build --no-restore
dotnet run --project ZenEcsCoreSamples-09-WriteHooksValidators.csproj
```

Press **any key** to exit.

---

## Example output

```
=== ZenECS Core Sample - Write Hooks & Validators (Kernel) ===
[Setup] Write hooks and validators registered
[Demo] Created normal entity 1 and locked entity 2

=== Frame 60 ===
[Demo] Valid write: Updated entity 1 Health to 150

=== Frame 120 ===
[Permission] Write denied: Entity 2 is Locked, cannot modify Health
[Demo] Write blocked by permission hook: ...

=== Frame 180 ===
[Validator] Health validation failed: 250 (must be 0-200)
[Demo] Write blocked by validator: ...

=== Frame 240 ===
[Validator] Position validation failed: (NaN, 0) (must be finite)
[Demo] Write blocked by validator: ...
```

---

## APIs highlighted

* **Write Permission:**
    * `world.AddWritePermission(predicate)`
    * `world.RemoveWritePermission(predicate)`
    * `world.ClearWritePermissions()`
* **Read Permission:**
    * `world.AddReadPermission(predicate)`
    * `world.RemoveReadPermission(predicate)`
    * `world.ClearReadPermissions()`
* **Validators:**
    * `world.AddValidator<T>(predicate)` — typed validator
    * `world.AddValidator(predicate)` — object-level validator
    * `world.RemoveValidator<T>(predicate)`
    * `world.ClearValidators()`

---

## Notes & best practices

* **Write permission hooks** are evaluated for every write attempt — all hooks must allow the write.
* **Validators** run before writes — invalid values are rejected and exceptions are thrown.
* Use **typed validators** for component-specific rules (e.g., Health range).
* Use **object-level validators** for generic checks (e.g., no NaN, no nulls).
* Hooks are **per-world** and cleared when the world is disposed.
* Use hooks for **server-authoritative** rules, **debug tooling**, or **phase-based restrictions**.

---

## License

MIT © 2026 Pippapips Limited.
