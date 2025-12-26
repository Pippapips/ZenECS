# 02 - Binding

> Docs / Samples / 02 - Binding

Bind ECS components to Unity GameObjects reactively using the binding system.

## Prerequisites

- **Unity 2021.3+** with ZenECS installed
- **ZenECS Unity Adapter** package
- Basic understanding of [View Binder](../adapter-unity/view-binder.md)

## Run It

1. Open Unity project
2. Open scene: `Packages/com.zenecs.adapter.unity/Samples~/02-Binding/Scene.unity`
3. Press Play
4. Observe GameObjects updating based on ECS components

## Code Walkthrough

### Step 1: Create Position Binder

```csharp
using UnityEngine;
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
```

### Step 2: Link GameObject to Entity

```csharp
using ZenECS.Adapter.Unity.Linking;
using ZenECS.Core;

// Create entity
var world = KernelLocator.CurrentWorld;
Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position(0, 0, 0));
}

// Link GameObject
var link = gameObject.AddComponent<EntityLink>();
link.Attach(world, entity);

// Register binder
var binder = new PositionBinder(transform);
world.RegisterBinder(entity, binder);
```

### Step 3: Update Component

```csharp
// Component changes automatically trigger binder
world.ReplaceComponent(entity, new Position(1, 0, 0));
// PositionBinder.OnDelta() is called automatically
// GameObject position updates
```

## Complete Example

```csharp
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Linking;
using ZenECS.Core;
using ZenECS.Core.Binding;

public class BindingExample : MonoBehaviour
{
    private IWorld _world;
    private Entity _entity;
    
    private void Start()
    {
        // Setup
        var kernel = KernelLocator.Current;
        _world = kernel.CreateWorld(null, "GameWorld", setAsCurrent: true);
        
        // Create entity
        using (var cmd = _world.BeginWrite())
        {
            _entity = cmd.CreateEntity();
            cmd.AddComponent(_entity, new Position(0, 0, 0));
        }
        
        // Link GameObject
        var link = gameObject.AddComponent<EntityLink>();
        link.Attach(_world, _entity);
        
        // Register binder
        var binder = new PositionBinder(transform);
        _world.RegisterBinder(_entity, binder);
    }
    
    private void Update()
    {
        // Update component (binder reacts automatically)
        var pos = _world.Get<Position>(_entity);
        _world.ReplaceComponent(_entity, new Position(
            pos.X + Time.deltaTime,
            pos.Y,
            pos.Z
        ));
    }
}
```

## What to Try Next

### Experiment 1: Multiple Components

Bind multiple components:

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

### Experiment 2: Health Bar

Bind health to UI:

```csharp
public class HealthBarBinder : IBinder<Health>
{
    private readonly Slider _healthBar;
    
    public HealthBarBinder(Slider healthBar)
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

## Next Samples

- **[03 - Messages](../samples/03-messages.md)** - Event-driven architecture
- **[04 - Command Buffer](../samples/04-command-buffer.md)** - Structural changes
- **[View Binder Guide](../adapter-unity/view-binder.md)** - Detailed binding guide

## See Also

- [View Binder](../adapter-unity/view-binder.md) - Binding system details
- [Binding System](../core/binding.md) - Core binding concepts
- [EntityLink API](../adapter-unity/overview.md) - Entity linking
