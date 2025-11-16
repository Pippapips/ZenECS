#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Systems;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.DI;
using ZenECS.Adapter.Unity.Util;
using ZenECS.Adapter.Unity.Install; // SystemsPreset
#if ZENECS_ZENJECT
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using Zenject;
#endif

namespace ZenECS.Adapter.Unity.Install
{
#if ZENECS_ZENJECT
    /// <summary>
    /// 월드 보장/선택 후 시스템 등록.
    /// - Zenject가 있으면 DI로 시스템 인스턴스 생성
    /// - 없으면 기본 생성자 경로
    /// - Preset + Local 목록을 합쳐 **타입 중복은 항상 제거**하여 한 번만 등록
    /// - 해제(Dispose)는 월드 수명 규칙에 따름(여기서 별도 제거 작업 안 함)
    /// </summary>
    public sealed class WorldSystemInstaller : MonoInstaller
#else
    public sealed class WorldSystemInstaller : MonoBehaviour
#endif
    {
        [Header("World")]
        public string worldName = "Game";
        public bool   useCurrentWorld       = true;
        public bool   setAsCurrentOnEnsure  = true;

        [Header("Systems")]
        [Tooltip("ISystem 구현 타입(Installer-local). Preset과 병합됩니다.")]
        [SystemTypeFilter(typeof(ISystem), allowAbstract:false)]
        public SystemTypeRef[]? systemTypes;

        [Tooltip("SystemsPreset SO. 여기에 담긴 타입 + Local 타입을 합쳐 등록합니다.")]
        public SystemsPreset? systemsPreset;
        
        private IWorld? _world;

#if ZENECS_ZENJECT
        public override void InstallBindings()
        {
            _world = ResolveWorld();
        }

        private void Awake()
        {
            addSystems();
        }
#else
        void Awake()
        {
            _world = ResolveWorld();
            addSystems();
        }
#endif

        private void addSystems()
        {
            if (_world == null)
            {
                Debug.LogWarning("World is null systems are not registered.");
                return;
            }
            
            var types = CollectDistinctTypes();
            if (types.Count == 0) return;

#if ZENECS_ZENJECT
            var instances = ZenEcsUnityBridge.SystemPresetResolver?.InstantiateSystems(types);
#else
            var instances = InstantiateSystemsActivator(types);
#endif
            if (instances.Count > 0)
                _world.AddSystems(instances); // 다음 BeginFrame에 반영
        }

        // ───────────────────────────────── helpers ─────────────────────────────────
        private IWorld ResolveWorld()
        {
            if (useCurrentWorld) return KernelLocator.CurrentWorld;
            if (!string.IsNullOrWhiteSpace(worldName))
                return KernelLocator.EnsureWorld(worldName, setAsCurrent: setAsCurrentOnEnsure);
            return KernelLocator.CurrentWorld;
        }

        /// <summary>Preset + Local을 합쳐 **항상 타입 중복 제거**.</summary>
        private List<Type> CollectDistinctTypes()
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            var list = new List<Type>();

            // 1) Preset
            if (systemsPreset != null)
            {
                foreach (var t in systemsPreset.GetValidTypes())
                    AddDistinct(t, set, list);
            }

            // 2) Local (SystemTypeRef[])
            if (systemTypes != null)
            {
                foreach (var r in systemTypes)
                {
                    var t = r.Resolve();
                    AddDistinct(t, set, list);
                }
            }

            return list;

            static void AddDistinct(Type? t, HashSet<string> seen, List<Type> dst)
            {
                if (t == null || t.IsAbstract || !typeof(ISystem).IsAssignableFrom(t)) return;
                var key = t.AssemblyQualifiedName ?? t.FullName;
                if (string.IsNullOrEmpty(key)) return;
                if (seen.Add(key)) dst.Add(t);
            }
        }

#if ZENECS_ZENJECT
        private static List<ISystem> InstantiateSystemsZenject(List<Type> types, DiContainer c)
        {
            var list = new List<ISystem>(types.Count);
            foreach (var t in types)
            {
                try { list.Add((ISystem)c.Instantiate(t)); }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WorldSetupInstaller] DI instantiate failed: {t?.Name} — {ex.Message}");
                }
            }
            return list;
        }
#else
        private static List<ISystem> InstantiateSystemsActivator(List<Type> types)
        {
            var list = new List<ISystem>(types.Count);
            foreach (var t in types)
            {
                try { list.Add((ISystem)Activator.CreateInstance(t)); }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WorldSetupInstaller] new() failed: {t?.Name} — {ex.Message}");
                }
            }
            return list;
        }
#endif
    }
}
