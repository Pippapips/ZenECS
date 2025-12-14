// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenBlueprintPickerWindow.cs
// Purpose: Searchable picker window for selecting EntityBlueprint ScriptableObject
//          assets in ZenECS editor tooling.
// Key concepts:
//   • Blueprint asset selection: filters and displays available EntityBlueprint assets.
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
using ZenECS.Adapter.Unity.Blueprints;
using ZenECS.Adapter.Unity.Editor.Common;

namespace ZenECS.Adapter.Unity.Editor.GUIs
{
    /// <summary>
    /// Dropdown picker window for EntityBlueprint ScriptableObjects.
    /// </summary>
    internal sealed class ZenBlueprintPickerWindow : ZenPickerWindowBase<EntityBlueprint>
    {
        private readonly List<EntityBlueprint> _all = new();
        private Action<EntityBlueprint> _onPick = _ => { };

        public static void Show(
            Rect activatorRectGui,
            Action<EntityBlueprint> onPick,
            string title = "Select Entity Blueprint",
            Vector2? size = null)
        {
            var win = CreateInstance<ZenBlueprintPickerWindow>();
            win._title = title;
            win.titleContent = new GUIContent(title);
            win._onPick = onPick ?? (_ => { });
            win._closeOnLostFocus = true;

            // Load all EntityBlueprint assets in project
            win._all.Clear();
            win._all.AddRange(ZenAssetDatabase.FindAndLoadAllAssets<EntityBlueprint>());

            var w = size?.x ?? 500f;
            var h = size?.y ?? 360f;

            var topLeft = GUIUtility.GUIToScreenPoint(new Vector2(activatorRectGui.x, activatorRectGui.y));
            var bottomLeft = GUIUtility.GUIToScreenPoint(
                new Vector2(activatorRectGui.x, activatorRectGui.y + activatorRectGui.height));

            var screenRect = new Rect(
                topLeft.x,
                bottomLeft.y + 10f,
                activatorRectGui.width,
                activatorRectGui.height
            );

            win.ShowAsDropDown(screenRect, new Vector2(w, h));
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

        protected override IEnumerable<EntityBlueprint> GetSourceItems() => _all;

        protected override bool MatchesSearch(EntityBlueprint bp, string search)
        {
            if (bp == null) return false;
            var name = bp.name ?? string.Empty;
            var path = AssetDatabase.GetAssetPath(bp) ?? string.Empty;

            return (!string.IsNullOrEmpty(name) &&
                    name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                   || (!string.IsNullOrEmpty(path) &&
                       path.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        protected override IEnumerable<EntityBlueprint> OrderItems(IEnumerable<EntityBlueprint> items)
        {
            return items.Where(bp => bp != null).OrderBy(bp => bp.name);
        }

        protected override string GetEmptyMessage()
        {
            return "No EntityBlueprint assets found.\n" +
                   "Create an EntityBlueprint ScriptableObject to use this picker.";
        }

        protected override GUIContent GetItemContent(EntityBlueprint bp, bool disabled)
        {
            var name = bp.name;
            var path = AssetDatabase.GetAssetPath(bp);

            const int maxLen = 60;
            string shortPath = path;
            if (!string.IsNullOrEmpty(path) && path.Length > maxLen)
            {
                shortPath = "…" + path.Substring(path.Length - maxLen);
            }

            var displayText = $"{name}   <size=9><color=#888888>{shortPath}</color></size>";
            return new GUIContent(displayText, path);
        }

        protected override void OnItemPicked(EntityBlueprint item)
        {
            _onPick(item);
        }

        private void OnGUI() => DrawDefaultGUI();
    }
}
#endif
