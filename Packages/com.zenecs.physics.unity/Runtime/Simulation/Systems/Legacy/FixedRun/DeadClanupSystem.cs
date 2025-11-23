// #nullable enable
// using UnityEngine;
// using ZenECS.Core;
// using ZenECS.Core.Attributes;
// using ZenECS.Core.Systems;
// using ZenECS.Physics.Unity.Simulation.Components;
//
// namespace ZenECS.Physics.Unity.Simulation.Systems
// {
//     /// <summary>
//     /// Applies HitEvent damage to Health and despawns dead entities.
//     /// </summary>
//     [ZenSystemWatch(typeof(DeadTag))]
//     [OrderAfter(typeof(KinematicMoveOnGrid2DSystem))]
//     [OrderAfter(typeof(ProjectileHitDetectionSystem))]
//     [OrderAfter(typeof(ProjectileLifetimeSystem))]
//     [OrderAfter(typeof(ApplyHitEventSystem))]
//     public sealed class DeadCleanupSystem : IFixedRunSystem
//     {
//         public void Run(IWorld w, float dt)
//         {
//             using var cmd = w.BeginWrite();
//             foreach (var (e, _) in w.Query<DeadTag>())
//             {
//                 Debug.Log($"<color=#ff0000>DeadTag Despawned</color> F:{w.Kernel.FrameCount} T:{w.Kernel.FixedFrameCount}");
//                 cmd.DespawnEntity(e);
//             }
//         }
//     }
// }