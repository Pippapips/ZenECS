# ZenECS Core Testing Guide

A comprehensive guide for writing tests for ZenECS Core using the TestFramework.

## Table of Contents

1. [Getting Started](#getting-started)
2. [Test Structure](#test-structure)
3. [Common Testing Patterns](#common-testing-patterns)
4. [API-Specific Testing Patterns](#api-specific-testing-patterns)
5. [Edge Cases and Error Handling](#edge-cases-and-error-handling)
6. [Best Practices](#best-practices)

## Getting Started

### Basic Test Setup

Every test starts with creating a `TestWorldHost`:

```csharp
using Xunit;
using ZenECS.Core;
using ZenECS.Core.TestFramework;

public class MyTests
{
    [Fact]
    public void MyTest()
    {
        using var host = new TestWorldHost();
        // Your test code here
    }
}
```

**Important:** Always use `using var` to ensure proper disposal of the host and its resources.

### Component Definitions

Define test components as private structs within your test class:

```csharp
public class MyTests
{
    private struct Position
    {
        public int X;
        public int Y;
    }

    private struct Health
    {
        public int Value;
    }
}
```

## Test Structure

### Naming Conventions

- Test files: `*Tests.cs` for basic tests, `*AdvancedTests.cs` for advanced features
- Test methods: Use descriptive names that explain what is being tested
  - Good: `AddComponent_creates_component_on_entity`
  - Bad: `Test1`, `AddComponentTest`

### Test Method Organization

Follow the Arrange-Act-Assert (AAA) pattern:

```csharp
[Fact]
public void AddComponent_creates_component_on_entity()
{
    // Arrange
    using var host = new TestWorldHost();
    Entity e = host.World.CreateEntity();

    // Act
    host.World.Apply(cmd =>
    {
        cmd.AddComponent(e, new Position { X = 10, Y = 20 });
    });

    // Assert
    Assert.True(host.World.HasComponent<Position>(e));
    var pos = host.World.ReadComponent<Position>(e);
    Assert.Equal(10, pos.X);
    Assert.Equal(20, pos.Y);
}
```

## Common Testing Patterns

### Pattern 1: Entity Creation with Components

```csharp
Entity e = host.World.CreateEntity((cmd, entity) =>
{
    cmd.AddComponent(entity, new Position { X = 1, Y = 2 });
    cmd.AddComponent(entity, new Health { Value = 100 });
});
```

### Pattern 2: Command Buffer Operations

Use `Apply()` for any command buffer operations:

```csharp
host.World.Apply(cmd =>
{
    cmd.AddComponent(e, new Position { X = 1, Y = 2 });
    cmd.ReplaceComponent(e, new Health { Value = 50 });
    cmd.RemoveComponent<Velocity>(e);
});
```

### Pattern 3: Query Iteration

For queries that don't implement `IEnumerable`, use explicit loops:

```csharp
var results = new List<QueryEnumerable<Position, Health>.Result>();
foreach (var result in host.World.Query<Position, Health>())
{
    results.Add(result);
}
Assert.Equal(2, results.Count);
```

### Pattern 4: Message Subscription Testing

```csharp
var receivedMessages = new List<TestMessage>();
host.World.Subscribe<TestMessage>(msg => receivedMessages.Add(msg));

host.World.Publish(new TestMessage { Value = 42 });
host.TickFrame(); // Messages are pumped during BeginFrame

Assert.Single(receivedMessages);
Assert.Equal(42, receivedMessages[0].Value);
```

### Pattern 5: System Testing

```csharp
var system = new MySystem();
host.World.AddSystem(system);

host.TickFrame(dt: 0.016f);

Assert.True(system.WasExecuted);
Assert.Equal(1, system.ExecutionCount);
```

## API-Specific Testing Patterns

### Component API

**Basic Operations:**
```csharp
// Add
host.World.Apply(cmd => cmd.AddComponent(e, new Position { X = 1, Y = 2 }));

// Read
var pos = host.World.ReadComponent<Position>(e);

// Try Read
if (host.World.TryReadComponent<Position>(e, out var pos))
{
    // Use pos
}

// Replace
host.World.Apply(cmd => cmd.ReplaceComponent(e, new Position { X = 3, Y = 4 }));

// Remove
host.World.Apply(cmd => cmd.RemoveComponent<Position>(e));

// Check existence
bool has = host.World.HasComponent<Position>(e);
```

### Query API

**Single Component Query:**
```csharp
foreach (var (entity, pos) in host.World.Query<Position>())
{
    // Process entity with Position
}
```

**Multi-Component Query:**
```csharp
foreach (var (entity, pos, health) in host.World.Query<Position, Health>())
{
    // Process entity with both Position and Health
}
```

**With Filters:**
```csharp
var filter = Filter.New
    .With<Velocity>()
    .Without<Tagged>()
    .Build();

foreach (var (entity, pos) in host.World.Query<Position>(filter))
{
    // Process entities with Position and Velocity, but without Tagged
}
```

### QuerySpan API

**Collecting Entities:**
```csharp
Span<Entity> buffer = stackalloc Entity[10];
int count = host.World.QueryToSpan<Position, Health>(buffer);

// Process entities in buffer[..count]
```

**Processing Components:**
```csharp
host.World.Process<Health>(buffer[..count], (ref Health h) =>
{
    h.Value -= 10;
});
```

### Singleton Components

```csharp
// Set
host.World.Apply(cmd => cmd.SetSingleton(new GameSettings { MaxPlayers = 4 }));

// Get
var settings = host.World.GetSingleton<GameSettings>();

// Try Get
if (host.World.TryGetSingleton<GameSettings>(out var settings))
{
    // Use settings
}

// Remove
host.World.Apply(cmd => cmd.RemoveSingleton<GameSettings>());
```

### Messages API

```csharp
// Subscribe
using var subscription = host.World.Subscribe<TestMessage>(msg => { /* handle */ });

// Publish
host.World.Publish(new TestMessage { Value = 42 });

// Messages are delivered during TickFrame
host.TickFrame();
```

### Snapshot API

```csharp
// Save
using var stream = new MemoryStream();
host.World.SaveFullSnapshotBinary(stream);

// Load
stream.Position = 0;
using var host2 = new TestWorldHost();
host2.World.LoadFullSnapshotBinary(stream);
```

## Edge Cases and Error Handling

### Testing Empty Worlds

```csharp
[Fact]
public void Query_returns_empty_on_empty_world()
{
    using var host = new TestWorldHost();
    
    var results = new List<QueryEnumerable<Position>.Result>();
    foreach (var result in host.World.Query<Position>())
    {
        results.Add(result);
    }
    
    Assert.Empty(results);
}
```

### Testing Destroyed Entities

```csharp
[Fact]
public void IsAlive_returns_false_after_destruction()
{
    using var host = new TestWorldHost();
    Entity e = host.World.CreateEntity();
    
    host.World.Apply(cmd => cmd.DestroyEntity(e));
    
    Assert.False(host.World.IsAlive(e));
}
```

### Testing Missing Components

```csharp
[Fact]
public void TryReadComponent_returns_false_when_missing()
{
    using var host = new TestWorldHost();
    Entity e = host.World.CreateEntity();
    
    bool found = host.World.TryReadComponent<Position>(e, out var pos);
    
    Assert.False(found);
}
```

### Testing Exceptions

```csharp
[Fact]
public void GetSingleton_throws_when_not_set()
{
    using var host = new TestWorldHost();
    
    Assert.Throws<InvalidOperationException>(() =>
    {
        host.World.GetSingleton<GameSettings>();
    });
}
```

### Testing Write Permissions

```csharp
[Fact]
public void WritePermission_denies_write_when_hook_returns_false()
{
    using var host = new TestWorldHost();
    Entity e = host.World.CreateEntity();
    
    host.World.AddWritePermission((entity, type) => false);
    
    try
    {
        host.World.Apply(cmd => cmd.AddComponent(e, new Position { X = 1, Y = 2 }));
    }
    catch
    {
        // Exception expected
    }
    
    Assert.False(host.World.HasComponent<Position>(e));
}
```

## Best Practices

### 1. Always Dispose Resources

```csharp
// ✅ Good
using var host = new TestWorldHost();

// ❌ Bad
var host = new TestWorldHost();
// Missing disposal
```

### 2. Use Descriptive Test Names

```csharp
// ✅ Good
public void AddComponent_creates_component_on_entity()

// ❌ Bad
public void Test1()
```

### 3. Test One Thing Per Test

```csharp
// ✅ Good - Single responsibility
[Fact]
public void AddComponent_creates_component()
{
    // Test only AddComponent
}

[Fact]
public void RemoveComponent_removes_component()
{
    // Test only RemoveComponent
}

// ❌ Bad - Multiple responsibilities
[Fact]
public void Component_operations()
{
    // Tests Add, Remove, Replace all in one
}
```

### 4. Use Helper Methods for Repeated Setup

```csharp
private Entity CreateEntityWithPosition(TestWorldHost host, int x, int y)
{
    return host.World.CreateEntity((cmd, e) =>
    {
        cmd.AddComponent(e, new Position { X = x, Y = y });
    });
}
```

### 5. Test Both Success and Failure Cases

```csharp
[Fact]
public void TryGetSingleton_returns_true_when_exists()
{
    // Success case
}

[Fact]
public void TryGetSingleton_returns_false_when_missing()
{
    // Failure case
}
```

### 6. Use Appropriate Assertions

```csharp
// ✅ Good - Specific assertions
Assert.True(host.World.HasComponent<Position>(e));
Assert.Equal(10, pos.X);
Assert.Single(results);

// ❌ Bad - Vague assertions
Assert.NotNull(something); // Too generic
```

### 7. Keep Tests Independent

Each test should be able to run in isolation:

```csharp
// ✅ Good - Independent
[Fact]
public void Test1() { /* Creates its own world */ }

[Fact]
public void Test2() { /* Creates its own world */ }

// ❌ Bad - Dependent
private static IWorld sharedWorld; // Shared state
```

### 8. Test Edge Cases

Always consider:
- Empty collections
- Null values (where applicable)
- Destroyed entities
- Missing components
- Boundary values
- Invalid inputs

## Additional Resources

- [TestFramework README](../../ZenECS.Core.TestFramework/README.md) - TestFramework API reference
- [Test Coverage](./TEST_COVERAGE.md) - Complete test coverage analysis
- [Best Practices](./BEST_PRACTICES.md) - Detailed best practices guide

