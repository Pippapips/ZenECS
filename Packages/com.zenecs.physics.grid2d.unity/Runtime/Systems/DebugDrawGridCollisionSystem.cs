using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Attributes;
using ZenECS.Core.Systems;
using ZenECS.Physics.Components;
using CircleCollider2D = ZenECS.Physics.Components.CircleCollider2D;

namespace ZenECS.Physics.Grid2D.Unity.Systems
{
    [PresentationGroup] // LateFrame에서 호출되면 SceneView/Game 뷰 둘 다에서 보임
    public sealed class DebugDrawGridCollisionSystem : IPresentationSystem
    {
        public void Run(IWorld w, float dt, float alpha)
        {
#if UNITY_EDITOR
            foreach (var (e, fpos, col, stats) in
                     w.Query<FixedPosition2D, CircleCollider2D, MovementStats2D>())
            {
                // FixedPosition(int) → Unity 좌표(float)
                float x = fpos.x / (float)stats.UnitsPerUnity;
                float y = fpos.y / (float)stats.UnitsPerUnity;

                // radius도 같은 스케일로 변환
                float radius = col.radius / (float)stats.UnitsPerUnity;

                var center = new Vector3(x, 0f, y);

                DrawWireSphereLike(center, radius, Color.yellow);
            }
#endif
        }

#if UNITY_EDITOR
        static void DrawWireSphereLike(Vector3 center, float radius, Color color, int segments = 24)
        {
            // XZ 평면 (바닥 원)
            DrawWireCircle(center, Vector3.up, radius, color, segments);
            // XY 평면 (카메라 정면에 보이는 원)
            DrawWireCircle(center, Vector3.forward, radius, color, segments);
            // YZ 평면 (옆에서 보이는 원)
            DrawWireCircle(center, Vector3.right, radius, color, segments);
        }
        
        static void DrawWireCircle(Vector3 center, Vector3 normal, float radius, Color color, int segments)
        {
            // normal 기준으로 임의의 축 계산
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.sqrMagnitude < 0.0001f)
                tangent = Vector3.Cross(normal, Vector3.right);
            tangent.Normalize();
            Vector3 bitangent = Vector3.Cross(normal, tangent);

            Vector3 prev = center + tangent * radius;
            for (int i = 1; i <= segments; i++)
            {
                float t = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 next = center +
                               (Mathf.Cos(t) * tangent + Mathf.Sin(t) * bitangent) * radius;
                Debug.DrawLine(prev, next, color, 0f, false);
                prev = next;
            }
        }
#endif
    }
}