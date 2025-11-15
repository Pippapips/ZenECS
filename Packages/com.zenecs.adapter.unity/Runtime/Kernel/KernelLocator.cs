#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using ZenECS.Core;
#if ZENECS_ZENJECT
using Zenject;
#endif

namespace ZenECS.Adapter.Unity
{
    /// <summary>
    /// Global-access gateway for IKernel / CurrentWorld.
    /// - Zenject provider가 있으면 우선 사용
    /// - 씬의 EcsDriver가 있으면 그것 사용
    /// - 없으면 자동 생성(DontDestroyOnLoad)
    /// </summary>
    public static class KernelLocator
    {
        private static IKernel? _cached;
        
        public static IKernel Current
        {
            get
            {
                if (_cached != null) return _cached;

#if UNITY_2022_2_OR_NEWER
                var drv = UnityEngine.Object.FindFirstObjectByType<EcsDriver>(FindObjectsInactive.Include);
#else
                var drv = UnityEngine.Object.FindObjectOfType<EcsDriver>(true);
#endif
                if (drv != null && drv.Kernel != null)
                    return _cached = drv.Kernel;

                return _cached ??= drv?.Kernel!;
            }
        }

        public static EcsDriver CreateEcsDriver(bool dontDestroyOnLoad = true)
        {
#if UNITY_2022_2_OR_NEWER
            var drv = UnityEngine.Object.FindFirstObjectByType<EcsDriver>(FindObjectsInactive.Include);
#else
            var drv = UnityEngine.Object.FindObjectOfType<EcsDriver>(true);
#endif
            if (drv == null)
            {
                var go = new GameObject("[ZenECS] EcsDriver (auto)");
                drv = go.AddComponent<EcsDriver>();
            }

            if (dontDestroyOnLoad)
            {
                UnityEngine.Object.DontDestroyOnLoad(drv.gameObject);
            }

            return drv;
        }

        public static IKernel CreateEcsDriverWithKernel(bool dontDestroyOnLoad = true)
        {
            return CreateEcsDriver(dontDestroyOnLoad).CreateKernel();
        }

        public static IWorld CurrentWorld =>
            Current.CurrentWorld ?? throw new InvalidOperationException("Kernel.CurrentWorld is null");

        internal static void Attach(IKernel k) => _cached = k;

        internal static void Detach(IKernel k)
        {
            if (ReferenceEquals(_cached, k)) _cached = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetOnDomainReload() => _cached = null;

        // ──────────────────────────────────────────────
        // Multi-world helpers (strongly typed)
        // ──────────────────────────────────────────────

        /// <summary>Try get a world by unique id.</summary>
        public static bool TryGetWorldById(WorldId id, out IWorld world)
            => Current.TryGet(id, out world);

        /// <summary>Find worlds by exact name (kernel-side matching, may return multiple).</summary>
        public static IEnumerable<IWorld> FindByName(string name)
            => string.IsNullOrWhiteSpace(name) ? Enumerable.Empty<IWorld>() : Current.FindByName(name);

        /// <summary>Find worlds whose names start with prefix (case-insensitive).</summary>
        public static IEnumerable<IWorld> FindByNamePrefix(string prefix)
            => prefix is null ? Enumerable.Empty<IWorld>() : Current.FindByNamePrefix(prefix);

        /// <summary>Try get a single world by exact name (first match).</summary>
        public static bool TryGetWorldByName(string name, out IWorld world)
        {
            world = FindByName(name).FirstOrDefault();
            return world != null;
        }

        /// <summary>Set the given world as current.</summary>
        public static bool SetCurrentWorld(IWorld w)
        {
            if (w == null) return false;
            try
            {
                Current.SetCurrentWorld(w);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[KernelLocator] Failed to set current world: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Ensure a world exists by name; if missing, create it and optionally set as current.
        /// </summary>
        public static IWorld EnsureWorld(string name, bool setAsCurrent = true)
        {
            if (TryGetWorldByName(name, out var w))
            {
                if (setAsCurrent) SetCurrentWorld(w);
                return w;
            }

            return Current.CreateWorld(cfg: null, name: name, tags: null, presetId: null, setAsCurrent: setAsCurrent);
        }

        // ──────────────────────────────────────────────
        // 🔖 Tag helpers
        // ─────────────────────────────────────────────-

        /// <summary>Find worlds that contain the given tag (case-insensitive, kernel-provided).</summary>
        public static IEnumerable<IWorld> FindByTag(string tag)
            => string.IsNullOrWhiteSpace(tag) ? Enumerable.Empty<IWorld>() : Current.FindByTag(tag);

        /// <summary>Find worlds that match any of the provided tags (OR semantics).</summary>
        public static IEnumerable<IWorld> FindByAnyTag(params string[] tags)
            => (tags == null || tags.Length == 0) ? Enumerable.Empty<IWorld>() : Current.FindByAnyTag(tags);

        /// <summary>Find worlds that contain all provided tags (AND semantics).</summary>
        public static IEnumerable<IWorld> FindByAllTags(params string[] tags)
        {
            if (tags == null || tags.Length == 0) return Enumerable.Empty<IWorld>();

            // AND = 교집합. 각 단일 태그 검색 결과를 WorldId 기준으로 교집합.
            HashSet<WorldId>? acc = null;
            Dictionary<WorldId, IWorld>? lastMap = null;

            foreach (var t in tags)
            {
                var list = Current.FindByTag(t) ?? Enumerable.Empty<IWorld>();
                var map = list.Where(w => w != null).ToDictionary(w => w.Id, w => w);
                if (acc == null)
                {
                    acc = new HashSet<WorldId>(map.Keys);
                    lastMap = map;
                }
                else
                {
                    acc.IntersectWith(map.Keys);
                    lastMap = map;
                }

                if (acc.Count == 0) break;
            }

            if (acc == null || acc.Count == 0 || lastMap == null) return Enumerable.Empty<IWorld>();
            // acc에는 모든 태그를 만족하는 Id들이 담겨있음. 마지막 map과 무관하게 Current에서 AllWorlds로 꺼내도 됨.
            var byId = AllWorldsById();
            return acc.Select(id => byId.TryGetValue(id, out var w) ? w : null).Where(w => w != null)!;
        }

        /// <summary>Try get a single world by tag (first match).</summary>
        public static bool TryGetWorldByTag(string tag, out IWorld world)
        {
            world = FindByTag(tag).FirstOrDefault();
            return world != null;
        }

        /// <summary>
        /// Ensure a world exists by name with tags; if missing, create with given tags.
        /// </summary>
        public static IWorld EnsureWorld(string name, IEnumerable<string> tags, bool setCurrent = true)
        {
            if (TryGetWorldByName(name, out var w))
            {
                if (setCurrent) SetCurrentWorld(w);
                return w;
            }

            return Current.CreateWorld(cfg: null, name: name, tags: tags, presetId: null, setAsCurrent: setCurrent);
        }

        // ──────────────────────────────────────────────
        // 📚 Dictionary views
        // ─────────────────────────────────────────────-

        /// <summary>Snapshot dictionary of all worlds keyed by <see cref="WorldId"/>.</summary>
        public static IReadOnlyDictionary<WorldId, IWorld> AllWorldsById()
        {
            var dict = new Dictionary<WorldId, IWorld>();
            foreach (var w in AllWorlds)
                if (w != null)
                    dict[w.Id] = w;
            return dict;
        }

        /// <summary>
        /// Snapshot multi-map of worlds grouped by name.
        /// Key comparer는 기본적으로 대/소문자 무시(OrdinalIgnoreCase).
        /// </summary>
        public static IReadOnlyDictionary<string, List<IWorld>> AllWorldsByName(bool ignoreCase = true)
        {
            var cmp = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var dict = new Dictionary<string, List<IWorld>>(cmp);
            foreach (var w in AllWorlds)
            {
                if (w == null) continue;
                var name = w.Name ?? string.Empty;
                if (!dict.TryGetValue(name, out var list)) dict[name] = list = new List<IWorld>();
                list.Add(w);
            }

            return dict;
        }

        /// <summary>Enumerate all worlds known to the kernel (prefix "" == all).</summary>
        public static IEnumerable<IWorld> AllWorlds =>
            Current.FindByNamePrefix(string.Empty).Where(w => w != null);
    }
}