// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenPickerWindowBase.cs
// Purpose: Common base class for searchable dropdown picker windows used in
//          ZenECS editor tooling for selecting types, assets, and other items.
// Key concepts:
//   • Searchable list: toolbar with search field and scrollable item list.
//   • Keyboard navigation: Up/Down/Enter/Escape key support.
//   • Hover highlighting: visual feedback for mouse interaction.
//   • Derived classes: provide source items, matching logic, and pick callbacks.
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
using UnityEngine;

namespace ZenECS.Adapter.Unity.Editor.GUIs
{
    /// <summary>
    /// Common base for searchable dropdown picker windows.
    /// Handles:
    /// - Toolbar (title + search)
    /// - Scrollable list with hover highlight
    /// - Keyboard navigation (Up/Down/Enter/Escape)
    /// - Optional status bar with GUI.tooltip
    /// Derived classes provide:
    /// - Source items
    /// - Search matching, sort order, disabled logic
    /// - Row content + pick callback
    /// </summary>
    internal abstract class ZenPickerWindowBase<TItem> : EditorWindow
    {
        protected const float ROW_HEIGHT = 22f;
        protected const float PADDING = 6f;

        protected string _title = "";
        protected string _search = "";
        protected Vector2 _scroll;
        protected int _hoverIndex = -1;

        protected GUIStyle? _rowStyle;
        protected GUIStyle? _searchStyle;
        protected GUIStyle? _statusStyle;

        /// <summary>Show status bar with GUI.tooltip text.</summary>
        protected bool _useStatusBar = true;

        /// <summary>Close picker when focus is lost (dropdown-like behavior).</summary>
        protected bool _closeOnLostFocus = true;

        /// <summary>Invoked when window is closed without picking.</summary>
        protected Action? _onCancel;

        private bool _picked;

        protected virtual void OnEnable()
        {
            var baseLabel = EditorStyles.label;
            _rowStyle = new GUIStyle(baseLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = ROW_HEIGHT,
                richText = true,
                padding = new RectOffset(4, 4, 0, 0),
                clipping = TextClipping.Clip
            };

            _searchStyle = new GUIStyle(EditorStyles.toolbarSearchField);

            _statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                clipping = TextClipping.Clip
            };

            wantsMouseMove = true;
        }

        protected virtual void OnLostFocus()
        {
            if (_closeOnLostFocus)
            {
                Close();
            }
        }

        protected virtual void OnDestroy()
        {
            if (!_picked)
            {
                _onCancel?.Invoke();
            }
        }

        /// <summary>
        /// Call this from derived classes' OnGUI().
        /// </summary>
        protected void DrawDefaultGUI()
        {
            titleContent = new GUIContent(string.IsNullOrEmpty(_title) ? GetType().Name : _title);

            DrawToolbar();
            EditorGUILayout.Space(2);

            var list = BuildFilteredList();

            if (list.Count == 0)
            {
                DrawEmptyState();
                return;
            }

            DrawList(list);
            HandleKeyboard(list);

            if (_useStatusBar)
            {
                DrawStatusBar();
            }
        }

        // --------------------------------------------------------------------
        // Toolbar
        // --------------------------------------------------------------------

        protected virtual void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (!string.IsNullOrEmpty(_title))
                {
                    EditorGUILayout.LabelField(_title, EditorStyles.boldLabel);
                }
                else
                {
                    EditorGUILayout.LabelField(GetType().Name, EditorStyles.boldLabel);
                }

                GUILayout.FlexibleSpace();

                GUI.SetNextControlName("ZenPickerSearch");
                var newSearch = GUILayout.TextField(_search, _searchStyle, GUILayout.ExpandWidth(true));
                if (!string.Equals(newSearch, _search, StringComparison.Ordinal))
                {
                    _search = newSearch;
                    _hoverIndex = -1;
                    Repaint();
                }

                if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(24)))
                {
                    _search = "";
                    GUI.FocusControl("ZenPickerSearch");
                    _hoverIndex = -1;
                    Repaint();
                }
            }
        }

        // --------------------------------------------------------------------
        // List & filtering
        // --------------------------------------------------------------------

        protected List<TItem> BuildFilteredList()
        {
            var src = GetSourceItems() ?? Enumerable.Empty<TItem>();
            IEnumerable<TItem> q = src;

            var s = _search;
            if (!string.IsNullOrWhiteSpace(s))
            {
                s = s.Trim();
                q = q.Where(item => MatchesSearch(item, s));
            }

            return OrderItems(q).ToList();
        }

        protected virtual void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                var style = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                EditorGUILayout.LabelField(GetEmptyMessage(), style, GUILayout.MaxWidth(420));
                GUILayout.FlexibleSpace();
            }

            GUILayout.FlexibleSpace();
        }

        protected virtual void DrawList(IReadOnlyList<TItem> list)
        {
            var viewRect = GUILayoutUtility.GetRect(
                0, 100000,
                0, 100000,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            var contentRect = new Rect(
                0, 0,
                viewRect.width - 16,
                list.Count * ROW_HEIGHT + PADDING * 2);

            _scroll = GUI.BeginScrollView(viewRect, _scroll, contentRect);

            float y = PADDING;
            _hoverIndex = Mathf.Clamp(_hoverIndex, -1, list.Count - 1);

            var evt = Event.current;

            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                bool disabled = IsDisabled(item);

                var rowRect = new Rect(
                    PADDING,
                    y,
                    contentRect.width - PADDING * 2,
                    ROW_HEIGHT);

                if (rowRect.Contains(evt.mousePosition))
                {
                    _hoverIndex = i;
                    if (evt.type == EventType.MouseMove)
                        Repaint();
                }

                if (_hoverIndex == i)
                {
                    var bg = EditorGUIUtility.isProSkin
                        ? new Color(1f, 1f, 1f, 0.06f)
                        : new Color(0f, 0f, 0f, 0.06f);
                    EditorGUI.DrawRect(rowRect, bg);
                }

                var content = GetItemContent(item, disabled);

                if (disabled)
                {
                    GUI.Label(rowRect, content, _rowStyle);
                }
                else
                {
                    if (GUI.Button(rowRect, content, _rowStyle))
                    {
                        PickAndClose(item);
                    }
                }

                y += ROW_HEIGHT;
            }

            GUI.EndScrollView();
        }

        // --------------------------------------------------------------------
        // Keyboard + status bar
        // --------------------------------------------------------------------

        protected virtual void HandleKeyboard(IReadOnlyList<TItem> list)
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;

            switch (e.keyCode)
            {
                case KeyCode.UpArrow:
                    if (list.Count == 0)
                        return;
                    _hoverIndex = Mathf.Clamp(
                        (_hoverIndex < 0 ? list.Count : _hoverIndex) - 1,
                        0,
                        Mathf.Max(0, list.Count - 1));
                    e.Use();
                    Repaint();
                    break;

                case KeyCode.DownArrow:
                    if (list.Count == 0)
                        return;
                    _hoverIndex = Mathf.Clamp(
                        _hoverIndex + 1,
                        0,
                        Mathf.Max(0, list.Count - 1));
                    e.Use();
                    Repaint();
                    break;

                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (_hoverIndex >= 0 && _hoverIndex < list.Count)
                    {
                        var item = list[_hoverIndex];
                        if (!IsDisabled(item))
                        {
                            PickAndClose(item);
                        }
                    }
                    e.Use();
                    break;

                case KeyCode.Escape:
                    Close();
                    e.Use();
                    break;
            }
        }

        protected virtual void DrawStatusBar()
        {
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                var tip = GUI.tooltip ?? string.Empty;
                if (string.IsNullOrEmpty(tip))
                    return;

                var gc = new GUIContent(tip);
                var rect = GUILayoutUtility.GetRect(gc, _statusStyle, GUILayout.ExpandWidth(true));
                EditorGUI.LabelField(rect, gc, _statusStyle);
            }
        }

        protected void PickAndClose(TItem item)
        {
            _picked = true;
            OnItemPicked(item);
            Close();
            GUIUtility.ExitGUI();
        }

        // --------------------------------------------------------------------
        // Hooks for derived classes
        // --------------------------------------------------------------------

        /// <summary>Return full source items (before search).</summary>
        protected abstract IEnumerable<TItem> GetSourceItems();

        /// <summary>Search match. Default: ToString contains search (case-insensitive).</summary>
        protected virtual bool MatchesSearch(TItem item, string search)
        {
            if (item == null) return false;
            var text = item.ToString() ?? string.Empty;
            return text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Sort order after filtering.</summary>
        protected virtual IEnumerable<TItem> OrderItems(IEnumerable<TItem> items) => items;

        /// <summary>Return true if item should be disabled in UI.</summary>
        protected virtual bool IsDisabled(TItem item) => false;

        /// <summary>Message for empty state center label.</summary>
        protected virtual string GetEmptyMessage() => "No items.";

        /// <summary>Provide row content (label + tooltip) for given item.</summary>
        protected abstract GUIContent GetItemContent(TItem item, bool disabled);

        /// <summary>Called when user picks an item (via click or Enter).</summary>
        protected abstract void OnItemPicked(TItem item);
    }
}
#endif
