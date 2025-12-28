// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenEntityForm.Contents.Contexts.cs
// Purpose: Contexts section implementation for ZenEntityForm partial class,
//          providing UI for displaying and editing entity contexts.
// Key concepts:
//   • Context display: shows all IContext instances attached to an entity.
//   • Context editing: add/remove contexts via picker windows.
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
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Core;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Editor.GUIs
{
    public static partial class ZenEntityForm
    {
        
        private static void drawContextsMenus(IWorld w, Entity e, bool canEdit, ref EntityFoldoutInfo foldoutInfo)
        {
            var rects = new Rect[3];
            ZenGUIStyles.GetLeftIndentedSingleLineRects(20, 1, ref rects);
            using (new EditorGUI.DisabledScope(!canEdit))
            {
                if (GUI.Button(rects[0], ZenGUIContents.IconPlus(), ZenGUIStyles.ButtonPadding))
                {
                    var ctxs = w.GetAllContexts(e);
                    var disabledCtxTypes = new HashSet<Type>(ctxs.Select(c => c.type));

                    ZenContextAssetPickerWindow.Show(
                        activatorRectGui: rects[0],
                        onPick: asset =>
                        {
                            switch (asset)
                            {
                                case SharedContextAsset markerAsset:
                                {
                                    var resolver = ZenEcsUnityBridge.SharedContextResolver;
                                    if (resolver != null)
                                    {
                                        var ctx = resolver.Resolve(markerAsset);
                                        if (ctx != null)
                                            w.RegisterContext(e, ctx);
                                    }

                                    break;
                                }
                                case PerEntityContextAsset perEntityAsset:
                                {
                                    var ctx = perEntityAsset.Create();
                                    w.RegisterContext(e, ctx);
                                    break;
                                }
                            }
                        },
                        disabledContextTypes: disabledCtxTypes,
                        title: $"Entity #{e.Id}:{e.Gen} - Add Context");
                }
            }

            if (GUI.Button(rects[1], "▼", ZenGUIStyles.ButtonMCNormal10))
            {
                foldoutInfo.ExpandAll(EEntitySection.Contexts);
            }

            if (GUI.Button(rects[2], "▲", ZenGUIStyles.ButtonMCNormal10))
            {
                foldoutInfo.CollapseAll(EEntitySection.Contexts);
            }
        }

        private static void drawContextMenus(IWorld w, Entity e, Type t, bool canEdit, ref EntityFoldoutInfo foldoutInfo, object? ctx = null, bool indent = false)
        {
            if (indent) EditorGUI.indentLevel++;

            var rects = new Rect[2];
            ZenGUIStyles.GetLeftIndentedSingleLineRects(20, 1, ref rects);
            using (new EditorGUI.DisabledScope(!canEdit))
            {
                if (GUI.Button(rects[0], "X", ZenGUIStyles.ButtonMCNormal10))
                {
                    if (EditorUtility.DisplayDialog(
                            "Remove Context",
                            $"Remove this context?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}",
                            "Yes",
                            "No"))
                    {
                        if (ctx != null)
                        {
                            w.RemoveContext(e, (IContext)ctx);
                            foldoutInfo.RemoveFoldout(EEntitySection.Contexts, t);
                        }
                    }
                }
            }

            if (GUI.Button(rects[1], ZenGUIContents.IconPing(), EditorStyles.iconButton))
            {
                ZenUtil.PingType(t);
            }

            if (indent) EditorGUI.indentLevel--;
        }

        private static void drawContexts(IWorld w, Entity e, bool canEdit, ref EntityFoldoutInfo foldoutInfo)
        {
            var contents = w.GetAllContexts(e).ToArray();
            foreach (var (t, boxed) in contents)
            {
                var ctxType = t;
                var members = new List<(string name, Type type, Func<object?> getter)>();

                // Fields
                foreach (var f in ctxType.GetFields(BindingFlags.Public | BindingFlags.Instance |
                                                    BindingFlags.DeclaredOnly))
                {
                    if (Attribute.IsDefined(f, typeof(HideInInspector), true)) continue;

                    var lf = f;
                    members.Add((
                        lf.Name,
                        lf.FieldType,
                        () => lf.GetValue(boxed)
                    ));
                }

                // Properties
                foreach (var p in ctxType.GetProperties(BindingFlags.Public | BindingFlags.Instance |
                                                        BindingFlags.DeclaredOnly))
                {
                    if (!p.CanRead) continue;
                    if (p.GetIndexParameters().Length > 0) continue;
                    if (Attribute.IsDefined(p, typeof(HideInInspector), true)) continue;

                    var lp = p;
                    members.Add((
                        lp.Name,
                        lp.PropertyType,
                        () =>
                        {
                            try
                            {
                                return lp.GetValue(boxed);
                            }
                            catch
                            {
                                return null;
                            }
                        }
                    ));
                }

                var hasFields = members.Count > 0;
                var ns = string.IsNullOrEmpty(t.Namespace) ? "Global" : t.Namespace;
                var foldoutName = $"{t.Name} <size=9><color=#707070>[{ns}]</color></size>";

                if (!hasFields)
                {
                    EditorGUILayout.LabelField(foldoutName, ZenGUIStyles.LabelMLNormal10);
                    drawContextMenus(w, e, t, canEdit, ref foldoutInfo, boxed);
                }
                else
                {
                    var open = foldoutInfo.GetFoldout(EEntitySection.Contexts, t, false);
                    open = EditorGUILayout.Foldout(open, foldoutName, true, ZenGUIStyles.SystemFoldout10);
                    foldoutInfo.SetFoldout(EEntitySection.Contexts, t, open);

                    if (open)
                    {
                        drawContextContent(w, e, canEdit, members);
                    }

                    drawContextMenus(w, e, t, canEdit, ref foldoutInfo, boxed, true);
                }

                ZenGUIContents.DrawLine();

                GUILayout.Space(4);
            }
        }

        private static void drawContextContent(IWorld w, Entity e, bool canEdit, List<(string name, Type type, Func<object?> getter)> members)
        {
            var prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            using (new ZenGUIStyles.LabelScope(ZenGUIStyles.LabelMLNormal10, 300))
            {
                #region CONTENTS

                foreach (var (ctxName, mType, getter) in members)
                {
                    object? value = null;
                    try
                    {
                        value = getter();
                    }
                    catch
                    {
                        /* ignore */
                    }

                    var rect = GUILayoutUtility.GetRect(
                        0,
                        EditorGUIUtility.singleLineHeight,
                        GUILayout.ExpandWidth(true));

                    var labelRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
                    var valRect = new Rect(
                        rect.x + EditorGUIUtility.labelWidth,
                        rect.y,
                        rect.width - EditorGUIUtility.labelWidth,
                        rect.height);

                    EditorGUI.LabelField(labelRect, ctxName);

                    if (typeof(UnityEngine.Object).IsAssignableFrom(mType))
                    {
                        var obj = value as UnityEngine.Object;
                        var content = EditorGUIUtility.ObjectContent(obj, mType);

                        // Link cursor
                        EditorGUIUtility.AddCursorRect(valRect, MouseCursor.Link);

                        // Draw as Button instead of LabelField here = hover color applied
                        if (GUI.Button(valRect, content, ZenGUIStyles.LinkLabel))
                        {
                            if (obj != null)
                            {
                                Selection.activeObject = obj;
                                EditorGUIUtility.PingObject(obj);
                            }
                        }
                    }
                    else
                    {
                        var text = value != null ? (value.ToString() ?? "null") : "null";
                        EditorGUI.LabelField(valRect, text);
                    }
                }

                #endregion
            }

            EditorGUI.indentLevel = prevIndent;
        }
    }
}
#endif