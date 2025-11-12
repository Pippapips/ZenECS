#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ZenECS.Adapter.Unity.Install;
using ZenECS.Adapter.Unity.Util;
using ZenECS.Core.Systems;
using ZenECS.EditorCommon;

namespace ZenECS.EditorInspectors
{
    /// <summary>
    /// Custom inspector for <see cref="WorldSystemInstaller"/>.
    /// Provides:
    /// - ReorderableList UX with a popup picker to add ISystem types
    /// - Automatic de-duplication against the assigned SystemsPreset (SO)
    /// - Immediate removal of installer-local entries that are duplicated by the preset, with Debug.LogWarning
    /// </summary>
    [CustomEditor(typeof(WorldSystemInstaller))]
    public sealed class WorldSystemInstallerInspector : Editor
    {
        private ReorderableList? _list;
        private SerializedProperty? _propTypes;
        private SerializedProperty? _propPreset;
        private SerializedProperty? _propUseCurrent;
        private SerializedProperty? _propWorldName;
        private SerializedProperty? _propSetCurrent;

        private bool _didAutoCleanThisFrame;

        /// <summary>
        /// Unity callback: initialize serialized properties and ReorderableList bindings.
        /// </summary>
        private void OnEnable()
        {
            _propTypes = serializedObject.FindProperty("systemTypes");
            _propPreset = serializedObject.FindProperty("systemsPreset");
            _propUseCurrent = serializedObject.FindProperty("useCurrentWorld");
            _propWorldName = serializedObject.FindProperty("worldName");
            _propSetCurrent = serializedObject.FindProperty("setAsCurrentOnEnsure");

            _list = new ReorderableList(serializedObject, _propTypes, draggable: true, displayHeader: true,
                displayAddButton: true, displayRemoveButton: true);
            _list.drawHeaderCallback = rect =>
                EditorGUI.LabelField(rect, "System Types (Installer-local)", EditorStyles.boldLabel);
            _list.elementHeight = EditorGUIUtility.singleLineHeight + 15;

            _list.drawElementCallback = (rect, index, active, focused) =>
            {
                var elem = _propTypes!.GetArrayElementAtIndex(index);
                var aqnStr = elem.FindPropertyRelative("_assemblyQualifiedName").stringValue;
                var type = string.IsNullOrWhiteSpace(aqnStr) ? null : Type.GetType(aqnStr, throwOnError: false);

                var lineRect = new Rect(rect.x + 6, rect.y + 3, rect.width - 12,
                    EditorGUIUtility.singleLineHeight + 10);
                if (type != null)
                {
                    var ns = type.Namespace ?? "Global";
                    var name = type.Name;
                    var style = new GUIStyle(EditorStyles.label) { richText = true };
                    var text = $"<b>{name}</b>\n<color=#888888><size=10>({ns})</size></color>";
                    EditorGUI.LabelField(lineRect, new GUIContent(text, type.AssemblyQualifiedName), style);
                }
                else
                {
                    var style = new GUIStyle(EditorStyles.label)
                        { normal = { textColor = new Color(1f, 0.35f, 0.35f) } };
                    EditorGUI.LabelField(lineRect, "None (Click + to open picker)", style);
                }
            };

            _list.onAddCallback = list =>
            {
                serializedObject.Update();
                var newIndex = _propTypes!.arraySize;
                _propTypes.InsertArrayElementAtIndex(newIndex);
                var elem = _propTypes.GetArrayElementAtIndex(newIndex);
                elem.FindPropertyRelative("_assemblyQualifiedName").stringValue = string.Empty;
                serializedObject.ApplyModifiedProperties();

                // Open picker; if canceled, remove the just-created empty slot.
                ShowPickerForIndex(newIndex, () =>
                {
                    serializedObject.Update();
                    var e = _propTypes.GetArrayElementAtIndex(newIndex);
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

        /// <summary>
        /// Unity callback: render the custom inspector GUI and run auto-clean pass once per frame.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("World Setup Installer", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("World", EditorStyles.miniBoldLabel);
                EditorGUILayout.PropertyField(_propUseCurrent!, new GUIContent("Use Current World"));
                using (new EditorGUI.DisabledScope(_propUseCurrent is { boolValue: true }))
                {
                    EditorGUILayout.PropertyField(_propWorldName!, new GUIContent("World Name"));
                    EditorGUILayout.PropertyField(_propSetCurrent!, new GUIContent("Set As Current On Ensure"));
                }
            }

            EditorGUILayout.Space(6);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Systems (Preset + Installer-local)", EditorStyles.miniBoldLabel);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_propPreset!, new GUIContent("Systems Preset (SO)"));
                _ = EditorGUI.EndChangeCheck();

                EditorGUILayout.HelpBox(
                    "At registration time, the Preset and the Installer-local lists are merged and duplicates are always removed.\n" +
                    "If a type exists in the Preset, any duplicate in the Installer-local list is removed immediately and a warning is logged.",
                    MessageType.Info);
            }

            // Installer-local list (with popup picker on +)
            _list!.DoLayoutList();

            // Auto de-dup against the preset once per GUI pass.
            if (!_didAutoCleanThisFrame)
            {
                _didAutoCleanThisFrame = true;
                AutoRemoveLocalDuplicatesAgainstPreset();
            }

            serializedObject.ApplyModifiedProperties();
            _didAutoCleanThisFrame = false;
        }

        /// <summary>
        /// Removes entries from the installer-local list that are already present in the assigned SystemsPreset.
        /// Logs a warning for each removed entry. Empty slots are preserved.
        /// </summary>
        private void AutoRemoveLocalDuplicatesAgainstPreset()
        {
            if (_propPreset == null || _propTypes == null) return;

            var presetObj = _propPreset.objectReferenceValue as SystemsPreset;
            if (presetObj == null) return;

            // Collect preset types into a hash set for fast lookups.
            var presetTypes = new HashSet<string>(StringComparer.Ordinal);
            if (presetObj.systemTypes != null)
            {
                foreach (var r in presetObj.systemTypes)
                {
                    var t = r.Resolve();
                    var key = t?.AssemblyQualifiedName ?? t?.FullName;
                    if (!string.IsNullOrEmpty(key))
                        presetTypes.Add(key!);
                }
            }

            if (presetTypes.Count == 0) return;

            // Remove duplicates from local list and warn the user.
            bool changed = false;
            for (int i = _propTypes.arraySize - 1; i >= 0; i--)
            {
                var elem = _propTypes.GetArrayElementAtIndex(i);
                var aqn = elem.FindPropertyRelative("_assemblyQualifiedName").stringValue;

                if (string.IsNullOrWhiteSpace(aqn))
                    continue; // keep empty slots

                if (presetTypes.Contains(aqn))
                {
                    var t = Type.GetType(aqn, throwOnError: false);
                    var typeName = t != null ? t.FullName : aqn;
                    Debug.LogWarning(
                        $"[WorldSetupInstaller] Removed installer-local system '{typeName}' because it exists in SystemsPreset '{presetObj.name}'.",
                        target);
                    _propTypes.DeleteArrayElementAtIndex(i);
                    changed = true;
                }
            }

            if (changed)
                serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Opens the system picker for a specific list index. On cancel, invokes <paramref name="onCancel"/>.
        /// Disables types already present in the local list (except the current index) and in the SystemsPreset.
        /// </summary>
        /// <param name="index">Installer-local list index to populate.</param>
        /// <param name="onCancel">Callback invoked if the picker is canceled.</param>
        private void ShowPickerForIndex(int index, Action onCancel)
        {
            // 1) Collect all concrete ISystem types (Editor-only TypeCache for performance).
            var all = TypeCache.GetTypesDerivedFrom<ISystem>()
                .Where(t => t != null && !t.IsAbstract)
                .Distinct()
                .OrderBy(t => t.FullName)
                .ToList();

            // 2) Compute disabled set: already in local list (excluding the current slot) + already in preset.
            var disabled = new HashSet<Type>();

            // (a) local list
            for (int i = 0; i < _propTypes!.arraySize; i++)
            {
                if (i == index) continue;
                var elem = _propTypes.GetArrayElementAtIndex(i);
                var aqn = elem.FindPropertyRelative("_assemblyQualifiedName").stringValue;
                var t = string.IsNullOrWhiteSpace(aqn) ? null : Type.GetType(aqn, throwOnError: false);
                if (t != null) disabled.Add(t);
            }

            // (b) preset
            var presetObj = _propPreset!.objectReferenceValue as SystemsPreset;
            if (presetObj != null && presetObj.systemTypes != null)
            {
                foreach (var r in presetObj.systemTypes)
                {
                    var t = r.Resolve();
                    if (t != null) disabled.Add(t);
                }
            }

            // 3) Place dropdown near the list bottom line.
            var rect = GUILayoutUtility.GetLastRect();
            rect = new Rect(rect.x + rect.width - 200, rect.yMax, 200, 20);

            ZenSystemPickerWindow.Show(
                allSystemTypes: all,
                disabled: disabled,
                onPick: (t) =>
                {
                    serializedObject.Update();
                    var elem = _propTypes.GetArrayElementAtIndex(index);
                    elem.FindPropertyRelative("_assemblyQualifiedName").stringValue = t.AssemblyQualifiedName;
                    serializedObject.ApplyModifiedProperties();
                    Repaint();
                },
                activatorRectGui: rect,
                title: "Add System",
                onCancel: onCancel);
        }
    }
}
#endif