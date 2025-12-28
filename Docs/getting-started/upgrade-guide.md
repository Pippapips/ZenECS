# Upgrade Guide

> Docs / Getting started / Upgrade guide

Migration guide for upgrading between ZenECS versions.

## Overview

This guide helps you upgrade ZenECS between versions, covering breaking changes and migration steps.

## Version History

### 1.0.0 (Current)

**Status:** Release Candidate

**Breaking Changes:**
- None (first stable release)

**New Features:**
- Initial release
- Core ECS runtime
- Unity adapter
- API documentation

## Upgrade Process

### Step 1: Backup

Before upgrading:
1. **Backup your project**
2. **Commit changes** to version control
3. **Test current version** to establish baseline

### Step 2: Update Packages

**Unity (UPM):**
```
https://github.com/Pippapips/ZenECS.git?path=Packages/com.zenecs.core#v1.0.0
```

**NuGet:**
```bash
dotnet add package ZenECS.Core --version 1.0.0
```

### Step 3: Review Changes

Check:
- **Breaking Changes**: API changes
- **Migration Notes**: Specific migration steps

### Step 4: Update Code

Follow migration steps for your version:
- Update API calls
- Fix breaking changes
- Update deprecated APIs

### Step 5: Test

Verify:
- Code compiles
- Tests pass
- Runtime behavior correct

## Common Issues

### API Changes

**Issue:** API methods changed

**Solution:**
- Review API documentation
- Update method calls
- Check migration notes

### Namespace Changes

**Issue:** Namespaces moved

**Solution:**
- Update using statements
- Check new namespace locations
- Review API reference

### Behavior Changes

**Issue:** Runtime behavior differs

**Solution:**
- Review changelog
- Check migration notes
- Test thoroughly

## Migration Checklists

### Pre-Upgrade

- [ ] Backup project
- [ ] Commit to version control
- [ ] Review changelog
- [ ] Check breaking changes

### During Upgrade

- [ ] Update packages
- [ ] Fix compilation errors
- [ ] Update deprecated APIs
- [ ] Review migration notes

### Post-Upgrade

- [ ] Run tests
- [ ] Verify functionality
- [ ] Check performance
- [ ] Update documentation

## Getting Help

If you encounter issues:

1. **Search Issues**: Look for similar problems
3. **Create Issue**: Provide detailed information
4. **Ask Community**: Use GitHub Discussions

## See Also

- [Versioning](../release/versioning.md) - Version strategy
- [Support](../community/support.md) - Get help
