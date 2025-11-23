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
//     [ZenSystemWatch(typeof(HitEvent))]
//     [OrderAfter(typeof(ProjectileHitDetectionSystem))]
//     public sealed class ApplyHitEventSystem : IFixedRunSystem
//     {
//         public void Run(IWorld w, float dt)
//         {
//             using var cmd = w.BeginWrite();
//
//             foreach (var (e, hitEvent) in w.Query<HitEvent>())
//             {
//                 // 대상이 여전히 살아있으면 Health 적용
//                 // if (w.TryReadComponent<Health>(hit.Target, out var health))
//                 // {
//                 //     var newHealth = health;
//                 //     newHealth.Value -= hit.Damage;
//                 //
//                 //     cmd.ReplaceComponent(hit.Target, newHealth);
//                 //
//                 //     if (newHealth.Value <= 0)
//                 //     {
//                 //         // 죽으면 엔티티 제거
//                 //         // (원하면 별도의 DeathEvent 를 생성해서 이펙트/사운드 트리거 가능)
//                 //         cmd.DespawnEntity(hit.Target);
//                 //     }
//                 // }
//
//                 Debug.Log($"<color=#ff0000>Hit Event</color> F:{w.Kernel.FrameCount} T:{w.Kernel.FixedFrameCount}");
//                 // HitEvent 는 1틱짜리이므로 바로 제거
//                 //cmd.DespawnEntity(e);
//             }
//         }
//     }
// }