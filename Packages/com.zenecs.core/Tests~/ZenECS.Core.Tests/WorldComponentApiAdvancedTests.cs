using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZenECS.Core;
using ZenECS.Core.TestFramework;

namespace ZenECS.Core.Tests;

public class WorldComponentApiAdvancedTests
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

    [Fact]
    public void SnapshotComponent_returns_true_when_component_exists()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 10, Y = 20 });
        });

        bool result = host.World.SnapshotComponent<Position>(e1);
        Assert.True(result);
    }

    [Fact]
    public void SnapshotComponent_returns_false_when_component_not_exists()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();

        bool result = host.World.SnapshotComponent<Position>(e1);
        Assert.False(result);
    }

    [Fact]
    public void SnapshotComponentBoxed_returns_true_when_component_exists()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 10, Y = 20 });
        });

        bool result = host.World.SnapshotComponentBoxed(e1, new Position { X = 10, Y = 20 });
        Assert.True(result);
    }

    [Fact]
    public void SnapshotComponentBoxed_returns_false_when_component_not_exists()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();

        bool result = host.World.SnapshotComponentBoxed(e1, new Position { X = 10, Y = 20 });
        Assert.False(result);
    }

    [Fact]
    public void SnapshotComponentBoxed_throws_when_not_value_type()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();

        Assert.Throws<ArgumentException>(() =>
        {
            host.World.SnapshotComponentBoxed(e1, "not a struct");
        });
    }

    [Fact]
    public void SnapshotComponentTyped_returns_true_when_component_exists()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 10, Y = 20 });
        });

        bool result = host.World.SnapshotComponentTyped(e1, typeof(Position));
        Assert.True(result);
    }

    [Fact]
    public void SnapshotComponentTyped_returns_false_when_component_not_exists()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();

        bool result = host.World.SnapshotComponentTyped(e1, typeof(Position));
        Assert.False(result);
    }

    [Fact]
    public void SnapshotComponentTyped_returns_false_when_type_is_null()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();

        bool result = host.World.SnapshotComponentTyped(e1, null);
        Assert.False(result);
    }

    [Fact]
    public void HasComponentBoxed_returns_true_when_component_exists()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 10, Y = 20 });
        });

        bool result = host.World.HasComponentBoxed(e1, typeof(Position));
        Assert.True(result);
    }

    [Fact]
    public void HasComponentBoxed_returns_false_when_component_not_exists()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();

        bool result = host.World.HasComponentBoxed(e1, typeof(Position));
        Assert.False(result);
    }

    [Fact]
    public void HasComponentBoxed_returns_false_when_type_is_null()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();

        bool result = host.World.HasComponentBoxed(e1, null);
        Assert.False(result);
    }

    [Fact]
    public void GetAllComponents_returns_all_components_on_entity()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 10, Y = 20 });
            cmd.AddComponent(e, new Health { Value = 100 });
        });

        var components = host.World.GetAllComponents(e1).ToList();
        Assert.Equal(2, components.Count);

        var types = components.Select(c => c.type).ToList();
        Assert.Contains(typeof(Position), types);
        Assert.Contains(typeof(Health), types);
    }

    [Fact]
    public void GetAllComponents_returns_empty_when_no_components()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();

        var components = host.World.GetAllComponents(e1).ToList();
        Assert.Empty(components);
    }
}

