// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 03 - EntityBlueprint
// File: EntityBlueprintSample.cs
// Purpose: Example of entity spawning using EntityBlueprint
// Key concepts:
//   • ScriptableObject-based entity blueprint
//   • Component snapshot storage and application
//   • Context Assets and Binders setup
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
    /// Health component - stores entity health.
    /// </summary>
    [ZenComponent]
    public readonly struct Health
    {
        public readonly int Max;
        public readonly int Current;
        public Health(int max, int current) { Max = max; Current = current; }
        public Health WithCurrent(int current) => new Health(Max, current);
    }

    /// <summary>
    /// Position component - stores entity position.
    /// </summary>
    [ZenComponent]
    public readonly struct Position
    {
        public readonly float X, Y, Z;
        public Position(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    /// <summary>
    /// Rotation component - stores entity rotation.
    /// </summary>
    [ZenComponent]
    public readonly struct Rotation
    {
        public readonly float X, Y, Z;
        public Rotation(float x, float y, float z) { X = x; Y = y; Z = z; }
    }
    
    /// <summary>
    /// EntityBlueprint sample - demonstrates ScriptableObject-based entity spawning.
    /// </summary>
    public sealed class EntityBlueprintSample : MonoBehaviour
    {
        [Header("Blueprint")]
        [SerializeField] private ZenECS.Adapter.Unity.Blueprints.EntityBlueprint? _blueprint;

        [Header("Spawn Settings")]
        [SerializeField] private float _spawnInterval = 1f;

        private IWorld? _world;
        private float _spawnTimer;

        private void Start()
        {
            var kernel = KernelLocator.Current;
            if (kernel == null)
            {
                Debug.LogError("[EntityBlueprintSample] Kernel not found.");
                return;
            }

            _world = kernel.CreateWorld(null, "BlueprintWorld", setAsCurrent: true);
            Debug.Log("[EntityBlueprintSample] World created");

            if (_blueprint == null)
            {
                Debug.LogWarning("[EntityBlueprintSample] Blueprint not assigned. Please assign it in Inspector.");
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

        /// <summary>
        /// Spawns an entity from the EntityBlueprint asset.
        /// </summary>
        private void SpawnFromBlueprint()
        {
            if (_world == null || _blueprint == null) return;

            // Spawn entity from Blueprint
            _blueprint.Spawn(
                _world,
                ZenEcsUnityBridge.SharedContextResolver,
                onCreated: entity =>
                {
                    Debug.Log($"[EntityBlueprintSample] Entity {entity.Id} spawned from Blueprint.");

                    // Check spawned entity components
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
