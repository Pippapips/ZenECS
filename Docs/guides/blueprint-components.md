# Blueprint Components

> Docs / Guides / Blueprint components

Use EntityBlueprint to create data-driven entity configurations with component snapshots, contexts, and binders.

## Overview

**EntityBlueprint** is a ScriptableObject asset that stores:

- **Component Snapshots**: Serialized component data as JSON
- **Context Assets**: Shared and per-entity context configurations
- **Binder Assets**: View binding configurations

Blueprints enable **data-driven entity spawning** without code changes.

## How It Works

### Blueprint Structure

```
EntityBlueprint
├── Component Snapshot (EntityBlueprintData)
│   ├── Position { X: 0, Y: 0 }
│   ├── Velocity { X: 1, Y: 0 }
│   └── Health { Current: 100, Max: 100 }
├── Context Assets
│   ├── SharedContextAsset (UI Root)
│   └── PerEntityContextAsset (Audio Source)
└── Binder Assets
    ├── PositionBinderAsset
    └── HealthBinderAsset
```

### Spawning Process

1. **Create Entity**: Via external command
2. **Apply Snapshot**: Deserialize and attach components
3. **Register Contexts**: Create and register contexts
4. **Create Binders**: Instantiate and attach binders

## Creating Blueprints

### Step 1: Create Asset

1. **Project Window** → Right-click
2. **Create** → **ZenECS** → **Entity Blueprint**
3. Name the asset (e.g., "PlayerBlueprint")

### Step 2: Configure Components

In the Blueprint inspector:

1. Expand **Components (snapshot)** section
2. Click **+** to add component entries
3. Select component type
4. Configure component values

**Supported Types:**
- Primitive types (int, float, bool, string)
- Unity types (Vector2, Vector3, Color)
- Unity.Mathematics types (float2, float3, quaternion)
- Custom struct components

### Step 3: Add Contexts (Optional)

1. Expand **Contexts (ScriptableObject assets)** section
2. Drag context assets into the list
3. Configure shared or per-entity contexts

### Step 4: Add Binders (Optional)

1. Expand **Binders (ScriptableObject assets)** section
2. Drag binder assets into the list
3. Binders will be created when entity spawns

## Usage

### Spawning from Blueprint

```csharp
using ZenECS.Adapter.Unity.Blueprints;

// Spawn entity
blueprint.Spawn(
    KernelLocator.CurrentWorld,
    ZenEcsUnityBridge.SharedContextResolver,
    onCreated: entity => Debug.Log($"Spawned: {entity.Id}")
);
```

### Spawning with EntityLink

```csharp
// Spawn and link to GameObject
var world = KernelLocator.CurrentWorld;
blueprint.Spawn(world, sharedContextResolver, entity =>
{
    var link = gameObject.AddComponent<EntityLink>();
    link.Attach(world, entity);
});
```

## Component Serialization

### JSON Format

Components are serialized as JSON:

```json
{
  "type": "Position",
  "data": {
    "X": 0.0,
    "Y": 0.0,
    "Z": 0.0
  }
}
```

### Supported Types

**Primitive Types:**
- `int`, `float`, `double`, `bool`, `string`
- `byte`, `short`, `long`, `uint`, `ushort`, `ulong`

**Unity Types:**
- `Vector2`, `Vector3`, `Vector4`
- `Color`, `Quaternion`
- `Rect`, `Bounds`

**Unity.Mathematics:**
- `float2`, `float3`, `float4`
- `int2`, `int3`, `int4`
- `quaternion`

**Custom Structs:**
- Any `struct` with public fields
- Nested structs supported

## Examples

### Basic Blueprint

**PlayerBlueprint** asset:
- Components: `Position`, `Velocity`, `Health`
- Contexts: None
- Binders: `PositionBinderAsset`

**Usage:**
```csharp
playerBlueprint.Spawn(world, sharedContextResolver);
```

### Complex Blueprint

**EnemyBlueprint** asset:
- Components: `Position`, `Velocity`, `Health`, `AIState`
- Contexts: `EnemyAudioContext` (per-entity)
- Binders: `PositionBinderAsset`, `HealthBarBinderAsset`

**Usage:**
```csharp
enemyBlueprint.Spawn(world, sharedContextResolver, entity =>
{
    // Additional setup after spawning
    world.Publish(new EnemySpawnedMessage { Entity = entity });
});
```

## Best Practices

### ✅ Do

- **Use blueprints for common entities**: Players, enemies, items
- **Organize by type**: Group related blueprints in folders
- **Test in editor**: Verify blueprints before runtime
- **Version snapshots**: Keep backup of blueprint data

### ❌ Don't

- **Don't store references**: Blueprints are data, not code
- **Don't over-complicate**: Keep blueprints focused
- **Don't duplicate**: Reuse blueprints with variations

## Advanced Patterns

### Blueprint Inheritance

Create base blueprints and extend:

```csharp
// Base blueprint: Common components
var baseBlueprint = CreateAsset<EntityBlueprint>("BaseEntity");
baseBlueprint.Data.AddComponent(new Position(0, 0));
baseBlueprint.Data.AddComponent(new Health(100, 100));

// Extended blueprint: Add specific components
var enemyBlueprint = CreateAsset<EntityBlueprint>("Enemy");
enemyBlueprint.Data = baseBlueprint.Data.Clone();
enemyBlueprint.Data.AddComponent(new AIState());
```

### Runtime Blueprint Creation

Create blueprints at runtime:

```csharp
var blueprint = ScriptableObject.CreateInstance<EntityBlueprint>();
blueprint.Data.AddComponent(new Position(0, 0));
blueprint.Data.AddComponent(new Velocity(1, 0));

// Spawn immediately
blueprint.Spawn(world, sharedContextResolver);
```

## Troubleshooting

### Components Not Serializing

**Issue:** Component not appearing in snapshot

**Solutions:**
- Check component is a `struct`
- Verify public fields exist
- Check type is supported
- Review serialization format

### Blueprint Spawn Fails

**Issue:** Entity not created

**Solutions:**
- Verify world is active
- Check shared context resolver is configured
- Review component types are valid
- Check for serialization errors

## See Also

- [Entity Blueprint API](../adapter-unity/overview.md) - API reference
- [View Binder](../adapter-unity/view-binder.md) - Binder system
- [Binding System](../core/binding.md) - Core binding concepts
