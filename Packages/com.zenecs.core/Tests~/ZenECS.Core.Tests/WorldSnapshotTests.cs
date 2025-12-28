using System.IO;
using Xunit;
using ZenECS.Core;
using ZenECS.Core.Serialization;
using ZenECS.Core.TestFramework;

namespace ZenECS.Core.Tests;

public class WorldSnapshotTests
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

    private struct UnregisteredComponent
    {
        public int Value;
    }

    private sealed class PositionFormatter : BinaryComponentFormatter<Position>
    {
        public override void Write(in Position value, ISnapshotBackend backend)
        {
            backend.WriteInt(value.X);
            backend.WriteInt(value.Y);
        }

        public override Position ReadTyped(ISnapshotBackend backend)
        {
            return new Position
            {
                X = backend.ReadInt(),
                Y = backend.ReadInt()
            };
        }
    }

    private sealed class HealthFormatter : BinaryComponentFormatter<Health>
    {
        public override void Write(in Health value, ISnapshotBackend backend)
        {
            backend.WriteInt(value.Value);
        }

        public override Health ReadTyped(ISnapshotBackend backend)
        {
            return new Health { Value = backend.ReadInt() };
        }
    }

    [Fact]
    public void Save_and_load_snapshot_preserves_entities_and_components()
    {
        // Register formatters
        ComponentRegistry.RegisterFormatter(new PositionFormatter(), "test.position");
        ComponentRegistry.RegisterFormatter(new HealthFormatter(), "test.health");

        try
        {
            using var host1 = new TestWorldHost();
            
            // Create entities with components
            Entity e1 = host1.World.CreateEntity((cmd, e) =>
            {
                cmd.AddComponent(e, new Position { X = 10, Y = 20 });
                cmd.AddComponent(e, new Health { Value = 100 });
            });

            Entity e2 = host1.World.CreateEntity((cmd, e) =>
            {
                cmd.AddComponent(e, new Position { X = 30, Y = 40 });
            });

            Assert.Equal(2, host1.World.AliveCount);

            // Save snapshot
            using var stream = new MemoryStream();
            host1.World.SaveFullSnapshotBinary(stream);
            stream.Position = 0;

            // Load into new world
            using var host2 = new TestWorldHost();
            host2.World.LoadFullSnapshotBinary(stream);

            // Verify entities and components
            Assert.Equal(2, host2.World.AliveCount);
            
            var entities = host2.World.GetAllEntities();
            Assert.Equal(2, entities.Count);

            // Find entities by component values
            Entity? loadedE1 = null;
            Entity? loadedE2 = null;

            foreach (var entity in entities)
            {
                if (host2.World.TryReadComponent(entity, out Position pos))
                {
                    if (pos.X == 10 && pos.Y == 20)
                    {
                        loadedE1 = entity;
                    }
                    else if (pos.X == 30 && pos.Y == 40)
                    {
                        loadedE2 = entity;
                    }
                }
            }

            Assert.NotNull(loadedE1);
            Assert.NotNull(loadedE2);

            // Verify e1 components
            Assert.True(host2.World.HasComponent<Position>(loadedE1.Value));
            Assert.True(host2.World.HasComponent<Health>(loadedE1.Value));
            var pos1 = host2.World.ReadComponent<Position>(loadedE1.Value);
            var health1 = host2.World.ReadComponent<Health>(loadedE1.Value);
            Assert.Equal(10, pos1.X);
            Assert.Equal(20, pos1.Y);
            Assert.Equal(100, health1.Value);

            // Verify e2 components
            Assert.True(host2.World.HasComponent<Position>(loadedE2.Value));
            Assert.False(host2.World.HasComponent<Health>(loadedE2.Value));
            var pos2 = host2.World.ReadComponent<Position>(loadedE2.Value);
            Assert.Equal(30, pos2.X);
            Assert.Equal(40, pos2.Y);
        }
        finally
        {
            // Cleanup: Note that ComponentRegistry is global, so we can't easily unregister
            // In a real scenario, tests should use isolated registries or reset between tests
        }
    }

    [Fact]
    public void Snapshot_preserves_entity_generations()
    {
        ComponentRegistry.RegisterFormatter(new PositionFormatter(), "test.position");

        try
        {
            using var host1 = new TestWorldHost();

            Entity e1 = host1.World.CreateEntity((cmd, e) =>
            {
                cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            });

            var originalId = e1.Id;
            var originalGen = e1.Gen;

            // Destroy and recreate to bump generation
            host1.World.Apply(cmd => cmd.DestroyEntity(e1));
            Entity e2 = host1.World.CreateEntity((cmd, e) =>
            {
                cmd.AddComponent(e, new Position { X = 3, Y = 4 });
            });

            Assert.Equal(originalId, e2.Id);
            Assert.NotEqual(originalGen, e2.Gen);

            // Save snapshot
            using var stream = new MemoryStream();
            host1.World.SaveFullSnapshotBinary(stream);
            stream.Position = 0;

            // Load into new world
            using var host2 = new TestWorldHost();
            host2.World.LoadFullSnapshotBinary(stream);

            // Verify generation is preserved
            var entities = host2.World.GetAllEntities();
            Assert.Single(entities);
            var loaded = entities[0];
            
            Assert.Equal(originalId, loaded.Id);
            Assert.Equal(e2.Gen, loaded.Gen); // Should match the bumped generation
            Assert.True(host2.World.IsAlive(loaded));
        }
        finally
        {
            // Cleanup
        }
    }

    [Fact]
    public void Snapshot_clears_existing_world_state()
    {
        ComponentRegistry.RegisterFormatter(new PositionFormatter(), "test.position");

        try
        {
            using var host1 = new TestWorldHost();
            
            // Create entity in first world
            Entity e1 = host1.World.CreateEntity((cmd, e) =>
            {
                cmd.AddComponent(e, new Position { X = 100, Y = 200 });
            });

            // Save snapshot
            using var stream = new MemoryStream();
            host1.World.SaveFullSnapshotBinary(stream);
            stream.Position = 0;

            // Create second world with different entities
            using var host2 = new TestWorldHost();
            Entity e2 = host2.World.CreateEntity((cmd, e) =>
            {
                cmd.AddComponent(e, new Position { X = 999, Y = 888 });
            });
            Entity e3 = host2.World.CreateEntity();
            
            Assert.Equal(2, host2.World.AliveCount);

            // Load snapshot - should clear existing state
            host2.World.LoadFullSnapshotBinary(stream);

            // Should only have entities from snapshot
            Assert.Equal(1, host2.World.AliveCount);
            
            var entities = host2.World.GetAllEntities();
            Assert.Single(entities);
            
            var pos = host2.World.ReadComponent<Position>(entities[0]);
            Assert.Equal(100, pos.X);
            Assert.Equal(200, pos.Y);
        }
        finally
        {
            // Cleanup
        }
    }

    [Fact]
    public void Snapshot_empty_world_saves_and_loads_correctly()
    {
        try
        {
            using var host1 = new TestWorldHost();
            Assert.Equal(0, host1.World.AliveCount);

            // Save empty snapshot
            using var stream = new MemoryStream();
            host1.World.SaveFullSnapshotBinary(stream);
            stream.Position = 0;

            // Load into new world
            using var host2 = new TestWorldHost();
            host2.World.LoadFullSnapshotBinary(stream);

            // Should still be empty
            Assert.Equal(0, host2.World.AliveCount);
            var entities = host2.World.GetAllEntities();
            Assert.Empty(entities);
        }
        finally
        {
            // Cleanup
        }
    }

    [Fact]
    public void Snapshot_entity_without_components_preserved()
    {
        try
        {
            using var host1 = new TestWorldHost();

            // Create entity without components
            Entity e1 = host1.World.CreateEntity();
            Entity e2 = host1.World.CreateEntity();

            Assert.Equal(2, host1.World.AliveCount);

            // Save snapshot
            using var stream = new MemoryStream();
            host1.World.SaveFullSnapshotBinary(stream);
            stream.Position = 0;

            // Load into new world
            using var host2 = new TestWorldHost();
            host2.World.LoadFullSnapshotBinary(stream);

            // Verify entities exist but have no components
            Assert.Equal(2, host2.World.AliveCount);
            var entities = host2.World.GetAllEntities();
            Assert.Equal(2, entities.Count);

            foreach (var entity in entities)
            {
                Assert.True(host2.World.IsAlive(entity));
                // No components should be present
            }
        }
        finally
        {
            // Cleanup
        }
    }

    [Fact]
    public void Snapshot_preserves_free_ids_list()
    {
        ComponentRegistry.RegisterFormatter(new PositionFormatter(), "test.position");

        try
        {
            using var host1 = new TestWorldHost();

            // Create and destroy entities to populate free list
            Entity e1 = host1.World.CreateEntity((cmd, e) =>
            {
                cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            });
            Entity e2 = host1.World.CreateEntity((cmd, e) =>
            {
                cmd.AddComponent(e, new Position { X = 3, Y = 4 });
            });
            Entity e3 = host1.World.CreateEntity((cmd, e) =>
            {
                cmd.AddComponent(e, new Position { X = 5, Y = 6 });
            });

            var id1 = e1.Id;
            var id2 = e2.Id;
            var id3 = e3.Id;

            // Destroy middle entity to create a free ID
            host1.World.Apply(cmd => cmd.DestroyEntity(e2));

            // Save snapshot (should preserve free list)
            using var stream = new MemoryStream();
            host1.World.SaveFullSnapshotBinary(stream);
            stream.Position = 0;

            // Load into new world
            using var host2 = new TestWorldHost();
            host2.World.LoadFullSnapshotBinary(stream);

            // Verify entities before creating new one
            Assert.Equal(2, host2.World.AliveCount); // e1, e3 (e2 was destroyed)

            // Create new entity - should reuse the freed ID
            Entity newEntity = host2.World.CreateEntity((cmd, e) =>
            {
                cmd.AddComponent(e, new Position { X = 7, Y = 8 });
            });

            // The new entity should reuse id2 (the freed one)
            Assert.Equal(id2, newEntity.Id);
            Assert.Equal(3, host2.World.AliveCount); // e1, e3, newEntity
        }
        finally
        {
            // Cleanup
        }
    }

    [Fact]
    public void Snapshot_preserves_next_id()
    {
        ComponentRegistry.RegisterFormatter(new PositionFormatter(), "test.position");

        try
        {
            using var host1 = new TestWorldHost();

            // Create several entities
            Entity e1 = host1.World.CreateEntity((cmd, e) =>
            {
                cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            });
            Entity e2 = host1.World.CreateEntity((cmd, e) =>
            {
                cmd.AddComponent(e, new Position { X = 3, Y = 4 });
            });
            Entity e3 = host1.World.CreateEntity((cmd, e) =>
            {
                cmd.AddComponent(e, new Position { X = 5, Y = 6 });
            });

            var maxId = System.Math.Max(System.Math.Max(e1.Id, e2.Id), e3.Id);

            // Save snapshot
            using var stream = new MemoryStream();
            host1.World.SaveFullSnapshotBinary(stream);
            stream.Position = 0;

            // Load into new world
            using var host2 = new TestWorldHost();
            host2.World.LoadFullSnapshotBinary(stream);

            // Create new entity - should get next ID after the max
            Entity newEntity = host2.World.CreateEntity((cmd, e) =>
            {
                cmd.AddComponent(e, new Position { X = 7, Y = 8 });
            });

            // New entity ID should be greater than the max from snapshot
            Assert.True(newEntity.Id > maxId);
        }
        finally
        {
            // Cleanup
        }
    }

    [Fact]
    public void Snapshot_invalid_magic_header_throws_exception()
    {
        using var host = new TestWorldHost();

        // Create invalid snapshot data
        using var stream = new MemoryStream();
        var invalidHeader = new byte[] { (byte)'I', (byte)'N', (byte)'V', (byte)'A', (byte)'L', (byte)'I', (byte)'D', (byte)'!' };
        stream.Write(invalidHeader, 0, invalidHeader.Length);
        stream.Position = 0;

        // Should throw InvalidOperationException
        Assert.Throws<InvalidOperationException>(() =>
        {
            host.World.LoadFullSnapshotBinary(stream);
        });
    }

    [Fact]
    public void Snapshot_missing_formatter_throws_exception()
    {
        // Use a different component type that is definitely not registered
        using var host1 = new TestWorldHost();

        // Try to save entity with unregistered component type
        // This should fail when saving because formatter is required
        Entity e1 = host1.World.CreateEntity((cmd, e) =>
        {
            cmd.AddComponent(e, new UnregisteredComponent { Value = 42 });
        });

        // Ensure the component is actually applied (pool is created)
        host1.World.FlushJobs();
        
        // Verify component exists
        Assert.True(host1.World.HasComponent<UnregisteredComponent>(e1));

        using var stream = new MemoryStream();
        
        // Save should throw InvalidOperationException if formatter is missing
        // ComponentRegistry.GetFormatter throws InvalidOperationException when formatter is not found
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            host1.World.SaveFullSnapshotBinary(stream);
        });
        
        // Verify exception message
        Assert.Contains("Formatter not registered", exception.Message);
    }

    [Fact]
    public void Snapshot_post_load_migration_executes()
    {
        ComponentRegistry.RegisterFormatter(new PositionFormatter(), "test.position");

        bool migrationRan = false;

        // Create a test migration
        var migration = new TestMigration(() => migrationRan = true);

        try
        {
            PostLoadMigrationRegistry.Register(migration);

            using var host1 = new TestWorldHost();

            Entity e1 = host1.World.CreateEntity((cmd, e) =>
            {
                cmd.AddComponent(e, new Position { X = 1, Y = 2 });
            });

            // Save snapshot
            using var stream = new MemoryStream();
            host1.World.SaveFullSnapshotBinary(stream);
            stream.Position = 0;

            // Load into new world - should trigger migration
            using var host2 = new TestWorldHost();
            host2.World.LoadFullSnapshotBinary(stream);

            // Migration should have run
            Assert.True(migrationRan);
        }
        finally
        {
            PostLoadMigrationRegistry.Clear();
        }
    }

    [Fact]
    public void Snapshot_multiple_save_load_cycles_work()
    {
        ComponentRegistry.RegisterFormatter(new PositionFormatter(), "test.position");
        ComponentRegistry.RegisterFormatter(new HealthFormatter(), "test.health");

        try
        {
            using var host1 = new TestWorldHost();

            // Create initial entities
            Entity e1 = host1.World.CreateEntity((cmd, e) =>
            {
                cmd.AddComponent(e, new Position { X = 10, Y = 20 });
                cmd.AddComponent(e, new Health { Value = 100 });
            });

            // First save/load cycle
            using var stream1 = new MemoryStream();
            host1.World.SaveFullSnapshotBinary(stream1);
            stream1.Position = 0;

            using var host2 = new TestWorldHost();
            host2.World.LoadFullSnapshotBinary(stream1);

            // Modify in second world
            host2.World.Apply(cmd =>
            {
                var pos = host2.World.ReadComponent<Position>(e1);
                cmd.ReplaceComponent(e1, new Position { X = pos.X + 1, Y = pos.Y + 1 });
            });

            // Second save/load cycle
            using var stream2 = new MemoryStream();
            host2.World.SaveFullSnapshotBinary(stream2);
            stream2.Position = 0;

            using var host3 = new TestWorldHost();
            host3.World.LoadFullSnapshotBinary(stream2);

            // Verify final state
            var entities = host3.World.GetAllEntities();
            Assert.Single(entities);
            var pos = host3.World.ReadComponent<Position>(entities[0]);
            Assert.Equal(11, pos.X);
            Assert.Equal(21, pos.Y);
        }
        finally
        {
            // Cleanup
        }
    }

    private sealed class TestMigration : IPostLoadMigration
    {
        private readonly System.Action _onRun;

        public TestMigration(System.Action onRun)
        {
            _onRun = onRun;
        }

        public int Order => 0;

        public void Run(IWorld world)
        {
            _onRun();
        }
    }
}

