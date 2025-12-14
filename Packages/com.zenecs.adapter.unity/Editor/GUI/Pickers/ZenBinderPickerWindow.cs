// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenBinderPickerWindow.cs
// Purpose: Searchable picker window for selecting IBinder implementation types
//          in ZenECS editor tooling.
// Key concepts:
//   • Binder type selection: filters and displays available IBinder types.
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
using UnityEditor;
using UnityEngine;

namespace ZenECS.Adapter.Unity.Editor.GUIs
{
    /// <summary>
    /// Dropdown picker window for selecting binder types.
    /// Uses ZenPickerWindowBase to share common UI logic.
    /// </summary>
    internal sealed class ZenBinderPickerWindow : ZenPickerWindowBase<Type>
    {
        private readonly List<Type> _all = new();
        private HashSet<Type> _disabled = new();
        private Action<Type>? _onPick;

        /// <summary>
        /// Shows the binder picker as a dropdown near the given activator rect.
        /// </summary>
        public static void Show(
            IEnumerable<Type> allBinderTypes,
            HashSet<Type> disabled,
            Action<Type> onPick,
            Rect activatorRectGui,
            string title = "Add Binder",
            Action? onCancel = null)
        {
            var win = CreateInstance<ZenBinderPickerWindow>();
            win._title = title;
            win.titleContent = new GUIContent(title);
            win._onPick = onPick ?? (_ => { });
            win._onCancel = onCancel;
            win._closeOnLostFocus = true;

            // Only concrete types with public parameterless ctor
            win._all.Clear();
            win._all.AddRange(
                allBinderTypes
                    .Where(t =>
                        t != null &&
                        !t.IsAbstract &&
                        t.GetConstructor(Type.EmptyTypes) != null)
                    .Distinct()
                    .OrderBy(t => t.FullName));

            win._disabled = disabled ?? new HashSet<Type>();

            var width = 560f; // Same size as ComponentPicker
            var height = 400f;

            var topLeft = GUIUtility.GUIToScreenPoint(
                new Vector2(activatorRectGui.x, activatorRectGui.y));
            var bottomLeft = GUIUtility.GUIToScreenPoint(
                new Vector2(activatorRectGui.x, activatorRectGui.y + activatorRectGui.height));

            var anchorRect = new Rect(
                topLeft.x,
                bottomLeft.y + 10f,
                activatorRectGui.width,
                activatorRectGui.height
            );

            win.ShowAsDropDown(anchorRect, new Vector2(width, height));
            win.Focus();
        }

        // --- Base hooks -----------------------------------------------------

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
        }

        protected override IEnumerable<Type> GetSourceItems() => _all;

        protected override bool IsDisabled(Type item) => _disabled.Contains(item);

        protected override bool MatchesSearch(Type t, string search)
        {
            if (t == null) return false;

            var name = t.Name ?? string.Empty;
            var ns = t.Namespace ?? string.Empty;
            var full = t.FullName ?? string.Empty;

            return (!string.IsNullOrEmpty(name) &&
                    name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                   || (!string.IsNullOrEmpty(ns) &&
                       ns.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                   || (!string.IsNullOrEmpty(full) &&
                       full.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        protected override IEnumerable<Type> OrderItems(IEnumerable<Type> items)
        {
            return items.Where(t => t != null).OrderBy(t => t.FullName);
        }

        protected override string GetEmptyMessage()
        {
            return "No matching binders found.\n" +
                   "Check search text or provide concrete binder types.";
        }

        protected override GUIContent GetItemContent(Type t, bool disabled)
        {
            string nsStr = string.IsNullOrEmpty(t.Namespace) ? "(global)" : t.Namespace!;
            string label;

            // ComponentPicker style: name / namespace format
            label = $"{t.Name}   <size=9><color=#888888>— {nsStr}</color></size>";

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

        // Unity entry point
        private void OnGUI() => DrawDefaultGUI();
    }
}
#endif
