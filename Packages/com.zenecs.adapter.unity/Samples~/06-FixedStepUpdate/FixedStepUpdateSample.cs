// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 06 - FixedStep vs Update
// File: FixedStepUpdateSample.cs
// Purpose: Example comparing Unity Update/FixedUpdate with ECS BeginFrame/FixedStep/LateFrame
// Key concepts:
//   • BeginFrame (variable timestep) vs FixedStep (fixed timestep)
//   • FixedGroup vs FrameViewGroup execution timing
//   • EcsDriver automatic integration
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

namespace ZenEcsAdapterUnitySamples.FixedStepUpdate
{
    /// <summary>
    /// Position component.
    /// </summary>
    [ZenComponent]
    public readonly struct Position
    {
        public readonly float X, Y, Z;
        public Position(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    /// <summary>
    /// Velocity component.
    /// </summary>
    [ZenComponent]
    public readonly struct Velocity
    {
        public readonly float X, Y, Z;
        public Velocity(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    /// <summary>
    /// Frame timing information component.
    /// </summary>
    [ZenComponent]
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
    /// Simulation system (FixedGroup) - runs in FixedStep.
    /// </summary>
    [FixedGroup]
    [ZenSystemWatch(typeof(Position), typeof(Velocity), typeof(FrameTiming))]
    public sealed class SimulationSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            // dt is fixedDeltaTime (fixed timestep, e.g., 0.02f for 50Hz)
            using var cmd = w.BeginWrite();
            foreach (var (e, pos, vel) in w.Query<Position, Velocity>())
            {
                cmd.ReplaceComponent(e, new Position(
                    pos.X + vel.X * dt,
                    pos.Y + vel.Y * dt,
                    pos.Z + vel.Z * dt
                ));
            }

            // Update frame timing
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
    /// Presentation system (FrameViewGroup) - runs in LateFrame.
    /// </summary>
    [FrameViewGroup]
    [ZenSystemWatch(typeof(FrameTiming))]
    public sealed class PresentationSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            // dt is deltaTime (variable timestep)
            // Read-only - does not modify data

            // Update frame timing
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
    /// FixedStepUpdate sample - demonstrates frame structure.
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
                Debug.LogError("[FixedStepUpdateSample] Kernel not found.");
                return;
            }

            _world = kernel.CreateWorld(null, "FixedStepWorld", setAsCurrent: true);
            _world.AddSystems(new List<ISystem>
            {
                new SimulationSystem(),
                new PresentationSystem()
            }.AsReadOnly());

            Debug.Log("[FixedStepUpdateSample] World and systems registered");
            Debug.Log("[FixedStepUpdateSample] EcsDriver automatically converts Unity lifecycle to ECS frame structure:");
            Debug.Log("  - Update() → BeginFrame(deltaTime)");
            Debug.Log("  - FixedUpdate() → FixedStep(fixedDeltaTime)");
            Debug.Log("  - LateUpdate() → LateFrame()");

            CreateEntities();
        }

        /// <summary>
        /// Creates test entities with Position and Velocity components, plus a timing tracking entity.
        /// </summary>
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

            // Timing tracking entity
            _timingEntity = cmd.CreateEntity();
            cmd.AddComponent(_timingEntity, new FrameTiming(0, 0, 0, 0));

            Debug.Log($"[FixedStepUpdateSample] {_entityCount} entities created");
        }

        private void OnGUI()
        {
            if (_world == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 400, 300));
            GUILayout.Label("FixedStep vs Update Sample", GUI.skin.box);
            GUILayout.Space(10);

            // Unity frame information
            GUILayout.Label($"Unity Time.deltaTime: {Time.deltaTime:F4}s");
            GUILayout.Label($"Unity Time.fixedDeltaTime: {Time.fixedDeltaTime:F4}s");
            GUILayout.Label($"Unity Time.fixedTime: {Time.fixedTime:F2}s");
            GUILayout.Space(10);

            // ECS frame information
            if (_world.HasComponent<FrameTiming>(_timingEntity))
            {
                var timing = _world.ReadComponent<FrameTiming>(_timingEntity);
                GUILayout.Label($"ECS FixedStep Count: {timing.FixedStepCount}");
                GUILayout.Label($"ECS LateFrame Count: {timing.LateFrameCount}");
                GUILayout.Label($"Last FixedStep dt: {timing.LastFixedDelta:F4}s");
                GUILayout.Label($"Last LateFrame dt: {timing.LastLateDelta:F4}s");
            }

            GUILayout.Space(10);
            GUILayout.Label("FixedGroup systems run in FixedStep (fixed timestep)");
            GUILayout.Label("FrameViewGroup systems run in LateFrame (variable timestep)");

            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            Debug.Log("[FixedStepUpdateSample] Sample terminated");
        }
    }
}
