// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 02 - EntityLink
// File: EntityLinkSample.cs
// Purpose: Example of using EntityLink to connect GameObject and Entity
// Key concepts:
//   • GameObject ↔ Entity connection via EntityLink
//   • View management via EntityViewRegistry
//   • Link lifecycle management
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Linking;
using ZenECS.Core;
using ZenECS.Core.Systems;

namespace ZenEcsAdapterUnitySamples.EntityLink
{
    /// <summary>
    /// Position component - stores 3D position.
    /// </summary>
    [ZenComponent]
    public readonly struct Position
    {
        public readonly float X, Y, Z;
        public Position(float x, float y, float z) { X = x; Y = y; Z = z; }
        public Vector3 ToVector3() => new Vector3(X, Y, Z);
    }

    /// <summary>
    /// System that applies Position to Transform (FrameViewGroup, read-only).
    /// </summary>
    [FrameViewGroup]
    [ZenSystemWatch(typeof(Position))]
    public sealed class PositionViewSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            var registry = EntityViewRegistry.For(w);
            foreach (var (entity, pos) in w.Query<Position>())
            {
                if (registry.TryGet(entity, out var link))
                {
                    if (link) link.transform.position = pos.ToVector3();
                }
            }
        }
    }

    /// <summary>
    /// EntityLink sample - demonstrates how to connect GameObject and Entity.
    /// </summary>
    public sealed class EntityLinkSample : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int _spawnCount = 5;
        [SerializeField] private float _spawnRadius = 5f;
        [SerializeField] private GameObject? _prefab;

        private IWorld? _world;

        private void Start()
        {
            var kernel = KernelLocator.Current;
            if (kernel == null)
            {
                Debug.LogError("[EntityLinkSample] Kernel not found. Please add EcsDriver.");
                return;
            }

            _world = kernel.CreateWorld(null, "EntityLinkWorld", setAsCurrent: true);
            _world.AddSystems(new List<ISystem> { new PositionViewSystem() }.AsReadOnly());

            Debug.Log("[EntityLinkSample] World created. Creating entities...");

            SpawnEntities();
        }

        /// <summary>
        /// Spawns entities in a circular pattern and links them to GameObjects.
        /// </summary>
        private void SpawnEntities()
        {
            if (_world == null) return;

            var registry = EntityViewRegistry.For(_world);

            for (int i = 0; i < _spawnCount; i++)
            {
                // Create Entity
                Entity entity;
                using (var cmd = _world.BeginWrite())
                {
                    entity = cmd.CreateEntity();
                    float angle = (i / (float)_spawnCount) * 360f * Mathf.Deg2Rad;
                    float x = Mathf.Cos(angle) * _spawnRadius;
                    float z = Mathf.Sin(angle) * _spawnRadius;
                    cmd.AddComponent(entity, new Position(x, 0, z));
                }

                // Create GameObject and link
                GameObject go;
                if (_prefab != null)
                {
                    go = Instantiate(_prefab);
                }
                else
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                }

                go.name = $"Entity_{entity.Id}";

#if UNITY_EDITOR
                // Use extension method in editor
                var link = go.CreateEntityLink(_world, entity);
#else
                // Add component directly at runtime
                var link = go.AddComponent<EntityLink>();
                link.Attach(_world, entity);
#endif

                Debug.Log($"[EntityLinkSample] Entity {entity.Id} linked to {go.name}.");

                // Verify registry
                if (registry.TryGet(entity, out var registeredLink))
                {
                    Debug.Log($"[EntityLinkSample] Registry check: Entity {entity.Id} → {registeredLink?.gameObject.name}");
                }
            }

            // List all views
            Debug.Log($"[EntityLinkSample] Total {_spawnCount} EntityLinks created.");
            int count = 0;
            foreach (var (e, view) in registry.EnumerateViews())
            {
                count++;
            }
            Debug.Log($"[EntityLinkSample] Views registered in registry: {count}");
        }

        private void OnDestroy()
        {
            if (_world != null)
            {
                Debug.Log("[EntityLinkSample] Sample terminated. Cleaning up World...");
            }
        }
    }
}
