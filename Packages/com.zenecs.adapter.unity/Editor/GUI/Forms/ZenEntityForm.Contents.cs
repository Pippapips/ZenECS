// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenEntityForm.Contents.cs
// Purpose: Content rendering methods for ZenEntityForm partial class, providing
//          UI for displaying entity information and section headers.
// Key concepts:
//   • Entity display: shows entity ID, generation, and section tabs.
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
        private static void drawContents(EEntitySection section, IWorld w, Entity e, bool canEdit, ref EntityFoldoutInfo foldoutInfo)
        {
            int prevIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            drawContentsMenus(section, w, e, canEdit, ref foldoutInfo);

            if (foldoutInfo.GetSectionFoldout(section))
            {
                switch (section)
                {
                    case EEntitySection.Components:
                        drawComponents(w, e, canEdit, ref foldoutInfo);
                        break;
                    case EEntitySection.Contexts:
                        drawContexts(w, e, canEdit, ref foldoutInfo);
                        break;
                    case EEntitySection.Binders:
                        drawBinders(w, e, canEdit, ref foldoutInfo);
                        break;
                }
            }

            EditorGUI.indentLevel = prevIndent;
        }

        private static void drawContentsMenus(EEntitySection section, IWorld w, Entity e, bool canEdit, ref EntityFoldoutInfo foldoutInfo)
        {
            switch (section)
            {
                case EEntitySection.Components:
                    if (!w.HasSingleton(e)) drawComponentsMenus(w, e, canEdit, ref foldoutInfo);
                    break;
                case EEntitySection.Contexts:
                    drawContextsMenus(w, e, canEdit, ref foldoutInfo);
                    break;
                case EEntitySection.Binders:
                    drawBindersMenus(w, e, canEdit, ref foldoutInfo);
                    break;
            }
        }
    }
}
#endif