# API Reference

> Docs / Core / API reference

Complete API reference for ZenECS Core, auto-generated from XML documentation comments.

## Overview

ZenECS API documentation is automatically generated from XML comments in the source code using DocFX. This ensures documentation stays in sync with the code.

## How It Works

### XML Documentation

All public APIs have XML documentation comments:

```csharp
/// <summary>
/// Creates a new entity in the world.
/// </summary>
/// <returns>A new entity instance.</returns>
public Entity CreateEntity() { }
```

### DocFX Generation

1. **Build projects** with XML documentation enabled
2. **Extract metadata** from XML comments and code
3. **Generate HTML** documentation site
4. **Combine** with manual documentation

### Build Process

```powershell
# Build with XML documentation
dotnet build -c Release /p:GenerateDocumentationFile=true

# Generate API docs
docfx Docs_/docfx.json
```

## API Surface

### Core Namespaces

- **ZenECS.Core**: Core ECS runtime
  - `IKernel`, `IWorld`, `Entity`, `Component`
- **ZenECS.Core.Systems**: System interfaces
  - `ISystem`, `IFixedRunSystem`, `IFrameRunSystem`
- **ZenECS.Core.Messaging**: Message bus
  - `IMessageBus`, message publishing/consumption
- **ZenECS.Core.Binding**: Binding system
  - `IBinder<T>`, `ComponentDelta<T>`, `IContext`

### Unity Adapter Namespaces

- **ZenECS.Adapter.Unity**: Unity integration
  - `EcsDriver`, `KernelLocator`, `ZenEcsUnityBridge`
- **ZenECS.Adapter.Unity.Linking**: Entity linking
  - `EntityLink`, `EntityViewRegistry`
- **ZenECS.Adapter.Unity.Blueprints**: Blueprint system
  - `EntityBlueprint`, `EntityBlueprintData`

## Accessing API Documentation

### Generated Site

After building documentation:

1. Open `Docs_/_site/api/index.html`
2. Browse namespaces and types
3. Search for specific APIs

### Online (Future)

When deployed to GitHub Pages:
- Visit documentation site
- Navigate to API Reference section
- Search and browse APIs

## Examples

### Finding API Documentation

**Search for method:**
- Navigate to namespace (e.g., `ZenECS.Core`)
- Find type (e.g., `IWorld`)
- Locate method (e.g., `CreateEntity`)

**Browse by topic:**
- **Entities**: `Entity`, `IWorldEntityApi`
- **Components**: `IWorldComponentApi`
- **Systems**: `ISystem`, `IWorldSystemsApi`
- **Messages**: `IMessageBus`, `IWorldMessagesApi`

## See Also

- [API Index](../references/api-index.md) - Complete API index
- [DocFX Integration Guide](../references/docfx-integration-guide.md) - Build guide
- [Documentation Guidelines](../references/documentation-guidelines.md) - Writing docs
