// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: SharedContextAssetInspector.cs
// Purpose: Custom inspector for SharedContextAsset ScriptableObject that
//          provides editing UI for shared context type selection.
// Key concepts:
//   • Context type picker: selects IContext implementation type.
//   • Type validation: ensures selected type implements IContext.
//   • Editor-only: compiled out in player builds via #if UNITY_EDITOR.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Adapter.Unity.Editor.GUIs;

namespace ZenECS.Adapter.Unity.Editor.Inspectors
{
    /// <summary>
    /// Inspector for SharedContextMarkerAsset.
    /// Layout:
    ///   1) Script
    ///   2) ContextType header + ping icon
    ///   3) Context Type / Namespace
    ///   4) Public members (fields + properties, excluding private)
    ///   5) "Properties" header
    ///   6) Unity default inspector (excluding m_Script)
    /// </summary>
    [CustomEditor(typeof(SharedContextAsset), editorForChildClasses: true)]
    public sealed class SharedContextAssetInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var icon = EditorGUIUtility.ObjectContent(target, target.GetType()).image;

            ZenEcsGUIHeader.DrawHeader(
                "Shared Context",
                "Declares a world-level context type that is instantiated once and shared across all entities.",
                new[]
                {
                    "Context",
                    "World-Level",
                    "Runtime Marker"
                }
            );
            
            serializedObject.Update();

            var marker  = (SharedContextAsset)target;
            var ctxType = marker.ContextType;

            // // ─────────────────────────────────────────────
            // // 1) Script (always first line, read-only)
            // // ─────────────────────────────────────────────
            // var scriptProp = serializedObject.FindProperty("m_Script");
            // if (scriptProp != null)
            // {
            //     using (new EditorGUI.DisabledScope(true))
            //     {
            //         EditorGUILayout.PropertyField(scriptProp);
            //     }
            // }

            // EditorGUILayout.Space(4f);

            // Display Context info only if ContextType is valid
            if (ctxType != null)
            {
                // ─────────────────────────────────────────
                // 2) ContextType name header + right-side ping icon
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
                EditorGUI.LabelField(headerLabelRect, ctxType.Name, EditorStyles.boldLabel);

                // Ping icon button → source Ping (no Selection change)
                using (new EditorGUI.DisabledScope(false))
                {
                    var gcPing = GetSearchIconContent("Ping context script in Project");
                    if (GUI.Button(pingRect, gcPing, EditorStyles.iconButton))
                    {
                        PingTypeSource(ctxType);
                    }
                }

                // ─────────────────────────────────────────
                // 3) Context Type / Namespace info
                // ─────────────────────────────────────────
                var typeRect = EditorGUILayout.GetControlRect();
                EditorGUI.LabelField(typeRect, "Context Type", "Shared Context");

                var nsRect = EditorGUILayout.GetControlRect();
                string ns = ctxType.Namespace ?? "(global)";
                EditorGUI.LabelField(nsRect, "Namespace", ns);

                EditorGUILayout.Space(4f);

                // ─────────────────────────────────────────
                // 4) Display public members of Context Type
                //    (exclude private: public fields + public properties)
                // ─────────────────────────────────────────
                var members = CollectPublicMembers(ctxType);
                if (members.Count > 0)
                {
                    EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);

                    if (members.Count <= 0)
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUILayout.LabelField("No properties");
                        }
                    }
                    else
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            foreach (var (name, typeName) in members)
                            {
                                EditorGUILayout.LabelField(name, typeName);
                            }
                        }

                        EditorGUILayout.Space(4f);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "ContextType is null. This marker does not point to any context type.",
                    MessageType.Warning);
                EditorGUILayout.Space(4f);
            }

            // // ─────────────────────────────────────────────
            // // 5) Properties header
            // // ─────────────────────────────────────────────
            // EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);
            //
            // // ─────────────────────────────────────────────
            // // 6) Rest uses Unity default inspector as-is
            // //    (Script(m_Script) only excluded)
            // // ─────────────────────────────────────────────
            // EditorGUILayout.Space(2f);
            //
            // DrawPropertiesExcluding(serializedObject, "m_Script");

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Collects "accessible" members of the specified type.
        /// - Excludes private
        /// - Public instance fields + public instance properties
        /// - DeclaredOnly: Excludes transform/gameObject etc. from base class (MonoBehaviour)
        /// </summary>
        static List<(string name, string typeName)> CollectPublicMembers(Type ctxType)
        {
            var result = new List<(string, string)>();

            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            // 1) public fields
            foreach (var f in ctxType.GetFields(flags))
            {
                // Filter only [HideInInspector] etc. if needed
                if (Attribute.IsDefined(f, typeof(HideInInspector), inherit: true))
                    continue;

                result.Add((f.Name, f.FieldType.Name));
            }

            // 2) public properties (only those with getter, exclude indexers)
            foreach (var p in ctxType.GetProperties(flags))
            {
                if (!p.CanRead) continue;
                if (p.GetIndexParameters().Length != 0) continue;
                if (Attribute.IsDefined(p, typeof(HideInInspector), inherit: true))
                    continue;

                result.Add((p.Name, p.PropertyType.Name));
            }

            return result;
        }

        // ─────────────────────────────────────────────
        // Helpers (same pattern as ModelContextAssetInspector)
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
