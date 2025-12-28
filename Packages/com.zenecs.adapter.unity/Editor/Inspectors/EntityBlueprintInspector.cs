// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: EntityBlueprintInspector.cs
// Purpose: Custom inspector for EntityBlueprint ScriptableObject that provides
//          rich editing UI for component snapshots, contexts, and binders.
// Key concepts:
//   • Component editing: ReorderableList for component entries with JSON editing.
//   • Context management: list of ContextAsset references with pickers.
//   • Binder editing: list of BinderAsset references with pickers.
//   • Validation: visual indicators for assigned/missing components.
//   • Editor-only: compiled out in player builds via #if UNITY_EDITOR.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using ZenECS.Adapter.Unity.Blueprints;
using ZenECS.Adapter.Unity.Binding.Binders.Assets;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Adapter.Unity.Editor.GUIs;
using ZenECS.Core;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Editor.Inspectors
{
    [CustomEditor(typeof(EntityBlueprint))]
    public sealed class EntityBlueprintInspector : UnityEditor.Editor
    {
        // ───────── Components (BlueprintData) ─────────
        SerializedProperty _dataProp; // _data
        SerializedProperty _compEntriesProp; // _data.entries
        ReorderableList _componentsList;

        // ───────── Contexts (ContextAsset refs) ─────────
        SerializedProperty _contextsProp; // _contextAssets (List<ContextAsset>)
        ReorderableList _contextsList;

        // ───────── Binders (BinderAsset refs) ─────────
        SerializedProperty _binderAssetsProp; // _binderAssets (List<BinderAsset>)
        ReorderableList _binderAssetsList;

        readonly Dictionary<string, bool> _fold = new();
        const float PAD = 6f;

        GUIStyle _obsOkStyle; // Assigned component name
        GUIStyle _obsMissingStyle; // Not-assigned component name
        GUIStyle _obsCheckStyle; // ✔ icon
        GUIStyle _obsXStyle; // ✕ icon
        GUIStyle _namespaceStyle;
        bool _stylesReady;

        void OnEnable()
        {
            // Components
            _dataProp = serializedObject.FindProperty("_data");
            _compEntriesProp = _dataProp?.FindPropertyRelative("entries");
            if (_compEntriesProp != null && _compEntriesProp.isArray)
                BuildComponentsList();

            // Contexts (SO refs: _contextAssets)
            _contextsProp = serializedObject.FindProperty("_contextAssets");
            if (_contextsProp != null && _contextsProp.isArray)
            {
                // Try to repair broken references
                ValidateAndRepairContextAssets();
                BuildContextsList();
            }

            // Binders (SO refs: _binderAssets)
            _binderAssetsProp = serializedObject.FindProperty("_binderAssets");
            if (_binderAssetsProp != null && _binderAssetsProp.isArray)
            {
                // Try to repair broken references
                ValidateAndRepairBinderAssets();
                BuildBinderAssetsList();
            }
        }

        /// <summary>
        /// Validates and repairs broken references in _contextAssets.
        /// Handles cases where references are broken after file copy when GUID changes.
        /// </summary>
        void ValidateAndRepairContextAssets()
        {
            if (_contextsProp == null || !_contextsProp.isArray) return;

            bool needsRepair = false;
            var blueprint = target as EntityBlueprint;
            if (blueprint == null) return;

            // Directory path where current asset is located
            var blueprintPath = AssetDatabase.GetAssetPath(blueprint);
            var blueprintDir = System.IO.Path.GetDirectoryName(blueprintPath).Replace('\\', '/');

            serializedObject.Update();

            for (int i = 0; i < _contextsProp.arraySize; i++)
            {
                var elem = _contextsProp.GetArrayElementAtIndex(i);
                var asset = elem.objectReferenceValue as ScriptableObject;

                // Try to repair if reference is null or broken
                if (asset == null)
                {
                    // Try to find by name in the same directory
                    var allContextAssets = ZenAssetDatabase.FindAndLoadAllAssets<ScriptableObject>($"t:ScriptableObject");
                    
                    // Find ContextAsset in the same directory
                    ScriptableObject foundAsset = null;
                    foreach (var candidate in allContextAssets)
                    {
                        var candidatePath = AssetDatabase.GetAssetPath(candidate);
                        var candidateDir = System.IO.Path.GetDirectoryName(candidatePath).Replace('\\', '/');
                        
                        // If same directory and ContextAsset type
                        if (candidateDir == blueprintDir && 
                            candidate.GetType().IsSubclassOf(typeof(ZenECS.Adapter.Unity.Binding.Contexts.Assets.ContextAsset)))
                        {
                            // Use first found (more sophisticated matching can be improved later)
                            foundAsset = candidate;
                            break;
                        }
                    }

                    if (foundAsset != null)
                    {
                        elem.objectReferenceValue = foundAsset;
                        needsRepair = true;
                    }
                }
            }

            if (needsRepair)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(blueprint);
                // Mark referenced assets as dirty
                MarkReferencedAssetsDirty();
            }
        }

        void EnsureStylesReady()
        {
            if (_stylesReady) return;

            var baseStyle = EditorStyles.miniLabel ?? EditorStyles.label ?? GUI.skin?.label ?? new GUIStyle();

            _obsMissingStyle = new GUIStyle(baseStyle)
            {
                fontStyle = FontStyle.Italic,
                richText = false,
                normal = { textColor = Color.gray }
            };

            _obsOkStyle = new GUIStyle(baseStyle)
            {
                fontStyle = FontStyle.Normal,
                richText = false,
                normal = { textColor = Color.white }
            };

            _obsCheckStyle = new GUIStyle(baseStyle)
            {
                alignment = TextAnchor.MiddleRight,
                richText = true
            };

            _obsXStyle = new GUIStyle(baseStyle)
            {
                alignment = TextAnchor.MiddleRight,
                richText = true
            };

            // Namespace style
            _namespaceStyle = new GUIStyle(baseStyle)
            {
                fontStyle = FontStyle.Normal,
                richText = false,
            };

            // Light gray when ProSkin (dark theme)
            if (EditorGUIUtility.isProSkin)
            {
                _namespaceStyle.normal.textColor = new Color(0.78f, 0.78f, 0.78f); // #C7C7C7 approximately
            }
            else
            {
                // Medium gray in light theme
                _namespaceStyle.normal.textColor = new Color(0.25f, 0.25f, 0.25f);
            }

            _stylesReady = true;
        }

        public override void OnInspectorGUI()
        {
            ZenEcsGUIHeader.DrawHeader(
                "Entity Blueprint",
                "Defines components, contexts, and binders used to spawn a fully configured entity.",
                new[]
                {
                    "Runtime Blueprint",
                    "Components + Contexts + Binders"
                }
            );
            
            EnsureStylesReady(); // ← Must be called first

            serializedObject.Update();

            if (_componentsList != null)
            {
                EditorGUILayout.Space(3);
                _componentsList.DoLayoutList();
            }

            if (_contextsList != null)
            {
                EditorGUILayout.Space(8);
                _contextsList.DoLayoutList();
            }

            if (_binderAssetsList != null)
            {
                EditorGUILayout.Space(8);
                _binderAssetsList.DoLayoutList();
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                // Mark referenced ContextAssets as dirty so they are saved together
                MarkReferencedAssetsDirty();
                EditorUtility.SetDirty(target);
            }
        }

        /// <summary>
        /// Marks referenced assets (ContextAssets, BinderAssets) as dirty so Unity saves them together.
        /// </summary>
        private void MarkReferencedAssetsDirty()
        {
            if (_contextsProp != null && _contextsProp.isArray)
            {
                for (int i = 0; i < _contextsProp.arraySize; i++)
                {
                    var elem = _contextsProp.GetArrayElementAtIndex(i);
                    var asset = elem.objectReferenceValue as ScriptableObject;
                    if (asset != null)
                    {
                        EditorUtility.SetDirty(asset);
                    }
                }
            }

            if (_binderAssetsProp != null && _binderAssetsProp.isArray)
            {
                for (int i = 0; i < _binderAssetsProp.arraySize; i++)
                {
                    var elem = _binderAssetsProp.GetArrayElementAtIndex(i);
                    var asset = elem.objectReferenceValue as ScriptableObject;
                    if (asset != null)
                    {
                        EditorUtility.SetDirty(asset);
                    }
                }
            }
        }

        /// <summary>
        /// Validates and repairs broken references in _binderAssets.
        /// Handles cases where references are broken after file copy when GUID changes.
        /// </summary>
        void ValidateAndRepairBinderAssets()
        {
            if (_binderAssetsProp == null || !_binderAssetsProp.isArray) return;

            bool needsRepair = false;
            var blueprint = target as EntityBlueprint;
            if (blueprint == null) return;

            // Directory path where current asset is located
            var blueprintPath = AssetDatabase.GetAssetPath(blueprint);
            var blueprintDir = System.IO.Path.GetDirectoryName(blueprintPath).Replace('\\', '/');

            serializedObject.Update();

            for (int i = 0; i < _binderAssetsProp.arraySize; i++)
            {
                var elem = _binderAssetsProp.GetArrayElementAtIndex(i);
                var asset = elem.objectReferenceValue as ScriptableObject;

                // Try to repair if reference is null or broken
                if (asset == null)
                {
                    // Try to find by name in the same directory
                    var allBinderAssets = ZenAssetDatabase.FindAndLoadAllAssets<ScriptableObject>($"t:ScriptableObject");
                    
                    // Find BinderAsset in the same directory
                    ScriptableObject foundAsset = null;
                    foreach (var candidate in allBinderAssets)
                    {
                        var candidatePath = AssetDatabase.GetAssetPath(candidate);
                        var candidateDir = System.IO.Path.GetDirectoryName(candidatePath).Replace('\\', '/');
                        
                        // If same directory and BinderAsset type
                        if (candidateDir == blueprintDir && 
                            candidate.GetType().IsSubclassOf(typeof(BinderAsset)))
                        {
                            // Use first found (more sophisticated matching can be improved later)
                            foundAsset = candidate;
                            break;
                        }
                    }

                    if (foundAsset != null)
                    {
                        elem.objectReferenceValue = foundAsset;
                        needsRepair = true;
                        Debug.LogWarning(
                            $"[EntityBlueprint] Repaired broken BinderAsset reference: " +
                            $"{AssetDatabase.GetAssetPath(blueprint)} -> {AssetDatabase.GetAssetPath(foundAsset)}"
                        );
                    }
                }
            }

            if (needsRepair)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(blueprint);
                // Mark referenced assets as dirty
                MarkReferencedAssetsDirty();
            }
        }

        // =====================================================================
        // Helpers: expand/collapse all
        // =====================================================================
        void SetComponentsFoldAll(bool open)
        {
            if (_compEntriesProp == null || !_compEntriesProp.isArray)
                return;

            for (int i = 0; i < _compEntriesProp.arraySize; i++)
            {
                var e = _compEntriesProp.GetArrayElementAtIndex(i);
                var tname = e.FindPropertyRelative("typeName").stringValue;
                var key = $"comp:{i}:{tname}";
                _fold[key] = open;
            }
        }


        // =====================================================================
        // Components (BlueprintData as-is)
        // =====================================================================
        void BuildComponentsList()
        {
            _componentsList = new ReorderableList(serializedObject, _compEntriesProp, true, true, true, true);

            _componentsList.drawHeaderCallback = r =>
            {
                const float buttonWidth = 24f;
                const float buttonGap = 2f;

                var labelRect = new Rect(r.x, r.y, r.width - (buttonWidth * 2f + buttonGap * 2f), r.height);
                EditorGUI.LabelField(labelRect, "Components (BlueprintData)", EditorStyles.boldLabel);

                var expandRect = new Rect(r.xMax - (buttonWidth * 2f + buttonGap), r.y, buttonWidth, r.height);
                var collapseRect = new Rect(r.xMax - buttonWidth, r.y, buttonWidth, r.height);

                if (GUI.Button(expandRect, new GUIContent("▼", "Expand all component entries"), EditorStyles.miniButton))
                {
                    SetComponentsFoldAll(true);
                }

                if (GUI.Button(collapseRect, new GUIContent("▲", "Collapse all component entries"), EditorStyles.miniButton))
                {
                    SetComponentsFoldAll(false);
                }
            };

            _componentsList.onAddDropdownCallback = (rect, list) =>
            {
                var all = ZenComponentPickerWindow.FindAllZenComponents().ToList();
                var disabled = new HashSet<Type>();

                for (int i = 0; i < _compEntriesProp.arraySize; i++)
                {
                    var tname = _compEntriesProp.GetArrayElementAtIndex(i).FindPropertyRelative("typeName").stringValue;
                    var rt = EntityBlueprintData.Resolve(tname);
                    if (rt != null) disabled.Add(rt);
                }

                ZenComponentPickerWindow.Show(
                    all, disabled,
                    onPick: pickedType =>
                    {
                        serializedObject.Update();
                        int idx = _compEntriesProp.arraySize;
                        _compEntriesProp.InsertArrayElementAtIndex(idx);
                        var elem = _compEntriesProp.GetArrayElementAtIndex(idx);
                        elem.FindPropertyRelative("typeName").stringValue = pickedType.AssemblyQualifiedName;

                        var inst = ZenDefaults.CreateWithDefaults(pickedType);
                        elem.FindPropertyRelative("json").stringValue = EntityBlueprintComponentJson.Serialize(inst, pickedType);

                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(target);
                    },
                    activatorRectGui: rect,
                    title: "Add Component"
                );
            };

            _componentsList.onRemoveCallback = rl =>
            {
                if (rl.index >= 0 && rl.index < _compEntriesProp.arraySize)
                    _compEntriesProp.DeleteArrayElementAtIndex(rl.index);
            };

            _componentsList.elementHeightCallback = (index) =>
            {
                var e = _compEntriesProp.GetArrayElementAtIndex(index);
                var tname = e.FindPropertyRelative("typeName").stringValue;
                var key = $"comp:{index}:{tname}";

                float headerH = EditorGUIUtility.singleLineHeight + 6f;
                bool open = _fold.TryGetValue(key, out var o) ? o : true;
                if (!open) return headerH;

                var t = EntityBlueprintData.Resolve(tname);
                bool hasFields = t != null && ZenComponentFormGUI.HasDrawableFields(t);
                if (!hasFields) return headerH;

                // Namespace one-line height
                float nsH = 0f;
                if (t != null && !string.IsNullOrEmpty(t.Namespace))
                {
                    // 1 line + slight top/bottom margins
                    nsH = EditorGUIUtility.singleLineHeight + 4f;
                }

                var jsonProp = e.FindPropertyRelative("json");
                var obj = EntityBlueprintComponentJson.Deserialize(jsonProp.stringValue, t);
                float bodyH = ZenComponentFormGUI.CalcHeightForObject(obj, t);

                return headerH // Foldout header
                       + nsH // Namespace line
                       + 6f // Margin between header/namespace and body
                       + Mathf.Max(0f, bodyH)
                       + 6f; // Final margin
            };

            _componentsList.drawElementCallback = (rect, index, active, focused) =>
            {
                var e = _compEntriesProp.GetArrayElementAtIndex(index);
                var pType = e.FindPropertyRelative("typeName");
                var pJson = e.FindPropertyRelative("json");
                var t = EntityBlueprintData.Resolve(pType.stringValue);

                var key = $"comp:{index}:{pType.stringValue}";
                if (!_fold.ContainsKey(key)) _fold[key] = true;

                bool hasFields = t != null && ZenComponentFormGUI.HasDrawableFields(t);

                // ─ Header: Foldout + Reset(R) + Ping (search icon) ─
                float line = EditorGUIUtility.singleLineHeight;
                var rHead = new Rect(rect.x + 4, rect.y + 3, rect.width - 8, line);

                const float btnW = 20f; // Reset / Ping both same width

                // Right end: Ping
                var rPing = new Rect(rHead.xMax - btnW, rHead.y, btnW, rHead.height);
                // Left of that: Reset button (R)
                var rReset = new Rect(rHead.xMax - btnW * 2f - 2f, rHead.y, btnW, rHead.height);
                // Remaining area: Foldout
                var rFold = new Rect(
                    rHead.x,
                    rHead.y,
                    rHead.width - (btnW * 2f + 6f),
                    rHead.height
                );

                string label = t != null ? t.Name : "(Missing Type)";

                bool openBefore = _fold[key];
                bool openNow = EditorGUI.Foldout(
                    rFold,
                    openBefore,
                    label,
                    true,
                    EditorStyles.foldoutHeader
                );
                _fold[key] = hasFields && openNow;

                // Reset(R) button: initialize to default values
                using (new EditorGUI.DisabledScope(t == null))
                {
                    if (GUI.Button(rReset, new GUIContent("R", "Reset component to defaults"),
                            EditorStyles.miniButton) && t != null)
                    {
                        Undo.RecordObject(target, "Reset Blueprint Component");

                        // Create new instance with ZenDefaults then serialize to JSON again
                        var inst = ZenDefaults.CreateWithDefaults(t);
                        pJson.stringValue = EntityBlueprintComponentJson.Serialize(inst, t);

                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(target);
                    }
                }

                // Ping button next to name (search icon)
                using (new EditorGUI.DisabledScope(t == null))
                {
                    var gcPing = GetSearchIconContent("Ping component script in Project");
                    if (GUI.Button(rPing, gcPing, EditorStyles.iconButton) && t != null)
                    {
                        PingTypeSource(t);
                    }
                }

                if (!hasFields || !_fold[key]) return;

                // ─ Namespace + body keep existing code ─
                float y = rHead.yMax;

                if (t != null && !string.IsNullOrEmpty(t.Namespace))
                {
                    EnsureStylesReady(); // Use _namespaceStyle
                    y += 2f;
                    var nsRect = new Rect(rect.x + 8, y, rect.width - 16, EditorGUIUtility.singleLineHeight);
                    EditorGUI.LabelField(nsRect, t.Namespace, _namespaceStyle);
                    y += EditorGUIUtility.singleLineHeight + 2f;
                }
                else
                {
                    y += 6f;
                }

                var rBody = new Rect(rect.x + 8, y, rect.width - 16, rect.yMax - y - 6f);

                if (t != null)
                {
                    var obj = EntityBlueprintComponentJson.Deserialize(pJson.stringValue, t);
                    EditorGUI.BeginChangeCheck();
                    ZenComponentFormGUI.DrawObject(rBody, obj, t, false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Edit Blueprint Component");
                        pJson.stringValue = EntityBlueprintComponentJson.Serialize(obj, t);
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(target);
                    }
                }
            };
        }

        // =====================================================================
        // Contexts (ContextAsset refs)
        // =====================================================================
        void BuildContextsList()
        {
            _contextsList = new ReorderableList(serializedObject, _contextsProp, true, true, true, true);

            _contextsList.drawHeaderCallback = r =>
                EditorGUI.LabelField(r, "Contexts (Context Assets)", EditorStyles.boldLabel);

            // Simply: onAdd adds one empty slot, user directly drags/selects SO
            _contextsList.onAddCallback = rl =>
            {
                int idx = _contextsProp.arraySize;
                _contextsProp.InsertArrayElementAtIndex(idx);
                var elem = _contextsProp.GetArrayElementAtIndex(idx);
                elem.objectReferenceValue = null;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            };

            _contextsList.onRemoveCallback = rl =>
            {
                if (rl.index >= 0 && rl.index < _contextsProp.arraySize)
                {
                    var elem = _contextsProp.GetArrayElementAtIndex(rl.index);
                    var asset = elem.objectReferenceValue as ScriptableObject;
                    _contextsProp.DeleteArrayElementAtIndex(rl.index);
                    serializedObject.ApplyModifiedProperties();
                    // Mark removed asset as dirty (so reference removal is saved)
                    if (asset != null)
                    {
                        EditorUtility.SetDirty(asset);
                    }
                    EditorUtility.SetDirty(target);
                }
            };

            _contextsList.elementHeightCallback = index =>
            {
                float line = EditorGUIUtility.singleLineHeight;
                float h = line; // ObjectField

                // var p = _contextsProp.GetArrayElementAtIndex(index);
                // var obj = p.objectReferenceValue;
                // if (obj != null)
                // {
                //     var path = AssetDatabase.GetAssetPath(obj);
                //     if (!string.IsNullOrEmpty(path))
                //     {
                //         h += line + 4f; // Path one line
                //     }
                //     else
                //     {
                //         h += line + 4f; // "(Not saved asset)" one line
                //     }
                // }

                return h + 4f;
            };

            _contextsList.drawElementCallback = (rect, index, active, focused) =>
            {
                var p = _contextsProp.GetArrayElementAtIndex(index);
                var objBefore = p.objectReferenceValue;

                float line = EditorGUIUtility.singleLineHeight;

                // 1) Context SO ObjectField
                var rField = new Rect(rect.x + 4, rect.y + 3, rect.width - 8, line);
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rField, p, GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                {
                    var objAfter = p.objectReferenceValue;
                    // Mark as dirty if reference changed or newly assigned
                    if (objAfter != null && objAfter is ScriptableObject so)
                    {
                        EditorUtility.SetDirty(so);
                    }
                    // Also mark previous reference as dirty (so removal is saved)
                    if (objBefore != null && objBefore is ScriptableObject soBefore)
                    {
                        EditorUtility.SetDirty(soBefore);
                    }
                    EditorUtility.SetDirty(target);
                }

                var obj = p.objectReferenceValue;
                if (obj == null)
                    return;

                // // 2) SO path line
                // var path = AssetDatabase.GetAssetPath(obj);
                // if (string.IsNullOrEmpty(path))
                // {
                //     path = "(Not saved asset)";
                // }
                // else
                // {
                //     path = $"[{path}]";
                // }
                //
                // // Align with field start position, dark gray miniLabel style
                // var rPath = new Rect(rect.x + 8, rField.yMax + 2f, rect.width - 16, line);
                //
                // var prevColor = GUI.color;
                // GUI.color = new Color(0.45f, 0.45f, 0.45f); // Dark gray
                // EditorGUI.LabelField(rPath, path, EditorStyles.miniLabel);
                // GUI.color = prevColor;
            };
        }

        // =====================================================================
        // Binders (BinderAsset refs)
        // =====================================================================
        void BuildBinderAssetsList()
        {
            _binderAssetsList = new ReorderableList(serializedObject, _binderAssetsProp, true, true, true, true);

            _binderAssetsList.drawHeaderCallback = r =>
                EditorGUI.LabelField(r, "Binders (Binder Assets)", EditorStyles.boldLabel);

            // Simply: onAdd adds one empty slot, user directly drags/selects SO
            _binderAssetsList.onAddCallback = rl =>
            {
                int idx = _binderAssetsProp.arraySize;
                _binderAssetsProp.InsertArrayElementAtIndex(idx);
                var elem = _binderAssetsProp.GetArrayElementAtIndex(idx);
                elem.objectReferenceValue = null;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            };

            _binderAssetsList.onRemoveCallback = rl =>
            {
                if (rl.index >= 0 && rl.index < _binderAssetsProp.arraySize)
                {
                    var elem = _binderAssetsProp.GetArrayElementAtIndex(rl.index);
                    var asset = elem.objectReferenceValue as ScriptableObject;
                    _binderAssetsProp.DeleteArrayElementAtIndex(rl.index);
                    serializedObject.ApplyModifiedProperties();
                    // Mark removed asset as dirty (so reference removal is saved)
                    if (asset != null)
                    {
                        EditorUtility.SetDirty(asset);
                    }
                    EditorUtility.SetDirty(target);
                }
            };

            _binderAssetsList.elementHeightCallback = index =>
            {
                float line = EditorGUIUtility.singleLineHeight;
                float h = line; // ObjectField

                return h + 4f;
            };

            _binderAssetsList.drawElementCallback = (rect, index, active, focused) =>
            {
                var p = _binderAssetsProp.GetArrayElementAtIndex(index);
                var objBefore = p.objectReferenceValue;

                float line = EditorGUIUtility.singleLineHeight;

                // 1) BinderAsset SO ObjectField
                var rField = new Rect(rect.x + 4, rect.y + 3, rect.width - 8, line);
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rField, p, GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                {
                    var objAfter = p.objectReferenceValue;
                    // Mark as dirty if reference changed or newly assigned
                    if (objAfter != null && objAfter is ScriptableObject so)
                    {
                        EditorUtility.SetDirty(so);
                    }
                    // Also mark previous reference as dirty (so removal is saved)
                    if (objBefore != null && objBefore is ScriptableObject soBefore)
                    {
                        EditorUtility.SetDirty(soBefore);
                    }
                    EditorUtility.SetDirty(target);
                }
            };
        }

        // =====================================================================
        // Badge helpers
        // =====================================================================
        static List<Type> GetProvidedContextTypes(SerializedProperty contextsProp)
        {
            var list = new List<Type>();
            if (contextsProp == null || !contextsProp.isArray) return list;

            for (int i = 0; i < contextsProp.arraySize; i++)
            {
                var p = contextsProp.GetArrayElementAtIndex(i);
                var inst = p?.managedReferenceValue;
                if (inst != null) list.Add(inst.GetType());
                // NOTE: Currently contextsProp is ContextAsset (Object ref), so
                // managedReferenceValue will mostly be null.
                // When reviving pill badges later, resolve ContextAsset → IContext type mapping
                // as separate metadata.
            }

            return list;
        }

        static bool IsSatisfied(Type required, IEnumerable<Type> providedTypes)
        {
            foreach (var pt in providedTypes)
                if (required.IsAssignableFrom(pt))
                    return true;
            return false;
        }

        // === Binder meta extraction: collect observed component types from IBind<T...> ===
        static IReadOnlyList<Type> ExtractObservedComponentTypes(Type binderType)
        {
            static bool IsBindsInterface(Type t)
                => t.IsInterface && t.IsGenericType && t.Name.StartsWith("IBind", StringComparison.Ordinal);

            var set = new HashSet<Type>();
            foreach (var itf in binderType.GetInterfaces())
            {
                if (!IsBindsInterface(itf)) continue;
                foreach (var ga in itf.GetGenericArguments())
                {
                    if (ga.IsAbstract) continue;
                    if (ga.Namespace?.EndsWith(".Editor", StringComparison.Ordinal) == true) continue;
                    set.Add(ga);
                }
            }

            return set.OrderBy(t => t.Name).ToArray();
        }

        // === Collect [Context] field types from Binder ===
        static Type[] ExtractContextFieldTypes(object binderInstance)
        {
            if (binderInstance == null) return Array.Empty<Type>();
            var t = binderInstance.GetType();
            var list = new List<Type>(8);

            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                // Only accept fields with [Context] attribute
                var hasAttr = f.GetCustomAttributes(inherit: true)
                    .Any(a => a.GetType().Name is "ContextAttribute");
                if (!hasAttr) continue;

                var ft = f.FieldType;
                if (typeof(ZenECS.Core.Binding.IContext).IsAssignableFrom(ft))
                    list.Add(ft);
            }

            return list.Distinct().ToArray();
        }

        static float CalcBadgesHeight(IReadOnlyList<string> labels, float width, float minPill = 60f)
        {
            if (labels == null || labels.Count == 0) return 18f;
            float h = 18f, x = 0f, pad = 4f;
            foreach (var s in labels)
            {
                var w = Mathf.Clamp(GUI.skin.label.CalcSize(new GUIContent(s)).x + 16f, minPill, width);
                if (x + w > width)
                {
                    h += 20f;
                    x = 0f;
                }

                x += w + pad;
            }

            return h;
        }

        static void DrawContextPills(Rect area, Type[] required, IReadOnlyList<Type> providedTypes)
        {
            // pill UI currently disabled (reimplement based on ContextAsset if needed)
        }


        static Type ResolveTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            var t = Type.GetType(typeName, throwOnError: false);
            if (t != null) return t;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var tt = asm.GetType(typeName, false);
                if (tt != null) return tt;
            }

            return null;
        }

        static IReadOnlyList<Type> GetProvidedComponentTypesFromEntries(SerializedProperty compEntriesProp)
        {
            if (compEntriesProp == null || !compEntriesProp.isArray) return Array.Empty<Type>();

            var list = new List<Type>(compEntriesProp.arraySize);
            for (int i = 0; i < compEntriesProp.arraySize; i++)
            {
                var e = compEntriesProp.GetArrayElementAtIndex(i);
                var typeNameProp = e.FindPropertyRelative("typeName");
                var tn = typeNameProp != null ? typeNameProp.stringValue : null;
                var t = ResolveTypeName(tn);
                if (t != null) list.Add(t);
            }

            return list;
        }

        static IReadOnlyList<Type> GetProvidedComponentTypes(SerializedProperty componentsProp)
        {
            if (componentsProp == null || !componentsProp.isArray) return Array.Empty<Type>();
            var list = new List<Type>(componentsProp.arraySize);
            for (int i = 0; i < componentsProp.arraySize; i++)
            {
                var e = componentsProp.GetArrayElementAtIndex(i);
                var val = e.managedReferenceValue;
                if (val != null) list.Add(val.GetType());
            }

            return list;
        }

        static void PingTypeSource(Type t)
        {
            ZenAssetDatabase.PingMonoScript(t);
        }

        static GUIContent GetSearchIconContent(string tooltip)
        {
            return ZenGUIContents.IconPing(tooltip);
        }
    }
}
#endif
