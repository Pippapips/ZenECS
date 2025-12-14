// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenContextAssetPickerWindow.cs
// Purpose: Searchable picker window for selecting ContextAsset ScriptableObject
//          assets (SharedContextAsset, PerEntityContextAsset) in ZenECS editor tooling.
// Key concepts:
//   • Context asset selection: filters and displays available ContextAsset assets.
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
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Core;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Editor.GUIs
{
    /// <summary>
    /// Dropdown picker window for selecting ContextAsset ScriptableObjects.
    /// Uses ZenPickerWindowBase to share common UI logic.
    /// </summary>
    internal sealed class ZenContextAssetPickerWindow : ZenPickerWindowBase<ContextAsset>
    {
        private readonly List<ContextAsset> _all = new();
        private HashSet<Type> _disabledSet = new();
        private Action<ContextAsset>? _onPick;

        /// <summary>
        /// Shows the context asset picker as a dropdown near the given activator rect.
        /// </summary>
        public static void Show(
            Rect activatorRectGui,
            Action<ContextAsset> onPick,
            IReadOnlyCollection<Type>? disabledContextTypes,
            string title = "Add Context",
            Vector2? size = null)
        {
            var win = CreateInstance<ZenContextAssetPickerWindow>();
            win._title = title;
            win.titleContent = new GUIContent(title);
            win._onPick = onPick ?? (_ => { });
            win._closeOnLostFocus = true;

            // Load all ContextAsset assets in project
            win._all.Clear();
            win._all.AddRange(ZenAssetDatabase.FindAndLoadAllAssets<ContextAsset>());

            win._disabledSet = disabledContextTypes != null
                ? new HashSet<Type>(disabledContextTypes)
                : new HashSet<Type>();

            var w = size?.x ?? 780f; // 520 * 1.5 = 780
            var h = size?.y ?? 400f;

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

            win.ShowAsDropDown(anchorRect, new Vector2(w, h));
            win.Focus();
        }

        // --- Base hooks -----------------------------------------------------

        protected override IEnumerable<ContextAsset> GetSourceItems() => _all;

        protected override bool IsDisabled(ContextAsset asset)
        {
            if (asset == null) return false;
            var ctxType = TryResolveContextType(asset);
            if (ctxType == null) return false;

            return _disabledSet.Any(t =>
                t == ctxType ||
                t.IsSubclassOf(ctxType) ||
                ctxType.IsSubclassOf(t));
        }

        protected override bool MatchesSearch(ContextAsset asset, string search)
        {
            if (asset == null) return false;
            var name = asset.name ?? string.Empty;
            var path = AssetDatabase.GetAssetPath(asset) ?? string.Empty;

            return (!string.IsNullOrEmpty(name) &&
                    name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                   || (!string.IsNullOrEmpty(path) &&
                       path.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        protected override IEnumerable<ContextAsset> OrderItems(IEnumerable<ContextAsset> items)
        {
            return items.Where(a => a != null).OrderBy(a => a.name);
        }

        protected override string GetEmptyMessage()
        {
            return "No ContextAsset found.\n" +
                   "Create a ContextAsset ScriptableObject to use this picker.";
        }

        protected override GUIContent GetItemContent(ContextAsset asset, bool disabled)
        {
            if (asset == null)
                return new GUIContent("(null)");

            var name = asset.name;
            var ctxType = TryResolveContextType(asset);
            string nsStr = ctxType != null && !string.IsNullOrEmpty(ctxType.Namespace)
                ? ctxType.Namespace
                : "(global)";
            
            string label;
            // ComponentPicker/BinderPicker style: name — namespace format
            label = $"{name}   <size=9><color=#888888>— {nsStr}</color></size>";

            if (disabled)
            {
                label = $"<color=#888888>{label}  <color=#AA4444>(already added)</color></color>";
            }
            else
            {
                label = $"<b>{label}</b>";
            }

            var path = AssetDatabase.GetAssetPath(asset);
            return new GUIContent(label, path);
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

        protected override void OnItemPicked(ContextAsset item)
        {
            _onPick?.Invoke(item);
        }

        // Unity entry point
        private void OnGUI() => DrawDefaultGUI();

        // Infer IContext type that ContextAsset creates
        private static Type? TryResolveContextType(ContextAsset asset)
        {
            if (asset == null) return null;
            var aType = asset.GetType();

            // 1) ContextType property convention
            var prop = aType.GetProperty("ContextType",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (prop != null && typeof(Type).IsAssignableFrom(prop.PropertyType))
            {
                var v = prop.GetValue(prop.GetGetMethod(true)?.IsStatic == true ? null : asset) as Type;
                if (v != null && typeof(IContext).IsAssignableFrom(v))
                    return v;
            }

            // 2) GetContextType() method convention
            var mGet = aType.GetMethod("GetContextType",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
                null, Type.EmptyTypes, null);
            if (mGet != null && typeof(Type).IsAssignableFrom(mGet.ReturnType))
            {
                var v = mGet.Invoke(mGet.IsStatic ? null : asset, Array.Empty<object>()) as Type;
                if (v != null && typeof(IContext).IsAssignableFrom(v))
                    return v;
            }

            // 3) Infer from Create/Build/Instantiate/Make/ToInstance return type
            var names = new[] { "Create", "Build", "Instantiate", "Make", "ToInstance" };
            foreach (var name in names)
            {
                var methods = aType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(mi => mi.Name == name);
                foreach (var mi in methods)
                {
                    var rt = mi.ReturnType;
                    if (typeof(IContext).IsAssignableFrom(rt))
                        return rt;
                }
            }

            return null;
        }
    }
}
#endif
