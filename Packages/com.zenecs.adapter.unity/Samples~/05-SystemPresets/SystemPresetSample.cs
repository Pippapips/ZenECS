// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 05 - System Presets
// File: SystemPresetSample.cs
// Purpose: SystemPreset을 사용한 시스템 설정 및 등록 예제
// Key concepts:
//   • ScriptableObject 기반 시스템 프리셋
//   • SystemPresetResolver를 통한 시스템 자동 등록
//   • 타입 안전한 시스템 참조
//
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.SystemPresets;
using ZenECS.Core;
using ZenECS.Core.Systems;

namespace ZenEcsAdapterUnitySamples.SystemPresets
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
    /// 렌더링 시스템 (FrameViewGroup, 읽기 전용).
    /// </summary>
    [FrameViewGroup]
    public sealed class RenderSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            foreach (var (e, pos) in w.Query<Position>())
            {
                // 실제로는 Transform에 반영하거나 렌더링합니다
                // 여기서는 로그만 출력
                if (w.FrameCount % 60 == 0) // 1초마다 출력
                {
                    Debug.Log($"[RenderSystem] Entity {e.Id}: pos=({pos.X:0.##}, {pos.Y:0.##}, {pos.Z:0.##})");
                }
            }
        }
    }

    /// <summary>
    /// SystemPreset 샘플 - ScriptableObject 기반 시스템 설정을 보여줍니다.
    /// </summary>
    public sealed class SystemPresetSample : MonoBehaviour
    {
        [Header("System Preset")]
        [SerializeField] private SystemPreset? _preset;

        [Header("Manual Systems (Preset가 없을 때 사용)")]
        [SerializeField] private bool _useManualSystems = false;

        private IWorld? _world;

        private void Start()
        {
            var kernel = KernelLocator.Current;
            if (kernel == null)
            {
                Debug.LogError("[SystemPresetSample] Kernel을 찾을 수 없습니다.");
                return;
            }

            _world = kernel.CreateWorld("SystemPresetWorld", setAsCurrent: true);
            Debug.Log("[SystemPresetSample] World 생성 완료");

            if (_preset != null && !_useManualSystems)
            {
                RegisterSystemsFromPreset();
            }
            else if (_useManualSystems)
            {
                RegisterSystemsManually();
            }
            else
            {
                Debug.LogWarning("[SystemPresetSample] SystemPreset이 할당되지 않았습니다. Inspector에서 할당하거나 Manual Systems를 사용하세요.");
            }

            CreateTestEntities();
        }

        private void RegisterSystemsFromPreset()
        {
            if (_world == null || _preset == null) return;

            var resolver = new SystemPresetResolver();
            var systems = resolver.Resolve(_preset);

            _world.AddSystems(systems);
            Debug.Log($"[SystemPresetSample] SystemPreset '{_preset.name}'에서 {systems.Count}개의 시스템을 등록했습니다.");

            foreach (var system in systems)
            {
                Debug.Log($"[SystemPresetSample]   - {system.GetType().Name}");
            }
        }

        private void RegisterSystemsManually()
        {
            if (_world == null) return;

            _world.AddSystems([
                new MovementSystem(),
                new RenderSystem()
            ]);
            Debug.Log("[SystemPresetSample] 시스템을 수동으로 등록했습니다.");
        }

        private void CreateTestEntities()
        {
            if (_world == null) return;

            using var cmd = _world.BeginWrite();
            for (int i = 0; i < 3; i++)
            {
                var entity = cmd.CreateEntity();
                cmd.AddComponent(entity, new Position(i * 2f, 0, 0));
                cmd.AddComponent(entity, new Velocity(1f, 0, 0));
                Debug.Log($"[SystemPresetSample] 테스트 엔티티 {entity.Id} 생성");
            }
        }

        private void OnDestroy()
        {
            Debug.Log("[SystemPresetSample] 샘플 종료");
        }
    }
}
