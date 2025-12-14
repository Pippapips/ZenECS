// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenSystemMetaForm.Contents.Order.cs
// Purpose: Execution order editing implementation for ZenSystemMetaForm partial
//          class, providing UI for modifying system execution order.
// Key concepts:
//   • Execution order editing: modify system execution order within groups.
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
        private static void drawContentOrder(Type? t, float labelWidth = 70)
        {
            if (t == null) return;

            // Order Before/After (Attribute-based)
            var beforeList = new List<string>();
            var afterList = new List<string>();

            try
            {
                var beforeAttrs = t.GetCustomAttributes(typeof(OrderBeforeAttribute), true)
                    .Cast<OrderBeforeAttribute>();
                foreach (var a in beforeAttrs)
                {
                    var target = a.Target;
                    if (target != null)
                        beforeList.Add(target.Name);
                }

                var afterAttrs = t.GetCustomAttributes(typeof(OrderAfterAttribute), true)
                    .Cast<OrderAfterAttribute>();
                foreach (var a in afterAttrs)
                {
                    var target = a.Target;
                    if (target != null)
                        afterList.Add(target.Name);
                }
            }
            catch
            {
                // Silently ignore as types may differ in older versions
            }

            string beforeText = beforeList.Count > 0
                ? string.Join(", ", beforeList.Distinct())
                : "—";

            string afterText = afterList.Count > 0
                ? string.Join(", ", afterList.Distinct())
                : "—";

            // Order (Before/After)
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Order Before", ZenGUIStyles.LabelMLNormal10, GUILayout.Width(labelWidth));
            EditorGUILayout.LabelField(beforeText, ZenGUIStyles.LabelMLNormal9);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Order After", ZenGUIStyles.LabelMLNormal10, GUILayout.Width(labelWidth));
            EditorGUILayout.LabelField(afterText, ZenGUIStyles.LabelMLNormal9);
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif