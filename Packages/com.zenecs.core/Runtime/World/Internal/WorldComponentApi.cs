#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ZenECS.Core.Abstractions.Config;
using ZenECS.Core.Binding;
using ZenECS.Core.Internal.ComponentPooling;

namespace ZenECS.Core.Internal
{
    internal sealed partial class World : IWorldComponentApi
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HandleDenied(string reason)
        {
            switch (EcsRuntimeOptions.WritePolicy)
            {
                case EcsRuntimeOptions.WriteFailurePolicy.Throw:
                    throw new InvalidOperationException(reason);
                case EcsRuntimeOptions.WriteFailurePolicy.Log:
                    EcsRuntimeOptions.Log.Warn(reason);
                    return false;
                default:
                    return false;
            }
        }

        public bool AddComponent<T>(Entity e, in T value) where T : struct
        {
            if (!_permissionHook.EvaluateWritePermission(e, typeof(T)))
            {
                if (!HandleDenied($"[Denied] Add<{typeof(T).Name}> e={e.Id} reason=WritePermission"))
                    return false;
            }

            bool valid = _permissionHook.ValidateTyped(in value);
            if (!valid)
            {
                if (!HandleDenied($"[Denied] Add<{typeof(T).Name}> e={e.Id} reason=ValidateFailed value={value}"))
                    return false;
            }
            else if (!_permissionHook.ValidateObject(value!))
            {
                if (!HandleDenied(
                        $"[Denied] Add<{typeof(T).Name}> e={e.Id} reason=ValidateFailed(value-hook) value={value}"))
                    return false;
            }

            if (HasComponent<T>(e)) return false;

            ref var r = ref RefComponent<T>(e);
            r = value;
            _bindingRouter.Dispatch(new ComponentDelta<T>(e, ComponentDeltaKind.Added, value));

            return true;
        }

        public ref T RefComponent<T>(Entity e) where T : struct
        {
            var pool = (ComponentPool<T>)_componentPoolRepository.GetPool<T>();
            return ref pool.Ref(e.Id);
        }

        public ref T RefComponentExisting<T>(Entity e) where T : struct
        {
            var pool = _componentPoolRepository.TryGetPool<T>();
            if (pool == null || !pool.Has(e.Id))
                throw new InvalidOperationException($"RefExisting<{typeof(T).Name}> missing on {e.Id}");
            return ref ((ComponentPool<T>)pool).Ref(e.Id);
        }

        public bool HasComponent<T>(Entity e) where T : struct
        {
            var pool = _componentPoolRepository.TryGetPool<T>();
            return pool != null && pool.Has(e.Id);
        }

        public ref T ReadComponent<T>(Entity e) where T : struct
        {
            return ref RefComponent<T>(e);
        }

        public bool ReplaceComponent<T>(Entity e, in T value) where T : struct
        {
            if (!_permissionHook.EvaluateWritePermission(e, typeof(T)))
            {
                if (!HandleDenied($"[Denied] Replace<{typeof(T).Name}> e={e.Id} reason=WritePermission"))
                    return false;
            }

            bool valid = _permissionHook.ValidateTyped(in value);
            if (!valid)
            {
                if (!HandleDenied($"[Denied] Replace<{typeof(T).Name}> e={e.Id} reason=ValidateFailed value={value}"))
                    return false;
            }
            else if (!_permissionHook.ValidateObject(value!))
            {
                if (!HandleDenied(
                        $"[Denied] Replace<{typeof(T).Name}> e={e.Id} reason=ValidateFailed(value-hook) value={value}"))
                    return false;
            }

            ref var r = ref RefComponent<T>(e);
            r = value;
            _bindingRouter.Dispatch(new ComponentDelta<T>(e, ComponentDeltaKind.Changed, value));
            return true;
        }
        
         public bool RemoveComponent<T>(Entity e) where T : struct
         {
             if (!_permissionHook.EvaluateWritePermission(e, typeof(T)))
             {
                 if (!HandleDenied($"[Denied] Remove<{typeof(T).Name}> e={e.Id} reason=WritePermission"))
                     return false;
             }

             var pool = _componentPoolRepository.TryGetPool<T>();
             if (pool == null) return false;
             pool.Remove(e.Id);
             _bindingRouter.Dispatch(new ComponentDelta<T>(e, ComponentDeltaKind.Removed));
             return true;
         }

         public IEnumerable<(Type type, object? boxed)> GetAllComponents(Entity e)
         {
             foreach (var kv in _componentPoolRepository.Pools)
                 if (kv.Value.Has(e.Id))
                     yield return (kv.Key, kv.Value.GetBoxed(e.Id));
         }

         public bool TryRead<T>(Entity e, out T value) where T : struct
         {
             if (!HasComponent<T>(e)) { value = default; return false; }
             value = ReadComponent<T>(e);
             return true;
         }
    }
}