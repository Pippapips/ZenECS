// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 06 - View Binder
// File: ViewBinderSample.cs
// Purpose: View Binder 패턴을 사용한 ECS 데이터 → Unity 뷰 바인딩 예제
// Key concepts:
//   • IBinder를 통한 뷰 바인딩
//   • 읽기 전용 업데이트 (FrameViewGroup)
//   • EntityLink를 통한 GameObject 접근
//
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Linking;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Systems;

namespace ZenEcsAdapterUnitySamples.ViewBinder
{
    /// <summary>
    /// Position 컴포넌트.
    /// </summary>
    public readonly struct Position
    {
        public readonly float X, Y, Z;
        public Position(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    /// <summary>
    /// Velocity 컴포넌트.
    /// </summary>
    public readonly struct Velocity
    {
        public readonly float X, Y, Z;
        public Velocity(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    /// <summary>
    /// Position을 Transform에 바인딩하는 Binder.
    /// </summary>
    public sealed class PositionBinder : IBinder
    {
        private Transform? _transform;
        public int ApplyOrder => 0;
        public int AttachOrder => 0;

        public void OnAttach(IWorld w, Entity e)
        {
            // EntityLink를 통해 GameObject 찾기
            var registry = EntityViewRegistry.For(w);
            if (registry.TryGetView(e, out var link))
            {
                _transform = link.transform;
                Debug.Log($"[PositionBinder] Entity {e.Id}의 Transform에 바인딩되었습니다.");
            }
        }

        public void OnDetach(IWorld w, Entity e)
        {
            _transform = null;
            Debug.Log($"[PositionBinder] Entity {e.Id}의 바인딩이 해제되었습니다.");
        }

        public void Apply(IWorld w, Entity e, float alpha)
        {
            if (_transform == null || !w.HasComponent<Position>(e)) return;

            var pos = w.ReadComponent<Position>(e);
            _transform.position = new Vector3(pos.X, pos.Y, pos.Z);
        }
    }

    /// <summary>
    /// 이동 시스템 (FixedGroup).
    /// </summary>
    [FixedGroup]
    public sealed class MovementSystem : ISystem
    {
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

    /// <summary>
    /// 바인더 적용 시스템 (FrameViewGroup, 읽기 전용).
    /// </summary>
    [FrameViewGroup]
    public sealed class ViewUpdateSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            // 모든 엔티티의 바인더 적용
            foreach (var entity in w.Query<Entity>())
            {
                w.ApplyBinders(entity, alpha: 1f);
            }
        }
    }

    /// <summary>
    /// ViewBinder 샘플 - Binder 패턴을 보여줍니다.
    /// </summary>
    public sealed class ViewBinderSample : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int _spawnCount = 5;
        [SerializeField] private float _spawnRadius = 5f;
        [SerializeField] private GameObject? _prefab;

        private IWorld? _world;

        private void Start()
        {
            var kernel = KernelLocator.Current;
            if (kernel == null)
            {
                Debug.LogError("[ViewBinderSample] Kernel을 찾을 수 없습니다.");
                return;
            }

            _world = kernel.CreateWorld("ViewBinderWorld", setAsCurrent: true);
            _world.AddSystems([
                new MovementSystem(),
                new ViewUpdateSystem()
            ]);

            Debug.Log("[ViewBinderSample] World 및 시스템 등록 완료");

            SpawnEntitiesWithBinders();
        }

        private void SpawnEntitiesWithBinders()
        {
            if (_world == null) return;

            for (int i = 0; i < _spawnCount; i++)
            {
                // GameObject 생성
                GameObject go;
                if (_prefab != null)
                {
                    go = Instantiate(_prefab);
                }
                else
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                }

                go.name = $"Entity_{i}";

                // Entity 생성
                Entity entity;
                using (var cmd = _world.BeginWrite())
                {
                    entity = cmd.CreateEntity();
                    float angle = (i / (float)_spawnCount) * 360f * Mathf.Deg2Rad;
                    float x = Mathf.Cos(angle) * _spawnRadius;
                    float z = Mathf.Sin(angle) * _spawnRadius;
                    cmd.AddComponent(entity, new Position(x, 0, z));
                    cmd.AddComponent(entity, new Velocity(
                        Mathf.Cos(angle + Mathf.PI / 2) * 2f,
                        0,
                        Mathf.Sin(angle + Mathf.PI / 2) * 2f
                    ));
                }

                // EntityLink 생성
#if UNITY_EDITOR
                var link = go.CreateEntityLink(_world, entity);
#else
                var link = go.AddComponent<EntityLink>();
                link.Attach(_world, entity);
#endif

                // Binder 등록
                var binder = new PositionBinder();
                _world.AttachBinder(entity, binder);

                Debug.Log($"[ViewBinderSample] Entity {entity.Id} 생성 및 Binder 등록 완료");
            }
        }

        private void OnDestroy()
        {
            Debug.Log("[ViewBinderSample] 샘플 종료");
        }
    }
}
