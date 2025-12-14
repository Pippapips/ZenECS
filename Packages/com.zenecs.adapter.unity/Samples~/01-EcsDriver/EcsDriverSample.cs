// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 01 - EcsDriver
// File: EcsDriverSample.cs
// Purpose: EcsDriver를 사용한 기본 Kernel 초기화 및 Unity 생명주기 연동 예제
// Key concepts:
//   • EcsDriver를 통한 Kernel 자동 생성
//   • KernelLocator를 통한 전역 접근
//   • World 생성 및 시스템 등록
//
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Core;
using ZenECS.Core.Systems;

namespace ZenEcsAdapterUnitySamples.EcsDriver
{
    /// <summary>
    /// Position 컴포넌트 - 엔티티의 위치를 저장합니다.
    /// </summary>
    [ZenComponent]
    public readonly struct Position
    {
        public readonly float X, Y;
        public Position(float x, float y) { X = x; Y = y; }
        public override string ToString() => $"({X:0.##}, {Y:0.##})";
    }

    /// <summary>
    /// Velocity 컴포넌트 - 초당 이동량을 나타냅니다.
    /// </summary>
    [ZenComponent]
    public readonly struct Velocity
    {
        public readonly float X, Y;
        public Velocity(float x, float y) { X = x; Y = y; }
    }

    /// <summary>
    /// 이동 시스템 - Position += Velocity * dt를 계산합니다 (FixedGroup).
    /// </summary>
    [FixedGroup]
    [ZenSystemWatch(typeof(Position), typeof(Velocity))]
    public sealed class MovementSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            using var cmd = w.BeginWrite();
            foreach (var (e, pos, vel) in w.Query<Position, Velocity>())
            {
                cmd.ReplaceComponent(e, new Position(pos.X + vel.X * dt, pos.Y + vel.Y * dt));
            }
        }
    }

    /// <summary>
    /// 위치 출력 시스템 - Position을 읽어서 출력합니다 (FrameViewGroup, 읽기 전용).
    /// </summary>
    [FrameViewGroup]
    [ZenSystemWatch(typeof(Position))]
    public sealed class PrintPositionSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            foreach (var (e, pos) in w.Query<Position>())
            {
                Debug.Log($"Entity {e.Id}: pos={pos}");
            }
        }
    }

    /// <summary>
    /// EcsDriver 샘플 - Kernel 초기화 및 World 설정을 보여줍니다.
    /// </summary>
    public sealed class EcsDriverSample : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _createTestEntities = true;
        [SerializeField] private int _testEntityCount = 3;

        private void Awake()
        {
            // Kernel은 EcsDriver가 자동으로 생성합니다
            var kernel = KernelLocator.Current;
            if (kernel == null)
            {
                Debug.LogError("[EcsDriverSample] EcsDriver가 씬에 없습니다! EcsDriver 컴포넌트를 추가해주세요.");
                return;
            }

            Debug.Log("[EcsDriverSample] Kernel을 찾았습니다. World를 생성합니다...");

            // World 생성
            var world = kernel.CreateWorld(null, "GameWorld", setAsCurrent: true);
            Debug.Log($"[EcsDriverSample] World '{world.Name}' (ID: {world.Id}) 생성 완료");

            // 시스템 등록
            world.AddSystems(new List<ISystem>
            {
                new MovementSystem(),
                new PrintPositionSystem()
            }.AsReadOnly());
            Debug.Log("[EcsDriverSample] 시스템 등록 완료");

            // 테스트 엔티티 생성
            if (_createTestEntities)
            {
                CreateTestEntities(world);
            }

            Debug.Log("[EcsDriverSample] 초기화 완료!");
        }

        private void CreateTestEntities(IWorld world)
        {
            using var cmd = world.BeginWrite();
            for (int i = 0; i < _testEntityCount; i++)
            {
                var entity = cmd.CreateEntity();
                cmd.AddComponent(entity, new Position(i * 2f, 0));
                cmd.AddComponent(entity, new Velocity(1f + i * 0.5f, 0));
                Debug.Log($"[EcsDriverSample] 테스트 엔티티 {entity.Id} 생성: pos=({i * 2f}, 0), vel=({1f + i * 0.5f}, 0)");
            }
        }

        private void OnDestroy()
        {
            Debug.Log("[EcsDriverSample] 샘플이 종료되었습니다.");
        }
    }
}
