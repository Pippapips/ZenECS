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
        private static void drawContentGroupExec(Type? t, float labelWidth = 70)
        {
            if (t == null) return;
            
            // 그룹 & Phase (Fixed/Variable/Presentation)
            ZenUtil.ResolveSystemGroupAndPhase(t, out var group, out var phase);

            var groupLabel = group.ToString();

            // Execution Group + 대표 인터페이스
            var execLabel = "Unknown";

            if (group is SystemGroup.FrameInput or SystemGroup.FrameSync or SystemGroup.FrameView or SystemGroup.FrameUI)
            {
                execLabel = "Non-deterministic";
            }
            else if (group is SystemGroup.FixedInput or SystemGroup.FixedDecision or SystemGroup.FixedSimulation or SystemGroup.FixedPost)
            {
                execLabel = "Deterministic";
            }
            
            // 상단: System 이름 + Namespace + Ping 아이콘
            var ns = string.IsNullOrEmpty(t.Namespace) ? "(global)" : t.Namespace;

            using (new EditorGUILayout.HorizontalScope())
            {
                // 이름 + 네임스페이스를 한 덩어리로 왼쪽에 붙여서 표시
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField(t.Name, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
                    EditorGUILayout.LabelField($"[{ns}]", ZenGUIStyles.LabelMLNormal9Gray);
                }

                var line = EditorGUIUtility.singleLineHeight;
                var r = GUILayoutUtility.GetRect(10, line, GUILayout.ExpandWidth(true));
                var marginRight = new Rect(r.xMax - 20, r.y, 20, r.height);
                
                var searchContent = ZenGUIContents.IconPing();
                if (GUI.Button(marginRight, searchContent, EditorStyles.iconButton))
                {
                    ZenUtil.PingType(t);
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Group", ZenGUIStyles.LabelMLNormal10, GUILayout.Width(labelWidth));
            EditorGUILayout.LabelField(groupLabel, ZenGUIStyles.LabelMLNormal9);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Execution", ZenGUIStyles.LabelMLNormal10, GUILayout.Width(labelWidth));
            EditorGUILayout.LabelField(execLabel, ZenGUIStyles.LabelMLNormal9);
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif