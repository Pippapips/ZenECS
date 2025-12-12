using Xunit;
using ZenECS.Core;
using ZenECS.Core.TestFramework;

namespace ZenECS.Core.Tests;

public class WorldCommandBufferAdvancedTests
{
    private struct Position
    {
        public int X;
        public int Y;
    }

    private struct GameSettings : IWorldSingletonComponent
    {
        public int MaxPlayers;
        public string GameMode;
    }

    [Fact]
    public void DestroyAllEntities_destroys_all_entities()
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

        Assert.Equal(3, host.World.AliveCount);

        host.World.Apply(cmd =>
        {
            cmd.DestroyAllEntities();
        });

        Assert.Equal(0, host.World.AliveCount);
        Assert.False(host.World.IsAlive(e1));
        Assert.False(host.World.IsAlive(e2));
        Assert.False(host.World.IsAlive(e3));
    }

    [Fact]
    public void DestroyAllEntities_on_empty_world_is_noop()
    {
        using var host = new TestWorldHost();

        Assert.Equal(0, host.World.AliveCount);

        host.World.Apply(cmd =>
        {
            cmd.DestroyAllEntities();
        });

        Assert.Equal(0, host.World.AliveCount);
    }

    [Fact]
    public void SetSingleton_via_command_buffer_creates_singleton()
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
    public void SetSingleton_via_command_buffer_updates_existing()
    {
        using var host = new TestWorldHost();

        host.World.Apply(cmd =>
        {
            cmd.SetSingleton(new GameSettings { MaxPlayers = 2, GameMode = "Duel" });
        });

        host.World.Apply(cmd =>
        {
            cmd.SetSingleton(new GameSettings { MaxPlayers = 8, GameMode = "FreeForAll" });
        });

        var settings = host.World.GetSingleton<GameSettings>();
        Assert.Equal(8, settings.MaxPlayers);
        Assert.Equal("FreeForAll", settings.GameMode);
    }

    [Fact]
    public void RemoveSingleton_via_command_buffer_removes_singleton()
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
    }

    [Fact]
    public void RemoveSingleton_via_command_buffer_on_nonexistent_is_noop()
    {
        using var host = new TestWorldHost();

        host.World.Apply(cmd =>
        {
            cmd.RemoveSingleton<GameSettings>();
        });

        Assert.False(host.World.TryGetSingleton<GameSettings>(out _));
    }
}

