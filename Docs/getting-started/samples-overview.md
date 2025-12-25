# Samples Overview

> Docs / Getting started / Samples overview

Complete index of all ZenECS samples with descriptions and learning paths.

## How to Run

### Unity Samples

1. Open Unity project with ZenECS installed
2. Navigate to `Packages/com.zenecs.core/Samples~` or `Packages/com.zenecs.adapter.unity/Samples~`
3. Open scene file
4. Press Play

### .NET Samples

```bash
cd Packages/com.zenecs.core/Samples~/XX-SampleName
dotnet run
```

## Sample Index

### Core Samples

#### [01 - Basic](../samples/01-basic.md)

**Description:** Minimal console sample demonstrating world creation, entity management, and system execution.

**Concepts:**
- World creation
- Entity creation
- Component management
- Basic systems

**Difficulty:** ⭐ Beginner

#### [02 - Binding](../samples/02-binding.md)

**Description:** Bind ECS components to Unity GameObjects reactively.

**Concepts:**
- EntityLink
- IBinder interface
- ComponentDelta
- Reactive updates

**Difficulty:** ⭐⭐ Intermediate

#### [03 - Messages](../samples/03-messages.md)

**Description:** Event-driven architecture using the message bus.

**Concepts:**
- Message publishing
- Message consumption
- Event-driven patterns
- Message pumping

**Difficulty:** ⭐⭐ Intermediate

#### [04 - Command Buffer](../samples/04-command-buffer.md)

**Description:** Safely batch structural changes with command buffers.

**Concepts:**
- Command buffers
- Structural changes
- Safe boundaries
- Batching operations

**Difficulty:** ⭐⭐ Intermediate

#### [05 - World Reset](../samples/05-world-reset.md)

**Description:** Reset world state while keeping systems registered.

**Concepts:**
- World reset
- State management
- Game restarts
- Level transitions

**Difficulty:** ⭐⭐ Intermediate

#### [06 - World Hook](../samples/06-world-hook.md)

**Description:** Use world hooks to intercept component operations.

**Concepts:**
- Write permission hooks
- Value validators
- Custom logic
- Interception

**Difficulty:** ⭐⭐⭐ Advanced

#### [07 - WriteHooks & Validators](../samples/07-writehooks-validators.md)

**Description:** Control component writes and validate values.

**Concepts:**
- Write permissions
- Value validation
- Security patterns
- Data integrity

**Difficulty:** ⭐⭐⭐ Advanced

#### [08 - System Runner](../samples/08-system-runner.md)

**Description:** Understand system execution order and groups.

**Concepts:**
- System groups
- Execution order
- Frame structure
- System pipeline

**Difficulty:** ⭐⭐ Intermediate

## Learning Path

### Beginner Path

1. **[01 - Basic](../samples/01-basic.md)** - Start here
2. **[03 - Messages](../samples/03-messages.md)** - Event-driven
3. **[04 - Command Buffer](../samples/04-command-buffer.md)** - Safe changes

### Intermediate Path

1. **[02 - Binding](../samples/02-binding.md)** - Unity integration
2. **[05 - World Reset](../samples/05-world-reset.md)** - State management
3. **[08 - System Runner](../samples/08-system-runner.md)** - Execution order

### Advanced Path

1. **[06 - World Hook](../samples/06-world-hook.md)** - Custom hooks
2. **[07 - WriteHooks & Validators](../samples/07-writehooks-validators.md)** - Validation

## Common Pitfalls

### Issue: Samples Don't Run

**Solutions:**
- Verify ZenECS is installed
- Check Unity version (2021.3+)
- Verify package references
- Check for compilation errors

### Issue: Entities Not Found

**Solutions:**
- Verify entities are created
- Check components are added
- Ensure world is being stepped
- Use ECS Explorer to inspect

### Issue: Systems Not Running

**Solutions:**
- Check system is registered
- Verify system has correct attribute
- Ensure world is being stepped
- Check system group is correct

## FAQ

### Q: Which sample should I start with?

**A:** Start with [01 - Basic](../samples/01-basic.md) to understand core concepts.

### Q: Can I modify the samples?

**A:** Yes! Samples are meant to be modified and experimented with.

### Q: Where are the sample files?

**A:** Samples are in `Packages/com.zenecs.core/Samples~` and `Packages/com.zenecs.adapter.unity/Samples~`.

## See Also

- [Quick Start Guide](quickstart-basic.md) - Getting started
- [Core Concepts](../core/world.md) - ECS fundamentals
- [Unity Adapter](../adapter-unity/overview.md) - Unity integration
