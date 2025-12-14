// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: SystemPresetInspector.cs
// Purpose: Custom inspector for SystemsPreset ScriptableObject that provides
//          editing UI for system type collections and metadata.
// Key concepts:
//   • System type editing: ReorderableList for SystemTypeRef entries.
//   • Metadata display: system group, execution order, watch attributes.
//   • Type picker: custom UI for selecting ISystem implementation types.
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
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Adapter.Unity.Editor.GUIs;
using ZenECS.Adapter.Unity.SystemPresets;
using ZenECS.Core;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.Editor.Inspectors
{
    [CustomEditor(typeof(SystemsPreset))]
    public sealed class SystemsPresetInspector : UnityEditor.Editor
    {
        ReorderableList? _list;
        SerializedProperty? _propTypes;

        readonly Dictionary<int, bool> _foldouts = new();
        bool _globalExpanded = false;

        string _filterText = string.Empty;

        bool UseFilter => !string.IsNullOrWhiteSpace(_filterText);

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

            _list.elementHeightCallback = CalcElementHeight;
            _list.drawElementCallback = (rect, index, active, focused) =>
                DrawElement(rect, index, active, focused);

            // Add / Remove keep as-is
            _list.onAddDropdownCallback = (buttonRect, list) =>
            {
                serializedObject.Update();
                var newIndex = _propTypes!.arraySize;
                _propTypes.InsertArrayElementAtIndex(newIndex);
                var elem = _propTypes.GetArrayElementAtIndex(newIndex);
                elem.FindPropertyRelative("_assemblyQualifiedName").stringValue = string.Empty;
                serializedObject.ApplyModifiedProperties();

                ShowPickerForIndex(newIndex, buttonRect, () =>
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

            _list.onAddCallback = list =>
            {
                var mousePos = Event.current != null ? Event.current.mousePosition : Vector2.zero;
                var rect = new Rect(mousePos.x, mousePos.y, 1, 1);

                serializedObject.Update();
                var newIndex = _propTypes!.arraySize;
                _propTypes.InsertArrayElementAtIndex(newIndex);
                var elem = _propTypes.GetArrayElementAtIndex(newIndex);
                elem.FindPropertyRelative("_assemblyQualifiedName").stringValue = string.Empty;
                serializedObject.ApplyModifiedProperties();

                ShowPickerForIndex(newIndex, rect, () =>
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
                _foldouts.Clear();
            };
        }

        public override void OnInspectorGUI()
        {
            var icon = EditorGUIUtility.ObjectContent(target, target.GetType()).image;

            ZenEcsGUIHeader.DrawHeader(
                "System Preset",
                "Defines a reusable group of systems that can be registered to a world in a single operation.",
                new[]
                {
                    "Editor Tool",
                    "System Group",
                    "Runtime-Ready"
                }
            );
            
            serializedObject.Update();

            DrawTopToolbar();

            GUILayout.Space(4);

            if (!UseFilter)
            {
                // If no filter, use ReorderableList as-is (includes drag handle)
                _list!.DoLayoutList();
            }
            else
            {
                // When filter exists, custom view without "=" handle
                DrawFilteredList();
            }

            serializedObject.ApplyModifiedProperties();
        }

        #region Top Toolbar (Expand/Collapse + Filter)

        void DrawTopToolbar()
        {
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Expand All", GUILayout.Width(120)))
                {
                    _globalExpanded = true;
                    if (_propTypes != null)
                    {
                        for (int i = 0; i < _propTypes.arraySize; i++)
                            _foldouts[i] = true;
                    }
                }

                if (GUILayout.Button("Collapse All", GUILayout.Width(120)))
                {
                    _globalExpanded = false;
                    if (_propTypes != null)
                    {
                        for (int i = 0; i < _propTypes.arraySize; i++)
                            _foldouts[i] = false;
                    }
                }

                GUILayout.FlexibleSpace();

                // Right-aligned Filter
                var searchTextStyle = GUI.skin.FindStyle("ToolbarSeachTextField") ?? GUI.skin.textField;
                var cancelStyle = GUI.skin.FindStyle("ToolbarSeachCancelButton");

                GUILayout.Label("Filter", GUILayout.Width(40));
                _filterText = EditorGUILayout.TextField(_filterText, searchTextStyle, GUILayout.MaxWidth(200));

                if (cancelStyle != null)
                {
                    if (GUILayout.Button(GUIContent.none, cancelStyle))
                    {
                        _filterText = string.Empty;
                        GUI.FocusControl(null);
                    }
                }
                else
                {
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        _filterText = string.Empty;
                        GUI.FocusControl(null);
                    }
                }
            }
        }

        #endregion

        #region Filtered View (no "=" handle)

        void DrawFilteredList()
        {
            if (_propTypes == null)
                return;

            var indices = new List<int>();

            for (int i = 0; i < _propTypes.arraySize; i++)
            {
                var elem = _propTypes.GetArrayElementAtIndex(i);
                var aqn = elem.FindPropertyRelative("_assemblyQualifiedName").stringValue;
                var type = string.IsNullOrWhiteSpace(aqn) ? null : Type.GetType(aqn, false);

                if (PassFilter(type))
                    indices.Add(i);
            }

            if (indices.Count == 0)
            {
                EditorGUILayout.HelpBox("No systems match current filter.", MessageType.Info);
                return;
            }

            // Header shape similar to ReorderableList once more
            var headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(headerRect, "Filtered Systems", EditorStyles.boldLabel);

            GUILayout.Space(2);

            foreach (var idx in indices)
            {
                float h = CalcElementHeight(idx);
                var r = EditorGUILayout.GetControlRect(false, h);
                DrawElementWithoutHandle(r, idx);
                GUILayout.Space(2);
            }

            GUILayout.Space(4);

            // Filter mode should also have one add button for convenience
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add System...", GUILayout.Width(120)))
                {
                    var mousePos = Event.current != null ? Event.current.mousePosition : Vector2.zero;
                    var rect = new Rect(mousePos.x, mousePos.y, 1, 1);

                    serializedObject.Update();
                    var newIndex = _propTypes.arraySize;
                    _propTypes.InsertArrayElementAtIndex(newIndex);
                    var elem = _propTypes.GetArrayElementAtIndex(newIndex);
                    elem.FindPropertyRelative("_assemblyQualifiedName").stringValue = string.Empty;
                    serializedObject.ApplyModifiedProperties();

                    ShowPickerForIndex(newIndex, rect, () =>
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
                }
            }
        }

        #endregion

        #region Element Height & Drawing

        float CalcElementHeight(int index)
        {
            const float pad = 6f;
            float line = EditorGUIUtility.singleLineHeight;

            if (_propTypes == null || index < 0 || index >= _propTypes.arraySize)
                return line + pad * 2;

            var elemBase = _propTypes.GetArrayElementAtIndex(index);
            var aqnBase = elemBase.FindPropertyRelative("_assemblyQualifiedName").stringValue;
            var typeBase = string.IsNullOrWhiteSpace(aqnBase) ? null : Type.GetType(aqnBase, false);

            bool opened = _globalExpanded;
            if (_foldouts.TryGetValue(index, out var v))
                opened = v;

            if (!opened)
            {
                int lines = (typeBase != null) ? 2 : 1;
                return pad * 2 + lines * line;
            }

            int openedLines = 2; // name + namespace

            if (typeBase != null)
            {
                if (!string.IsNullOrEmpty(GetSystemGroupSummary(typeBase)))
                    openedLines++;

                if (!string.IsNullOrEmpty(GetSystemRunKinds(typeBase)))
                    openedLines++;

                // Order Before/After (Attribute-based)
                openedLines += 2;
                
                var watched = GetSystemWatchedComponents(typeBase);
                if (watched.Count > 0)
                    openedLines += 1 + watched.Count;
            }

            return pad * 2 + openedLines * line;
        }

        void DrawElement(Rect rect, int index, bool active, bool focused)
        {
            // Version called from ReorderableList (= has handle)
            InternalDrawElement(rect, index, drawHandleOffset: true);
        }

        void DrawElementWithoutHandle(Rect rect, int index)
        {
            // Filter-only version (= no handle)
            InternalDrawElement(rect, index, drawHandleOffset: false);
        }

        void InternalDrawElement(Rect rect, int index, bool drawHandleOffset)
        {
            if (_propTypes == null || index < 0 || index >= _propTypes.arraySize)
                return;

            var elem = _propTypes.GetArrayElementAtIndex(index);
            var aqn = elem.FindPropertyRelative("_assemblyQualifiedName").stringValue;
            var type = string.IsNullOrWhiteSpace(aqn) ? null : Type.GetType(aqn, false);

            float line = EditorGUIUtility.singleLineHeight;

            // Add more margin when ReorderableList handle exists
            float handleOffset = drawHandleOffset ? 10f : 0f;

            const float padX = 0f;
            const float padRight = 6f;
            float x = rect.x + padX + handleOffset;
            float y = rect.y + 1f;
            float fullW = rect.width - padX - padRight - handleOffset;

            if (!_foldouts.ContainsKey(index))
                _foldouts[index] = _globalExpanded;

            const float foldW = 14f;
            Rect foldRect = new Rect(x, y, foldW, line);
            _foldouts[index] = EditorGUI.Foldout(foldRect, _foldouts[index], GUIContent.none);

            var nameStyle = new GUIStyle(EditorStyles.label) { richText = true };
            var miniGrayStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                richText = true,
                normal = { textColor = new Color(0.4f, 0.4f, 0.4f) }
            };
            var infoStyle = new GUIStyle(EditorStyles.miniLabel) { richText = true };

            float contentX = foldRect.xMax - 14f;
            float contentW = rect.x + rect.width - padRight - contentX;

            if (!_foldouts[index])
            {
                const float pingW = 20f;

                if (type == null)
                {
                    string label = "None (empty slot)";

                    Rect pingRect = new Rect(
                        rect.x + rect.width - padRight - pingW,
                        y,
                        pingW,
                        line);

                    Rect nameRect = new Rect(
                        contentX,
                        y,
                        pingRect.xMin - 2f - contentX,
                        line);

                    EditorGUI.LabelField(nameRect, label, nameStyle);
                    return;
                }

                string ns = type.Namespace ?? "Global";
                string labelName = $"<b>{type.Name}</b>";

                Rect pingRectCollapsed = new Rect(
                    rect.x + rect.width - padRight - pingW,
                    y,
                    pingW,
                    line);

                Rect nameRectCollapsed = new Rect(
                    contentX,
                    y,
                    pingRectCollapsed.xMin - 2f - contentX,
                    line);

                EditorGUI.LabelField(nameRectCollapsed, labelName, nameStyle);

                using (new EditorGUI.DisabledScope(false))
                {
                    var gcPing = GetSearchIconContent("Ping system script in Project");
                    if (GUI.Button(pingRectCollapsed, gcPing, EditorStyles.iconButton))
                    {
                        PingTypeSource(type);
                    }
                }

                y += line;
                Rect nsRectCollapsed = new Rect(contentX + 4f, y, contentW - 4f, line);
                EditorGUI.LabelField(nsRectCollapsed, $"[{ns}]", miniGrayStyle);
                return;
            }

            if (type != null)
            {
                string ns = type.Namespace ?? "Global";
                string name = type.Name;

                const float pingW2 = 20f;
                Rect headRect = new Rect(contentX, y, contentW, line);
                Rect pingRect = new Rect(
                    rect.x + rect.width - padRight - pingW2,
                    y,
                    pingW2,
                    line);
                Rect nameRect = new Rect(
                    headRect.x,
                    y,
                    pingRect.xMin - 2f - headRect.x,
                    line);

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

                Rect nsRect = new Rect(contentX + 4f, y, contentW - 4f, line);
                EditorGUI.LabelField(nsRect, $"[{ns}]", miniGrayStyle);
                y += line;

                var groupSummary = GetSystemGroupSummary(type);
                if (!string.IsNullOrEmpty(groupSummary))
                {
                    Rect r = new Rect(contentX + 8f, y, contentW - 8f, line);
                    EditorGUI.LabelField(r, $"Group: <color=#909090>{groupSummary}</color>", infoStyle);
                    y += line;
                }

                var runKinds = GetSystemRunKinds(type);
                if (!string.IsNullOrEmpty(runKinds))
                {
                    Rect r = new Rect(contentX + 8f, y, contentW - 8f, line);
                    EditorGUI.LabelField(r, $"Execution: <color=#909090>{runKinds}</color>", infoStyle);
                    y += line;
                }

                // Order Before/After (Attribute-based)
                var beforeList = new List<string>();
                var afterList = new List<string>();

                try
                {
                    var beforeAttrs = type.GetCustomAttributes(typeof(OrderBeforeAttribute), true)
                        .Cast<OrderBeforeAttribute>();
                    foreach (var a in beforeAttrs)
                    {
                        var target = a.Target;
                        if (target != null)
                            beforeList.Add(target.Name);
                    }
                
                    var afterAttrs = type.GetCustomAttributes(typeof(OrderAfterAttribute), true)
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

                var orderBeforeRect = new Rect(contentX + 8f, y, contentW - 8f, line);
                EditorGUI.LabelField(orderBeforeRect, $"Order Before: <color=#909090>{beforeText}</color>", infoStyle);
                y += line;

                var orderAfterRect = new Rect(contentX + 8f, y, contentW - 8f, line);
                EditorGUI.LabelField(orderAfterRect, $"Order After: <color=#909090>{afterText}</color>", infoStyle);
                y += line;
                
                var watched = GetSystemWatchedComponents(type);
                if (watched.Count > 0)
                {
                    Rect rLabel = new Rect(contentX + 8f, y, contentW - 8f, line);
                    EditorGUI.LabelField(rLabel, "Watched", infoStyle);
                    y += line;

                    foreach (var ct in watched)
                    {
                        if (ct == null) continue;

                        string compName = ct.Name;
                        string compNs = ct.Namespace ?? "Global";

                        Rect itemRect = new Rect(contentX + 16f, y, contentW - 16f, line);
                        string labelText =
                            $"• {compName} <color=#909090>[{compNs}]</color>";

                        EditorGUI.LabelField(
                            itemRect,
                            new GUIContent(labelText, ct.AssemblyQualifiedName),
                            infoStyle
                        );

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
                Rect lineRect = new Rect(contentX, y, contentW, line);
                var style = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(1f, 0.35f, 0.35f) }
                };
                EditorGUI.LabelField(lineRect, "None (Click + to add with picker)", style);
            }
        }

        #endregion

        #region Picker & Utility

        bool PassFilter(Type? type)
        {
            if (!UseFilter)
                return true;

            if (type == null)
                return false;

            var name = type.Name ?? string.Empty;
            return name.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void ShowPickerForIndex(int index, Rect activatorRectGui, Action onCancel)
        {
            var all = TypeCache.GetTypesDerivedFrom<ISystem>()
                .Where(t => t != null && !t.IsAbstract)
                .Distinct()
                .OrderBy(t => t.FullName)
                .ToList();

            var disabled = new HashSet<Type>();
            for (int i = 0; i < _propTypes!.arraySize; i++)
            {
                if (i == index) continue;
                var elem = _propTypes.GetArrayElementAtIndex(i);
                var aqn = elem.FindPropertyRelative("_assemblyQualifiedName").stringValue;
                var t = string.IsNullOrWhiteSpace(aqn) ? null : Type.GetType(aqn, false);
                if (t != null) disabled.Add(t);
            }

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
                activatorRectGui: activatorRectGui,
                title: "Add System",
                onCancel: onCancel);
        }

        static string GetSystemGroupSummary(Type t)
        {
            return SystemUtil.ResolveGroup(t).ToString();
        }

        static string GetSystemRunKinds(Type t)
        {
            if (t == null) return string.Empty;

            bool Implements(string fullName)
                => t.GetInterfaces().Any(i => string.Equals(i.FullName, fullName, StringComparison.Ordinal));

            var kinds = new List<string>();

            if (Implements("ZenECS.Core.Systems.IFrameSetupSystem"))
                kinds.Add("Variable (IFrameSetupSystem)");
            if (Implements("ZenECS.Core.Systems.IFixedSetupSystem"))
                kinds.Add("Fixed (IFixedSetupSystem)");
            if (Implements("ZenECS.Core.Systems.IVariableRunSystem"))
                kinds.Add("Variable (IVariableRunSystem)");
            if (Implements("ZenECS.Core.Systems.IFixedRunSystem"))
                kinds.Add("Fixed (IFixedRunSystem)");
            if (Implements("ZenECS.Core.Systems.IPresentationSystem"))
                kinds.Add("Presentation (IPresentationSystem)");

            if (kinds.Count == 0)
                return "ISystem";

            return string.Join(", ", kinds);
        }

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

            return result
                .Where(c => c != null)
                .Distinct()
                .ToList();
        }

        static void PingTypeSource(Type? t)
        {
            ZenUtil.PingType(t);
        }

        static GUIContent GetSearchIconContent(string tooltip)
        {
            return ZenGUIContents.IconPing(tooltip);
        }

        #endregion
    }
}
#endif
