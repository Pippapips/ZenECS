#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using ZenECS.Adapter.Unity.DI;
using ZenECS.Core;
using ZenECS.Core.Systems;
using ZenECS.Adapter.Unity.Util; // ← WorldInstaller 사용
#if ZENECS_ZENJECT
using Zenject;
#endif

namespace ZenECS.Adapter.Unity.Install
{
#if ZENECS_ZENJECT
    public sealed class WorldSetupInstaller : MonoInstaller, IInitializable, IDisposable
#else
    public sealed class WorldSetupInstaller : MonoBehaviour
#endif
    {
        [Header("World")]
        public string _worldName = "Game";
        public bool _useCurrentWorld = true;
        public bool _setAsCurrentOnEnsure = true;

        [Header("Systems")]
        [Tooltip("등록할 ISystem 구현 타입들(런타임 안전)")]
        [SystemTypeFilter(typeof(ZenECS.Core.Systems.ISystem), allowAbstract:false)]
        public SystemTypeRef[]? _systemTypes;
        public SystemsPreset? _systemsPreset;
        public bool _skipIfTypeAlreadyExists = true;
        public bool _removeOnDispose = true;

        private IWorld? _world;
        private readonly List<ISystem> _created = new();

#if ZENECS_ZENJECT
        public override void InstallBindings()
        {
            _world = ResolveWorld();

            // ★ 통합 포인트: IWorld/EntityViewRegistry를 DI로도 사용 가능하게 바인딩
            WorldInstaller.Install(Container, _world);

            CreateSystemsViaZenject(Container, _created);
            if (_created.Count > 0) _world.AddSystems(_created);

            Container.BindInterfacesTo<WorldSetupInstaller>().FromInstance(this).AsSingle();
        }
        public void Initialize() { }
        public void Dispose() { if (_removeOnDispose) TryRemoveAll(); }
#else
        void Awake()
        {
            _world = ResolveWorld();
            CreateSystemsViaActivator(_created);
            if (_created.Count > 0) _world.AddSystems(_created);
        }
        void OnDisable() { if (_removeOnDispose) TryRemoveAll(); }
#endif

#if !ZENECS_ZENJECT
        void OnValidate() => CleanupInvalids();
#else
        void OnValidate() { CleanupInvalids(); }
#endif

        private void CleanupInvalids()
        {
            if (_systemTypes == null || _systemTypes.Length == 0) return;

            var list = new List<SystemTypeRef>(_systemTypes.Length);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var r in _systemTypes)
            {
                var aqn = r.AssemblyQualifiedName;

                // ✅ 빈 슬롯은 보존
                if (string.IsNullOrWhiteSpace(aqn))
                {
                    list.Add(r);
                    continue;
                }

                // ✅ 값이 있는 경우만 검사
                var t = r.Resolve();
                if (t == null || t.IsAbstract) continue;
                if (!typeof(ZenECS.Core.Systems.ISystem).IsAssignableFrom(t)) continue;
                if (!seen.Add(aqn)) continue;

                list.Add(r);
            }

            _systemTypes = list.ToArray();
        }
        
        // ───────────────────────────────── helpers ─────────────────────────────────
        private IWorld ResolveWorld()
        {
            if (_useCurrentWorld) return KernelLocator.CurrentWorld;
            if (!string.IsNullOrWhiteSpace(_worldName))
                return KernelLocator.EnsureWorld(_worldName, setAsCurrent: _setAsCurrentOnEnsure);
            return KernelLocator.CurrentWorld;
        }

        private IEnumerable<Type> EnumerateSystemTypes()
        {
            if (_systemsPreset != null)
                foreach (var t in _systemsPreset.GetValidTypes())
                    yield return t;

            if (_systemTypes == null) yield break;

            foreach (var tr in _systemTypes)
            {
                var t = tr.Resolve();                          // ← 여기!
                if (t == null || t.IsAbstract) continue;
                if (!typeof(ISystem).IsAssignableFrom(t)) continue;
                yield return t;
            }
        }
#if ZENECS_ZENJECT
        private void CreateSystemsViaZenject(DiContainer c, List<ISystem> outList)
        {
            outList.Clear();
            foreach (var t in EnumerateSystemTypes())
            {
                if (_skipIfTypeAlreadyExists && SafeHasSystemType(_world, t)) continue;
                var sys = (ISystem)c.Instantiate(t);   // DI 주입
                outList.Add(sys);
            }
        }
#else
        private void CreateSystemsViaActivator(List<ISystem> outList)
        {
            outList.Clear();
            foreach (var t in EnumerateSystemTypes())
            {
                if (_skipIfTypeAlreadyExists && _world != null && SafeHasSystemType(_world, t)) continue;
                try
                {
                    var sys = (ISystem)Activator.CreateInstance(t);   // 기본 생성자
                    outList.Add(sys);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WorldSetupInstaller] {t.Name} 생성 실패(기본 생성자 필요): {ex.Message}");
                }
            }
        }
#endif

        private void TryRemoveAll()
        {
            if (_world == null || _created.Count == 0) return;
            foreach (var s in _created)
            {
                if (s == null) continue;
                try { _world.RemoveSystem(s.GetType()); }
                catch (Exception ex) { Debug.LogWarning($"[WorldSetupInstaller] RemoveSystem 실패: {ex.Message}"); }
            }
            _created.Clear();
        }

        private static bool SafeHasSystemType(IWorldSystemsApi? api, Type t)
        {
            if (api == null) return false;
            try { return api.TryGetSystem(t, out _) || api.IsEnabledSystem(t); }
            catch { return false; }
        }
    }
}
