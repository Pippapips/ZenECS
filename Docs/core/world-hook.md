# World Hook

> Docs / Core / World Hook

Hooks provide lifecycle events and interception points for component operations.

## Overview

World hooks enable:

- **Lifecycle Events**: React to world events
- **Component Interception**: Intercept component operations
- **Custom Logic**: Add custom behavior
- **Validation**: Validate component values

## API Surface

### Write Permission Hook

```csharp
world.Hooks.AddWritePermission((entity, componentType) =>
{
    // Custom permission logic
    return HasPermission(entity, componentType);
});
```

### Value Validator

```csharp
world.Hooks.AddValidator<Health>(health =>
{
    // Validate health values
    return health.Current >= 0 && health.Current <= health.Max;
});
```

## Examples

See [Write Hooks & Validators](./write-hooks-validators.md) for detailed examples.

## See Also

- [Write Hooks & Validators](./write-hooks-validators.md) - Detailed guide
- [World](./world.md) - World management
- [Advanced Topics](../guides/advanced-topics.md) - Advanced patterns
