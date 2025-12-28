using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ZenECS.Core;
using ZenECS.Core.TestFramework;

namespace ZenECS.Core.Tests;

public class WorldSingletonTests
{
    private struct GameSettings : IWorldSingletonComponent
    {
        public int MaxPlayers;
        public string GameMode;
    }

    private struct GlobalConfig : IWorldSingletonComponent
    {
        public float TimeScale;
        public bool DebugMode;
    }

    [Fact]
    public void SetSingleton_creates_singleton_entity()
    {
        using var host = new TestWorldHost();

        host.World.Apply(cmd =>
        {
            cmd.SetSingleton(new GameSettings { MaxPlayers = 4, GameMode = "Team" });
        });

        var settings = host.World.GetSingleton<GameSettings>();
        Assert.Equal(4, settings.MaxPlayers);
        Assert.Equal("Team", settings.GameMode);
    }

    [Fact]
    public void GetSingleton_returns_singleton_value()
    {
        using var host = new TestWorldHost();

        host.World.Apply(cmd =>
        {
            cmd.SetSingleton(new GameSettings { MaxPlayers = 8, GameMode = "FreeForAll" });
        });

        var settings = host.World.GetSingleton<GameSettings>();
        Assert.Equal(8, settings.MaxPlayers);
        Assert.Equal("FreeForAll", settings.GameMode);
    }

    [Fact]
    public void GetSingleton_throws_when_not_exists()
    {
        using var host = new TestWorldHost();

        Assert.Throws<InvalidOperationException>(() =>
        {
            host.World.GetSingleton<GameSettings>();
        });
    }

    [Fact]
    public void TryGetSingleton_returns_false_when_not_exists()
    {
        using var host = new TestWorldHost();

        bool result = host.World.TryGetSingleton<GameSettings>(out var settings);
        Assert.False(result);
        Assert.Equal(0, settings.MaxPlayers);
    }

    [Fact]
    public void TryGetSingleton_returns_true_when_exists()
    {
        using var host = new TestWorldHost();

        host.World.Apply(cmd =>
        {
            cmd.SetSingleton(new GameSettings { MaxPlayers = 6, GameMode = "Capture" });
        });

        bool result = host.World.TryGetSingleton<GameSettings>(out var settings);
        Assert.True(result);
        Assert.Equal(6, settings.MaxPlayers);
        Assert.Equal("Capture", settings.GameMode);
    }

    [Fact]
    public void SetSingleton_updates_existing_singleton()
    {
        using var host = new TestWorldHost();

        host.World.Apply(cmd =>
        {
            cmd.SetSingleton(new GameSettings { MaxPlayers = 2, GameMode = "Duel" });
        });

        var first = host.World.GetSingleton<GameSettings>();
        Assert.Equal(2, first.MaxPlayers);

        host.World.Apply(cmd =>
        {
            cmd.SetSingleton(new GameSettings { MaxPlayers = 10, GameMode = "BattleRoyale" });
        });

        var updated = host.World.GetSingleton<GameSettings>();
        Assert.Equal(10, updated.MaxPlayers);
        Assert.Equal("BattleRoyale", updated.GameMode);
    }

    [Fact]
    public void RemoveSingleton_removes_singleton()
    {
        using var host = new TestWorldHost();

        host.World.Apply(cmd =>
        {
            cmd.SetSingleton(new GameSettings { MaxPlayers = 4, GameMode = "Team" });
        });

        Assert.True(host.World.TryGetSingleton<GameSettings>(out _));

        host.World.Apply(cmd =>
        {
            cmd.RemoveSingleton<GameSettings>();
        });

        Assert.False(host.World.TryGetSingleton<GameSettings>(out _));
        Assert.Throws<InvalidOperationException>(() =>
        {
            host.World.GetSingleton<GameSettings>();
        });
    }

    [Fact]
    public void RemoveSingleton_returns_false_when_not_exists()
    {
        using var host = new TestWorldHost();

        // RemoveSingleton is internal, but we can test via command buffer
        // The operation should be a no-op if singleton doesn't exist
        host.World.Apply(cmd =>
        {
            cmd.RemoveSingleton<GameSettings>();
        });

        Assert.False(host.World.TryGetSingleton<GameSettings>(out _));
    }

    [Fact]
    public void HasSingleton_returns_true_for_singleton_entity()
    {
        using var host = new TestWorldHost();

        Entity singletonEntity = default;
        host.World.Apply(cmd =>
        {
            cmd.SetSingleton(new GameSettings { MaxPlayers = 4, GameMode = "Team" });
        });

        // Find the singleton entity
        foreach (var (entity, settings) in host.World.Query<GameSettings>())
        {
            singletonEntity = entity;
            break;
        }

        Assert.True(host.World.HasSingleton(singletonEntity));
    }

    [Fact]
    public void HasSingleton_returns_false_for_non_singleton_entity()
    {
        using var host = new TestWorldHost();

        Entity regularEntity = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
        });

        Assert.False(host.World.HasSingleton(regularEntity));
    }

    private struct Position
    {
        public int X;
        public int Y;
    }

    [Fact]
    public void GetAllSingletons_returns_all_singletons()
    {
        using var host = new TestWorldHost();

        host.World.Apply(cmd =>
        {
            cmd.SetSingleton(new GameSettings { MaxPlayers = 4, GameMode = "Team" });
            cmd.SetSingleton(new GlobalConfig { TimeScale = 1.5f, DebugMode = true });
        });

        var singletons = host.World.GetAllSingletons().ToList();
        Assert.Equal(2, singletons.Count);

        var types = singletons.Select(s => s.type).ToList();
        Assert.Contains(typeof(GameSettings), types);
        Assert.Contains(typeof(GlobalConfig), types);
    }

    [Fact]
    public void Singleton_violation_throws_exception()
    {
        using var host = new TestWorldHost();

        // Create first entity with singleton component
        Entity e1 = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new GameSettings { MaxPlayers = 4, GameMode = "Team" });
        });

        // Try to add same singleton component to another entity
        Entity e2 = host.World.CreateEntity();
        Assert.Throws<InvalidOperationException>(() =>
        {
            host.World.Apply(cmd =>
            {
                cmd.AddComponent(e2, new GameSettings { MaxPlayers = 8, GameMode = "FreeForAll" });
            });
        });
    }

    [Fact]
    public void Multiple_singleton_types_can_coexist()
    {
        using var host = new TestWorldHost();

        host.World.Apply(cmd =>
        {
            cmd.SetSingleton(new GameSettings { MaxPlayers = 4, GameMode = "Team" });
            cmd.SetSingleton(new GlobalConfig { TimeScale = 2.0f, DebugMode = false });
        });

        var settings = host.World.GetSingleton<GameSettings>();
        var config = host.World.GetSingleton<GlobalConfig>();

        Assert.Equal(4, settings.MaxPlayers);
        Assert.Equal(2.0f, config.TimeScale);
    }

    [Fact]
    public void Singleton_entity_is_removed_when_component_removed()
    {
        using var host = new TestWorldHost();

        Entity singletonEntity = default;
        host.World.Apply(cmd =>
        {
            cmd.SetSingleton(new GameSettings { MaxPlayers = 4, GameMode = "Team" });
        });

        // Find singleton entity
        foreach (var (entity, _) in host.World.Query<GameSettings>())
        {
            singletonEntity = entity;
            break;
        }

        Assert.True(host.World.HasSingleton(singletonEntity));

        // Remove component directly
        host.World.Apply(cmd =>
        {
            cmd.RemoveComponent<GameSettings>(singletonEntity);
        });

        Assert.False(host.World.HasSingleton(singletonEntity));
        Assert.False(host.World.TryGetSingleton<GameSettings>(out _));
    }

    [Fact]
    public void Singleton_index_updates_after_entity_destruction()
    {
        using var host = new TestWorldHost();

        Entity singletonEntity = default;
        host.World.Apply(cmd =>
        {
            cmd.SetSingleton(new GameSettings { MaxPlayers = 4, GameMode = "Team" });
        });

        // Find singleton entity
        foreach (var (entity, _) in host.World.Query<GameSettings>())
        {
            singletonEntity = entity;
            break;
        }

        Assert.True(host.World.HasSingleton(singletonEntity));

        // Destroy entity
        host.World.Apply(cmd =>
        {
            cmd.DestroyEntity(singletonEntity);
        });

        Assert.False(host.World.TryGetSingleton<GameSettings>(out _));
    }

    [Fact]
    public void GetAllSingletons_returns_empty_when_no_singletons()
    {
        using var host = new TestWorldHost();

        var singletons = host.World.GetAllSingletons().ToList();
        Assert.Empty(singletons);
    }

    [Fact]
    public void SetSingleton_via_command_buffer_applies_at_barrier()
    {
        using var host = new TestWorldHost();

        // Set singleton via command buffer
        host.World.Apply(cmd =>
        {
            cmd.SetSingleton(new GameSettings { MaxPlayers = 4, GameMode = "Team" });
        });

        // Should be available after Apply
        var settings = host.World.GetSingleton<GameSettings>();
        Assert.Equal(4, settings.MaxPlayers);
    }

    [Fact]
    public void Singleton_can_be_updated_via_replace_component()
    {
        using var host = new TestWorldHost();

        host.World.Apply(cmd =>
        {
            cmd.SetSingleton(new GameSettings { MaxPlayers = 4, GameMode = "Team" });
        });

        Entity singletonEntity = default;
        foreach (var (entity, _) in host.World.Query<GameSettings>())
        {
            singletonEntity = entity;
            break;
        }

        // Update via ReplaceComponent
        host.World.Apply(cmd =>
        {
            cmd.ReplaceComponent(singletonEntity, new GameSettings { MaxPlayers = 8, GameMode = "FreeForAll" });
        });

        var updated = host.World.GetSingleton<GameSettings>();
        Assert.Equal(8, updated.MaxPlayers);
        Assert.Equal("FreeForAll", updated.GameMode);
    }
}

