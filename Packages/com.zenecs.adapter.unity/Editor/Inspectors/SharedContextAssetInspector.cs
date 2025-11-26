#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.EditorCommon;

namespace ZenECS.EditorInspectors
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
    public sealed class SharedContextAssetInspector : Editor
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
                EditorGUI.LabelField(typeRect, "Context Type", "Shared Context");

                var nsRect = EditorGUILayout.GetControlRect();
                string ns = ctxType.Namespace ?? "(global)";
                EditorGUI.LabelField(nsRect, "Namespace", ns);

                EditorGUILayout.Space(4f);

                // ─────────────────────────────────────────
                // 4) Context Type의 public 멤버들 표시
                //    (private 제외: public fields + public properties)
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
            // // 5) Properties 헤더
            // // ─────────────────────────────────────────────
            // EditorGUILayout.LabelField("Properties", EditorStyles.boldLabel);
            //
            // // ─────────────────────────────────────────────
            // // 6) 나머지는 Unity 기본 인스펙터 그대로
            // //    (Script(m_Script)만 제외)
            // // ─────────────────────────────────────────────
            // EditorGUILayout.Space(2f);
            //
            // DrawPropertiesExcluding(serializedObject, "m_Script");

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 지정된 타입의 "접근 가능한" 멤버들 수집.
        /// - private 제외
        /// - public instance 필드 + public instance 프로퍼티
        /// - DeclaredOnly: base 클래스(MonoBehaviour)의 transform/gameObject 등은 제외
        /// </summary>
        static List<(string name, string typeName)> CollectPublicMembers(Type ctxType)
        {
            var result = new List<(string, string)>();

            const BindingFlags flags =
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            // 1) public fields
            foreach (var f in ctxType.GetFields(flags))
            {
                // 필요하면 [HideInInspector] 같은 것만 필터링
                if (Attribute.IsDefined(f, typeof(HideInInspector), inherit: true))
                    continue;

                result.Add((f.Name, f.FieldType.Name));
            }

            // 2) public properties (getter 있는 것만, 인덱서 제외)
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
        // Helpers (ModelContextAssetInspector와 동일 패턴)
        // ─────────────────────────────────────────────

        static void PingTypeSource(Type? t)
        {
            if (t == null) return;

            var guids = AssetDatabase.FindAssets($"t:MonoScript {t.Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ms   = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms == null) continue;

                try
                {
                    if (ms.GetClass() == t)
                    {
                        // Project에서만 Ping, Selection은 그대로 유지
                        EditorGUIUtility.PingObject(ms);
                        break;
                    }
                }
                catch
                {
                    // GetClass() 실패 방어
                }
            }
        }

        static GUIContent GetSearchIconContent(string tooltip)
        {
            var gc = EditorGUIUtility.IconContent("d_Search Icon");
            if (gc == null || gc.image == null)
                gc = EditorGUIUtility.IconContent("Search Icon");

            if (gc == null)
                gc = new GUIContent("🔍", tooltip);
            else
                gc.tooltip = tooltip;

            return gc;
        }
    }
}
#endif
