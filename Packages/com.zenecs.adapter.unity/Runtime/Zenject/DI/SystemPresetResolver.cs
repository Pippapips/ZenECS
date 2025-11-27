#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Core.Systems;
#if ZENECS_ZENJECT
using Zenject;
#endif

namespace ZenECS.Adapter.Unity.DI
{
    public sealed class SystemPresetResolver : ISystemPresetResolver
    {
#if ZENECS_ZENJECT
        private readonly DiContainer? _container;

        public SystemPresetResolver(DiContainer container)
        {
            _container = container;
        }
#endif    
        public List<ISystem> InstantiateSystems(List<Type> types)
        {
#if !ZENECS_ZENJECT
            retrun;
#else
            if (_container == null) return new List<ISystem>();
            var kernel = ZenEcsUnityBridge.Kernel;
            var list = new List<ISystem>(types.Count);
            foreach (var t in types)
            {
                if (kernel is { CurrentWorld: not null })
                {
                    if (kernel.CurrentWorld.TryGetSystem(t, out ISystem? system))
                        continue;
                }
                
                try { list.Add((ISystem)_container.Instantiate(t)); }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SystemPresetResolver] instantiate failed: {t?.Name} — {ex.Message}");
                }
            }
            return list;
#endif
        }
    }
}