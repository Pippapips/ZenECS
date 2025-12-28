# Error handling

> Docs / Core / Error handling

Exceptions, denials, and logging strategy.

## Overview

ZenECS provides a comprehensive error handling strategy that balances developer experience with production robustness. The framework uses exceptions for programming errors, configurable policies for denied operations, and a centralized error reporting system.

**Key Principles:**

- **Fail Fast**: Programming errors throw exceptions immediately
- **Configurable Policies**: Denied operations can throw, log, or ignore based on configuration
- **Safe Reporting**: Error reporting is guarded against cascading failures
- **Clear Diagnostics**: Error messages include context and actionable information

## Exception Types

ZenECS may throw the following exception types:

### `InvalidOperationException`

Thrown when an operation is invalid in the current state:

- **World not alive**: Attempting to use a destroyed world
- **Component missing**: Accessing a component that doesn't exist
- **System dependency cycle**: Circular dependencies detected in system ordering
- **Write denied**: Structural write denied by permission hook (when `WritePolicy` is `Throw`)
- **Invalid state**: Operation not allowed in current world/kernel state

**Example:**
```csharp
// Thrown when accessing a component that doesn't exist
var health = world.Get<Health>(entity); // InvalidOperationException if missing

// Thrown when write is denied
using (var cmd = world.BeginWrite())
{
    cmd.AddComponent(entity, new AdminComponent()); // May throw if permission denied
}
```

### `ArgumentException` / `ArgumentNullException`

Thrown when invalid arguments are passed:

- **Null arguments**: Required parameters are null
- **Invalid types**: Component type doesn't match expected type
- **Invalid values**: Argument values are out of valid range

**Example:**
```csharp
// Thrown when passing null kernel
var handle = new WorldHandle(null, worldId); // ArgumentNullException

// Thrown when component type is invalid
world.AddComponent(entity, (object)null); // ArgumentException
```

### `ObjectDisposedException`

Thrown when using disposed objects:

- **Kernel disposed**: Attempting to use a disposed kernel
- **World disposed**: Attempting to use a disposed world
- **Service container disposed**: Attempting to use disposed services

**Example:**
```csharp
kernel.Dispose();
var world = kernel.CreateWorld(); // ObjectDisposedException
```

### `KeyNotFoundException`

Thrown when a requested key/entity/context is not found:

- **Context not found**: Requested context doesn't exist for entity
- **Service not found**: Requested service not registered in DI container

**Example:**
```csharp
// Thrown when context doesn't exist
var context = world.GetContext<MyContext>(entity); // KeyNotFoundException if missing
```

### `NotSupportedException`

Thrown when an operation is not supported:

- **Formatter missing**: No formatter registered for component type during serialization
- **Type not supported**: Component type not supported for operation

**Example:**
```csharp
// Thrown when saving snapshot without formatter
world.SaveSnapshot(stream); // NotSupportedException if component has no formatter
```

### `EndOfStreamException`

Thrown during snapshot I/O when stream ends unexpectedly:

- **Corrupted snapshot**: Stream ends before expected data
- **Invalid format**: Snapshot format is invalid or corrupted

**Example:**
```csharp
// Thrown when loading corrupted snapshot
world.LoadSnapshot(stream); // EndOfStreamException if stream is corrupted
```

## Write Failure Policy

When a structural write operation (Add/Replace/Remove) is denied by a permission hook or validator, ZenECS uses a configurable policy to handle the failure.

### Policy Options

The `EcsRuntimeOptions.WritePolicy` property controls the behavior:

#### `Throw` (Default)

Immediately throws `InvalidOperationException` with a descriptive message. Best for development and strict correctness.

```csharp
EcsRuntimeOptions.WritePolicy = EcsRuntimeOptions.WriteFailurePolicy.Throw;

using (var cmd = world.BeginWrite())
{
    // Throws InvalidOperationException if write is denied
    cmd.AddComponent(entity, new Health(100));
}
```

#### `Log`

Logs a warning message and silently ignores the operation. Non-fatal, suitable for production environments where you want to continue execution.

```csharp
EcsRuntimeOptions.WritePolicy = EcsRuntimeOptions.WriteFailurePolicy.Log;

using (var cmd = world.BeginWrite())
{
    // Logs warning and continues if write is denied
    cmd.AddComponent(entity, new Health(100));
}
```

#### `Ignore`

Silently ignores the operation without logging. Use with caution in production.

```csharp
EcsRuntimeOptions.WritePolicy = EcsRuntimeOptions.WriteFailurePolicy.Ignore;

using (var cmd = world.BeginWrite())
{
    // Silently ignores if write is denied
    cmd.AddComponent(entity, new Health(100));
}
```

### Configuring Write Policy

Set the policy during application bootstrap:

```csharp
// Development: strict error checking
EcsRuntimeOptions.WritePolicy = EcsRuntimeOptions.WriteFailurePolicy.Throw;

// Production: graceful degradation
EcsRuntimeOptions.WritePolicy = EcsRuntimeOptions.WriteFailurePolicy.Log;
```

## Error Reporting

ZenECS provides a centralized error reporting mechanism through `EcsRuntimeOptions.Report()`.

### Reporting Non-Fatal Errors

Use `EcsRuntimeOptions.Report()` to report exceptions that shouldn't crash the application:

```csharp
try
{
    // Some operation that might fail
    ProcessEntity(entity);
}
catch (Exception ex)
{
    // Report but continue execution
    EcsRuntimeOptions.Report(ex, "ProcessEntity");
}
```

### Global Error Handler

Register a global error handler to be notified of all reported errors:

```csharp
EcsRuntimeOptions.OnUnhandledError = (ex) =>
{
    // Send to error tracking service (e.g., Sentry, Application Insights)
    ErrorTrackingService.CaptureException(ex);
    
    // Or log to file
    File.AppendAllText("errors.log", $"{DateTime.Now}: {ex}\n");
};
```

### Logging Integration

Configure a custom logger to integrate with your logging system:

```csharp
public class MyLogger : IEcsLogger
{
    public void Info(string message) => MyLoggingFramework.Info(message);
    public void Warn(string message) => MyLoggingFramework.Warn(message);
    public void Error(string message) => MyLoggingFramework.Error(message);
}

// Set during bootstrap
EcsRuntimeOptions.Log = new MyLogger();
```

## Best Practices

### 1. Use Try-Resolve Patterns

For operations that might fail, use try-resolve patterns instead of throwing:

```csharp
// ✅ Good: Check before use
if (world.TryGetComponent<Health>(entity, out var health))
{
    // Use health
}

// ❌ Avoid: Throws exception if missing
var health = world.Get<Health>(entity);
```

### 2. Handle Disposed Objects

Always check if objects are disposed before use:

```csharp
// ✅ Good: Check disposal state
if (!kernel.IsRunning)
    return;

// ❌ Avoid: May throw ObjectDisposedException
var world = kernel.CreateWorld();
```

### 3. Validate Before Operations

Validate entities and components before operations:

```csharp
// ✅ Good: Validate first
if (world.IsAlive(entity) && world.HasComponent<Health>(entity))
{
    var health = world.Get<Health>(entity);
}

// ❌ Avoid: May throw if entity is dead
var health = world.Get<Health>(entity);
```

### 4. Use Command Buffers Safely

Always use command buffers within `using` blocks:

```csharp
// ✅ Good: Automatic disposal
using (var cmd = world.BeginWrite())
{
    cmd.CreateEntity();
    // Automatically flushed and disposed
}

// ❌ Avoid: Manual disposal required
var cmd = world.BeginWrite();
// Must remember to dispose
```

### 5. Configure Policies Appropriately

Use different policies for different environments:

```csharp
#if DEVELOPMENT
    EcsRuntimeOptions.WritePolicy = EcsRuntimeOptions.WriteFailurePolicy.Throw;
#else
    EcsRuntimeOptions.WritePolicy = EcsRuntimeOptions.WriteFailurePolicy.Log;
#endif
```

### 6. Handle System Exceptions

Systems should handle their own exceptions to prevent cascading failures:

```csharp
[FixedGroup]
public sealed class MySystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        try
        {
            // System logic
            foreach (var (e, health) in w.Query<Health>())
            {
                // Process health
            }
        }
        catch (Exception ex)
        {
            // Report but don't crash
            EcsRuntimeOptions.Report(ex, nameof(MySystem));
        }
    }
}
```

## Examples

### Example 1: Handling Write Denials

```csharp
// Configure policy
EcsRuntimeOptions.WritePolicy = EcsRuntimeOptions.WriteFailurePolicy.Log;

// Add permission hook
world.AddWritePermission((entity, componentType) =>
{
    if (componentType == typeof(AdminComponent))
    {
        return world.HasComponent<Admin>(entity);
    }
    return true;
});

// Attempt write (may be denied)
try
{
    using (var cmd = world.BeginWrite())
    {
        cmd.AddComponent(entity, new AdminComponent());
    }
}
catch (InvalidOperationException ex)
{
    // Only thrown if WritePolicy is Throw
    Console.WriteLine($"Write denied: {ex.Message}");
}
```

### Example 2: Safe Component Access

```csharp
// Safe component access pattern
public void ProcessEntity(IWorld world, Entity entity)
{
    if (!world.IsAlive(entity))
        return;
    
    if (world.TryGetComponent<Health>(entity, out var health))
    {
        // Process health
        if (health.Current <= 0)
        {
            // Handle death
        }
    }
    
    if (world.TryGetComponent<Position>(entity, out var pos))
    {
        // Process position
    }
}
```

### Example 3: Error Reporting Setup

```csharp
// Bootstrap error handling
public void SetupErrorHandling()
{
    // Configure logger
    EcsRuntimeOptions.Log = new UnityLogger();
    
    // Configure write policy
    EcsRuntimeOptions.WritePolicy = EcsRuntimeOptions.WriteFailurePolicy.Log;
    
    // Register global error handler
    EcsRuntimeOptions.OnUnhandledError = (ex) =>
    {
        // Send to analytics
        Analytics.LogException(ex);
        
        // Show user-friendly message
        if (ex is InvalidOperationException)
        {
            UI.ShowError("An operation failed. Please try again.");
        }
    };
}
```

### Example 4: World Handle Safety

```csharp
// Store world handle (safe to serialize)
var handle = new WorldHandle(kernel, world.Id);

// Later: resolve safely
if (handle.TryResolve(out var world))
{
    // Use world
    world.AddSystems([new MySystem()]);
}
else
{
    // World was destroyed
    Console.WriteLine("World no longer exists");
}

// Or: resolve with exception
try
{
    var world = handle.ResolveOrThrow();
    // Use world
}
catch (InvalidOperationException)
{
    // Handle missing world
}
```

### Example 5: Snapshot Error Handling

```csharp
// Save snapshot with error handling
try
{
    using (var stream = File.Create("save.dat"))
    {
        world.SaveSnapshot(stream);
    }
}
catch (NotSupportedException ex)
{
    // Component type has no formatter
    EcsRuntimeOptions.Report(ex, "SaveSnapshot");
    Console.WriteLine("Some components could not be saved. Register formatters.");
}
catch (ArgumentException ex)
{
    // Invalid stream
    EcsRuntimeOptions.Report(ex, "SaveSnapshot");
    Console.WriteLine("Invalid stream for saving.");
}

// Load snapshot with error handling
try
{
    using (var stream = File.OpenRead("save.dat"))
    {
        world.LoadSnapshot(stream);
    }
}
catch (EndOfStreamException ex)
{
    // Corrupted snapshot
    EcsRuntimeOptions.Report(ex, "LoadSnapshot");
    Console.WriteLine("Snapshot file is corrupted.");
}
catch (InvalidOperationException ex)
{
    // Invalid format
    EcsRuntimeOptions.Report(ex, "LoadSnapshot");
    Console.WriteLine("Snapshot format is invalid.");
}
```

## FAQ

### When should I use `Throw` vs `Log` policy?

- **Development**: Use `Throw` to catch errors early and fix them
- **Production**: Use `Log` to gracefully handle errors without crashing
- **Testing**: Use `Throw` to ensure tests fail on invalid operations

### How do I prevent exceptions in my systems?

Use defensive programming patterns:

- Check entity liveness before operations
- Use `TryGetComponent` instead of `Get`
- Validate inputs before processing
- Handle exceptions within systems

### What happens if a system throws an exception?

Exceptions in systems propagate to the caller. If you're using `EcsDriver`, the exception will crash the Unity application. Always wrap system logic in try-catch blocks for production code.

### How do I debug permission denials?

1. Set `WritePolicy` to `Throw` to see exceptions immediately
2. Check permission hooks and validators
3. Use logging to trace which hook denied the operation
4. Enable verbose logging in your logger

### Can I customize error messages?

Error messages are generated by the framework, but you can add context when reporting:

```csharp
EcsRuntimeOptions.Report(ex, $"MySystem.ProcessEntity(entity={entity.Id})");
```

### How do I handle errors in async operations?

ZenECS operations are synchronous. If you're using async/await, handle exceptions in your async methods:

```csharp
async Task ProcessWorldAsync(IWorld world)
{
    try
    {
        await SomeAsyncOperation();
        // Use world synchronously
        world.AddSystems([new MySystem()]);
    }
    catch (Exception ex)
    {
        EcsRuntimeOptions.Report(ex, "ProcessWorldAsync");
    }
}
```

## See Also

- [Write Hooks & Validators](./write-hooks-validators.md) - Control write permissions
- [World Hook](./world-hook.md) - Hook system overview
- [Tracing & Logging](./tracing-logging.md) - Logging and diagnostics
- [Testing](./testing.md) - Testing error scenarios
- [Troubleshooting](../adapter-unity/troubleshooting.md) - Common issues and solutions
