#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ZenECS.Adapter.Unity.Attributes;
using ZenECS.Adapter.Unity.Install;
using ZenECS.Adapter.Unity.Util;
using ZenECS.Core;
using ZenECS.Core.Systems;
using ZenECS.EditorCommon;

namespace ZenECS.EditorInspectors
{
    [CustomEditor(typeof(SystemsPreset))]
    public sealed class SystemsPresetInspector : Editor
    {
        ReorderableList? _list;
        SerializedProperty? _propTypes;

        void OnEnable()
        {
            _propTypes = serializedObject.FindProperty("systemTypes");

            _list = new ReorderableList(serializedObject, _propTypes,
                draggable: true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true);

            _list.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "System Types (ISystem)", EditorStyles.boldLabel);

            // ─ elementHeight: System 타입 정보에 따라 줄 수 계산 ─
            _list.elementHeightCallback = index =>
            {
                const float pad = 6f;
                float line = EditorGUIUtility.singleLineHeight;

                if (_propTypes == null || index < 0 || index >= _propTypes.arraySize)
                    return line + pad * 2;

                var elem = _propTypes.GetArrayElementAtIndex(index);
                var aqn = elem.FindPropertyRelative("_assemblyQualifiedName").stringValue;
                var type = string.IsNullOrWhiteSpace(aqn) ? null : Type.GetType(aqn, false);

                // 기본: 이름 1줄 + 네임스페이스 1줄
                int lines = 2;

                if (type != null)
                {
                    if (!string.IsNullOrEmpty(GetSystemGroupSummary(type)))
                        lines++; // Group

                    if (!string.IsNullOrEmpty(GetSystemRunKinds(type)))
                        lines++; // Run

                    // 🔹 여기부터 수정: Watch 줄수 = "Watch:" 한 줄 + 컴포넌트 개수
                    var watched = GetSystemWatchedComponents(type);
                    if (watched.Count > 0)
                        lines += 1 + watched.Count;
                }

                return pad * 2 + lines * line;
            };

            // ─ 각 System 항목 렌더링 ─
            _list.drawElementCallback = (rect, index, active, focused) =>
            {
                if (_propTypes == null || index < 0 || index >= _propTypes.arraySize)
                    return;

                var elem = _propTypes.GetArrayElementAtIndex(index);
                var aqn = elem.FindPropertyRelative("_assemblyQualifiedName").stringValue;
                var type = string.IsNullOrWhiteSpace(aqn) ? null : Type.GetType(aqn, false);

                float line = EditorGUIUtility.singleLineHeight;
                float x = rect.x + 6f;
                float w = rect.width - 12f;
                float y = rect.y + 3f;

                if (type != null)
                {
                    string ns = type.Namespace ?? "Global";
                    string name = type.Name;

                    // ─ 1) 이름 + 우측 돋보기 버튼 ─
                    var headRect = new Rect(x, y, w, line);
                    const float pingW = 20f;
                    var pingRect = new Rect(headRect.xMax - pingW, headRect.y, pingW, headRect.height);
                    var nameRect = new Rect(headRect.x, headRect.y, headRect.width - pingW - 4f, headRect.height);

                    var nameStyle = new GUIStyle(EditorStyles.label) { richText = true };
                    EditorGUI.LabelField(nameRect, $"<b>{name}</b>", nameStyle);

                    using (new EditorGUI.DisabledScope(false))
                    {
                        var gcPing = GetSearchIconContent("Ping system script in Project");
                        if (GUI.Button(pingRect, gcPing, EditorStyles.iconButton))
                        {
                            PingTypeSource(type);
                        }
                    }

                    y += line;

                    // ─ 2) 네임스페이스 ─
                    var nsStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        richText = true,
                        normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                    };
                    var nsRect = new Rect(x + 4f, y, w - 4f, line);
                    EditorGUI.LabelField(nsRect, $"[{ns}]", nsStyle);
                    y += line;

                    // 공통 info 스타일
                    var infoStyle = new GUIStyle(EditorStyles.miniLabel) { richText = true };

                    // ─ 3) Group 정보 ─
                    var groupSummary = GetSystemGroupSummary(type);
                    if (!string.IsNullOrEmpty(groupSummary))
                    {
                        var r = new Rect(x + 8f, y, w - 8f, line);
                        EditorGUI.LabelField(r, $"Group: <b>{groupSummary}</b>", infoStyle);
                        y += line;
                    }

                    // ─ 4) RunSystem 종류 ─
                    var runKinds = GetSystemRunKinds(type); // 예: "IVariableRunSystem"
                    if (!string.IsNullOrEmpty(runKinds))
                    {
                        var r = new Rect(x + 8f, y, w - 8f, line);
                        EditorGUI.LabelField(r, $"Run: <b>{runKinds}</b>", infoStyle);
                        y += line;
                    }

                    // ─ 5) Watch 대상 (목록) ─
                    var watched = GetSystemWatchedComponents(type);
                    if (watched.Count > 0)
                    {
                        // "Watch:" 라벨 한 줄
                        var rLabel = new Rect(x + 8f, y, w - 8f, line);
                        EditorGUI.LabelField(rLabel, "Watch:", infoStyle);
                        y += line;

                        // 각 컴포넌트 한 줄씩: "• Name [Namespace]" + 클릭 시 Ping
                        foreach (var ct in watched)
                        {
                            if (ct == null) continue;

                            string compName = ct.Name;
                            string compNs = ct.Namespace ?? "Global";

                            var itemRect = new Rect(x + 16f, y, w - 16f, line);

                            // richText 라벨: 컴포넌트명 + 짙은 회색 네임스페이스
                            // Namespace 부분만 색 태그 적용
                            string labelText =
                                $"• <b>{compName}</b> <color=#777777>[{compNs}]</color>";

                            EditorGUI.LabelField(
                                itemRect,
                                new GUIContent(labelText, ct.AssemblyQualifiedName),
                                infoStyle
                            );

                            // 클릭 시 해당 컴포넌트 스크립트 Ping
                            var e = Event.current;
                            if (e.type == EventType.MouseDown && e.button == 0 && itemRect.Contains(e.mousePosition))
                            {
                                PingTypeSource(ct);
                                e.Use();
                            }

                            y += line;
                        }
                    }
                }
                else
                {
                    var style = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = new Color(1f, 0.35f, 0.35f) }
                    };
                    var lineRect = new Rect(x, y, w, line);
                    EditorGUI.LabelField(lineRect, "None (Click + to add with picker)", style);
                }
            };

            // onAdd / onRemove / CleanupInvalidDuplicates / ShowPickerForIndex는 기존 그대로
            _list.onAddCallback = list =>
            {
                serializedObject.Update();
                var newIndex = _propTypes!.arraySize;
                _propTypes.InsertArrayElementAtIndex(newIndex);
                var elem = _propTypes.GetArrayElementAtIndex(newIndex);
                elem.FindPropertyRelative("_assemblyQualifiedName").stringValue = string.Empty;
                serializedObject.ApplyModifiedProperties();

                ShowPickerForIndex(newIndex, () =>
                {
                    serializedObject.Update();
                    var e = _propTypes.GetArrayElementAtIndex(newIndex);
                    var aqn = e.FindPropertyRelative("_assemblyQualifiedName").stringValue;
                    if (string.IsNullOrWhiteSpace(aqn))
                    {
                        _propTypes.DeleteArrayElementAtIndex(newIndex);
                        serializedObject.ApplyModifiedProperties();
                    }
                });
            };

            _list.onRemoveCallback = list =>
            {
                if (_propTypes!.arraySize <= 0) return;
                _propTypes.DeleteArrayElementAtIndex(list.index);
                serializedObject.ApplyModifiedProperties();
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // ── 시스템 목록 ────────────────────────────────────────────────────
            _list!.DoLayoutList();

            // (선택) 정리 버튼: 빈 슬롯은 유지, 값 있는 항목만 유효성/중복 정리
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Clean Invalid & Duplicates", GUILayout.Width(220)))
                    CleanupInvalidDuplicates();
            }

            serializedObject.ApplyModifiedProperties();
        }

        void CleanupInvalidDuplicates()
        {
            if (_propTypes == null) return;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = _propTypes.arraySize - 1; i >= 0; i--)
            {
                var elem = _propTypes.GetArrayElementAtIndex(i);
                var aqn = elem.FindPropertyRelative("_assemblyQualifiedName").stringValue;

                if (string.IsNullOrWhiteSpace(aqn)) continue; // 빈 슬롯 유지

                var t = Type.GetType(aqn, false);
                if (t == null || t.IsAbstract || !typeof(ISystem).IsAssignableFrom(t) || !seen.Add(aqn))
                {
                    _propTypes.DeleteArrayElementAtIndex(i);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        void ShowPickerForIndex(int index, Action onCancel)
        {
            // 모든 ISystem 타입 수집
            var all = TypeCache.GetTypesDerivedFrom<ISystem>()
                .Where(t => t != null && !t.IsAbstract)
                .Distinct()
                .OrderBy(t => t.FullName)
                .ToList();

            // 이미 담긴 타입은 disabled (현재 index 제외)
            var disabled = new HashSet<Type>();
            for (int i = 0; i < _propTypes!.arraySize; i++)
            {
                if (i == index) continue;
                var elem = _propTypes.GetArrayElementAtIndex(i);
                var aqn = elem.FindPropertyRelative("_assemblyQualifiedName").stringValue;
                var t = string.IsNullOrWhiteSpace(aqn) ? null : Type.GetType(aqn, false);
                if (t != null) disabled.Add(t);
            }

            // 리스트 끝 근처에 드롭다운 표시
            var rect = GUILayoutUtility.GetLastRect();
            rect = new Rect(rect.x + rect.width - 200, rect.yMax, 200, 20);

            ZenSystemPickerWindow.Show(
                allSystemTypes: all,
                disabled: disabled,
                onPick: (t) =>
                {
                    serializedObject.Update();
                    var elem = _propTypes.GetArrayElementAtIndex(index);
                    elem.FindPropertyRelative("_assemblyQualifiedName").stringValue = t.AssemblyQualifiedName;
                    serializedObject.ApplyModifiedProperties();
                    Repaint();
                },
                activatorRectGui: rect,
                title: "Add System",
                onCancel: onCancel);
        }

        // ─ 어떤 그룹인지: SimulationGroup 등 GroupAttribute 기반 요약 ─
        static string GetSystemGroupSummary(Type t)
        {
            return SystemUtil.ResolveGroup(t).ToString();
        }

        // ─ 어떤 RunSystem 인터페이스를 구현하는지 요약 ─
        static string GetSystemRunKinds(Type t)
        {
            if (t == null) return string.Empty;

            // 런타임에 인터페이스 풀네임 기준으로 검사 (컴파일 의존성 줄이기)
            bool Implements(string fullName)
                => t.GetInterfaces().Any(i => string.Equals(i.FullName, fullName, StringComparison.Ordinal));

            var kinds = new List<string>();

            if (Implements("ZenECS.Core.Systems.IInitSystem"))
                kinds.Add("IInitSystem");
            if (Implements("ZenECS.Core.Systems.IRunSystem"))
                kinds.Add("IRunSystem");
            if (Implements("ZenECS.Core.Systems.IVariableRunSystem"))
                kinds.Add("IVariableRunSystem");
            if (Implements("ZenECS.Core.Systems.IFixedRunSystem"))
                kinds.Add("IFixedRunSystem");
            if (Implements("ZenECS.Core.Systems.ILateRunSystem"))
                kinds.Add("ILateRunSystem");
            if (Implements("ZenECS.Core.Systems.ICleanupSystem"))
                kinds.Add("ICleanupSystem");

            if (kinds.Count == 0)
                return "ISystem";

            return string.Join(", ", kinds);
        }

        // ─ ZenSystemWatch로 어떤 컴포넌트를 Watch하는지 요약 ─
        static string GetSystemWatchSummary(Type t)
        {
            if (t == null) return string.Empty;

            var attrs = (ZenSystemWatchAttribute[])t
                .GetCustomAttributes(typeof(ZenSystemWatchAttribute), inherit: false);

            if (attrs == null || attrs.Length == 0)
                return string.Empty;

            var watched = new List<string>();
            foreach (var a in attrs)
            {
                if (a.AllOf == null) continue;
                foreach (var ct in a.AllOf)
                {
                    if (ct == null) continue;
                    watched.Add(ct.Name);
                }
            }

            if (watched.Count == 0) return string.Empty;

            // ex: "Rotation, Translation"
            var distinct = watched.Distinct().ToArray();
            return $"<b>{string.Join(", ", distinct)}</b>";
        }

        static void PingTypeSource(Type? t)
        {
            if (t == null) return;

            // MonoScript 검색
            var guids = AssetDatabase.FindAssets($"t:MonoScript {t.Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms == null) continue;

                if (ms.GetClass() == t)
                {
                    EditorGUIUtility.PingObject(ms);
                    break;
                }
            }
        }

        static GUIContent GetSearchIconContent(string tooltip)
        {
            // Unity 기본 돋보기 아이콘
            var gc = EditorGUIUtility.IconContent("d_Search Icon");
            if (gc == null || gc.image == null)
                gc = EditorGUIUtility.IconContent("Search Icon");

            if (gc == null)
                gc = new GUIContent("🔍", tooltip);
            else
                gc.tooltip = tooltip;

            return gc;
        }

        // ─ ZenSystemWatch로 어떤 컴포넌트들을 Watch하는지 목록 ─
        static List<Type> GetSystemWatchedComponents(Type t)
        {
            var result = new List<Type>();
            if (t == null) return result;

            var attrs = (ZenSystemWatchAttribute[])t
                .GetCustomAttributes(typeof(ZenSystemWatchAttribute), inherit: false);

            if (attrs == null || attrs.Length == 0)
                return result;

            foreach (var a in attrs)
            {
                if (a.AllOf == null) continue;
                foreach (var ct in a.AllOf)
                {
                    if (ct == null) continue;
                    result.Add(ct);
                }
            }

            // 중복 제거
            return result
                .Where(c => c != null)
                .Distinct()
                .ToList();
        }
    }
}
#endif