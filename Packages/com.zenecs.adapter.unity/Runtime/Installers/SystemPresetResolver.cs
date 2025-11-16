#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Systems;
using Zenject;

namespace ZenECS.Adapter.Unity.Install
{
    public sealed class SystemPresetResolver : ISystemPresetResolver
    {
        private readonly DiContainer? _container;

        public SystemPresetResolver(DiContainer container)
        {
            _container = container;
        }
    
        public List<ISystem> InstantiateSystems(List<Type> types)
        {
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
        }
        
        // /// <summary>Preset + Local을 합쳐 **항상 타입 중복 제거**.</summary>
        // private List<Type> CollectDistinctTypes(IEnumerable<Type>? validTypes)
        // {
        //     if (validTypes == null) return new List<Type>();
        //     var set = new HashSet<string>(StringComparer.Ordinal);
        //     var list = new List<Type>();
        //
        //     // 1) Preset
        //     foreach (var t in validTypes)
        //         AddDistinct(t, set, list);
        //
        //     // 2) Local (SystemTypeRef[])
        //     if (systemTypes != null)
        //     {
        //         foreach (var r in systemTypes)
        //         {
        //             var t = r.Resolve();
        //             AddDistinct(t, set, list);
        //         }
        //     }
        //
        //     return list;
        //
        //     static void AddDistinct(Type? t, HashSet<string> seen, List<Type> dst)
        //     {
        //         if (t == null || t.IsAbstract || !typeof(ISystem).IsAssignableFrom(t)) return;
        //         var key = t.AssemblyQualifiedName ?? t.FullName;
        //         if (string.IsNullOrEmpty(key)) return;
        //         if (seen.Add(key)) dst.Add(t);
        //     }
        // }
    }
}