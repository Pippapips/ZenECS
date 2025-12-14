// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 07 - Input → Intent
// File: InputIntentSample.cs
// Purpose: Unity Input을 ECS Intent 컴포넌트로 변환하는 패턴 예제
// Key concepts:
//   • Intent 컴포넌트로 입력 의도 표현
//   • Input 수집 (FrameViewGroup)
//   • Intent 처리 (FixedGroup)
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
using ZenECS.Core.Systems;

namespace ZenEcsAdapterUnitySamples.InputIntent
{
    /// <summary>
    /// 이동 Intent - 플레이어의 이동 의도를 나타냅니다.
    /// </summary>
    public readonly struct MoveIntent
    {
        public readonly float X, Y, Z;
        public MoveIntent(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    /// <summary>
    /// Position 컴포넌트.
    /// </summary>
    public readonly struct Position
    {
        public readonly float X, Y, Z;
        public Position(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    /// <summary>
    /// Input 수집 시스템 (FrameViewGroup) - Unity Input을 Intent로 변환합니다.
    /// </summary>
    [FrameViewGroup]
    public sealed class InputCollectionSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            // 플레이어 엔티티 찾기 (Player 태그 또는 특정 컴포넌트로 식별)
            foreach (var entity in w.Query<Entity>())
            {
                // 간단한 예제: Position이 있는 모든 엔티티를 플레이어로 간주
                if (!w.HasComponent<Position>(entity)) continue;

                // Unity Input 수집 (Legacy Input 사용)
                float x = Input.GetAxis("Horizontal");
                float z = Input.GetAxis("Vertical");

                // Intent 컴포넌트 추가/업데이트
                using var cmd = w.BeginWrite();
                if (w.HasComponent<MoveIntent>(entity))
                {
                    cmd.ReplaceComponent(entity, new MoveIntent(x, 0, z));
                }
                else
                {
                    cmd.AddComponent(entity, new MoveIntent(x, 0, z));
                }
            }
        }
    }

    /// <summary>
    /// Intent 처리 시스템 (FixedGroup) - Intent를 읽어 게임 로직을 실행합니다.
    /// </summary>
    [FixedGroup]
    public sealed class MovementIntentSystem : ISystem
    {
        private const float MoveSpeed = 5f;

        public void Run(IWorld w, float dt)
        {
            using var cmd = w.BeginWrite();
            foreach (var (e, intent, pos) in w.Query<MoveIntent, Position>())
            {
                // Intent를 기반으로 Position 업데이트
                var newPos = new Position(
                    pos.X + intent.X * dt * MoveSpeed,
                    pos.Y,
                    pos.Z + intent.Z * dt * MoveSpeed
                );
                cmd.ReplaceComponent(e, newPos);

                // Intent 소비 (제거) - 한 프레임에 한 번만 처리
                cmd.RemoveComponent<MoveIntent>(e);
            }
        }
    }

    /// <summary>
    /// Position을 Transform에 반영하는 시스템 (FrameViewGroup, 읽기 전용).
    /// </summary>
    [FrameViewGroup]
    public sealed class PositionViewSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            var registry = EntityViewRegistry.For(w);
            foreach (var (entity, pos) in w.Query<Position>())
            {
                if (registry.TryGetView(entity, out var link))
                {
                    link.transform.position = new Vector3(pos.X, pos.Y, pos.Z);
                }
            }
        }
    }

    /// <summary>
    /// InputIntent 샘플 - Input → Intent 패턴을 보여줍니다.
    /// </summary>
    public sealed class InputIntentSample : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private GameObject? _playerPrefab;

        private IWorld? _world;
        private Entity _playerEntity;

        private void Start()
        {
            var kernel = KernelLocator.Current;
            if (kernel == null)
            {
                Debug.LogError("[InputIntentSample] Kernel을 찾을 수 없습니다.");
                return;
            }

            _world = kernel.CreateWorld("InputIntentWorld", setAsCurrent: true);
            _world.AddSystems([
                new InputCollectionSystem(),
                new MovementIntentSystem(),
                new PositionViewSystem()
            ]);

            Debug.Log("[InputIntentSample] World 및 시스템 등록 완료");

            CreatePlayer();
        }

        private void CreatePlayer()
        {
            if (_world == null) return;

            // GameObject 생성
            GameObject playerGo;
            if (_playerPrefab != null)
            {
                playerGo = Instantiate(_playerPrefab);
            }
            else
            {
                playerGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                playerGo.name = "Player";
            }

            // Entity 생성
            using (var cmd = _world.BeginWrite())
            {
                _playerEntity = cmd.CreateEntity();
                cmd.AddComponent(_playerEntity, new Position(0, 0, 0));
            }

            // EntityLink 생성
#if UNITY_EDITOR
            var link = playerGo.CreateEntityLink(_world, _playerEntity);
#else
            var link = playerGo.AddComponent<EntityLink>();
            link.Attach(_world, _playerEntity);
#endif

            Debug.Log($"[InputIntentSample] 플레이어 Entity {_playerEntity.Id} 생성 완료");
            Debug.Log("[InputIntentSample] WASD 또는 화살표 키로 이동하세요!");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Label("Input → Intent Sample");
            GUILayout.Label("WASD 또는 화살표 키로 이동");
            if (_world != null && _world.HasComponent<Position>(_playerEntity))
            {
                var pos = _world.ReadComponent<Position>(_playerEntity);
                GUILayout.Label($"Position: ({pos.X:0.##}, {pos.Y:0.##}, {pos.Z:0.##})");
            }
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            Debug.Log("[InputIntentSample] 샘플 종료");
        }
    }
}
