// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenAssetDatabase.cs
// Purpose: Unity AssetDatabase wrapper utilities for finding and loading
//          ScriptableObject assets used by ZenECS editor tooling.
// Key concepts:
//   • Asset discovery: FindAndLoadAllAssets<T> searches project for assets.
//   • Script pinging: PingMonoScript locates script files by type name.
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
using UnityEditor;
using UnityEngine;

namespace ZenECS.Adapter.Unity.Editor.Common
{
    /// <summary>
    /// Common utilities for AssetDatabase operations
    /// </summary>
    public static class ZenAssetDatabase
    {
        /// <summary>
        /// Finds and loads all assets of the specified type
        /// </summary>
        public static List<T> FindAndLoadAllAssets<T>(string? filter = null) where T : UnityEngine.Object
        {
            var result = new List<T>(64);
            var searchFilter = string.IsNullOrEmpty(filter) ? $"t:{typeof(T).Name}" : filter;
            
            var guids = AssetDatabase.FindAssets(searchFilter);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    result.Add(asset);
            }

            return result.OrderBy(a => a.name).ToList();
        }

        /// <summary>
        /// Finds and loads the first asset of the specified type matching the name
        /// </summary>
        public static T? FindAssetByName<T>(string name) where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null && asset.name == name)
                    return asset;
            }
            return null;
        }

        /// <summary>
        /// Finds MonoScript by type name (quick search)
        /// </summary>
        public static MonoScript? FindMonoScriptByTypeName(string typeName)
        {
            var guids = AssetDatabase.FindAssets($"t:MonoScript {typeName}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms != null && ms.GetClass()?.Name == typeName)
                    return ms;
            }
            return null;
        }

        /// <summary>
        /// Finds MonoScript by exact type match
        /// </summary>
        public static MonoScript? FindMonoScriptByType(Type type)
        {
            if (type == null) return null;

            // Quick search by name first
            var guids = AssetDatabase.FindAssets($"t:MonoScript {type.Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms != null && ms.GetClass() == type)
                    return ms;
            }

            // Fallback: full scan
            var allGuids = AssetDatabase.FindAssets("t:MonoScript");
            foreach (var guid in allGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms != null && ms.GetClass() == type)
                    return ms;
            }

            return null;
        }

        /// <summary>
        /// Pings the MonoScript asset for the given type
        /// </summary>
        public static bool PingMonoScript(Type? type)
        {
            if (type == null) return false;
            
            var ms = FindMonoScriptByType(type);
            if (ms != null)
            {
                EditorGUIUtility.PingObject(ms);
                return true;
            }
            return false;
        }
    }
}
#endif
