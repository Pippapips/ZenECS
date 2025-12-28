// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 07 - UniRx Integration
// File: UniRxSample.cs
// Purpose: Example of ECS message stream conversion using UniRx
// Key concepts:
//   • WorldRx.Messages<T>() - Convert ECS messages to IObservable
//   • WorldRx.PublishFrom() - Publish UniRx streams as ECS messages
//   • Conditional compilation (ZENECS_UNIRX)
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#if ZENECS_UNIRX
#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.UniRx;
using ZenECS.Core;
using ZenECS.Core.Messaging;
using ZenECS.Core.Systems;

namespace ZenEcsAdapterUnitySamples.UniRx
{
    /// <summary>
    /// Damage message - ECS message example.
    /// </summary>
    public readonly struct DamageMessage : IMessage
    {
        public readonly Entity Target;
        public readonly int Amount;
        public DamageMessage(Entity target, int amount)
        {
            Target = target;
            Amount = amount;
        }
    }

    /// <summary>
    /// Jump Intent message.
    /// </summary>
    public readonly struct JumpIntent : IMessage
    {
        public readonly Entity Entity;
        public JumpIntent(Entity entity) { Entity = entity; }
    }

    /// <summary>
    /// Health component.
    /// </summary>
    [ZenComponent]
    public readonly struct Health
    {
        public readonly int Max;
        public readonly int Current;
        public Health(int max, int current) { Max = max; Current = current; }
    }

    /// <summary>
    /// Damage processing system (FixedGroup).
    /// </summary>
    [FixedGroup]
    public sealed class DamageSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            // Process directly instead of subscribing to DamageMessage
            // (In practice, use Subscribe in ISystemLifecycle)
        }
    }

    /// <summary>
    /// UniRx sample - demonstrates integration of ECS messages and UniRx streams.
    /// </summary>
    public sealed class UniRxSample : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _damageInterval = 1f;

        private IWorld? _world;
        private Entity _playerEntity;
        private CompositeDisposable _disposables = new();

        private void Start()
        {
            var kernel = KernelLocator.Current;
            if (kernel == null)
            {
                Debug.LogError("[UniRxSample] Kernel not found.");
                return;
            }

            _world = kernel.CreateWorld(null, "UniRxWorld", setAsCurrent: true);
            _world.AddSystems(new List<ISystem> { new DamageSystem() }.AsReadOnly());

            Debug.Log("[UniRxSample] World created");

            CreatePlayer();
            SetupUniRxSubscriptions();
        }

        /// <summary>
        /// Creates the player entity with Health component.
        /// </summary>
        private void CreatePlayer()
        {
            if (_world == null) return;

            using var cmd = _world.BeginWrite();
            _playerEntity = cmd.CreateEntity();
            cmd.AddComponent(_playerEntity, new Health(100, 100));
        }

        /// <summary>
        /// Sets up UniRx subscriptions for ECS message conversion and reactive pipelines.
        /// </summary>
        private void SetupUniRxSubscriptions()
        {
            if (_world == null) return;

            // 1. Convert ECS messages to UniRx Observable
            _world.Messages<DamageMessage>()
                .ThrottleFirst(TimeSpan.FromMilliseconds(100))
                .ObserveOnMainThread()
                .Subscribe(msg =>
                {
                    Debug.Log($"[UniRxSample] DamageMessage received: Entity={msg.Target.Id}, Amount={msg.Amount}");
                    
                    // Update Health
                    if (_world.HasComponent<Health>(msg.Target))
                    {
                        using var cmd = _world.BeginWrite();
                        var health = _world.ReadComponent<Health>(msg.Target);
                        var newHealth = Math.Max(0, health.Current - msg.Amount);
                        cmd.ReplaceComponent(msg.Target, new Health(health.Max, newHealth));
                        Debug.Log($"[UniRxSample] Health updated: {newHealth}/{health.Max}");
                    }
                })
                .AddTo(_disposables);

            // 2. Convert Unity Input to Observable and publish as ECS message
            this.UpdateAsObservable()
                .Where(_ => Input.GetKeyDown(KeyCode.Space))
                .Select(_ => new JumpIntent(_playerEntity))
                .Do(msg => Debug.Log($"[UniRxSample] JumpIntent created: Entity={msg.Entity.Id}"))
                .PublishFrom(_world)
                .AddTo(_disposables);

            // 3. Periodically publish damage messages (simulation)
            Observable.Interval(TimeSpan.FromSeconds(_damageInterval))
                .Select(_ => new DamageMessage(_playerEntity, UnityEngine.Random.Range(5, 15)))
                .Do(msg => Debug.Log($"[UniRxSample] DamageMessage published: Entity={msg.Target.Id}, Amount={msg.Amount}"))
                .PublishFrom(_world)
                .AddTo(_disposables);

            Debug.Log("[UniRxSample] UniRx subscriptions set up:");
            Debug.Log("  - DamageMessage → Health update");
            Debug.Log("  - Space key → JumpIntent publish");
            Debug.Log($"  - Auto damage every {_damageInterval} seconds");
        }

        private void OnDestroy()
        {
            _disposables?.Dispose();
            Debug.Log("[UniRxSample] Sample terminated and subscriptions disposed");
        }

        private void OnGUI()
        {
            if (_world == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Label("UniRx Integration Sample", GUI.skin.box);
            GUILayout.Space(10);
            GUILayout.Label("Press Space key to publish JumpIntent");
            GUILayout.Label($"Auto damage every {_damageInterval} seconds");

            if (_world.HasComponent<Health>(_playerEntity))
            {
                var health = _world.ReadComponent<Health>(_playerEntity);
                GUILayout.Label($"Health: {health.Current}/{health.Max}");
            }

            GUILayout.EndArea();
        }
    }
}
#else
using UnityEngine;

namespace ZenEcsAdapterUnitySamples.UniRx
{
    /// <summary>
    /// UniRx sample - ZENECS_UNIRX define is required.
    /// </summary>
    public sealed class UniRxSample : MonoBehaviour
    {
        private void Start()
        {
            Debug.LogWarning("[UniRxSample] ZENECS_UNIRX define is not set. Please add ZENECS_UNIRX to Scripting Define Symbols in Player Settings.");
        }
    }
}
#endif
