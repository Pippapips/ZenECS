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

            win.ShowAsDropDown(anchorRect, new Vector2(width, height));
            win.Focus();
        }

        // --- Base hooks -----------------------------------------------------

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
            var ns = t.Namespace ?? "Global";
            var typeName = t.Name;
            var fullName = t.FullName ?? typeName;

            const int maxLen = 70;
            string secondary = fullName.Length > maxLen
                ? "…" + fullName.Substring(fullName.Length - maxLen)
                : ns;

            string displayText = disabled
                ? $"<color=#888888>{typeName}</color>   <size=9><color=#999999>{secondary}</color></size>"
                : $"<b>{typeName}</b>   <size=9><color=#888888>{secondary}</color></size>";

            return new GUIContent(displayText, fullName);
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
