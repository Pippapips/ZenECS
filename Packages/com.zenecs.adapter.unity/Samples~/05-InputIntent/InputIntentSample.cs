// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 05 - Input → Intent
// File: InputIntentSample.cs
// Purpose: Example pattern of converting Unity Input to ECS Intent components
// Key concepts:
//   • Express input intent with Intent components
//   • Input collection (FrameInputGroup)
//   • Intent processing (FixedGroup)
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

namespace ZenEcsAdapterUnitySamples.InputIntent
{
    /// <summary>
    /// Move Intent - represents player's movement intent.
    /// </summary>
    [ZenComponent]
    public readonly struct MoveIntent
    {
        public readonly float X, Y, Z;
        public MoveIntent(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

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
    /// Input collection system (FrameInputGroup) - converts Unity Input to Intent.
    /// </summary>
    [FrameInputGroup]
    [ZenSystemWatch(typeof(MoveIntent))]
    public sealed class InputCollectionSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            foreach (var (e, intent) in w.Query<MoveIntent>())
            {
                // Collect Unity Input (using Legacy Input)
                float x = Input.GetAxis("Horizontal");
                float z = Input.GetAxis("Vertical");

                // Add/update Intent component
                using var cmd = w.BeginWrite();
                cmd.ReplaceComponent(e, new MoveIntent(x, 0, z));
            }
        }
    }

    /// <summary>
    /// Intent processing system (FixedGroup) - reads Intent and executes game logic.
    /// </summary>
    [FixedGroup]
    [ZenSystemWatch(typeof(MoveIntent), typeof(Position))]
    public sealed class MovementIntentSystem : ISystem
    {
        private const float MoveSpeed = 5f;

        public void Run(IWorld w, float dt)
        {
            using var cmd = w.BeginWrite();
            foreach (var (e, intent, pos) in w.Query<MoveIntent, Position>())
            {
                // Update Position based on Intent
                var newPos = new Position(
                    pos.X + intent.X * dt * MoveSpeed,
                    pos.Y,
                    pos.Z + intent.Z * dt * MoveSpeed
                );
                cmd.ReplaceComponent(e, newPos);

                // Consume Intent (remove) - process only once per frame
                cmd.ReplaceComponent(e, new MoveIntent());
            }
        }
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
                    if (link) link.transform.position = new Vector3(pos.X, pos.Y, pos.Z);
                }
            }
        }
    }

    /// <summary>
    /// InputIntent sample - demonstrates Input → Intent pattern.
    /// </summary>
    public sealed class InputIntentSample : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private GameObject? _playerPrefab;

        private IWorld? _world;
        private Entity _playerEntity;

        private void Start()
        {
            var kernel = KernelLocator.Current;
            if (kernel == null)
            {
                Debug.LogError("[InputIntentSample] Kernel not found.");
                return;
            }

            _world = kernel.CreateWorld(null, "InputIntentWorld", setAsCurrent: true);
            _world.AddSystems(new List<ISystem>
            {
                new InputCollectionSystem(),
                new MovementIntentSystem(),
                new PositionViewSystem()
            }.AsReadOnly());

            Debug.Log("[InputIntentSample] World and systems registered");

            CreatePlayer();
        }

        /// <summary>
        /// Creates the player entity and links it to a GameObject.
        /// </summary>
        private void CreatePlayer()
        {
            if (_world == null) return;

            // Create GameObject
            GameObject playerGo;
            if (_playerPrefab != null)
            {
                playerGo = Instantiate(_playerPrefab);
            }
            else
            {
                playerGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                playerGo.name = "Player";
            }

            // Create Entity
            using (var cmd = _world.BeginWrite())
            {
                _playerEntity = cmd.CreateEntity();
                cmd.AddComponent(_playerEntity, new Position(0, 0, 0));
                cmd.AddComponent(_playerEntity, new MoveIntent());
            }

            // Create EntityLink
            // Only created this way for testing
            // In practice, use Context/Binder
#if UNITY_EDITOR
            var link = playerGo.CreateEntityLink(_world, _playerEntity);
#else
            var link = playerGo.AddComponent<EntityLink>();
            link.Attach(_world, _playerEntity);
#endif

            Debug.Log($"[InputIntentSample] Player Entity {_playerEntity.Id} created");
            Debug.Log("[InputIntentSample] Use WASD or arrow keys to move!");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Label("Input → Intent Sample");
            GUILayout.Label("Use WASD or arrow keys to move");
            if (_world != null && _world.HasComponent<Position>(_playerEntity))
            {
                var pos = _world.ReadComponent<Position>(_playerEntity);
                GUILayout.Label($"Position: ({pos.X:0.##}, {pos.Y:0.##}, {pos.Z:0.##})");
            }
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            Debug.Log("[InputIntentSample] Sample terminated");
        }
    }
}
