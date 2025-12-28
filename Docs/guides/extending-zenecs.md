# Extending ZenECS

> Docs / Guides / Extending ZenECS

How to extend ZenECS with custom functionality, hooks, validators, and formatters.

## Overview

ZenECS is designed for extensibility:

- **Custom Systems**: Implement `ISystem` interface
- **Custom Hooks**: Add write permissions and validators
- **Custom Formatters**: Implement custom serialization
- **Custom Bindings**: Create view binders
- **Adapter Pattern**: Extend for other engines

## Custom Systems

### Basic System

```csharp
using ZenECS.Core;
using ZenECS.Core.Systems;

[FixedGroup]
public sealed class MyCustomSystem : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        using var cmd = world.BeginWrite();
        
        foreach (var (entity, component) in world.Query<Component>())
        {
            // Custom logic
        }
    }
}
```

### System with Dependencies

```csharp
public sealed class MySystem : ISystem
{
    private readonly IMyService _service;
    
    public MySystem(IMyService service)
    {
        _service = service;
    }
    
    public void Run(IWorld world, float deltaTime)
    {
        // Use service
        _service.DoSomething();
    }
}
```

## Custom Hooks

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

## Custom Formatters

### Component Formatter

Implement custom serialization:

```csharp
public class JsonComponentFormatter : IComponentFormatter
{
    public void WriteComponent(Stream stream, Type componentType, object component)
    {
        var json = JsonUtility.ToJson(component);
        var bytes = Encoding.UTF8.GetBytes(json);
        stream.Write(bytes, 0, bytes.Length);
    }
    
    public object ReadComponent(Stream stream, Type componentType)
    {
        var bytes = new byte[stream.Length];
        stream.Read(bytes, 0, bytes.Length);
        var json = Encoding.UTF8.GetString(bytes);
        return JsonUtility.FromJson(json, componentType);
    }
}

// Use custom formatter
world.Save(stream, new JsonComponentFormatter());
```

## Custom Bindings

### Binder Implementation

Create custom view binders:

```csharp
public class MyBinder : IBinder<Position>
{
    private readonly Transform _transform;
    
    public MyBinder(Transform transform)
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

// Register binder
world.RegisterBinder(entity, new MyBinder(transform));
```

## Adapter Pattern

### Engine Adapter

Extend ZenECS for other engines:

```csharp
// Example: Godot adapter
public class GodotEcsDriver : Node
{
    private IKernel _kernel;
    
    public override void _Ready()
    {
        _kernel = new Kernel();
        var world = _kernel.CreateWorld(null, "GameWorld");
        world.AddSystems([new MovementSystem()]);
    }
    
    public override void _Process(float delta)
    {
        _kernel.BeginFrame(delta);
    }
    
    public override void _PhysicsProcess(float delta)
    {
        _kernel.FixedStep(delta);
    }
}
```

## Extension Points

### Service Container Extension

Extend DI container:

```csharp
// Create child scope
var extendedScope = worldScope.CreateChildScope();

// Register custom services
extendedScope.RegisterFactory<IMyService>(
    _ => new MyService(),
    asSingleton: true
);

// Use in systems
var service = extendedScope.Resolve<IMyService>();
```

### System Group Extension

Add custom system groups:

```csharp
// 1. Extend SystemGroup enum (requires source access)
public enum SystemGroup
{
    // ... existing groups
    CustomPhase
}

// 2. Add execution in SystemRunner
public void CustomPhase(IWorld world, float deltaTime)
{
    RunGroup(SystemGroup.CustomPhase, world, deltaTime);
}
```

## Best Practices

### ✅ Do

- **Follow interfaces**: Implement required interfaces
- **Document extensions**: Explain custom functionality
- **Test thoroughly**: Verify extensions work correctly
- **Keep optional**: Don't break core functionality

### ❌ Don't

- **Don't modify core**: Extend, don't modify
- **Don't break contracts**: Follow interface contracts
- **Don't add dependencies**: Keep extensions optional

## See Also

- [Advanced Topics](./advanced-topics.md) - Advanced patterns
- [Architecture](../overview/architecture.md) - System design
- [Hooks & Validators](../core/write-hooks-validators.md) - Hook system
