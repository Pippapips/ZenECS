// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 09 - UniRx Integration
// File: UniRxSample.cs
// Purpose: UniRx를 사용한 ECS 메시지 스트림 변환 예제
// Key concepts:
//   • WorldRx.Messages<T>() - ECS 메시지를 IObservable로 변환
//   • WorldRx.PublishFrom() - UniRx 스트림을 ECS 메시지로 발행
//   • 조건부 컴파일 (ZENECS_UNIRX)
//
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#if ZENECS_UNIRX
#nullable enable
using System;
using UnityEngine;
using UniRx;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.UniRx;
using ZenECS.Core;
using ZenECS.Core.Messaging;
using ZenECS.Core.Systems;

namespace ZenEcsAdapterUnitySamples.UniRx
{
    /// <summary>
    /// 데미지 메시지 - ECS 메시지 예제.
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
    /// 점프 Intent 메시지.
    /// </summary>
    public readonly struct JumpIntent : IMessage
    {
        public readonly Entity Entity;
        public JumpIntent(Entity entity) { Entity = entity; }
    }

    /// <summary>
    /// Health 컴포넌트.
    /// </summary>
    public readonly struct Health
    {
        public readonly int Max;
        public readonly int Current;
        public Health(int max, int current) { Max = max; Current = current; }
    }

    /// <summary>
    /// 데미지 처리 시스템 (FixedGroup).
    /// </summary>
    [FixedGroup]
    public sealed class DamageSystem : ISystem
    {
        public void Run(IWorld w, float dt)
        {
            // DamageMessage를 구독하여 처리하는 대신, 직접 처리
            // (실제로는 ISystemLifecycle에서 Subscribe 사용)
        }
    }

    /// <summary>
    /// UniRx 샘플 - ECS 메시지와 UniRx 스트림 통합을 보여줍니다.
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
                Debug.LogError("[UniRxSample] Kernel을 찾을 수 없습니다.");
                return;
            }

            _world = kernel.CreateWorld("UniRxWorld", setAsCurrent: true);
            _world.AddSystems([new DamageSystem()]);

            Debug.Log("[UniRxSample] World 생성 완료");

            CreatePlayer();
            SetupUniRxSubscriptions();
        }

        private void CreatePlayer()
        {
            if (_world == null) return;

            using var cmd = _world.BeginWrite();
            _playerEntity = cmd.CreateEntity();
            cmd.AddComponent(_playerEntity, new Health(100, 100));
        }

        private void SetupUniRxSubscriptions()
        {
            if (_world == null) return;

            // 1. ECS 메시지를 UniRx Observable로 변환
            _world.Messages<DamageMessage>()
                .ThrottleFirst(TimeSpan.FromMilliseconds(100))
                .ObserveOnMainThread()
                .Subscribe(msg =>
                {
                    Debug.Log($"[UniRxSample] DamageMessage 수신: Entity={msg.Target.Id}, Amount={msg.Amount}");
                    
                    // Health 업데이트
                    if (_world.HasComponent<Health>(msg.Target))
                    {
                        using var cmd = _world.BeginWrite();
                        var health = _world.ReadComponent<Health>(msg.Target);
                        var newHealth = Math.Max(0, health.Current - msg.Amount);
                        cmd.ReplaceComponent(msg.Target, new Health(health.Max, newHealth));
                        Debug.Log($"[UniRxSample] Health 업데이트: {newHealth}/{health.Max}");
                    }
                })
                .AddTo(_disposables);

            // 2. Unity Input을 Observable로 변환하여 ECS 메시지로 발행
            this.UpdateAsObservable()
                .Where(_ => Input.GetKeyDown(KeyCode.Space))
                .Select(_ => new JumpIntent(_playerEntity))
                .Do(msg => Debug.Log($"[UniRxSample] JumpIntent 생성: Entity={msg.Entity.Id}"))
                .PublishFrom(_world)
                .AddTo(_disposables);

            // 3. 주기적으로 데미지 메시지 발행 (시뮬레이션)
            Observable.Interval(TimeSpan.FromSeconds(_damageInterval))
                .Select(_ => new DamageMessage(_playerEntity, UnityEngine.Random.Range(5, 15)))
                .Do(msg => Debug.Log($"[UniRxSample] DamageMessage 발행: Entity={msg.Target.Id}, Amount={msg.Amount}"))
                .PublishFrom(_world)
                .AddTo(_disposables);

            Debug.Log("[UniRxSample] UniRx 구독 설정 완료:");
            Debug.Log("  - DamageMessage → Health 업데이트");
            Debug.Log("  - Space 키 → JumpIntent 발행");
            Debug.Log($"  - {_damageInterval}초마다 자동 데미지");
        }

        private void OnDestroy()
        {
            _disposables?.Dispose();
            Debug.Log("[UniRxSample] 샘플 종료 및 구독 해제");
        }

        private void OnGUI()
        {
            if (_world == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Label("UniRx Integration Sample", GUI.skin.box);
            GUILayout.Space(10);
            GUILayout.Label("Space 키를 눌러 JumpIntent 발행");
            GUILayout.Label($"{_damageInterval}초마다 자동 데미지");

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
    /// UniRx 샘플 - ZENECS_UNIRX define이 필요합니다.
    /// </summary>
    public sealed class UniRxSample : MonoBehaviour
    {
        private void Start()
        {
            Debug.LogWarning("[UniRxSample] ZENECS_UNIRX define이 설정되지 않았습니다. Player Settings에서 Scripting Define Symbols에 ZENECS_UNIRX를 추가하세요.");
        }
    }
}
#endif
