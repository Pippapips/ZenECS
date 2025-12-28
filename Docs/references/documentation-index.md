# Documentation Index

> Docs / References / Documentation Index

Complete index of all ZenECS documentation organized by topic and purpose.

## Quick Navigation

- [Getting Started](#getting-started)
- [Core Concepts](#core-concepts)
- [Unity Integration](#unity-integration)
- [Advanced Topics](#advanced-topics)
- [API Reference](#api-reference)
- [Samples & Examples](#samples--examples)
- [Tools & Utilities](#tools--utilities)
- [Community & Support](#community--support)

## Getting Started

### Installation

- [Install via Unity Package Manager (UPM)](../getting-started/install-upm.md)
- [Install via NuGet](../getting-started/install-nuget.md)
- [Unity Project Setup](../getting-started/project-setup-unity.md)

### Quick Start Guides

- [Quick Start (Basic)](../getting-started/quickstart-basic.md)
- [Samples Overview](../getting-started/samples-overview.md)
- [Developer Guide (Korean)](../getting-started/developer-guide-ko.md)

### Migration

- [Upgrade Guide](../getting-started/upgrade-guide.md)

## Core Concepts

### Overview

- [ZenECS at a Glance](../overview/zenecs-at-a-glance.md)
- [What is ECS?](../overview/what-is-ecs.md)
- [Architecture](../overview/architecture.md)
- [Design Philosophy](../overview/design-philosophy.md)
- [Glossary](../overview/glossary.md)
- [FAQ](../overview/faq.md)

### Core Features

- [World](../core/world.md) - ECS simulation spaces
- [Entities](../core/entities.md) - Entity management
- [Components](../core/components.md) - Data structures
- [Systems](../core/systems.md) - Logic processing
- [System Runner](../core/system-runner.md) - System execution
- [EcsKernel](../core/ecs-kernel.md) - Kernel management
- [EcsHost](../core/ecs-host.md) - Host integration

### Advanced Features

- [Message Bus](../core/message-bus.md) - Event messaging
- [Command Buffer](../core/command-buffer.md) - Deferred operations
- [Binding](../core/binding.md) - View integration
- [World Hook](../core/world-hook.md) - Lifecycle hooks
- [Write Hooks & Validators](../core/write-hooks-validators.md) - Validation
- [World Reset](../core/world-reset.md) - State reset
- [Snapshot I/O](../core/snapshot-io.md) - Save/load
- [Migration & PostMig](../core/migration-postmig.md) - Version migration

### System Features

- [Policy & Permissions](../core/policy-permissions.md) - Access control
- [Tracing & Logging](../core/tracing-logging.md) - Debugging
- [Error Handling](../core/error-handling.md) - Error management
- [Performance](../core/performance.md) - Optimization
- [Memory Growth Policy](../core/memory-growth-policy.md) - Memory management
- [Serialization](../core/serialization.md) - Data serialization
- [Stable IDs](../core/stable-ids.md) - Entity identification
- [Testing](../core/testing.md) - Testing strategies

## Unity Integration

### Setup & Configuration

- [Overview](../adapter-unity/overview.md)
- [Setup](../adapter-unity/setup.md)

### Features

- [View Binder](../adapter-unity/view-binder.md) - GameObject binding
- [Input → Intent](../adapter-unity/input-intent.md) - Input handling
- [FixedStep vs Update](../adapter-unity/fixedstep-update.md) - Update modes
- [Zenject / UniRx](../adapter-unity/unity-di-unirx.md) - DI integration

### Troubleshooting

- [Troubleshooting](../adapter-unity/troubleshooting.md)

## Advanced Topics

- [Advanced Topics](../guides/advanced-topics.md)
- [Blueprint Components](../guides/blueprint-components.md)
- [Surrogate Components](../guides/surrogate-components.md)
- [Extending ZenECS](../guides/extending-zenecs.md)
- [Contributing Examples](../guides/contributing-examples.md)

## API Reference

### Core API

- [API Reference](../core/api-reference.md) - Overview
- [ZenECS.Core](api/ZenECS.Core.html) - Core namespace
- [ZenECS.Core.Systems](api/ZenECS.Core.Systems.html) - System interfaces
- [ZenECS.Core.Messaging](api/ZenECS.Core.Messaging.html) - Message bus
- [ZenECS.Core.Binding](api/ZenECS.Core.Binding.html) - Binding contracts

### Unity Adapter API

- [ZenECS.Adapter.Unity](api/ZenECS.Adapter.Unity.html) - Unity integration
- [ZenECS.Adapter.Unity.Linking](api/ZenECS.Adapter.Unity.Linking.html) - Entity linking
- [ZenECS.Adapter.Unity.Blueprints](api/ZenECS.Adapter.Unity.Blueprints.html) - Blueprints
- [ZenECS.Adapter.Unity.Binding](api/ZenECS.Adapter.Unity.Binding.html) - Unity binding

### API Index

- [Complete API Index](../references/api-index.md)

## Samples & Examples

### Basic Samples

- [01 - Basic](../samples/01-basic.md) - Basic ECS usage
- [02 - Binding](../samples/02-binding.md) - View binding
- [03 - Messages](../samples/03-messages.md) - Message bus

### Advanced Samples

- [04 - Command Buffer](../samples/04-command-buffer.md) - Deferred operations
- [05 - World Reset](../samples/05-world-reset.md) - State management
- [06 - World Hook](../samples/06-world-hook.md) - Lifecycle hooks
- [07 - WriteHooks & Validators](../samples/07-writehooks-validators.md) - Validation
- [08 - System Runner](../samples/08-system-runner.md) - System execution

## Tools & Utilities

- [ECS Explorer](../tooling/ecs-explorer.md) - Entity browser
- [Trace Center](../tooling/trace-center.md) - Debugging tool
- [Editor Windows](../tooling/editor-windows.md) - Unity editor tools
- [Codegen](../tooling/codegen.md) - Code generation

## Community & Support

- [Contributing](../community/contributing.md) - How to contribute
- [Code of Conduct](../community/code-of-conduct.md) - Community guidelines
- [Security](../community/security.md) - Security policy
- [Governance](../community/governance.md) - Project governance
- [Support](../community/support.md) - Get help

## Release Information

- [Versioning](../release/versioning.md) - Version strategy
- [Roadmap](../release/roadmap.md) - Future plans

## References

- [Documentation Guidelines](./documentation-guidelines.md) - Writing guidelines
- [Writing Manual](./writing-manual.md) - Writing guide
- [DocFX Integration Guide](./docfx-integration-guide.md) - Build guide
- [API Index](./api-index.md) - API reference index
- [Schema](./schema.md) - Data schemas
- [Repository Structure](./repo-structure.md) - Project structure

## Documentation for Contributors

If you're contributing to ZenECS documentation:

1. Read [Documentation Guidelines](./documentation-guidelines.md)
2. Follow [Writing Manual](./writing-manual.md)
3. Use [DocFX Integration Guide](./docfx-integration-guide.md) for builds
4. Check [Contributing Guide](../community/contributing.md)

## Search Tips

### Finding Information

- **API Reference**: Use [API Index](./api-index.md) or search in generated API docs
- **Concepts**: Check [Overview](../overview/) section
- **How-to Guides**: See [Getting Started](../getting-started/) and [Guides](../guides/)
- **Examples**: Browse [Samples](../samples/)
- **Troubleshooting**: Check [Troubleshooting](../adapter-unity/troubleshooting.md) and [FAQ](../overview/faq.md)

### Documentation Structure

- **Overview**: High-level concepts and philosophy
- **Getting Started**: Installation and quick start
- **Core**: Detailed feature documentation
- **Adapter Unity**: Unity-specific guides
- **Guides**: Advanced topics and best practices
- **Samples**: Code examples
- **References**: Technical references and guidelines

---

**Last Updated**: 2026-01-XX  
**Maintainer**: ZenECS Documentation Team

