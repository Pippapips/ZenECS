// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenSystemMetaForm.Contents.Watched.cs
// Purpose: Watch attributes display implementation for ZenSystemMetaForm
//          partial class, showing entities watched by a system.
// Key concepts:
//   • Watch attributes: displays entities watched via ZenSystemWatchAttribute.
//   • Entity filtering: shows entities matching watch criteria.
//   • Partial class: part of ZenSystemMetaForm split across multiple files.
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
using Codice.Client.Common.GameUI;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.Editor.GUIs
{
    public static partial class ZenSystemMetaForm
    {
        private static void drawContentWatched(Type? t, float labelWidth = 70)
        {
            if (t == null) return;

            var watchedTypes = new List<Type>();
            try
            {
                var watchAttrs = t.GetCustomAttributes(typeof(ZenSystemWatchAttribute), false)
                    .Cast<ZenSystemWatchAttribute>();

                foreach (var wa in watchAttrs)
                {
                    var allOf = wa.AllOf;
                    if (allOf == null || allOf.Length == 0)
                        continue;

                    foreach (var compType in allOf)
                    {
                        if (compType != null)
                            watchedTypes.Add(compType);
                    }
                }
            }
            catch
            {
                // Silently ignore if older version or reflection fails
            }

            var watchedDistinct = watchedTypes
                .Where(x => x != null)
                .Distinct()
                .OrderBy(x => x.Name)
                .ToList();

            if (watchedDistinct.Count == 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Watched", ZenGUIStyles.LabelMLNormal10, GUILayout.Width(labelWidth));
                EditorGUILayout.LabelField("—", ZenGUIStyles.LabelMLNormal9);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("Watched", ZenGUIStyles.LabelMLNormal10, GUILayout.Width(labelWidth));

                EditorGUI.indentLevel++;

                foreach (var compType in watchedDistinct)
                {
                    if (compType == null) continue;

                    string cns = string.IsNullOrEmpty(compType.Namespace) ? "(global)" : compType.Namespace;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // Component name
                        EditorGUILayout.LabelField($"{compType.Name} <size=9><color=#707070>[{cns}]</color></size>",
                            ZenGUIStyles.LabelMLNormal10);

                        // Ping icon (right end)
                        var icon = ZenGUIContents.IconPing();
                        if (GUILayout.Button(icon, EditorStyles.iconButton, GUILayout.Width(18),
                                GUILayout.Height(16)))
                        {
                            // Keep selection and only ping
                            ZenUtil.PingType(compType);
                        }
                    }
                }

                EditorGUI.indentLevel--;
            }
        }
    }
}
#endif