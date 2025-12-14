// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 02 - EntityLink
// File: EntityLinkSample.cs
// Purpose: GameObject와 Entity를 연결하는 EntityLink 사용 예제
// Key concepts:
//   • EntityLink를 통한 GameObject ↔ Entity 연결
//   • EntityViewRegistry를 통한 뷰 관리
//   • 링크 생명주기 관리
//
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Linking;
using ZenECS.Core;
using ZenECS.Core.Systems;

namespace ZenEcsAdapterUnitySamples.EntityLink
{
    /// <summary>
    /// Position 컴포넌트 - 3D 위치를 저장합니다.
    /// </summary>
    [ZenComponent]
    public readonly struct Position
    {
        public readonly float X, Y, Z;
        public Position(float x, float y, float z) { X = x; Y = y; Z = z; }
        public Vector3 ToVector3() => new Vector3(X, Y, Z);
    }

    /// <summary>
    /// Position을 Transform에 반영하는 시스템 (FrameViewGroup, 읽기 전용).
    /// </summary>
    [FrameViewGroup]
    [ZenSystemWatch(typeof(Position))]
    public sealed class PositionViewSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            var registry = EntityViewRegistry.For(w);
            foreach (var (entity, pos) in w.Query<Position>())
            {
                if (registry.TryGet(entity, out var link))
                {
                    if (link) link.transform.position = pos.ToVector3();
                }
            }
        }
    }

    /// <summary>
    /// EntityLink 샘플 - GameObject와 Entity를 연결하는 방법을 보여줍니다.
    /// </summary>
    public sealed class EntityLinkSample : MonoBehaviour
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
                Debug.LogError("[EntityLinkSample] Kernel을 찾을 수 없습니다. EcsDriver를 추가해주세요.");
                return;
            }

            _world = kernel.CreateWorld(null, "EntityLinkWorld", setAsCurrent: true);
            _world.AddSystems(new List<ISystem> { new PositionViewSystem() }.AsReadOnly());

            Debug.Log("[EntityLinkSample] World 생성 완료. 엔티티를 생성합니다...");

            SpawnEntities();
        }

        private void SpawnEntities()
        {
            if (_world == null) return;

            var registry = EntityViewRegistry.For(_world);

            for (int i = 0; i < _spawnCount; i++)
            {
                // Entity 생성
                Entity entity;
                using (var cmd = _world.BeginWrite())
                {
                    entity = cmd.CreateEntity();
                    float angle = (i / (float)_spawnCount) * 360f * Mathf.Deg2Rad;
                    float x = Mathf.Cos(angle) * _spawnRadius;
                    float z = Mathf.Sin(angle) * _spawnRadius;
                    cmd.AddComponent(entity, new Position(x, 0, z));
                }

                // GameObject 생성 및 링크
                GameObject go;
                if (_prefab != null)
                {
                    go = Instantiate(_prefab);
                }
                else
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                }

                go.name = $"Entity_{entity.Id}";

#if UNITY_EDITOR
                // 에디터에서는 확장 메서드 사용
                var link = go.CreateEntityLink(_world, entity);
#else
                // 런타임에서는 직접 컴포넌트 추가
                var link = go.AddComponent<EntityLink>();
                link.Attach(_world, entity);
#endif

                Debug.Log($"[EntityLinkSample] Entity {entity.Id}가 {go.name}에 연결되었습니다.");

                // 레지스트리 확인
                if (registry.TryGet(entity, out var registeredLink))
                {
                    Debug.Log($"[EntityLinkSample] 레지스트리 확인: Entity {entity.Id} → {registeredLink?.gameObject.name}");
                }
            }

            // 모든 뷰 나열
            Debug.Log($"[EntityLinkSample] 총 {_spawnCount}개의 EntityLink가 생성되었습니다.");
            int count = 0;
            foreach (var (e, view) in registry.EnumerateViews())
            {
                count++;
            }
            Debug.Log($"[EntityLinkSample] 레지스트리에 등록된 뷰: {count}개");
        }

        private void OnDestroy()
        {
            if (_world != null)
            {
                Debug.Log("[EntityLinkSample] 샘플 종료. World 정리 중...");
            }
        }
    }
}
