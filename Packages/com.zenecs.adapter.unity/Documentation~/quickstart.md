# Quick Start (Unity)

Get started with ZenECS Unity Adapter in 5 minutes.

## Prerequisites

- Unity 2021.3+
- `com.zenecs.core` package installed
- `com.zenecs.adapter.unity` package installed

## Step 1: Add EcsDriver

1. Create an empty GameObject in your scene
2. Add the `EcsDriver` component

The kernel is automatically created when `EcsDriver` exists in the scene.

## Step 2: Create a Bootstrap Script

```csharp
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Core;

public class GameBootstrap : MonoBehaviour
{
    private void Awake()
    {
        // Access kernel (automatically created by EcsDriver)
        var kernel = KernelLocator.Current;
        var world = kernel.CreateWorld(null, "GameWorld", setAsCurrent: true);
        
        // Register systems
        world.AddSystems([new MovementSystem()]);
    }
}
```

## Step 3: Define Components

```csharp
public readonly struct Position
{
    public readonly float X, Y;
    public Position(float x, float y) { X = x; Y = y; }
}

public readonly struct Velocity
{
    public readonly float X, Y;
    public Velocity(float x, float y) { X = x; Y = y; }
}
```

## Step 4: Create a System

```csharp
using ZenECS.Core;
using ZenECS.Core.Systems;

[FixedGroup]
public sealed class MovementSystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        using var cmd = w.BeginWrite();
        foreach (var (e, pos, vel) in w.Query<Position, Velocity>())
        {
            cmd.ReplaceComponent(e, new Position(
                pos.X + vel.X * dt,
                pos.Y + vel.Y * dt
            ));
        }
    }
}
```

## Step 5: Create Entities

```csharp
var world = KernelLocator.CurrentWorld;
using (var cmd = world.BeginWrite())
{
    var entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position(0, 0));
    cmd.AddComponent(entity, new Velocity(1, 0));
}
```

## Step 6: Link GameObjects (Optional)

```csharp
using ZenECS.Adapter.Unity.Linking;

// Link GameObject to Entity
var link = gameObject.AddComponent<EntityLink>();
link.Attach(world, entity);
```

## Next Steps

- **Setup Guide**: See `setup.md` for detailed installation and configuration
- **Full Documentation**: https://github.com/Pippapips/ZenECS/tree/main/Docs/adapter-unity
- **Samples**: Check `Samples~` folder in Package Manager

