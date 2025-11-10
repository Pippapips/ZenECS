using System;
using UnityEngine;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Linking
{
    public enum ViewDespawnPolicy
    {
        None,                 // View가 죽어도 Entity는 손대지 않음 (기본/권장)
        DestroyEntityOnView,  // View가 죽는 즉시 연결된 Entity 삭제 (선택)
        
    }
    
    /// <summary>
    /// Purpose: Attach this to a spawned GameObject to carry the owning ECS Entity reference.
    /// Key Concepts:
    /// - Holds (World, Entity) pair safely.
    /// - Attached only at runtime (never store prefab-time stale IDs).
    /// - Plays nicely with pooling and context (Initialize/Deinitialize).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EntityLink : MonoBehaviour
    {
        public IWorld World { get; private set; }
        public Entity Entity { get; private set; }

        // 이 뷰가 엔티티 삭제를 트리거해도 되는가?
        public ViewDespawnPolicy Policy { get; private set; } = ViewDespawnPolicy.None;

        // “내가 만든 Entity만 지운다” 보장을 위한 소유 토큰(컨텍스트/스폰너 측에서 부여)
        public int OwnerToken { get; private set; }

        // 풀링 구분(풀 반환은 Destroy가 아님!)
        public bool IsPooledInstance { get; set; }

        public void Attach(IWorld w, Entity e, ViewDespawnPolicy policy, int ownerToken)
        {
            World = w;
            Entity = e;
            Policy = policy;
            OwnerToken = ownerToken;
            IsPooledInstance = false;
        }

        public void Detach()
        {
            World = null;
            Entity = default;
            Policy = ViewDespawnPolicy.None;
            OwnerToken = 0;
            IsPooledInstance = false;
        }

        private void OnDestroy()
        {
            // 1) 에디터 Stop/도메인리로드/씬 언로드 등 비정상 종료에서 남발 방지
            if (!Application.isPlaying) return;

            // 2) 풀링으로 반환되는 케이스는 OnDestroy가 아니라 Despawn 콜백으로 처리해야 함
            if (IsPooledInstance) return;

            // 3) 정책상 View->Model 파기가 허용된 경우만
            if (Policy != ViewDespawnPolicy.DestroyEntityOnView) return;

            // 4) 안전 가드: 월드/엔티티 유효성 & 내가 만든 소유 토큰인지
            if (World == null || Entity.Id < 0 || !World.IsAlive(Entity)) return;

            // (선택) 월드가 종료 중인지 플래그가 있다면 체크
            // if (World.IsDisposing) return;

            // 5) OwnerToken 일치 시에만 삭제
            // (ModelContext가 자신이 스폰한 인스턴스에만 자신의 토큰을 부여)
            World.DespawnEntity(Entity);
        }
    }
}