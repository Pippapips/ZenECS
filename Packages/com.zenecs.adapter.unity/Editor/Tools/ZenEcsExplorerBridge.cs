// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenEcsExplorerBridge.cs
// Purpose: Loose coupling bridge that allows external code to open and select
//          entities in the ZenECS Explorer window via reflection.
// Key concepts:
//   • Type resolution: finds ExplorerWindow type by name via reflection.
//   • Method invocation: calls SelectEntity(IWorld, int, int) dynamically.
//   • Loose coupling: no direct reference to ExplorerWindow assembly.
//   • Editor-only: compiled out in player builds via #if UNITY_EDITOR.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
#nullable enable
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Editor.Tools
{
    /// <summary>
    /// Bridge for loosely coupling with ExplorerWindow.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Enables opening the ZenECS Explorer window and selecting entities through reflection.
    /// </para>
    /// <para>
    /// Main operations:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Type name: searches for "ZenEcsExplorerWindow"</description></item>
    /// <item><description>Instance acquisition: <see cref="EditorWindow.GetWindow(Type, bool, string, bool)"/></description></item>
    /// <item><description>Method invocation: <c>SelectEntity(IWorld, int, int)</c></description></item>
    /// </list>
    /// </remarks>
    internal static class ZenEcsExplorerBridge
    {
        private static Type? _cachedType;
        private static MethodInfo? _cachedSelectMethod;

        /// <summary>
        /// Opens the ExplorerWindow and selects the specified entity.
        /// </summary>
        /// <param name="world">The world that the entity belongs to.</param>
        /// <param name="entityId">The ID of the entity to select.</param>
        /// <param name="entityGen">The generation of the entity to select.</param>
        /// <returns>
        /// Returns <c>true</c> if the ExplorerWindow was found and entity selection succeeded;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Uses reflection to find the ExplorerWindow type, opens the window, and then
        /// calls the SelectEntity method. Returns <c>false</c> if the type or method cannot be found.
        /// </para>
        /// </remarks>
        public static bool TryOpenAndSelect(IWorld world, int entityId, int entityGen)
        {
            var t = ResolveExplorerType();
            if (t == null) return false;

            var win = EditorWindow.GetWindow(t, utility: false, title: "ZenECS Explorer", focus: true);
            if (win == null)
            {
                // Last resort if focus fails
                win = ScriptableObject.CreateInstance(t) as EditorWindow;
                if (win == null) return false;
                win.Show();
            }

            var mi = ResolveSelectEntityMethod(t);
            if (mi == null) return false;

            try
            {
                mi.Invoke(win, new object[] { world, entityId, entityGen });
                win.Repaint();
                return true;
            }
            catch (TargetInvocationException tie)
            {
                Debug.LogError($"[ExplorerBridge] Exception while calling SelectEntity: {tie.InnerException?.Message ?? tie.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExplorerBridge] Failed to call SelectEntity: {ex.Message}");
                return false;
            }
        }

        private static Type? ResolveExplorerType()
        {
            if (_cachedType != null) return _cachedType;

            // Priority: clear names first
            string[] candidates = { "ZenEcsExplorerWindow" };
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type? t = null;
                try
                {
                    t = asm.GetTypes()
                           .FirstOrDefault(x =>
                               typeof(EditorWindow).IsAssignableFrom(x) &&
                               candidates.Contains(x.Name));
                }
                catch (ReflectionTypeLoadException e)
                {
                    foreach (var le in e.LoaderExceptions)
                        Debug.LogWarning($"[ExplorerBridge] Type load warning: {le.Message}");
                    continue;
                }

                if (t != null)
                {
                    _cachedType = t;
                    break;
                }
            }

            if (_cachedType == null)
                Debug.LogWarning("[ExplorerBridge] Could not find ExplorerWindow type. (Names: ExplorerWindow/EcsExplorerWindow)");

            return _cachedType;
        }

        private static MethodInfo? ResolveSelectEntityMethod(Type? t)
        {
            if (_cachedSelectMethod != null) return _cachedSelectMethod;

            if (t == null)
            {
                Debug.LogWarning($"[ExplorerBridge] {t} is null");
                return null;
            }
            _cachedSelectMethod = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                   .FirstOrDefault(m =>
                                   {
                                       if (m.Name != "SelectEntity") return false;
                                       var ps = m.GetParameters();
                                       return ps.Length == 3
                                              && ps[0].ParameterType == typeof(IWorld)
                                              && ps[1].ParameterType == typeof(int)
                                              && ps[2].ParameterType == typeof(int);
                                   });

            if (_cachedSelectMethod == null)
                Debug.LogWarning("[ExplorerBridge] Could not find SelectEntity(IWorld, int, int) method.");

            return _cachedSelectMethod;
        }
    }
}
#endif
