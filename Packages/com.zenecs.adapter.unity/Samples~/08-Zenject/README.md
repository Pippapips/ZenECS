# ZenECS Adapter Unity — Sample 08: Zenject Integration

This sample demonstrates how to manage ZenECS Kernel and systems using dependency injection with **Zenject**.

* **ProjectInstaller** — Project setup based on Zenject MonoInstaller
* **KernelLocator** — Global Kernel access
* **ISharedContextResolver** — Context resolution via Zenject
* **ISystemPresetResolver** — System instance creation via Zenject
* **Conditional Compilation** — `ZENECS_ZENJECT` define required

---

## What This Sample Shows

1. **ProjectInstaller Setup**
   Use Zenject MonoInstaller to bind Kernel and Resolver to DI container.

2. **Dependency Injection**
   Automatically inject dependencies into systems and Context via Zenject.

3. **Dual Mode**
   Shows behavior differences when Zenject is present and when it's not.

4. **Kernel Injection**
   Demonstrates injecting `IKernel` into MonoBehaviour via `[Inject]` attribute.

5. **EntityBlueprint Integration**
   Shows how to use EntityBlueprint with Zenject-injected Kernel.

6. **Periodic Spawning**
   Demonstrates spawning entities periodically using EntityBlueprint.

7. **OnGUI Display**
   Simple GUI showing Blueprint name and World information.

---

## TL;DR Flow

```
[Zenject SceneContext]
  └─ ProjectInstaller (MonoInstaller)
      └─ InstallBindings()
          ├─ Kernel creation and binding
          ├─ ISharedContextResolver binding
          └─ ISystemPresetResolver binding

[System Creation]
  └─ SystemPresetResolver.Resolve()
      └─ DiContainer.Instantiate<T>() (Zenject mode)
          └─ Automatic dependency injection
```

---

## File Structure

```
08-Zenject/
├── README.md
├── ZenjectSample.cs             # Sample script (contains all components)
│   ├── Health component
│   ├── Position component
│   ├── Rotation component
│   └── EntityBlueprint spawning logic
├── ZenjectSampleInstaller.cs    # Zenject MonoInstaller (optional)
├── EntityBlueprint.asset         # Example EntityBlueprint asset
├── UnityTransformContext.cs      # Example Context
├── UnityTransformContextAsset.cs
├── UnityTransformSyncBinder.cs  # Example Binder
├── UnityTransformSyncBinderAsset.cs
├── Cube.prefab                   # Optional prefab
└── 08 - Zenject.unity            # Sample scene
```

---

## Usage

### 1. ProjectInstaller Setup

1. Add **SceneContext** to Unity scene (Zenject)
2. Add **ProjectInstaller** component to SceneContext
3. Or add `ProjectInstaller` to separate GameObject

### 2. Kernel Injection into MonoBehaviour

```csharp
#if ZENECS_ZENJECT
using Zenject;
using ZenECS.Core;

public class ZenjectSample : MonoBehaviour
{
    private IKernel? _kernel;

    [Inject]
    void Construct(IKernel kernel)
    {
        _kernel = kernel;
    }

    private void Start()
    {
        if (_kernel == null) return;
        var world = _kernel.CreateWorld(null, "GameWorld", setAsCurrent: true);
        // Use world...
    }
}
#endif
```

### 3. System Creation via Zenject

```csharp
#if ZENECS_ZENJECT
using Zenject;
using ZenECS.Adapter.Unity.SystemPresets;

public class MovementSystem : ISystem
{
    private readonly IGameConfig _config;

    // Zenject automatically injects IGameConfig
    public MovementSystem(IGameConfig config)
    {
        _config = config;
    }

    public void Run(IWorld w, float dt)
    {
        // Use _config.GameSpeed
    }
}
#endif
```

### 4. Context Resolver Setup

```csharp
#if ZENECS_ZENJECT
using Zenject;
using ZenECS.Adapter.Unity.Binding.Contexts;

public class GameInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Bind Context to Zenject
        Container.Bind<IGameConfig>().To<GameConfig>().AsSingle();
    }
}
#endif
```

### 5. EntityBlueprint with Zenject

```csharp
#if ZENECS_ZENJECT
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Blueprints;
using ZenECS.Core;

public class ZenjectSample : MonoBehaviour
{
    [SerializeField] private EntityBlueprint? _blueprint;
    private IWorld? _world;
    private IKernel? _kernel;

    [Inject]
    void Construct(IKernel kernel)
    {
        _kernel = kernel;
    }

    private void Start()
    {
        if (_kernel == null) return;
        _world = _kernel.CreateWorld(null, "BlueprintWorld", setAsCurrent: true);
    }

    private void Update()
    {
        if (_world == null || _blueprint == null) return;
        
        // Spawn from Blueprint
        _blueprint.Spawn(
            _world,
            ZenEcsUnityBridge.SharedContextResolver,
            onCreated: entity => Debug.Log($"Entity {entity.Id} spawned!")
        );
    }
}
#endif
```

### 6. Kernel Access

```csharp
var kernel = KernelLocator.Current;
var world = kernel.CreateWorld(null, "GameWorld", setAsCurrent: true);

// SystemPresetResolver is automatically injected via Zenject
var presetResolver = ZenEcsUnityBridge.SystemPresetResolver;
var systems = presetResolver.Resolve(preset);
world.AddSystems(systems);
```

---

## Key APIs

* **ProjectInstaller**: Project setup based on Zenject MonoInstaller
* **KernelLocator.Current**: Access global Kernel
* **ZenEcsUnityBridge.SharedContextResolver**: Context Resolver via Zenject
* **ZenEcsUnityBridge.SystemPresetResolver**: System Preset Resolver via Zenject
* **Conditional Compilation**: `#if ZENECS_ZENJECT` required

---

## Notes and Best Practices

* Zenject integration is provided via **conditional compilation** and requires `ZENECS_ZENJECT` define.
* `ProjectInstaller` should exist only one per scene.
* When Zenject is not available, `ProjectInstaller` works as MonoBehaviour and manually creates Kernel.
* When creating systems, use `DiContainer.Instantiate<T>()` if Zenject is available, otherwise use `Activator.CreateInstance()`.
* Context Resolver also uses DI container in Zenject mode, and uses manual registry in non-Zenject mode.

---

## License

MIT © 2026 Pippapips Limited.
