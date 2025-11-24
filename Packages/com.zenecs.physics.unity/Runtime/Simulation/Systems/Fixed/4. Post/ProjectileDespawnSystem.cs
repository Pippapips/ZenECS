#nullable enable
using ZenECS.Core;
using ZenECS.Core.Attributes;
using ZenECS.Core.Systems;
using ZenECS.Physics.Unity.Simulation.Components;

namespace ZenECS.Physics.Unity.Simulation.Systems
{
    /// <summary>
    /// FixedSimulation 후반:
    /// - DeadTag 또는 MaxDistance 초과 Projectile을 삭제.
    /// </summary>
    [FixedPostGroup]
    [ZenSystemWatch(typeof(Projectile))]
    [OrderAfter(typeof(ResolveHitEventsSystem))]
    public sealed class ProjectileDespawnSystem : IFixedRunSystem
    {
        public void Run(IWorld w, float dt)
        {
            using var cmd = w.BeginWrite();

            foreach (var (e, proj) in w.Query<Projectile>())
            {
                bool shouldDespawn = false;

                if (w.TryReadComponent<DeadTag>(e, out var deadTag))
                {
                    shouldDespawn = deadTag.DelayTickCount <= w.Tick;
                }

                if (!shouldDespawn && proj.MaxDistance > 0 && proj.Traveled >= proj.MaxDistance)
                    shouldDespawn = true;

                if (!shouldDespawn)
                    continue;

                cmd.DespawnEntity(e);
            }
        }
    }
}