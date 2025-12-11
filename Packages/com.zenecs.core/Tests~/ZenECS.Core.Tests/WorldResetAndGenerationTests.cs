using Xunit;
using ZenECS.Core.TestFramework;

namespace ZenECS.Core.Tests;

public class WorldResetAndGenerationTests
{
    [Fact]
    public void Destroyed_ids_are_reused_with_bumped_generation()
    {
        using var host = new TestWorldHost();

        Entity first = host.World.CreateEntity();

        host.World.Apply(cmd => cmd.DestroyEntity(first));

        Entity second = host.World.CreateEntity();

        Assert.Equal(first.Id, second.Id);
        Assert.NotEqual(first.Gen, second.Gen);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Reset_clears_entities_and_components(bool keepCapacity)
    {
        using var host = new TestWorldHost();

        Entity entity = host.World.CreateEntity((cmd, e) => cmd.AddComponent(e, 5));
        Assert.Equal(1, host.World.AliveCount);
        Assert.True(host.World.HasComponent<int>(entity));

        host.World.Reset(keepCapacity);
        Assert.Equal(0, host.World.AliveCount);

        entity = host.World.CreateEntity();

        Assert.Equal(1, host.World.AliveCount);
        Assert.False(host.World.HasComponent<int>(entity));
        Assert.False(host.World.TryReadComponent(entity, out int _)); // Should return default without throwing
    }
}

