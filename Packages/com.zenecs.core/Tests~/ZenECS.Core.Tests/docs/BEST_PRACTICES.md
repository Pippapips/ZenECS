# ZenECS Core Testing Best Practices

This document outlines best practices for writing and maintaining tests for ZenECS Core.

## Table of Contents

1. [Test Organization](#test-organization)
2. [Test Naming](#test-naming)
3. [Test Structure](#test-structure)
4. [Assertions](#assertions)
5. [Resource Management](#resource-management)
6. [Performance Considerations](#performance-considerations)
7. [Maintainability](#maintainability)
8. [Common Pitfalls](#common-pitfalls)

## Test Organization

### File Structure

Organize tests by API surface area:

```
WorldComponentApiTests.cs          → Basic component operations
WorldComponentApiAdvancedTests.cs  → Advanced component features
WorldQueryTests.cs                 → Query API
WorldQuerySpanTests.cs             → QuerySpan API
```

### Class Organization

Group related tests in the same class:

```csharp
public class WorldComponentApiTests
{
    // All component-related tests here
    [Fact] public void AddComponent_creates_component() { }
    [Fact] public void RemoveComponent_removes_component() { }
    [Fact] public void ReplaceComponent_updates_component() { }
}
```

### Component Definitions

Define test components at the class level:

```csharp
public class MyTests
{
    // ✅ Good - Class-level definitions
    private struct Position { public int X; public int Y; }
    private struct Health { public int Value; }
    
    // ❌ Bad - Method-level definitions (not allowed in C#)
    // private struct Position { } // Inside method
}
```

## Test Naming

### Naming Convention

Use the pattern: `MethodName_Scenario_ExpectedBehavior`

```csharp
// ✅ Good
[Fact] public void AddComponent_creates_component_on_entity()
[Fact] public void RemoveComponent_removes_component_when_exists()
[Fact] public void Query_returns_empty_when_no_matches()

// ❌ Bad
[Fact] public void Test1()
[Fact] public void AddComponentTest()
[Fact] public void Test_AddComponent()
```

### Descriptive Names

Test names should be self-documenting:

```csharp
// ✅ Good - Clear what is being tested
[Fact] public void IsAlive_returns_false_after_entity_destruction()
[Fact] public void TryGetSingleton_returns_false_when_singleton_not_set()

// ❌ Bad - Unclear
[Fact] public void TestIsAlive()
[Fact] public void SingletonTest()
```

## Test Structure

### Arrange-Act-Assert Pattern

Always follow the AAA pattern:

```csharp
[Fact]
public void AddComponent_creates_component()
{
    // Arrange
    using var host = new TestWorldHost();
    Entity e = host.World.CreateEntity();

    // Act
    host.World.Apply(cmd =>
    {
        cmd.AddComponent(e, new Position { X = 1, Y = 2 });
    });

    // Assert
    Assert.True(host.World.HasComponent<Position>(e));
    var pos = host.World.ReadComponent<Position>(e);
    Assert.Equal(1, pos.X);
    Assert.Equal(2, pos.Y);
}
```

### Single Responsibility

Each test should verify one behavior:

```csharp
// ✅ Good - Single responsibility
[Fact]
public void AddComponent_creates_component()
{
    // Only tests AddComponent
}

[Fact]
public void RemoveComponent_removes_component()
{
    // Only tests RemoveComponent
}

// ❌ Bad - Multiple responsibilities
[Fact]
public void Component_operations()
{
    // Tests Add, Remove, Replace - too many things
}
```

### Test Independence

Tests should be independent and runnable in any order:

```csharp
// ✅ Good - Independent
[Fact]
public void Test1()
{
    using var host = new TestWorldHost();
    // Creates its own world
}

[Fact]
public void Test2()
{
    using var host = new TestWorldHost();
    // Creates its own world
}

// ❌ Bad - Dependent on shared state
private static IWorld sharedWorld;

[Fact]
public void Test1()
{
    sharedWorld = new TestWorldHost().World; // Shared state
}
```

## Assertions

### Use Specific Assertions

```csharp
// ✅ Good - Specific
Assert.True(host.World.HasComponent<Position>(e));
Assert.Equal(10, pos.X);
Assert.Single(results);
Assert.Empty(collection);

// ❌ Bad - Vague
Assert.NotNull(something); // Too generic, doesn't verify behavior
```

### Assert Early

Fail fast with clear assertions:

```csharp
// ✅ Good
Assert.True(host.World.HasComponent<Position>(e));
var pos = host.World.ReadComponent<Position>(e);
Assert.Equal(10, pos.X);

// ❌ Bad - Assumes component exists
var pos = host.World.ReadComponent<Position>(e); // May throw
Assert.Equal(10, pos.X);
```

### Meaningful Assertion Messages

Use custom messages when helpful:

```csharp
// ✅ Good - Clear failure message
Assert.True(host.World.HasComponent<Position>(e), 
    $"Entity {e} should have Position component");

// ❌ Bad - Default message may be unclear
Assert.True(host.World.HasComponent<Position>(e));
```

## Resource Management

### Always Dispose

```csharp
// ✅ Good
using var host = new TestWorldHost();
// Automatic disposal

// ❌ Bad
var host = new TestWorldHost();
// Missing disposal - resource leak
```

### Dispose Order

Dispose in reverse order of creation:

```csharp
// ✅ Good
using var host = new TestWorldHost();
using var subscription = host.World.Subscribe<Message>(...);
// Disposed in reverse order automatically

// ❌ Bad - Manual disposal can be error-prone
var host = new TestWorldHost();
var subscription = host.World.Subscribe<Message>(...);
// Must remember to dispose both
```

## Performance Considerations

### Avoid Unnecessary Setup

```csharp
// ✅ Good - Minimal setup
[Fact]
public void Query_returns_empty_on_empty_world()
{
    using var host = new TestWorldHost();
    // No entities created - test empty world
}

// ❌ Bad - Unnecessary setup
[Fact]
public void Query_returns_empty_on_empty_world()
{
    using var host = new TestWorldHost();
    // Creating entities that won't be used
    for (int i = 0; i < 100; i++)
    {
        host.World.CreateEntity();
    }
}
```

### Use Appropriate Data Structures

```csharp
// ✅ Good - Appropriate for small collections
var results = new List<QueryEnumerable<Position>.Result>();
foreach (var result in host.World.Query<Position>())
{
    results.Add(result);
}

// For large collections, consider streaming or counting
int count = 0;
foreach (var _ in host.World.Query<Position>())
{
    count++;
}
```

## Maintainability

### Extract Common Setup

```csharp
// ✅ Good - Reusable helper
private Entity CreateEntityWithPosition(TestWorldHost host, int x, int y)
{
    return host.World.CreateEntity((cmd, e) =>
    {
        cmd.AddComponent(e, new Position { X = x, Y = y });
    });
}

[Fact]
public void Test1()
{
    using var host = new TestWorldHost();
    Entity e = CreateEntityWithPosition(host, 10, 20);
    // ...
}
```

### Keep Tests Simple

```csharp
// ✅ Good - Simple and clear
[Fact]
public void AddComponent_creates_component()
{
    using var host = new TestWorldHost();
    Entity e = host.World.CreateEntity();
    
    host.World.Apply(cmd => cmd.AddComponent(e, new Position { X = 1, Y = 2 }));
    
    Assert.True(host.World.HasComponent<Position>(e));
}

// ❌ Bad - Overly complex
[Fact]
public void AddComponent_creates_component()
{
    using var host = new TestWorldHost();
    var entities = new List<Entity>();
    for (int i = 0; i < 10; i++)
    {
        var e = host.World.CreateEntity();
        entities.Add(e);
        host.World.Apply(cmd => cmd.AddComponent(e, new Position { X = i, Y = i }));
        // ... complex logic
    }
    // ...
}
```

### Document Complex Tests

```csharp
[Fact]
public void Complex_scenario_with_multiple_entities()
{
    // This test verifies that when entities are destroyed and recreated,
    // the generation bump prevents stale references from being valid.
    
    using var host = new TestWorldHost();
    // ... test implementation
}
```

## Common Pitfalls

### Pitfall 1: Forgetting to Flush Jobs

```csharp
// ❌ Bad - Commands may not be applied
host.World.BeginWrite();
cmd.AddComponent(e, new Position { X = 1, Y = 2 });
cmd.EndWrite();
// Missing RunScheduledJobs()

// ✅ Good - Use Apply() which handles flushing
host.World.Apply(cmd => cmd.AddComponent(e, new Position { X = 1, Y = 2 }));
```

### Pitfall 2: Not Waiting for Message Delivery

```csharp
// ❌ Bad - Messages aren't delivered yet
host.World.Publish(new Message { Value = 42 });
Assert.Single(receivedMessages); // May fail - message not pumped

// ✅ Good - Wait for message pump
host.World.Publish(new Message { Value = 42 });
host.TickFrame(); // Pumps messages during BeginFrame
Assert.Single(receivedMessages);
```

### Pitfall 3: Using ToList() on QueryEnumerable

```csharp
// ❌ Bad - QueryEnumerable doesn't implement IEnumerable
var results = host.World.Query<Position>().ToList(); // Compile error

// ✅ Good - Explicit loop
var results = new List<QueryEnumerable<Position>.Result>();
foreach (var result in host.World.Query<Position>())
{
    results.Add(result);
}
```

### Pitfall 4: Testing Implementation Details

```csharp
// ❌ Bad - Testing internal implementation
var internalState = GetInternalState(host.World); // May break on refactor

// ✅ Good - Testing public API behavior
Assert.True(host.World.HasComponent<Position>(e));
```

### Pitfall 5: Not Testing Edge Cases

```csharp
// ❌ Bad - Only testing happy path
[Fact]
public void GetSingleton_returns_value()
{
    // Only tests when singleton exists
}

// ✅ Good - Testing both success and failure
[Fact]
public void GetSingleton_returns_value_when_set() { }

[Fact]
public void GetSingleton_throws_when_not_set() { }
```

## Summary

1. **Organize** tests by API surface area
2. **Name** tests descriptively using the pattern `MethodName_Scenario_ExpectedBehavior`
3. **Structure** tests with Arrange-Act-Assert pattern
4. **Assert** specifically and early
5. **Dispose** all resources properly
6. **Keep** tests simple and independent
7. **Test** both success and failure cases
8. **Avoid** testing implementation details
9. **Document** complex scenarios
10. **Extract** common setup into helper methods

Following these practices will result in maintainable, reliable, and clear tests that serve as both documentation and validation of ZenECS Core behavior.

