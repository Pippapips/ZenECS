# Unity Setup

> Docs / Adapter (Unity) / Unity setup

Complete setup guide for ZenECS Unity Adapter, including installation, configuration, and project setup.

## Overview

Setting up ZenECS in Unity involves:

1. **Installing packages** - Core and Unity Adapter
2. **Configuring scripting defines** - Optional feature flags
3. **Setting up EcsDriver** - Kernel lifecycle management
4. **Configuring assembly definitions** - Optional optimization

## Installation

### Step 1: Install Core Package

See [Install via UPM](../getting-started/install-upm.md) for detailed instructions.

**Quick install:**
1. Package Manager → **+** → **Add package from git URL**
2. Enter: `https://github.com/Pippapips/ZenECS.git?path=Packages/com.zenecs.core#v1.0.0`

### Step 2: Install Unity Adapter

1. Package Manager → **+** → **Add package from git URL**
2. Enter: `https://github.com/Pippapips/ZenECS.git?path=Packages/com.zenecs.adapter.unity#v1.0.0`

### Step 3: Verify Installation

Check Package Manager for both packages:
- ✅ `com.zenecs.core` (version 1.0.0+)
- ✅ `com.zenecs.adapter.unity` (version 1.0.0+)

## Version Defines

Scripting Define Symbols are automatically detected:

### Automatic Detection

The adapter automatically sets defines based on installed packages:

- **ZENECS_UNIRX**: Set if UniRx package is detected
- **ZENECS_ZENJECT**: Set if Zenject package is detected

### Manual Configuration

If automatic detection fails, manually add to **Player Settings** → **Other Settings** → **Scripting Define Symbols**:

```
ZENECS_UNIRX;ZENECS_ZENJECT
```

## Project Setup

### Step 1: Create EcsDriver

1. Create empty GameObject in scene
2. Add `EcsDriver` component
3. Configure in inspector (optional)

```csharp
// EcsDriver is automatically created
// Access kernel via KernelLocator
var kernel = KernelLocator.Current;
```

### Step 2: Create Bootstrap Script

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
            new HealthSystem(),
            new RenderSystem()
        ]);
    }
}
```

### Step 3: Configure Assembly Definitions (Optional)

For better compilation performance:

1. Create assembly definition for your game code
2. Reference `ZenECS.Adapter.Unity` assembly
3. Reference `ZenECS.Core` assembly

**Example asmdef:**

```json
{
    "name": "MyGame",
    "references": [
        "ZenECS.Core",
        "ZenECS.Adapter.Unity"
    ],
    "includePlatforms": [],
    "excludePlatforms": []
}
```

## Configuration

### KernelOptions

Configure kernel behavior:

```csharp
var options = new KernelOptions
{
    // Configuration options
};

var kernel = new Kernel(options);
```

### World Configuration

Configure world behavior:

```csharp
var world = kernel.CreateWorld(
    name: "GameWorld",
    tags: new[] { "gameplay" },
    setAsCurrent: true
);
```

## Integration Setup

### Zenject Integration (Optional)

If using Zenject:

1. Install Zenject package
2. Create `ProjectInstaller`:

```csharp
#if ZENECS_ZENJECT
using Zenject;
using ZenECS.Adapter.Unity;

public class ProjectInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<ISystemPresetResolver>()
            .To<SystemPresetResolver>()
            .AsSingle();
    }
}
#endif
```

### UniRx Integration (Optional)

If using UniRx:

1. Install UniRx package
2. Use extension methods:

```csharp
#if ZENECS_UNIRX
using UniRx;
using ZenECS.Adapter.Unity.UniRx;

// Convert messages to observables
world.ObserveMessages<DamageMessage>()
    .Subscribe(msg => HandleDamage(msg));
#endif
```

## Troubleshooting

### Package Not Found

**Issue:** Package Manager can't find package

**Solutions:**
- Check Git is installed
- Verify internet connection
- Try local path installation
- Check Unity version (2021.3+)

### Scripting Defines Not Set

**Issue:** Optional features not available

**Solutions:**
- Check package detection in adapter
- Manually add defines in Player Settings
- Verify package installation

### Compilation Errors

**Issue:** Scripts don't compile

**Solutions:**
- Check Unity version compatibility
- Verify .NET Standard 2.1 support
- Clear Library folder
- Reimport packages

## Next Steps

After setup:

1. **[Quick Start](../getting-started/quickstart-basic.md)** - Build first system
2. **[View Binder](./view-binder.md)** - Connect to GameObjects
3. **[Entity Blueprints](./view-binder.md)** - Data-driven entities

## See Also

- [Install via UPM](../getting-started/install-upm.md) - Detailed installation
- [Unity Adapter Overview](./overview.md) - Feature overview
- [Troubleshooting](./troubleshooting.md) - Common issues
