# ZenECS Adapter Unity — Sample 02: EntityLink (GameObject ↔ Entity Connection)

This sample demonstrates how to use the **EntityLink** component to connect Unity **GameObject** and ZenECS **Entity**.

* **EntityLink** — MonoBehaviour that connects GameObject and Entity
* **EntityViewRegistry** — View registry management per World
* Link creation/removal at runtime and in editor

---

## What This Sample Shows

1. **EntityLink Creation**
   Use `CreateEntityLink()` extension method to add EntityLink to GameObject and connect it to Entity.

2. **Automatic View Registry Management**
   `EntityLink` automatically registers/unregisters with `EntityViewRegistry` to manage views per World.

3. **Link Lifecycle**
   When GameObject is destroyed, the link is automatically detached and removed from registry.

4. **View System Integration**
   Demonstrates `PositionViewSystem` that reads Position components and applies them to Transform via EntityLink.

5. **Circular Spawn Pattern**
   Shows how to spawn entities in a circular pattern and link them to GameObjects.

---

## TL;DR Flow

```
[GameObject]
  └─ EntityLink component
      └─ Attach(world, entity)
          └─ EntityViewRegistry.For(world).Register(entity, link)

[EntityViewRegistry]
  └─ View map management per World
      └─ entity → EntityLink mapping
```

---

## File Structure

```
02-EntityLink/
├── README.md
├── EntityLinkSample.cs          # Sample script
│   ├── Position component
│   └── PositionViewSystem (FrameViewGroup)
├── Cube.prefab                  # Optional prefab for spawned entities
└── 02 - EntityLink.unity        # Sample scene
```

---

## Usage

### 1. Basic Link Creation

```csharp
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Linking;
using ZenECS.Core;

public class EntityLinkSample : MonoBehaviour
{
    private void Start()
    {
        var world = KernelLocator.CurrentWorld;
        if (world == null) return;

        // Create Entity
        Entity entity;
        using (var cmd = world.BeginWrite())
        {
            entity = cmd.CreateEntity();
            cmd.AddComponent(entity, new Position(0, 0, 0));
        }

        // Connect GameObject and Entity
        var link = gameObject.CreateEntityLink(world, entity);
        Debug.Log($"Entity {entity.Id} linked to {gameObject.name}.");
    }
}
```

### 2. View Lookup via EntityViewRegistry

```csharp
var world = KernelLocator.CurrentWorld;
var registry = EntityViewRegistry.For(world);

// Find link by Entity
if (registry.TryGetView(entity, out var link))
{
    Debug.Log($"Entity {entity.Id}'s GameObject: {link.gameObject.name}");
}

// Iterate all views
foreach (var (e, view) in registry.EnumerateViews())
{
    Debug.Log($"Entity {e.Id} → {view.gameObject.name}");
}
```

### 3. Link Detachment

```csharp
// Manual detachment
var link = GetComponent<EntityLink>();
if (link != null)
{
    link.Detach();
}

// Or automatically detached when GameObject is destroyed
Destroy(gameObject);
```

---

## Key APIs

* **EntityLink**: MonoBehaviour that connects GameObject and Entity
* **EntityLink.Attach(world, entity)**: Connect link to specific World and Entity
* **EntityLink.Detach()**: Detach link
* **EntityLink.IsAlive**: Check if Entity is alive
* **EntityViewRegistry.For(world)**: Get view registry per World
* **GameObject.CreateEntityLink()**: Convenience extension method (editor only)

---

## Notes and Best Practices

* `EntityLink` can exist **only one per GameObject** (`DisallowMultipleComponent`).
* Link is automatically detached when GameObject is destroyed.
* `EntityViewRegistry` is managed per World, and automatically cleaned up when World is destroyed.
* `CreateEntityLink()` in editor is editor-only; at runtime, you must add `EntityLink` component directly.

---

## License

MIT © 2026 Pippapips Limited.
