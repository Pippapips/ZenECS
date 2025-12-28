# Contributing Examples

> Docs / Guides / Contributing examples

Guidelines for contributing code examples, samples, and tutorials to ZenECS.

## Overview

Examples and samples help users learn ZenECS:

- **Code Examples**: Short, focused code snippets
- **Sample Projects**: Complete, runnable projects
- **Tutorials**: Step-by-step guides
- **Patterns**: Common design patterns

## Example Guidelines

### Code Examples

**Requirements:**
- ✅ **Complete**: Runnable code
- ✅ **Clear**: Well-commented
- ✅ **Focused**: One concept per example
- ✅ **Tested**: Verified to work

**Format:**
```csharp
// Example: Basic entity creation
using ZenECS.Core;

var world = kernel.CreateWorld(null, "GameWorld");
Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position(0, 0));
}
```

### Sample Projects

**Structure:**
```
Samples~/XX-SampleName/
├── README.md          # Sample documentation
├── Scene.unity        # Unity scene (if applicable)
├── Scripts/           # Sample code
└── Assets/            # Sample assets
```

**Requirements:**
- ✅ **Complete**: Fully functional
- ✅ **Documented**: Clear README
- ✅ **Focused**: Demonstrates specific concept
- ✅ **Tested**: Works in target environment

## Contribution Process

### Step 1: Choose Topic

Select a topic that:
- Demonstrates a ZenECS feature
- Fills a gap in existing examples
- Is useful to users

### Step 2: Write Code

Follow guidelines:
- Use clear naming
- Add comments
- Follow style guide
- Test thoroughly

### Step 3: Write Documentation

Create README:
- Overview
- Prerequisites
- How to run
- Code walkthrough
- What to try next

### Step 4: Submit PR

Include:
- Code files
- Documentation
- Screenshots (if applicable)
- Test results

## Example Categories

### Basic Examples

Simple, focused examples:
- Entity creation
- Component management
- Basic systems
- Simple queries

### Intermediate Examples

More complex scenarios:
- Multiple systems
- Message bus
- Command buffers
- Binding system

### Advanced Examples

Complex patterns:
- Multi-world setups
- Networking patterns
- Custom extensions
- Performance optimization

## Best Practices

### ✅ Do

- **Keep it simple**: Focus on one concept
- **Add comments**: Explain non-obvious parts
- **Test thoroughly**: Verify examples work
- **Update docs**: Keep documentation current

### ❌ Don't

- **Don't over-complicate**: Keep examples focused
- **Don't skip testing**: Verify examples work
- **Don't forget docs**: Document your examples
- **Don't break existing**: Maintain compatibility

## See Also

- [Contributing Guide](../community/contributing.md) - General contribution guide
- [Writing Manual](../references/writing-manual.md) - Documentation guidelines
- [Samples Overview](../getting-started/samples-overview.md) - Sample index
