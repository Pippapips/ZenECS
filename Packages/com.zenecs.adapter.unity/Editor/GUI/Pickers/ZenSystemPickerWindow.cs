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
    /// Dropdown picker window for system types.
    /// </summary>
    internal sealed class ZenSystemPickerWindow : ZenPickerWindowBase<Type>
    {
        private readonly List<Type> _all = new();
        private HashSet<Type> _disabled = new();
        private Action<Type>? _onPick;
        private Action? _onCancelLocal;

        public static void Show(
            IEnumerable<Type> allSystemTypes,
            HashSet<Type> disabled,
            Action<Type> onPick,
            Rect activatorRectGui,
            string title = "Add System",
            Action? onCancel = null)
        {
            var win = CreateInstance<ZenSystemPickerWindow>();
            win._title = title;
            win.titleContent = new GUIContent(title);
            win._onPick = onPick;
            win._onCancel = onCancel;
            win._onCancelLocal = onCancel;
            win._closeOnLostFocus = true;

            win._all.Clear();
            win._all.AddRange(
                allSystemTypes
                    .Where(t => t != null && !t.IsAbstract)
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
            return "No matching systems found.\n" +
                   "Check search text or implement ISystem types.";
        }

        protected override GUIContent GetItemContent(Type t, bool disabled)
        {
            var ns = t.Namespace ?? "Global";
            var typeName = t.Name;
            var fullName = t.FullName ?? typeName;

            string secondary = ns;
            const int maxLen = 70;
            if (fullName.Length > maxLen)
                secondary = "…" + fullName.Substring(fullName.Length - maxLen);

            string displayText = disabled
                ? $"<color=#888888>{typeName}</color>   <size=9><color=#999999>{secondary}</color></size>"
                : $"<b>{typeName}</b>   <size=9><color=#888888>{secondary}</color></size>";

            return new GUIContent(displayText, fullName);
        }

        protected override void OnItemPicked(Type item)
        {
            _onPick?.Invoke(item);
        }

        private void OnGUI() => DrawDefaultGUI();
    }
}
#endif
