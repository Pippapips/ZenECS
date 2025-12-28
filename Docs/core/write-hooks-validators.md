# Write Hooks & Validators

> Docs / Core / Write Hooks & Validators

Control component writes with permission hooks and value validators.

## Overview

Write hooks and validators provide:

- **Write Permissions**: Control who can write components
- **Value Validation**: Validate component values before write
- **Security**: Prevent unauthorized modifications
- **Data Integrity**: Ensure valid component states

## API Surface

### Write Permission Hook

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

## Examples

### Permission Hook

```csharp
// Only admins can modify admin components
world.Hooks.AddWritePermission((entity, componentType) =>
{
    if (componentType == typeof(AdminComponent))
    {
        return world.HasComponent<Admin>(entity);
    }
    return true;
});
```

### Value Validator

```csharp
// Ensure health is within bounds
world.Hooks.AddValidator<Health>(health =>
{
    return health.Current >= 0 && 
           health.Current <= health.Max &&
           health.Max > 0;
});
```

## See Also

- [World Hook](./world-hook.md) - Hook system
- [Samples](../samples/07-writehooks-validators.md) - Sample project
- [Advanced Topics](../guides/advanced-topics.md) - Advanced patterns
