// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenSingletonPickerWindow.cs
// Purpose: Searchable picker window for selecting IWorldSingletonComponent
//          struct types in ZenECS editor tooling.
// Key concepts:
//   • Singleton type selection: filters and displays available singleton struct types.
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
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Editor.GUIs
{
    /// <summary>
    /// Picker window for IWorldSingletonComponent structs.
    /// </summary>
    internal sealed class ZenSingletonPickerWindow : ZenPickerWindowBase<Type>
    {
        private readonly List<Type> _all = new();
        private HashSet<Type> _disabled = new();
        private Action<Type>? _onPick;

        public static void Show(
            IEnumerable<Type> allSingletonTypes,
            HashSet<Type> disabled,
            Action<Type> onPick,
            Rect activatorRectGui,
            string title = "Add Singleton",
            Action? onCancel = null)
        {
            var win = CreateInstance<ZenSingletonPickerWindow>();
            win._title = title;
            win.titleContent = new GUIContent(title);
            win._onPick = onPick;
            win._onCancel = onCancel;
            win._closeOnLostFocus = true;

            win._all.Clear();
            win._all.AddRange(
                allSingletonTypes
                    .Where(t =>
                        t != null &&
                        !t.IsAbstract &&
                        t.IsValueType &&
                        typeof(IWorldSingletonComponent).IsAssignableFrom(t))
                    .Distinct()
                    .OrderBy(t => t.FullName));

            win._disabled = disabled ?? new HashSet<Type>();

            var width = 520f;
            var height = 420f;

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
            var full = t.FullName ?? string.Empty;

            search = search.ToLowerInvariant();
            return name.ToLowerInvariant().Contains(search) ||
                   full.ToLowerInvariant().Contains(search);
        }

        protected override IEnumerable<Type> OrderItems(IEnumerable<Type> items)
        {
            return items.Where(t => t != null).OrderBy(t => t.FullName);
        }

        protected override string GetEmptyMessage()
        {
            return "No matching singleton components found.\n" +
                   "Check search text or implement IWorldSingletonComponent structs.";
        }

        protected override GUIContent GetItemContent(Type t, bool disabled)
        {
            var label = t.FullName ?? t.Name ?? string.Empty;
            if (disabled)
            {
                label = $"<color=#888888>{label}  <color=#AA4444>(already added)</color></color>";
            }

            return new GUIContent(label, t.FullName);
        }

        protected override void OnItemPicked(Type item)
        {
            _onPick?.Invoke(item);
        }

        private void OnGUI() => DrawDefaultGUI();
    }
}
#endif
