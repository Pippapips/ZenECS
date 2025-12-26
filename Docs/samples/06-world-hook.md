# 06 - World Hook

> Docs / Samples / 06 - World Hook

Use world hooks to intercept component operations and add custom logic.

## Prerequisites

- **.NET SDK 8.0+** or **Unity 2021.3+**
- **ZenECS Core** package installed
- Understanding of [World Hook](../core/world-hook.md)

## Run It

### .NET Standalone

```bash
cd Packages/com.zenecs.core/Samples~/06-WorldHook
dotnet run
```

### Unity

1. Open Unity project
2. Open scene: `Packages/com.zenecs.core/Samples~/06-WorldHook/Scene.unity`
3. Press Play

## Code Walkthrough

### Write Permission Hook

Control who can write components:

```csharp
world.Hooks.AddWritePermission((entity, componentType) =>
{
    // Custom permission logic
    if (componentType == typeof(Health))
    {
        return HasHealthPermission(entity);
    }
    return true;
});
```

### Value Validator

Validate component values:

```csharp
world.Hooks.AddValidator<Health>(health =>
{
    // Validate health values
    if (health.Current < 0)
        return false;
    if (health.Current > health.Max)
        return false;
    return true;
});
```

## Complete Example

```csharp
using ZenECS.Core;

var world = kernel.CreateWorld(null, "GameWorld");

// Add write permission hook
world.Hooks.AddWritePermission((entity, componentType) =>
{
    // Only allow health changes for player entities
    if (componentType == typeof(Health))
    {
        return world.HasComponent<Player>(entity);
    }
    return true;
});

// Add validator
world.Hooks.AddValidator<Health>(health =>
{
    // Ensure health is within bounds
    return health.Current >= 0 && health.Current <= health.Max;
});

// Try to modify health
Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Player());
    cmd.AddComponent(entity, new Health(100, 100));
}

// This will be validated
world.ReplaceComponent(entity, new Health(150, 100));  // Clamped to 100
```

## See Also

- [World Hook](../core/world-hook.md) - Detailed documentation
- [Write Hooks & Validators](../core/write-hooks-validators.md) - Validation system
- [Advanced Topics](../guides/advanced-topics.md) - Advanced patterns
