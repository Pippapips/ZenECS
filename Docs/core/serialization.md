# Serialization

> Docs / Core / Serialization

Component serialization, formatters, and snapshot backends.

## Overview

ZenECS provides a flexible serialization system for saving and loading world state. The system uses pluggable formatters and backends, allowing you to customize serialization formats and storage mechanisms.

**Key Concepts:**

- **Formatters**: Convert components to/from binary or custom formats
- **Backends**: Abstract I/O layer (streams, memory, custom storage)
- **Stable IDs**: Version-independent component type identifiers
- **Migrations**: Post-load data transformations for version compatibility

## How It Works

### Serialization Pipeline

```
Component → Formatter → Backend → Storage
Storage → Backend → Formatter → Component
```

1. **Component**: Value type component data
2. **Formatter**: Converts component to/from backend format
3. **Backend**: Handles I/O operations (read/write)
4. **Storage**: Physical storage (file, memory, network, etc.)

### Snapshot Format

Snapshots contain:

- **Header**: Version, entity count, metadata
- **Entity Data**: Alive bits, generations, free IDs
- **Component Pools**: Type ID, component count, component data per entity

## API Surface

### Component Formatters

#### `IComponentFormatter`

Non-generic interface for boxed component serialization:

```csharp
public interface IComponentFormatter
{
    Type ComponentType { get; }
    void Write(object boxed, ISnapshotBackend backend);
    object Read(ISnapshotBackend backend);
}
```

#### `IComponentFormatter<T>`

Strongly-typed interface avoiding boxing:

```csharp
public interface IComponentFormatter<T> : IComponentFormatter where T : struct
{
    void Write(in T value, ISnapshotBackend backend);
    T ReadTyped(ISnapshotBackend backend);
}
```

#### `BinaryComponentFormatter<T>`

Base class for binary formatters:

```csharp
public abstract class BinaryComponentFormatter<T> : IComponentFormatter<T> where T : struct
{
    public abstract void Write(in T value, ISnapshotBackend backend);
    public abstract T ReadTyped(ISnapshotBackend backend);
}
```

### Snapshot Backends

#### `ISnapshotBackend`

Abstract I/O interface:

```csharp
public interface ISnapshotBackend : IDisposable
{
    // Primitives
    void WriteInt(int v);
    int ReadInt();
    void WriteFloat(float v);
    float ReadFloat();
    void WriteString(string s);
    string ReadString();
    void WriteBool(bool v);
    bool ReadBool();
    
    // Bytes
    void WriteBytes(ReadOnlySpan<byte> data);
    void ReadBytes(Span<byte> dst, int length);
    
    // Cursor
    long Position { get; set; }
    long Length { get; }
    void Rewind();
}
```

### Component Registry

#### `ComponentRegistry`

Global registry for component types and formatters:

```csharp
public static class ComponentRegistry
{
    // Register component type with stable ID
    public static void Register<T>(string stableId) where T : struct;
    public static void Register(string stableId, Type type);
    
    // Register formatter
    public static void RegisterFormatter(IComponentFormatter f);
    public static void RegisterFormatter(IComponentFormatter f, string declaredStableId);
    
    // Lookup
    public static bool TryGetType(string id, out Type? t);
    public static bool TryGetId(Type t, out string? id);
    public static IComponentFormatter GetFormatter(Type t);
    
    // Validation
    public static int ValidateStrictStableIdMatch(bool throwOnError = true, Action<string>? log = null);
}
```

### Post-Load Migrations

#### `IPostLoadMigration`

Interface for post-load data transformations:

```csharp
public interface IPostLoadMigration
{
    int Order { get; }
    void Run(IWorld world);
}
```

#### `PostLoadMigrationRegistry`

Registry for migrations:

```csharp
public static class PostLoadMigrationRegistry
{
    public static bool Register(IPostLoadMigration mig);
    public static bool IsRegistered<T>() where T : IPostLoadMigration;
    public static void RunAll(IWorld world);
    public static void Clear();
}
```

## Examples

### Basic Binary Formatter

```csharp
public struct Position
{
    public float X, Y, Z;
}

public sealed class PositionFormatter : BinaryComponentFormatter<Position>
{
    public override void Write(in Position value, ISnapshotBackend backend)
    {
        backend.WriteFloat(value.X);
        backend.WriteFloat(value.Y);
        backend.WriteFloat(value.Z);
    }
    
    public override Position ReadTyped(ISnapshotBackend backend)
    {
        return new Position
        {
            X = backend.ReadFloat(),
            Y = backend.ReadFloat(),
            Z = backend.ReadFloat()
        };
    }
}
```

### Registering Formatters

```csharp
// Register component type with stable ID
ComponentRegistry.Register<Position>("com.game.position.v1");

// Register formatter
var formatter = new PositionFormatter();
ComponentRegistry.RegisterFormatter(formatter, "com.game.position.v1");

// Validate consistency
ComponentRegistry.ValidateStrictStableIdMatch();
```

### Custom Backend

```csharp
public class MemorySnapshotBackend : ISnapshotBackend
{
    private readonly MemoryStream _stream;
    private readonly BinaryWriter _writer;
    private readonly BinaryReader _reader;
    
    public MemorySnapshotBackend()
    {
        _stream = new MemoryStream();
        _writer = new BinaryWriter(_stream, Encoding.UTF8, leaveOpen: true);
        _reader = new BinaryReader(_stream, Encoding.UTF8, leaveOpen: true);
    }
    
    public void WriteInt(int v) => _writer.Write(v);
    public int ReadInt() => _reader.ReadInt32();
    
    // Implement other ISnapshotBackend methods...
    
    public byte[] ToArray() => _stream.ToArray();
    
    public void Dispose()
    {
        _writer?.Dispose();
        _reader?.Dispose();
        _stream?.Dispose();
    }
}
```

### Versioned Components with Migration

```csharp
// Version 1 component
public struct PositionV1
{
    public float X, Y;
}

// Version 2 component (added Z)
public struct PositionV2
{
    public float X, Y, Z;
}

// Migration from V1 to V2
public sealed class PositionMigration : IPostLoadMigration
{
    public int Order => 0;
    
    public void Run(IWorld world)
    {
        using var cmd = world.BeginWrite();
        
        foreach (var (entity, posV1) in world.Query<PositionV1>())
        {
            // Migrate to V2
            cmd.AddComponent(entity, new PositionV2(posV1.X, posV1.Y, 0));
            cmd.RemoveComponent<PositionV1>(entity);
        }
    }
}

// Register migration
PostLoadMigrationRegistry.Register(new PositionMigration());
```

### Save and Load Snapshot

```csharp
// Save snapshot
using (var stream = File.Create("save.dat"))
{
    world.SaveSnapshot(stream);
}

// Load snapshot
using (var stream = File.OpenRead("save.dat"))
{
    world.LoadSnapshot(stream);
    // Migrations run automatically after load
}
```

### Formatter with Versioning

```csharp
public sealed class PositionFormatterV2 : BinaryComponentFormatter<Position>
{
    private const int FormatVersion = 2;
    
    public override void Write(in Position value, ISnapshotBackend backend)
    {
        // Write version header
        backend.WriteInt(FormatVersion);
        
        // Write data
        backend.WriteFloat(value.X);
        backend.WriteFloat(value.Y);
        backend.WriteFloat(value.Z);
    }
    
    public override Position ReadTyped(ISnapshotBackend backend)
    {
        // Read version
        int version = backend.ReadInt();
        
        // Handle different versions
        return version switch
        {
            1 => ReadV1(backend),
            2 => ReadV2(backend),
            _ => throw new NotSupportedException($"Unsupported format version: {version}")
        };
    }
    
    private Position ReadV1(ISnapshotBackend backend)
    {
        // V1 had no Z, default to 0
        return new Position
        {
            X = backend.ReadFloat(),
            Y = backend.ReadFloat(),
            Z = 0
        };
    }
    
    private Position ReadV2(ISnapshotBackend backend)
    {
        return new Position
        {
            X = backend.ReadFloat(),
            Y = backend.ReadFloat(),
            Z = backend.ReadFloat()
        };
    }
}
```

## Best Practices

### 1. Use Stable IDs

Always register components with stable IDs for version-independent serialization:

```csharp
// ✅ Good: Stable ID
ComponentRegistry.Register<Position>("com.game.position.v1");

// ❌ Bad: No stable ID (breaks with type name changes)
// ComponentRegistry.Register<Position>("Position");
```

### 2. Version Your Formats

Include version information in formatters:

```csharp
public override void Write(in Position value, ISnapshotBackend backend)
{
    backend.WriteInt(1); // Format version
    // Write data...
}
```

### 3. Handle Missing Fields

Be tolerant of missing fields when reading:

```csharp
public override Position ReadTyped(ISnapshotBackend backend)
{
    int version = backend.ReadInt();
    
    if (version >= 1)
    {
        var x = backend.ReadFloat();
        var y = backend.ReadFloat();
        
        // Z only exists in version 2+
        var z = version >= 2 ? backend.ReadFloat() : 0f;
        
        return new Position(x, y, z);
    }
    
    throw new NotSupportedException($"Unsupported version: {version}");
}
```

### 4. Validate After Deserialization

Validate data after reading:

```csharp
public override Position ReadTyped(ISnapshotBackend backend)
{
    var pos = new Position
    {
        X = backend.ReadFloat(),
        Y = backend.ReadFloat(),
        Z = backend.ReadFloat()
    };
    
    // Validate
    if (float.IsNaN(pos.X) || float.IsInfinity(pos.X))
        throw new InvalidDataException("Invalid X coordinate");
    
    return pos;
}
```

### 5. Use Migrations for Structural Changes

For breaking changes, use migrations:

```csharp
// Instead of complex formatter logic, use migrations
public sealed class PositionV1ToV2Migration : IPostLoadMigration
{
    public int Order => 0;
    
    public void Run(IWorld world)
    {
        // Transform V1 to V2
    }
}
```

## Format Guidelines

### Binary Format

- **Endianness**: Platform default (little-endian on common platforms)
- **Strings**: UTF-8 with 32-bit length prefix
- **Numbers**: Native size (int = 32-bit, float = 32-bit)
- **Versioning**: Include format version in header

### Stable ID Format

Recommended format: `com.domain.component.version`

Examples:
- `com.game.position.v1`
- `com.game.health.v2`
- `com.game.player.v1`

## Error Handling

### Missing Formatters

```csharp
try
{
    world.SaveSnapshot(stream);
}
catch (NotSupportedException ex)
{
    // Component type has no formatter
    EcsRuntimeOptions.Report(ex, "SaveSnapshot");
}
```

### Invalid Data

```csharp
public override Position ReadTyped(ISnapshotBackend backend)
{
    try
    {
        return new Position
        {
            X = backend.ReadFloat(),
            Y = backend.ReadFloat(),
            Z = backend.ReadFloat()
        };
    }
    catch (EndOfStreamException ex)
    {
        throw new InvalidDataException("Incomplete position data", ex);
    }
}
```

## Performance Considerations

### 1. Minimize Allocations

Use `in` parameters and struct returns:

```csharp
// ✅ Good: No allocation
public override void Write(in Position value, ISnapshotBackend backend)

// ❌ Bad: Boxing allocation
public override void Write(object boxed, ISnapshotBackend backend)
```

### 2. Batch Operations

Save/load entire pools at once:

```csharp
// Snapshot saves all components efficiently
world.SaveSnapshot(stream); // Efficient batch operation
```

### 3. Stream Buffering

Use buffered streams for file I/O:

```csharp
using (var fileStream = File.Create("save.dat"))
using (var buffered = new BufferedStream(fileStream))
{
    world.SaveSnapshot(buffered);
}
```

## Security Considerations

See [Security Guide](./security.md) for:

- Untrusted data handling
- Type validation
- Snapshot integrity
- Secure formatters

## FAQ

### How do I add serialization to a new component?

1. Create a formatter implementing `IComponentFormatter<T>`
2. Register component type with stable ID
3. Register formatter

```csharp
// 1. Create formatter
public sealed class MyComponentFormatter : BinaryComponentFormatter<MyComponent>
{
    // Implement Write/ReadTyped
}

// 2. Register component
ComponentRegistry.Register<MyComponent>("com.game.mycomponent.v1");

// 3. Register formatter
ComponentRegistry.RegisterFormatter(new MyComponentFormatter(), "com.game.mycomponent.v1");
```

### Can I use JSON instead of binary?

Yes, implement a custom formatter:

```csharp
public sealed class JsonPositionFormatter : IComponentFormatter<Position>
{
    public void Write(in Position value, ISnapshotBackend backend)
    {
        var json = JsonSerializer.Serialize(value);
        backend.WriteString(json);
    }
    
    public Position ReadTyped(ISnapshotBackend backend)
    {
        var json = backend.ReadString();
        return JsonSerializer.Deserialize<Position>(json);
    }
}
```

### How do I handle component renames?

Use migrations:

```csharp
public sealed class RenameMigration : IPostLoadMigration
{
    public void Run(IWorld world)
    {
        using var cmd = world.BeginWrite();
        
        // Find old component type
        foreach (var (entity, oldComp) in world.Query<OldComponentName>())
        {
            cmd.AddComponent(entity, new NewComponentName(oldComp.Value));
            cmd.RemoveComponent<OldComponentName>(entity);
        }
    }
}
```

### Can I compress snapshots?

Yes, wrap the stream:

```csharp
using (var fileStream = File.Create("save.dat"))
using (var compressed = new GZipStream(fileStream, CompressionLevel.Optimal))
{
    world.SaveSnapshot(compressed);
}
```

## See Also

- [Snapshot I/O](./snapshot-io.md) - Save/load operations
- [Stable IDs](./stable-ids.md) - Component type identification
- [Security](./security.md) - Serialization security
- [Migration Guide](./migration-postmig.md) - Data migrations
