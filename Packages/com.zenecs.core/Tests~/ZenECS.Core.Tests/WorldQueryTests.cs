using System.Collections.Generic;
using Xunit;
using ZenECS.Core;
using ZenECS.Core.TestFramework;

namespace ZenECS.Core.Tests;

public class WorldQueryTests
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

    private struct Velocity
    {
        public float X;
        public float Y;
    }

    private struct Tagged
    {
        public int Tag;
    }

    private struct Component4
    {
        public int Value;
    }

    private struct Component5
    {
        public float Value;
    }

    private struct Component6
    {
        public bool Flag;
    }

    private struct Component7
    {
        public string Name;
    }

    private struct Component8
    {
        public int Id;
    }

    [Fact]
    public void Query_single_component_returns_all_entities_with_component()
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

        Entity e3 = host.World.CreateEntity(); // No Position

        var results = new List<QueryEnumerable<Position>.Result>();
        foreach (var result in host.World.Query<Position>())
        {
            results.Add(result);
        }
        Assert.Equal(2, results.Count);

        var positions = new List<Position>();
        foreach (var result in results)
        {
            positions.Add(result.Component);
        }
        Assert.Contains(new Position { X = 1, Y = 2 }, positions);
        Assert.Contains(new Position { X = 3, Y = 4 }, positions);
    }

    [Fact]
    public void Query_two_components_returns_entities_with_both()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            cmd.AddComponent(e, new Health { Value = 100 });
        });

        Entity e2 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 3, Y = 4 });
            // No Health
        });

        Entity e3 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Health { Value = 50 });
            // No Position
        });

        var results = new List<QueryEnumerable<Position, Health>.Result>();
        foreach (var result in host.World.Query<Position, Health>())
        {
            results.Add(result);
        }
        Assert.Single(results);

        var (entity, pos, health) = results[0];
        Assert.Equal(1, pos.X);
        Assert.Equal(2, pos.Y);
        Assert.Equal(100, health.Value);
    }

    [Fact]
    public void Query_three_components_returns_entities_with_all_three()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            cmd.AddComponent(e, new Health { Value = 100 });
            cmd.AddComponent(e, new Velocity { X = 1.0f, Y = 2.0f });
        });

        Entity e2 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 3, Y = 4 });
            cmd.AddComponent(e, new Health { Value = 50 });
            // No Velocity
        });

        var results = new List<QueryEnumerable<Position, Health, Velocity>.Result>();
        foreach (var result in host.World.Query<Position, Health, Velocity>())
        {
            results.Add(result);
        }
        Assert.Single(results);

        var (entity, pos, health, vel) = results[0];
        Assert.Equal(1, pos.X);
        Assert.Equal(100, health.Value);
        Assert.Equal(1.0f, vel.X);
    }

    [Fact]
    public void Query_with_filter_with_additional_component()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            cmd.AddComponent(e, new Health { Value = 100 });
            cmd.AddComponent(e, new Tagged { Tag = 1 });
        });

        Entity e2 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 3, Y = 4 });
            cmd.AddComponent(e, new Health { Value = 50 });
            // No Tagged
        });

        // Query Position entities that also have Tagged
        var filter = Filter.New.With<Tagged>().Build();
        var results = new List<QueryEnumerable<Position>.Result>();
        foreach (var result in host.World.Query<Position>(filter))
        {
            results.Add(result);
        }

        Assert.Single(results);
        Assert.Equal(1, results[0].Component.X);
    }

    [Fact]
    public void Query_with_filter_without_component()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            cmd.AddComponent(e, new Health { Value = 100 });
        });

        Entity e2 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 3, Y = 4 });
            // No Health
        });

        // Query Position entities that do NOT have Health
        var filter = Filter.New.Without<Health>().Build();
        var results = new List<QueryEnumerable<Position>.Result>();
        foreach (var result in host.World.Query<Position>(filter))
        {
            results.Add(result);
        }

        Assert.Single(results);
        Assert.Equal(3, results[0].Component.X);
    }

    [Fact]
    public void Query_with_filter_with_any_component()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            cmd.AddComponent(e, new Health { Value = 100 });
        });

        Entity e2 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 3, Y = 4 });
            cmd.AddComponent(e, new Velocity { X = 1.0f, Y = 2.0f });
        });

        Entity e3 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 5, Y = 6 });
            // No Health or Velocity
        });

        // Query Position entities that have Health OR Velocity
        var filter = Filter.New.WithAny(typeof(Health), typeof(Velocity)).Build();
        var results = new List<QueryEnumerable<Position>.Result>();
        foreach (var result in host.World.Query<Position>(filter))
        {
            results.Add(result);
        }

        Assert.Equal(2, results.Count);
        var xs = new List<int>();
        foreach (var result in results)
        {
            xs.Add(result.Component.X);
        }
        Assert.Contains(1, xs);
        Assert.Contains(3, xs);
        Assert.DoesNotContain(5, xs);
    }

    [Fact]
    public void Query_with_filter_without_any_component()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            cmd.AddComponent(e, new Health { Value = 100 });
        });

        Entity e2 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 3, Y = 4 });
            cmd.AddComponent(e, new Velocity { X = 1.0f, Y = 2.0f });
        });

        Entity e3 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 5, Y = 6 });
            // No Health or Velocity
        });

        // Query Position entities that do NOT have Health AND do NOT have Velocity
        var filter = Filter.New.WithoutAny(typeof(Health), typeof(Velocity)).Build();
        var results = new List<QueryEnumerable<Position>.Result>();
        foreach (var result in host.World.Query<Position>(filter))
        {
            results.Add(result);
        }

        Assert.Single(results);
        Assert.Equal(5, results[0].Component.X);
    }

    [Fact]
    public void Query_with_complex_filter_combination()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            cmd.AddComponent(e, new Health { Value = 100 });
            cmd.AddComponent(e, new Tagged { Tag = 1 });
        });

        Entity e2 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 3, Y = 4 });
            cmd.AddComponent(e, new Health { Value = 50 });
            cmd.AddComponent(e, new Velocity { X = 1.0f, Y = 2.0f });
            // No Tagged
        });

        Entity e3 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 5, Y = 6 });
            cmd.AddComponent(e, new Health { Value = 75 });
            cmd.AddComponent(e, new Tagged { Tag = 2 });
            cmd.AddComponent(e, new Velocity { X = 2.0f, Y = 3.0f });
        });

        // Query Position entities that:
        // - Have Health
        // - Have Tagged
        // - Do NOT have Velocity
        var filter = Filter.New
            .With<Health>()
            .With<Tagged>()
            .Without<Velocity>()
            .Build();

        var results = new List<QueryEnumerable<Position>.Result>();
        foreach (var result in host.World.Query<Position>(filter))
        {
            results.Add(result);
        }

        Assert.Single(results);
        Assert.Equal(1, results[0].Component.X);
    }

    [Fact]
    public void Query_empty_result_when_no_matching_entities()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity(); // No components

        var results = new List<QueryEnumerable<Position>.Result>();
        foreach (var result in host.World.Query<Position>())
        {
            results.Add(result);
        }
        Assert.Empty(results);
    }

    [Fact]
    public void Query_updates_after_component_changes()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
        });

        var results1 = new List<QueryEnumerable<Position>.Result>();
        foreach (var result in host.World.Query<Position>())
        {
            results1.Add(result);
        }
        Assert.Single(results1);

        // Remove component
        host.World.Apply(cmd =>
        {
            cmd.RemoveComponent<Position>(e1);
        });

        var results2 = new List<QueryEnumerable<Position>.Result>();
        foreach (var result in host.World.Query<Position>())
        {
            results2.Add(result);
        }
        Assert.Empty(results2);

        // Add component back
        host.World.Apply(cmd =>
        {
            cmd.AddComponent(e1, new Position { X = 3, Y = 4 });
        });

        var results3 = new List<QueryEnumerable<Position>.Result>();
        foreach (var result in host.World.Query<Position>())
        {
            results3.Add(result);
        }
        Assert.Single(results3);
        Assert.Equal(3, results3[0].Component.X);
    }

    [Fact]
    public void Query_multiple_components_with_filter()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            cmd.AddComponent(e, new Health { Value = 100 });
            cmd.AddComponent(e, new Tagged { Tag = 1 });
        });

        Entity e2 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 3, Y = 4 });
            cmd.AddComponent(e, new Health { Value = 50 });
            // No Tagged
        });

        // Query Position + Health entities that also have Tagged
        var filter = Filter.New.With<Tagged>().Build();
        var results = new List<QueryEnumerable<Position, Health>.Result>();
        foreach (var result in host.World.Query<Position, Health>(filter))
        {
            results.Add(result);
        }

        Assert.Single(results);
        var (entity, pos, health) = results[0];
        Assert.Equal(1, pos.X);
        Assert.Equal(100, health.Value);
    }

    [Fact]
    public void Query_four_components()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            cmd.AddComponent(e, new Health { Value = 100 });
            cmd.AddComponent(e, new Velocity { X = 1.0f, Y = 2.0f });
            cmd.AddComponent(e, new Component4 { Value = 42 });
        });

        var results = new List<QueryEnumerable<Position, Health, Velocity, Component4>.Result>();
        foreach (var result in host.World.Query<Position, Health, Velocity, Component4>())
        {
            results.Add(result);
        }
        Assert.Single(results);

        var (entity, pos, health, vel, c4) = results[0];
        Assert.Equal(1, pos.X);
        Assert.Equal(100, health.Value);
        Assert.Equal(1.0f, vel.X);
        Assert.Equal(42, c4.Value);
    }

    [Fact]
    public void Query_five_components()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            cmd.AddComponent(e, new Health { Value = 100 });
            cmd.AddComponent(e, new Velocity { X = 1.0f, Y = 2.0f });
            cmd.AddComponent(e, new Component4 { Value = 42 });
            cmd.AddComponent(e, new Component5 { Value = 3.14f });
        });

        var results = new List<QueryEnumerable<Position, Health, Velocity, Component4, Component5>.Result>();
        foreach (var result in host.World.Query<Position, Health, Velocity, Component4, Component5>())
        {
            results.Add(result);
        }
        Assert.Single(results);

        var (entity, pos, health, vel, c4, c5) = results[0];
        Assert.Equal(1, pos.X);
        Assert.Equal(100, health.Value);
        Assert.Equal(1.0f, vel.X);
        Assert.Equal(42, c4.Value);
        Assert.Equal(3.14f, c5.Value);
    }

    [Fact]
    public void Query_six_components()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            cmd.AddComponent(e, new Health { Value = 100 });
            cmd.AddComponent(e, new Velocity { X = 1.0f, Y = 2.0f });
            cmd.AddComponent(e, new Component4 { Value = 42 });
            cmd.AddComponent(e, new Component5 { Value = 3.14f });
            cmd.AddComponent(e, new Component6 { Flag = true });
        });

        var results = new List<QueryEnumerable<Position, Health, Velocity, Component4, Component5, Component6>.Result>();
        foreach (var result in host.World.Query<Position, Health, Velocity, Component4, Component5, Component6>())
        {
            results.Add(result);
        }
        Assert.Single(results);

        var (entity, pos, health, vel, c4, c5, c6) = results[0];
        Assert.Equal(1, pos.X);
        Assert.Equal(100, health.Value);
        Assert.Equal(1.0f, vel.X);
        Assert.Equal(42, c4.Value);
        Assert.Equal(3.14f, c5.Value);
        Assert.True(c6.Flag);
    }

    [Fact]
    public void Query_seven_components()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            cmd.AddComponent(e, new Health { Value = 100 });
            cmd.AddComponent(e, new Velocity { X = 1.0f, Y = 2.0f });
            cmd.AddComponent(e, new Component4 { Value = 42 });
            cmd.AddComponent(e, new Component5 { Value = 3.14f });
            cmd.AddComponent(e, new Component6 { Flag = true });
            cmd.AddComponent(e, new Component7 { Name = "Test" });
        });

        var results = new List<QueryEnumerable<Position, Health, Velocity, Component4, Component5, Component6, Component7>.Result>();
        foreach (var result in host.World.Query<Position, Health, Velocity, Component4, Component5, Component6, Component7>())
        {
            results.Add(result);
        }
        Assert.Single(results);

        var (entity, pos, health, vel, c4, c5, c6, c7) = results[0];
        Assert.Equal(1, pos.X);
        Assert.Equal(100, health.Value);
        Assert.Equal(1.0f, vel.X);
        Assert.Equal(42, c4.Value);
        Assert.Equal(3.14f, c5.Value);
        Assert.True(c6.Flag);
        Assert.Equal("Test", c7.Name);
    }

    [Fact]
    public void Query_eight_components()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            cmd.AddComponent(e, new Health { Value = 100 });
            cmd.AddComponent(e, new Velocity { X = 1.0f, Y = 2.0f });
            cmd.AddComponent(e, new Component4 { Value = 42 });
            cmd.AddComponent(e, new Component5 { Value = 3.14f });
            cmd.AddComponent(e, new Component6 { Flag = true });
            cmd.AddComponent(e, new Component7 { Name = "Test" });
            cmd.AddComponent(e, new Component8 { Id = 999 });
        });

        var results = new List<QueryEnumerable<Position, Health, Velocity, Component4, Component5, Component6, Component7, Component8>.Result>();
        foreach (var result in host.World.Query<Position, Health, Velocity, Component4, Component5, Component6, Component7, Component8>())
        {
            results.Add(result);
        }
        Assert.Single(results);

        var (entity, pos, health, vel, c4, c5, c6, c7, c8) = results[0];
        Assert.Equal(1, pos.X);
        Assert.Equal(100, health.Value);
        Assert.Equal(1.0f, vel.X);
        Assert.Equal(42, c4.Value);
        Assert.Equal(3.14f, c5.Value);
        Assert.True(c6.Flag);
        Assert.Equal("Test", c7.Name);
        Assert.Equal(999, c8.Id);
    }

    [Fact]
    public void Query_multiple_components_with_filter_T5()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            cmd.AddComponent(e, new Health { Value = 100 });
            cmd.AddComponent(e, new Velocity { X = 1.0f, Y = 2.0f });
            cmd.AddComponent(e, new Component4 { Value = 42 });
            cmd.AddComponent(e, new Component5 { Value = 3.14f });
            cmd.AddComponent(e, new Tagged { Tag = 1 });
        });

        Entity e2 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 3, Y = 4 });
            cmd.AddComponent(e, new Health { Value = 50 });
            cmd.AddComponent(e, new Velocity { X = 2.0f, Y = 3.0f });
            cmd.AddComponent(e, new Component4 { Value = 10 });
            cmd.AddComponent(e, new Component5 { Value = 2.71f });
            // No Tagged
        });

        // Query 5 components with filter
        var filter = Filter.New.With<Tagged>().Build();
        var results = new List<QueryEnumerable<Position, Health, Velocity, Component4, Component5>.Result>();
        foreach (var result in host.World.Query<Position, Health, Velocity, Component4, Component5>(filter))
        {
            results.Add(result);
        }

        Assert.Single(results);
        var (entity, pos, health, vel, c4, c5) = results[0];
        Assert.Equal(1, pos.X);
        Assert.Equal(100, health.Value);
        Assert.Equal(3.14f, c5.Value);
    }
}

