# Stable IDs

> Docs / Core / Stable IDs

Version-independent component type identification for serialization.

## Overview

Stable IDs are version-independent identifiers for component types used in serialization and networking. They allow components to be identified even when type names change, enabling backward compatibility and data migration.

**Key Concepts:**

- **Version Independence**: IDs don't change when types are renamed
- **Format**: Reverse domain notation (e.g., `com.game.position.v1`)
- **Registration**: Register component types with stable IDs at startup
- **Validation**: Verify consistency between components and formatters

## Why Stable IDs?

### Problem: Type Name Changes

When component types are renamed or moved:

```csharp
// Old version
public struct PlayerPosition { }

// New version (renamed)
public struct Position { }
```

Without stable IDs, snapshots saved with `PlayerPosition` cannot be loaded after renaming to `Position`.

### Solution: Stable IDs

Stable IDs remain constant across type changes:

```csharp
// Register with stable ID
ComponentRegistry.Register<PlayerPosition>("com.game.position.v1");

// Later, even after rename
ComponentRegistry.Register<Position>("com.game.position.v1");  // Same ID!
```

## Stable ID Format

### Recommended Format

Use reverse domain notation with version:

```
com.domain.component.version
```

**Examples:**
- `com.game.position.v1`
- `com.game.health.v2`
- `com.game.player.v1`
- `com.mycompany.weapon.v3`

### Format Guidelines

1. **Reverse Domain**: Start with your domain in reverse (com.yourcompany)
2. **Component Name**: Use lowercase, dot-separated component name
3. **Version**: Include version number (v1, v2, etc.)

**Good Examples:**
```
com.game.position.v1
com.game.health.v2
com.game.player.stats.v1
```

**Bad Examples:**
```
Position          // No domain, no version
position.v1       // No domain
com.game.Position // Mixed case
```

## How It Works

### Registration

Register component types with stable IDs:

```csharp
// Register component type
ComponentRegistry.Register<Position>("com.game.position.v1");

// Or with explicit type
ComponentRegistry.Register("com.game.position.v1", typeof(Position));
```

### Lookup

Resolve types from stable IDs:

```csharp
// Get type from stable ID
if (ComponentRegistry.TryGetType("com.game.position.v1", out var type))
{
    // Use type
}

// Get stable ID from type
if (ComponentRegistry.TryGetId(typeof(Position), out var stableId))
{
    // Use stable ID
}
```

### Formatter Association

Formatters declare which stable ID they handle:

```csharp
[ZenFormatterFor(typeof(Position), "com.game.position.v1", isLatest: true)]
public sealed class PositionFormatter : BinaryComponentFormatter<Position>
{
    // Formatter implementation
}
```

## API Surface

### Component Registry

#### `ComponentRegistry.Register<T>(string stableId)`

Register a component type with a stable ID:

```csharp
ComponentRegistry.Register<Position>("com.game.position.v1");
```

#### `ComponentRegistry.Register(string stableId, Type type)`

Register with explicit type:

```csharp
ComponentRegistry.Register("com.game.position.v1", typeof(Position));
```

#### `ComponentRegistry.TryGetType(string id, out Type? t)`

Resolve type from stable ID:

```csharp
if (ComponentRegistry.TryGetType("com.game.position.v1", out var type))
{
    // Type found
}
```

#### `ComponentRegistry.TryGetId(Type t, out string? id)`

Get stable ID for a type:

```csharp
if (ComponentRegistry.TryGetId(typeof(Position), out var stableId))
{
    // Stable ID found
}
```

#### `ComponentRegistry.ValidateStrictStableIdMatch()`

Validate consistency between components and formatters:

```csharp
// Validate and throw on mismatch
ComponentRegistry.ValidateStrictStableIdMatch(throwOnError: true);

// Validate and log issues
int issues = ComponentRegistry.ValidateStrictStableIdMatch(
    throwOnError: false,
    log: msg => Console.WriteLine(msg)
);
```

### Attributes

#### `ZenComponentAttribute`

Editor-only attribute for component metadata:

```csharp
[ZenComponent(StableId = "com.game.position.v1")]
public struct Position
{
    public float X, Y, Z;
}
```

**Note:** This attribute is only available in Unity Editor builds.

#### `ZenFormatterForAttribute`

Editor-only attribute for formatter registration:

```csharp
[ZenFormatterFor(typeof(Position), "com.game.position.v1", isLatest: true)]
public sealed class PositionFormatter : BinaryComponentFormatter<Position>
{
    // Formatter implementation
}
```

## Examples

### Basic Registration

```csharp
// Register components at startup
public void RegisterComponents()
{
    ComponentRegistry.Register<Position>("com.game.position.v1");
    ComponentRegistry.Register<Velocity>("com.game.velocity.v1");
    ComponentRegistry.Register<Health>("com.game.health.v1");
}
```

### Version Management

```csharp
// Version 1
ComponentRegistry.Register<PositionV1>("com.game.position.v1");

// Version 2 (new component type)
ComponentRegistry.Register<PositionV2>("com.game.position.v2");

// Both can coexist for migration
```

### Formatter Registration with Stable ID

```csharp
// Register component
ComponentRegistry.Register<Position>("com.game.position.v1");

// Register formatter with same stable ID
var formatter = new PositionFormatter();
ComponentRegistry.RegisterFormatter(formatter, "com.game.position.v1");

// Validate consistency
ComponentRegistry.ValidateStrictStableIdMatch();
```

### Migration Scenario

```csharp
// Old component (V1)
public struct PositionV1
{
    public float X, Y;
}

// New component (V2)
public struct PositionV2
{
    public float X, Y, Z;
}

// Register both with same stable ID for migration
ComponentRegistry.Register<PositionV1>("com.game.position.v1");
ComponentRegistry.Register<PositionV2>("com.game.position.v2");

// Migration handles V1 → V2 conversion
public sealed class PositionMigration : IPostLoadMigration
{
    public void Run(IWorld world)
    {
        using var cmd = world.BeginWrite();
        
        foreach (var (entity, posV1) in world.Query<PositionV1>())
        {
            cmd.AddComponent(entity, new PositionV2(posV1.X, posV1.Y, 0));
            cmd.RemoveComponent<PositionV1>(entity);
        }
    }
}
```

### Validation

```csharp
// Validate at startup
public void ValidateSerializationSetup()
{
    int issues = ComponentRegistry.ValidateStrictStableIdMatch(
        throwOnError: false,
        log: msg => Console.WriteLine($"Validation: {msg}")
    );
    
    if (issues > 0)
    {
        Console.WriteLine($"Found {issues} validation issues");
        // Handle issues
    }
}
```

## Best Practices

### 1. Use Reverse Domain Notation

```csharp
// ✅ Good: Reverse domain notation
ComponentRegistry.Register<Position>("com.game.position.v1");

// ❌ Bad: Simple name
ComponentRegistry.Register<Position>("Position");
```

### 2. Include Version Numbers

```csharp
// ✅ Good: With version
ComponentRegistry.Register<Position>("com.game.position.v1");

// ❌ Bad: No version
ComponentRegistry.Register<Position>("com.game.position");
```

### 3. Keep IDs Stable

Once assigned, never change stable IDs:

```csharp
// ✅ Good: Keep same ID across versions
ComponentRegistry.Register<PositionV1>("com.game.position.v1");
ComponentRegistry.Register<PositionV2>("com.game.position.v2");  // New version, new ID

// ❌ Bad: Reuse ID for different types
ComponentRegistry.Register<Position>("com.game.position.v1");
ComponentRegistry.Register<NewPosition>("com.game.position.v1");  // Wrong!
```

### 4. Validate at Startup

Always validate stable ID consistency:

```csharp
// ✅ Good: Validate at startup
public void Bootstrap()
{
    RegisterComponents();
    RegisterFormatters();
    ComponentRegistry.ValidateStrictStableIdMatch();
}
```

### 5. Use Consistent Naming

Follow a consistent naming scheme:

```csharp
// ✅ Good: Consistent pattern
ComponentRegistry.Register<Position>("com.game.position.v1");
ComponentRegistry.Register<Velocity>("com.game.velocity.v1");
ComponentRegistry.Register<Health>("com.game.health.v1");
```

## Versioning Strategy

### Increment Version on Breaking Changes

When component structure changes significantly:

```csharp
// V1: 2D position
ComponentRegistry.Register<PositionV1>("com.game.position.v1");

// V2: 3D position (breaking change)
ComponentRegistry.Register<PositionV2>("com.game.position.v2");
```

### Keep Same Version for Compatible Changes

For backward-compatible changes, keep the same version:

```csharp
// V1: Original
public struct PositionV1
{
    public float X, Y;
}

// V1.1: Added optional field (compatible)
public struct PositionV1_1
{
    public float X, Y;
    public float Z;  // Optional, defaults to 0
}

// Use same stable ID with migration
ComponentRegistry.Register<PositionV1>("com.game.position.v1");
ComponentRegistry.Register<PositionV1_1>("com.game.position.v1");
```

## Migration Patterns

### Pattern 1: Type Rename

```csharp
// Old type
ComponentRegistry.Register<OldName>("com.game.component.v1");

// New type (same ID)
ComponentRegistry.Register<NewName>("com.game.component.v1");

// Migration
public sealed class RenameMigration : IPostLoadMigration
{
    public void Run(IWorld world)
    {
        using var cmd = world.BeginWrite();
        foreach (var (entity, old) in world.Query<OldName>())
        {
            cmd.AddComponent(entity, new NewName(old.Value));
            cmd.RemoveComponent<OldName>(entity);
        }
    }
}
```

### Pattern 2: Field Addition

```csharp
// V1: 2 fields
public struct PositionV1 { public float X, Y; }

// V2: 3 fields (added Z)
public struct PositionV2 { public float X, Y, Z; }

// Migration
public sealed class AddFieldMigration : IPostLoadMigration
{
    public void Run(IWorld world)
    {
        using var cmd = world.BeginWrite();
        foreach (var (entity, posV1) in world.Query<PositionV1>())
        {
            cmd.AddComponent(entity, new PositionV2(posV1.X, posV1.Y, 0));
            cmd.RemoveComponent<PositionV1>(entity);
        }
    }
}
```

### Pattern 3: Field Removal

```csharp
// V1: 3 fields
public struct PositionV1 { public float X, Y, Z; }

// V2: 2 fields (removed Z)
public struct PositionV2 { public float X, Y; }

// Migration
public sealed class RemoveFieldMigration : IPostLoadMigration
{
    public void Run(IWorld world)
    {
        using var cmd = world.BeginWrite();
        foreach (var (entity, posV1) in world.Query<PositionV1>())
        {
            cmd.AddComponent(entity, new PositionV2(posV1.X, posV1.Y));
            cmd.RemoveComponent<PositionV1>(entity);
        }
    }
}
```

## Validation

### Strict Validation

Validate that all formatters have matching stable IDs:

```csharp
// Validate and throw on mismatch
try
{
    ComponentRegistry.ValidateStrictStableIdMatch(throwOnError: true);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}
```

### Non-Strict Validation

Validate and log issues without throwing:

```csharp
int issues = ComponentRegistry.ValidateStrictStableIdMatch(
    throwOnError: false,
    log: msg => Console.WriteLine($"Issue: {msg}")
);

if (issues > 0)
{
    Console.WriteLine($"Found {issues} validation issues");
}
```

## Common Patterns

### Pattern 1: Centralized Registration

```csharp
public static class ComponentRegistration
{
    public static void RegisterAll()
    {
        // Core components
        ComponentRegistry.Register<Position>("com.game.position.v1");
        ComponentRegistry.Register<Velocity>("com.game.velocity.v1");
        ComponentRegistry.Register<Health>("com.game.health.v1");
        
        // Game-specific components
        ComponentRegistry.Register<Player>("com.game.player.v1");
        ComponentRegistry.Register<Enemy>("com.game.enemy.v1");
        
        // Validate
        ComponentRegistry.ValidateStrictStableIdMatch();
    }
}
```

### Pattern 2: Attribute-Based (Unity Editor)

```csharp
// In Unity Editor, attributes are processed automatically
[ZenComponent(StableId = "com.game.position.v1")]
public struct Position
{
    public float X, Y, Z;
}

[ZenFormatterFor(typeof(Position), "com.game.position.v1", isLatest: true)]
public sealed class PositionFormatter : BinaryComponentFormatter<Position>
{
    // Implementation
}
```

### Pattern 3: Versioned Components

```csharp
// Register all versions
ComponentRegistry.Register<PositionV1>("com.game.position.v1");
ComponentRegistry.Register<PositionV2>("com.game.position.v2");
ComponentRegistry.Register<PositionV3>("com.game.position.v3");

// Formatters for each version
ComponentRegistry.RegisterFormatter(new PositionV1Formatter(), "com.game.position.v1");
ComponentRegistry.RegisterFormatter(new PositionV2Formatter(), "com.game.position.v2");
ComponentRegistry.RegisterFormatter(new PositionV3Formatter(), "com.game.position.v3");
```

## FAQ

### Do I need stable IDs for all components?

Only if you plan to serialize components or use networking. Components that are never serialized don't need stable IDs.

### Can I change a stable ID?

No. Stable IDs should never change once assigned. Create a new version with a new ID instead.

### What happens if I don't register a stable ID?

Components without stable IDs cannot be serialized. You'll get a `NotSupportedException` when trying to save snapshots.

### How do I handle component renames?

1. Keep the same stable ID
2. Register the new type with the old stable ID
3. Create a migration to transform old → new

### Can multiple types share the same stable ID?

No. Each component type should have a unique stable ID. Use version numbers to distinguish versions of the same logical component.

### How do I choose a domain for stable IDs?

Use your organization's reverse domain:
- Personal projects: `com.yourname`
- Company projects: `com.companyname`
- Open source: `org.projectname`

## See Also

- [Serialization](./serialization.md) - Serialization system
- [Migration Guide](./migration-postmig.md) - Data migrations
- [Security](./security.md) - Serialization security
