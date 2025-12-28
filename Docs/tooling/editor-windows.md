# Editor Windows

> Docs / Tooling / Editor windows

Unity editor windows and tools for ZenECS development and debugging.

## Overview

ZenECS provides several Unity editor windows:

- **ECS Explorer**: Runtime entity and system inspection
- **Trace Center**: Logging and tracing tools
- **Blueprint Editor**: Visual blueprint configuration
- **System Preset Editor**: System configuration tools

## ECS Explorer

The primary debugging tool for runtime inspection.

**Open:** **Window** → **ZenECS** → **Tools** → **ZenECS Explorer**

**Features:**
- System tree view
- Entity inspection
- Component editing
- Context and binder management

See [ECS Explorer](./ecs-explorer.md) for detailed documentation.

## Trace Center

Runtime tracing and logging window.

**Open:** **Window** → **ZenECS** → **Tools** → **Trace Center**

**Features:**
- System execution logs
- Component change traces
- Message bus activity
- Performance metrics

See [Trace Center](./trace-center.md) for detailed documentation.

## Blueprint Editor

Visual editor for EntityBlueprint assets.

**Usage:**
1. Select EntityBlueprint asset
2. Inspector shows visual component editor
3. Add/remove components
4. Configure component values

**Features:**
- Component type picker
- Value editors for all supported types
- JSON preview
- Validation warnings

## System Preset Editor

Editor for SystemsPreset assets.

**Usage:**
1. Select SystemsPreset asset
2. Inspector shows system list
3. Add/remove system types
4. Configure system order

**Features:**
- System type picker
- Order configuration
- Validation
- Deduplication

## Keyboard Shortcuts

Configure in Unity's **Shortcuts Manager**:

- **ECS Explorer**: `Ctrl+Shift+E` (default)
- **Trace Center**: `Ctrl+Shift+T` (default)

## Custom Inspectors

ZenECS provides custom inspectors for:

- **EntityLink**: Shows entity info and quick actions
- **EntityBlueprint**: Visual component editor
- **SystemsPreset**: System configuration
- **EcsDriver**: Kernel status and controls

## See Also

- [ECS Explorer](./ecs-explorer.md) - Detailed explorer guide
- [Trace Center](./trace-center.md) - Tracing documentation
- [Entity Blueprints](../guides/blueprint-components.md) - Blueprint system
