#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Editor.Common
{
    public static class ZenUtil
    {
        public static void PingType(Type? t)
        {
            if (t == null) return;

            var scripts = Resources.FindObjectsOfTypeAll<MonoScript>();
            foreach (var ms in scripts)
            {
                if (ms == null) continue;
                try
                {
                    if (ms.GetClass() == t)
                    {
                        // Selection은 건드리지 않고 Ping만
                        EditorGUIUtility.PingObject(ms);
                        return;
                    }
                }
                catch
                {
                    // 무시
                }
            }

            Debug.Log($"ZenEcsExplorer: Unable to locate a script asset for component type {t.FullName}.\nIt may not exist, or a matching type name is required to ping the script source.");
        }
        
        public static class BinderTypeFinder
        {
            private static List<Type>? _cache;

            public static IEnumerable<Type> All()
            {
                if (_cache != null) return _cache;

                var list = new List<Type>(256);
                var asms = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var asm in asms)
                {
                    // 편의상 에디터/시스템 어셈블리는 스킵 (원하면 조건 완화 가능)
                    var n = asm.GetName().Name;
                    if (n.StartsWith("UnityEditor", StringComparison.OrdinalIgnoreCase)) continue;
                    if (n.StartsWith("System", StringComparison.OrdinalIgnoreCase)) continue;
                    if (n.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase)) continue;
                    if (n.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)) continue;

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
                        if (t == null || t.IsAbstract) continue;
                        if (t.IsGenericTypeDefinition) continue;
                        if (t.Namespace != null && t.Namespace.EndsWith(".Editor", StringComparison.Ordinal)) continue;

                        // 생성자 조건: 기본 생성자
                        if (t.GetConstructor(Type.EmptyTypes) == null) continue;

                        // “Binder” 후보 판별 (마커 인터페이스/특성/이름 규칙 중 하나라도 맞으면 통과)
                        if (LooksLikeBinder(t))
                            list.Add(t);
                    }
                }

                // 정렬 및 캐시
                _cache = list
                    .Distinct()
                    .OrderBy(t => t.FullName)
                    .ToList();
                return _cache;
            }

            static bool LooksLikeBinder(Type t) => typeof(IBinder).IsAssignableFrom(t);
        }
        
        public static class ContextTypeFinder
        {
            private static List<Type>? _cache;

            public static IEnumerable<Type> All()
            {
                if (_cache != null) return _cache;

                var list = new List<Type>(256);
                var asms = AppDomain.CurrentDomain.GetAssemblies();

                foreach (var asm in asms)
                {
                    // 편의상 에디터/시스템 어셈블리는 스킵 (원하면 조건 완화 가능)
                    var n = asm.GetName().Name;
                    if (n.StartsWith("UnityEditor", StringComparison.OrdinalIgnoreCase)) continue;
                    if (n.StartsWith("System", StringComparison.OrdinalIgnoreCase)) continue;
                    if (n.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase)) continue;
                    if (n.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)) continue;

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
                        if (t == null || t.IsAbstract) continue;
                        if (t.IsGenericTypeDefinition) continue;
                        if (t.Namespace != null && t.Namespace.EndsWith(".Editor", StringComparison.Ordinal)) continue;

                        // 생성자 조건: 기본 생성자
                        if (t.GetConstructor(Type.EmptyTypes) == null) continue;

                        // “Binder” 후보 판별 (마커 인터페이스/특성/이름 규칙 중 하나라도 맞으면 통과)
                        if (LooksLikeContext(t))
                            list.Add(t);
                    }
                }

                // 정렬 및 캐시
                _cache = list
                    .Distinct()
                    .OrderBy(t => t.FullName)
                    .ToList();
                return _cache;
            }

            static bool LooksLikeContext(Type t) => typeof(ContextAsset).IsAssignableFrom(t);
        }
        
        public static List<ContextAsset> LoadAllAssets()
        {
            var res = new List<ContextAsset>(64);
            foreach (var guid in AssetDatabase.FindAssets("t:ContextAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var a = AssetDatabase.LoadAssetAtPath<ContextAsset>(path);
                if (a) res.Add(a);
            }

            return res.OrderBy(a => a.name).ToList();
        }
    }
}
#endif