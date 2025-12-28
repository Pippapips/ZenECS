# ZenECS Adapter Unity — Sample 04: System Presets

This sample demonstrates how to set up and manage systems using **System Presets** based on ScriptableObject.

* **SystemPreset** — ScriptableObject-based system setup
* **SystemPresetResolver** — System preset resolution and registration
* **SystemTypeRef** — Type-safe system reference
* **SystemTypeFilterAttribute** — System type filtering

---

## What This Sample Shows

1. **SystemPreset Creation**
   Create SystemPreset asset in Unity editor and set system types.

2. **Automatic System Registration**
   Use `SystemPresetResolver` to automatically register systems defined in the preset.

3. **Type Filtering**
   Use `SystemTypeFilterAttribute` to select only specific types of systems.

4. **Manual System Registration**
   Fallback option to register systems manually when preset is not available.

5. **Test Entity Creation**
   Creates test entities with Position and Velocity components to demonstrate system functionality.

---

## TL;DR Flow

```
[SystemPreset ScriptableObject]
  └─ SystemTypeRef[] (system type references)

[SystemPresetResolver]
  └─ Resolve(preset)
      └─ Create system instances
          └─ world.AddSystems(systems)
```

---

## File Structure

```
04-SystemPresets/
├── README.md
├── SystemPresetSample.cs       # Sample script (contains Position, Velocity components)
├── MovementSystem.cs            # Example system (FixedGroup)
├── RenderSystem.cs              # Example system (FrameViewGroup)
├── SystemsPreset.asset          # Example SystemPreset asset
└── 04 - System Presets.unity    # Sample scene
```

---

## Usage

### 1. Create SystemPreset Asset

1. In Unity editor: **Project window** → Right-click → **Create** → **ZenECS** → **System Preset**
2. Select the created Preset asset
3. Add system types in Inspector:
   - Add `SystemTypeRef` to **Systems** array
   - Select system type in each `SystemTypeRef`

### 2. Use at Runtime

```csharp
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.SystemPresets;
using ZenECS.Core;
using System.Linq;

public class SystemPresetSample : MonoBehaviour
{
    [SerializeField] private SystemsPreset? _preset;
    [SerializeField] private bool _useManualSystems = false;

    private void Start()
    {
        var kernel = KernelLocator.Current;
        if (kernel == null) return;

        var world = kernel.CreateWorld(null, "SystemPresetWorld", setAsCurrent: true);

        if (_preset != null && !_useManualSystems)
        {
            // Register systems using SystemPresetResolver
            var resolver = new SystemPresetResolver();
            var systems = resolver.InstantiateSystems(_preset.GetValidTypes().ToList());
            world.AddSystems(systems);
            Debug.Log($"{systems.Count} systems registered from preset.");
        }
        else if (_useManualSystems)
        {
            // Manual registration
            world.AddSystems(new List<ISystem>
            {
                new MovementSystem(),
                new RenderSystem()
            }.AsReadOnly());
        }
    }
}
```

### 3. Custom SystemPresetResolver

```csharp
public class CustomSystemPresetResolver : ISystemPresetResolver
{
    public IReadOnlyList<ISystem> Resolve(SystemPreset preset)
    {
        var systems = new List<ISystem>();
        
        foreach (var systemRef in preset.Systems)
        {
            if (systemRef.Type != null)
            {
                var system = Activator.CreateInstance(systemRef.Type) as ISystem;
                if (system != null)
                    systems.Add(system);
            }
        }
        
        return systems;
    }
}
```

---

## Key APIs

* **SystemPreset**: ScriptableObject-based system preset
* **SystemPresetResolver**: System preset resolution and instance creation
* **ISystemPresetResolver**: Custom Resolver interface
* **SystemTypeRef**: Type-safe system type reference
* **SystemTypeFilterAttribute**: System type filtering attribute

---

## Notes and Best Practices

* SystemPreset stores **only type references**; system instances are created at runtime.
* System types must implement `ISystem`.
* Use `SystemTypeFilterAttribute` to select only systems that meet specific conditions.
* Using SystemPreset allows managing system configuration as assets, increasing flexibility.

---

## License

MIT © 2026 Pippapips Limited.
