// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenComponentPickerWindow.cs
// Purpose: Searchable picker window for selecting ECS component types in
//          ZenECS editor tooling.
// Key concepts:
//   • Component type selection: filters and displays available component types.
//   • Searchable: toolbar with search field for filtering types.
//   • Derived from ZenPickerWindowBase: inherits common picker functionality.
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
using UnityEditor;
using UnityEngine;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Editor.GUIs
{
    /// <summary>
    /// Searchable component type picker window.
    /// Uses ZenPickerWindowBase but keeps namespace dropdown + fixed-width utility mode.
    /// </summary>
    internal sealed class ZenComponentPickerWindow : ZenPickerWindowBase<Type>
    {
        /// <summary>
        /// Represents the mode in which the component picker window opens.
        /// </summary>
        public enum PickerOpenMode
        {
            /// <summary>
            /// Dropdown mode. The window is displayed as a dropdown.
            /// </summary>
            DropDown,
            
            /// <summary>
            /// Fixed-width utility window mode. The window is displayed as a utility window with fixed width.
            /// </summary>
            UtilityFixedWidth
        }

        private const float WINDOW_WIDTH  = 560f;
        private const float WINDOW_MIN_H  = 320f;
        private const float WINDOW_MAX_H  = 2000f;
        private const float WINDOW_INIT_H = 680f;

        private PickerOpenMode _openMode = PickerOpenMode.DropDown;

        private readonly List<Type> _all = new();
        private HashSet<Type> _disabled = new();

        private readonly List<string> _nsOptions = new();
        private int _nsIndex;
        private Action<Type>? _onPick;

        protected override void OnEnable()
        {
            base.OnEnable();
            // Simplified row style similar to PR Label (Context Picker Window style)
            _rowStyle = new GUIStyle("PR Label")
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = ROW_HEIGHT,
                richText = true
            };

            minSize = new Vector2(WINDOW_WIDTH, WINDOW_MIN_H);
            maxSize = new Vector2(WINDOW_WIDTH, WINDOW_MAX_H);

            // Focus search field on open
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                Focus();
                EditorGUI.FocusTextInControl("ZenPickerSearch");
            };
        }

        public static void Show(
            IEnumerable<Type> allTypes,
            IEnumerable<Type> disabled,
            Action<Type> onPick,
            Rect? activatorRectGui = null,
            string title = "Add Component",
            PickerOpenMode mode = PickerOpenMode.DropDown)
        {
            var win = CreateInstance<ZenComponentPickerWindow>();

            win._all.Clear();
            win._all.AddRange(allTypes
                .Where(t => t != null)
                .Distinct()
                .OrderBy(t => t.FullName));

            win._disabled = new HashSet<Type>(disabled ?? Array.Empty<Type>());
            win._onPick = onPick ?? (_ => { });
            win._title = title;
            win._openMode = mode;
            win._closeOnLostFocus = (mode == PickerOpenMode.DropDown);

            // Namespace list: (All), (global), others...
            var nsSet = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var t in win._all)
            {
                nsSet.Add(t.Namespace ?? "(global)");
            }

            win._nsOptions.Clear();
            win._nsOptions.Add("(All)");

            if (nsSet.Remove("(global)"))
            {
                win._nsOptions.Add("(global)");
            }

            win._nsOptions.AddRange(nsSet);

            float initH = Mathf.Clamp(WINDOW_INIT_H, WINDOW_MIN_H, WINDOW_MAX_H);

            // Anchor rect (in screen space)
            Rect anchorScr;
            if (activatorRectGui.HasValue && activatorRectGui.Value.width > 0f)
            {
                anchorScr = GUIToScreenRect(activatorRectGui.Value);
            }
            else
            {
                var mp = Event.current != null
                    ? GUIUtility.GUIToScreenPoint(Event.current.mousePosition)
                    : new Vector2(Screen.currentResolution.width * 0.5f, Screen.currentResolution.height * 0.5f);

                anchorScr = new Rect(mp.x, mp.y, 1f, 1f);
            }

            var editorRect = GetEditorScreenRect();
            float x = anchorScr.xMin;

            if (x + WINDOW_WIDTH > editorRect.xMax)
                x = anchorScr.xMax - WINDOW_WIDTH;

            x = Mathf.Clamp(x, editorRect.xMin + 6f, editorRect.xMax - WINDOW_WIDTH - 6f);

            float y;
            float spaceBelow = editorRect.yMax - (anchorScr.yMax + 6f);
            float spaceAbove = (anchorScr.yMin - 6f) - editorRect.yMin;

            if (spaceBelow >= initH)
            {
                y = anchorScr.yMax + 2f;
            }
            else if (spaceAbove >= initH)
            {
                y = anchorScr.yMin - initH - 2f;
            }
            else
            {
                float maxH = Mathf.Clamp(Mathf.Max(spaceBelow, spaceAbove), WINDOW_MIN_H, WINDOW_MAX_H);
                initH = maxH;

                y = (spaceBelow >= spaceAbove)
                    ? Mathf.Clamp(anchorScr.yMax + 2f, editorRect.yMin + 6f, editorRect.yMax - initH - 6f)
                    : Mathf.Clamp(anchorScr.yMin - initH - 2f, editorRect.yMin + 6f, editorRect.yMax - initH - 6f);
            }

            if (mode == PickerOpenMode.DropDown)
            {
                win.ShowAsDropDown(anchorScr, new Vector2(WINDOW_WIDTH, initH));
                win.Focus();
                return;
            }

            win.position = new Rect(x, y, WINDOW_WIDTH, initH);
            win.ShowUtility();
            win.Focus();
        }

        // --- Base hooks -----------------------------------------------------

        protected override IEnumerable<Type> GetSourceItems() => _all;

        protected override bool IsDisabled(Type item) => _disabled.Contains(item);

        protected override bool MatchesSearch(Type t, string search)
        {
            if (t == null) return false;

            // Namespace filter
            if (_nsOptions.Count > 0 && _nsIndex > 0)
            {
                string sel = _nsOptions[_nsIndex];
                string ns = t.Namespace ?? "(global)";
                if (!string.Equals(sel, ns, StringComparison.Ordinal))
                    return false;
            }

            if (string.IsNullOrWhiteSpace(search))
                return true;

            return (t.Name?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0
                   || (t.FullName?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
        }

        protected override IEnumerable<Type> OrderItems(IEnumerable<Type> items)
        {
            return items.Where(t => t != null).OrderBy(t => t.FullName);
        }

        protected override string GetEmptyMessage() => "No matching components.";

        protected override GUIContent GetItemContent(Type t, bool disabled)
        {
            string nsStr = string.IsNullOrEmpty(t.Namespace) ? "(global)" : t.Namespace!;
            string label;

            if (_nsIndex == 0)
            {
                // Show namespace inline
                label = $"{t.Name}   <size=9><color=#888888>— {nsStr}</color></size>";
            }
            else
            {
                label = t.Name;
            }

            if (disabled)
            {
                label = $"<color=#888888>{label}  <color=#AA4444>(already added)</color></color>";
            }
            else
            {
                label = $"<b>{label}</b>";
            }

            return new GUIContent(label, t.FullName);
        }

        protected override void OnItemPicked(Type item)
        {
            _onPick?.Invoke(item);
        }

        // --- Toolbar override (title + namespace dropdown + search) ---------

        protected override void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField(_title, EditorStyles.boldLabel, GUILayout.Width(140));

                // Namespace dropdown
                if (_nsOptions.Count > 0)
                {
                    using (new EditorGUI.DisabledScope(_nsOptions.Count <= 1))
                    {
                        var oldIdx = _nsIndex;
                        _nsIndex = EditorGUILayout.Popup(
                            _nsIndex,
                            _nsOptions.ToArray(),
                            EditorStyles.toolbarPopup,
                            GUILayout.Width(160));

                        if (_nsIndex != oldIdx)
                        {
                            _hoverIndex = -1;
                            Repaint();
                        }
                    }
                }

                GUILayout.Space(6f);

                // Search field
                GUI.SetNextControlName("ZenPickerSearch");
                var newFilter = GUILayout.TextField(
                    _search,
                    _searchStyle,
                    GUILayout.MinWidth(120),
                    GUILayout.MaxWidth(240));

                if (!string.Equals(newFilter, _search, StringComparison.Ordinal))
                {
                    _search = newFilter;
                    _hoverIndex = -1;
                    Repaint();
                }

                if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(22)))
                {
                    _search = "";
                    GUI.FocusControl("ZenPickerSearch");
                    _hoverIndex = -1;
                    Repaint();
                }
            }
        }

        // --- Helpers --------------------------------------------------------

        private static Rect GUIToScreenRect(Rect guiRect)
        {
            var tl = GUIUtility.GUIToScreenPoint(new Vector2(guiRect.xMin, guiRect.yMin));
            return new Rect(tl.x, tl.y, guiRect.width, guiRect.height);
        }

        private static Rect GetEditorScreenRect()
        {
            return EditorGUIUtility.GetMainWindowPosition();
        }

        /// <summary>
        /// Finds all types marked with ZenComponentAttribute in loaded assemblies.
        /// </summary>
        public static IEnumerable<Type> FindAllZenComponents()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] ts;
                try
                {
                    ts = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    ts = ex.Types.Where(x => x != null).ToArray()!;
                }

                foreach (var t in ts)
                {
                    if (t == null || t.IsAbstract || t.IsGenericType)
                        continue;

                    if (!t.IsClass && !t.IsValueType)
                        continue;

                    if (t.GetCustomAttribute<ZenComponentAttribute>() == null)
                        continue;

                    yield return t;
                }
            }
        }

        // Unity entry point (※ not override!)
        private void OnGUI()
        {
            // Keep fixed width in utility mode
            if (_openMode == PickerOpenMode.UtilityFixedWidth &&
                !Mathf.Approximately(position.width, WINDOW_WIDTH))
            {
                position = new Rect(position.x, position.y, WINDOW_WIDTH, position.height);
            }

            DrawDefaultGUI();
        }
    }
}
#endif
