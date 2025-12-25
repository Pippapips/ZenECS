# ZenECS Adapter Unity — Sample 01: EcsDriver Basic Setup

This sample demonstrates how to initialize ZenECS Kernel using **EcsDriver** and integrate it with Unity's lifecycle.

* **EcsDriver** — MonoBehaviour-based Kernel lifecycle management
* **KernelLocator** — Global Kernel access
* Unity frame callbacks and ECS frame structure integration

---

## What This Sample Shows

1. **EcsDriver Setup**
   Add `EcsDriver` component to the scene to automatically create and manage Kernel.

2. **World Creation and System Registration**
   Access Kernel via `KernelLocator.Current`, create World, and register systems.

3. **Unity Lifecycle Integration**
   `EcsDriver` automatically converts Unity's `Update`, `FixedUpdate`, `LateUpdate` to ECS's `BeginFrame`, `FixedStep`, `LateFrame`.

4. **System Groups**
   Demonstrates both `FixedGroup` (MovementSystem) and `FrameViewGroup` (PrintPositionSystem) systems.

5. **Test Entity Creation**
   Shows how to create test entities with components using `ExternalCommand`.

---

## TL;DR Flow

```
[Unity Scene]
  └─ EcsDriver (MonoBehaviour)
      └─ Kernel creation and registration to KernelLocator
          └─ Update() → BeginFrame()
          └─ FixedUpdate() → FixedStep()
          └─ LateUpdate() → LateFrame()

[Bootstrap Script]
  └─ KernelLocator.Current to access Kernel
      └─ CreateWorld() to create World
      └─ AddSystems() to register systems
```

---

## File Structure

```
01-EcsDriver/
├── README.md
├── EcsDriverSample.cs          # Bootstrap script (contains all components and systems)
│   ├── Position component
│   ├── Velocity component
│   ├── MovementSystem (FixedGroup)
│   └── PrintPositionSystem (FrameViewGroup)
└── 01 - EcsDriver.unity        # Sample scene
```

---

## Usage

### 1. Scene Setup

1. Open Unity scene.
2. Create an empty GameObject and name it "EcsDriver".
3. Add `EcsDriver` component (automatically added or manually).

### 2. Add Bootstrap Script

Create a new GameObject and add `EcsDriverSample` script:

```csharp
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Core;

public class EcsDriverSample : MonoBehaviour
{
    private void Awake()
    {
        // Kernel is automatically created by EcsDriver
        var kernel = KernelLocator.Current;
        if (kernel == null)
        {
            Debug.LogError("EcsDriver not found in scene!");
            return;
        }

        // Create World
        var world = kernel.CreateWorld(null, "GameWorld", setAsCurrent: true);
        
        // Register systems
        world.AddSystems(new List<ISystem>
        {
            new MovementSystem(),
            new PrintPositionSystem()
        }.AsReadOnly());
        
        // Create test entities
        using (var cmd = world.BeginWrite())
        {
            var entity = cmd.CreateEntity();
            cmd.AddComponent(entity, new Position(0, 0));
            cmd.AddComponent(entity, new Velocity(1, 0));
        }
        
        Debug.Log("EcsDriver sample initialized!");
    }
}
```

### 3. Run

When you run the scene, `EcsDriver` automatically creates Kernel, and Unity's frame callbacks are converted to ECS frame structure.

---

## Key APIs

* **EcsDriver**: Unity MonoBehaviour-based Kernel driver
* **KernelLocator.Current**: Access global Kernel instance
* **KernelLocator.CurrentWorld**: Access currently active World
* **IKernel.CreateWorld()**: Create new World
* **IWorld.AddSystems()**: Register systems

---

## Notes and Best Practices

* There should be **only one EcsDriver** in the scene (duplicates are automatically removed).
* `EcsDriver` is set to `DefaultExecutionOrder(-32000)` to run before other scripts.
* Kernel is automatically created/destroyed according to `EcsDriver` lifecycle.
* World creation is typically performed in `Awake()` or `Start()`.

---

## License

MIT © 2026 Pippapips Limited.
