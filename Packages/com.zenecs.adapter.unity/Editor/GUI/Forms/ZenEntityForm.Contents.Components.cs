#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Core;

#if UNITY_EDITOR
namespace ZenECS.Adapter.Unity.Editor.GUIs
{
    public static partial class ZenEntityForm
    {
        private static void drawComponentsMenus(IWorld w, Entity e, ref EntityFoldoutInfo foldoutInfo)
        {
            var rects = new Rect[3];
            ZenGUIStyles.GetLeftIndentedSingleLineRects(20, 1, ref rects);
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

            if (GUI.Button(rects[1], "▼", ZenGUIStyles.ButtonMCNormal10))
            {
                foldoutInfo.ExpandAll(EEntitySection.Components);
            }

            if (GUI.Button(rects[2], "▲", ZenGUIStyles.ButtonMCNormal10))
            {
                foldoutInfo.CollapseAll(EEntitySection.Components);
            }
        }

        private static void drawComponentMenus(IWorld w, Entity e, Type t, ref EntityFoldoutInfo foldoutInfo, bool indent = false)
        {
            if (w.HasSingleton(e)) return;
            
            if (indent) EditorGUI.indentLevel++;
            
            var rects = new Rect[3];
            ZenGUIStyles.GetLeftIndentedSingleLineRects(20, 1, ref rects);
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

            if (GUI.Button(rects[2], ZenGUIContents.IconPing(), ZenGUIStyles.ButtonPadding))
            {
                ZenUtil.PingType(t);
            }

            if (indent) EditorGUI.indentLevel--;
        }

        private static void drawComponents(IWorld w, Entity e, ref EntityFoldoutInfo foldoutInfo)
        {
            using (new EditorGUI.DisabledScope(false))
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
                        drawComponentMenus(w, e, t, ref foldoutInfo);
                    }
                    else
                    {
                        var open = foldoutInfo.GetFoldout(EEntitySection.Components, t, false);
                        open = EditorGUILayout.Foldout(open, foldoutName, true, ZenGUIStyles.SystemFoldout10);
                        foldoutInfo.SetFoldout(EEntitySection.Components, t, open);

                        if (open)
                        {
                            drawComponentContent(w, e, t, boxed);
                        }
                        
                        drawComponentMenus(w, e, t, ref foldoutInfo, true);
                    }

                    ZenGUIContents.DrawLine();

                    GUILayout.Space(4);
                }
            }
        }

        private static void drawComponentContent(IWorld w, Entity e, Type t, object boxed)
        {
            var prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

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
                    // 컴포넌트 타입이 레지스트리에 없는 경우는 무시
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