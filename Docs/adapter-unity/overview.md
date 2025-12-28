# Unity Adapter Overview

> Docs / Adapter (Unity) / Unity Adapter overview

The **ZenECS Adapter for Unity** is an optional integration layer that seamlessly connects ZenECS Core with the Unity engine. It provides MonoBehaviour-based lifecycle management, editor tools, and Unity-specific data-driven workflows.

## Overview

The Unity Adapter bridges ZenECS Core with Unity's ecosystem:

- **Lifecycle Management**: Automatic kernel and world management via `EcsDriver`
- **GameObject Integration**: Connect Unity GameObjects to ECS entities with `EntityLink`
- **Data-Driven Workflows**: ScriptableObject-based entity blueprints and system presets
- **Editor Tools**: Custom inspectors, ECS Explorer window, and debugging utilities
- **Optional Integrations**: Zenject DI and UniRx reactive programming support

### Key Components

#### EcsDriver

MonoBehaviour that manages kernel lifecycle and bridges Unity's frame callbacks:

```csharp
// Automatically created when EcsDriver exists in scene
var kernel = KernelLocator.Current;
var world = kernel.CreateWorld(null, "GameWorld");
```

#### EntityLink

MonoBehaviour that links Unity GameObjects to ECS entities:

```csharp
var link = gameObject.AddComponent<EntityLink>();
link.Attach(world, entity);
```

#### EntityBlueprint

ScriptableObject asset for data-driven entity spawning:

```csharp
[CreateAssetMenu(menuName = "ZenECS/Entity Blueprint")]
public class PlayerBlueprint : EntityBlueprint { }

// Spawn at runtime
blueprint.Spawn(world, sharedContextResolver);
```

## How It Works

### Architecture

```
Unity Layer (GameObjects, MonoBehaviours)
    ↓
Unity Adapter (EcsDriver, EntityLink, Blueprints)
    ↓
ZenECS Core (Kernel, World, Systems)
```

### Frame Bridge

`EcsDriver` automatically bridges Unity's frame callbacks:

```csharp
void Update()      → kernel.BeginFrame(deltaTime)
void FixedUpdate() → kernel.FixedStep(fixedDeltaTime)
void LateUpdate()  → kernel.LateFrame(deltaTime, alpha)
```

### Entity Linking

`EntityLink` maintains bidirectional connection:

- **ECS → Unity**: Query entity, get linked GameObject
- **Unity → ECS**: GameObject has EntityLink, access entity

## API Surface

### Core Classes

#### EcsDriver

```csharp
public sealed class EcsDriver : MonoBehaviour
{
    public IKernel? Kernel { get; }
    public IKernel CreateKernel(KernelOptions? options = null);
}
```

#### EntityLink

```csharp
public sealed class EntityLink : MonoBehaviour
{
    public IWorld? World { get; }
    public Entity Entity { get; }
    public bool IsAlive { get; }
    
    public void Attach(IWorld world, Entity entity);
    public void Detach();
}
```

#### EntityBlueprint

```csharp
public sealed class EntityBlueprint : ScriptableObject
{
    public EntityBlueprintData Data { get; }
    
    public void Spawn(
        IWorld world,
        ISharedContextResolver? sharedContextResolver = null,
        Action<Entity>? onCreated = null
    );
}
```

### Helper Classes

#### KernelLocator

Static access to current kernel:

```csharp
public static class KernelLocator
{
    public static IKernel Current { get; }
    public static IWorld? CurrentWorld { get; }
    public static bool TryGetCurrent(out IKernel? kernel);
}
```

#### EntityViewRegistry

Per-world registry for entity-to-GameObject lookup:

```csharp
public static class EntityViewRegistry
{
    public static EntityLink? TryGetLink(IWorld world, Entity entity);
    public static void Register(IWorld world, Entity entity, EntityLink link);
    public static void Unregister(IWorld world, Entity entity);
}
```

## Examples

### Basic Setup

```csharp
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Core;

public class GameBootstrap : MonoBehaviour
{
    private void Awake()
    {
        // Kernel is automatically created by EcsDriver
        var kernel = KernelLocator.Current;
        var world = kernel.CreateWorld(null, "GameWorld", setAsCurrent: true);
        
        // Register systems
        world.AddSystems([new MovementSystem(), new HealthSystem()]);
    }
}
```

### Linking GameObjects

```csharp
using ZenECS.Adapter.Unity.Linking;
using ZenECS.Core;

// Create entity
var world = KernelLocator.CurrentWorld;
Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position(0, 0));
}

// Link GameObject
var link = gameObject.AddComponent<EntityLink>();
link.Attach(world, entity);

// Later: Find GameObject from entity
var linkedObject = EntityViewRegistry.TryGetLink(world, entity)?.gameObject;
```

### Spawning with Blueprints

```csharp
using ZenECS.Adapter.Unity.Blueprints;

// Create blueprint asset in editor
// Then spawn at runtime:
blueprint.Spawn(
    KernelLocator.CurrentWorld,
    ZenEcsUnityBridge.SharedContextResolver,
    onCreated: entity => Debug.Log($"Spawned: {entity.Id}")
);
```

## Best Practices

### ✅ Do

- **Use EcsDriver**: Let it manage kernel lifecycle
- **Use EntityLink**: Connect GameObjects to entities
- **Use Blueprints**: Data-driven entity configuration
- **Use Command Buffers**: For structural changes

### ❌ Don't

- **Don't create Kernel manually**: Use EcsDriver
- **Don't store entity references**: Use EntityLink
- **Don't access GameObjects directly**: Use EntityViewRegistry
- **Don't mix Update and FixedUpdate**: Use appropriate system groups

## Integration Points

### Zenject (Optional)

If Zenject is installed, systems can be resolved via DI:

```csharp
#if ZENECS_ZENJECT
using Zenject;

public class ProjectInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<ISystemPresetResolver>().To<SystemPresetResolver>().AsSingle();
    }
}
#endif
```

### UniRx (Optional)

If UniRx is installed, message streams become observables:

```csharp
#if ZENECS_UNIRX
using UniRx;
using ZenECS.Adapter.Unity.UniRx;

// Convert message stream to observable
world.ObserveMessages<DamageMessage>()
    .Subscribe(msg => Debug.Log($"Damage: {msg.Amount}"));
#endif
```

## See Also

- [Setup Guide](./setup.md) - Installation and configuration
- [View Binder](./view-binder.md) - Reactive view updates
- [Input → Intent](./input-intent.md) - Input handling patterns
- [Troubleshooting](./troubleshooting.md) - Common issues
