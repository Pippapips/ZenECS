# Policy & Permissions

> Docs / Core / Policy & Permissions

Read/write permissions and validation guards for component operations.

## Overview

ZenECS provides a flexible permission and validation system that allows you to control component read/write access and validate component values before they are written to the world.

**Key Concepts:**

- **Write Permissions**: Control who can modify components
- **Read Permissions**: Control who can read components
- **Validators**: Validate component values before writes
- **Typed Validators**: Type-specific validation rules
- **Object Validators**: Generic validation across all types

## How It Works

### Permission Evaluation

Permissions are evaluated using predicates:

- **Write Permission**: `Func<Entity, Type, bool>` - Returns `true` if write is allowed
- **Read Permission**: `Func<Entity, Type, bool>` - Returns `true` if read is allowed

All registered predicates must return `true` (logical AND) for the operation to be allowed.

### Validation Flow

When writing a component:

1. **Write Permission Check**: All write permission predicates must pass
2. **Object Validators**: All object-level validators must pass
3. **Typed Validators**: All type-specific validators must pass
4. **Write Operation**: Component is written if all checks pass

### Failure Policy

When a write is denied or validation fails, the behavior is controlled by `EcsRuntimeOptions.WritePolicy`:

- **Throw**: Throw an exception immediately (default)
- **Log**: Log a warning and ignore the operation
- **Ignore**: Silently ignore the operation

## API Surface

### Write Permissions

#### `AddWritePermission(Func<Entity, Type, bool> hook)`

Add a write permission predicate:

```csharp
world.Hooks.AddWritePermission((entity, componentType) =>
{
    // Return true if write is allowed
    return IsAllowed(entity, componentType);
});
```

#### `RemoveWritePermission(Func<Entity, Type, bool> hook)`

Remove a write permission predicate:

```csharp
var hook = (Entity e, Type t) => true;
world.Hooks.AddWritePermission(hook);
world.Hooks.RemoveWritePermission(hook);
```

#### `ClearWritePermissions()`

Clear all write permission predicates:

```csharp
world.Hooks.ClearWritePermissions();
```

### Read Permissions

#### `AddReadPermission(Func<Entity, Type, bool> hook)`

Add a read permission predicate:

```csharp
world.Hooks.AddReadPermission((entity, componentType) =>
{
    // Return true if read is allowed
    return IsVisible(entity, componentType);
});
```

#### `RemoveReadPermission(Func<Entity, Type, bool> hook)`

Remove a read permission predicate:

```csharp
var hook = (Entity e, Type t) => true;
world.Hooks.AddReadPermission(hook);
world.Hooks.RemoveReadPermission(hook);
```

#### `ClearReadPermissions()`

Clear all read permission predicates:

```csharp
world.Hooks.ClearReadPermissions();
```

### Object-Level Validators

#### `AddValidator(Func<object, bool> hook)`

Add a generic validator for all component types:

```csharp
world.Hooks.AddValidator((object value) =>
{
    // Validate any component value
    return !HasInvalidValues(value);
});
```

#### `RemoveValidator(Func<object, bool> hook)`

Remove an object-level validator:

```csharp
var validator = (object v) => true;
world.Hooks.AddValidator(validator);
world.Hooks.RemoveValidator(validator);
```

#### `ClearValidators()`

Clear all object-level validators:

```csharp
world.Hooks.ClearValidators();
```

### Typed Validators

#### `AddValidator<T>(Func<T, bool> predicate)`

Add a type-specific validator:

```csharp
world.Hooks.AddValidator<Health>((health) =>
{
    // Validate Health component
    return health.Current >= 0 && health.Current <= health.Max;
});
```

#### `RemoveValidator<T>(Func<T, bool> predicate)`

Remove a typed validator:

```csharp
var validator = (Health h) => h.Current >= 0;
world.Hooks.AddValidator(validator);
world.Hooks.RemoveValidator(validator);
```

#### `ClearTypedValidators()`

Clear all typed validators:

```csharp
world.Hooks.ClearTypedValidators();
```

## Examples

### Phase-Based Write Lock

Prevent structural changes during specific phases:

```csharp
bool _allowWrites = true;

world.Hooks.AddWritePermission((entity, componentType) =>
{
    // Only allow writes during specific phases
    return _allowWrites;
});

// Lock writes during simulation
_allowWrites = false;
// ... simulation phase ...
_allowWrites = true;
```

### Server-Authoritative Writes

Enforce server authority in client:

```csharp
bool _isServer = false;

world.Hooks.AddWritePermission((entity, componentType) =>
{
    // Only server can write certain components
    if (componentType == typeof(Position) && !_isServer)
        return false;
    
    return true;
});
```

### Entity-Specific Permissions

Control access per entity:

```csharp
var lockedEntities = new HashSet<Entity>();

world.Hooks.AddWritePermission((entity, componentType) =>
{
    // Prevent writes to locked entities
    return !lockedEntities.Contains(entity);
});

// Lock an entity
lockedEntities.Add(entity);

// Unlock an entity
lockedEntities.Remove(entity);
```

### Component Type Restrictions

Restrict writes to specific component types:

```csharp
var allowedTypes = new HashSet<Type>
{
    typeof(Position),
    typeof(Velocity)
};

world.Hooks.AddWritePermission((entity, componentType) =>
{
    // Only allow writes to allowed types
    return allowedTypes.Contains(componentType);
});
```

### Range Validation

Validate numeric ranges:

```csharp
world.Hooks.AddValidator<Health>((health) =>
{
    // Health must be in valid range
    return health.Current >= 0 && 
           health.Current <= health.Max && 
           health.Max > 0;
});

world.Hooks.AddValidator<Position>((position) =>
{
    // Position must be finite
    return float.IsFinite(position.X) &&
           float.IsFinite(position.Y) &&
           float.IsFinite(position.Z);
});
```

### Generic NaN/Infinity Check

Prevent invalid floating-point values:

```csharp
world.Hooks.AddValidator((object value) =>
{
    // Check all float fields for NaN/Infinity
    var type = value.GetType();
    foreach (var field in type.GetFields())
    {
        if (field.FieldType == typeof(float))
        {
            var floatValue = (float)field.GetValue(value)!;
            if (float.IsNaN(floatValue) || float.IsInfinity(floatValue))
                return false;
        }
    }
    return true;
});
```

### Clamping Values

Clamp values to valid ranges:

```csharp
world.Hooks.AddValidator<Health>((health) =>
{
    // Clamp health to valid range
    if (health.Current < 0)
        health = new Health { Current = 0, Max = health.Max };
    if (health.Current > health.Max)
        health = new Health { Current = health.Max, Max = health.Max };
    
    return true;
});
```

### Spectator Mode

Hide certain entities from spectators:

```csharp
bool _isSpectator = false;
var hiddenEntities = new HashSet<Entity>();

world.Hooks.AddReadPermission((entity, componentType) =>
{
    // Spectators can't see hidden entities
    if (_isSpectator && hiddenEntities.Contains(entity))
        return false;
    
    return true;
});
```

### Debug Tooling

Restrict access in debug tools:

```csharp
bool _debugMode = false;

world.Hooks.AddReadPermission((entity, componentType) =>
{
    // In debug mode, only show specific components
    if (_debugMode)
    {
        var allowedTypes = new[] { typeof(Position), typeof(Health) };
        return allowedTypes.Contains(componentType);
    }
    
    return true;
});
```

### Temporary Lock

Temporarily lock writes:

```csharp
var writeLock = new object();
bool _writeLocked = false;

world.Hooks.AddWritePermission((entity, componentType) =>
{
    lock (writeLock)
    {
        return !_writeLocked;
    }
});

// Lock writes
lock (writeLock)
{
    _writeLocked = true;
}

// ... perform operations that should not trigger writes ...

// Unlock writes
lock (writeLock)
{
    _writeLocked = false;
}
```

### Complex Validation

Multiple validators for complex rules:

```csharp
// Health validation
world.Hooks.AddValidator<Health>((health) =>
{
    return health.Current >= 0 && health.Current <= health.Max;
});

// Position validation
world.Hooks.AddValidator<Position>((position) =>
{
    return float.IsFinite(position.X) && 
           float.IsFinite(position.Y) && 
           float.IsFinite(position.Z);
});

// Generic validation
world.Hooks.AddValidator((object value) =>
{
    // Check for null references in nested objects
    return !HasNullReferences(value);
});
```

## Best Practices

### 1. Use Typed Validators for Type-Specific Rules

```csharp
// ✅ Good: Typed validator
world.Hooks.AddValidator<Health>((health) => health.Current >= 0);

// ❌ Bad: Object validator with type checking
world.Hooks.AddValidator((object v) => 
    v is Health h && h.Current >= 0);
```

### 2. Keep Validators Fast

Validators run on every write, so keep them fast:

```csharp
// ✅ Good: Fast validation
world.Hooks.AddValidator<Health>((health) => health.Current >= 0);

// ❌ Bad: Slow validation (reflection, allocations)
world.Hooks.AddValidator((object v) => 
    ValidateWithReflection(v));
```

### 3. Use Write Policy for Error Handling

Configure write policy based on environment:

```csharp
#if DEBUG
    EcsRuntimeOptions.WritePolicy = WriteFailurePolicy.Throw;
#else
    EcsRuntimeOptions.WritePolicy = WriteFailurePolicy.Log;
#endif
```

### 4. Clear Hooks in Tests

Reset hooks between tests:

```csharp
[SetUp]
public void Setup()
{
    world.Hooks.ClearWritePermissions();
    world.Hooks.ClearReadPermissions();
    world.Hooks.ClearValidators();
    world.Hooks.ClearTypedValidators();
}
```

### 5. Document Permission Rules

Document why permissions are set:

```csharp
// Lock writes during network sync to prevent desync
world.Hooks.AddWritePermission((entity, componentType) =>
{
    // Server-authoritative: only server can write Position
    if (componentType == typeof(Position) && !IsServer)
        return false;
    
    return true;
});
```

## Write Failure Policy

### `WriteFailurePolicy.Throw` (Default)

Throw an exception when write is denied:

```csharp
EcsRuntimeOptions.WritePolicy = WriteFailurePolicy.Throw;

try
{
    world.AddComponent(entity, new Health());
}
catch (InvalidOperationException ex)
{
    // Write was denied
}
```

### `WriteFailurePolicy.Log`

Log a warning and ignore the operation:

```csharp
EcsRuntimeOptions.WritePolicy = WriteFailurePolicy.Log;

// Write is silently ignored if denied
world.AddComponent(entity, new Health());
// Warning logged if denied
```

### `WriteFailurePolicy.Ignore`

Silently ignore denied operations:

```csharp
EcsRuntimeOptions.WritePolicy = WriteFailurePolicy.Ignore;

// Write is silently ignored if denied
world.AddComponent(entity, new Health());
// No logging, no exception
```

## Performance Considerations

### Validator Performance

Validators run on every write operation:

```csharp
// ✅ Good: Fast, inline validation
world.Hooks.AddValidator<Health>((health) => health.Current >= 0);

// ❌ Bad: Slow validation (allocations, reflection)
world.Hooks.AddValidator((object v) => 
{
    var json = JsonSerializer.Serialize(v);
    return ValidateJson(json);
});
```

### Permission Check Performance

Permission checks run on every read/write:

```csharp
// ✅ Good: Fast permission check
var allowedTypes = new HashSet<Type> { typeof(Position) };
world.Hooks.AddWritePermission((entity, type) => 
    allowedTypes.Contains(type));

// ❌ Bad: Slow permission check (reflection, string operations)
world.Hooks.AddWritePermission((entity, type) => 
    type.Name.Contains("Position"));
```

### Caching Permission Results

Cache permission results when possible:

```csharp
var permissionCache = new Dictionary<(Entity, Type), bool>();

world.Hooks.AddWritePermission((entity, componentType) =>
{
    var key = (entity, componentType);
    if (!permissionCache.TryGetValue(key, out var allowed))
    {
        allowed = ComputePermission(entity, componentType);
        permissionCache[key] = allowed;
    }
    return allowed;
});
```

## Common Patterns

### Pattern 1: Phase-Based Locking

```csharp
public class PhaseLock
{
    private bool _locked = false;
    
    public void Lock() => _locked = true;
    public void Unlock() => _locked = false;
    
    public void Setup(IWorld world)
    {
        world.Hooks.AddWritePermission((entity, type) => !_locked);
    }
}
```

### Pattern 2: Role-Based Access

```csharp
public enum UserRole { Admin, User, Spectator }

public class RoleBasedPermissions
{
    private UserRole _role = UserRole.User;
    
    public void Setup(IWorld world)
    {
        world.Hooks.AddWritePermission((entity, type) =>
        {
            // Only admins can write
            return _role == UserRole.Admin;
        });
        
        world.Hooks.AddReadPermission((entity, type) =>
        {
            // Spectators have limited read access
            if (_role == UserRole.Spectator)
                return type == typeof(Position);
            
            return true;
        });
    }
}
```

### Pattern 3: Entity Ownership

```csharp
public class EntityOwnership
{
    private readonly Dictionary<Entity, int> _owners = new();
    
    public void Setup(IWorld world)
    {
        world.Hooks.AddWritePermission((entity, type) =>
        {
            // Only owner can write
            if (_owners.TryGetValue(entity, out var ownerId))
                return ownerId == CurrentUserId;
            
            return true; // No owner = anyone can write
        });
    }
    
    public void SetOwner(Entity entity, int userId)
    {
        _owners[entity] = userId;
    }
}
```

## FAQ

### How do I temporarily disable all permissions?

Clear all hooks:

```csharp
world.Hooks.ClearWritePermissions();
world.Hooks.ClearReadPermissions();
world.Hooks.ClearValidators();
world.Hooks.ClearTypedValidators();
```

### Can I have different permissions for different worlds?

Yes, permissions are per-world:

```csharp
var world1 = kernel.CreateWorld("World1");
var world2 = kernel.CreateWorld("World2");

// Different permissions per world
world1.Hooks.AddWritePermission((e, t) => true);
world2.Hooks.AddWritePermission((e, t) => false);
```

### How do I check if a write would be allowed?

Try the write and catch the exception, or check permissions directly (if exposed):

```csharp
try
{
    world.AddComponent(entity, new Health());
    // Write succeeded
}
catch (InvalidOperationException)
{
    // Write was denied
}
```

### Can validators modify values?

No, validators should only validate. Use systems or command buffers to modify values:

```csharp
// ❌ Bad: Modifying in validator
world.Hooks.AddValidator<Health>((health) =>
{
    health.Current = Math.Max(0, health.Current); // Won't work
    return true;
});

// ✅ Good: Use system to clamp
[FixedGroup]
public sealed class HealthClampSystem : ISystem
{
    public void Run(IWorld world, float dt)
    {
        foreach (var (entity, health) in world.Query<Health>())
        {
            if (health.Current < 0)
            {
                using var cmd = world.BeginWrite();
                cmd.ReplaceComponent(entity, new Health { Current = 0, Max = health.Max });
            }
        }
    }
}
```

### How do I debug permission issues?

Enable logging and check write policy:

```csharp
EcsRuntimeOptions.WritePolicy = WriteFailurePolicy.Log;
EcsRuntimeOptions.Log = new DebugLogger();

// Writes will log warnings when denied
```

## See Also

- [Error Handling](./error-handling.md) - Error handling strategy
- [Security](./security.md) - Security considerations
- [World API](./world.md) - World operations
