// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenSystemPresetPickerWindow.cs
// Purpose: Searchable picker window for selecting SystemsPreset ScriptableObject
//          assets in ZenECS editor tooling.
// Key concepts:
//   • Preset asset selection: filters and displays available SystemsPreset assets.
//   • Searchable: toolbar with search field for filtering assets.
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
using ZenECS.Adapter.Unity.Editor.Common;

namespace ZenECS.Adapter.Unity.Editor.GUIs
{
    /// <summary>
    /// Dropdown picker window for SystemsPreset ScriptableObjects.
    /// </summary>
    internal sealed class ZenSystemPresetPickerWindow : ZenPickerWindowBase<ScriptableObject>
    {
        private readonly List<ScriptableObject> _all = new();
        private Action<ScriptableObject>? _onPick;

        public static void Show(
            Rect activatorRectGui,
            Action<ScriptableObject> onPick,
            string title = "Add System Preset")
        {
            var w = CreateInstance<ZenSystemPresetPickerWindow>();
            w._title = title;
            w._onPick = onPick;
            w._closeOnLostFocus = true;

            w._all.Clear();
            w._all.AddRange(LoadAllPresets());

            var width = 520f;
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

            w.ShowAsDropDown(anchorRect, new Vector2(width, height));
            w.titleContent = new GUIContent(title);
            w.Focus();
        }

        /// <summary>
        /// Finds all ScriptableObject assets of type "SystemsPreset".
        /// (Matches by type name string, so it doesn't require a hard reference to the C# type.)
        /// </summary>
        private static List<ScriptableObject> LoadAllPresets()
        {
            return ZenAssetDatabase.FindAndLoadAllAssets<ScriptableObject>("t:SystemsPreset");
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

        protected override IEnumerable<ScriptableObject> GetSourceItems() => _all;

        protected override bool MatchesSearch(ScriptableObject asset, string search)
        {
            if (asset == null) return false;

            var name = asset.name ?? string.Empty;
            var path = AssetDatabase.GetAssetPath(asset) ?? string.Empty;

            return name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0
                   || path.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        protected override IEnumerable<ScriptableObject> OrderItems(IEnumerable<ScriptableObject> items)
        {
            return items.Where(a => a != null).OrderBy(a => a.name);
        }

        protected override string GetEmptyMessage()
        {
            return "No SystemsPreset assets found.\n" +
                   "Create a SystemsPreset ScriptableObject first.";
        }

        protected override GUIContent GetItemContent(ScriptableObject asset, bool disabled)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            var displayText =
                $"<b>{asset.name}</b>   <size=9><color=#888888>[{path}]</color></size>";
            return new GUIContent(displayText, path);
        }

        protected override void OnItemPicked(ScriptableObject item)
        {
            _onPick?.Invoke(item);
        }

        private void OnGUI() => DrawDefaultGUI();
    }
}
#endif
