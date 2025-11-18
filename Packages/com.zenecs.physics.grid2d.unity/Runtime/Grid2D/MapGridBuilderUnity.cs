#nullable enable
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Physics.Kinematic2D;

namespace ZenECS.Physics.Grid2D.Unity
{
    /// <summary>
    /// Purpose:
    ///   Unity scene → MapGrid2D 변환 유틸.
    ///
    /// Key points:
    ///   • IWorld/SetSingleton 모름 (ECS 독립).
    ///   • root Transform 아래 자식들의 XZ 좌표를 타일 인덱스로 스냅.
    ///   • (minTx,minTy)~(maxTx,maxTy) 범위로 그리드 생성.
    ///   • originX, originY 를 min 타일에 맞춰 세팅.
    /// </summary>
    public static class MapGridBuilderUnity
    {
        /// <summary>
        /// Build a MapGrid2D from child transforms under root.
        /// </summary>
        /// <param name="root">Root transform whose children are treated as tiles.</param>
        /// <param name="unitsPerUnity">1 Unity unit = this many world units.</param>
        /// <param name="tileSizeUnity">Tile size in Unity units (1 cube = 1 tile → 1).</param>
        /// <param name="cubeLayerMask">
        /// Only objects on these layers are treated as solid tiles.
        /// Use ~0 to include all layers.
        /// </param>
        /// <param name="includeInactiveChildren">
        /// Whether to include inactive children while scanning.
        /// </param>
        public static MapGrid2D BuildFromChildren(
            Transform root,
            int unitsPerUnity,
            float tileSizeUnity,
            LayerMask cubeLayerMask,
            bool includeInactiveChildren)
        {
            var cubes = CollectCubeTransforms(root, cubeLayerMask, includeInactiveChildren);
            return BuildFromTransforms(cubes, unitsPerUnity, tileSizeUnity);
        }

        /// <summary>
        /// Build MapGrid2D from an arbitrary list of transforms.
        /// </summary>
        public static MapGrid2D BuildFromTransforms(
            IReadOnlyList<Transform> transforms,
            int unitsPerUnity,
            float tileSizeUnity)
        {
            if (transforms.Count == 0)
            {
                return new MapGrid2D
                {
                    width = 0,
                    height = 0,
                    tileSize = Mathf.Max(1, unitsPerUnity),
                    originX = 0,
                    originY = 0,
                    collision = System.Array.Empty<byte>()
                };
            }

            float tileSize = Mathf.Max(0.0001f, tileSizeUnity);
            int tileSizeUnits = Mathf.RoundToInt(tileSize * unitsPerUnity);

            int minTx = int.MaxValue;
            int maxTx = int.MinValue;
            int minTy = int.MaxValue;
            int maxTy = int.MinValue;

            var tilePositions = new List<Vector2Int>(transforms.Count);

            for (int i = 0; i < transforms.Count; i++)
            {
                var t = transforms[i];
                var wp = t.position;

                int tx = Mathf.RoundToInt(wp.x / tileSize);
                int ty = Mathf.RoundToInt(wp.z / tileSize);

                tilePositions.Add(new Vector2Int(tx, ty));

                if (tx < minTx) minTx = tx;
                if (tx > maxTx) maxTx = tx;
                if (ty < minTy) minTy = ty;
                if (ty > maxTy) maxTy = ty;
            }

            int width = maxTx - minTx + 1;
            int height = maxTy - minTy + 1;

            if (width <= 0 || height <= 0)
            {
                return new MapGrid2D
                {
                    width = 0,
                    height = 0,
                    tileSize = tileSizeUnits,
                    originX = 0,
                    originY = 0,
                    collision = System.Array.Empty<byte>()
                };
            }

            int originX = Mathf.RoundToInt(minTx * tileSizeUnits);
            int originY = Mathf.RoundToInt(minTy * tileSizeUnits);

            var collision = new byte[width * height];

            for (int i = 0; i < tilePositions.Count; i++)
            {
                var tp = tilePositions[i];
                int localX = tp.x - minTx;
                int localY = tp.y - minTy;

                int idx = localX + localY * width;
                if ((uint)idx < (uint)collision.Length)
                {
                    collision[idx] = MapTileFlags.Wall;
                }
            }

            return new MapGrid2D
            {
                width = width,
                height = height,
                tileSize = tileSizeUnits,
                originX = originX,
                originY = originY,
                collision = collision
            };
        }

        static List<Transform> CollectCubeTransforms(
            Transform root,
            LayerMask cubeLayerMask,
            bool includeInactiveChildren)
        {
            var result = new List<Transform>();
            var queue = new Queue<Transform>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                for (int i = 0; i < current.childCount; i++)
                {
                    var child = current.GetChild(i);

                    if (!includeInactiveChildren && !child.gameObject.activeInHierarchy)
                        continue;

                    if ((cubeLayerMask.value & (1 << child.gameObject.layer)) != 0)
                    {
                        result.Add(child);
                    }

                    queue.Enqueue(child);
                }
            }

            return result;
        }
    }
}