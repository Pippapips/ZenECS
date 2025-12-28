# Tracing & Logging

> Docs / Core / Tracing & Logging

Tracing, counters, and logging strategy for ZenECS.

## Overview

ZenECS provides a lightweight logging system that can be integrated with any logging framework. The core uses a simple logger interface that you can implement to bridge to your preferred logging system.

**Key Concepts:**

- **Pluggable Logger**: Bridge to any logging framework
- **Severity Levels**: Info, Warn, Error
- **Error Reporting**: Centralized error reporting with callbacks
- **Minimal Dependencies**: Core remains dependency-free

## How It Works

### Logger Interface

ZenECS uses `IEcsLogger` interface for all logging:

```csharp
public interface IEcsLogger
{
    void Info(string message);
    void Warn(string message);
    void Error(string message);
}
```

### Default Logger

By default, ZenECS uses a no-op logger (`NullLogger`) that discards all messages. Replace it during bootstrap to enable logging.

### Error Reporting

Centralized error reporting via `EcsRuntimeOptions.Report()`:

```csharp
EcsRuntimeOptions.Report(exception, "Context");
```

## API Surface

### Logger Configuration

#### `EcsRuntimeOptions.Log`

Global logger instance:

```csharp
// Set custom logger
EcsRuntimeOptions.Log = new MyLogger();

// Or during kernel creation
var kernel = new Kernel(options, new MyLogger());
```

#### `IEcsLogger`

Logger interface:

```csharp
public interface IEcsLogger
{
    void Info(string message);
    void Warn(string message);
    void Error(string message);
}
```

### Error Reporting

#### `EcsRuntimeOptions.Report()`

Report non-fatal exceptions:

```csharp
public static void Report(Exception ex, string context = "")
```

#### `EcsRuntimeOptions.OnUnhandledError`

Global error handler callback:

```csharp
public static Action<Exception>? OnUnhandledError { get; set; }
```

## Examples

### Unity Logger Integration

```csharp
using UnityEngine;

public class UnityLogger : IEcsLogger
{
    public void Info(string message)
    {
        Debug.Log($"[ZenECS] {message}");
    }
    
    public void Warn(string message)
    {
        Debug.LogWarning($"[ZenECS] {message}");
    }
    
    public void Error(string message)
    {
        Debug.LogError($"[ZenECS] {message}");
    }
}

// Configure during bootstrap
EcsRuntimeOptions.Log = new UnityLogger();
```

### Serilog Integration

```csharp
using Serilog;

public class SerilogLogger : IEcsLogger
{
    private readonly ILogger _logger;
    
    public SerilogLogger(ILogger logger)
    {
        _logger = logger;
    }
    
    public void Info(string message)
    {
        _logger.Information("[ZenECS] {Message}", message);
    }
    
    public void Warn(string message)
    {
        _logger.Warning("[ZenECS] {Message}", message);
    }
    
    public void Error(string message)
    {
        _logger.Error("[ZenECS] {Message}", message);
    }
}

// Configure
var serilogLogger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
    
EcsRuntimeOptions.Log = new SerilogLogger(serilogLogger);
```

### NLog Integration

```csharp
using NLog;

public class NLogLogger : IEcsLogger
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public void Info(string message)
    {
        Logger.Info($"[ZenECS] {message}");
    }
    
    public void Warn(string message)
    {
        Logger.Warn($"[ZenECS] {message}");
    }
    
    public void Error(string message)
    {
        Logger.Error($"[ZenECS] {message}");
    }
}

// Configure
EcsRuntimeOptions.Log = new NLogLogger();
```

### File Logger

```csharp
public class FileLogger : IEcsLogger, IDisposable
{
    private readonly StreamWriter _writer;
    private readonly object _lock = new();
    
    public FileLogger(string filePath)
    {
        _writer = new StreamWriter(filePath, append: true)
        {
            AutoFlush = true
        };
    }
    
    public void Info(string message)
    {
        Write("INFO", message);
    }
    
    public void Warn(string message)
    {
        Write("WARN", message);
    }
    
    public void Error(string message)
    {
        Write("ERROR", message);
    }
    
    private void Write(string level, string message)
    {
        lock (_lock)
        {
            _writer.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
        }
    }
    
    public void Dispose()
    {
        _writer?.Dispose();
    }
}

// Configure
EcsRuntimeOptions.Log = new FileLogger("zenecs.log");
```

### Error Reporting Setup

```csharp
// Configure error reporting
EcsRuntimeOptions.OnUnhandledError = (ex) =>
{
    // Send to error tracking service
    ErrorTrackingService.CaptureException(ex);
    
    // Log to file
    File.AppendAllText("errors.log", $"{DateTime.UtcNow}: {ex}\n");
    
    // Show user-friendly message
    if (ex is InvalidOperationException)
    {
        UI.ShowError("An operation failed. Please try again.");
    }
};

// Report errors
try
{
    ProcessEntity(entity);
}
catch (Exception ex)
{
    EcsRuntimeOptions.Report(ex, "ProcessEntity");
}
```

### Conditional Logging

```csharp
public class ConditionalLogger : IEcsLogger
{
    private readonly IEcsLogger _baseLogger;
    private readonly bool _enableInfo;
    private readonly bool _enableWarn;
    private readonly bool _enableError;
    
    public ConditionalLogger(
        IEcsLogger baseLogger,
        bool enableInfo = true,
        bool enableWarn = true,
        bool enableError = true)
    {
        _baseLogger = baseLogger;
        _enableInfo = enableInfo;
        _enableWarn = enableWarn;
        _enableError = enableError;
    }
    
    public void Info(string message)
    {
        if (_enableInfo)
            _baseLogger.Info(message);
    }
    
    public void Warn(string message)
    {
        if (_enableWarn)
            _baseLogger.Warn(message);
    }
    
    public void Error(string message)
    {
        if (_enableError)
            _baseLogger.Error(message);
    }
}

// Configure with conditional logging
#if DEBUG
    EcsRuntimeOptions.Log = new ConditionalLogger(
        new UnityLogger(),
        enableInfo: true,
        enableWarn: true,
        enableError: true
    );
#else
    EcsRuntimeOptions.Log = new ConditionalLogger(
        new UnityLogger(),
        enableInfo: false,
        enableWarn: true,
        enableError: true
    );
#endif
```

### Structured Logging

```csharp
public class StructuredLogger : IEcsLogger
{
    private readonly ILogger _logger;  // Your structured logger
    
    public void Info(string message)
    {
        _logger.LogInformation("ZenECS: {Message}", message);
    }
    
    public void Warn(string message)
    {
        _logger.LogWarning("ZenECS: {Message}", message);
    }
    
    public void Error(string message)
    {
        _logger.LogError("ZenECS: {Message}", message);
    }
}
```

### System-Specific Logging

```csharp
[FixedGroup]
public sealed class MySystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        try
        {
            // System logic
            foreach (var (entity, health) in w.Query<Health>())
            {
                // Process health
            }
        }
        catch (Exception ex)
        {
            // Report with context
            EcsRuntimeOptions.Report(ex, $"{nameof(MySystem)}.Run");
        }
    }
}
```

## Best Practices

### 1. Configure Logger Early

Set logger during application bootstrap:

```csharp
// ✅ Good: Configure before creating kernel
EcsRuntimeOptions.Log = new UnityLogger();
var kernel = new Kernel();
```

### 2. Use Context in Error Reports

Provide context when reporting errors:

```csharp
// ✅ Good: With context
EcsRuntimeOptions.Report(ex, "MySystem.ProcessEntity");

// ❌ Bad: No context
EcsRuntimeOptions.Report(ex);
```

### 3. Handle Logger Failures

Loggers should be safe and not throw:

```csharp
public class SafeLogger : IEcsLogger
{
    private readonly IEcsLogger _baseLogger;
    
    public void Info(string message)
    {
        try
        {
            _baseLogger.Info(message);
        }
        catch
        {
            // Swallow logger failures to avoid cascading errors
        }
    }
    
    // Same for Warn and Error
}
```

### 4. Filter Sensitive Information

Don't log sensitive data:

```csharp
public class SanitizedLogger : IEcsLogger
{
    private readonly IEcsLogger _baseLogger;
    
    public void Error(string message)
    {
        var sanitized = SanitizeMessage(message);
        _baseLogger.Error(sanitized);
    }
    
    private string SanitizeMessage(string message)
    {
        // Remove sensitive patterns
        return System.Text.RegularExpressions.Regex.Replace(
            message,
            @"(password|token|secret|key)\s*[:=]\s*\S+",
            "$1: [REDACTED]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
    }
}
```

### 5. Use Appropriate Log Levels

```csharp
// ✅ Good: Appropriate levels
EcsRuntimeOptions.Log.Info("System initialized");
EcsRuntimeOptions.Log.Warn("Entity count approaching limit");
EcsRuntimeOptions.Log.Error("Failed to load snapshot");

// ❌ Bad: Wrong levels
EcsRuntimeOptions.Log.Error("System initialized");  // Should be Info
EcsRuntimeOptions.Log.Info("Failed to load snapshot");  // Should be Error
```

## Logging in Systems

### System Logging Pattern

```csharp
[FixedGroup]
public sealed class MySystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        // Log system start (optional, for debugging)
        // EcsRuntimeOptions.Log.Info($"[{nameof(MySystem)}] Running");
        
        try
        {
            // System logic
            ProcessEntities(w);
        }
        catch (Exception ex)
        {
            // Report errors with context
            EcsRuntimeOptions.Report(ex, nameof(MySystem));
        }
    }
    
    private void ProcessEntities(IWorld world)
    {
        // System implementation
    }
}
```

### Performance-Critical Logging

For performance-critical paths, use conditional compilation:

```csharp
#if ZENECS_DEBUG_LOGGING
    EcsRuntimeOptions.Log.Info($"Processing {count} entities");
#endif
```

## Error Reporting Patterns

### Pattern 1: Try-Catch with Reporting

```csharp
public void ProcessEntity(IWorld world, Entity entity)
{
    try
    {
        // Process entity
        var health = world.ReadComponent<Health>(entity);
        // ...
    }
    catch (Exception ex)
    {
        EcsRuntimeOptions.Report(ex, $"ProcessEntity(entity={entity.Id})");
    }
}
```

### Pattern 2: Global Error Handler

```csharp
// Setup once during bootstrap
EcsRuntimeOptions.OnUnhandledError = (ex) =>
{
    // Send to analytics
    Analytics.LogException(ex);
    
    // Log to file
    LogToFile(ex);
    
    // Notify user if critical
    if (IsCritical(ex))
    {
        UI.ShowError("A critical error occurred. Please restart.");
    }
};
```

### Pattern 3: Contextual Error Reporting

```csharp
public class EntityProcessor
{
    public void Process(IWorld world, Entity entity)
    {
        try
        {
            ProcessInternal(world, entity);
        }
        catch (Exception ex)
        {
            // Include entity context
            var context = $"EntityProcessor.Process(entity={entity.Id}, world={world.Name})";
            EcsRuntimeOptions.Report(ex, context);
        }
    }
}
```

## Debugging Tips

### 1. Enable Verbose Logging

```csharp
#if DEBUG
    EcsRuntimeOptions.Log = new VerboseLogger();
#endif
```

### 2. Log System Execution

```csharp
[FixedGroup]
public sealed class DebugSystem : ISystem
{
    public void Run(IWorld w, float dt)
    {
        #if DEBUG
            EcsRuntimeOptions.Log.Info($"[DebugSystem] Frame {w.FrameCount}, Tick {w.Tick}");
        #endif
    }
}
```

### 3. Track Entity Operations

```csharp
public class TrackingLogger : IEcsLogger
{
    private readonly IEcsLogger _baseLogger;
    private int _entityCreates = 0;
    private int _entityDestroys = 0;
    
    public void Info(string message)
    {
        if (message.Contains("Entity created"))
            _entityCreates++;
        else if (message.Contains("Entity destroyed"))
            _entityDestroys++;
            
        _baseLogger.Info(message);
    }
    
    public void LogStats()
    {
        _baseLogger.Info($"Stats: Creates={_entityCreates}, Destroys={_entityDestroys}");
    }
}
```

## FAQ

### How do I disable logging?

Use the default `NullLogger` (already default):

```csharp
// No configuration needed - NullLogger is default
// Or explicitly:
EcsRuntimeOptions.Log = new NullLogger();
```

### Can I use multiple loggers?

Yes, create a composite logger:

```csharp
public class CompositeLogger : IEcsLogger
{
    private readonly IEcsLogger[] _loggers;
    
    public CompositeLogger(params IEcsLogger[] loggers)
    {
        _loggers = loggers;
    }
    
    public void Info(string message)
    {
        foreach (var logger in _loggers)
            logger.Info(message);
    }
    
    // Same for Warn and Error
}

// Use
EcsRuntimeOptions.Log = new CompositeLogger(
    new UnityLogger(),
    new FileLogger("zenecs.log")
);
```

### How do I log to a file?

See the FileLogger example above, or use your preferred logging framework's file sink.

### Can I filter log messages?

Yes, create a filtering logger:

```csharp
public class FilteringLogger : IEcsLogger
{
    private readonly IEcsLogger _baseLogger;
    private readonly Func<string, bool> _filter;
    
    public FilteringLogger(IEcsLogger baseLogger, Func<string, bool> filter)
    {
        _baseLogger = baseLogger;
        _filter = filter;
    }
    
    public void Info(string message)
    {
        if (_filter(message))
            _baseLogger.Info(message);
    }
    
    // Same for Warn and Error
}

// Filter out verbose messages
EcsRuntimeOptions.Log = new FilteringLogger(
    new UnityLogger(),
    msg => !msg.Contains("[VERBOSE]")
);
```

## See Also

- [Error Handling](./error-handling.md) - Error handling strategy
- [Security](./security.md) - Logging security considerations
- [Performance](./performance.md) - Performance impact of logging
