using System;

namespace ZenECS.Core.TestFramework;

/// <summary>
/// Small helper extensions to cut down boilerplate in Core tests.
/// </summary>
public static class WorldTestExtensions
{
    /// <summary>
    /// Records commands into a buffer and immediately flushes scheduled jobs.
    /// </summary>
    /// <param name="world">Target world.</param>
    /// <param name="record">Command recording action.</param>
    public static void Apply(this IWorld world, Action<ICommandBuffer> record)
    {
        if (world is null) throw new ArgumentNullException(nameof(world));
        if (record is null) throw new ArgumentNullException(nameof(record));

        using var cmd = world.BeginWrite();
        record(cmd);
        cmd.EndWrite();
        world.RunScheduledJobs();
    }

    /// <summary>
    /// Creates an entity via command buffer, optionally chaining more commands, then flushes.
    /// </summary>
    /// <param name="world">Target world.</param>
    /// <param name="afterCreate">Optional additional recording that uses the buffer.</param>
    /// <returns>Created entity handle.</returns>
    public static Entity CreateEntity(this IWorld world, Action<ICommandBuffer>? afterCreate = null)
    {
        Entity created = Entity.None;
        world.Apply(cmd =>
        {
            created = cmd.CreateEntity();
            afterCreate?.Invoke(cmd);
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

