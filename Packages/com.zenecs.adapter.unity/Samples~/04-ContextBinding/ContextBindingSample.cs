// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter Unity Samples 04 - Context Binding
// File: ContextBindingSample.cs
// Purpose: Shared Context와 Per-Entity Context 사용 예제
// Key concepts:
//   • Shared Context - 여러 엔티티가 공유하는 Context
//   • Per-Entity Context - 엔티티별 독립적인 Context
//   • ISharedContextResolver 구현
//
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Binding.Contexts;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Core;
using ZenECS.Core.Binding;

namespace ZenEcsAdapterUnitySamples.ContextBinding
{
    /// <summary>
    /// 게임 설정 Context (Shared) - 여러 엔티티가 공유합니다.
    /// </summary>
    public interface IGameConfig : IContext
    {
        float GameSpeed { get; }
        int MaxPlayers { get; }
    }

    public class GameConfig : IGameConfig
    {
        public float GameSpeed { get; set; } = 1f;
        public int MaxPlayers { get; set; } = 4;
    }

    /// <summary>
    /// 플레이어 데이터 Context (Per-Entity) - 각 엔티티마다 독립적입니다.
    /// </summary>
    public interface IPlayerData : IContext
    {
        string PlayerName { get; }
        int Score { get; set; }
    }

    public class PlayerData : IPlayerData
    {
        public string PlayerName { get; set; } = "";
        public int Score { get; set; }
    }

    /// <summary>
    /// 커스텀 Shared Context Resolver 구현 예제.
    /// </summary>
    public class CustomContextResolver : ISharedContextResolver
    {
        private readonly Dictionary<SharedContextAsset, IContext> _cache = new();

        public IContext? Resolve(SharedContextAsset asset)
        {
            if (_cache.TryGetValue(asset, out var ctx))
                return ctx;

            // Shared Context 생성 및 캐싱
            // 실제로는 asset의 설정을 읽어서 Context를 생성합니다
            if (asset.name.Contains("GameConfig"))
            {
                var config = new GameConfig { GameSpeed = 1.5f, MaxPlayers = 8 };
                _cache[asset] = config;
                Debug.Log($"[CustomContextResolver] GameConfig 생성: Speed={config.GameSpeed}, MaxPlayers={config.MaxPlayers}");
                return config;
            }

            return null;
        }
    }

    /// <summary>
    /// Context Binding 샘플 - Shared와 Per-Entity Context 사용을 보여줍니다.
    /// </summary>
    public sealed class ContextBindingSample : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int _playerCount = 3;

        private IWorld? _world;
        private CustomContextResolver? _resolver;
        private GameConfig? _sharedConfig;

        private void Start()
        {
            var kernel = KernelLocator.Current;
            if (kernel == null)
            {
                Debug.LogError("[ContextBindingSample] Kernel을 찾을 수 없습니다.");
                return;
            }

            _world = kernel.CreateWorld("ContextWorld", setAsCurrent: true);
            _resolver = new CustomContextResolver();

            Debug.Log("[ContextBindingSample] World 생성 완료. Context 설정 중...");

            SetupContexts();
        }

        private void SetupContexts()
        {
            if (_world == null) return;

            // Shared Context 생성 (모든 엔티티가 공유)
            _sharedConfig = new GameConfig { GameSpeed = 2f, MaxPlayers = _playerCount };
            Debug.Log($"[ContextBindingSample] Shared GameConfig 생성: Speed={_sharedConfig.GameSpeed}");

            // 플레이어 엔티티 생성 및 Context 등록
            for (int i = 0; i < _playerCount; i++)
            {
                Entity entity;
                using (var cmd = _world.BeginWrite())
                {
                    entity = cmd.CreateEntity();
                }

                // Shared Context 등록 (같은 인스턴스)
                _world.RegisterContext(entity, _sharedConfig);

                // Per-Entity Context 등록 (각각 다른 인스턴스)
                var playerData = new PlayerData
                {
                    PlayerName = $"Player_{i + 1}",
                    Score = 0
                };
                _world.RegisterContext(entity, playerData);

                Debug.Log($"[ContextBindingSample] Entity {entity.Id}: {playerData.PlayerName} 생성 완료");

                // Context 조회 테스트
                var config = _world.GetContext<IGameConfig>(entity);
                var player = _world.GetContext<IPlayerData>(entity);

                if (config != null && player != null)
                {
                    Debug.Log($"[ContextBindingSample] Entity {entity.Id} Context 확인:");
                    Debug.Log($"  - GameConfig (Shared): Speed={config.GameSpeed}, MaxPlayers={config.MaxPlayers}");
                    Debug.Log($"  - PlayerData (Per-Entity): Name={player.PlayerName}, Score={player.Score}");
                }
            }

            // Shared Context가 실제로 공유되는지 확인
            if (_playerCount >= 2)
            {
                var entities = new List<Entity>();
                foreach (var (e, _) in _world.Query<Entity>())
                {
                    entities.Add(e);
                    if (entities.Count >= 2) break;
                }

                if (entities.Count >= 2)
                {
                    var config1 = _world.GetContext<IGameConfig>(entities[0]);
                    var config2 = _world.GetContext<IGameConfig>(entities[1]);
                    bool isShared = ReferenceEquals(config1, config2);
                    Debug.Log($"[ContextBindingSample] Shared Context 공유 확인: {isShared} (같은 인스턴스여야 함)");
                }
            }
        }

        private void OnDestroy()
        {
            Debug.Log("[ContextBindingSample] 샘플 종료");
        }
    }
}
