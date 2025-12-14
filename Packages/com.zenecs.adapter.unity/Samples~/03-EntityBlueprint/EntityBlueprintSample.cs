// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 03 - EntityBlueprint
// File: EntityBlueprintSample.cs
// Purpose: EntityBlueprint를 사용한 엔티티 스폰 예제
// Key concepts:
//   • ScriptableObject 기반 엔티티 블루프린트
//   • 컴포넌트 스냅샷 저장 및 적용
//   • Context Assets와 Binders 설정
//
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Blueprints;
using ZenECS.Core;

namespace ZenEcsAdapterUnitySamples.EntityBlueprint
{
    /// <summary>
    /// Health 컴포넌트 - 엔티티의 체력을 저장합니다.
    /// </summary>
    public readonly struct Health
    {
        public readonly int Max;
        public readonly int Current;
        public Health(int max, int current) { Max = max; Current = current; }
        public Health WithCurrent(int current) => new Health(Max, current);
    }

    /// <summary>
    /// Position 컴포넌트 - 엔티티의 위치를 저장합니다.
    /// </summary>
    public readonly struct Position
    {
        public readonly float X, Y, Z;
        public Position(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    /// <summary>
    /// EntityBlueprint 샘플 - ScriptableObject 기반 엔티티 스폰을 보여줍니다.
    /// </summary>
    public sealed class EntityBlueprintSample : MonoBehaviour
    {
        [Header("Blueprint")]
        [SerializeField] private EntityBlueprint? _blueprint;

        [Header("Spawn Settings")]
        [SerializeField] private int _spawnCount = 3;
        [SerializeField] private float _spawnInterval = 1f;

        private IWorld? _world;
        private float _spawnTimer;

        private void Start()
        {
            var kernel = KernelLocator.Current;
            if (kernel == null)
            {
                Debug.LogError("[EntityBlueprintSample] Kernel을 찾을 수 없습니다.");
                return;
            }

            _world = kernel.CreateWorld("BlueprintWorld", setAsCurrent: true);
            Debug.Log("[EntityBlueprintSample] World 생성 완료");

            if (_blueprint == null)
            {
                Debug.LogWarning("[EntityBlueprintSample] Blueprint가 할당되지 않았습니다. Inspector에서 할당해주세요.");
            }
        }

        private void Update()
        {
            if (_world == null || _blueprint == null) return;

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= _spawnInterval)
            {
                _spawnTimer = 0f;
                SpawnFromBlueprint();
            }
        }

        private void SpawnFromBlueprint()
        {
            if (_world == null || _blueprint == null) return;

            // Blueprint로 엔티티 스폰
            _blueprint.Spawn(
                _world,
                ZenEcsUnityBridge.SharedContextResolver,
                onCreated: entity =>
                {
                    Debug.Log($"[EntityBlueprintSample] Entity {entity.Id}가 Blueprint에서 스폰되었습니다.");

                    // 스폰된 엔티티의 컴포넌트 확인
                    if (_world.HasComponent<Health>(entity))
                    {
                        var health = _world.ReadComponent<Health>(entity);
                        Debug.Log($"[EntityBlueprintSample] Entity {entity.Id} Health: {health.Current}/{health.Max}");
                    }

                    if (_world.HasComponent<Position>(entity))
                    {
                        var pos = _world.ReadComponent<Position>(entity);
                        Debug.Log($"[EntityBlueprintSample] Entity {entity.Id} Position: ({pos.X}, {pos.Y}, {pos.Z})");
                    }
                }
            );
        }

        private void OnGUI()
        {
            if (_blueprint == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Label("EntityBlueprint Sample");
            GUILayout.Label($"Blueprint: {_blueprint.name}");
            if (_world != null)
            {
                GUILayout.Label($"World: {_world.Name}");
            }
            GUILayout.EndArea();
        }
    }
}
