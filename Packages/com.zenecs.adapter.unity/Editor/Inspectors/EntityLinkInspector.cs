// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: EntityLinkInspector.cs
// Purpose: Custom inspector for EntityLink MonoBehaviour that displays
//          world/entity metadata and provides debugging utilities.
// Key concepts:
//   • Inspector UI: shows linked world name, entity ID/Gen, alive status.
//   • Debug tools: ping entity in Explorer, detach link, view components.
//   • Editor-only: compiled out in player builds via #if UNITY_EDITOR.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
#nullable enable
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Editor.GUIs;
using ZenECS.Adapter.Unity.Editor.Tools;
using ZenECS.Core;
using ZenECS.Adapter.Unity.Linking;

namespace ZenECS.Adapter.Unity.Editor.Inspectors
{
    /// <summary>
    /// Displays EntityLink runtime metadata in the inspector and provides ExplorerWindow integration button.
    /// </summary>
    [CustomEditor(typeof(EntityLink))]
    public sealed class EntityLinkInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var link = (EntityLink)target;
            var icon = EditorGUIUtility.ObjectContent(target, target.GetType()).image;

            // ─────────────────────────────────────────────
            // ZenECS Header (NEW)
            // ─────────────────────────────────────────────
            ZenEcsGUIHeader.DrawHeader(
                "Entity Link",
                "Links this GameObject to an ECS entity, providing world/entity metadata and debugging utilities.",
                new[]
                {
                    "Runtime Metadata",
                    "Debug Tool",
                    "Unity ↔ ECS Bridge"
                }
            );
            
            // (2) Meta panel
            DrawMetaBox(link);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!link || link.World == null))
            {
                // Select in ExplorerWindow
                if (GUILayout.Button("Select Linked Entity in Explorer", GUILayout.Height(24)))
                {
                    if (!TrySelectInExplorer(link))
                        EditorUtility.DisplayDialog("Explorer",
                            "Cannot find ExplorerWindow or cannot call SelectEntity method.\n" +
                            "Please check window/name/namespace.", "OK");
                }
            }
        }

        private static void DrawMetaBox(EntityLink link)
        {
            var alive = link && link.World != null && link.IsAlive;
            var style = new GUIStyle(EditorStyles.helpBox) { richText = true };
            EditorGUILayout.BeginVertical(style);
            {
                EditorGUILayout.LabelField("EntityLink Metadata", EditorStyles.boldLabel);

                if (!link || link.World == null)
                {
                    EditorGUILayout.HelpBox("World is not connected. (link not configured)", MessageType.Info);
                }
                else
                {
                    var e = link.Entity;
                    var worldName = SafeWorldName(link.World);
                    EditorGUILayout.LabelField("World", worldName);
                    EditorGUILayout.LabelField("Entity", alive ? $"{e.Id}:{e.Gen}" : "(invalid)");
                    EditorGUILayout.LabelField("IsAlive", alive ? "True" : "False");
                }
            }
            EditorGUILayout.EndVertical();
        }

        private static string SafeWorldName(IWorld w)
        {
            try { return string.IsNullOrEmpty(w.Name) ? "(unnamed)" : w.Name; }
            catch { return "(unknown)"; }
        }

        private static bool TrySelectInExplorer(EntityLink? link)
        {
            if (link == null || link.World == null || !link.IsAlive) return false;

            var e = link.Entity;
            return ZenEcsExplorerBridge.TryOpenAndSelect(link.World, e.Id, e.Gen);
        }
    }
}
#endif