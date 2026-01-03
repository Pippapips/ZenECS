// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: WorldSystemCreatorInspector.cs
// Purpose: Custom inspector for WorldSystemCreator MonoBehaviour that provides
//          editing UI for world configuration and system registration.
// Key concepts:
//   • World configuration: name, tags, system presets, local system types.
//   • System editing: ReorderableList for system type references.
//   • Runtime info: displays resolved world and registered systems.
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
using UnityEditorInternal;
using UnityEngine;
using ZenECS.Adapter.Unity;
using ZenECS.Adapter.Unity.DI;
using ZenECS.Adapter.Unity.Editor.GUIs;
using ZenECS.Adapter.Unity.SystemPresets;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.Editor.Inspectors
{
    /// <summary>
    /// Custom inspector for <see cref="WorldSystemCreator"/>.
    /// - World Name + World Tags UX (with accent colors)
    /// - Multiple SystemsPreset assets selection via ZenSystemPresetPickerWindow
    /// - Installer-local systemTypes list via ReorderableList
    /// - At runtime, all presets + local types are merged and deduplicated
    /// </summary>
    [CustomEditor(typeof(WorldSystemCreator))]
    public sealed class WorldSystemCreatorInspector : UnityEditor.Editor
    {
        private ReorderableList? _list;
        private SerializedProperty? _propTypes;
        private SerializedProperty? _propPresets;
        private SerializedProperty? _propWorldName;
        private SerializedProperty? _propWorldTags;

        // World Tags input buffer
        private string _newTag = string.Empty;

        // ───────────────────────── Colors / Styles ─────────────────────────

        private static Color AccentColor =>
            EditorGUIUtility.isProSkin
                ? new Color(0.35f, 0.8f, 1.0f)   // dark skin: cyan-ish
                : new Color(0.1f, 0.45f, 0.8f);  // light skin: blue-ish

        private static Color TagPillBg =>
            EditorGUIUtility.isProSkin
                ? new Color(0.20f, 0.30f, 0.40f, 0.95f)
                : new Color(0.80f, 0.88f, 0.98f, 0.95f);

        private GUIStyle? _worldHeaderStyle;
        private GUIStyle? _sectionHeaderStyle;

        private void EnsureStyles()
        {
            if (_worldHeaderStyle == null)
            {
                _worldHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = AccentColor },
                    fontSize = 13
                };
            }

            if (_sectionHeaderStyle == null)
            {
                _sectionHeaderStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    normal = { textColor = AccentColor }
                };
            }
        }

        private void OnEnable()
        {
            _propTypes      = serializedObject.FindProperty("systemTypes");
            _propPresets    = serializedObject.FindProperty("systemPresets");
            _propWorldName  = serializedObject.FindProperty("worldName");
            _propWorldTags  = serializedObject.FindProperty("worldTags");

            EnsureStyles();

            // ReorderableList for installer-local systemTypes
            _list = new ReorderableList(
                serializedObject,
                _propTypes,
                draggable: true,
                displayHeader: true,
                displayAddButton: true,
                displayRemoveButton: true
            );

            _list.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "System Types (Installer-local)", _sectionHeaderStyle);

            _list.elementHeight = EditorGUIUtility.singleLineHeight + 15;

            _list.drawElementCallback = (rect, index, active, focused) =>
            {
                var elem  = _propTypes!.GetArrayElementAtIndex(index);
                var aqn   = elem.FindPropertyRelative("_assemblyQualifiedName").stringValue;
                var type  = string.IsNullOrWhiteSpace(aqn) ? null : Type.GetType(aqn, throwOnError: false);

                var lineRect = new Rect(
                    rect.x + 6,
                    rect.y + 3,
                    rect.width - 12,
                    EditorGUIUtility.singleLineHeight + 10
                );

                if (type != null)
                {
                    var ns   = type.Namespace ?? "Global";
                    var name = type.Name;
                    var style = new GUIStyle(EditorStyles.label) { richText = true };
                    style.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
                    var text  = $"<b>{name}</b>\n<color=#888888><size=10>({ns})</size></color>";
                    EditorGUI.LabelField(lineRect, new GUIContent(text, type.AssemblyQualifiedName), style);
                }
                else
                {
                    var style = new GUIStyle(EditorStyles.label)
                    {
                        normal = { textColor = new Color(1f, 0.35f, 0.35f) }
                    };
                    EditorGUI.LabelField(lineRect, "None (Click + to open picker)", style);
                }
            };

            // Use onAddDropdownCallback so we get the [+] button rect
            _list.onAddDropdownCallback = (buttonRect, list) =>
            {
                serializedObject.Update();
                var newIndex = _propTypes!.arraySize;
                _propTypes.InsertArrayElementAtIndex(newIndex);
                var elem = _propTypes.GetArrayElementAtIndex(newIndex);
                elem.FindPropertyRelative("_assemblyQualifiedName").stringValue = string.Empty;
                serializedObject.ApplyModifiedProperties();

                ShowSystemTypePickerForIndex(
                    newIndex,
                    buttonRect,
                    onCancel: () =>
                    {
                        // If nothing was picked, remove the empty slot
                        serializedObject.Update();
                        var e   = _propTypes.GetArrayElementAtIndex(newIndex);
                        var aqn = e.FindPropertyRelative("_assemblyQualifiedName").stringValue;
                        if (string.IsNullOrWhiteSpace(aqn))
                        {
                            _propTypes.DeleteArrayElementAtIndex(newIndex);
                            serializedObject.ApplyModifiedProperties();
                        }
                    });
            };

            _list.onRemoveCallback = list =>
            {
                if (_propTypes!.arraySize <= 0) return;
                _propTypes.DeleteArrayElementAtIndex(list.index);
                serializedObject.ApplyModifiedProperties();
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var icon = EditorGUIUtility.ObjectContent(target, target.GetType()).image;

            ZenEcsGUIHeader.DrawHeader(
                "World System Creator",
                "Creates or resolves a world, assigns tags, and installs systems from presets and installer-local types.",
                new[]
                {
                    "Create",
                    "World Setup",
                    "System Registration"
                }
            );
            
            EnsureStyles();
            // EditorGUILayout.LabelField("World Setup Installer", _worldHeaderStyle);
            // EditorGUILayout.Space(4);

            DrawWorldSection();

            EditorGUILayout.Space(8);

            DrawSystemsSection();

            serializedObject.ApplyModifiedProperties();

            // Clean up installer-local systems that are duplicated by any preset
            AutoRemoveLocalDuplicatesAgainstPresets();
        }

        // ───────────────────────────────── World section ─────────────────────────────────

        private void DrawWorldSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("World", _sectionHeaderStyle);
                EditorGUILayout.PropertyField(
                    _propWorldName!,
                    new GUIContent("Name", "Name passed to KernelLocator.EnsureWorld.")
                );

                EditorGUILayout.Space(4);
                DrawWorldTags();
            }
        }

        private void DrawWorldTags()
        {
            EditorGUILayout.LabelField("World Tags", _sectionHeaderStyle);

            // New tag input + add button
            using (new EditorGUILayout.HorizontalScope())
            {
                _newTag = EditorGUILayout.TextField(
                    new GUIContent(
                        "New Tag",
                        "You can enter multiple tags separated by comma, e.g. 'gameplay, client, offline'."),
                    _newTag
                );

                using (new EditorGUIUtility.IconSizeScope(new Vector2(12, 12)))
                {
                    var oldBg = GUI.backgroundColor;
                    GUI.backgroundColor = AccentColor;
                    if (GUILayout.Button("+", GUILayout.Width(28)))
                    {
                        AddTagFromInput();
                    }
                    GUI.backgroundColor = oldBg;
                }
            }

            EditorGUILayout.Space(2);

            // Quick tag buttons
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Quick:", GUILayout.Width(40));
                DrawQuickTagButton("gameplay");
                DrawQuickTagButton("editor");
                DrawQuickTagButton("client");
                DrawQuickTagButton("server");
                DrawQuickTagButton("offline");
            }

            EditorGUILayout.Space(4);

            // Existing tags as colored pills
            DrawTagPills();

            EditorGUILayout.Space(2);
            EditorGUILayout.HelpBox(
                "Tags are lower-case, kebab-case identifiers (e.g. gameplay, client, offline).\n" +
                "They are passed to EnsureWorld(name, tags) and used for filtering / locating worlds.",
                MessageType.None);
        }

        private void AddTagFromInput()
        {
            var raw = _newTag ?? string.Empty;
            raw = raw.Trim();
            if (string.IsNullOrEmpty(raw))
                return;

            // Allow multiple tags: "gameplay, client, offline"
            var tokens = raw
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t));

            bool changed = false;

            foreach (var token in tokens)
            {
                var norm = token.ToLowerInvariant().Replace(' ', '-');
                if (string.IsNullOrEmpty(norm))
                    continue;
                if (HasTag(norm))
                    continue;

                int idx = _propWorldTags!.arraySize;
                _propWorldTags.InsertArrayElementAtIndex(idx);
                _propWorldTags.GetArrayElementAtIndex(idx).stringValue = norm;
                changed = true;
            }

            if (changed)
                serializedObject.ApplyModifiedProperties();

            _newTag = string.Empty;
        }

        private void DrawQuickTagButton(string tag)
        {
            var oldBg = GUI.backgroundColor;
            GUI.backgroundColor = Color.Lerp(TagPillBg, AccentColor, 0.4f);

            if (GUILayout.Button(tag, EditorStyles.miniButton))
            {
                if (!HasTag(tag))
                {
                    int idx = _propWorldTags!.arraySize;
                    _propWorldTags.InsertArrayElementAtIndex(idx);
                    _propWorldTags.GetArrayElementAtIndex(idx).stringValue = tag;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            GUI.backgroundColor = oldBg;
        }

        private bool HasTag(string tag)
        {
            if (_propWorldTags == null) return false;
            for (int i = 0; i < _propWorldTags.arraySize; i++)
            {
                var elem = _propWorldTags.GetArrayElementAtIndex(i);
                if (elem.stringValue == tag)
                    return true;
            }
            return false;
        }

        private void DrawTagPills()
        {
            if (_propWorldTags == null || _propWorldTags.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "No tags assigned yet. Use the Quick buttons or the New Tag field above to add tags.",
                    MessageType.Info);
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                int count = _propWorldTags.arraySize;
                int i     = 0;

                while (i < count)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // Up to 4 pills per row
                        for (int col = 0; col < 4 && i < count; col++, i++)
                        {
                            var elem = _propWorldTags.GetArrayElementAtIndex(i);
                            var tag  = elem.stringValue;

                            var oldBg = GUI.backgroundColor;
                            GUI.backgroundColor = TagPillBg;

                            // One button: "tag  ✕"
                            if (GUILayout.Button($"{tag}  ✕", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                            {
                                _propWorldTags.DeleteArrayElementAtIndex(i);
                                serializedObject.ApplyModifiedProperties();
                                i--;
                                count--;
                                GUI.backgroundColor = oldBg;
                                break;
                            }

                            GUI.backgroundColor = oldBg;
                        }
                    }
                }
            }
        }

        // ───────────────────────────────── Systems section ─────────────────────────────────

        private void DrawSystemsSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("System Presets", _sectionHeaderStyle);
                DrawPresetList();

                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "All SystemsPreset assets listed above will be merged at runtime.\n" +
                    "Installer-local systemTypes are added on top. Duplicates are removed per ISystem Type.",
                    MessageType.None);
            }

            EditorGUILayout.Space(4);

            if (_list != null) _list.DoLayoutList();
        }

        private void DrawPresetList()
        {
            if (_propPresets == null)
                return;

            // Existing presets
            int count = _propPresets.arraySize;

            if (count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No System Presets assigned. Use the button below to add presets.",
                    MessageType.Info);
            }
            else
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    for (int i = 0; i < _propPresets.arraySize; i++)
                    {
                        var elem = _propPresets.GetArrayElementAtIndex(i);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.PropertyField(
                                elem,
                                GUIContent.none,
                                GUILayout.ExpandWidth(true)
                            );

                            if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(20)))
                            {
                                _propPresets.DeleteArrayElementAtIndex(i);
                                serializedObject.ApplyModifiedProperties();
                                break;
                            }
                        }
                    }
                }
            }

            EditorGUILayout.Space(2);

            // "Add Preset..." button using ZenSystemPresetPickerWindow
            var buttonContent = new GUIContent("Add Preset...", "Pick a SystemsPreset asset to add.");
            var buttonRect = GUILayoutUtility.GetRect(buttonContent, EditorStyles.miniButton);
            if (GUI.Button(buttonRect, buttonContent, EditorStyles.miniButton))
            {
                ZenSystemPresetPickerWindow.Show(
                    buttonRect,
                    onPick: so =>
                    {
                        var preset = so as SystemsPreset;
                        if (preset == null)
                            return;

                        if (HasPreset(preset))
                            return;

                        serializedObject.Update();
                        int idx = _propPresets.arraySize;
                        _propPresets.InsertArrayElementAtIndex(idx);
                        _propPresets.GetArrayElementAtIndex(idx).objectReferenceValue = preset;
                        serializedObject.ApplyModifiedProperties();
                    },
                    title: "Add System Preset");
            }
        }

        private bool HasPreset(SystemsPreset preset)
        {
            if (_propPresets == null) return false;

            for (int i = 0; i < _propPresets.arraySize; i++)
            {
                var elem   = _propPresets.GetArrayElementAtIndex(i);
                var exists = elem.objectReferenceValue as SystemsPreset;
                if (exists == preset)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Remove installer-local systems that also exist in ANY of the System Presets, and log a warning.
        /// </summary>
        private void AutoRemoveLocalDuplicatesAgainstPresets()
        {
            if (_propPresets == null || _propTypes == null)
                return;

            // Collect all types from all presets into a set of AQN strings
            var presetTypes = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < _propPresets.arraySize; i++)
            {
                var elem   = _propPresets.GetArrayElementAtIndex(i);
                var preset = elem.objectReferenceValue as SystemsPreset;
                if (preset == null) continue;
                if (preset.systemTypes == null) continue;

                foreach (var r in preset.systemTypes)
                {
                    var t = r.Resolve();
                    var key = t?.AssemblyQualifiedName ?? t?.FullName;
                    if (!string.IsNullOrEmpty(key))
                        presetTypes.Add(key!);
                }
            }

            if (presetTypes.Count == 0)
                return;

            bool changed = false;

            // From installer-local systemTypes, remove any that are already in presets
            for (int i = _propTypes!.arraySize - 1; i >= 0; i--)
            {
                var elem = _propTypes.GetArrayElementAtIndex(i);
                var aqn  = elem.FindPropertyRelative("_assemblyQualifiedName").stringValue;

                if (string.IsNullOrWhiteSpace(aqn))
                    continue; // keep empty slots

                if (presetTypes.Contains(aqn))
                {
                    var t        = Type.GetType(aqn, throwOnError: false);
                    var typeName = t != null ? t.FullName : aqn;

                    Debug.LogWarning(
                        $"[WorldSystemInstaller] Removed installer-local system '{typeName}' " +
                        $"because it already exists in one of the assigned System Presets.",
                        target
                    );

                    _propTypes.DeleteArrayElementAtIndex(i);
                    changed = true;
                }
            }

            if (changed)
                serializedObject.ApplyModifiedProperties();
        }

        // ───────────────────────────────── System picker ─────────────────────────────────

        /// <summary>
        /// Open the system picker for a specific index in systemTypes, anchored to the given rect.
        /// </summary>
        private void ShowSystemTypePickerForIndex(int index, Rect activatorRectGui, Action onCancel)
        {
            // 1) collect all concrete ISystem types
            var all = TypeCache
                .GetTypesDerivedFrom<ISystem>()
                .Where(t => t != null && !t.IsAbstract)
                .Distinct()
                .OrderBy(t => t.FullName)
                .ToList();

            // 2) disabled set: already in local list (except this index) + in presets
            var disabled = new HashSet<Type>();

            // (a) local list
            for (int i = 0; i < _propTypes!.arraySize; i++)
            {
                if (i == index) continue;

                var elem = _propTypes.GetArrayElementAtIndex(i);
                var aqn  = elem.FindPropertyRelative("_assemblyQualifiedName").stringValue;
                var t    = string.IsNullOrWhiteSpace(aqn)
                    ? null
                    : Type.GetType(aqn, throwOnError: false);

                if (t != null)
                    disabled.Add(t);
            }

            // (b) presets
            if (_propPresets != null)
            {
                for (int i = 0; i < _propPresets.arraySize; i++)
                {
                    var elem   = _propPresets.GetArrayElementAtIndex(i);
                    var preset = elem.objectReferenceValue as SystemsPreset;
                    if (preset == null || preset.systemTypes == null) continue;

                    foreach (var r in preset.systemTypes)
                    {
                        var t = r.Resolve();
                        if (t != null)
                            disabled.Add(t);
                    }
                }
            }

            // 3) show picker at the [+] button rect
            ZenSystemPickerWindow.Show(
                allSystemTypes: all,
                disabled: disabled,
                onPick: (t) =>
                {
                    serializedObject.Update();
                    var elem = _propTypes!.GetArrayElementAtIndex(index);
                    elem.FindPropertyRelative("_assemblyQualifiedName").stringValue = t.AssemblyQualifiedName;
                    serializedObject.ApplyModifiedProperties();
                    Repaint();
                },
                activatorRectGui: activatorRectGui,
                title: "Add System",
                onCancel: onCancel
            );
        }
    }
}
#endif
