using System;
using Xunit;
using ZenECS.Core;
using ZenECS.Core.TestFramework;

namespace ZenECS.Core.Tests;

public class WorldQuerySpanTests
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

    [Fact]
    public void QueryToSpan_single_component_collects_entities()
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

        Span<Entity> buffer = stackalloc Entity[10];
        int count = host.World.QueryToSpan<Position>(buffer);

        Assert.Equal(2, count);
        Assert.Contains(e1, buffer[..count].ToArray());
        Assert.Contains(e2, buffer[..count].ToArray());
        Assert.DoesNotContain(e3, buffer[..count].ToArray());
    }

    [Fact]
    public void QueryToSpan_two_components_collects_entities_with_both()
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

        Span<Entity> buffer = stackalloc Entity[10];
        int count = host.World.QueryToSpan<Position, Health>(buffer);

        Assert.Equal(1, count);
        Assert.Equal(e1, buffer[0]);
    }

    [Fact]
    public void QueryToSpan_respects_span_capacity()
    {
        using var host = new TestWorldHost();

        // Create more entities than buffer size
        for (int i = 0; i < 5; i++)
        {
            host.World.CreateEntity((cmd, e) =>
            {
                cmd.AddComponent(e, new Position { X = i, Y = i * 2 });
            });
        }

        Span<Entity> buffer = stackalloc Entity[3]; // Smaller buffer
        int count = host.World.QueryToSpan<Position>(buffer);

        Assert.Equal(3, count); // Should only collect up to buffer size
        Assert.Equal(3, buffer[..count].Length);
    }

    [Fact]
    public void QueryToSpan_with_filter_applies_filter()
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

        var filter = Filter.New.With<Tagged>().Build();
        Span<Entity> buffer = stackalloc Entity[10];
        int count = host.World.QueryToSpan<Position>(buffer, filter);

        Assert.Equal(1, count);
        Assert.Equal(e1, buffer[0]);
    }

    [Fact]
    public void QueryToSpan_returns_zero_when_no_matches()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity(); // No components

        Span<Entity> buffer = stackalloc Entity[10];
        int count = host.World.QueryToSpan<Position>(buffer);

        Assert.Equal(0, count);
    }

    [Fact]
    public void Process_modifies_components_by_reference()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Health { Value = 100 });
        });

        Entity e2 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Health { Value = 50 });
        });

        // Collect entities
        Span<Entity> buffer = stackalloc Entity[10];
        int count = host.World.QueryToSpan<Health>(buffer);

        // Process components by reference
        host.World.Process<Health>(buffer[..count], (ref Health h) =>
        {
            h.Value += 10;
        });

        // Verify changes
        Assert.Equal(110, host.World.ReadComponent<Health>(e1).Value);
        Assert.Equal(60, host.World.ReadComponent<Health>(e2).Value);
    }

    [Fact]
    public void Process_skips_dead_entities()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Health { Value = 100 });
        });

        Entity e2 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Health { Value = 50 });
        });

        // Collect entities
        Span<Entity> buffer = stackalloc Entity[10];
        int count = host.World.QueryToSpan<Health>(buffer);

        // Destroy one entity
        host.World.Apply(cmd =>
        {
            cmd.DestroyEntity(e1);
        });

        // Process - should skip dead entity
        int processedCount = 0;
        host.World.Process<Health>(buffer[..count], (ref Health h) =>
        {
            processedCount++;
            h.Value += 10;
        });

        // Should only process e2 (e1 is dead)
        Assert.Equal(1, processedCount);
        Assert.Equal(60, host.World.ReadComponent<Health>(e2).Value);
    }

    [Fact]
    public void Process_skips_entities_without_component()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Health { Value = 100 });
        });

        Entity e2 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            // No Health
        });

        // Collect Position entities (includes e2, but e2 doesn't have Health)
        Span<Entity> buffer = stackalloc Entity[10];
        int count = host.World.QueryToSpan<Position>(buffer);

        // Process Health - should skip e2
        int processedCount = 0;
        host.World.Process<Health>(buffer[..count], (ref Health h) =>
        {
            processedCount++;
        });

        // Should process 0 (e2 doesn't have Health)
        Assert.Equal(0, processedCount);
    }

    [Fact]
    public void QueryToSpan_and_Process_workflow()
    {
        using var host = new TestWorldHost();

        // Create entities with Position and Health
        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            cmd.AddComponent(e, new Health { Value = 100 });
        });

        Entity e2 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 3, Y = 4 });
            cmd.AddComponent(e, new Health { Value = 50 });
        });

        // Collect entities with both Position and Health
        Span<Entity> buffer = stackalloc Entity[10];
        int count = host.World.QueryToSpan<Position, Health>(buffer);

        Assert.Equal(2, count);

        // Process Health components
        host.World.Process<Health>(buffer[..count], (ref Health h) =>
        {
            h.Value = Math.Max(0, h.Value - 10);
        });

        // Verify changes
        Assert.Equal(90, host.World.ReadComponent<Health>(e1).Value);
        Assert.Equal(40, host.World.ReadComponent<Health>(e2).Value);
    }

    [Fact]
    public void QueryToSpan_three_components()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            cmd.AddComponent(e, new Health { Value = 100 });
            cmd.AddComponent(e, new Velocity { X = 1.0f, Y = 2.0f });
        });

        Span<Entity> buffer = stackalloc Entity[10];
        int count = host.World.QueryToSpan<Position, Health, Velocity>(buffer);

        Assert.Equal(1, count);
        Assert.Equal(e1, buffer[0]);
    }

    [Fact]
    public void QueryToSpan_four_components()
    {
        using var host = new TestWorldHost();

        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            cmd.AddComponent(e, new Health { Value = 100 });
            cmd.AddComponent(e, new Velocity { X = 1.0f, Y = 2.0f });
            cmd.AddComponent(e, new Component4 { Value = 42 });
        });

        Span<Entity> buffer = stackalloc Entity[10];
        int count = host.World.QueryToSpan<Position, Health, Velocity, Component4>(buffer);

        Assert.Equal(1, count);
        Assert.Equal(e1, buffer[0]);
    }

    [Fact]
    public void Process_modifies_multiple_components()
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
            cmd.AddComponent(e, new Health { Value = 50 });
        });

        Span<Entity> buffer = stackalloc Entity[10];
        int count = host.World.QueryToSpan<Position, Health>(buffer);

        // Process Position
        host.World.Process<Position>(buffer[..count], (ref Position p) =>
        {
            p.X += 10;
            p.Y += 10;
        });

        // Process Health
        host.World.Process<Health>(buffer[..count], (ref Health h) =>
        {
            h.Value += 5;
        });

        // Verify both components were modified
        var pos1 = host.World.ReadComponent<Position>(e1);
        var health1 = host.World.ReadComponent<Health>(e1);
        Assert.Equal(11, pos1.X);
        Assert.Equal(12, pos1.Y);
        Assert.Equal(105, health1.Value);

        var pos2 = host.World.ReadComponent<Position>(e2);
        var health2 = host.World.ReadComponent<Health>(e2);
        Assert.Equal(13, pos2.X);
        Assert.Equal(14, pos2.Y);
        Assert.Equal(55, health2.Value);
    }

    [Fact]
    public void QueryToSpan_with_filter_without_component()
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

        var filter = Filter.New.Without<Health>().Build();
        Span<Entity> buffer = stackalloc Entity[10];
        int count = host.World.QueryToSpan<Position>(buffer, filter);

        Assert.Equal(1, count);
        Assert.Equal(e2, buffer[0]);
    }

    [Fact]
    public void QueryToSpan_empty_span_returns_zero()
    {
        using var host = new TestWorldHost();

        host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
        });

        Span<Entity> buffer = stackalloc Entity[0]; // Empty span
        int count = host.World.QueryToSpan<Position>(buffer);

        Assert.Equal(0, count);
    }
}

