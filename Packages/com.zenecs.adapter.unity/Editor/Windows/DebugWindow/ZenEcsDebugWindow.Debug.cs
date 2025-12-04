#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Adapter.Unity.Editor.GUIs;
using ZenECS.Core;
using ZenECS.Core.Binding;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.Editor.Windows
{
    public sealed partial class ZenEcsDebugWindow
    {
        private Vector2 _debugScroll;

        Dictionary<Entity, ZenEntityForm.EntityFoldoutInfo> _debugEntityFoldoutInfos = new();

        void DrawRightDebug()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (var sv = new EditorGUILayout.ScrollViewScope(_debugScroll))
                {
                    _debugScroll = sv.scrollPosition;

                    EditorGUILayout.Space(4);

                    using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                    {
                        // 상단 Close 버튼
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button(ZenStringTable.BtnClose, GUILayout.Width(80)))
                            {
                                return;
                            }
                        }

                        // 결과 표시
                        //DrawDebugResult();

                        for (int i = 0; i < 2; i++)
                        {
                            var e = new Entity(i + 1, 0);

                            if (!_debugEntityFoldoutInfos.TryGetValue(e, out var foldoutInfo))
                            {
                                foldoutInfo = new ZenEntityForm.EntityFoldoutInfo();
                                _debugEntityFoldoutInfos.Add(e, foldoutInfo);
                            }

                            if (_world != null)
                            {
                                ZenEntityForm.DrawEntity(_world, e, ref foldoutInfo);
                            }
                        }
                    }
                }
            }
        }
    }
}