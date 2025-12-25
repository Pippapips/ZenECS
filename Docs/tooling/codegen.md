# Codegen

> Docs / Tooling / Codegen

Code generation tools for ZenECS (if applicable).

## Overview

ZenECS currently does not use code generation. All APIs are explicit and require no generated code.

### Future Considerations

Potential code generation features:

- **Component serialization**: Generate serialization code
- **Query optimization**: Generate optimized query code
- **System templates**: Generate system boilerplate
- **Binding code**: Generate binder implementations

## Manual Alternatives

### Component Templates

Use code snippets for common patterns:

```csharp
// Snippet: zenecs-component
public struct $NAME$
{
    public $TYPE$ $FIELD$;
}
```

### System Templates

Use templates for system structure:

```csharp
// Template: zenecs-system
[FixedGroup]
public sealed class $SYSTEMNAME$ : ISystem
{
    public void Run(IWorld world, float deltaTime)
    {
        using var cmd = world.BeginWrite();
        foreach (var (entity, $COMPONENT$) in world.Query<$COMPONENT$>())
        {
            // System logic
        }
    }
}
```

## See Also

- [ECS Explorer](./ecs-explorer.md) - Runtime inspection
- [Editor Windows](./editor-windows.md) - Unity editor tools
- [Architecture](../overview/architecture.md) - System design
