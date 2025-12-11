using Xunit;
using ZenECS.Core.TestFramework;

namespace ZenECS.Core.Tests;

public class WorldComponentApiTests
{
    private struct Position
    {
        public int X;
        public int Y;
    }

    [Fact]
    public void Add_replace_and_remove_components_via_command_buffer()
    {
        using var host = new TestWorldHost();

        Entity entity = host.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new Position { X = 1, Y = 2 });
        });

        Assert.True(host.World.HasComponent<Position>(entity));
        Assert.True(host.World.TryReadComponent(entity, out Position p));
        Assert.Equal(1, p.X);
        Assert.Equal(2, p.Y);

        host.World.Apply(cmd =>
        {
            cmd.ReplaceComponent(entity, new Position { X = 3, Y = 4 });
        });

        var replaced = host.World.ReadComponent<Position>(entity);
        Assert.Equal(3, replaced.X);
        Assert.Equal(4, replaced.Y);

        host.World.Apply(cmd =>
        {
            cmd.RemoveComponent<Position>(entity);
        });

        Assert.False(host.World.HasComponent<Position>(entity));
    }
}

