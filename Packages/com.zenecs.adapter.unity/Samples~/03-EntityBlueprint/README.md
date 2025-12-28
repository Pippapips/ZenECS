# ZenECS Adapter Unity — Sample 03: EntityBlueprint (Entity Spawning)

This sample demonstrates how to spawn entities using **EntityBlueprint** based on ScriptableObject.

* **EntityBlueprint** — ScriptableObject-based entity blueprint
* **EntityBlueprintData** — Component snapshot storage
* **Context Assets** — Shared/Per-Entity Context setup
* **Binders** — Per-entity Binder setup

---

## What This Sample Shows

1. **Blueprint Creation**
   Create EntityBlueprint asset in Unity editor and set component data.

2. **Runtime Spawning**
   Call `EntityBlueprint.Spawn()` to create entities defined in the blueprint.

3. **Context and Binder Application**
   Context Assets and Binders set in the blueprint are automatically applied to entities.

4. **Periodic Spawning**
   Demonstrates spawning entities periodically using `Update()` loop with configurable spawn interval.

5. **Component Verification**
   Shows how to verify spawned entity components after creation.

6. **OnGUI Display**
   Simple GUI display showing Blueprint name and World information.

---

## TL;DR Flow

```
[EntityBlueprint ScriptableObject]
  ├─ EntityBlueprintData (component snapshot)
  ├─ ContextAssets (Shared/Per-Entity)
  └─ Binders (managed references)

[Runtime]
  └─ blueprint.Spawn(world, contextResolver)
      └─ ExternalCommand.CreateEntity
          ├─ ApplyComponents (apply snapshot)
          ├─ ApplyContexts (register Context)
          └─ ApplyBinders (connect Binder)
```

---

## File Structure

```
03-EntityBlueprint/
├── README.md
├── EntityBlueprintSample.cs    # Sample script (contains Health, Position, Rotation components)
├── EntityBlueprint.asset        # Example EntityBlueprint asset
├── UnityTransformContext.cs     # Example Context
├── UnityTransformContextAsset.cs
├── UnityTransformSyncBinder.cs  # Example Binder
├── UnityTransformSyncBinderAsset.cs
├── Cube.prefab                  # Optional prefab
└── 03 - EntityBlueprint.unity   # Sample scene
```

---

## Usage

### 1. Create EntityBlueprint Asset

1. In Unity editor: **Project window** → Right-click → **Create** → **ZenECS** → **Entity Blueprint**
2. Select the created Blueprint asset
3. Add component data in Inspector:
   - Add components in **Components (snapshot)** section
   - Add Context Assets in **Contexts** section (optional)
   - Add Binders in **Binders** section (optional)

### 2. Spawn at Runtime

```csharp
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Blueprints;
using ZenECS.Core;

public class EntityBlueprintSample : MonoBehaviour
{
    [SerializeField] private EntityBlueprint _blueprint;
    [SerializeField] private float _spawnInterval = 1f;

    private IWorld? _world;
    private float _spawnTimer;

    private void Start()
    {
        var kernel = KernelLocator.Current;
        if (kernel == null) return;

        _world = kernel.CreateWorld(null, "BlueprintWorld", setAsCurrent: true);
    }

    private void Update()
    {
        if (_world == null || _blueprint == null) return;

        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= _spawnInterval)
        {
            _spawnTimer = 0f;
            SpawnFromBlueprint();
        }
    }

    private void SpawnFromBlueprint()
    {
        if (_world == null || _blueprint == null) return;

        // Spawn entity from Blueprint
        _blueprint.Spawn(
            _world,
            ZenEcsUnityBridge.SharedContextResolver,
            onCreated: entity =>
            {
                Debug.Log($"Entity {entity.Id} spawned!");
                // Verify components
                if (_world.HasComponent<Health>(entity))
                {
                    var health = _world.ReadComponent<Health>(entity);
                    Debug.Log($"Health: {health.Current}/{health.Max}");
                }
            }
        );
    }
}
```

### 3. Component Snapshot Setup

To add components in Blueprint Inspector:
1. Expand **Components (snapshot)** section
2. Click **Add Component** button
3. Select component type and enter values

---

## Key APIs

* **EntityBlueprint**: ScriptableObject-based entity blueprint
* **EntityBlueprint.Spawn()**: Spawn entity from blueprint
* **EntityBlueprintData**: Component snapshot data
* **SharedContextAsset**: Shared Context marker
* **PerEntityContextAsset**: Per-entity Context factory
* **IBinder**: View binding interface

---

## Notes and Best Practices

* Blueprint uses **ExternalCommand** to safely create entities.
* Binders are **shallow-cloned** to create independent instances per entity.
* Shared Context must be resolved via `ISharedContextResolver`.
* Blueprint component data is serialized as JSON, so only serializable types can be used.

---

## License

MIT © 2026 Pippapips Limited.
