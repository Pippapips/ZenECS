# View Binder

> Docs / Adapter (Unity) / View Binder

Connect ECS components to Unity GameObjects reactively using the binding system.

## Overview

View binders automatically update Unity GameObjects when ECS components change. This enables clean separation between simulation (ECS) and presentation (Unity).

### Key Concepts

- **EntityLink**: Connects GameObject to ECS entity
- **IBinder**: Reactive component change handler
- **ComponentDelta**: Change notifications (Added/Changed/Removed)
- **EntityViewRegistry**: Lookup registry for entity-to-GameObject mapping

## How It Works

### Binding Flow

```
ECS Component Change
    ↓
ComponentDelta Generated
    ↓
IBinder.OnDelta() Called
    ↓
GameObject Updated
```

### EntityLink

`EntityLink` maintains the connection:

```csharp
var link = gameObject.AddComponent<EntityLink>();
link.Attach(world, entity);
```

### IBinder Interface

Binders implement `IBinder<T>`:

```csharp
public interface IBinder<T> where T : struct
{
    void OnDelta(ComponentDelta<T> delta);
}
```

## Usage

### Basic Binder

```csharp
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Binding;

public class PositionBinder : IBinder<Position>
{
    private Transform _transform;
    
    public PositionBinder(Transform transform)
    {
        _transform = transform;
    }
    
    public void OnDelta(ComponentDelta<Position> delta)
    {
        if (delta.IsAdded || delta.IsChanged)
        {
            var pos = delta.NewValue;
            _transform.position = new Vector3(pos.X, pos.Y, pos.Z);
        }
        
        if (delta.IsRemoved)
        {
            // Handle removal
        }
    }
}
```

### Registering Binders

```csharp
var world = KernelLocator.CurrentWorld;
Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();
}
var link = gameObject.AddComponent<EntityLink>();
link.Attach(world, entity);

// Register binder
var binder = new PositionBinder(transform);
world.RegisterBinder(entity, binder);
```

### EntityViewRegistry

Lookup GameObjects from entities:

```csharp
// Find GameObject from entity
var link = EntityViewRegistry.TryGetLink(world, entity);
if (link != null)
{
    var gameObject = link.gameObject;
    // Use GameObject
}

// Find entity from GameObject
var link = gameObject.GetComponent<EntityLink>();
if (link != null && link.IsAlive)
{
    var entity = link.Entity;
    // Use entity
}
```

## Examples

### Position Binder

```csharp
public class PositionBinder : IBinder<Position>
{
    private readonly Transform _transform;
    
    public PositionBinder(Transform transform)
    {
        _transform = transform;
    }
    
    public void OnDelta(ComponentDelta<Position> delta)
    {
        if (delta.IsAdded || delta.IsChanged)
        {
            var pos = delta.NewValue;
            _transform.position = new Vector3(pos.X, pos.Y, pos.Z);
        }
    }
}
```

### Health Bar Binder

```csharp
public class HealthBinder : IBinder<Health>
{
    private readonly Slider _healthBar;
    
    public HealthBinder(Slider healthBar)
    {
        _healthBar = healthBar;
    }
    
    public void OnDelta(ComponentDelta<Health> delta)
    {
        if (delta.IsAdded || delta.IsChanged)
        {
            var health = delta.NewValue;
            _healthBar.value = health.Current / health.Max;
        }
    }
}
```

### Multiple Component Binder

```csharp
public class TransformBinder : IBinder<Position>, IBinder<Rotation>
{
    private readonly Transform _transform;
    
    public TransformBinder(Transform transform)
    {
        _transform = transform;
    }
    
    public void OnDelta(ComponentDelta<Position> delta)
    {
        if (delta.IsAdded || delta.IsChanged)
        {
            var pos = delta.NewValue;
            _transform.position = new Vector3(pos.X, pos.Y, pos.Z);
        }
    }
    
    public void OnDelta(ComponentDelta<Rotation> delta)
    {
        if (delta.IsAdded || delta.IsChanged)
        {
            var rot = delta.NewValue;
            _transform.rotation = Quaternion.Euler(rot.X, rot.Y, rot.Z);
        }
    }
}
```

## Best Practices

### ✅ Do

- **Use EntityLink**: Maintain GameObject-entity connection
- **Register binders**: Use `world.RegisterBinder()`
- **Handle all deltas**: Added, Changed, Removed
- **Use EntityViewRegistry**: For entity-to-GameObject lookup

### ❌ Don't

- **Don't access GameObjects directly**: Use EntityViewRegistry
- **Don't mutate components in binders**: Use messages instead
- **Don't store entity references**: Use EntityLink
- **Don't create entities in binders**: Use external commands

## Advanced Patterns

### Binder Factory

Create binders from ScriptableObject assets:

```csharp
[CreateAssetMenu(menuName = "ZenECS/Binder Asset")]
public class PositionBinderAsset : BinderAsset
{
    public override IBinder CreateBinder(Entity entity, IWorld world)
    {
        var link = EntityViewRegistry.TryGetLink(world, entity);
        if (link != null)
        {
            return new PositionBinder(link.transform);
        }
        return null;
    }
}
```

### Conditional Binding

Only bind when conditions are met:

```csharp
public void OnDelta(ComponentDelta<Position> delta)
{
    if (delta.IsAdded || delta.IsChanged)
    {
        // Only update if entity is visible
        if (ShouldUpdate(delta.Entity))
        {
            UpdateTransform(delta.NewValue);
        }
    }
}
```

## See Also

- [Unity Adapter Overview](./overview.md) - Feature overview
- [Binding System](../core/binding.md) - Core binding concepts
- [Entity Blueprints](./view-binder.md) - Data-driven entities
