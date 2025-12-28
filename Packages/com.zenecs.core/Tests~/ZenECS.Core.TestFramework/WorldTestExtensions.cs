using System;
using ZenECS.Core;
using ZenECS.Core.Internal;

namespace ZenECS.Core.TestFramework;

/// <summary>
/// Small helper extensions to cut down boilerplate in Core tests.
/// </summary>
public static class WorldTestExtensions
{
    /// <summary>
    /// Records commands into a buffer and immediately flushes scheduled jobs.
    /// Sets WritePhase to Simulation to allow structural changes during tests.
    /// </summary>
    /// <param name="world">Target world.</param>
    /// <param name="record">Command recording action.</param>
    public static void Apply(this IWorld world, Action<ICommandBuffer> record)
    {
        if (world is null) throw new ArgumentNullException(nameof(world));
        if (record is null) throw new ArgumentNullException(nameof(record));

        // Set WritePhase to Simulation to allow structural changes (like SystemRunner does)
        world.SetWritePhaseForTest(denyAllWrites: false, structuralChangesAllowed: true);
        try
        {
            using var cmd = world.BeginWrite();
            record(cmd);
            cmd.EndWrite();
            world.RunScheduledJobs();
        }
        finally
        {
            world.ClearWritePhaseForTest();
        }
    }

    /// <summary>
    /// Creates an entity via command buffer, optionally chaining more commands, then flushes.
    /// </summary>
    /// <param name="world">Target world.</param>
    /// <param name="afterCreate">Optional additional recording that uses the buffer and created entity.</param>
    /// <returns>Created entity handle.</returns>
    public static Entity CreateEntity(this IWorld world, Action<ICommandBuffer, Entity>? afterCreate = null)
    {
        if (world is null) throw new ArgumentNullException(nameof(world));

        Entity created = Entity.None;
        world.Apply(cmd =>
        {
            created = cmd.CreateEntity();
            afterCreate?.Invoke(cmd, created);
        });
        return created;
    }

    /// <summary>
    /// Flushes any scheduled jobs for the world and returns how many ran.
    /// </summary>
    public static int FlushJobs(this IWorld world)
    {
        if (world is null) throw new ArgumentNullException(nameof(world));
        return world.RunScheduledJobs();
    }
}
