// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: PerEntityContextAssetInspector.cs
// Purpose: Custom inspector for PerEntityContextAsset ScriptableObject that
//          provides editing UI for per-entity context factory configuration.
// Key concepts:
//   • Context factory: creates IContext instances per entity at spawn time.
//   • Type selection: picks IContext implementation type for factory.
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
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Adapter.Unity.Editor.GUIs;

namespace ZenECS.Adapter.Unity.Editor.Inspectors
{
    /// <summary>
    /// Inspector for ModelContextAsset (Per-entity context asset).
    /// Layout:
    ///   1) Script
    ///   2) ContextType header + info
    ///   3) "Properties" header
    ///   4) Unity default inspector (excluding m_Script)
    /// </summary>
    [CustomEditor(typeof(PerEntityContextAsset), editorForChildClasses: true)]
    public sealed class PerEntityContextAssetInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var icon = EditorGUIUtility.ObjectContent(target, target.GetType()).image;

            // ZenECS 헤더 추가
            ZenEcsGUIHeader.DrawHeader(
                "Per-Entity Context",
                "Creates a unique context instance for every spawned entity.",
                new[] { "Context", "Per-Entity", "Runtime Factory" }
            );
            
            var asset   = (PerEntityContextAsset)target;
            var ctxType = asset.ContextType;

            // // ─────────────────────────────────────────────
            // // 1) Script (항상 맨 첫 줄, 읽기 전용)
            // // ─────────────────────────────────────────────
            // var scriptProp = serializedObject.FindProperty("m_Script");
            // if (scriptProp != null)
            // {
            //     using (new EditorGUI.DisabledScope(true))
            //     {
            //         EditorGUILayout.PropertyField(scriptProp);
            //     }
            // }
            //
            // EditorGUILayout.Space(4f);

            // ContextType이 유효한 경우에만 Context 정보 표시
            if (ctxType != null)
            {
                // ─────────────────────────────────────────
                // 2) ContextType명 헤더 + 우측 돋보기 아이콘
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

                // 클래스명 헤더 (굵게)
                EditorGUI.LabelField(headerLabelRect, ctxType.Name, EditorStyles.boldLabel);

                // 돋보기 아이콘 버튼 → 소스 Ping (Selection 변경 없음)
                using (new EditorGUI.DisabledScope(false))
                {
                    var gcPing = GetSearchIconContent("Ping context script in Project");
                    if (GUI.Button(pingRect, gcPing, EditorStyles.iconButton))
                    {
                        PingTypeSource(ctxType);
                    }
                }

                // ─────────────────────────────────────────
                // 3) Context Type / Namespace 정보
                // ─────────────────────────────────────────
                var typeRect = EditorGUILayout.GetControlRect();
                EditorGUI.LabelField(typeRect, "Context Type", "Per Entity Context");

                var nsRect = EditorGUILayout.GetControlRect();
                string ns = ctxType.Namespace ?? "(global)";
                EditorGUI.LabelField(nsRect, "Namespace", ns);

                EditorGUILayout.Space(4f);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "ContextType is null. This asset does not point to any context type.",
                    MessageType.Warning);
                EditorGUILayout.Space(4f);
            }

            // ─────────────────────────────────────────────
            // 4) Properties 헤더
            // ─────────────────────────────────────────────
            EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);

            // ─────────────────────────────────────────────
            // 5) 나머지는 Unity 기본 인스펙터 그대로
            //    (Script(m_Script)만 제외)
            // ─────────────────────────────────────────────
            EditorGUILayout.Space(2f);

            DrawPropertiesExcluding(serializedObject, "m_Script");

            serializedObject.ApplyModifiedProperties();
        }

        // ─────────────────────────────────────────────
        // Helpers (SharedContextMarker 스타일과 동일)
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
