# 07 - WriteHooks & Validators

> Docs / Samples / 07 - WriteHooks & Validators

Use write hooks and validators to control component writes and validate values.

## Prerequisites

- **.NET SDK 8.0+** or **Unity 2021.3+**
- **ZenECS Core** package installed
- Understanding of [Write Hooks & Validators](../core/write-hooks-validators.md)

## Run It

### .NET Standalone

```bash
cd Packages/com.zenecs.core/Samples~/07-WriteHooksValidators
dotnet run
```

### Unity

1. Open Unity project
2. Open scene: `Packages/com.zenecs.core/Samples~/07-WriteHooksValidators/Scene.unity`
3. Press Play

## Code Walkthrough

### Write Permission Hook

Control component write permissions:

```csharp
world.Hooks.AddWritePermission((entity, componentType) =>
{
    // Custom permission logic
    if (componentType == typeof(AdminComponent))
    {
        return world.HasComponent<Admin>(entity);
    }
    return true;
});
```

### Value Validator

Validate component values before write:

```csharp
world.Hooks.AddValidator<Health>(health =>
{
    // Validate health is within bounds
    return health.Current >= 0 && 
           health.Current <= health.Max &&
           health.Max > 0;
});
```

## Complete Example

```csharp
using ZenECS.Core;

var world = kernel.CreateWorld(null, "GameWorld");

// Permission hook: Only admins can modify admin components
world.Hooks.AddWritePermission((entity, componentType) =>
{
    if (componentType == typeof(AdminComponent))
    {
        return world.HasComponent<Admin>(entity);
    }
    return true;
});

// Validator: Ensure health is valid
world.Hooks.AddValidator<Health>(health =>
{
    if (health.Current < 0)
        return false;
    if (health.Current > health.Max)
        return false;
    if (health.Max <= 0)
        return false;
    return true;
});

// Test permissions
Entity player;
using (var cmd = world.BeginWrite())
{
    player = cmd.CreateEntity();
    cmd.AddComponent(player, new Health(100, 100));
}

// This will be validated
world.ReplaceComponent(player, new Health(150, 100));  // Invalid: exceeds max

// This will be allowed
world.ReplaceComponent(player, new Health(50, 100));  // Valid
```

## See Also

- [Write Hooks & Validators](../core/write-hooks-validators.md) - Detailed documentation
- [World Hook](../core/world-hook.md) - Hook system
- [Advanced Topics](../guides/advanced-topics.md) - Advanced patterns
