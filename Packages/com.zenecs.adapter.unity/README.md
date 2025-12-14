# ZenECS Adapter for Unity

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black.svg)](https://unity.com/)

**ZenECS Adapter for Unity** is an optional integration layer that connects ZenECS Core with the Unity engine. It bridges ZenECS with Unity's lifecycle, ScriptableObject, MonoBehaviour, editor tools, and more.

> **Detailed Documentation:** [Docs/adapter-unity/](../../Docs/adapter-unity/)

---

## ✨ Key Features

- **EcsDriver** — MonoBehaviour-based Kernel lifecycle management and frame driving
- **EntityLink** — Component that links Unity GameObjects to ECS Entities
- **EntityBlueprint** — ScriptableObject-based entity blueprint system
- **UniRx Integration** — Extension methods that convert World message bus to IObservable (optional)
- **Zenject Support** — Integration with Zenject dependency injection framework (optional)
- **System Presets** — Asset-based system configuration via ScriptableObject
- **Context Binding** — Shared/per-entity context management and binder system
- **Editor Tools** — ECS Explorer, custom inspectors, code generation, and more

---

## 📦 Installation

### Unity (UPM)

#### Install via Git URL

1. Open **Package Manager** → **Add package from git URL…**
2. Enter the following URL:
   ```
   https://github.com/Pippapips/ZenECS_deprecated.git?path=Packages/com.zenecs.adapter.unity#v1.0.0
   ```

#### Local Development

Place the repository under your project and reference via `file:` URL or add an entry in `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.zenecs.core": "https://github.com/Pippapips/ZenECS_deprecated.git?path=Packages/com.zenecs.core#v1.0.0",
    "com.zenecs.adapter.unity": "file:../../ZenECS_deprecated/Packages/com.zenecs.adapter.unity"
  }
}
```

### Dependencies

- **com.zenecs.core** (required) — ZenECS Core runtime
- **UniRx** (optional, requires `ZENECS_UNIRX` define) — For UniRx integration features
- **Zenject** (optional, requires `ZENECS_ZENJECT` define) — For Zenject DI integration

> **Note:** Scripting Define Symbols are automatically detected and configured by the package.

---

## 🚀 Quick Start

### 1. Setting up EcsDriver

Add an `EcsDriver` component to your scene or use `ProjectInstaller` to initialize the Kernel.

```csharp
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Core;

public class Bootstrap : MonoBehaviour
{
    private void Awake()
    {
        // Kernel is automatically created if EcsDriver exists in the scene
        var kernel = KernelLocator.Current;
        var world = kernel.CreateWorld("GameWorld", setAsCurrent: true);
        
        // Register systems
        world.AddSystems([new MovementSystem()]);
    }
}
```

### 2. Linking GameObjects with EntityLink

```csharp
using UnityEngine;
using ZenECS.Adapter.Unity.Linking;
using ZenECS.Core;

var world = KernelLocator.CurrentWorld;
var entity = world.CreateEntity();

// Link GameObject to Entity
var link = gameObject.CreateEntityLink(world, entity);

// Add components
world.AddComponent(entity, new Position(0, 0, 0));
```

### 3. Spawning Entities with EntityBlueprint

1. **Project Window** → Right-click → **Create** → **ZenECS** → **Entity Blueprint**
2. Configure component data in the Blueprint inspector
3. Spawn at runtime:

```csharp
using ZenECS.Adapter.Unity.Blueprints;

blueprint.Spawn(
    KernelLocator.CurrentWorld,
    ZenEcsUnityBridge.SharedContextResolver,
    onCreated: entity => Debug.Log($"Entity spawned: {entity.Id}")
);
```

---

## 📚 Key Namespaces

- `ZenECS.Adapter.Unity` — Core Unity integration (EcsDriver, KernelLocator, ZenEcsUnityBridge)
- `ZenECS.Adapter.Unity.Linking` — EntityLink and view registry
- `ZenECS.Adapter.Unity.Blueprints` — EntityBlueprint and related types
- `ZenECS.Adapter.Unity.Binding.Contexts` — Context binding system
- `ZenECS.Adapter.Unity.SystemPresets` — System preset and resolver
- `ZenECS.Adapter.Unity.DI` — Dependency injection setup (ProjectInstaller)
- `ZenECS.Adapter.Unity.UniRx` — UniRx integration extension methods (conditional compilation)

---

## 📖 Documentation

Detailed documentation is available at:

- [Overview](../../Docs/adapter-unity/overview.md)
- [Setup](../../Docs/adapter-unity/setup.md)
- [View Binder](../../Docs/adapter-unity/view-binder.md)
- [Input → Intent](../../Docs/adapter-unity/input-intent.md)
- [FixedStep vs Update](../../Docs/adapter-unity/fixedstep-update.md)
- [Zenject / UniRx](../../Docs/adapter-unity/unity-di-unirx.md)
- [Troubleshooting](../../Docs/adapter-unity/troubleshooting.md)

Full documentation index: [Docs/adapter-unity/](../../Docs/adapter-unity/)

---

## 🔗 Related Links

- [ZenECS Core](../com.zenecs.core/README.md)
- [Main Documentation](../../Docs/README.md)
- [GitHub Repository](https://github.com/Pippapips/ZenECS_deprecated)

---

## 📄 License

MIT © Pippapips Limited

---

**Version:** 1.0.0  
**Unity Version:** 2021.3 or higher  
**Dependencies:** com.zenecs.core 1.0.0
