// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenEntityForm.Contents.Components.cs
// Purpose: Components section implementation for ZenEntityForm partial class,
//          providing UI for displaying and editing entity components.
// Key concepts:
//   • Component display: shows all components attached to an entity.
//   • Component editing: add/remove components via picker windows.
//   • Partial class: part of ZenEntityForm split across multiple files.
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
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Editor.GUIs
{
    public static partial class ZenEntityForm
    {
        private static void drawComponentsMenus(IWorld w, Entity e, bool canEdit, ref EntityFoldoutInfo foldoutInfo)
        {
            var rects = new Rect[3];
            ZenGUIStyles.GetLeftIndentedSingleLineRects(20, 1, ref rects);
            using (new EditorGUI.DisabledScope(!canEdit))
            {
                if (GUI.Button(rects[0], ZenGUIContents.IconPlus(), ZenGUIStyles.ButtonPadding))
                {
                    var allTypes = ZenComponentPickerWindow.FindAllZenComponents().ToList();
                    var disabled = new HashSet<Type>();
                    foreach (var (tHave, _) in w.GetAllComponents(e))
                        disabled.Add(tHave);

                    ZenComponentPickerWindow.Show(
                        allTypes,
                        disabled,
                        picked =>
                        {
                            var inst = ZenDefaults.CreateWithDefaults(picked);
                            if (inst != null)
                            {
                                w.ExternalCommandEnqueue(ExternalCommand.AddComponent(e, inst.GetType(), inst));
                            }
                        },
                        rects[0],
                        ZenStringTable.GetAddComponent(e));
                }
            }

            if (GUI.Button(rects[1], "▼", ZenGUIStyles.ButtonMCNormal10))
            {
                foldoutInfo.ExpandAll(EEntitySection.Components);
            }

            if (GUI.Button(rects[2], "▲", ZenGUIStyles.ButtonMCNormal10))
            {
                foldoutInfo.CollapseAll(EEntitySection.Components);
            }
        }

        private static void drawComponentMenus(IWorld w, Entity e, Type t, bool canEdit, ref EntityFoldoutInfo foldoutInfo,
            bool indent = false)
        {
            if (indent) EditorGUI.indentLevel++;

            var rects = new Rect[3];
            ZenGUIStyles.GetLeftIndentedSingleLineRects(20, 1, ref rects);

            if (w.HasSingleton(e))
            {
                if (GUI.Button(rects[0], ZenGUIContents.IconPing(), EditorStyles.iconButton))
                {
                    ZenUtil.PingType(t);
                }
            }
            else
            {
                using (new EditorGUI.DisabledScope(!canEdit))
                {
                    if (GUI.Button(rects[0], "X", ZenGUIStyles.ButtonMCNormal10))
                    {
                        if (EditorUtility.DisplayDialog(
                                "Remove Component",
                                $"Remove this component?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}",
                                "Yes",
                                "No"))
                        {
                            w.ExternalCommandEnqueue(ExternalCommand.RemoveComponent(e, t));
                            foldoutInfo.RemoveFoldout(EEntitySection.Components, t);
                        }
                    }

                    if (GUI.Button(rects[1], "R", ZenGUIStyles.ButtonMCNormal10))
                    {
                        var instance = ZenDefaults.CreateWithDefaults(t);
                        if (instance != null)
                        {
                            w.ExternalCommandEnqueue(ExternalCommand.ReplaceComponent(e, t, instance));
                        }
                    }
                }

                if (GUI.Button(rects[2], ZenGUIContents.IconPing(), EditorStyles.iconButton))
                {
                    ZenUtil.PingType(t);
                }
            }

            if (indent) EditorGUI.indentLevel--;
        }

        private static void drawComponents(IWorld w, Entity e, bool canEdit, ref EntityFoldoutInfo foldoutInfo)
        {
            var contents = w.GetAllComponents(e).ToArray();
            foreach (var (t, boxed) in contents)
            {
                var hasFields = ZenComponentFormGUI.HasDrawableFields(t);
                var ns = string.IsNullOrEmpty(t.Namespace) ? "Global" : t.Namespace;
                var foldoutName = $"{t.Name} <size=9><color=#707070>[{ns}]</color></size>";

                if (!hasFields || boxed == null)
                {
                    EditorGUILayout.LabelField(foldoutName, ZenGUIStyles.LabelMLNormal10);
                    drawComponentMenus(w, e, t, canEdit, ref foldoutInfo);
                }
                else
                {
                    var open = foldoutInfo.GetFoldout(EEntitySection.Components, t, false);
                    open = EditorGUILayout.Foldout(open, foldoutName, true, ZenGUIStyles.SystemFoldout10);
                    foldoutInfo.SetFoldout(EEntitySection.Components, t, open);

                    if (open)
                    {
                        drawComponentContent(w, e, t, boxed, canEdit);
                    }
                    
                    drawComponentMenus(w, e, t, canEdit, ref foldoutInfo, true);
                }

                ZenGUIContents.DrawLine();

                GUILayout.Space(4);
            }
        }

        private static void drawComponentContent(IWorld w, Entity e, Type t, object boxed, bool canEdit)
        {
            var prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            using (new EditorGUI.DisabledScope(!canEdit))
            {
                using (new ZenGUIStyles.LabelScope(ZenGUIStyles.LabelMLNormal10, 300))
                {
                    try
                    {
                        object obj = CopyBox(boxed, t);
                        float bodyH = ZenComponentFormGUI.CalcHeightForObject(obj, t);
                        bodyH = Mathf.Max(bodyH, EditorGUIUtility.singleLineHeight + 6f);

                        var body = GUILayoutUtility.GetRect(0, bodyH, GUILayout.ExpandWidth(true));
                        var bodyInner = new Rect(body.x, body.y, body.width, body.height + 4f);

                        EditorGUI.BeginChangeCheck();
                        ZenComponentFormGUI.DrawSmallForm(bodyInner, obj, t);
                        if (EditorGUI.EndChangeCheck())
                        {
                            w.ExternalCommandEnqueue(ExternalCommand.ReplaceComponent(e, t, obj));
                        }
                    }
                    catch (KeyNotFoundException)
                    {
                        // Ignore if component type is not in registry
                    }
                }
            }

            EditorGUI.indentLevel = prevIndent;
        }
        
        private static object CopyBox(object? src, Type t)
        {
            if (src == null) return SafeNew.New(t);
            if (t.IsValueType) return src;
            var dst = SafeNew.New(t);
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
                f.SetValue(dst, f.GetValue(src));
            return dst;
        }
        private static class SafeNew
        {
            public static object New(Type t)
            {
                if (t.IsValueType) return Activator.CreateInstance(t);
                var ctor = t.GetConstructor(Type.EmptyTypes);
                if (ctor != null) return Activator.CreateInstance(t);
                return System.Runtime.Serialization.FormatterServices.GetUninitializedObject(t);
            }
        }
    }
}
#endif