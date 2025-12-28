# Binding

> Docs / Core / Binding

Connect ECS data to view layer (Unity GameObjects, UI, etc.) reactively using the binding system.

## Overview

Binding enables:

- **Reactive Updates**: Views update automatically when components change
- **Clean Separation**: View layer doesn't write to ECS
- **ComponentDelta**: Change notifications (Added/Changed/Removed)
- **Type-Safe**: Compile-time type checking

### Key Concepts

- **IBinder**: Reactive component change handler
- **ComponentDelta**: Change notification structure
- **Context**: Shared or per-entity view data
- **Binding Router**: Manages binders and applies changes

## How It Works

### Binding Flow

```
Component Change
    ↓
ComponentDelta Generated
    ↓
IBinder.OnDelta() Called
    ↓
View Updated
```

### IBinder Interface

```csharp
public interface IBinder<T> where T : struct
{
    void OnDelta(ComponentDelta<T> delta);
}
```

### ComponentDelta Structure

```csharp
public struct ComponentDelta<T>
{
    public bool IsAdded { get; }
    public bool IsChanged { get; }
    public bool IsRemoved { get; }
    public T? OldValue { get; }
    public T NewValue { get; }
}
```

## API Surface

### Registering Binders

```csharp
// Register binder for entity
world.RegisterBinder(entity, new PositionBinder(transform));

// Unregister binder
world.UnregisterBinder(entity, binder);
```

### Binder Implementation

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
        
        if (delta.IsRemoved)
        {
            // Handle removal
        }
    }
}
```

## Examples

### Basic Binder

```csharp
using ZenECS.Core;
using ZenECS.Core.Binding;

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

// Usage
var world = kernel.CreateWorld(null, "GameWorld");
Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position(0, 0, 0));
}

var binder = new PositionBinder(transform);
world.RegisterBinder(entity, binder);

// Component changes automatically trigger binder
world.ReplaceComponent(entity, new Position(1, 0, 0));
// PositionBinder.OnDelta() called automatically
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

- **Use binders for views**: Connect ECS to presentation
- **Handle all deltas**: Added, Changed, Removed
- **Keep binders focused**: One responsibility
- **Register properly**: Use world.RegisterBinder()

### ❌ Don't

- **Don't mutate components**: Binders are read-only
- **Don't create entities**: Use messages instead
- **Don't access GameObjects directly**: Use EntityViewRegistry
- **Don't store entity references**: Use EntityLink

## See Also

- [View Binder](../adapter-unity/view-binder.md) - Unity integration
- [EntityLink](../adapter-unity/overview.md) - GameObject linking
- [World](./world.md) - World API
