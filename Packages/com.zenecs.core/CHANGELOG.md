# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned
- Performance optimizations
- Additional Unity editor tools
- Enhanced documentation
- More sample projects

---

## [1.0.0] - 2025-01-27

### Added

#### Core Features
- **Kernel**: Multi-world management and frame coordination
- **World**: Complete ECS runtime with entities, components, systems
- **Systems**: Fixed and frame group execution with ordering
- **Message Bus**: Struct-based pub/sub messaging
- **Command Buffers**: Safe structural change batching
- **Binding System**: Reactive component-to-view binding
- **Snapshot I/O**: Save/load world state with migrations
- **Hooks & Validators**: Write permissions and value validation

#### Unity Adapter
- **EcsDriver**: Automatic kernel lifecycle management
- **EntityLink**: GameObject-to-entity linking
- **EntityBlueprint**: Data-driven entity spawning
- **System Presets**: Asset-based system configuration
- **Editor Tools**: ECS Explorer, custom inspectors
- **Zenject Integration**: Optional DI support
- **UniRx Integration**: Optional reactive programming support

#### Documentation
- Complete API documentation (auto-generated)
- Comprehensive guides and tutorials
- Sample projects and code examples
- Architecture documentation

### Changed

- Initial release - no previous versions

### Fixed

- N/A (initial release)

### Security

- N/A (initial release)

---

## Upgrade Notes

### From Pre-1.0 (Beta/Alpha)

If upgrading from pre-1.0 versions:

1. **Review API Changes**: Check breaking changes
2. **Update Code**: Follow migration guide
3. **Test Thoroughly**: Verify functionality
4. **Update Dependencies**: Ensure compatible versions

### Breaking Changes

**1.0.0** is the first stable release. No breaking changes from previous versions (if any).

---

## Links

- [Unreleased]: https://github.com/Pippapips/ZenECS/compare/1.0.0...HEAD
- [1.0.0]: https://github.com/Pippapips/ZenECS/releases/tag/v1.0.0

---

**Note:** This changelog is maintained manually. For detailed API changes, see the [API Reference](../core/api-reference.md).
