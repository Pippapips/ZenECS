// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenReadOnlyInInspector.cs
// Purpose: Custom PropertyDrawer for ZenReadOnlyInInspectorAttribute that
//          renders fields as read-only in Unity inspectors.
// Key concepts:
//   • Read-only rendering: displays value but prevents editing.
//   • UIElements-based: uses PropertyField with enabled=false.
//   • Editor-only: compiled out in player builds.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using ZenECS.Adapter.Unity;

namespace ZenECS.Adapter.Unity.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(ZenReadOnlyInInspectorAttribute), useForChildren: true)]
    public sealed class ZenReadOnlyInInspector : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new PropertyField(property);
            field.SetEnabled(false);
            return field;
        }
    }
}
