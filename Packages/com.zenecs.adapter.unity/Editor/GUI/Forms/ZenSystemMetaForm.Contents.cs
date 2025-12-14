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
        private static void drawContents(Type? t, float labelWidth = 70)
        {
            if (t == null) return;

            drawContentGroupExec(t, labelWidth);
            drawContentOrder(t, labelWidth);
            drawContentWatched(t, labelWidth);
        }
    }
}
#endif