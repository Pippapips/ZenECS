// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 01 - EcsDriver
// File: EcsDriverSample.cs
// Purpose: Basic Kernel initialization and Unity lifecycle integration using EcsDriver
// Key concepts:
//   • Automatic Kernel creation via EcsDriver
//   • Global access via KernelLocator
//   • World creation and system registration
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
    /// Position component - stores entity position.
    /// </summary>
    [ZenComponent]
    public readonly struct Position
    {
        public readonly float X, Y;
        public Position(float x, float y) { X = x; Y = y; }
        public override string ToString() => $"({X:0.##}, {Y:0.##})";
    }

    /// <summary>
    /// Velocity component - represents movement per second.
    /// </summary>
    [ZenComponent]
    public readonly struct Velocity
    {
        public readonly float X, Y;
        public Velocity(float x, float y) { X = x; Y = y; }
    }

    /// <summary>
    /// Movement system - calculates Position += Velocity * dt (FixedGroup).
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
    /// Position output system - reads and outputs Position (FrameViewGroup, read-only).
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
    /// EcsDriver sample - demonstrates Kernel initialization and World setup.
    /// </summary>
    public sealed class EcsDriverSample : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _createTestEntities = true;
        [SerializeField] private int _testEntityCount = 3;

        private void Awake()
        {
            // Kernel is automatically created by EcsDriver
            var kernel = KernelLocator.Current;
            if (kernel == null)
            {
                Debug.LogError("[EcsDriverSample] EcsDriver not found in scene! Please add EcsDriver component.");
                return;
            }

            Debug.Log("[EcsDriverSample] Kernel found. Creating World...");

            // Create World
            var world = kernel.CreateWorld(null, "GameWorld", setAsCurrent: true);
            Debug.Log($"[EcsDriverSample] World '{world.Name}' (ID: {world.Id}) created");

            // Register systems
            world.AddSystems(new List<ISystem>
            {
                new MovementSystem(),
                new PrintPositionSystem()
            }.AsReadOnly());
            Debug.Log("[EcsDriverSample] Systems registered");

            // Create test entities
            if (_createTestEntities)
            {
                CreateTestEntities(world);
            }

            Debug.Log("[EcsDriverSample] Initialization complete!");
        }

        /// <summary>
        /// Creates test entities with Position and Velocity components.
        /// </summary>
        /// <param name="world">The world to create entities in.</param>
        private void CreateTestEntities(IWorld world)
        {
            using var cmd = world.BeginWrite();
            for (int i = 0; i < _testEntityCount; i++)
            {
                var entity = cmd.CreateEntity();
                cmd.AddComponent(entity, new Position(i * 2f, 0));
                cmd.AddComponent(entity, new Velocity(1f + i * 0.5f, 0));
                Debug.Log($"[EcsDriverSample] Test entity {entity.Id} created: pos=({i * 2f}, 0), vel=({1f + i * 0.5f}, 0)");
            }
        }

        private void OnDestroy()
        {
            Debug.Log("[EcsDriverSample] Sample terminated.");
        }
    }
}
