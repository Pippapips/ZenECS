// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenEcsAutoDefines.cs
// Purpose: Automatic preprocessor definition manager that detects and applies
//          scripting define symbols (ZENECS_ZENJECT, ZENECS_UNIRX) based on
//          installed packages.
// Key concepts:
//   • Auto-detection: scans assemblies for Zenject/UniRx presence.
//   • Define management: adds/removes symbols for all build target groups.
//   • Event-driven: runs on domain reload, compilation, and asset changes.
//   • Editor-only: compiled out in player builds via #if UNITY_EDITOR.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
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

namespace ZenECS.Adapter.Unity.Editor.Setup
{
    /// <summary>
    /// Automatic preprocessor definition manager
    /// - When Zenject is installed: ZENECS_ZENJECT
    /// - When UniRx is installed: ZENECS_UNIRX
    ///
    /// Execution timing
    /// - After domain reload / editor start / script recompilation
    /// - After asset changes (scripts/packages)
    /// - Menu: ZenECS/Tools/Defines/Rescan & Apply
    /// </summary>
    [InitializeOnLoad]
    public static class ZenEcsAutoDefines
    {
        // Target symbols to detect
        private const string SYM_ZENJECT = "ZENECS_ZENJECT";
        private const string SYM_UNIRX   = "ZENECS_UNIRX";

        private const string MENU_RESYNC = "ZenECS/Tools/Defines/Rescan & Apply";

        static ZenEcsAutoDefines()
        {
            // Execute one frame after editor initialization (assembly load stabilization)
            EditorApplication.update += DelayedOnce;
            AssemblyReloadEvents.afterAssemblyReload += SafeApply;
            CompilationPipeline.compilationFinished += _ => SafeApply();
        }

        private static void DelayedOnce()
        {
            EditorApplication.update -= DelayedOnce;
            SafeApply();
        }

        // Manual execution from menu
        [MenuItem(MENU_RESYNC)]
        public static void MenuRescanAndApply()
        {
            SafeApply(true);
            Debug.Log("[ZenECS] Defines rescan finished.");
        }

        // Menu checkbox (for status display)
        [MenuItem(MENU_RESYNC, true)]
        private static bool MenuValidate()
        {
            var detected = Detect();
            var (hasZenject, hasUniRx) = (detected.HasZenject, detected.HasUniRx);

            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
#if UNITY_2021_2_OR_NEWER
            if (!TryGetNamedBuildTarget(group, out var nbt))
            {
                Menu.SetChecked(MENU_RESYNC, false);
                return true;
            }
            var defines = GetDefines(nbt);
#else
            var defines = GetDefinesLegacy(group);
#endif

            // Simple status output
            bool ok =
                (!hasZenject || defines.Contains(SYM_ZENJECT)) &&
                (!hasUniRx   || defines.Contains(SYM_UNIRX));

            Menu.SetChecked(MENU_RESYNC, ok);
            return true;
        }

        /// <summary>Hook for automatic reapplication on package/assembly changes</summary>
        private sealed class AutoPostprocessor : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(
                string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                // Quick check only for script/package related changes
                bool touched = importedAssets.Concat(deletedAssets).Concat(movedAssets).Any(path =>
                    path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase) ||
                    path.Contains("Packages/"));

                if (touched) SafeApply();
            }
        }

        /// <summary>Exception protection wrapper</summary>

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

        /// <summary>Detect if Zenject/UniRx exists in current domain</summary>
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
                return false; // Legacy groups like WebPlayer are filtered here
            }
        }

        private static IEnumerable<BuildTargetGroup> EnumerateValidGroups()
        {
            return Enum.GetValues(typeof(BuildTargetGroup))
                .Cast<BuildTargetGroup>()
                .Where(g => g != BuildTargetGroup.Unknown)
                .Where(g => TryGetNamedBuildTarget(g, out _)); // Only valid ones
        }
#endif
        
        /// <summary>Apply symbols for all build target groups</summary>
        private static void ApplyDefines(bool logDetails)
        {
            var detected = Detect();

#if UNITY_2021_2_OR_NEWER
            foreach (var group in EnumerateValidGroups())
            {
                if (!TryGetNamedBuildTarget(group, out var nbt))
                    continue; // Safety mechanism (double filter)

                var list = GetDefines(nbt);

                list = Update(list, SYM_ZENJECT, detected.HasZenject);
                list = Update(list, SYM_UNIRX,  detected.HasUniRx);

                SetDefines(nbt, list);

                if (logDetails)
                    Debug.Log($"[ZenECS] ({group}) defines = {string.Join(";", list)}");
            }
#else
            // Legacy versions remain as before
            var targetGroups = Enum.GetValues(typeof(BuildTargetGroup))
                                   .Cast<BuildTargetGroup>()
                                   .Where(g => g != BuildTargetGroup.Unknown);

            foreach (var group in targetGroups)
            {
                var list = GetDefinesLegacy(group);
                list = Update(list, SYM_ZENJECT, detected.HasZenject);
                list = Update(list, SYM_UNIRX,  detected.HasUniRx);
                SetDefinesLegacy(group, list);
                if (logDetails)
                    Debug.Log($"[ZenECS] ({group}) defines = {string.Join(";", list)}");
            }
#endif

            MenuValidate();
        }

        // Utility for updating define list
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

        // ── Legacy version compatibility ─────────────────────────────────────────────────────
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
