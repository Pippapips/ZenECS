# Security

> Docs / Core / Security

Security considerations and best practices for ZenECS.

## Overview

ZenECS is designed with security in mind, but proper configuration and usage patterns are essential for secure applications. This guide covers security considerations for serialization, input validation, threading, and access control.

**Key Security Principles:**

- **Defense in Depth**: Multiple layers of security
- **Principle of Least Privilege**: Restrict access to minimum necessary
- **Input Validation**: Validate all external inputs
- **Secure Defaults**: Safe defaults with explicit opt-in for risky operations
- **Audit Trail**: Log security-relevant events

## Serialization Security

Serialization is a common attack vector. ZenECS provides mechanisms to secure serialization operations.

### Untrusted Data

**Never deserialize untrusted data without validation.**

```csharp
// ❌ DANGEROUS: Loading untrusted snapshot
using (var stream = File.OpenRead("user_uploaded_save.dat"))
{
    world.LoadSnapshot(stream); // Risk: Type confusion, code injection
}

// ✅ SAFE: Validate source and use allowlist
if (!IsTrustedSource(filePath))
    throw new SecurityException("Untrusted source");

// Use allowlist of allowed component types
var allowedTypes = new HashSet<Type> { typeof(Position), typeof(Health) };
world.LoadSnapshot(stream, allowedTypes);
```

### Type Safety

**Validate component types during deserialization:**

```csharp
public class SecureSnapshotLoader
{
    private readonly HashSet<string> _allowedStableIds;
    
    public SecureSnapshotLoader()
    {
        // Only allow known, safe component types
        _allowedStableIds = new HashSet<string>
        {
            ComponentRegistry.GetId<Position>(),
            ComponentRegistry.GetId<Health>(),
            ComponentRegistry.GetId<Velocity>()
        };
    }
    
    public void LoadSnapshot(IWorld world, Stream stream)
    {
        // Validate types before loading
        var snapshotTypes = ReadSnapshotTypes(stream);
        foreach (var typeId in snapshotTypes)
        {
            if (!_allowedStableIds.Contains(typeId))
            {
                throw new SecurityException($"Disallowed component type: {typeId}");
            }
        }
        
        // Safe to load after validation
        world.LoadSnapshot(stream);
    }
}
```

### Custom Formatters

**Implement secure formatters for sensitive data:**

```csharp
public class SecureHealthFormatter : IComponentFormatter
{
    public Type ComponentType => typeof(Health);
    
    public void Write(object boxed, ISnapshotBackend backend)
    {
        var health = (Health)boxed;
        
        // Validate before serialization
        if (health.Current < 0 || health.Current > health.Max)
            throw new ArgumentException("Invalid health value");
        
        backend.WriteFloat(health.Current);
        backend.WriteFloat(health.Max);
    }
    
    public object Read(ISnapshotBackend backend)
    {
        var current = backend.ReadFloat();
        var max = backend.ReadFloat();
        
        // Validate after deserialization
        if (current < 0 || current > max || max <= 0)
            throw new SecurityException("Invalid health data in snapshot");
        
        return new Health { Current = current, Max = max };
    }
}
```

### Snapshot Integrity

**Verify snapshot integrity:**

```csharp
public class SecureSnapshotManager
{
    public void SaveSnapshot(IWorld world, Stream stream, byte[] secretKey)
    {
        using (var memoryStream = new MemoryStream())
        {
            world.SaveSnapshot(memoryStream);
            
            // Compute hash
            var hash = ComputeHMAC(memoryStream.ToArray(), secretKey);
            
            // Write hash before snapshot
            stream.Write(hash, 0, hash.Length);
            memoryStream.Position = 0;
            memoryStream.CopyTo(stream);
        }
    }
    
    public void LoadSnapshot(IWorld world, Stream stream, byte[] secretKey)
    {
        // Read and verify hash
        var hash = new byte[32];
        stream.Read(hash, 0, hash.Length);
        
        var snapshotData = new byte[stream.Length - hash.Length];
        stream.Read(snapshotData, 0, snapshotData.Length);
        
        var computedHash = ComputeHMAC(snapshotData, secretKey);
        if (!ConstantTimeEquals(hash, computedHash))
        {
            throw new SecurityException("Snapshot integrity check failed");
        }
        
        // Safe to load
        using (var snapshotStream = new MemoryStream(snapshotData))
        {
            world.LoadSnapshot(snapshotStream);
        }
    }
    
    private byte[] ComputeHMAC(byte[] data, byte[] key)
    {
        using (var hmac = new System.Security.Cryptography.HMACSHA256(key))
        {
            return hmac.ComputeHash(data);
        }
    }
    
    private bool ConstantTimeEquals(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        int result = 0;
        for (int i = 0; i < a.Length; i++)
            result |= a[i] ^ b[i];
        return result == 0;
    }
}
```

## Input Validation

All external inputs must be validated before use.

### Component Value Validation

**Use validators to enforce data constraints:**

```csharp
// Validate component values
world.AddValidator<Health>(health =>
{
    // Enforce bounds
    if (health.Current < 0 || health.Current > health.Max)
        return false;
    
    // Prevent NaN and Infinity
    if (float.IsNaN(health.Current) || float.IsInfinity(health.Current))
        return false;
    
    if (float.IsNaN(health.Max) || float.IsInfinity(health.Max))
        return false;
    
    return true;
});

world.AddValidator<Position>(pos =>
{
    // Prevent extreme values that could cause overflow
    if (Math.Abs(pos.X) > 1000000 || Math.Abs(pos.Y) > 1000000)
        return false;
    
    // Prevent NaN and Infinity
    if (float.IsNaN(pos.X) || float.IsNaN(pos.Y))
        return false;
    
    if (float.IsInfinity(pos.X) || float.IsInfinity(pos.Y))
        return false;
    
    return true;
});
```

### Entity ID Validation

**Validate entity IDs from external sources:**

```csharp
public bool IsValidEntityId(ulong entityHandle)
{
    var (id, gen) = Entity.Unpack(entityHandle);
    
    // Check ID range
    if (id <= 0 || id > int.MaxValue)
        return false;
    
    // Check generation
    if (gen < 0 || gen > int.MaxValue)
        return false;
    
    // Verify entity exists and generation matches
    if (!world.IsAlive(Entity.Pack(id, gen)))
        return false;
    
    return world.GenerationOf(id) == gen;
}
```

### String Input Validation

**Sanitize string inputs:**

```csharp
public string SanitizeString(string input, int maxLength = 256)
{
    if (string.IsNullOrEmpty(input))
        return string.Empty;
    
    // Limit length
    if (input.Length > maxLength)
        input = input.Substring(0, maxLength);
    
    // Remove control characters
    input = new string(input.Where(c => !char.IsControl(c)).ToArray());
    
    return input;
}
```

## Access Control

Control who can modify components using permission hooks.

### Write Permissions

**Implement role-based access control:**

```csharp
public class SecuritySystem
{
    public void SetupPermissions(IWorld world)
    {
        // Only server can modify authoritative components
        world.AddWritePermission((entity, componentType) =>
        {
            if (componentType == typeof(ServerAuthoritative))
            {
                return IsServer();
            }
            return true;
        });
        
        // Only admins can modify admin components
        world.AddWritePermission((entity, componentType) =>
        {
            if (componentType == typeof(AdminComponent))
            {
                return world.HasComponent<Admin>(entity) && 
                       IsCurrentUserAdmin();
            }
            return true;
        });
        
        // Locked entities cannot be modified
        world.AddWritePermission((entity, componentType) =>
        {
            if (world.HasComponent<Locked>(entity))
            {
                return HasUnlockPermission(entity);
            }
            return true;
        });
    }
    
    private bool IsServer() => /* ... */;
    private bool IsCurrentUserAdmin() => /* ... */;
    private bool HasUnlockPermission(Entity entity) => /* ... */;
}
```

### Read Permissions

**Control read access for sensitive data:**

```csharp
public class SecureQuerySystem
{
    public IEnumerable<(Entity, Health)> QueryHealthSecurely(IWorld world)
    {
        foreach (var (entity, health) in world.Query<Health>())
        {
            // Check read permission
            if (HasReadPermission(entity, typeof(Health)))
            {
                yield return (entity, health);
            }
        }
    }
    
    private bool HasReadPermission(Entity entity, Type componentType)
    {
        // Implement read permission logic
        // For example: only owner or admin can read sensitive health
        return true; // Implement your logic
    }
}
```

## Threading Safety

ZenECS provides thread-safe operations, but proper usage is required.

### Concurrent Access

**Kernel and World operations are thread-safe for reads:**

```csharp
// ✅ SAFE: Concurrent reads
Parallel.ForEach(entities, entity =>
{
    if (world.IsAlive(entity))
    {
        var pos = world.Get<Position>(entity);
        // Process position
    }
});

// ❌ UNSAFE: Concurrent writes without synchronization
Parallel.ForEach(entities, entity =>
{
    using (var cmd = world.BeginWrite())
    {
        cmd.ReplaceComponent(entity, new Position { X = 1, Y = 1 });
    }
    // Risk: Race conditions, inconsistent state
});

// ✅ SAFE: Synchronized writes
lock (writeLock)
{
    using (var cmd = world.BeginWrite())
    {
        foreach (var entity in entities)
        {
            cmd.ReplaceComponent(entity, new Position { X = 1, Y = 1 });
        }
    }
}
```

### Message Bus Thread Safety

**Message bus is thread-safe for publishing:**

```csharp
// ✅ SAFE: Concurrent message publishing
Parallel.ForEach(messages, msg =>
{
    world.Publish(msg); // Thread-safe
});

// ⚠️ CAUTION: Subscriber execution order is not guaranteed
world.Subscribe<DamageMessage>(msg =>
{
    // This may execute concurrently if multiple threads publish
    // Ensure subscriber is thread-safe
    ProcessDamage(msg);
});
```

### Command Buffer Thread Safety

**Command buffers are not thread-safe:**

```csharp
// ❌ UNSAFE: Sharing command buffer across threads
var cmd = world.BeginWrite();
Parallel.ForEach(entities, entity =>
{
    cmd.AddComponent(entity, new Position()); // Race condition!
});

// ✅ SAFE: One command buffer per thread
Parallel.ForEach(entities, entity =>
{
    using (var cmd = world.BeginWrite())
    {
        cmd.AddComponent(entity, new Position());
    }
});
```

## Secure Defaults

Configure secure defaults for production.

### Write Failure Policy

**Use appropriate policy for environment:**

```csharp
#if DEVELOPMENT
    // Development: Strict error checking
    EcsRuntimeOptions.WritePolicy = EcsRuntimeOptions.WriteFailurePolicy.Throw;
#else
    // Production: Log and continue (prevent DoS from exceptions)
    EcsRuntimeOptions.WritePolicy = EcsRuntimeOptions.WriteFailurePolicy.Log;
#endif
```

### Error Reporting

**Configure secure error reporting:**

```csharp
// Don't expose sensitive information in error messages
EcsRuntimeOptions.OnUnhandledError = (ex) =>
{
    // Log full details internally
    InternalLogger.LogError(ex);
    
    // Send sanitized version to external service
    var sanitized = new Exception(SanitizeErrorMessage(ex.Message));
    ErrorTrackingService.CaptureException(sanitized);
};

private string SanitizeErrorMessage(string message)
{
    // Remove sensitive data (paths, tokens, etc.)
    return message
        .Replace(Environment.UserName, "[USER]")
        .Replace(Environment.MachineName, "[MACHINE]");
}
```

### Logging Security

**Avoid logging sensitive data:**

```csharp
public class SecureLogger : IEcsLogger
{
    public void Info(string message)
    {
        // Sanitize before logging
        var sanitized = SanitizeLogMessage(message);
        File.AppendAllText("log.txt", $"[INFO] {sanitized}\n");
    }
    
    public void Warn(string message)
    {
        var sanitized = SanitizeLogMessage(message);
        File.AppendAllText("log.txt", $"[WARN] {sanitized}\n");
    }
    
    public void Error(string message)
    {
        var sanitized = SanitizeLogMessage(message);
        File.AppendAllText("log.txt", $"[ERROR] {sanitized}\n");
    }
    
    private string SanitizeLogMessage(string message)
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

## Network Security

For networked applications, additional security measures are required.

### State Synchronization

**Validate server state before applying:**

```csharp
public class SecureStateSync
{
    public void ApplyServerState(IWorld world, Stream serverState, byte[] serverSignature)
    {
        // Verify server signature
        if (!VerifySignature(serverState, serverSignature))
        {
            throw new SecurityException("Invalid server signature");
        }
        
        // Validate state structure
        var stateTypes = ReadStateTypes(serverState);
        if (!IsValidStateStructure(stateTypes))
        {
            throw new SecurityException("Invalid state structure");
        }
        
        // Apply state
        world.LoadSnapshot(serverState);
    }
    
    private bool VerifySignature(Stream data, byte[] signature)
    {
        // Implement signature verification
        return true; // Implement your logic
    }
    
    private bool IsValidStateStructure(IEnumerable<string> types)
    {
        var allowedTypes = GetAllowedComponentTypes();
        return types.All(t => allowedTypes.Contains(t));
    }
}
```

### Client-Side Validation

**Never trust client input:**

```csharp
// ❌ DANGEROUS: Trusting client input
public void ProcessClientInput(Entity entity, Position newPos)
{
    using (var cmd = world.BeginWrite())
    {
        cmd.ReplaceComponent(entity, newPos); // Client can send any value!
    }
}

// ✅ SAFE: Validate and sanitize client input
public void ProcessClientInput(Entity entity, Position newPos, float serverPosX, float serverPosY)
{
    // Validate movement speed
    var distance = Math.Sqrt(
        Math.Pow(newPos.X - serverPosX, 2) + 
        Math.Pow(newPos.Y - serverPosY, 2)
    );
    
    const float maxSpeed = 10f; // units per frame
    if (distance > maxSpeed)
    {
        // Clamp to maximum speed
        var direction = Math.Atan2(newPos.Y - serverPosY, newPos.X - serverPosX);
        newPos = new Position
        {
            X = serverPosX + (float)(Math.Cos(direction) * maxSpeed),
            Y = serverPosY + (float)(Math.Sin(direction) * maxSpeed)
        };
    }
    
    // Validate bounds
    if (newPos.X < -1000 || newPos.X > 1000 || 
        newPos.Y < -1000 || newPos.Y > 1000)
    {
        throw new SecurityException("Position out of bounds");
    }
    
    using (var cmd = world.BeginWrite())
    {
        cmd.ReplaceComponent(entity, newPos);
    }
}
```

## Best Practices

### 1. Validate All Inputs

Always validate inputs from external sources:

```csharp
public void ProcessExternalInput<T>(Entity entity, T component) where T : struct
{
    // Validate entity
    if (!world.IsAlive(entity))
        throw new ArgumentException("Invalid entity");
    
    // Validate component (use validators)
    // Validators are automatically checked
    
    // Process
    using (var cmd = world.BeginWrite())
    {
        cmd.ReplaceComponent(entity, component);
    }
}
```

### 2. Use Allowlists

Prefer allowlists over blocklists:

```csharp
// ✅ GOOD: Allowlist
private readonly HashSet<Type> _allowedComponents = new()
{
    typeof(Position),
    typeof(Velocity),
    typeof(Health)
};

public bool CanAddComponent(Type componentType)
{
    return _allowedComponents.Contains(componentType);
}

// ❌ BAD: Blocklist (easy to miss new threats)
private readonly HashSet<Type> _blockedComponents = new()
{
    typeof(AdminComponent)
};
```

### 3. Principle of Least Privilege

Grant minimum necessary permissions:

```csharp
// Grant specific permissions per entity/component
world.AddWritePermission((entity, componentType) =>
{
    // Check specific permission for this entity/component pair
    return HasSpecificPermission(entity, componentType);
});
```

### 4. Audit Security Events

Log security-relevant events:

```csharp
public class SecurityAudit
{
    public void LogWriteAttempt(Entity entity, Type componentType, bool allowed)
    {
        if (!allowed)
        {
            AuditLog.LogWarning(
                $"Write denied: Entity={entity.Id}, " +
                $"Component={componentType.Name}, " +
                $"User={GetCurrentUser()}, " +
                $"Time={DateTime.UtcNow}"
            );
        }
    }
    
    public void LogValidationFailure(Entity entity, Type componentType, string reason)
    {
        AuditLog.LogError(
            $"Validation failed: Entity={entity.Id}, " +
            $"Component={componentType.Name}, " +
            $"Reason={reason}, " +
            $"Time={DateTime.UtcNow}"
        );
    }
}
```

### 5. Secure Configuration

Store sensitive configuration securely:

```csharp
// ❌ BAD: Hardcoded secrets
const string API_KEY = "secret123";

// ✅ GOOD: Use secure configuration
var apiKey = Environment.GetEnvironmentVariable("API_KEY") 
    ?? throw new InvalidOperationException("API_KEY not set");

// Or use secure configuration service
var apiKey = SecureConfig.GetSecret("api_key");
```

## Security Checklist

Before deploying to production:

- [ ] All external inputs are validated
- [ ] Serialization uses allowlists for component types
- [ ] Snapshot integrity is verified (HMAC/signature)
- [ ] Write permissions are properly configured
- [ ] Validators enforce all data constraints
- [ ] Error messages don't expose sensitive information
- [ ] Logging doesn't include sensitive data
- [ ] Thread safety is properly handled
- [ ] Network state is validated before applying
- [ ] Client inputs are validated and sanitized
- [ ] Security events are audited
- [ ] Secrets are stored securely (not hardcoded)

## See Also

- [Error Handling](./error-handling.md) - Error handling and reporting
- [Write Hooks & Validators](./write-hooks-validators.md) - Access control
- [Serialization](./serialization.md) - Serialization details
- [Performance](./performance.md) - Performance considerations

