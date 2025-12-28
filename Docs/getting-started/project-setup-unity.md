# Unity Project Setup

> Docs / Getting started / Unity project setup

Recommended project configuration for ZenECS Unity projects.

## Overview

Setting up a Unity project for ZenECS involves:

1. **Assembly Definitions**: Optional but recommended
2. **Scripting Runtime**: .NET Standard 2.1 or .NET 4.x
3. **Player Settings**: Scripting defines and API compatibility
4. **Project Structure**: Organize code and assets

## Assembly Definitions

### Why Use Assembly Definitions?

- **Faster compilation**: Only recompile changed assemblies
- **Better organization**: Logical code grouping
- **Dependency management**: Clear assembly references

### Recommended Structure

```
Assets/
├── Scripts/
│   ├── Game.asmdef          # Your game code
│   ├── Systems/             # ECS systems
│   ├── Components/          # Component definitions
│   └── ...
└── Packages/
    ├── com.zenecs.core/
    └── com.zenecs.adapter.unity/
```

### Create Assembly Definition

1. Right-click folder → **Create** → **Assembly Definition**
2. Name it (e.g., `Game.asmdef`)
3. Add references:
   - `ZenECS.Core`
   - `ZenECS.Adapter.Unity`

**Example asmdef:**

```json
{
    "name": "Game",
    "references": [
        "ZenECS.Core",
        "ZenECS.Adapter.Unity"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

## Scripting Runtime

### .NET Standard 2.1 (Recommended)

**Player Settings** → **Other Settings** → **Api Compatibility Level**:
- Set to **.NET Standard 2.1**

**Benefits:**
- Better performance
- Modern C# features
- Cross-platform compatibility

### .NET 4.x (Alternative)

If you need .NET 4.x features:
- Set to **.NET 4.x**
- ZenECS works with both

## Player Settings

### Scripting Define Symbols

Optional defines (auto-detected):
- `ZENECS_UNIRX` - UniRx integration
- `ZENECS_ZENJECT` - Zenject integration

### Other Settings

- **Scripting Backend**: IL2CPP or Mono (both supported)
- **Api Compatibility Level**: .NET Standard 2.1 (recommended)
- **Allow 'unsafe' Code**: Not required for ZenECS

## Project Structure

### Recommended Layout

```
Assets/
├── Scripts/
│   ├── Components/          # Component definitions
│   │   ├── Position.cs
│   │   └── Velocity.cs
│   ├── Systems/            # ECS systems
│   │   ├── MovementSystem.cs
│   │   └── HealthSystem.cs
│   ├── Bootstrap/          # Game initialization
│   │   └── GameBootstrap.cs
│   └── Game.asmdef
├── Prefabs/                # Entity prefabs
├── Blueprints/             # EntityBlueprint assets
└── Scenes/                 # Game scenes
```

## Best Practices

### ✅ Do

- **Use assembly definitions**: Faster compilation
- **Organize by feature**: Group related code
- **Separate concerns**: Components, systems, bootstrap
- **Use namespaces**: Logical code organization

### ❌ Don't

- **Don't mix concerns**: Keep components, systems separate
- **Don't skip assembly defs**: Slower compilation
- **Don't ignore structure**: Organize from the start

## See Also

- [Install via UPM](install-upm.md) - Package installation
- [Unity Adapter Setup](../adapter-unity/setup.md) - Adapter configuration
- [Quick Start](quickstart-basic.md) - Get started quickly
