// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenEcsStableIdAndFormatterCodegen.cs
// Purpose: Automatic code generation for ZenComponent StableId registration and
//          IComponentFormatter registration based on ZenFormatterForAttribute.
// Key concepts:
//   • Component registration: ComponentRegistry.Register<T>(stableId).
//   • Formatter registration: ComponentRegistry.RegisterFormatter(new Formatter()).
//   • Formatter selection: priority-based resolution (IsLatest > version > name).
//   • Build-time generation: runs on build and assembly reload.
//   • Editor-only: compiled out in player builds via #if UNITY_EDITOR.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using PackageSource = UnityEditor.PackageManager.PackageSource;

namespace ZenECS.Adapter.Unity.Editor.Codegen
{
    /// <summary>
    /// Automatic registration g.cs codegen for ZenComponent(StableId) + IComponentFormatter (ZenFormatterForAttribute extension).
    /// - Component: ComponentRegistry.Register&lt;T&gt;(stableId)
    /// - Formatter: ComponentRegistry.RegisterFormatter(new Formatter())
    /// - Formatter selection priority: [Attribute IsLatest] &gt; [StableId .vN max] &gt; [ClassName V(\d+) max] &gt; [Same assembly] &gt; [Name order]
    /// - Maintains batch/cleanup/Verbose/Custom Output rules
    /// - When isLatest duplicates (2 or more for same component): warning/exception based on menu toggle
    /// </summary>
    [InitializeOnLoad]
    public sealed class ZenEcsStableIdAndFormatterCodegen : IPreprocessBuildWithReport
    {
        // ===== Fixed settings =====
        const string GENERATED_NAMESPACE = "ZenECS.Codegen.Registry";
        const int ROOT_NS_DEPTH = 2;
        const string CUSTOM_OUTPUT_PREF = "ZenECS.Codegen.CustomOutputRootAssetPath";
        const string FILE_MARKER = "// ZENECS_STABLEID_CODEGEN";

        // Whether to block build with throw when duplicates are found (convenience toggle)
        const string FAIL_ON_LATEST_DUP_PREF = "ZenECS.Codegen.FailOnLatestDuplicate";

        // Build failure if latest formatter's StableId differs from component StableId (recommended: ON)
        const string STRICT_STABLEID_MATCH_PREF = "ZenECS.Codegen.StrictStableIdMatch";

        // SDK FQN (for string fallback)
        const string IFMT_OPEN_GENERIC_FQN = "ZenECS.Core.Serialization.IComponentFormatter`1";
        const string IFMT_NONGENERIC_FQN = "ZenECS.Core.Serialization.IComponentFormatter";
        const string ATTR_FORMATTER_FOR_FQN = "ZenECS.Core.Serialization.ZenFormatterForAttribute";
        const string IPOSTLOAD_FQN = "ZenECS.Core.Serialization.IPostLoadMigration";

        // ===== Verbosity =====
        const string VERBOSITY_PREF = "ZenECS.Codegen.Verbosity";
        enum Verbosity
        {
            Quiet = 0,
            Normal = 1,
            Verbose = 2
        }
        static Verbosity CurrentVerbosity => (Verbosity)EditorPrefs.GetInt(VERBOSITY_PREF, (int)Verbosity.Normal);
        static void LogVerbose(string msg)
        {
            if (CurrentVerbosity >= Verbosity.Verbose) Debug.Log(msg);
        }
        static void LogInfo(string msg)
        {
            if (CurrentVerbosity >= Verbosity.Normal) Debug.Log(msg);
        }
        static void LogWarn(string msg) => Debug.LogWarning(msg);

        [MenuItem("ZenECS/Tools/StableId Codegen/Verbosity/Quiet")]
        public static void VQ()
        {
            EditorPrefs.SetInt(VERBOSITY_PREF, (int)Verbosity.Quiet);
            Debug.Log("[ZenECS] Verbosity = Quiet");
        }
        [MenuItem("ZenECS/Tools/StableId Codegen/Verbosity/Normal")]
        public static void VN()
        {
            EditorPrefs.SetInt(VERBOSITY_PREF, (int)Verbosity.Normal);
            Debug.Log("[ZenECS] Verbosity = Normal");
        }
        [MenuItem("ZenECS/Tools/StableId Codegen/Verbosity/Verbose")]
        public static void VV()
        {
            EditorPrefs.SetInt(VERBOSITY_PREF, (int)Verbosity.Verbose);
            Debug.Log("[ZenECS] Verbosity = Verbose");
        }

        [MenuItem("ZenECS/Tools/StableId Codegen/Validation/Fail On Latest Duplicate: Toggle")]
        static void ToggleFailOnLatestDup()
        {
            var cur = EditorPrefs.GetBool(FAIL_ON_LATEST_DUP_PREF, true);
            EditorPrefs.SetBool(FAIL_ON_LATEST_DUP_PREF, !cur);
            Debug.Log($"[ZenECS] FailOnLatestDuplicate = {!cur}");
        }

        [MenuItem("ZenECS/Tools/StableId Codegen/Validation/Strict StableId Match: Toggle")]
        static void ToggleStrictStableIdMatch()
        {
            var cur = EditorPrefs.GetBool(STRICT_STABLEID_MATCH_PREF, true);
            EditorPrefs.SetBool(STRICT_STABLEID_MATCH_PREF, !cur);
            Debug.Log($"[ZenECS] StrictStableIdMatch = {!cur}");
        }

        [MenuItem("ZenECS/Tools/StableId Codegen/Debug/Print isLatest map")]
        static void DebugPrintLatest()
        {
            try
            {
                int warn;
                var fmts = ScanFormattersWithAttribute(new Dictionary<Type, string>(), out warn);
                var chosen = ChooseFormattersPerComponent_WithAttribute(fmts.Where(f => f.ComponentType != null).ToList());
                var lines = new List<string>();
                foreach (var g in chosen.Where(c => c.ComponentType != null).GroupBy(c => c.ComponentType))
                {
                    lines.Add($"[Latest] {g.Key.FullName}: {g.First().FormatterType.FullName}");
                }
                Debug.Log(string.Join("\n", lines));
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        // Reentry guard
        static bool _busy;
        const string BusyKey = "ZenECS_Codegen_Busy";

        // asmdef index
        static Dictionary<string, string> _asmdefPathByAsmName;

        // Reflection cache (robust Attribute/Interface detection)
        static Type _fmtAttrType;
        static Type _ifmtNonGenericType;
        static Type _ifmtOpenGenericType;
        static Type _ipostLoadType;

        static ZenEcsStableIdAndFormatterCodegen()
        {
            EditorApplication.delayCall += GenerateSafe;
        }

        [MenuItem("ZenECS/Tools/StableId Codegen/Generate")]
        public static void GenerateMenu() => GenerateSafe();

        [MenuItem("ZenECS/Tools/StableId Codegen/Set Custom Output Root (Assets/…)")]
        public static void SetCustomOutputRoot()
        {
            var abs = EditorUtility.OpenFolderPanel("Select output root (under Assets)", Application.dataPath, "ZenECS.Generated");
            if (string.IsNullOrEmpty(abs)) return;
            abs = abs.Replace("\\", "/");
            var data = Application.dataPath.Replace("\\", "/");
            if (!abs.StartsWith(data, StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog("Invalid folder", "Choose a folder under Assets/.", "OK");
                return;
            }
            var assetPath = "Assets/" + abs.Substring(data.Length).TrimStart('/');
            EditorPrefs.SetString(CUSTOM_OUTPUT_PREF, assetPath);
            LogInfo($"[ZenECS] Custom output root set: {assetPath}");
            GenerateSafe();
        }

        [MenuItem("ZenECS/Tools/StableId Codegen/Clear Custom Output Root")]
        public static void ClearCustomOutputRoot()
        {
            var prev = EditorPrefs.GetString(CUSTOM_OUTPUT_PREF, null);
            EditorPrefs.DeleteKey(CUSTOM_OUTPUT_PREF);
            LogInfo("[ZenECS] Custom output root cleared.");
            if (!string.IsNullOrEmpty(prev))
                PurgeCustomRoot(prev);
            GenerateSafe();
        }

        public int callbackOrder => 0;
        public void OnPreprocessBuild(BuildReport report) => GenerateSafe();

        static void GenerateSafe()
        {
            if (_busy || SessionState.GetBool(BusyKey, false)) return;
            _busy = true;
            SessionState.SetBool(BusyKey, true);
            try
            {
                AssetDatabase.DisallowAutoRefresh();
                AssetDatabase.StartAssetEditing();
                try
                {
                    GenerateImpl();
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.AllowAutoRefresh();
                }
            }
            finally
            {
                _busy = false;
                SessionState.EraseBool(BusyKey);
            }
        }

        public static void GenerateImpl()
        {
            // === Component scan ===
            var compAttr = FindZenComponentAttributeType();
            if (compAttr == null)
            {
                LogWarn("[ZenECS] ZenComponentAttribute (ZenECS.Core) not found. Skipping generation.");
                return;
            }

            var compItems = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !a.FullName.StartsWith("UnityEditor", StringComparison.Ordinal))
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch
                    {
                        return Array.Empty<Type>();
                    }
                })
                .Where(t => t != null && t.IsValueType && t.IsPublic)
                .Select(t => TryGetStableId(t, compAttr, out var sid) ? new CompScan(sid, t, t.Assembly) : default(CompScan?))
                .Where(x => x.HasValue).Select(x => x.Value)
                .ToArray();

            var dupComp = compItems.GroupBy(x => x.StableId).FirstOrDefault(g => g.Count() > 1);
            if (dupComp != null) throw new Exception($"[ZenECS] Duplicate StableId (components): {dupComp.Key}");

            var stableByType = compItems.GroupBy(c => c.Type).ToDictionary(g => g.Key, g => g.First().StableId);

            // === Formatter scan (Attribute extension) ===
            var fmtItems = ScanFormattersWithAttribute(stableByType, out int warnCount);
            if (warnCount > 0) LogWarn($"[ZenECS] Formatter warnings: {warnCount} (see Console)");

            // Immediately after formatter scan: check for isLatest duplicates
            var fail = EditorPrefs.GetBool(FAIL_ON_LATEST_DUP_PREF, true);
            ValidateLatestUniqueness(fmtItems, fail);

            // Latest formatter ↔ component StableId consistency check (strictly recommended)
            var chosenAll = ChooseFormattersPerComponent_WithAttribute(fmtItems.Where(f => f.ComponentType != null).ToList());
            var strictSid = EditorPrefs.GetBool(STRICT_STABLEID_MATCH_PREF, true);
            ValidateLatestStableIdMatch(chosenAll, stableByType, strictSid);

            // === Migration scan ===
            var migItems = ScanPostLoadMigrations();

            // === Group construction ===
            var groups = BuildGroups(compItems, fmtItems, migItems);

            // === Calculate output root/keep ===
            var customRoot = EditorPrefs.GetString(CUSTOM_OUTPUT_PREF, null);
            if (!string.IsNullOrEmpty(customRoot) && !customRoot.StartsWith("Assets/", StringComparison.Ordinal))
            {
                LogWarn($"[ZenECS] Custom output root must start with 'Assets/'. Ignored: {customRoot}");
                customRoot = null;
            }

            var internalKeepPrefixes = new HashSet<string>(StringComparer.Ordinal);
            foreach (var g in groups.Where(g => g.IsZenEcsInternal))
            {
                ResolveOutput(g, customRoot, out string assetBase, out _);
                internalKeepPrefixes.Add(Norm(assetBase) + "/");
            }

            var keepRoots = new HashSet<string>(StringComparer.Ordinal);
            foreach (var g in groups)
            {
                ResolveOutput(g, customRoot, out string assetBase, out _);
                var serRoot = FindSerializationRoot(assetBase);
                keepRoots.Add(Norm(serRoot ?? assetBase).TrimEnd('/') + "/");
            }

            if (!string.IsNullOrEmpty(customRoot))
                CleanStaleUserOutputs(customRoot, internalKeepPrefixes, keepRoots);

            // === Generation ===
            int totalComps = 0, totalFmts = 0, files = 0;

            foreach (var g in groups)
            {
                ResolveOutput(g, customRoot, out string assetBase, out string classSuffix);

                // a) Component registry
                if (g.Components.Count > 0)
                {
                    var cls = $"StableIdBootstrap_{classSuffix}";
                    var assetPath = $"{assetBase}/{cls}.g.cs".Replace("\\", "/");
                    var fsPath = ToFsPath(assetPath);

                    var sb = new StringBuilder(16 * 1024);
                    sb.AppendLine("// <auto-generated/> DO NOT EDIT");
                    sb.AppendLine(FILE_MARKER);
                    sb.AppendLine("using UnityEngine;");
                    sb.AppendLine("using ZenECS.Core.Serialization;");
                    sb.AppendLine($"namespace {GENERATED_NAMESPACE}");
                    sb.AppendLine("{");
                    sb.AppendLine($"  internal static class {cls}");
                    sb.AppendLine("  {");
                    sb.AppendLine("    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]");
                    sb.AppendLine("    private static void Register()");
                    sb.AppendLine("    {");
                    foreach (var x in g.Components.OrderBy(x => x.StableId, StringComparer.Ordinal))
                    {
                        var tn = (x.Type.FullName ?? x.Type.Name).Replace('+', '.');
                        var id = x.StableId.Replace("\"", "\\\"");
                        sb.AppendLine($"      ComponentRegistry.Register<{tn}>(\"{id}\");");
                        totalComps++;
                    }
                    sb.AppendLine("    }");
                    sb.AppendLine("  }");
                    sb.AppendLine("}");
                    WriteIfChangedAndImport(fsPath, assetPath, sb.ToString());
                    files++;
                    LogVerbose($"[ZenECS] Generated Component registry => {assetPath}");
                }

                // b) Formatter registry
                if (g.Formatters.Count > 0)
                {
                    var chosen = ChooseFormattersPerComponent_WithAttribute(g.Formatters);

                    var cls = $"FormatterBootstrap_{classSuffix}";
                    var assetPath = $"{assetBase}/{cls}.g.cs".Replace("\\", "/");
                    var fsPath = ToFsPath(assetPath);

                    var sb = new StringBuilder(16 * 1024);
                    sb.AppendLine("// <auto-generated/> DO NOT EDIT");
                    sb.AppendLine(FILE_MARKER);
                    sb.AppendLine("using UnityEngine;");
                    sb.AppendLine("using ZenECS.Core.Serialization;");
                    sb.AppendLine($"namespace {GENERATED_NAMESPACE}");
                    sb.AppendLine("{");
                    sb.AppendLine($"  internal static class {cls}");
                    sb.AppendLine("  {");
                    sb.AppendLine("    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]");
                    sb.AppendLine("    private static void Register()");
                    sb.AppendLine("    {");

                    // 3-1) Latest (for writing) registration
                    foreach (var f in chosen.OrderBy(f => f.ComponentType?.FullName ?? "", StringComparer.Ordinal)
                                 .ThenBy(f => f.FormatterType.FullName, StringComparer.Ordinal))
                    {
                        var fmtType = (f.FormatterType.FullName ?? f.FormatterType.Name).Replace('+', '.');
                        sb.AppendLine($"      // Latest-for-write");
                        sb.AppendLine($"      ComponentRegistry.RegisterFormatter(new {fmtType}());");
                        totalFmts++;
                    }

                    // 3-2) Legacy (read-only) registration: Attribute-based formatters where StableId differs from component StableId
                    var compSidMap = g.Components.ToDictionary(c => c.Type, c => c.StableId);
                    var latestSet = new HashSet<Type>(chosen.Select(c => c.FormatterType));
                    var legacy = g.Formatters
                        .Where(f => f.ComponentType != null
                                    && (!latestSet.Contains(f.FormatterType))
                                    && f.FromAttribute
                                    && !string.IsNullOrEmpty(f.StableIdFromAttribute)
                                    && compSidMap.TryGetValue(f.ComponentType, out var compSid)
                                    && !string.Equals(f.StableIdFromAttribute, compSid, StringComparison.Ordinal))
                        .OrderBy(f => f.ComponentType.FullName, StringComparer.Ordinal)
                        .ThenBy(f => f.FormatterType.FullName, StringComparer.Ordinal)
                        .ToList();
                    foreach (var f in legacy)
                    {
                        var fmtType = (f.FormatterType.FullName ?? f.FormatterType.Name).Replace('+', '.');
                        sb.AppendLine($"      // Legacy-readonly (StableId='{f.StableIdFromAttribute}')");
                        sb.AppendLine($"      ComponentRegistry.RegisterFormatter(new {fmtType}());");
                        totalFmts++;
                    }

                    sb.AppendLine("    }");
                    sb.AppendLine("  }");
                    sb.AppendLine("}");
                    WriteIfChangedAndImport(fsPath, assetPath, sb.ToString());
                    files++;
                    LogVerbose($"[ZenECS] Generated Formatter registry => {assetPath}");
                }

                // c) Post-Load Migration registry
                if (g.Migrations.Count > 0)
                {
                    var cls = $"MigrationBootstrap_{classSuffix}";
                    var assetPath = $"{assetBase}/{cls}.g.cs".Replace("\\", "/");
                    var fsPath = ToFsPath(assetPath);
                    var sb = new StringBuilder(8 * 1024);
                    sb.AppendLine("// <auto-generated/> DO NOT EDIT");
                    sb.AppendLine(FILE_MARKER);
                    sb.AppendLine("using UnityEngine;");
                    sb.AppendLine("using ZenECS.Core.Serialization;");
                    sb.AppendLine($"namespace {GENERATED_NAMESPACE}");
                    sb.AppendLine("{");
                    sb.AppendLine($"  internal static class {cls}");
                    sb.AppendLine("  {");
                    sb.AppendLine("    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]");
                    sb.AppendLine("    private static void Register()");
                    sb.AppendLine("    {");
                    foreach (var m in g.Migrations
                                 .OrderBy(x => x.Order ?? int.MaxValue) // If Order exists, use info order
                                 .ThenBy(x => x.MigrationType.FullName, StringComparer.Ordinal))
                    {
                        var mt = (m.MigrationType.FullName ?? m.MigrationType.Name).Replace('+', '.');
                        var comment = m.Order.HasValue ? $" // Order={m.Order.Value}" : "";
                        sb.AppendLine($"      PostLoadMigrationRegistry.Register(new {mt}());{comment}");
                    }
                    sb.AppendLine("    }");
                    sb.AppendLine("  }");
                    sb.AppendLine("}");
                    WriteIfChangedAndImport(fsPath, assetPath, sb.ToString());
                    files++;
                    LogVerbose($"[ZenECS] Generated Post-Load Migration registry => {assetPath}");
                }
            }

            LogVerbose($"[ZenECS] Codegen done. files={files}, components={totalComps}, formatters={totalFmts}");
        }

        // ===== Models =====
        readonly struct CompScan
        {
            public readonly string StableId;
            public readonly Type Type;
            public readonly Assembly Assembly;
            public CompScan(string id, Type t, Assembly asm)
            {
                StableId = id;
                Type = t;
                Assembly = asm;
            }
        }

        sealed class FmtScan
        {
            public Type FormatterType;
            public Type ComponentType;           // Estimated component type (may be absent)
            public bool FromAttribute;           // Whether Attribute-based metadata
            public bool IsLatest;                // Whether specified as latest by Attribute
            public int VersionHint;              // vN estimated from StableId/name (or -1 if absent)
            public string StableIdFromAttribute; // StableId from Attribute (for logging/validation)
            public Assembly Assembly => FormatterType.Assembly;
        }

        sealed class MigScan
        {
            public Type MigrationType;
            public Assembly Assembly => MigrationType.Assembly;
            public int? Order; // For information (not required for registration)
        }

        sealed class Group
        {
            public string AsmName;
            public string AsmdefAssetPath;
            public string RootNsKey;
            public bool IsZenEcsInternal;
            public readonly List<CompScan> Components = new();
            public readonly List<FmtScan> Formatters = new();
            public readonly List<MigScan> Migrations = new();
            public bool IsAsmdef => !string.IsNullOrEmpty(AsmdefAssetPath);
        }

        // ===== Group construction =====
        static List<Group> BuildGroups(CompScan[] comps, List<FmtScan> fmts, List<MigScan> migs = null)
        {
            var groups = new List<Group>();

            foreach (var byAsm in comps.GroupBy(x => x.Assembly))
            {
                var asm = byAsm.Key;
                var asmName = asm.GetName().Name ?? "Assembly";
                var asmdef = GetAsmdefAssetPath(asmName);

                if (!string.IsNullOrEmpty(asmdef))
                {
                    var g = new Group
                    {
                        AsmName = asmName,
                        AsmdefAssetPath = asmdef,
                        IsZenEcsInternal = IsZenEcsAssemblyOrTypes(byAsm)
                    };
                    g.Components.AddRange(byAsm);
                    groups.Add(g);
                }
                else
                {
                    foreach (var sub in byAsm.GroupBy(x => RootNamespaceKey(x.Type, ROOT_NS_DEPTH)))
                    {
                        var g = new Group
                        {
                            RootNsKey = sub.Key,
                            IsZenEcsInternal = IsZenEcsTypes(sub)
                        };
                        g.Components.AddRange(sub);
                        groups.Add(g);
                    }
                }
            }

            foreach (var f in fmts)
            {
                var comp = f.ComponentType;
                Group found = null;

                if (comp != null)
                {
                    var compAsm = comp.Assembly;
                    var compAsmName = compAsm.GetName().Name ?? "Assembly";
                    var compAsmdef = GetAsmdefAssetPath(compAsmName);

                    if (!string.IsNullOrEmpty(compAsmdef))
                    {
                        found = groups.FirstOrDefault(gg => gg.IsAsmdef && gg.AsmName == compAsmName);
                        if (found == null)
                        {
                            found = new Group { AsmName = compAsmName, AsmdefAssetPath = compAsmdef, IsZenEcsInternal = IsZenEcsFormatterInternal(f) };
                            groups.Add(found);
                        }
                    }
                    else
                    {
                        var key = RootNamespaceKey(comp, ROOT_NS_DEPTH);
                        found = groups.FirstOrDefault(gg => !gg.IsAsmdef && gg.RootNsKey == key);
                        if (found == null)
                        {
                            found = new Group { RootNsKey = key, IsZenEcsInternal = IsZenEcsFormatterInternal(f) };
                            groups.Add(found);
                        }
                    }
                }
                else
                {
                    var asmName = f.Assembly.GetName().Name ?? "Assembly";
                    var asmdef = GetAsmdefAssetPath(asmName);
                    if (!string.IsNullOrEmpty(asmdef))
                    {
                        found = groups.FirstOrDefault(gg => gg.IsAsmdef && gg.AsmName == asmName)
                                ?? new Group { AsmName = asmName, AsmdefAssetPath = asmdef, IsZenEcsInternal = IsZenEcsFormatterInternal(f) };
                        if (!groups.Contains(found)) groups.Add(found);
                    }
                    else
                    {
                        var key = "Global";
                        found = groups.FirstOrDefault(gg => !gg.IsAsmdef && gg.RootNsKey == key)
                                ?? new Group { RootNsKey = key, IsZenEcsInternal = IsZenEcsFormatterInternal(f) };
                        if (!groups.Contains(found)) groups.Add(found);
                    }
                }

                found.Formatters.Add(f);
            }

            // ===== Assign migrations to groups =====
            if (migs != null && migs.Count > 0)
            {
                foreach (var m in migs)
                {
                    Group found = null;
                    var asm = m.MigrationType.Assembly;
                    var asmName = asm.GetName().Name ?? "Assembly";
                    var asmdef = GetAsmdefAssetPath(asmName);
                    if (!string.IsNullOrEmpty(asmdef))
                    {
                        found = groups.FirstOrDefault(gg => gg.IsAsmdef && gg.AsmName == asmName)
                                ?? new Group
                                {
                                    AsmName = asmName, AsmdefAssetPath = asmdef,
                                    IsZenEcsInternal = (m.MigrationType.Namespace ?? "").StartsWith("ZenECS.", StringComparison.Ordinal)
                                };
                        if (!groups.Contains(found)) groups.Add(found);
                    }
                    else
                    {
                        var key = RootNamespaceKey(m.MigrationType, ROOT_NS_DEPTH);
                        found = groups.FirstOrDefault(gg => !gg.IsAsmdef && gg.RootNsKey == key)
                                ?? new Group { RootNsKey = key, IsZenEcsInternal = (m.MigrationType.Namespace ?? "").StartsWith("ZenECS.", StringComparison.Ordinal) };
                        if (!groups.Contains(found)) groups.Add(found);
                    }
                    found.Migrations.Add(m);
                }
            }

            return groups;
        }

        // ===== isLatest duplication validation =====
        static void ValidateLatestUniqueness(IEnumerable<FmtScan> fmtItems, bool failIfDuplicated)
        {
            var dups = fmtItems
                .Where(f => f.ComponentType != null && f.FromAttribute && f.IsLatest)
                .GroupBy(f => f.ComponentType)
                .Where(g => g.Count() > 1)
                .ToList();

            if (dups.Count == 0) return;

            foreach (var g in dups)
            {
                var comp = g.Key;
                var list = string.Join(", ", g.Select(x => x.FormatterType.FullName));
                var msg = $"[ZenECS] Multiple isLatest formatters for {comp.FullName}: {list}";
                if (failIfDuplicated) Debug.LogError(msg);
                else Debug.LogWarning(msg);
            }

            if (failIfDuplicated)
                throw new Exception("[ZenECS] isLatest duplication detected. Fix attributes so each component has exactly ONE isLatest.");
        }

        // Validates whether the StableId of the formatter selected as latest matches the component StableId.
        // If different: if strict=true, throws exception to stop build; if strict=false, warns.
        static void ValidateLatestStableIdMatch(IEnumerable<FmtScan> chosen, Dictionary<Type, string> stableByType, bool strict)
        {
            foreach (var f in chosen)
            {
                if (f.ComponentType == null) continue;
                if (!stableByType.TryGetValue(f.ComponentType, out var compSid) || string.IsNullOrEmpty(compSid))
                {
                    // Warn only if component has no StableId (abnormal case)
                    LogWarn($"[ZenECS] Component {f.ComponentType?.FullName} has no StableId. Formatter={f.FormatterType.FullName}");
                    continue;
                }
                // Warn if latest candidate without Attribute (recommended: explicitly specify with Attribute)
                if (!f.FromAttribute)
                {
                    var msg = $"[ZenECS] Latest formatter without [ZenFormatterFor] for {f.ComponentType.FullName}: {f.FormatterType.FullName}. " +
                              $"Add attribute with StableId='{compSid}' and IsLatest=true.";
                    if (strict) throw new Exception(msg);
                    else LogWarn(msg);
                    continue;
                }
                var fmtSid = f.StableIdFromAttribute;
                if (!string.Equals(fmtSid, compSid, StringComparison.Ordinal))
                {
                    var msg = $"[ZenECS] StableId mismatch: Component={f.ComponentType.FullName} '{compSid}' " +
                              $"<-> LatestFormatter={f.FormatterType.FullName} '{fmtSid}'. " +
                              $"Latest formatter's StableId MUST equal component StableId. " +
                              $"(Move old StableId formatters to legacy-read path and mark IsLatest=false)";
                    if (strict) throw new Exception(msg);
                    else LogWarn(msg);
                }
            }
        }

        // Static method area somewhere inside ZenEcsStableIdAndFormatterCodegen.cs
        static Dictionary<Type, string> ScanComponents(out CompScan[] compItems)
        {
            var compAttr = FindZenComponentAttributeType();
            if (compAttr == null)
                throw new Exception("[ZenECS] ZenComponentAttribute (ZenECS.Core) not found.");

            // Reuse scan logic from GenerateImpl
            compItems = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !a.FullName.StartsWith("UnityEditor", StringComparison.Ordinal))
                .SelectMany(a =>
                {
                    try
                    {
                        return a.GetTypes();
                    }
                    catch
                    {
                        return Array.Empty<Type>();
                    }
                })
                .Where(t => t != null && t.IsValueType && t.IsPublic)
                .Select(t => TryGetStableId(t, compAttr, out var sid)
                    ? new CompScan(sid, t, t.Assembly) : default(CompScan?))
                .Where(x => x.HasValue).Select(x => x.Value)
                .ToArray();

            var dupComp = compItems.GroupBy(x => x.StableId).FirstOrDefault(g => g.Count() > 1);
            if (dupComp != null)
                throw new Exception($"[ZenECS] Duplicate StableId (components): {dupComp.Key}");

            // Type -> StableId map
            return compItems.GroupBy(c => c.Type).ToDictionary(g => g.Key, g => g.First().StableId);
        }

        // Called each time Unity reloads domain after script compilation
        [UnityEditor.Callbacks.DidReloadScripts]
        static void OnScriptsReloaded()
        {
            try
            {
                // 1) Build Type->StableId map via component scan
                var stableByType = ScanComponents(out _); // ← Uses the same scanner as the existing Generate path

                // 2) Formatter scan
                int _warn;
                var fmts = ScanFormattersWithAttribute(stableByType, out _warn);

                // 3) isLatest duplication check (optional)
                var fail = EditorPrefs.GetBool(FAIL_ON_LATEST_DUP_PREF, true);
                ValidateLatestUniqueness(fmts, fail);

                // 4) Select latest formatter and validate StableId match
                var chosen = ChooseFormattersPerComponent_WithAttribute(
                    fmts.Where(f => f.ComponentType != null).ToList());

                var strictSid = EditorPrefs.GetBool(STRICT_STABLEID_MATCH_PREF, true);
                ValidateLatestStableIdMatch(chosen, stableByType, strictSid);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        // ===== Formatter scan (Attribute extension) =====
        static List<FmtScan> ScanFormattersWithAttribute(Dictionary<Type, string> stableByType, out int warnCount)
        {
            warnCount = 0;
            var results = new List<FmtScan>();

            // Robust type resolution (once only)
            var attrType = GetFormatterForAttributeType();
            var ifmtNonGeneric = GetIFormatterNonGenericType();
            var ifmtOpenGeneric = GetIFormatterOpenGenericType();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic || asm.FullName.StartsWith("UnityEditor", StringComparison.Ordinal)) continue;
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch
                {
                    continue;
                }

                foreach (var t in types)
                {
                    if (t == null || !t.IsClass || t.IsAbstract) continue;

                    // Whether implements IComponentFormatter / IComponentFormatter<T>
                    var ifaces = t.GetInterfaces();
                    bool hasIFmt = false;

                    if (ifmtNonGeneric != null && ifaces.Contains(ifmtNonGeneric))
                        hasIFmt = true;

                    if (!hasIFmt && ifmtOpenGeneric != null)
                        hasIFmt = ifaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == ifmtOpenGeneric);

                    // Last fallback: FQN string comparison
                    if (!hasIFmt)
                        hasIFmt = ifaces.Any(i => i.FullName == IFMT_NONGENERIC_FQN)
                                  || ifaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition().FullName == IFMT_OPEN_GENERIC_FQN);

                    if (!hasIFmt) continue;

                    // Estimate default component type
                    Type compType = ifaces.FirstOrDefault(i =>
                            i.IsGenericType && (ifmtOpenGeneric != null ? i.GetGenericTypeDefinition() == ifmtOpenGeneric
                                : i.GetGenericTypeDefinition().FullName == IFMT_OPEN_GENERIC_FQN))
                        ?.GetGenericArguments().FirstOrDefault();
                    if (compType == null)
                    {
                        // Fallback to ComponentType property of instance
                        try
                        {
                            var inst = Activator.CreateInstance(t);
                            var p = t.GetProperty("ComponentType", BindingFlags.Instance | BindingFlags.Public);
                            compType = p?.GetValue(inst) as Type;
                        }
                        catch
                        { /* ignore */
                        }
                    }

                    // Read Attribute (multiple allowed) — direct comparison with actual Type
                    var attrs = (attrType != null) ? t.GetCustomAttributes(attrType, inherit: false) : Array.Empty<object>();
                    if (attrs.Length > 0)
                    {
                        bool anyLatest = false;
                        foreach (var a in attrs)
                        {
                            var at = a.GetType();
                            var pComp = at.GetProperty("ComponentType", BindingFlags.Public | BindingFlags.Instance);
                            var pSID = at.GetProperty("StableId", BindingFlags.Public | BindingFlags.Instance);
                            var pL = at.GetProperty("IsLatest", BindingFlags.Public | BindingFlags.Instance);

                            var compFromAttr = pComp?.GetValue(a) as Type;
                            var sid = pSID?.GetValue(a) as string;
                            var isLatest = pL != null && (bool)pL.GetValue(a);

                            var effComp = compFromAttr ?? compType;
                            if (effComp == null)
                            {
                                LogWarn($"[ZenECS] Formatter {t.FullName} has ZenFormatterForAttribute but ComponentType is unknown.");
                                warnCount++;
                                continue;
                            }

                            int vHint = ExtractVnFromStableId(sid);

                            // Attribute StableId base vs Component StableId base (info log)
                            if (!string.IsNullOrEmpty(sid) && stableByType.TryGetValue(effComp, out var compSid))
                            {
                                var baseA = Regex.Replace(sid, @"\.v\d+$", "");
                                var baseC = Regex.Replace(compSid ?? "", @"\.v\d+$", "");
                                if (!string.Equals(baseA, baseC, StringComparison.Ordinal))
                                    LogVerbose($"[ZenECS] Formatter {t.Name} StableId base differs: attr={baseA}, comp={baseC}");
                            }

                            anyLatest |= isLatest;

                            results.Add(new FmtScan
                            {
                                FormatterType = t,
                                ComponentType = effComp,
                                FromAttribute = true,
                                IsLatest = isLatest,
                                VersionHint = vHint,
                                StableIdFromAttribute = sid
                            });
                        }

                        continue; // Skip naming fallback if Attribute exists
                    }

                    // Naming fallback: class name V(\d+)
                    int verName = -1;
                    var mv = Regex.Match(t.Name, @"V(\d+)\b");
                    if (mv.Success && int.TryParse(mv.Groups[1].Value, out var vn))
                        verName = vn;

                    results.Add(new FmtScan
                    {
                        FormatterType = t,
                        ComponentType = compType, // may be null
                        FromAttribute = false,
                        IsLatest = false, // determined in selection phase
                        VersionHint = verName
                    });
                }
            }

            return results;
        }

        static int ExtractVnFromStableId(string sid)
        {
            if (string.IsNullOrEmpty(sid)) return -1;
            var m = Regex.Match(sid, @"\.v(\d+)$");
            return (m.Success && int.TryParse(m.Groups[1].Value, out var v)) ? v : -1;
        }

        // Select one formatter per component (Attribute aware)
        static List<FmtScan> ChooseFormattersPerComponent_WithAttribute(List<FmtScan> list)
        {
            var withType = list.Where(f => f.ComponentType != null)
                .GroupBy(f => f.ComponentType)
                .Select(g => ChooseBest(g.Key, g.ToList()))
                .ToList();

            var withoutType = list.Where(f => f.ComponentType == null).ToList();
            withType.AddRange(withoutType);
            return withType;

            static FmtScan ChooseBest(Type compType, List<FmtScan> candidates)
            {
                // 1) Attribute IsLatest priority
                var latests = candidates.Where(c => c.FromAttribute && c.IsLatest).ToList();
                if (latests.Count == 1) return latests[0];
                if (latests.Count > 1)
                {
                    // Only warn and tie-break (build blocking is done in ValidateLatestUniqueness)
                    Debug.LogWarning($"[ZenECS] Multiple isLatest found for {compType.FullName}; tie-breaking.");
                    return latests
                        .OrderByDescending(c => c.VersionHint) // Larger Vn
                        .ThenByDescending(c => c.FormatterType.Assembly == compType.Assembly ? 1 : 0)
                        .ThenBy(c => c.FormatterType.FullName, StringComparer.Ordinal)
                        .First();
                }

                // 2) StableId .vN maximum (Attribute-based items only)
                var bestAttr = candidates.Where(c => c.FromAttribute && c.VersionHint >= 0)
                    .OrderByDescending(c => c.VersionHint)
                    .FirstOrDefault();
                if (bestAttr != null) return bestAttr;

                // 3) Class name V(\d+) maximum
                var bestName = candidates.OrderByDescending(c => c.VersionHint).FirstOrDefault();
                if (bestName != null && bestName.VersionHint >= 0) return bestName;

                // 4) Same assembly priority
                var sameAsm = candidates.Where(c => c.FormatterType.Assembly == compType.Assembly)
                    .OrderBy(c => c.FormatterType.FullName, StringComparer.Ordinal)
                    .ToList();
                if (sameAsm.Count > 0) return sameAsm.Last();

                // 5) Last resort: name order
                return candidates.OrderBy(c => c.FormatterType.FullName, StringComparer.Ordinal).Last();
            }
        }

        // ===== Internal/user identification =====
        static bool IsZenEcsAssemblyOrTypes(IEnumerable<CompScan> items)
        {
            var firstAsmName = items.First().Assembly.GetName().Name ?? "";
            if (firstAsmName.StartsWith("ZenECS.", StringComparison.Ordinal)) return true;
            return items.Any(it => (it.Type.Namespace ?? "").StartsWith("ZenECS.", StringComparison.Ordinal));
        }
        static bool IsZenEcsTypes(IEnumerable<CompScan> items)
            => items.Any(it => (it.Type.Namespace ?? "").StartsWith("ZenECS.", StringComparison.Ordinal));
        static bool IsZenEcsFormatterInternal(FmtScan f)
            => (f.FormatterType.Namespace ?? "").StartsWith("ZenECS.", StringComparison.Ordinal)
               || (f.ComponentType?.Namespace ?? "").StartsWith("ZenECS.", StringComparison.Ordinal);

        // ===== Scan/reflection utilities =====
        static Type GetIPostLoadMigrationType()
        {
            if (_ipostLoadType != null) return _ipostLoadType;
            // 1) FQN priority
            _ipostLoadType = Type.GetType(IPOSTLOAD_FQN, throwOnError: false);
            if (_ipostLoadType != null) return _ipostLoadType;
            // 2) Domain scan (to handle namespace changes)
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic) continue;
                try
                {
                    _ipostLoadType = asm.GetTypes().FirstOrDefault(t =>
                        t.IsInterface &&
                        (t.FullName == IPOSTLOAD_FQN || t.Name == "IPostLoadMigration"));
                    if (_ipostLoadType != null) break;
                }
                catch
                { /* ignore */
                }
            }
            return _ipostLoadType;
        }

        static List<MigScan> ScanPostLoadMigrations()
        {
            var tIface = GetIPostLoadMigrationType();
            var list = new List<MigScan>();
            if (tIface == null) return list; // Consider environment where registry may not exist
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic || asm.FullName.StartsWith("UnityEditor", StringComparison.Ordinal)) continue;
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch
                {
                    continue;
                }
                foreach (var t in types)
                {
                    if (t == null || !t.IsClass || t.IsAbstract) continue;
                    if (!tIface.IsAssignableFrom(t)) continue;
                    int? order = null;
                    try
                    {
                        var p = t.GetProperty("Order", BindingFlags.Public | BindingFlags.Instance);
                        if (p != null && p.PropertyType == typeof(int))
                        {
                            var inst = Activator.CreateInstance(t);
                            order = (int)p.GetValue(inst);
                        }
                    }
                    catch
                    { /* noop */
                    }
                    list.Add(new MigScan { MigrationType = t, Order = order });
                }
            }
            return list;
        }

        static Type FindZenComponentAttributeType()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic) continue;
                var t = asm.GetType("ZenECS.Core.ZenComponentAttribute", false);
                if (t != null && typeof(Attribute).IsAssignableFrom(t)) return t;
            }
            return null;
        }

        static Type GetFormatterForAttributeType()
        {
            if (_fmtAttrType != null) return _fmtAttrType;

            // 1) Try direct FQN (throwOnError:false as assembly name may change)
            _fmtAttrType = Type.GetType($"{ATTR_FORMATTER_FOR_FQN}", throwOnError: false);

            // 2) Full domain scan (to catch even if namespace differs slightly)
            if (_fmtAttrType == null)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.IsDynamic) continue;
                    try
                    {
                        foreach (var t in asm.GetTypes())
                        {
                            if (t.IsClass && typeof(Attribute).IsAssignableFrom(t)
                                          && t.Name == "ZenFormatterForAttribute"
                                          && (t.Namespace == "ZenECS.Core.Serialization" || t.FullName.EndsWith(".ZenFormatterForAttribute")))
                            {
                                _fmtAttrType = t;
                                break;
                            }
                        }
                        if (_fmtAttrType != null) break;
                    }
                    catch
                    { /* ignore */
                    }
                }
            }
            return _fmtAttrType;
        }

        static Type GetIFormatterNonGenericType()
        {
            if (_ifmtNonGenericType != null) return _ifmtNonGenericType;
            _ifmtNonGenericType = Type.GetType($"{IFMT_NONGENERIC_FQN}", throwOnError: false);
            if (_ifmtNonGenericType == null)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.IsDynamic) continue;
                    try
                    {
                        _ifmtNonGenericType = asm.GetTypes().FirstOrDefault(t => t.IsInterface && t.FullName == IFMT_NONGENERIC_FQN);
                        if (_ifmtNonGenericType != null) break;
                    }
                    catch
                    { /* ignore */
                    }
                }
            }
            return _ifmtNonGenericType;
        }

        static Type GetIFormatterOpenGenericType()
        {
            if (_ifmtOpenGenericType != null) return _ifmtOpenGenericType;
            _ifmtOpenGenericType = Type.GetType($"{IFMT_OPEN_GENERIC_FQN}", throwOnError: false);
            if (_ifmtOpenGenericType == null)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.IsDynamic) continue;
                    try
                    {
                        _ifmtOpenGenericType = asm.GetTypes().FirstOrDefault(t => t.IsInterface && t.IsGenericTypeDefinition && t.FullName == IFMT_OPEN_GENERIC_FQN);
                        if (_ifmtOpenGenericType != null) break;
                    }
                    catch
                    { /* ignore */
                    }
                }
            }
            return _ifmtOpenGenericType;
        }

        static bool TryGetStableId(Type target, Type attrType, out string stableId)
        {
            foreach (var a in target.GetCustomAttributes(false))
            {
                if (a.GetType() != attrType) continue;
                var prop = attrType.GetProperty("StableId", BindingFlags.Public | BindingFlags.Instance);
                if (prop != null)
                {
                    stableId = prop.GetValue(a) as string;
                    if (!string.IsNullOrWhiteSpace(stableId)) return true;
                }
            }
            stableId = null;
            return false;
        }

        static string RootNamespaceKey(Type t, int depth)
        {
            var ns = t.Namespace ?? "Global";
            var tokens = ns.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return "Global";
            var take = Math.Min(Math.Max(1, depth), tokens.Length);
            return string.Join(".", tokens.Take(take));
        }

        static string SanitizeId(string s) => Regex.Replace(s ?? string.Empty, @"[^A-Za-z0-9_]", "_");
        static string Norm(string p) => p?.Replace("\\", "/") ?? "";

        // ===== Path/output =====
        static void ResolveOutput(Group g, string customRootAssets, out string assetBase, out string classSuffix)
        {
            if (!string.IsNullOrEmpty(customRootAssets) && !g.IsZenEcsInternal)
            {
                if (g.IsAsmdef)
                {
                    classSuffix = SanitizeId(g.AsmName);
                    assetBase = $"{customRootAssets}/{classSuffix}/Serialization/Registry/Generated";
                }
                else
                {
                    classSuffix = SanitizeId(g.RootNsKey.Replace('.', '_'));
                    assetBase = $"{customRootAssets}/{g.RootNsKey.Replace('.', '/')}/Serialization/Registry/Generated";
                }
                return;
            }

            if (g.IsAsmdef)
            {
                classSuffix = SanitizeId(g.AsmName);
                var asmdefDir = Path.GetDirectoryName(g.AsmdefAssetPath).Replace("\\", "/");
                var baseDir = $"{asmdefDir}/Serialization/Registry/Generated";

                if (baseDir.StartsWith("Packages/", StringComparison.Ordinal))
                {
                    var pkg = PackageInfo.FindForAssetPath(asmdefDir);
                    if (pkg != null && (pkg.source == PackageSource.Embedded || pkg.source == PackageSource.Local))
                        assetBase = baseDir;
                    else
                        assetBase = $"Assets/ZenECS/{classSuffix}/Serialization/Registry/Generated";
                }
                else assetBase = baseDir;
            }
            else
            {
                classSuffix = SanitizeId(g.RootNsKey.Replace('.', '_'));
                assetBase = $"Assets/{g.RootNsKey.Replace('.', '/')}/Serialization/Registry/Generated";
            }
        }

        static string FindSerializationRoot(string assetPathOrDir)
        {
            var path = assetPathOrDir?.Replace("\\", "/") ?? "";
            while (!string.IsNullOrEmpty(path) && path != "Assets")
            {
                var name = Path.GetFileName(path.TrimEnd('/', '\\'));
                if (string.Equals(name, "Serialization", StringComparison.Ordinal))
                    return path;
                path = Path.GetDirectoryName(path)?.Replace("\\", "/");
            }
            return null;
        }

        static string ToFsPath(string assetPath)
        {
            assetPath = Norm(assetPath);
            if (assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                var rel = assetPath.Substring("Assets/".Length);
                return Path.Combine(Application.dataPath, rel).Replace("\\", "/");
            }
            if (assetPath.StartsWith("Packages/", StringComparison.Ordinal))
            {
                var seg = assetPath.Split('/');
                if (seg.Length > 1)
                {
                    var pkgName = seg[1];
                    var pkg = PackageInfo.FindForAssetPath($"Packages/{pkgName}");
                    if (pkg != null)
                    {
                        var rel = assetPath.Substring(("Packages/" + pkg.name + "/").Length);
                        return Path.Combine(pkg.resolvedPath, rel).Replace("\\", "/");
                    }
                }
            }
            throw new InvalidOperationException("Not an asset path: " + assetPath);
        }

        static string GetAsmdefAssetPath(string assemblyName)
        {
            if (_asmdefPathByAsmName == null) BuildAsmdefIndex();
            return _asmdefPathByAsmName.TryGetValue(assemblyName, out var path) ? path : null;
        }

        static void BuildAsmdefIndex()
        {
            _asmdefPathByAsmName = new Dictionary<string, string>(StringComparer.Ordinal);
            var guids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                try
                {
                    var text = File.ReadAllText(ToFsPath(path), new UTF8Encoding(false));
                    var m = Regex.Match(text, "\"name\"\\s*:\\s*\"([^\"]+)\"");
                    if (m.Success)
                    {
                        var name = m.Groups[1].Value.Trim();
                        if (!_asmdefPathByAsmName.ContainsKey(name))
                            _asmdefPathByAsmName.Add(name, Norm(path));
                    }
                }
                catch
                { /* ignore */
                }
            }
        }

        static void WriteIfChangedAndImport(string fsPath, string assetPath, string newText)
        {
            newText ??= string.Empty;
            var enc = new UTF8Encoding(false);
            string oldText = File.Exists(fsPath) ? File.ReadAllText(fsPath, enc) : null;
            if (oldText != null && string.Equals(oldText, newText, StringComparison.Ordinal))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(fsPath)!);
            File.WriteAllText(fsPath, newText, enc);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        // ===== Cleanup =====
        static void CleanStaleUserOutputs(string customRootAssets,
            HashSet<string> internalKeepPrefixes,
            HashSet<string> keepRoots)
        {
            var dataRoot = Norm(Application.dataPath);
            var customRootNorm = string.IsNullOrEmpty(customRootAssets) ? null : Norm(customRootAssets).TrimEnd('/') + "/";

            var candidates = Directory.GetFiles(Application.dataPath, "*.g.cs", SearchOption.AllDirectories);
            var dirsToDelete = new HashSet<string>(StringComparer.Ordinal);

            foreach (var fs in candidates)
            {
                var fsn = Norm(fs);
                if (fsn.IndexOf("/Serialization/Registry/Generated/", StringComparison.Ordinal) < 0) continue;

                var rel = fsn.Substring(dataRoot.Length).TrimStart('/');
                var assetPath = "Assets/" + rel;
                var assetDir = Path.GetDirectoryName(assetPath).Replace("\\", "/") + "/";

                if (internalKeepPrefixes.Any(pref => assetPath.StartsWith(pref, StringComparison.Ordinal)))
                    continue;

                string txt;
                try
                {
                    txt = File.ReadAllText(fsn, new UTF8Encoding(false));
                    if (!txt.Contains(FILE_MARKER)) continue;
                }
                catch
                {
                    continue;
                }

                var serRoot = FindSerializationRoot(assetDir.TrimEnd('/'));
                var serRootNorm = string.IsNullOrEmpty(serRoot) ? null : Norm(serRoot).TrimEnd('/') + "/";

                if (!string.IsNullOrEmpty(customRootNorm) && assetPath.StartsWith(customRootNorm, StringComparison.Ordinal))
                    continue;

                if (!string.IsNullOrEmpty(serRootNorm) && keepRoots.Contains(serRootNorm))
                    continue;

                if (!string.IsNullOrEmpty(customRootNorm) && !assetPath.StartsWith(customRootNorm, StringComparison.Ordinal))
                {
                    if (!string.IsNullOrEmpty(serRoot))
                        dirsToDelete.Add(serRoot);
                }
                else if (!string.IsNullOrEmpty(customRootNorm))
                {
                    dirsToDelete.Add(customRootNorm.TrimEnd('/'));
                }
            }

            foreach (var dir in dirsToDelete)
            {
                if (AssetDatabase.IsValidFolder(dir))
                    AssetDatabase.DeleteAsset(dir);
                else
                    AssetDatabase.DeleteAsset(dir);
                LogVerbose($"[ZenECS] Deleted stale output: {dir}");
            }
        }

        static void PurgeCustomRoot(string customRootAssets)
        {
            if (string.IsNullOrEmpty(customRootAssets) || !customRootAssets.StartsWith("Assets/"))
                return;
            if (AssetDatabase.IsValidFolder(customRootAssets))
            {
                AssetDatabase.DeleteAsset(customRootAssets);
                LogVerbose($"[ZenECS] Purged previous custom root: {customRootAssets}");
            }
        }
    }
}
#endif