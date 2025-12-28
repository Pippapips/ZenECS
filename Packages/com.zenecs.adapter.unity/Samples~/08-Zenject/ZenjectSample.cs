// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 08 - Zenject Integration
// File: ZenjectSample.cs
// Purpose: Example of dependency injection using Zenject
// Key concepts:
//   • Kernel and Resolver binding via ProjectInstaller
//   • System dependency injection via Zenject
//   • Conditional compilation (ZENECS_ZENJECT)
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Binding.Contexts;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Systems;

#if ZENECS_ZENJECT
using Zenject;
#endif

namespace ZenEcsAdapterUnitySamples.ZenjectSamples
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
    public sealed class ZenjectSample : MonoBehaviour
    {
        [Header("Blueprint")]
        [SerializeField] private ZenECS.Adapter.Unity.Blueprints.EntityBlueprint? _blueprint;

        [Header("Spawn Settings")]
        [SerializeField] private float _spawnInterval = 1f;

        private IWorld? _world;
        private float _spawnTimer;
        private IKernel? _kernel;

#if ZENECS_ZENJECT
        [Inject]
        void Construct(IKernel kernel)
        {
            _kernel = kernel;
        }
#endif
        
        private void Start()
        {
            if (_kernel == null) return;
            
            _world = _kernel.CreateWorld(null, "BlueprintWorld", setAsCurrent: true);
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
