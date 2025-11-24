// using Unity.Mathematics;
// using ZenECS.Physics.Unity.Simulation;
// using ZenECS.Physics.Unity.Simulation.Components;
//
// namespace ZenECS.Physics.Unity.Simulation.Systems
// {
//     public struct KinematicMoveResult2D
//     {
//         public bool DidMove;
//         public bool HitWall;
//
//         // "진짜 슬라이딩 / 코너 어시스트"가 발동했는지 플래그로 보고 싶으면 사용
//         public bool Slided;
//         public bool CornerAssist;
//
//         // 마지막으로 충돌한 타일의 타일 좌표 (tx, ty)
//         public int LastHitTileX;
//         public int LastHitTileY;
//
//         public static implicit operator bool(KinematicMoveResult2D r) => r.DidMove;
//     }
// }
