// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 08 - FixedStep vs Update
// File: FixedStepUpdateSample.cs
// Purpose: Unity Update/FixedUpdate와 ECS BeginFrame/FixedStep/LateFrame 비교 예제
// Key concepts:
//   • BeginFrame (가변 타임스텝) vs FixedStep (고정 타임스텝)
//   • FixedGroup vs FrameViewGroup 실행 시점
//   • EcsDriver 자동 연동
//
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Core;
using ZenECS.Core.Systems;

namespace ZenEcsAdapterUnitySamples.FixedStepUpdate
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
    /// 프레임 타이밍 정보 컴포넌트.
    /// </summary>
    public readonly struct FrameTiming
    {
        public readonly long FixedStepCount;
        public readonly long LateFrameCount;
        public readonly float LastFixedDelta;
        public readonly float LastLateDelta;

        public FrameTiming(long fixedStep, long lateFrame, float fixedDelta, float lateDelta)
        {
            FixedStepCount = fixedStep;
            LateFrameCount = lateFrame;
            LastFixedDelta = fixedDelta;
            LastLateDelta = lateDelta;
        }
    }

    /// <summary>
    /// 시뮬레이션 시스템 (FixedGroup) - FixedStep에서 실행됩니다.
    /// </summary>
    [FixedGroup]
    public sealed class SimulationSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            // dt는 fixedDeltaTime (고정 타임스텝, 예: 0.02f for 50Hz)
            using var cmd = w.BeginWrite();
            foreach (var (e, pos, vel) in w.Query<Position, Velocity>())
            {
                cmd.ReplaceComponent(e, new Position(
                    pos.X + vel.X * dt,
                    pos.Y + vel.Y * dt,
                    pos.Z + vel.Z * dt
                ));
            }

            // 프레임 타이밍 업데이트
            foreach (var (e, timing) in w.Query<FrameTiming>())
            {
                var newTiming = new FrameTiming(
                    timing.FixedStepCount + 1,
                    timing.LateFrameCount,
                    dt,
                    timing.LastLateDelta
                );
                cmd.ReplaceComponent(e, newTiming);
            }
        }
    }

    /// <summary>
    /// 프레젠테이션 시스템 (FrameViewGroup) - LateFrame에서 실행됩니다.
    /// </summary>
    [FrameViewGroup]
    public sealed class PresentationSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            // dt는 deltaTime (가변 타임스텝)
            // 읽기 전용 - 데이터를 수정하지 않음

            // 프레임 타이밍 업데이트
            using var cmd = w.BeginWrite();
            foreach (var (e, timing) in w.Query<FrameTiming>())
            {
                var newTiming = new FrameTiming(
                    timing.FixedStepCount,
                    timing.LateFrameCount + 1,
                    timing.LastFixedDelta,
                    dt
                );
                cmd.ReplaceComponent(e, newTiming);
            }
        }
    }

    /// <summary>
    /// FixedStepUpdate 샘플 - 프레임 구조를 보여줍니다.
    /// </summary>
    public sealed class FixedStepUpdateSample : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int _entityCount = 3;

        private IWorld? _world;
        private Entity _timingEntity;

        private void Start()
        {
            var kernel = KernelLocator.Current;
            if (kernel == null)
            {
                Debug.LogError("[FixedStepUpdateSample] Kernel을 찾을 수 없습니다.");
                return;
            }

            _world = kernel.CreateWorld("FixedStepWorld", setAsCurrent: true);
            _world.AddSystems([
                new SimulationSystem(),
                new PresentationSystem()
            ]);

            Debug.Log("[FixedStepUpdateSample] World 및 시스템 등록 완료");
            Debug.Log("[FixedStepUpdateSample] EcsDriver가 자동으로 Unity 생명주기를 ECS 프레임 구조로 변환합니다:");
            Debug.Log("  - Update() → BeginFrame(deltaTime)");
            Debug.Log("  - FixedUpdate() → FixedStep(fixedDeltaTime)");
            Debug.Log("  - LateUpdate() → LateFrame()");

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

            // 타이밍 추적 엔티티
            _timingEntity = cmd.CreateEntity();
            cmd.AddComponent(_timingEntity, new FrameTiming(0, 0, 0, 0));

            Debug.Log($"[FixedStepUpdateSample] {_entityCount}개의 엔티티 생성 완료");
        }

        private void OnGUI()
        {
            if (_world == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 400, 300));
            GUILayout.Label("FixedStep vs Update Sample", GUI.skin.box);
            GUILayout.Space(10);

            // Unity 프레임 정보
            GUILayout.Label($"Unity Time.deltaTime: {Time.deltaTime:F4}s");
            GUILayout.Label($"Unity Time.fixedDeltaTime: {Time.fixedDeltaTime:F4}s");
            GUILayout.Label($"Unity Time.fixedTime: {Time.fixedTime:F2}s");
            GUILayout.Space(10);

            // ECS 프레임 정보
            if (_world.HasComponent<FrameTiming>(_timingEntity))
            {
                var timing = _world.ReadComponent<FrameTiming>(_timingEntity);
                GUILayout.Label($"ECS FixedStep Count: {timing.FixedStepCount}");
                GUILayout.Label($"ECS LateFrame Count: {timing.LateFrameCount}");
                GUILayout.Label($"Last FixedStep dt: {timing.LastFixedDelta:F4}s");
                GUILayout.Label($"Last LateFrame dt: {timing.LastLateDelta:F4}s");
            }

            GUILayout.Space(10);
            GUILayout.Label("FixedGroup 시스템은 FixedStep에서 실행됩니다 (고정 타임스텝)");
            GUILayout.Label("FrameViewGroup 시스템은 LateFrame에서 실행됩니다 (가변 타임스텝)");

            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            Debug.Log("[FixedStepUpdateSample] 샘플 종료");
        }
    }
}
