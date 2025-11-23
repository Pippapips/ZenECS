// using System;
// using ZenECS.Physics.Unity.Simulation.Components;
//
// namespace ZenECS.Physics.Unity.Simulation
// {
//     /// <summary>
//     /// 코너 슬라이딩/강도 옵션.
//     /// Projectile / Player를 다르게 세팅할 때 사용.
//     /// </summary>
//     public readonly struct KinematicMoveOptions2D
//     {
//         public readonly bool EnableCornerSlide;
//         public readonly float CornerSlideIntensity; // 0~1 권장
//
//         public KinematicMoveOptions2D(bool enableCornerSlide, float cornerSlideIntensity)
//         {
//             EnableCornerSlide = enableCornerSlide;
//             CornerSlideIntensity = cornerSlideIntensity < 0f ? 0f : cornerSlideIntensity;
//         }
//
//         public static readonly KinematicMoveOptions2D None =
//             new KinematicMoveOptions2D(false, 0f);
//
//         /// <summary>Projectile용: 코너 슬라이딩 약하게</summary>
//         public static readonly KinematicMoveOptions2D ProjectileSoft =
//             new KinematicMoveOptions2D(true, 0.25f);
//
//         /// <summary>캐릭터용: 코너 슬라이딩 강하게</summary>
//         public static readonly KinematicMoveOptions2D CharacterStrong =
//             new KinematicMoveOptions2D(true, 1.0f);
//     }
//
//     public static class KinematicGridMove2D
//     {
//         /// <summary>
//         /// 타일맵 기반 2D 원형 이동.
//         /// - X,Y 축 분리 이동 + 1유닛 스윕
//         /// - X/Y 둘 다 막힌 코너에서, 옵션에 따라 슬라이딩 시도
//         /// 반환값: 실제로 위치가 바뀌었는지 여부.
//         /// </summary>
//         public static bool MoveWithTileCollision(
//             ref FixedPosition2D pos,
//             in Velocity2D vel,
//             in CircleCollider2D col,
//             in MapGrid2D map,
//             in KinematicMoveOptions2D options,
//             int moveInputX, // MoveInput.x
//             int moveInputY) // MoveInput.y
//         {
//             int vx = vel.vx;
//             int vy = vel.vy;
//
//             if (vx == 0 && vy == 0)
//                 return false;
//
//             int radius = col.radius;
//
//             int startX = pos.x;
//             int startY = pos.y;
//
//             bool movedX = false;
//             bool movedY = false;
//             bool hitX = false;
//             bool hitY = false;
//
//             // --- 1) X 축 이동 ---
//             if (vx != 0)
//             {
//                 int targetX = startX + vx;
//
//                 if (!TileCollision2D.CheckCircle(in map, targetX, startY, radius))
//                 {
//                     pos.x = targetX;
//                     movedX = true;
//                 }
//                 else
//                 {
//                     hitX = true;
//
//                     int dirX = vx > 0 ? 1 : -1;
//                     int currentX = startX;
//                     int remaining = Math.Abs(vx);
//
//                     while (remaining-- > 0)
//                     {
//                         int nextX = currentX + dirX;
//                         if (TileCollision2D.CheckCircle(in map, nextX, startY, radius))
//                             break;
//
//                         currentX = nextX;
//                     }
//
//                     if (currentX != startX)
//                     {
//                         pos.x = currentX;
//                         movedX = true;
//                     }
//                 }
//             }
//
//             // --- 2) Y 축 이동 (X 결과 기준) ---
//             if (vy != 0)
//             {
//                 int baseXForY = pos.x; // X 결과 기준
//
//                 int targetY = startY + vy;
//                 if (!TileCollision2D.CheckCircle(in map, baseXForY, targetY, radius))
//                 {
//                     pos.y = targetY;
//                     movedY = true;
//                 }
//                 else
//                 {
//                     hitY = true;
//
//                     int dirY = vy > 0 ? 1 : -1;
//                     int currentY = startY;
//                     int remaining = Math.Abs(vy);
//
//                     while (remaining-- > 0)
//                     {
//                         int nextY = currentY + dirY;
//                         if (TileCollision2D.CheckCircle(in map, baseXForY, nextY, radius))
//                             break;
//
//                         currentY = nextY;
//                     }
//
//                     if (currentY != startY)
//                     {
//                         pos.y = currentY;
//                         movedY = true;
//                     }
//                 }
//             }
//
//             bool moved = movedX || movedY;
//
//             // --- 3) 코너/슬라이딩 처리 ---
//             if (!options.EnableCornerSlide)
//                 return moved;
//
//             // (1) 대각선 코너: 기존 TryCornerSlide 유지
//             if (vx != 0 && vy != 0 &&
//                 !movedX && !movedY &&
//                 hitX && hitY)
//             {
//                 TryCornerSlideDiagonal(
//                     ref pos,
//                     startX, startY,
//                     vx, vy,
//                     radius,
//                     in map,
//                     in options,
//                     ref moved);
//             }
//             // (2) 수직 이동 중 위/아래가 막혔고, 아직 Y도 못 움직였다면 → 수평 슬라이드
//             else if (vy != 0 && !movedY && hitY)
//             {
//                 TryVerticalCornerSlideWithForward(
//                     ref pos,
//                     startX, startY,
//                     vx, vy,
//                     moveInputX, moveInputY,
//                     radius,
//                     in map,
//                     in options,
//                     ref moved);
//             }
//             // (3) 수평 이동 중 좌/우가 막혔고, 아직 X도 못 움직였다면 → 수직 슬라이드
//             else if (vx != 0 && !movedX && hitX)
//             {
//                 TryHorizontalCornerSlideWithForward(
//                     ref pos,
//                     startX, startY,
//                     vx, vy,
//                     moveInputX, moveInputY,
//                     radius,
//                     in map,
//                     in options,
//                     ref moved);
//             }
//
//             return moved;
//         }
//
//         private static void TryCornerSlideDiagonal(
//             ref FixedPosition2D pos,
//             int startX,
//             int startY,
//             int vx,
//             int vy,
//             int radius,
//             in MapGrid2D map,
//             in KinematicMoveOptions2D options,
//             ref bool moved)
//         {
//             // 입력 방향 정규화
//             float dx = vx;
//             float dy = vy;
//             float lenSq = dx * dx + dy * dy;
//             if (lenSq <= 0.0001f)
//                 return;
//
//             float invLen = 1.0f / (float)Math.Sqrt(lenSq);
//             dx *= invLen;
//             dy *= invLen;
//
//             // X쪽 벽 노멀: vx > 0 → 오른쪽 벽에 막힘 → 노멀은 왼쪽(-1,0)
//             float nxX = (vx > 0) ? -1f : 1f;
//             float nyX = 0f;
//
//             // Y쪽 벽 노멀: vy > 0 → 위쪽 벽에 막힘 → 노멀은 아래(0,-1)
//             float nxY = 0f;
//             float nyY = (vy > 0) ? -1f : 1f;
//
//             float dotX = dx * nxX + dy * nyX;
//             float dotY = dx * nxY + dy * nyY;
//
//             // dot 값이 더 작은 쪽이 "더 평행한" 벽
//             bool preferX = Math.Abs(dotX) < Math.Abs(dotY);
//
//             int maxBase = Math.Max(Math.Abs(vx), Math.Abs(vy));
//             if (maxBase <= 0) maxBase = 1;
//
//             int maxSlideUnits = (int)Math.Ceiling(options.CornerSlideIntensity * maxBase);
//             if (maxSlideUnits <= 0)
//                 return;
//
//             // 우선 한 축 먼저 시도하고, 안 움직였으면 반대 축 fallback
//             bool movedLocal = false;
//
//             if (preferX)
//             {
//                 movedLocal = TrySlideAlongXWall(ref pos, startX, startY, vy, radius, in map, maxSlideUnits);
//                 if (!movedLocal)
//                 {
//                     movedLocal = TrySlideAlongYWall(ref pos, startX, startY, vx, radius, in map, maxSlideUnits);
//                 }
//             }
//             else
//             {
//                 movedLocal = TrySlideAlongYWall(ref pos, startX, startY, vx, radius, in map, maxSlideUnits);
//                 if (!movedLocal)
//                 {
//                     movedLocal = TrySlideAlongXWall(ref pos, startX, startY, vy, radius, in map, maxSlideUnits);
//                 }
//             }
//
//             if (movedLocal)
//                 moved = true;
//         }
//
//         private static bool TrySlideAlongXWall(
//             ref FixedPosition2D pos,
//             int startX,
//             int startY,
//             int vy,
//             int radius,
//             in MapGrid2D map,
//             int maxSlideUnits)
//         {
//             int dirY = vy > 0 ? 1 : -1;
//             int currentY = startY;
//             int remaining = maxSlideUnits;
//
//             while (remaining-- > 0)
//             {
//                 int testY = currentY + dirY;
//
//                 // ✅ 슬라이딩은 "부딪히면 멈춤"
//                 if (TileCollision2D.CheckCircle(in map, startX, testY, radius))
//                     break;
//
//                 // 빈 칸이면 한 칸 전진
//                 currentY = testY;
//             }
//
//             if (currentY != startY)
//             {
//                 pos.x = startX;   // X는 코너에 붙어있고
//                 pos.y = currentY; // Y로만 미끄러져간 결과
//                 return true;
//             }
//
//             return false;
//         }
//
//         private static bool TrySlideAlongYWall(
//             ref FixedPosition2D pos,
//             int startX,
//             int startY,
//             int vx,
//             int radius,
//             in MapGrid2D map,
//             int maxSlideUnits)
//         {
//             int dirX = vx > 0 ? 1 : -1;
//             int currentX = startX;
//             int remaining = maxSlideUnits;
//
//             while (remaining-- > 0)
//             {
//                 int testX = currentX + dirX;
//
//                 // ✅ 여기서도 마찬가지로 "충돌이면 멈춤"
//                 if (TileCollision2D.CheckCircle(in map, testX, startY, radius))
//                     break;
//
//                 currentX = testX;
//             }
//
//             if (currentX != startX)
//             {
//                 pos.x = currentX; // X로만 미끄러져간 결과
//                 pos.y = startY;
//                 return true;
//             }
//
//             return false;
//         }
//
//         private static void TryVerticalCornerSlideWithForward(
//             ref FixedPosition2D pos,
//             int startX,
//             int startY,
//             int vx,
//             int vy,
//             int moveInputX,
//             int moveInputY,
//             int radius,
//             in MapGrid2D map,
//             in KinematicMoveOptions2D options,
//             ref bool moved)
//         {
//             // 이번 틱 Y 목표 (full speed)
//             int targetY = startY + vy;
//
//             int maxSlideUnits = Math.Abs(vy);
//             if (maxSlideUnits <= 0)
//                 return;
//
//             // Forward (MoveInput)를 정규화해서 "기울기" 판단
//             float fx = moveInputX;
//             float fy = moveInputY;
//             float lenSq = fx * fx + fy * fy;
//             if (lenSq > 0.0001f)
//             {
//                 float invLen = 1.0f / (float)Math.Sqrt(lenSq);
//                 fx *= invLen;
//                 fy *= invLen;
//             }
//             else
//             {
//                 fx = 0f;
//                 fy = 1f; // 기본값: 위로
//             }
//
//             // 슬라이드 후보 X 방향: Forward.x를 우선 반영
//             int[] slideDirsX;
//             if (fx > 0.01f)
//             {
//                 // 오른쪽 기울기 → 오른쪽 우선
//                 slideDirsX = new[] { +1, -1 };
//             }
//             else if (fx < -0.01f)
//             {
//                 // 왼쪽 기울기 → 왼쪽 우선
//                 slideDirsX = new[] { -1, +1 };
//             }
//             else
//             {
//                 // 수직 입력 (정면) → 양쪽 다 후보
//                 slideDirsX = new[] { -1, +1 };
//             }
//
//             // 수직 벽과 "완전 정면"이면 슬라이딩 안 함 (규칙 2)
//             // 여기서는 상/하 벽 normal ~ (0, ±1) 이라고 보고 dot 체크
//             float ny = (vy > 0) ? -1f : 1f; // 위로 갈 때는 위쪽에서 아래로 오는 normal
//             float dot = fx * 0f + fy * ny;
//             if (Math.Abs(dot) > 0.999f)
//             {
//                 // 정말 정면에 가까운 각도면 슬라이드 X
//                 return;
//             }
//
//             foreach (int dirX in slideDirsX)
//             {
//                 int slideX = startX;
//                 int remaining = maxSlideUnits;
//
//                 // 1단계: 현재 Y(startY) 기준으로 수평 슬라이드 가능한 X 찾기
//                 while (remaining-- > 0)
//                 {
//                     int testX = slideX + dirX;
//
//                     if (TileCollision2D.CheckCircle(in map, testX, startY, radius))
//                         break; // 이 방향으로는 더 가면 벽
//
//                     slideX = testX;
//                 }
//
//                 if (slideX == startX)
//                     continue; // 이 방향은 못 움직였음
//
//                 // 2단계: 찾은 slideX에서 위로 vy만큼 이동 시도
//                 int finalY = startY;
//                 int dirY = vy > 0 ? 1 : -1;
//                 int remainY = Math.Abs(vy);
//
//                 while (remainY-- > 0)
//                 {
//                     int testY = finalY + dirY;
//                     if (TileCollision2D.CheckCircle(in map, slideX, testY, radius))
//                         break;
//
//                     finalY = testY;
//                 }
//
//                 pos.x = slideX;
//                 pos.y = finalY;
//                 moved = (slideX != startX) || (finalY != startY);
//                 return;
//             }
//         }
//
//         private static void TryHorizontalCornerSlideWithForward(
//             ref FixedPosition2D pos,
//             int startX,
//             int startY,
//             int vx,
//             int vy,
//             int moveInputX,
//             int moveInputY,
//             int radius,
//             in MapGrid2D map,
//             in KinematicMoveOptions2D options,
//             ref bool moved)
//         {
//             int targetX = startX + vx;
//             int maxSlideUnits = Math.Abs(vx);
//             if (maxSlideUnits <= 0)
//                 return;
//
//             // Forward 정규화
//             float fx = moveInputX;
//             float fy = moveInputY;
//             float lenSq = fx * fx + fy * fy;
//             if (lenSq > 0.0001f)
//             {
//                 float invLen = 1.0f / (float)Math.Sqrt(lenSq);
//                 fx *= invLen;
//                 fy *= invLen;
//             }
//             else
//             {
//                 fx = 1f;
//                 fy = 0f;
//             }
//
//             // 슬라이드 후보 Y 방향: Forward.y를 반영
//             int[] slideDirsY;
//             if (fy > 0.01f)
//             {
//                 // 위로 기울기 → 위쪽 우선
//                 slideDirsY = new[] { +1, -1 };
//             }
//             else if (fy < -0.01f)
//             {
//                 // 아래로 기울기 → 아래쪽 우선
//                 slideDirsY = new[] { -1, +1 };
//             }
//             else
//             {
//                 slideDirsY = new[] { -1, +1 };
//             }
//
//             // 수평 벽 정면 체크 (normal ~ (±1, 0))
//             float nx = (vx > 0) ? -1f : 1f;
//             float dot = fx * nx + fy * 0f;
//             if (Math.Abs(dot) > 0.999f)
//             {
//                 // 정면에 가까우면 슬라이드 X
//                 return;
//             }
//
//             foreach (int dirY in slideDirsY)
//             {
//                 int slideY = startY;
//                 int remaining = maxSlideUnits;
//
//                 // 1단계: 현재 X(startX) 기준으로 수직 슬라이드 가능한 Y 찾기
//                 while (remaining-- > 0)
//                 {
//                     int testY = slideY + dirY;
//
//                     if (TileCollision2D.CheckCircle(in map, startX, testY, radius))
//                         break;
//
//                     slideY = testY;
//                 }
//
//                 if (slideY == startY)
//                     continue;
//
//                 // 2단계: slideY에서 왼/오른쪽으로 vx만큼 이동 시도
//                 int finalX = startX;
//                 int dirX = vx > 0 ? 1 : -1;
//                 int remainX = Math.Abs(vx);
//
//                 while (remainX-- > 0)
//                 {
//                     int testX = finalX + dirX;
//                     if (TileCollision2D.CheckCircle(in map, testX, slideY, radius))
//                         break;
//
//                     finalX = testX;
//                 }
//
//                 pos.x = finalX;
//                 pos.y = slideY;
//                 moved = (finalX != startX) || (slideY != startY);
//                 return;
//             }
//         }
//     }
// }