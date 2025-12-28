using ZenECS.Core;
using ZenECS.Core.Systems;

namespace ZenEcsAdapterUnitySamples.SystemPresets
{
    /// <summary>
    /// Movement system (FixedGroup).
    /// </summary>
    [FixedGroup]
    [ZenSystemWatch(typeof(Position), typeof(Velocity))]
    public sealed class MovementSystem : ISystem
    {
        /// <inheritdoc />
        public void Run(IWorld w, float dt)
        {
            using var cmd = w.BeginWrite();
            foreach (var (e, pos, vel) in w.Query<Position, Velocity>())
            {
                cmd.ReplaceComponent(e, new Position(
                    pos.X + vel.X * dt,
                    pos.Y + vel.Y * dt,
                    pos.Z + vel.Z * dt
                ));
            }
        }
    }
}