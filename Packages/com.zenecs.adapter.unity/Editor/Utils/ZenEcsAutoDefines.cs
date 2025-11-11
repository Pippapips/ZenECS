#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build; // NamedBuildTarget
#endif
using UnityEditor.Compilation;
using UnityEngine;

namespace ZenECS.EditorUtils
{
    /// <summary>
    /// 자동 전처리기 정의 관리기
    /// - Zenject 설치 시: ZENECS_ZENJECT
    /// - UniRx  설치 시: ZENECS_UNIRX
    ///
    /// 동작 시점
    /// - 도메인 리로드 / 에디터 시작 / 스크립트 리컴파일 이후
    /// - 에셋 변경(스크립트/패키지) 이후
    /// - 메뉴: ZenECS/Tools/Defines/Rescan & Apply
    /// </summary>
    [InitializeOnLoad]
    public static class ZenEcsAutoDefines
    {
        // 감지 대상 심볼
        private const string SYM_ZENJECT = "ZENECS_ZENJECT";
        private const string SYM_UNIRX   = "ZENECS_UNIRX";

        private const string MENU_RESYNC = "ZenECS/Tools/Defines/Rescan & Apply";

        static ZenEcsAutoDefines()
        {
            // 에디터 초기화 후 한 프레임 뒤에 실행(어셈블리 로드 안정화)
            EditorApplication.update += DelayedOnce;
            AssemblyReloadEvents.afterAssemblyReload += SafeApply;
            CompilationPipeline.compilationFinished += _ => SafeApply();
        }

        private static void AssemblyReloadEventsOnafterAssemblyReload()
        {
            throw new NotImplementedException();
        }

        private static void DelayedOnce()
        {
            EditorApplication.update -= DelayedOnce;
            SafeApply();
        }

        // 메뉴에서 수동 실행
        [MenuItem(MENU_RESYNC)]
        public static void MenuRescanAndApply()
        {
            SafeApply(true);
            Debug.Log("[ZenECS] Defines rescan finished.");
        }

        // 메뉴 체크박스(상태 표시용)
        [MenuItem(MENU_RESYNC, true)]
        private static bool MenuValidate()
        {
            var detected = Detect();
            var (hasZenject, hasUniRx) = (detected.HasZenject, detected.HasUniRx);

            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = GetDefines(NamedBuildTarget.FromBuildTargetGroup(group));

            // 간단한 상태 출력
            bool ok =
                (!hasZenject || defines.Contains(SYM_ZENJECT)) &&
                (!hasUniRx   || defines.Contains(SYM_UNIRX));

            Menu.SetChecked(MENU_RESYNC, ok);
            return true;
        }

        /// <summary>패키지/어셈블리 변경 시 자동 재적용용 훅</summary>
        private sealed class AutoPostprocessor : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(
                string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                // 스크립트/패키지 관련 변경만 빠르게 검사
                bool touched = importedAssets.Concat(deletedAssets).Concat(movedAssets).Any(path =>
                    path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase) ||
                    path.Contains("Packages/"));

                if (touched) SafeApply();
            }
        }

        /// <summary>예외 보호 래퍼</summary>

        private static void SafeApply()
        {
            SafeApply(false);
        }
        private static void SafeApply(bool logDetails = false)
        {
            try { ApplyDefines(logDetails); }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ZenECS] Auto defines apply failed: {ex.Message}");
            }
        }

        /// <summary>현재 도메인에서 Zenject/UniRx가 있는지 감지</summary>
        private static (bool HasZenject, bool HasUniRx) Detect()
        {
            bool hasZenject = HasType("Zenject.SignalBus") ||
                              HasType("Zenject.DiContainer") ||
                              HasAssemblyNamedLike("Zenject");

            bool hasUniRx   = HasType("UniRx.Unit") ||
                              HasType("UniRx.Subject`1") ||
                              HasAssemblyNamedLike("UniRx");

            return (hasZenject, hasUniRx);

            static bool HasType(string fullName)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try { if (asm.GetType(fullName, throwOnError: false) != null) return true; }
                    catch { /* ignore */ }
                }
                return false;
            }

            static bool HasAssemblyNamedLike(string keyword)
            {
                keyword = keyword.ToLowerInvariant();
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var name = asm.GetName().Name;
                    if (string.IsNullOrEmpty(name)) continue;
                    if (name.ToLowerInvariant().Contains(keyword)) return true;
                }
                return false;
            }
        }

#if UNITY_2021_2_OR_NEWER
        private static bool TryGetNamedBuildTarget(BuildTargetGroup group, out NamedBuildTarget nbt)
        {
            try
            {
                nbt = NamedBuildTarget.FromBuildTargetGroup(group);
                return true;
            }
            catch
            {
                nbt = default;
                return false; // WebPlayer 등 구식 그룹은 여기서 걸러짐
            }
        }

        private static IEnumerable<BuildTargetGroup> EnumerateValidGroups()
        {
            return Enum.GetValues(typeof(BuildTargetGroup))
                .Cast<BuildTargetGroup>()
                .Where(g => g != BuildTargetGroup.Unknown)
                .Where(g => TryGetNamedBuildTarget(g, out _)); // 유효한 것만
        }
#endif
        
        /// <summary>모든 빌드 타겟 그룹에 대해 심볼을 적용</summary>
        private static void ApplyDefines(bool logDetails)
        {
            var detected = Detect();

#if UNITY_2021_2_OR_NEWER
            foreach (var group in EnumerateValidGroups())
            {
                if (!TryGetNamedBuildTarget(group, out var nbt))
                    continue; // 안전장치(이중 필터)

                var list = GetDefines(nbt);

                list = Update(list, SYM_ZENJECT, detected.HasZenject);
                list = Update(list, SYM_UNIRX,  detected.HasUniRx);

                SetDefines(nbt, list);

                if (logDetails)
                    Debug.Log($"[ZenECS] ({group}) defines = {string.Join(";", list)}");
            }
#else
    // 구버전은 기존대로
    var targetGroups = Enum.GetValues(typeof(BuildTargetGroup))
                           .Cast<BuildTargetGroup>()
                           .Where(g => g != BuildTargetGroup.Unknown);

    foreach (var group in targetGroups)
    {
        var list = GetDefinesLegacy(group);
        list = Update(list, SYM_ZENJECT, detected.HasZenject);
        list = Update(list, SYM_UNIRX,  detected.HasUniRx);
        list = Update(list, SYM_ZEN_SIG, detected.HasUniRx);
        SetDefinesLegacy(group, list);
        if (logDetails)
            Debug.Log($"[ZenECS] ({group}) defines = {string.Join(";", list)}");
    }
#endif

            MenuValidate();
        }

        // define 리스트 갱신 유틸
        private static List<string> Update(List<string> current, string sym, bool shouldExist)
        {
            bool has = current.Contains(sym);
            if (shouldExist && !has) current.Add(sym);
            else if (!shouldExist && has) current.Remove(sym);
            return current;
        }

        // ── Unity 2021.2+ : NamedBuildTarget API ────────────────────────────
        
#if UNITY_2021_2_OR_NEWER
        private static List<string> GetDefines(NamedBuildTarget nbt)
        {
            var raw = PlayerSettings.GetScriptingDefineSymbols(nbt);
            return SplitDefines(raw);
        }

        private static void SetDefines(NamedBuildTarget nbt, List<string> list)
        {
            PlayerSettings.SetScriptingDefineSymbols(nbt, string.Join(";", list));
        }
#else
        private static List<string> GetDefines(BuildTargetGroup group)
        {
            return GetDefinesLegacy(group);
        }

        private static void SetDefines(BuildTargetGroup group, List<string> list)
        {
            SetDefinesLegacy(group, list);
        }

        // ── 구버전 호환 ─────────────────────────────────────────────────────
        private static List<string> GetDefinesLegacy(BuildTargetGroup group)
        {
            var raw = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            return SplitDefines(raw);
        }

        private static void SetDefinesLegacy(BuildTargetGroup group, List<string> list)
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", list));
        }
#endif

        private static List<string> SplitDefines(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
            return raw.Split(';')
                      .Select(s => s.Trim())
                      .Where(s => !string.IsNullOrEmpty(s))
                      .Distinct(StringComparer.Ordinal)
                      .ToList();
        }
    }
}
#endif
