using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZenECS.Core;
using ZenECS.Core.TestFramework;

namespace ZenECS.Core.Tests;

public class WorldEntityApiTests
{
    private struct Position
    {
        public int X;
        public int Y;
    }

    [Fact]
    public void GetAllEntities_returns_all_alive_entities()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
        });

        Entity e2 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 3, Y = 4 });
        });

        Entity e3 = host.World.CreateEntity();

        var entities = host.World.GetAllEntities();
        Assert.Equal(3, entities.Count);
        Assert.Contains(e1, entities);
        Assert.Contains(e2, entities);
        Assert.Contains(e3, entities);
    }

    [Fact]
    public void GetAllEntities_returns_empty_when_no_entities()
    {
        using var host = new TestWorldHost();

        var entities = host.World.GetAllEntities();
        Assert.Empty(entities);
    }

    [Fact]
    public void GetAllEntities_excludes_destroyed_entities()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        Entity e2 = host.World.CreateEntity();
        Entity e3 = host.World.CreateEntity();

        host.World.Apply(cmd =>
        {
            cmd.DestroyEntity(e2);
        });

        var entities = host.World.GetAllEntities();
        Assert.Equal(2, entities.Count);
        Assert.Contains(e1, entities);
        Assert.DoesNotContain(e2, entities);
        Assert.Contains(e3, entities);
    }

    [Fact]
    public void IsAlive_returns_true_for_live_entity()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();

        Assert.True(host.World.IsAlive(e1));
    }

    [Fact]
    public void IsAlive_returns_false_for_destroyed_entity()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        host.World.Apply(cmd =>
        {
            cmd.DestroyEntity(e1);
        });

        Assert.False(host.World.IsAlive(e1));
    }

    [Fact]
    public void IsAlive_returns_false_for_never_created_entity()
    {
        using var host = new TestWorldHost();

        Entity e1 = new Entity(999, 0);

        Assert.False(host.World.IsAlive(e1));
    }

    [Fact]
    public void IsAlive_with_id_and_gen_returns_true_for_live_entity()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();

        Assert.True(host.World.IsAlive(e1.Id, e1.Gen));
    }

    [Fact]
    public void IsAlive_with_id_and_gen_returns_false_for_wrong_generation()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        var originalGen = e1.Gen;

        host.World.Apply(cmd =>
        {
            cmd.DestroyEntity(e1);
        });

        // Wrong generation
        Assert.False(host.World.IsAlive(e1.Id, originalGen));

        // Recreate with same ID but different generation
        Entity e2 = host.World.CreateEntity();
        if (e2.Id == e1.Id)
        {
            Assert.NotEqual(originalGen, e2.Gen);
            Assert.True(host.World.IsAlive(e2.Id, e2.Gen));
            Assert.False(host.World.IsAlive(e2.Id, originalGen));
        }
    }

    [Fact]
    public void GetAllEntities_is_snapshot()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity();
        var snapshot1 = host.World.GetAllEntities();
        Assert.Single(snapshot1);

        Entity e2 = host.World.CreateEntity();
        // Snapshot should not change
        Assert.Single(snapshot1);

        var snapshot2 = host.World.GetAllEntities();
        Assert.Equal(2, snapshot2.Count);
    }
}

