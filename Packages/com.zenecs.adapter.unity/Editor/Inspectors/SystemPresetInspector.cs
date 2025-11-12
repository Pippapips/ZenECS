#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ZenECS.Adapter.Unity.Install;
using ZenECS.Adapter.Unity.Util;
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

            _list = new ReorderableList(serializedObject, _propTypes, draggable: true, displayHeader: true,
                displayAddButton: true, displayRemoveButton: true);
            _list.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "System Types (ISystem)", EditorStyles.boldLabel);
            _list.elementHeight = EditorGUIUtility.singleLineHeight + 15;

            _list.drawElementCallback = (rect, index, active, focused) =>
            {
                var elem = _propTypes!.GetArrayElementAtIndex(index);
                var aqnStr = elem.FindPropertyRelative("_assemblyQualifiedName").stringValue;
                var type = string.IsNullOrWhiteSpace(aqnStr) ? null : Type.GetType(aqnStr, false);

                var lineRect = new Rect(rect.x + 6, rect.y + 3, rect.width - 12,
                    EditorGUIUtility.singleLineHeight + 10);

                if (type != null)
                {
                    // Name (namespace) + tooltip에 AQN
                    var style = new GUIStyle(EditorStyles.label) { richText = true };
                    var ns = type.Namespace ?? "Global";
                    var name = type.Name;
                    var text = $"<b>{name}</b>\n<color=#888888><size=10>({ns})</size></color>";
                    EditorGUI.LabelField(lineRect, new GUIContent(text, type.AssemblyQualifiedName), style);
                }
                else
                {
                    var style = new GUIStyle(EditorStyles.label)
                        { normal = { textColor = new Color(1f, 0.35f, 0.35f) } };
                    EditorGUI.LabelField(lineRect, "None (Click + to add with picker)", style);
                }
            };

            _list.onAddCallback = list =>
            {
                serializedObject.Update();
                var newIndex = _propTypes!.arraySize;
                _propTypes.InsertArrayElementAtIndex(newIndex);
                var elem = _propTypes.GetArrayElementAtIndex(newIndex);
                elem.FindPropertyRelative("_assemblyQualifiedName").stringValue = string.Empty;
                serializedObject.ApplyModifiedProperties();

                // 피커 오픈: 선택 시 채우고, 취소 시 빈 항목 제거
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
    }
}
#endif