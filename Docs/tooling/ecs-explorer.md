# ECS Explorer

> Docs / Tooling / ECS Explorer

The ZenECS Explorer is a powerful debugging and inspection tool for visualizing and editing ECS systems, entities, components, contexts, and binders at runtime.

## Overview

The **ZenECS Explorer** is an EditorWindow that provides real-time inspection of your ECS world state during Play Mode. It displays:

- **Systems Tree** — Hierarchical view of all registered systems organized by phase and group
- **Entities Panel** — Lists entities filtered by selected system, with full editing capabilities
- **Find Mode** — Search for specific entities by ID and generation number
- **Singletons View** — Inspect and edit world-level singleton components
- **Component Editing** — Add, remove, and modify entity components inline
- **Context & Binder Management** — View and manage entity contexts and binders

---

## Opening the Explorer

The Explorer can be opened via the Unity menu:

**Window** → **ZenECS** → **Tools** → **ZenECS Explorer**

Or use the keyboard shortcut if configured in Unity's Shortcuts Manager.

The window requires:
- An active ZenECS Kernel (via `ZenEcsUnityBridge.Kernel` or scene `EcsDriver`)
- A current World selected in the Kernel

If either is missing, the window displays a helpful overlay message.

---

## Window Layout

The Explorer is divided into several sections:

### Header

The top toolbar contains:

- **World Selector** — Dropdown showing all available worlds with count
  - Displays current world name, tags, and WorldId GUID
  - Selecting a different world switches the current world context
- **Add Button (+)** — Context menu for adding:
  - **Entity from Blueprint** — Spawn entities using EntityBlueprint assets
  - **Singleton** — Add world-level singleton components
  - **System** — Register new systems to the world
  - **System Preset** — Apply SystemsPreset assets (if SystemPresetResolver is available)

### Main Layout

The main area is split into two panels:

#### Left Panel: Systems Tree

Displays a hierarchical view of all systems organized by:

1. **Phase Kind**
   - **Deterministic** — Fixed-step systems (FixedInput, FixedDecision, FixedSimulation, FixedPost)
   - **Non-deterministic** — Variable-step systems (FrameInput, FrameSync, FrameView, FrameUI)
   - **Unknown** — Systems without group attributes

2. **System Groups** (nested under phases)
   - Each group contains systems registered to that group
   - Systems are listed with their type name
   - Click a system to select it and filter entities in the right panel

3. **Singletons Section**
   - Lists all world singleton components
   - Click a singleton to view its entity details in the right panel
   - Remove singletons with the X button (requires Edit Mode)

Each section can be expanded/collapsed with foldouts. The tree preserves foldout state during the session.

#### Right Panel: Entity Details

Shows content based on selection:

**When a System is Selected:**
- **System Meta** — System type information and metadata
- **Entities List** — Entities that match the system's watched components (if the system uses `[ZenSystemWatch]` attributes)

**When a Singleton is Selected:**
- **Singleton Entity** — The entity that holds the singleton component

**When Nothing is Selected:**
- Info message prompting selection

Each entity displays:
- **Components Section** — Add/remove components, view/edit component data
- **Contexts Section** — View and manage entity contexts
- **Binders Section** — View and manage entity binders

### Footer

The bottom toolbar contains:

- **Pause Button** — Toggle kernel pause state (highlights when paused)
- **Simulation Time** — Elapsed simulation accumulator time
- **Find Controls**:
  - **Entity ID** — Text field for entity ID (numeric only)
  - **Generation** — Text field for entity generation (defaults to 0)
  - **Find Button** — Enter Find Mode with the specified entity
  - **Clear Button** — Exit Find Mode and return to system view
  - **Edit Toggle** — Enable/disable editing mode

---

## Find Mode

Find Mode allows you to search for a specific entity by ID and generation number.

### Entering Find Mode

1. Enter an entity ID (and optionally generation) in the footer fields
2. Click the **Find** button

If the entity exists and is alive, the Explorer switches to Find Mode, displaying:
- **Entity Details** — Full component/context/binder view for the found entity
- **Watched Systems** — List of systems that watch this entity (systems with `[ZenSystemWatch]` matching the entity's components)

If the entity is not found, a warning message is displayed.

### Exiting Find Mode

Click the **Clear** button in the footer or close the Find Mode panel to return to the normal system tree view.

---

## Editing Entities

### Edit Mode

Toggle **Edit Mode** in the footer to enable/disable editing capabilities:

- **Edit Mode ON** (default):
  - Components can be added/removed
  - Component values can be modified
  - Contexts and binders can be managed
  - Singletons can be removed

- **Edit Mode OFF**:
  - All controls are read-only
  - Useful for inspection without accidental modifications

### Adding Components

1. Select an entity in the right panel
2. Expand the **Components** section
3. Click the **+** button in the Components header
4. Choose a component type from the picker dialog
5. The component is created with default values (via `ZenDefaults`)

### Removing Components

1. Select an entity
2. Expand the **Components** section
3. Find the component in the list
4. Click the **X** button next to the component name
5. Confirm removal in the dialog

### Editing Component Values

1. Select an entity
2. Expand the **Components** section
3. Expand the component you want to edit
4. Modify field values using standard Unity Inspector controls:
   - Primitive types (int, float, bool, etc.)
   - Unity types (Vector2, Vector3, Color, etc.)
   - Unity.Mathematics types (float2, float3, float4, quaternion, etc.)
   - FixedString64Bytes (with UTF-8 validation)

Changes are applied immediately when Edit Mode is enabled.

### Managing Contexts

1. Select an entity
2. Expand the **Contexts** section
3. Use the **+** button to add contexts (via context asset picker)
4. Use the **X** button to remove contexts
5. View context properties in read-only mode

### Managing Binders

1. Select an entity
2. Expand the **Binders** section
3. View all attached binders
4. Remove binders with the **X** button (requires Edit Mode)

---

## Adding Systems

Systems can be added to the world via the **+** button menu:

### Add System

1. Click **+** → **Add System...**
2. Select a system type from the picker dialog
   - Already registered systems are disabled
   - Only concrete types implementing `ISystem` are shown
3. The system is instantiated and registered to the world

### Add System Preset

1. Click **+** → **Add System Preset...**
   - Requires `ZenEcsUnityBridge.SystemPresetResolver` to be configured
2. Select a SystemsPreset asset
3. All valid system types from the preset are instantiated and registered

---

## Adding Entities

### From Blueprint

1. Click **+** → **Add Entity from Blueprint...**
2. Select an EntityBlueprint asset
3. The entity is spawned with all components, contexts, and binders from the blueprint

This requires:
- An active Kernel
- A current World
- A configured `SharedContextResolver` (for shared contexts in the blueprint)

---

## Adding Singletons

1. Click **+** → **Add Singleton...**
2. Select a singleton component type
   - Types must implement `IWorldSingletonComponent`
   - Already existing singletons are disabled
3. The singleton is created and registered to the world

---

## System Tree Organization

The systems tree groups systems hierarchically:

```
Deterministic
  ├─ Input
  │  └─ MyInputSystem
  ├─ Decision
  │  └─ MyDecisionSystem
  ├─ Simulation
  │  ├─ PhysicsSystem
  │  └─ MovementSystem
  └─ Post
     └─ MyPostSystem

Non-deterministic
  ├─ Begin
  │  ├─ Input
  │  │  └─ MyFrameInputSystem
  │  └─ Sync
  │     └─ MySyncSystem
  └─ Late
     ├─ View
     │  └─ RenderSystem
     └─ UI
        └─ UISystem

Unknown
  └─ UnattributedSystem

Singletons
  ├─ Gravity (Entity #1:0)
  └─ TimeScale (Entity #1:0)
```

Systems without group attributes appear under "Unknown". Systems are organized by their `[FixedGroup]`, `[FrameViewGroup]`, etc. attributes.

---

## Watched Systems

When you select a system, the right panel shows entities that match the system's watched components. Systems must use `[ZenSystemWatch]` attributes to specify which components they watch:

```csharp
[FixedGroup]
[ZenSystemWatch(typeof(Position), typeof(Velocity))]
public class MovementSystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        // System logic
    }
}
```

In Find Mode, the Explorer displays all systems that watch the found entity, making it easy to understand which systems process a specific entity.

---

## Tips & Best Practices

### Performance

- The Explorer updates every 0.25 seconds automatically
- Large entity counts may impact performance; use system filtering to narrow the view
- Edit Mode does not affect performance, but frequent component modifications may trigger validation hooks

### Debugging Workflow

1. **Identify the System** — Use the systems tree to locate the system processing your entities
2. **Select the System** — View entities processed by that system
3. **Inspect Entities** — Expand entity details to view components, contexts, and binders
4. **Use Find Mode** — Search for specific entities by ID when debugging
5. **Check Watched Systems** — In Find Mode, see which systems process the entity

### Keyboard Shortcuts

- The Explorer respects Unity's standard window navigation
- Focus the Entity ID field and press Enter to trigger Find (if implemented)

### State Persistence

- Foldout states are preserved during the session
- Selection state is cleared when:
  - Switching worlds
  - Exiting Play Mode
  - Before assembly reload
- Window position and size are saved by Unity's EditorWindow system

---

## Limitations

- **Play Mode Only** — The Explorer only functions during Play Mode when a Kernel is active
- **Current World Only** — Only the Kernel's current world is displayed
- **Read-Only During Pause** — When the kernel is paused, editing may be limited
- **Component Type Support** — Complex types may not render fully; use code inspection for deep debugging

---

## Troubleshooting

### "Kernel not active" Message

**Cause:** No Kernel is available via `ZenEcsUnityBridge.Kernel` or scene `EcsDriver`.

**Solution:**
- Ensure an `EcsDriver` component exists in the scene, or
- Configure `ProjectInstaller` to initialize the Kernel, or
- Manually create a Kernel and assign it to `ZenEcsUnityBridge.Kernel`

### "No current World" Message

**Cause:** The Kernel has no current world selected.

**Solution:**
- Create a world: `kernel.CreateWorld(null, "MyWorld", setAsCurrent: true)`
- Or set an existing world as current: `kernel.SetCurrentWorld(myWorld)`

### No Entities Shown

**Cause:** The selected system has no watched components, or no entities match.

**Solution:**
- Check that the system uses `[ZenSystemWatch]` attributes
- Verify that entities have the required components
- Try selecting a different system or use Find Mode to locate specific entities

### Components Not Editable

**Cause:** Edit Mode is disabled.

**Solution:**
- Toggle the **Edit** button in the footer to enable Edit Mode

### System Preset Not Available

**Cause:** `ZenEcsUnityBridge.SystemPresetResolver` is null.

**Solution:**
- Configure a SystemPresetResolver in your ProjectInstaller or bootstrap code
- Or add systems individually via "Add System..."

---

## Related Documentation

- [ZenECS Core README](../../Packages/com.zenecs.core/README.md) — Core ECS concepts
- [Systems Documentation](../../Docs/core/systems.md) — System writing guide
- [Components Documentation](../../Docs/core/components.md) — Component definitions
- [Binding Documentation](../../Docs/core/binding.md) — Contexts and binders
- [Entity Blueprint Documentation](../../Docs/adapter-unity/view-binder.md) — Blueprint system
