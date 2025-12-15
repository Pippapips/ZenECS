// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: BinderAssetInspector.cs
// Purpose: Custom inspector for BinderAsset ScriptableObject that
//          provides editing UI for binder factory configuration.
// Key concepts:
//   • Binder factory: creates IBinder instances per entity at spawn time.
//   • Type selection: picks IBinder implementation type for factory.
//   • Editor-only: compiled out in player builds via #if UNITY_EDITOR.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
#nullable enable
using System;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Binders.Assets;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Adapter.Unity.Editor.GUIs;

namespace ZenECS.Adapter.Unity.Editor.Inspectors
{
    /// <summary>
    /// Inspector for BinderAsset (Binder asset).
    /// Layout:
    ///   1) Script
    ///   2) BinderType header + info
    ///   3) "Properties" header
    ///   4) Unity default inspector (excluding m_Script)
    /// </summary>
    [CustomEditor(typeof(BinderAsset), editorForChildClasses: true)]
    public sealed class BinderAssetInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var icon = EditorGUIUtility.ObjectContent(target, target.GetType()).image;

            // Add ZenECS header
            ZenEcsGUIHeader.DrawHeader(
                "Binder Asset",
                "Creates a unique binder instance for every spawned entity.",
                new[] { "Binder", "Runtime Factory" }
            );
            
            var asset   = (BinderAsset)target;
            var binderType = asset.BinderType;

            // Display Binder info only if BinderType is valid
            if (binderType != null)
            {
                // ─────────────────────────────────────────
                // 2) BinderType name header + right-side ping icon
                // ─────────────────────────────────────────
                var headerRect = EditorGUILayout.GetControlRect();
                const float pingW = 20f;

                var headerLabelRect = new Rect(
                    headerRect.x,
                    headerRect.y,
                    headerRect.width - pingW,
                    headerRect.height);

                var pingRect = new Rect(
                    headerRect.xMax - pingW,
                    headerRect.y,
                    pingW,
                    headerRect.height);

                // Class name header (bold)
                EditorGUI.LabelField(headerLabelRect, binderType.Name, EditorStyles.boldLabel);

                // Ping icon button → source Ping (no Selection change)
                using (new EditorGUI.DisabledScope(false))
                {
                    var gcPing = GetSearchIconContent("Ping binder script in Project");
                    if (GUI.Button(pingRect, gcPing, EditorStyles.iconButton))
                    {
                        PingTypeSource(binderType);
                    }
                }

                // ─────────────────────────────────────────
                // 3) Binder Type / Namespace info
                // ─────────────────────────────────────────
                var typeRect = EditorGUILayout.GetControlRect();
                EditorGUI.LabelField(typeRect, "Binder Type", "Binder");

                var nsRect = EditorGUILayout.GetControlRect();
                string ns = binderType.Namespace ?? "(global)";
                EditorGUI.LabelField(nsRect, "Namespace", ns);

                EditorGUILayout.Space(4f);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "BinderType is null. This asset does not point to any binder type.",
                    MessageType.Warning);
                EditorGUILayout.Space(4f);
            }

            // ─────────────────────────────────────────────
            // 4) Properties header
            // ─────────────────────────────────────────────
            EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);

            // ─────────────────────────────────────────────
            // 5) Rest uses Unity default inspector as-is
            //    (Script(m_Script) only excluded)
            // ─────────────────────────────────────────────
            EditorGUILayout.Space(2f);

            DrawPropertiesExcluding(serializedObject, "m_Script");

            serializedObject.ApplyModifiedProperties();
        }

        // ─────────────────────────────────────────────
        // Helpers (same style as PerEntityContextAssetInspector)
        // ─────────────────────────────────────────────

        static void PingTypeSource(Type? t)
        {
            ZenAssetDatabase.PingMonoScript(t);
        }

        static GUIContent GetSearchIconContent(string tooltip)
        {
            return ZenGUIContents.IconPing(tooltip);
        }
    }
}
#endif

