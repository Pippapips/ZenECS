#nullable enable
using ZenECS.Core;
using ZenECS.Core.Attributes;
using ZenECS.Core.Systems;
using ZenECS.Physics.Unity.Simulation.Components;

namespace ZenECS.Physics.Unity.Simulation.Systems
{
    /// <summary>
    /// FixedSimulation 후반:
    /// - HitEvent를 처리하여 Health 감소 및 DeadTag 부여.
    /// </summary>
    [FixedPostGroup]
    [ZenSystemWatch(typeof(HitEvent))]
    public sealed class ResolveHitEventsSystem : IFixedRunSystem
    {
        public void Run(IWorld w, float dt)
        {
            using var cmd = w.BeginWrite();

            foreach (var (evt, hit) in w.Query<HitEvent>())
            {
                if (w.IsAlive(hit.Target) && w.HasComponent<Health>(hit.Target))
                {
                    var health = w.ReadComponent<Health>(hit.Target);
                    health.Current -= hit.Damage;
                    if (health.Current < 0) health.Current = 0;
                    cmd.ReplaceComponent(hit.Target, health);

                    if (health.Current == 0 && !w.HasComponent<DeadTag>(hit.Target))
                    {
                        cmd.AddComponent(hit.Target, new DeadTag());
                    }
                }

                cmd.DespawnEntity(evt);
            }
        }
    }
}