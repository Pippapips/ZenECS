using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ZenECS.Core
{
    /// <summary>
    /// Allocation-free enumerator over a component pool's active entity ids.
    /// 설계 목표: 힙 할당 0, 분기 최소화, foreach 호환.
    /// </summary>
    internal struct PoolEnumerator
    {
        private readonly IComponentPool? _pool;
        private readonly int _end;    // 스캔 상한 (Capacity 스냅샷)
        private int _idx;             // 다음 검사할 인덱스 - 1
        private int _currentId;

        public static PoolEnumerator Empty => default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PoolEnumerator(IComponentPool pool)
        {
            _pool = pool;
            _end  = pool.Capacity;
            _idx  = -1;
            _currentId = -1;
        }

        public int CurrentId
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _currentId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var p = _pool;
            if (p == null) return false;

            while (++_idx < _end)
            {
                // 스파스 구조: index == entityId
                int id = p.EntityIdAt(_idx); // 여기서는 id == _idx
                if (p.Has(id))
                {
                    _currentId = id;
                    return true;
                }
            }
            _currentId = -1;
            return false;
        }
    }
    
    /// <summary>
    /// Common interface for all component pools.
    /// Keeps the minimal set of APIs required for snapshot save/load and tooling reflection.
    /// </summary>
    internal interface IComponentPool
    {
        /// <summary>
        /// Ensures that the internal storage is large enough to access the given entity ID.
        /// If necessary, expands the underlying arrays.
        /// </summary>
        void EnsureCapacity(int entityId);

        /// <summary>
        /// Returns whether the entity currently holds this component type.
        /// </summary>
        bool Has(int entityId);

        /// <summary>
        /// Removes the component from the given entity.
        /// Optionally clears the stored data to default.
        /// </summary>
        void Remove(int entityId, bool dataClear = true);

        /// <summary>
        /// Retrieves the component as a boxed value (returns null if not present).
        /// </summary>
        object? GetBoxed(int entityId);

        /// <summary>
        /// Sets the component using a boxed value.
        /// Adds a new component or overwrites an existing one.
        /// </summary>
        void SetBoxed(int entityId, object value);

        int  Capacity { get; }
        
        /// <summary>
        /// Enumerates all active components in the pool as (entityId, boxed value) pairs.
        /// </summary>
        PoolEnumerator EnumerateAll();

        // 스파스 구조이므로 denseIndex == entityId 로 취급
        // (필요하면 나중에 진짜 dense 테이블로 교체 가능)
        int  EntityIdAt(int index);  // 여기서는 index 그대로 반환
        
        /// <summary>
        /// Returns the number of active components stored in the pool.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Clears all data and resets bit flags — typically used before loading a new snapshot.
        /// </summary>
        void ClearAll();
    }
}