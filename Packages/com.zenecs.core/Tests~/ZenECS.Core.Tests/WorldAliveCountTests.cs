using Xunit;

namespace ZenECS.Core.Tests
{
    public class WorldAliveCountTests
    {
        [Fact]
        public void AliveCount_tracks_create_and_destroy()
        {
            var kernel = new Kernel();
            IWorld world = kernel.CreateWorld();

            using (var cmd = world.BeginWrite())
            {
                cmd.CreateEntity();
                cmd.CreateEntity();
            }
            world.RunScheduledJobs();

            Assert.Equal(2, world.AliveCount);

            // Destroy via command buffer for deterministic apply
            using (var cmd = world.BeginWrite())
            {
                cmd.DestroyEntity(new Entity(1, 0)); // generation ignored if dead
            }
            world.RunScheduledJobs();
            Assert.Equal(1, world.AliveCount);

            using (var cmd = world.BeginWrite())
            {
                cmd.DestroyEntity(new Entity(2, 0)); // generation ignored if dead
            }
            world.RunScheduledJobs();
            Assert.Equal(0, world.AliveCount);

            kernel.Dispose();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AliveCount_resets_and_reseeds(bool keepCapacity)
        {
            var kernel = new Kernel();
            IWorld world = kernel.CreateWorld();

            using (var cmd = world.BeginWrite())
            {
                cmd.CreateEntity();
                cmd.CreateEntity();
            }
            world.RunScheduledJobs();
            Assert.Equal(2, world.AliveCount);

            world.Reset(keepCapacity);
            Assert.Equal(0, world.AliveCount);

            Entity e;
            using (var cmd = world.BeginWrite())
            {
                e = cmd.CreateEntity();
            }
            world.RunScheduledJobs();
            Assert.Equal(1, world.AliveCount);

            using (var cmd = world.BeginWrite())
            {
                cmd.DestroyEntity(e);
            }
            world.RunScheduledJobs();
            Assert.Equal(0, world.AliveCount);

            kernel.Dispose();
        }
    }
}

