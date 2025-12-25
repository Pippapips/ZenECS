# ZenECS Adapter Unity — Sample 05: Input → Intent Pattern

This sample demonstrates the pattern of converting Unity **Input** to ECS **Intent** components.

* **Intent Component** — Component that represents input intent
* **Input System** — Converts Unity Input to Intent
* **Intent Processing System** — Reads Intent and executes game logic
* **Message-based Input** — View → Message → System flow

---

## What This Sample Shows

1. **Input Collection**
   Collect input using Unity's Input System (or Legacy Input).

2. **Intent Creation**
   Convert collected input to Intent components and add them to entities.

3. **Intent Processing**
   Read Intent in FixedGroup systems and execute game logic.

4. **Intent Consumption**
   Processed Intent is reset (not removed) after processing to be updated in the next frame.

5. **View System Integration**
   Demonstrates `PositionViewSystem` that applies Position to Transform via EntityLink.

6. **Player Entity Setup**
   Shows how to create a player entity with EntityLink for visual representation.

7. **OnGUI Display**
   Simple GUI showing current player position.

---

## TL;DR Flow

```
[Unity Input]
  └─ Input collection (Update)

[Input System (FrameInputGroup)]
  └─ Add/update Intent component

[Intent Processing System (FixedGroup)]
  └─ Read Intent → Execute game logic
  └─ Remove Intent (consume)
```

---

## File Structure

```
05-InputIntent/
├── README.md
├── InputIntentSample.cs         # Sample script (contains all components and systems)
│   ├── MoveIntent component
│   ├── Position component
│   ├── InputCollectionSystem (FrameInputGroup)
│   ├── MovementIntentSystem (FixedGroup)
│   └── PositionViewSystem (FrameViewGroup)
├── Cube.prefab                  # Optional player prefab
└── 05 - InputIntent.unity       # Sample scene
```

---

## Usage

### 1. Define Intent Component

```csharp
using ZenECS.Core;

/// <summary>
/// Move Intent - represents player's movement intent.
/// </summary>
public readonly struct MoveIntent
{
    public readonly float X, Y, Z;
    public MoveIntent(float x, float y, float z) { X = x; Y = y; Z = z; }
}
```

### 2. Input Collection System (FrameInputGroup)

```csharp
[FrameInputGroup]
public sealed class InputCollectionSystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        // Find player entity (e.g., entity with Player tag)
        foreach (var entity in w.Query<Entity>())
        {
            if (!w.HasTag(entity, "Player")) continue;

            // Collect Unity Input
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            // Add/update Intent component
            using var cmd = w.BeginWrite();
            if (w.HasComponent<MoveIntent>(entity))
            {
                cmd.ReplaceComponent(entity, new MoveIntent(x, 0, z));
            }
            else
            {
                cmd.AddComponent(entity, new MoveIntent(x, 0, z));
            }
        }
    }
}
```

### 3. Intent Processing System (FixedGroup)

```csharp
[FixedGroup]
public sealed class MovementIntentSystem : ISystem
{
    private const float MoveSpeed = 5f;

    public void Run(IWorld w, float dt)
    {
        using var cmd = w.BeginWrite();
        foreach (var (e, intent, pos) in w.Query<MoveIntent, Position>())
        {
            // Update Position based on Intent
            var newPos = new Position(
                pos.X + intent.X * dt * MoveSpeed,
                pos.Y,
                pos.Z + intent.Z * dt * MoveSpeed
            );
            cmd.ReplaceComponent(e, newPos);

            // Reset Intent (not removed, just reset to zero)
            cmd.ReplaceComponent(e, new MoveIntent());
        }
    }
}
```

### 4. View System (FrameViewGroup)

```csharp
[FrameViewGroup]
public sealed class PositionViewSystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        var registry = EntityViewRegistry.For(w);
        foreach (var (entity, pos) in w.Query<Position>())
        {
            if (registry.TryGet(entity, out var link))
            {
                if (link) link.transform.position = new Vector3(pos.X, pos.Y, pos.Z);
            }
        }
    }
}
```

---

## Key APIs

* **Intent Component**: Component that represents input intent
* **Input Collection**: Use Unity Input System or Legacy Input
* **Intent Processing**: Read Intent in FixedGroup and execute logic
* **Intent Consumption**: Remove Intent after processing

---

## Notes and Best Practices

* Input collection is performed in **FrameInputGroup** (variable timestep, only add Intent).
* Intent processing is performed in **FixedGroup** (execute game logic).
* Intent is processed **only once per frame** by removing it after processing.
* You can separate input using multiple Intent types (MoveIntent, JumpIntent, etc.).
* Do not connect Input directly to game logic; connect indirectly through Intent.

---

## License

MIT © 2026 Pippapips Limited.
