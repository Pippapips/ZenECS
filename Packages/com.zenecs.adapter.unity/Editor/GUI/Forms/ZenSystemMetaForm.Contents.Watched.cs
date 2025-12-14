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
        private static void drawContentWatched(Type? t, float labelWidth = 70)
        {
            if (t == null) return;

            var watchedTypes = new List<Type>();
            try
            {
                var watchAttrs = t.GetCustomAttributes(typeof(ZenSystemWatchAttribute), false)
                    .Cast<ZenSystemWatchAttribute>();

                foreach (var wa in watchAttrs)
                {
                    var allOf = wa.AllOf;
                    if (allOf == null || allOf.Length == 0)
                        continue;

                    foreach (var compType in allOf)
                    {
                        if (compType != null)
                            watchedTypes.Add(compType);
                    }
                }
            }
            catch
            {
                // 구버전이나 리플렉션 실패는 조용히 무시
            }

            var watchedDistinct = watchedTypes
                .Where(x => x != null)
                .Distinct()
                .OrderBy(x => x.Name)
                .ToList();

            if (watchedDistinct.Count == 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Watched", ZenGUIStyles.LabelMLNormal10, GUILayout.Width(labelWidth));
                EditorGUILayout.LabelField("—", ZenGUIStyles.LabelMLNormal9);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("Watched", ZenGUIStyles.LabelMLNormal10, GUILayout.Width(labelWidth));

                EditorGUI.indentLevel++;

                foreach (var compType in watchedDistinct)
                {
                    if (compType == null) continue;

                    string cns = string.IsNullOrEmpty(compType.Namespace) ? "(global)" : compType.Namespace;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // 컴포넌트명
                        EditorGUILayout.LabelField($"{compType.Name} <size=9><color=#707070>[{cns}]</color></size>",
                            ZenGUIStyles.LabelMLNormal10);

                        // 돋보기 아이콘 (우측 끝)
                        var icon = ZenGUIContents.IconPing();
                        if (GUILayout.Button(icon, EditorStyles.iconButton, GUILayout.Width(18),
                                GUILayout.Height(16)))
                        {
                            // 선택은 유지하고 Ping만
                            ZenUtil.PingType(compType);
                        }
                    }
                }

                EditorGUI.indentLevel--;
            }
        }
    }
}
#endif