#if UNITY_EDITOR
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
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Editor.GUIs
{
    public static partial class ZenEntityForm
    {
        private static void drawBindersMenus(IWorld w, Entity e, bool canEdit, ref EntityFoldoutInfo foldoutInfo)
        {
            var rects = new Rect[3];
            ZenGUIStyles.GetLeftIndentedSingleLineRects(20, 1, ref rects);
            using (new EditorGUI.DisabledScope(!canEdit))
            {
                if (GUI.Button(rects[0], ZenGUIContents.IconPlus(), ZenGUIStyles.ButtonPadding))
                {
                    var allBinders = ZenUtil.BinderTypeFinder.All();
                    var disabledB = new HashSet<Type>(w.GetAllBinders(e).Select(x => x.type));

                    ZenBinderPickerWindow.Show(
                        allBinderTypes: allBinders,
                        disabled: disabledB,
                        onPick: picked =>
                        {
                            var inst = ZenDefaults.CreateWithDefaults(picked);
                            if (inst != null) w.AttachBinder(e, (IBinder)inst);
                        },
                        activatorRectGui: rects[0],
                        title: $"Entity #{e.Id}:{e.Gen} - Add Binder");
                }
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

        private static void drawBinderMenus(IWorld w, Entity e, Type t, bool canEdit, ref EntityFoldoutInfo foldoutInfo, BaseBinder? binder = null, bool indent = false)
        {
            if (indent) EditorGUI.indentLevel++;

            var rects = new Rect[3];
            ZenGUIStyles.GetLeftIndentedSingleLineRects(20, 1, ref rects);
            using (new EditorGUI.DisabledScope(!canEdit))
            {
                if (GUI.Button(rects[0], "X", ZenGUIStyles.ButtonMCNormal10))
                {
                    if (EditorUtility.DisplayDialog(
                            "Remove Binder",
                            $"Remove this binder?\n\nEntity #{e.Id}:{e.Gen} - {t.Name}",
                            "Yes",
                            "No"))
                    {
                        w.DetachBinder(e, t);
                        foldoutInfo.RemoveFoldout(EEntitySection.Binders, t);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(!canEdit))
            {
                var isDisabled = binder is { Enabled: false };
                var prevBodyColor = GUI.color;
                if (isDisabled) GUI.color = new Color(0.3f, 0.9f, 1.0f);

                if (GUI.Button(rects[1], ZenGUIContents.IconPause(), ZenGUIStyles.ButtonPadding))
                {
                    if (binder != null)
                    {
                        binder.Enabled = !binder.Enabled;
                    }
                }

                GUI.color = prevBodyColor;
            }

            if (GUI.Button(rects[2], ZenGUIContents.IconPing(), EditorStyles.iconButton))
            {
                ZenUtil.PingType(t);
            }

            if (indent) EditorGUI.indentLevel--;
        }

        private static void drawBinders(IWorld w, Entity e, bool canEdit, ref EntityFoldoutInfo foldoutInfo)
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
                    drawBinderMenus(w, e, t, canEdit, ref foldoutInfo, boxed as BaseBinder);
                }
                else
                {
                    var open = foldoutInfo.GetFoldout(EEntitySection.Contexts, t, false);
                    open = EditorGUILayout.Foldout(open, foldoutName, true, ZenGUIStyles.SystemFoldout10);
                    foldoutInfo.SetFoldout(EEntitySection.Contexts, t, open);

                    if (open)
                    {
                        drawBinderContent(w, e, t, boxed, canEdit);
                    }

                    drawBinderMenus(w, e, t, canEdit, ref foldoutInfo, boxed as BaseBinder, true);
                }

                ZenGUIContents.DrawLine();

                GUILayout.Space(4);
            }
        }

        private static void drawBinderContent(IWorld w, Entity e, Type t, object boxed, bool canEdit)
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