# Unity Setup

Complete setup guide for ZenECS Unity Adapter.

## Installation

### Step 1: Install Core Package

1. Package Manager → **+** → **Add package from git URL**
2. Enter: `https://github.com/Pippapips/ZenECS.git?path=Packages/com.zenecs.core`

### Step 2: Install Unity Adapter

1. Package Manager → **+** → **Add package from git URL**
2. Enter: `https://github.com/Pippapips/ZenECS.git?path=Packages/com.zenecs.adapter.unity`

### Step 3: Verify Installation

Check Package Manager for both packages:
- ✅ `com.zenecs.core` (version 1.0.0+)
- ✅ `com.zenecs.adapter.unity` (version 1.0.0+)

## Scripting Define Symbols

The adapter automatically detects optional dependencies:

- **ZENECS_UNIRX**: Set if UniRx package is detected
- **ZENECS_ZENJECT**: Set if Zenject package is detected

If automatic detection fails, manually add to **Player Settings** → **Other Settings** → **Scripting Define Symbols**:
```
ZENECS_UNIRX;ZENECS_ZENJECT
```

## Project Setup

### Create EcsDriver

1. Create empty GameObject in scene
2. Add `EcsDriver` component

```csharp
// Kernel is automatically created
var kernel = KernelLocator.Current;
```

### Create Bootstrap Script

```csharp
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Core;

public class GameBootstrap : MonoBehaviour
{
    private void Awake()
    {
        var kernel = KernelLocator.Current;
        var world = kernel.CreateWorld(null, "GameWorld", setAsCurrent: true);
        
        // Register systems
        world.AddSystems([
            new MovementSystem(),
            new HealthSystem()
        ]);
    }
}
```

## Key Features

### EcsDriver
Automatically manages kernel lifecycle and bridges Unity's frame callbacks:
- `Update()` → `kernel.BeginFrame(deltaTime)`
- `FixedUpdate()` → `kernel.FixedStep(fixedDeltaTime)`
- `LateUpdate()` → `kernel.LateFrame(deltaTime)`

### EntityLink
Connect Unity GameObjects to ECS entities:

```csharp
using ZenECS.Adapter.Unity.Linking;

var link = gameObject.AddComponent<EntityLink>();
link.Attach(world, entity);
```

### EntityBlueprint
Data-driven entity spawning with ScriptableObject:

1. **Project Window** → Right-click → **Create** → **ZenECS** → **Entity Blueprint**
2. Configure components in inspector
3. Spawn at runtime:

```csharp
using ZenECS.Adapter.Unity.Blueprints;

blueprint.Spawn(
    KernelLocator.CurrentWorld,
    ZenEcsUnityBridge.SharedContextResolver
);
```

## Troubleshooting

### Package Not Found
- Check Git is installed
- Verify internet connection
- Try local path installation
- Check Unity version (2021.3+)

### Scripting Defines Not Set
- Check package detection in adapter
- Manually add defines in Player Settings
- Verify package installation

### Compilation Errors
- Check Unity version compatibility
- Verify .NET Standard 2.1 support
- Clear Library folder
- Reimport packages

## Next Steps

- **Quick Start**: See `quickstart.md` for a 5-minute tutorial
- **Full Documentation**: https://github.com/Pippapips/ZenECS/tree/main/Docs/adapter-unity
- **Samples**: Check `Samples~` folder in Package Manager

