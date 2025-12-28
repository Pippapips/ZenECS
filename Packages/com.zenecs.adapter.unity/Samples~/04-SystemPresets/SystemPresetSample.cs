// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 04 - System Presets
// File: SystemPresetSample.cs
// Purpose: Example of system setup and registration using SystemPreset
// Key concepts:
//   • ScriptableObject-based system preset
//   • Automatic system registration via SystemPresetResolver
//   • Type-safe system references
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.SystemPresets;
using ZenECS.Core;
using ZenECS.Core.Systems;

namespace ZenEcsAdapterUnitySamples.SystemPresets
{
    /// <summary>
    /// Position component.
    /// </summary>
    public readonly struct Position
    {
        public readonly float X, Y, Z;
        public Position(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    /// <summary>
    /// Velocity component.
    /// </summary>
    public readonly struct Velocity
    {
        public readonly float X, Y, Z;
        public Velocity(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    /// <summary>
    /// SystemPreset sample - demonstrates ScriptableObject-based system setup.
    /// </summary>
    public sealed class SystemPresetSample : MonoBehaviour
    {
        [Header("System Preset")]
        [SerializeField] private SystemsPreset? _preset;

        [Header("Manual Systems (Use when Preset is not available)")]
        [SerializeField] private bool _useManualSystems = false;

        private IWorld? _world;

        private void Start()
        {
            var kernel = KernelLocator.Current;
            if (kernel == null)
            {
                Debug.LogError("[SystemPresetSample] Kernel not found.");
                return;
            }

            _world = kernel.CreateWorld(null, "SystemPresetWorld", setAsCurrent: true);
            Debug.Log("[SystemPresetSample] World created");

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
                Debug.LogWarning("[SystemPresetSample] SystemPreset not assigned. Please assign it in Inspector or use Manual Systems.");
            }

            CreateTestEntities();
        }

        /// <summary>
        /// Registers systems from the SystemPreset asset using SystemPresetResolver.
        /// </summary>
        private void RegisterSystemsFromPreset()
        {
            if (_world == null || _preset == null) return;

            var resolver = new SystemPresetResolver();
            var systems = resolver.InstantiateSystems(_preset.GetValidTypes().ToList());

            _world.AddSystems(systems);
            Debug.Log($"[SystemPresetSample] Registered {systems.Count} systems from SystemPreset '{_preset.name}'.");

            foreach (var system in systems)
            {
                Debug.Log($"[SystemPresetSample]   - {system.GetType().Name}");
            }
        }

        /// <summary>
        /// Manually registers systems when SystemPreset is not available.
        /// </summary>
        private void RegisterSystemsManually()
        {
            if (_world == null) return;

            _world.AddSystems(new List<ISystem>
            {
                new MovementSystem(),
                new RenderSystem()
            }.AsReadOnly());
            Debug.Log("[SystemPresetSample] Systems registered manually.");
        }

        /// <summary>
        /// Creates test entities with Position and Velocity components.
        /// </summary>
        private void CreateTestEntities()
        {
            if (_world == null) return;

            using var cmd = _world.BeginWrite();
            for (int i = 0; i < 3; i++)
            {
                var entity = cmd.CreateEntity();
                cmd.AddComponent(entity, new Position(i * 2f, 0, 0));
                cmd.AddComponent(entity, new Velocity(1f, 0, 0));
                Debug.Log($"[SystemPresetSample] Test entity {entity.Id} created");
            }
        }

        private void OnDestroy()
        {
            Debug.Log("[SystemPresetSample] Sample terminated");
        }
    }
}
