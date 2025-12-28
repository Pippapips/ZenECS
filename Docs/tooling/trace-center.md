# Trace Center

> Docs / Tooling / Trace Center

The Trace Center provides runtime tracing and logging capabilities for debugging ZenECS systems and components.

## Overview

The **Trace Center** is a debugging tool that:

- **Logs system execution**: Track which systems run and when
- **Traces component changes**: Monitor component add/remove/replace operations
- **Records messages**: Log message publishing and consumption
- **Performance metrics**: Measure system execution times

## Features

### System Tracing

Enable tracing for specific systems:

```csharp
// Enable tracing for a system
world.TraceSystem<MovementSystem>(enabled: true);

// Trace all systems
world.TraceAllSystems(enabled: true);
```

### Component Tracing

Monitor component operations:

```csharp
// Trace component changes
world.TraceComponent<Position>(enabled: true);

// Trace all components
world.TraceAllComponents(enabled: true);
```

### Message Tracing

Log message bus activity:

```csharp
// Trace messages
world.TraceMessages<DamageMessage>(enabled: true);

// Trace all messages
world.TraceAllMessages(enabled: true);
```

## Usage

### Basic Tracing

```csharp
var world = kernel.CreateWorld(null, "GameWorld");

// Enable tracing
world.TraceSystem<MovementSystem>(enabled: true);
world.TraceComponent<Position>(enabled: true);

// Run simulation
kernel.PumpAndLateFrame(dt, fixedDelta, maxSubStepsPerFrame: 4);

// View traces in console or log file
```

### Advanced Filtering

```csharp
// Trace only specific entities
world.TraceEntity(entity, enabled: true);

// Trace with filters
world.TraceWithFilter(trace =>
{
    return trace.SystemType == typeof(MovementSystem) &&
           trace.Entity.Id == targetEntity.Id;
});
```

## Output Format

### System Trace

```
[TRACE] System: MovementSystem
  Frame: 123
  DeltaTime: 0.016
  Entities Processed: 42
  Execution Time: 0.5ms
```

### Component Trace

```
[TRACE] Component: Position
  Entity: #123:0
  Operation: Replace
  Old Value: (0.0, 0.0)
  New Value: (1.0, 0.0)
```

### Message Trace

```
[TRACE] Message: DamageMessage
  Publisher: HealthSystem
  Target: Entity #123:0
  Amount: 10
```

## Performance Impact

Tracing has minimal performance impact:

- **Disabled**: Zero overhead
- **Enabled**: ~1-2% overhead per traced operation
- **Filtered**: Additional overhead for filter evaluation

## Best Practices

### ✅ Do

- **Enable selectively**: Only trace what you need
- **Use filters**: Reduce trace volume
- **Disable in production**: Remove tracing for release builds
- **Review traces**: Analyze patterns and issues

### ❌ Don't

- **Don't trace everything**: Too much data is overwhelming
- **Don't leave enabled**: Disable when not debugging
- **Don't rely on traces**: Use proper logging for production

## See Also

- [ECS Explorer](./ecs-explorer.md) - Visual debugging tool
- [Editor Windows](./editor-windows.md) - Unity editor tools
- [Tracing & Logging](../core/tracing-logging.md) - Core tracing API
