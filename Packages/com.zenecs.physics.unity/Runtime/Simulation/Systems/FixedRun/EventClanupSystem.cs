#nullable enable
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Attributes;
using ZenECS.Core.Systems;
using ZenECS.Physics.Unity.Simulation.Components;

namespace ZenECS.Physics.Unity.Simulation.Systems
{
    /// <summary>
    /// Applies HitEvent damage to Health and despawns dead entities.
    /// </summary>
    [ZenSystemWatch(typeof(HitEvent))]
    [OrderAfter(typeof(DeadCleanupSystem))]
    public sealed class EventCleanupSystem : IFixedRunSystem
    {
        public void Run(IWorld w, float dt)
        {
            using var cmd = w.BeginWrite();
            foreach (var (e, _) in w.Query<HitEvent>())
            {
                Debug.Log($"<color=#ff0000>HitEvent Despawned</color> F:{w.Kernel.FrameCount} T:{w.Kernel.FixedFrameCount}");
                cmd.DespawnEntity(e);
            }
        }
    }
}