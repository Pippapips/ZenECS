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
    /// Dropdown picker window for context types.
    /// </summary>
    internal sealed class ZenContextPickerWindow : ZenPickerWindowBase<Type>
    {
        private readonly List<Type> _all = new();
        private HashSet<Type> _disabled = new();
        private Action<Type> _onPick = _ => { };

        public static void Show(
            IEnumerable<Type> allContextTypes,
            HashSet<Type> disabled,
            Action<Type> onPick,
            Rect activatorRectGui,
            string title = "Add Context",
            Vector2? size = null)
        {
            var win = CreateInstance<ZenContextPickerWindow>();
            win._title = title;
            win.titleContent = new GUIContent(title);
            win._onPick = onPick ?? (_ => { });
            win._closeOnLostFocus = true;

            win._all.Clear();
            win._all.AddRange(allContextTypes ?? Array.Empty<Type>());
            win._disabled = disabled ?? new HashSet<Type>();

            var w = size?.x ?? 420f;
            var h = size?.y ?? 360f;

            var screenPos = GUIUtility.GUIToScreenPoint(
                new Vector2(activatorRectGui.x, activatorRectGui.y));
            var screenRect = new Rect(
                screenPos.x,
                screenPos.y,
                activatorRectGui.width,
                activatorRectGui.height);

            win.ShowAsDropDown(screenRect, new Vector2(w, h));
            win.Focus();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            // Simplified row style similar to PR Label
            _rowStyle = new GUIStyle("PR Label")
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = ROW_HEIGHT
            };
        }

        protected override IEnumerable<Type> GetSourceItems() => _all;

        protected override bool IsDisabled(Type item) => _disabled.Contains(item);

        protected override bool MatchesSearch(Type t, string search)
        {
            if (t == null) return false;
            var label = t.FullName ?? t.Name ?? string.Empty;
            return label.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        protected override IEnumerable<Type> OrderItems(IEnumerable<Type> items)
        {
            return items.Where(t => t != null).OrderBy(t => t.FullName);
        }

        protected override string GetEmptyMessage() => "No matching contexts found.";

        protected override GUIContent GetItemContent(Type t, bool disabled)
        {
            var label = t.FullName ?? t.Name ?? string.Empty;
            if (disabled)
            {
                label = $"<color=#888888>{label}  (already added)</color>";
            }

            return new GUIContent(label, t.FullName);
        }

        protected override void OnItemPicked(Type item)
        {
            _onPick(item);
        }

        private void OnGUI() => DrawDefaultGUI();
    }
}
#endif
