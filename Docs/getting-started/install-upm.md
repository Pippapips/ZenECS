# Install via Unity Package Manager (UPM)

> Docs / Getting started / Install via UPM

Install ZenECS Core and Unity Adapter packages via Unity Package Manager.

## Requirements

- **Unity 2021.3** or higher
- **Git** installed and accessible from command line
- **Internet connection** (for Git URL installation)

## Add Core Package

### Option 1: Git URL (Recommended)

1. Open Unity Editor
2. Go to **Window** → **Package Manager**
3. Click **+** button → **Add package from git URL...**
4. Enter the URL:
   ```
   https://github.com/Pippapips/ZenECS.git?path=Packages/com.zenecs.core#v1.0.0
   ```
5. Click **Add**

### Option 2: Local Path

For local development:

1. Clone the repository locally
2. In Package Manager, click **+** → **Add package from disk...**
3. Navigate to `Packages/com.zenecs.core/package.json`
4. Click **Open**

Or add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.zenecs.core": "file:../../ZenECS/Packages/com.zenecs.core"
  }
}
```

## Add Unity Adapter (Optional)

The Unity Adapter provides Unity-specific integration features.

### Install Adapter

1. In Package Manager, click **+** → **Add package from git URL...**
2. Enter:
   ```
   https://github.com/Pippapips/ZenECS.git?path=Packages/com.zenecs.adapter.unity#v1.0.0
   ```
3. Click **Add**

**Note:** The adapter requires `com.zenecs.core` as a dependency.

## Version Defines

Scripting Define Symbols are automatically detected and configured:

- **ZENECS_UNIRX**: Automatically set if UniRx is installed
- **ZENECS_ZENJECT**: Automatically set if Zenject is installed

### Manual Configuration

If needed, add to **Player Settings** → **Scripting Define Symbols**:

```
ZENECS_UNIRX;ZENECS_ZENJECT
```

## Verify Installation

### Check Package Installation

1. Open **Package Manager**
2. Look for **ZenECS Core** and **ZenECS Adapter Unity** in the list
3. Verify version is **1.0.0** or higher

### Test in Code

Create a test script:

```csharp
using ZenECS.Core;
using UnityEngine;

public class ZenECSTest : MonoBehaviour
{
    void Start()
    {
        // Test Core
        var kernel = new Kernel();
        var world = kernel.CreateWorld(null, "TestWorld");
        Debug.Log("ZenECS Core: OK");
        
        // Test Adapter (if installed)
        #if ZENECS_ADAPTER_UNITY
        Debug.Log("ZenECS Adapter: OK");
        #endif
    }
}
```

If the script compiles and runs, installation is successful!

## Project Structure

After installation, your project should have:

```
Assets/
├── Packages/
│   ├── com.zenecs.core/
│   │   ├── Runtime/
│   │   └── Samples~/
│   └── com.zenecs.adapter.unity/
│       ├── Runtime/
│       ├── Editor/
│       └── Samples~/
```

## Dependencies

### Core Package

- **No dependencies** - Pure .NET Standard 2.1

### Unity Adapter

- **com.zenecs.core** (required)
- **UniRx** (optional, for reactive features)
- **Zenject** (optional, for DI integration)

## Troubleshooting

### Package Not Found

**Issue:** Package Manager can't find the package

**Solutions:**
- Check Git is installed: `git --version`
- Verify internet connection
- Try local path installation instead
- Check Unity version (2021.3+ required)

### Compilation Errors

**Issue:** Scripts don't compile after installation

**Solutions:**
- Check Unity version compatibility
- Verify .NET Standard 2.1 support
- Clear Library folder and reimport
- Check for conflicting packages

### Missing References

**Issue:** `using ZenECS.Core;` doesn't work

**Solutions:**
- Verify package is in Package Manager
- Check assembly definition files
- Restart Unity Editor
- Reimport package

### Version Conflicts

**Issue:** Multiple versions or conflicts

**Solutions:**
- Remove old versions from manifest.json
- Clear Package Manager cache
- Reimport packages
- Check for duplicate entries in manifest.json

## Next Steps

After successful installation:

1. **[Quick Start Guide](quickstart-basic.md)** - Build your first ECS system
2. **[Unity Project Setup](project-setup-unity.md)** - Configure your Unity project
3. **[Samples Overview](samples-overview.md)** - Explore example projects

## See Also

- [Install via NuGet](install-nuget.md) - .NET installation guide
- [Quick Start](quickstart-basic.md) - Get started quickly
- [Unity Adapter Overview](../adapter-unity/overview.md) - Unity integration guide
