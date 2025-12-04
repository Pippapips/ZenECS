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
        private static void drawContents(EEntitySection section, IWorld w, Entity e, ref EntityFoldoutInfo foldoutInfo)
        {
            int prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            drawContentsMenus(section, w, e, ref foldoutInfo);

            if (foldoutInfo.GetSectionFoldout(section))
            {
                switch (section)
                {
                    case EEntitySection.Components:
                        drawComponents(w, e, ref foldoutInfo);
                        break;
                    case EEntitySection.Contexts:
                        drawContexts(w, e, ref foldoutInfo);
                        break;
                    case EEntitySection.Binders:
                        drawBinders(w, e, ref foldoutInfo);
                        break;
                }
            }

            EditorGUI.indentLevel = prevIndent;
        }

        private static void drawContentsMenus(EEntitySection section, IWorld w, Entity e, ref EntityFoldoutInfo foldoutInfo)
        {
            switch (section)
            {
                case EEntitySection.Components:
                    drawComponentsMenus(w, e, ref foldoutInfo);
                    break;
                case EEntitySection.Contexts:
                    drawContextsMenus(w, e, ref foldoutInfo);
                    break;
                case EEntitySection.Binders:
                    drawBindersMenus(w, e, ref foldoutInfo);
                    break;
            }
        }
    }
}
#endif