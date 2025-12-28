using Xunit;
using ZenECS.Core.TestFramework;

namespace ZenECS.Core.Tests;

public class ExternalCommandTests
{
    private struct Health
    {
        public int Value;
    }

    [Fact]
    public void External_commands_flush_on_fixed_step()
    {
        using var host = new TestWorldHost();

        host.World.ExternalCommandEnqueue(ExternalCommand.CreateEntity((created, cmd) =>
        {
            cmd.AddComponent(created, new Health { Value = 10 });
        }));

        Assert.True(host.World.HasExternalCommand);

        host.TickFixed();

        var entities = host.World.GetAllEntities();
        Assert.Single(entities);
        var entity = entities[0];

        Assert.True(host.World.HasComponent<Health>(entity));
        Assert.Equal(10, host.World.ReadComponent<Health>(entity).Value);
        Assert.False(host.World.HasExternalCommand);
        Assert.Equal(1, host.World.Tick);
    }
}

