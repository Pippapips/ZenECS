# Writing Manual

> Docs / References / Writing Manual

A practical guide for writing ZenECS documentation with examples, templates, and best practices.

## Quick Start

### New Document Checklist

- [ ] Choose appropriate directory (`overview/`, `core/`, `guides/`, etc.)
- [ ] Use kebab-case filename: `my-new-guide.md`
- [ ] Follow document structure template
- [ ] Include overview and examples
- [ ] Add cross-references to related docs
- [ ] Test all code examples
- [ ] Preview with DocFX before submitting

### Document Template

```markdown
# Document Title

> Docs / Category / Subcategory

One-sentence description of what this document covers.

## Overview

Brief overview of the topic (2-3 paragraphs).

## Key Concepts

- Concept 1: Brief explanation
- Concept 2: Brief explanation

## Usage

### Basic Usage

Step-by-step instructions.

### Advanced Usage

More complex scenarios.

## Examples

### Example 1: Simple Case

```csharp
// Code example here
```

### Example 2: Complex Case

```csharp
// More complex code example
```

## Best Practices

- Practice 1
- Practice 2

## Common Pitfalls

- Pitfall 1: Why it's a problem and how to avoid it
- Pitfall 2: Why it's a problem and how to avoid it

## See Also

- [Related Document](path/to/doc.md)
- [API Reference](api/ZenECS.Core.html)
- [Sample Code](../samples/01-basic.md)
```

## Writing Style Guide

### Voice and Tone

**Good:**
- "Create a world using the `World` constructor."
- "Systems process entities that match their queries."
- "Use `EntityLink` to connect Unity GameObjects to ECS entities."

**Bad:**
- "A world can be created using the `World` constructor." (passive)
- "Entities that match system queries are processed." (passive)
- "Unity GameObjects can be connected to ECS entities using `EntityLink`." (unclear)

### Terminology

Use consistent terms throughout:

| Use This | Not This |
|----------|----------|
| Entity | GameObject (in Core context) |
| Component | Data, Struct |
| System | Processor, Handler |
| World | Scene, Context |
| Query | Filter, Search |
| Message | Event, Signal |
| Binding | Linking, Connection |

### Technical Terms

- **First use**: Define the term
  - "A **World** is an isolated ECS simulation space..."
- **Subsequent uses**: Use the term directly
  - "Create a new World for each game mode."

### Code References

- **Inline code**: Use backticks for code elements
  - "Use the `CreateEntity()` method to create new entities."
- **Code blocks**: Use triple backticks with language
  - ` ```csharp` for C# code
- **File names**: Use backticks
  - "Edit the `EcsDriver.cs` file."

## Content Structure

### Introduction

Every document should start with:

1. **Title**: Clear and descriptive
2. **Breadcrumb**: `> Docs / Category / Subcategory`
3. **One-sentence summary**: What the document covers
4. **Overview section**: 2-3 paragraphs explaining the topic

### Sections

Organize content logically:

1. **Overview** - What and why
2. **Key Concepts** - Important ideas
3. **Usage** - How to use
4. **Examples** - Code examples
5. **Best Practices** - Recommendations
6. **Common Pitfalls** - What to avoid
7. **See Also** - Related documentation

### Examples Section

Structure examples from simple to complex:

```markdown
## Examples

### Basic Example

Simplest possible use case.

```csharp
// Minimal code
```

### Intermediate Example

More realistic scenario.

```csharp
// More complete code
```

### Advanced Example

Complex use case with edge cases.

```csharp
// Complex code with comments
```
```

## Code Examples

### Example Quality Standards

**Good Example:**
```csharp
using ZenECS.Core;

// Create a world for the game simulation
var world = new World("GameWorld");

// Add a movement system
world.AddSystems([new MovementSystem()]);

// Create a player entity with position and velocity using command buffer
Entity player;
using (var cmd = world.BeginWrite())
{
    player = cmd.CreateEntity();
    cmd.AddComponent(player, new Position(0, 0));
    cmd.AddComponent(player, new Velocity(1, 0));
}

// Run one frame of simulation (60 FPS)
world.Step(0.016f);
```

**Bad Example:**
```csharp
var w = new World("w");
var e = w.CreateEntity();
// Too abstract, missing context
```

### Example Guidelines

1. **Complete**: Include necessary using statements
2. **Realistic**: Use meaningful names and values
3. **Commented**: Explain non-obvious parts
4. **Tested**: Verify examples compile
5. **Context**: Show where code fits in larger picture

### Example Annotations

Use comments to explain:

```csharp
// Step 1: Create the world
var world = new World("GameWorld");

// Step 2: Register systems
world.AddSystems([new MovementSystem()]);

// Step 3: Create entities with components using command buffer
Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position(0, 0));
}
```

## Cross-References

### Linking to Other Documents

**Relative links** for internal docs:
```markdown
See [Getting Started Guide](getting-started/quickstart-basic.md)
```

**API references**:
```markdown
See [World API](api/ZenECS.Core.World.html)
```

**Sections within document**:
```markdown
See [Examples](#examples) section below.
```

### Linking Best Practices

- Link to related concepts
- Link to API documentation
- Link to sample code
- Link to troubleshooting guides
- Avoid broken links (test all links)

## Diagrams and Images

### When to Use Diagrams

- Architecture overviews
- Data flow diagrams
- Sequence diagrams
- State diagrams
- Component relationships

### Image Guidelines

- Use SVG when possible (scalable)
- Use PNG for screenshots
- Keep file sizes reasonable
- Use descriptive alt text
- Store in `Docs/images/` directory

### Diagram Format

```markdown
![Architecture Diagram](images/architecture.svg)

*Figure 1: ZenECS Architecture Overview*
```

## Troubleshooting Sections

### Common Issues Format

```markdown
## Troubleshooting

### Issue: Entity not found

**Symptoms:**
- `IsAlive()` returns false
- Query doesn't return expected entity

**Cause:**
Entity was destroyed or never created.

**Solution:**
```csharp
// Verify entity exists
if (world.IsAlive(entity)) {
    // Entity is valid
}
```

### Issue: System not running

**Symptoms:**
- System code not executing
- No entities processed

**Cause:**
System not registered or no matching entities.

**Solution:**
1. Verify system is added: `world.AddSystems([new MySystem()])`
2. Check query matches entities
3. Ensure `Step()` is called
```

## Best Practices Section

### Format

```markdown
## Best Practices

### ✅ Do

- **Use meaningful names**: `PlayerPosition` not `Pos`
- **Group related components**: Keep position and velocity together
- **Use queries efficiently**: Filter early in system pipeline

### ❌ Don't

- **Don't store references**: Entities are value types
- **Don't mutate in queries**: Use command buffers
- **Don't create entities in systems**: Use external commands
```

## API Documentation Integration

### Linking to API Docs

```markdown
See the [World API documentation](api/ZenECS.Core.World.html) for
complete method reference.

The `CreateEntity()` method creates a new entity. See
[IWorld.CreateEntity()](api/ZenECS.Core.IWorld.html#CreateEntity)
for details.
```

### Code Examples with API

```markdown
Use the `World` class to create simulation spaces:

```csharp
// See World API: api/ZenECS.Core.World.html
var world = new World("MyWorld");
```
```

## Version-Specific Content

### Marking Version Requirements

```markdown
> **Requires**: ZenECS Core 1.0.0+

This feature was introduced in version 1.0.0.
```

### Deprecated Content

```markdown
> **⚠️ Deprecated**: This API is deprecated as of version 1.2.0.
> Use [New API](api/NewAPI.html) instead.

~~OldMethod()~~ → Use `NewMethod()` instead
```

## Review Checklist

Before submitting documentation:

- [ ] **Content**: Accurate, complete, up-to-date
- [ ] **Structure**: Follows template, logical flow
- [ ] **Code**: Examples compile and work
- [ ] **Links**: All links valid and correct
- [ ] **Grammar**: No spelling or grammar errors
- [ ] **Formatting**: Consistent markdown formatting
- [ ] **Images**: Clear, properly referenced
- [ ] **Cross-refs**: Links to related docs
- [ ] **Preview**: Tested with DocFX build

## Common Mistakes to Avoid

### ❌ Don't Do This

1. **Vague titles**: "Guide" → "Entity Creation Guide"
2. **Missing context**: Explain why, not just how
3. **Outdated examples**: Keep code current
4. **Broken links**: Test all links
5. **Inconsistent terms**: Use terminology guide
6. **Missing examples**: Always include code
7. **Passive voice**: Use active voice
8. **Too technical**: Write for target audience

### ✅ Do This Instead

1. **Clear titles**: Specific and descriptive
2. **Provide context**: Explain purpose and use cases
3. **Current examples**: Test and update regularly
4. **Valid links**: Verify before submitting
5. **Consistent terms**: Follow terminology guide
6. **Practical examples**: Real-world scenarios
7. **Active voice**: Direct and clear
8. **Appropriate level**: Match audience expertise

## Resources

- [Markdown Cheat Sheet](https://www.markdownguide.org/cheat-sheet/)
- [Technical Writing Style Guide](https://developers.google.com/tech-writing/one)
- [DocFX Markdown Reference](https://dotnet.github.io/docfx/spec/docfx_flavored_markdown.html)
- [XML Documentation Comments](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/)

---

**Questions?** Contact the documentation team or open an issue.

