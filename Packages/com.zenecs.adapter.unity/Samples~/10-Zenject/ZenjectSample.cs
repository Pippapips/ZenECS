// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 10 - Zenject Integration
// File: ZenjectSample.cs
// Purpose: Zenject를 사용한 의존성 주입 예제
// Key concepts:
//   • ProjectInstaller를 통한 Kernel 및 Resolver 바인딩
//   • Zenject를 통한 시스템 의존성 주입
//   • 조건부 컴파일 (ZENECS_ZENJECT)
//
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Binding.Contexts;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Systems;

#if ZENECS_ZENJECT
using Zenject;
#endif

namespace ZenEcsAdapterUnitySamples.Zenject
{
    /// <summary>
    /// 게임 설정 Context (DI로 주입됨).
    /// </summary>
    public interface IGameConfig : IContext
    {
        float GameSpeed { get; }
    }

    public class GameConfig : IGameConfig
    {
        public float GameSpeed { get; set; } = 2f;
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
    /// Velocity 컴포넌트.
    /// </summary>
    public readonly struct Velocity
    {
        public readonly float X, Y, Z;
        public Velocity(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

#if ZENECS_ZENJECT
    /// <summary>
    /// 이동 시스템 - Zenject를 통해 IGameConfig를 주입받습니다.
    /// </summary>
    [FixedGroup]
    public sealed class MovementSystem : ISystem
    {
        private readonly IGameConfig _config;

        // Zenject가 자동으로 IGameConfig를 주입
        public MovementSystem(IGameConfig config)
        {
            _config = config;
            Debug.Log($"[MovementSystem] 생성됨 - GameSpeed={config.GameSpeed} (Zenject 주입)");
        }

        public void Run(IWorld w, float dt)
        {
            using var cmd = w.BeginWrite();
            foreach (var (e, pos, vel) in w.Query<Position, Velocity>())
            {
                // GameSpeed를 사용하여 이동 속도 조절
                cmd.ReplaceComponent(e, new Position(
                    pos.X + vel.X * dt * _config.GameSpeed,
                    pos.Y + vel.Y * dt * _config.GameSpeed,
                    pos.Z + vel.Z * dt * _config.GameSpeed
                ));
            }
        }
    }
#else
    /// <summary>
    /// 이동 시스템 - Zenject 없이 동작 (수동 설정 필요).
    /// </summary>
    [FixedGroup]
    public sealed class MovementSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            const float defaultSpeed = 1f;
            using var cmd = w.BeginWrite();
            foreach (var (e, pos, vel) in w.Query<Position, Velocity>())
            {
                cmd.ReplaceComponent(e, new Position(
                    pos.X + vel.X * dt * defaultSpeed,
                    pos.Y + vel.Y * dt * defaultSpeed,
                    pos.Z + vel.Z * dt * defaultSpeed
                ));
            }
        }
    }
#endif

    /// <summary>
    /// Zenject 샘플 - 의존성 주입을 보여줍니다.
    /// </summary>
    public sealed class ZenjectSample : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int _entityCount = 3;

        private IWorld? _world;

        private void Start()
        {
            var kernel = KernelLocator.Current;
            if (kernel == null)
            {
                Debug.LogError("[ZenjectSample] Kernel을 찾을 수 없습니다. ProjectInstaller가 씬에 있는지 확인하세요.");
                return;
            }

            _world = kernel.CreateWorld("ZenjectWorld", setAsCurrent: true);

#if ZENECS_ZENJECT
            // Zenject 모드: 시스템은 SystemPresetResolver를 통해 자동으로 주입됨
            // 여기서는 수동으로 생성 (실제로는 SystemPreset 사용)
            var config = new GameConfig { GameSpeed = 2.5f };
            var system = new MovementSystem(config);
            _world.AddSystems([system]);
            Debug.Log("[ZenjectSample] Zenject 모드: 시스템이 DI로 생성되었습니다.");
#else
            // 비Zenject 모드: 시스템을 직접 생성
            _world.AddSystems([new MovementSystem()]);
            Debug.Log("[ZenjectSample] 비Zenject 모드: 시스템을 직접 생성했습니다.");
#endif

            CreateEntities();
        }

        private void CreateEntities()
        {
            if (_world == null) return;

            using var cmd = _world.BeginWrite();
            for (int i = 0; i < _entityCount; i++)
            {
                var entity = cmd.CreateEntity();
                cmd.AddComponent(entity, new Position(i * 2f, 0, 0));
                cmd.AddComponent(entity, new Velocity(1f, 0, 0));
            }

            Debug.Log($"[ZenjectSample] {_entityCount}개의 엔티티 생성 완료");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 350, 200));
            GUILayout.Label("Zenject Integration Sample", GUI.skin.box);
            GUILayout.Space(10);

#if ZENECS_ZENJECT
            GUILayout.Label("모드: Zenject (의존성 주입 활성화)");
            GUILayout.Label("MovementSystem은 IGameConfig를 DI로 주입받습니다.");
#else
            GUILayout.Label("모드: 비Zenject (수동 설정)");
            GUILayout.Label("ZENECS_ZENJECT define을 추가하면 Zenject 모드가 활성화됩니다.");
#endif

            GUILayout.Space(10);
            if (_world != null)
            {
                GUILayout.Label($"World: {_world.Name}");
                GUILayout.Label($"Kernel: {KernelLocator.Current != null}");
            }

            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            Debug.Log("[ZenjectSample] 샘플 종료");
        }
    }
}
