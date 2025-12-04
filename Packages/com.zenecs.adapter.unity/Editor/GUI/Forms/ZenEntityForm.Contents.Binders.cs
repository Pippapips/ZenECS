#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Codice.Client.Common.GameUI;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Core;

#if UNITY_EDITOR
namespace ZenECS.Adapter.Unity.Editor.GUIs
{
    public static partial class ZenEntityForm
    {
        private static void drawBindersMenus(IWorld w, Entity e, ref EntityFoldoutInfo foldoutInfo)
        {
            var rects = new Rect[3];
            ZenGUIStyles.GetLeftIndentedSingleLineRects(20, 1, ref rects);
            if (GUI.Button(rects[0], ZenGUIContents.IconPlus(), ZenGUIStyles.ButtonPadding))
            {
                Debug.Log("L0 clicked");
            }

            if (GUI.Button(rects[1], "▼", ZenGUIStyles.ButtonMCNormal10))
            {
                foldoutInfo.ExpandAll(EEntitySection.Binders);
            }

            if (GUI.Button(rects[2], "▲", ZenGUIStyles.ButtonMCNormal10))
            {
                foldoutInfo.CollapseAll(EEntitySection.Binders);
            }
        }

        private static void drawBinderMenus(IWorld w, Entity e, bool indent = false)
        {
            if (indent) EditorGUI.indentLevel++;

            var rects = new Rect[3];
            ZenGUIStyles.GetLeftIndentedSingleLineRects(20, 1, ref rects);
            if (GUI.Button(rects[0], "X", ZenGUIStyles.ButtonMCNormal10))
            {
                Debug.Log("L0 clicked");
            }

            if (GUI.Button(rects[1], "R", ZenGUIStyles.ButtonMCNormal10))
            {
                Debug.Log("L1 clicked");
            }

            if (GUI.Button(rects[2], ZenGUIContents.IconPing(), ZenGUIStyles.ButtonPadding))
            {
                Debug.Log("L2 clicked");
            }

            if (indent) EditorGUI.indentLevel--;
        }

        private static void drawBinders(IWorld w, Entity e, ref EntityFoldoutInfo foldoutInfo)
        {
            using (new EditorGUI.DisabledScope(false))
            {
                var contents = w.GetAllBinders(e).ToArray();
                foreach (var (t, boxed) in contents)
                {
                    var hasFields = ZenComponentFormGUI.HasDrawableFields(t);
                    var ns = string.IsNullOrEmpty(t.Namespace) ? "Global" : t.Namespace;
                    var foldoutName = $"{t.Name} <size=9><color=#707070>[{ns}]</color></size>";

                    if (!hasFields)
                    {
                        EditorGUILayout.LabelField(foldoutName, ZenGUIStyles.LabelMLNormal10);
                        drawBinderMenus(w, e);
                    }
                    else
                    {
                        var open = foldoutInfo.GetFoldout(EEntitySection.Contexts, t, false);
                        open = EditorGUILayout.Foldout(open, foldoutName, true, ZenGUIStyles.SystemFoldout10);
                        foldoutInfo.SetFoldout(EEntitySection.Contexts, t, open);

                        if (open)
                        {
                            drawBinderContent(w, e, t, boxed);
                        }

                        drawBinderMenus(w, e, true);
                    }

                    ZenGUIContents.DrawLine();

                    GUILayout.Space(4);
                }
            }
        }

        private static void drawBinderContent(IWorld w, Entity e, Type t, object boxed)
        {
            var prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            using (new ZenGUIStyles.LabelScope(ZenGUIStyles.LabelMLNormal10, 300))
            {
                #region CONTENTS

                // apply order
                // observing binds

                #endregion
            }

            EditorGUI.indentLevel = prevIndent;
        }
    }
}
#endif