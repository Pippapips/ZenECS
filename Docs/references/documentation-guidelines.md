# Documentation Guidelines

> Docs / References / Documentation Guidelines

This document provides comprehensive guidelines for writing, maintaining, and organizing ZenECS documentation.

## Table of Contents

- [Overview](#overview)
- [Documentation Structure](#documentation-structure)
- [Writing Standards](#writing-standards)
- [Content Guidelines](#content-guidelines)
- [API Documentation](#api-documentation)
- [Code Examples](#code-examples)
- [Markdown Standards](#markdown-standards)
- [Localization](#localization)
- [Review Process](#review-process)
- [Maintenance](#maintenance)

## Overview

ZenECS documentation is organized into several categories:

1. **API Documentation** - Auto-generated from XML comments in source code
2. **Conceptual Documentation** - Manual guides explaining concepts and architecture
3. **Tutorials** - Step-by-step guides for common tasks
4. **Samples** - Code examples demonstrating usage
5. **Reference** - Technical references and specifications

### Documentation Goals

- **Clarity**: Write for developers who are new to ZenECS
- **Completeness**: Cover all features and use cases
- **Accuracy**: Keep documentation in sync with code
- **Accessibility**: Make information easy to find and understand
- **Maintainability**: Structure for easy updates

## Documentation Structure

### Directory Organization

```
Docs/
├── overview/           # High-level concepts and philosophy
├── getting-started/    # Installation and quick start guides
├── core/              # Core ECS concepts and features
├── adapter-unity/     # Unity-specific integration guides
├── guides/             # Advanced topics and best practices
├── samples/            # Code examples and tutorials
├── tooling/            # Editor tools and utilities
├── release/            # Changelog, versioning, roadmap
├── community/          # Contributing, support, governance
└── references/         # Technical references and schemas
```

### File Naming Conventions

- Use **kebab-case** for all file names: `quickstart-basic.md`
- Be descriptive: `world-reset.md` not `reset.md`
- Use numbers for ordered content: `01-basic.md`, `02-binding.md`
- Keep names short but clear: `entity-blueprint.md` not `entity-blueprint-system.md`

### Document Structure Template

Each document should follow this structure:

```markdown
# Title

> Docs / Category / Subcategory

Brief one-sentence description of what this document covers.

## Overview

High-level explanation of the topic.

## Key Concepts

- Concept 1
- Concept 2

## Usage

How to use the feature.

## Examples

Code examples demonstrating usage.

## Best Practices

Recommended approaches and patterns.

## See Also

- Related documentation links
- API references
- Sample code
```

## Writing Standards

### Tone and Style

- **Professional but friendly**: Write as if explaining to a colleague
- **Active voice**: "Create a world" not "A world is created"
- **Clear and concise**: Avoid unnecessary words
- **Consistent terminology**: Use the same terms throughout

### Language

- **Primary language**: English
- **Code comments**: English only
- **Variable names**: English only

### Target Audience

Write for three levels:

1. **Beginners**: Clear explanations, step-by-step guides
2. **Intermediate**: Practical examples, common patterns
3. **Advanced**: Edge cases, performance considerations, internals

## Content Guidelines

### What to Document

- **All public APIs**: Every public class, method, property
- **Concepts**: Core ECS concepts and ZenECS-specific features
- **Workflows**: Common tasks and their solutions
- **Best practices**: Recommended patterns and anti-patterns
- **Troubleshooting**: Common issues and solutions
- **Migration guides**: How to upgrade between versions

### What NOT to Document

- Internal implementation details (unless in advanced guides)
- Deprecated features (mark as deprecated, don't expand)
- Platform-specific workarounds (unless critical)
- Temporary solutions (document permanent ones)

### Documentation Types

#### Conceptual Documentation

Explain **what** and **why**:

- Architecture decisions
- Design philosophy
- Core concepts
- Patterns and best practices

#### Tutorial Documentation

Explain **how**:

- Step-by-step instructions
- Common workflows
- Integration guides
- Migration paths

#### Reference Documentation

Explain **what exactly**:

- API signatures
- Configuration options
- Data structures
- Technical specifications

## API Documentation

### XML Comments

All public APIs must have XML documentation comments:

```csharp
/// <summary>
/// Creates a new entity in the world.
/// </summary>
/// <param name="world">The world to create the entity in.</param>
/// <returns>A new entity instance.</returns>
/// <remarks>
/// <para>
/// Entities are created empty and must have components added
/// before they can be used in systems.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// Entity entity;
/// using (var cmd = world.BeginWrite())
/// {
///     entity = cmd.CreateEntity();
///     cmd.AddComponent(entity, new Position(0, 0));
/// }
/// </code>
/// </example>
public Entity CreateEntity(IWorld world) { }
```

### XML Comment Guidelines

- **Summary**: One clear sentence describing the member
- **Parameters**: Document all parameters with `<param>`
- **Returns**: Document return values with `<returns>`
- **Remarks**: Additional context, behavior notes, warnings
- **Examples**: Code examples showing usage
- **See Also**: Links to related APIs with `<see cref="..."/>`

### API Documentation Best Practices

- Start summaries with a verb: "Creates", "Gets", "Sets"
- Be specific about behavior and side effects
- Document exceptions with `<exception>`
- Link related APIs with `<see cref="..."/>`
- Include code examples for complex APIs

## Code Examples

### Example Guidelines

- **Complete**: Examples should be runnable (or clearly marked as snippets)
- **Realistic**: Use realistic scenarios, not contrived examples
- **Commented**: Explain non-obvious parts
- **Tested**: Verify examples compile and work
- **Updated**: Keep examples in sync with API changes

### Example Format

```csharp
// Good example
using ZenECS.Core;

// Create a world and add a system
var world = new World("GameWorld");
world.AddSystems([new MovementSystem()]);

// Create an entity with components using command buffer
Entity entity;
using (var cmd = world.BeginWrite())
{
    entity = cmd.CreateEntity();
    cmd.AddComponent(entity, new Position(0, 0));
    cmd.AddComponent(entity, new Velocity(1, 0));
}

// Run the simulation
world.Step(0.016f); // 60 FPS
```

### Bad Example (Avoid)

```csharp
// Too abstract
var w = new World("w");
var e = w.CreateEntity();
// Missing context, unclear purpose
```

## Markdown Standards

### Headers

- Use `#` for document title (H1)
- Use `##` for major sections (H2)
- Use `###` for subsections (H3)
- Use `####` for minor points (H4)
- Don't skip levels

### Links

- **Relative links** for internal documentation: `[Getting Started](getting-started/quickstart-basic.md)`
- **Absolute links** for external resources: `[Unity Documentation](https://docs.unity3d.com/)`
- **Anchor links** for sections: `[API Reference](#api-reference)`

### Code Blocks

- Always specify language: ` ```csharp`
- Use ` ```text` for non-code content
- Use ` ```bash` for shell commands
- Indent code blocks properly

### Lists

- Use bullet lists for unordered items
- Use numbered lists for ordered steps
- Use definition lists for terms: `Term: Definition`

### Emphasis

- **Bold** for important terms and UI elements
- *Italic* for emphasis and variable names
- `Code` for code elements and file names
- ~~Strikethrough~~ for deprecated content

### Tables

Use tables for structured data:

```markdown
| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Entity name |
| `Id` | `int` | Unique identifier |
```

## Localization

### Primary Language

- **English** is the primary language
- All documentation is in English
- English documentation is the source of truth

## Review Process

### Documentation Review Checklist

- [ ] Content is accurate and up-to-date
- [ ] Code examples compile and work
- [ ] Links are valid and point to correct locations
- [ ] Grammar and spelling are correct
- [ ] Formatting is consistent
- [ ] Images and diagrams are clear
- [ ] Cross-references are correct

### Review Workflow

1. **Author**: Write initial documentation
2. **Self-review**: Check against guidelines
3. **Technical review**: Verify accuracy with code
4. **Editorial review**: Check grammar and style
5. **Final approval**: Merge to main branch

## Maintenance

### Keeping Documentation Current

- **Update with code changes**: When APIs change, update docs
- **Review regularly**: Schedule quarterly documentation reviews
- **Remove outdated content**: Delete or archive deprecated docs
- **Update examples**: Keep code examples working

### Documentation Ownership

- **Core features**: Core team maintains
- **Unity adapter**: Unity team maintains
- **Samples**: Community contributions welcome
- **Translations**: Native speakers maintain

### Versioning

- Document breaking changes in upgrade guides
- Maintain changelog for significant documentation updates
- Version API documentation with code versions
- Archive old versions when appropriate

## Tools and Workflows

### DocFX Integration

- API docs auto-generated from XML comments
- Manual docs in `Docs/` folder
- Combined output in `Docs_/_site/`
- Build with: `.\scripts\build-docfx.ps1`

### Writing Tools

- **Markdown editors**: VS Code, Typora, Mark Text
- **Spell checkers**: Grammarly, LanguageTool
- **Link checkers**: markdown-link-check
- **Linters**: markdownlint

### Contribution Workflow

1. Create branch: `docs/feature-name`
2. Write documentation
3. Build and preview: `docfx serve Docs_/_site`
4. Submit PR with documentation changes
5. Review and merge

## Resources

- [Markdown Guide](https://www.markdownguide.org/)
- [DocFX Documentation](https://dotnet.github.io/docfx/)
- [XML Documentation Comments](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/)
- [Technical Writing Best Practices](https://developers.google.com/tech-writing)

---

**Last Updated**: 2026-01-XX  
**Maintainer**: ZenECS Documentation Team

