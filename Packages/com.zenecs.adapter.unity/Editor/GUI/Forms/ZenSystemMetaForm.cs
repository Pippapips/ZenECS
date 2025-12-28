// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenSystemMetaForm.cs
// Purpose: Partial class that provides system metadata editing UI forms for
//          the ZenECS Explorer window, including group, execution order, and
//          watch attributes.
// Key concepts:
//   • System metadata: group, execution order, watch attributes editing.
//   • Partial class: split across multiple files for organization.
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
using Codice.Client.Common.GameUI;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.Editor.GUIs
{
    public static partial class ZenSystemMetaForm
    {
        public static void DrawSystemMeta(ISystem? sys, float labelWidth = 70)
        {
            if (sys == null) return;
            
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                drawContents(sys.GetType(), labelWidth);
            }
        }
    }
}
#endif