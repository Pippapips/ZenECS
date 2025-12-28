# Versioning

> Docs / Release / Versioning

ZenECS follows **Semantic Versioning (SemVer)** for version management.

## Semantic Versioning

Version format: **MAJOR.MINOR.PATCH**

- **MAJOR**: Incompatible API changes
- **MINOR**: Backward-compatible functionality additions
- **PATCH**: Backward-compatible bug fixes

### Examples

- **1.0.0** → **1.0.1**: Bug fix (patch)
- **1.0.0** → **1.1.0**: New feature (minor)
- **1.0.0** → **2.0.0**: Breaking change (major)

## Version Strategy

### Major Versions

Major version bumps indicate:

- **Breaking API changes**: Methods removed or signatures changed
- **Architecture changes**: Significant design changes
- **Incompatible updates**: Cannot upgrade without code changes

**Migration:** Major versions include migration guides.

### Minor Versions

Minor version bumps add:

- **New features**: Backward-compatible additions
- **New APIs**: Additional methods and types
- **Enhancements**: Improvements to existing features

**Migration:** Usually no code changes required.

### Patch Versions

Patch version bumps fix:

- **Bug fixes**: Correct behavior issues
- **Performance**: Optimizations
- **Documentation**: Documentation updates

**Migration:** No code changes required.

## Branching Strategy

### Main Branches

- **main**: Stable, production-ready code
- **develop**: Development branch for next release

### Feature Branches

- **feature/***: New features
- **fix/***: Bug fixes
- **docs/***: Documentation updates

## Release Process

### Release Candidate (RC)

Before major releases:

1. **Feature freeze**: No new features
2. **RC release**: `1.0.0-rc.1`, `1.0.0-rc.2`, etc.
3. **Testing**: Community testing and feedback
4. **Final release**: `1.0.0`

### Stable Release

After RC period:

1. **Final testing**: Comprehensive testing
2. **Documentation**: Complete documentation
3. **Release**: Tagged version release
4. **Announcement**: Release notes and announcements

## Version Compatibility

### API Compatibility

- **Same major version**: APIs are compatible
- **Different major version**: May require migration
- **Minor/Patch updates**: Always compatible

### Migration Policy

- **Major versions**: Migration guide provided
- **Minor versions**: Usually no migration needed
- **Patch versions**: No migration needed

## See Also

- [Upgrade Guide](../getting-started/upgrade-guide.md) - Migration steps
- [Roadmap](./roadmap.md) - Future plans
