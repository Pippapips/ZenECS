// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: SystemTypeRefDrawer.cs
// Purpose: Custom PropertyDrawer for SystemTypeRef that provides a MonoScript
//          picker UI with type filtering and validation.
// Key concepts:
//   • MonoScript picker: ObjectField for selecting script assets.
//   • Type validation: checks against SystemTypeFilterAttribute constraints.
//   • Type-to-script cache: reverse lookup from Type to MonoScript.
//   • Editor-only: compiled out in player builds via #if UNITY_EDITOR.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Adapter.Unity.SystemPresets;

namespace ZenECS.Adapter.Unity.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(SystemTypeRef))]
    public class SystemTypeRefDrawer : PropertyDrawer
    {
        // Type→MonoScript reverse lookup cache (persists during editor session)
        private static readonly Dictionary<Type, MonoScript> _typeToScript = new();
        private static int _reentryGuard; // Rare reentry guard

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var aqnProp = property.FindPropertyRelative("_assemblyQualifiedName");
            var aqn = aqnProp.stringValue;
            var currentType = string.IsNullOrEmpty(aqn) ? null : Type.GetType(aqn, false);

            // Read filter
            var filter = attribute as SystemTypeFilterAttribute;
            var baseType = filter?.BaseType;
            var allowAbstract = filter?.AllowAbstract ?? false;

            EditorGUI.BeginProperty(position, label, property);
            var fieldRect = EditorGUI.PrefixLabel(position, label);

            // Reverse lookup current type to MonoScript (may not exist)
            var currentScript = ResolveMonoScript(currentType);

            EditorGUI.BeginChangeCheck();
            var picked = EditorGUI.ObjectField(fieldRect, currentScript, typeof(MonoScript), false) as MonoScript;
            if (EditorGUI.EndChangeCheck())
            {
                if (picked == null)
                {
                    aqnProp.stringValue = string.Empty;
                }
                else
                {
                    var t = picked.GetClass();
                    if (!IsValidType(t, baseType, allowAbstract, out var msg))
                    {
                        EditorUtility.DisplayDialog("Type Selection Error", msg, "OK");
                        // Keep existing value
                    }
                    else
                    {
                        aqnProp.stringValue = t!.AssemblyQualifiedName;
                        Cache(t, picked);
                    }
                }
            }

            // Display info below
            var infoRect = new Rect(position.x, position.yMax + 2, position.width, EditorGUIUtility.singleLineHeight);
            if (currentType != null)
            {
                var ok = IsValidType(currentType, baseType, allowAbstract, out _);
                var style = ok ? EditorStyles.miniLabel : EditorStyles.miniBoldLabel;
                EditorGUI.LabelField(infoRect, currentType.FullName, style);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight * 1.6f;

        private static bool IsValidType(Type? t, Type? baseType, bool allowAbstract, out string message)
        {
            if (t == null)
            {
                message = "Not a valid type.";
                return false;
            }

            if (!allowAbstract && t.IsAbstract)
            {
                message = "Abstract types cannot be selected.";
                return false;
            }

            if (baseType != null && !baseType.IsAssignableFrom(t))
            {
                message = $"Selected type does not implement/inherit '{baseType.Name}'.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private static void Cache(Type t, MonoScript ms)
        {
            if (t != null && ms != null) _typeToScript[t] = ms;
        }

        private static MonoScript? ResolveMonoScript(Type? t)
        {
            if (t == null) return null;
            if (_typeToScript.TryGetValue(t, out var cached) && cached != null) return cached;

            // Rare reentry guard
            if (_reentryGuard > 0) return null;
            _reentryGuard++;

            try
            {
                // Use ZenAssetDatabase but maintain cache
                var ms = ZenAssetDatabase.FindMonoScriptByType(t);
                if (ms != null)
                {
                    _typeToScript[t] = ms;
                    return ms;
                }
            }
            finally
            {
                _reentryGuard--;
            }

            return null;
        }
    }
}
#endif
