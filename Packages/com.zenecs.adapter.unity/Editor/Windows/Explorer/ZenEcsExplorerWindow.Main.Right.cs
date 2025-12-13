#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Adapter.Unity.Editor.GUIs;
using ZenECS.Core;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.Editor.Windows
{
    public sealed partial class ZenEcsExplorerWindow
    {
        /// <summary>
        /// (Entity, Type) 조합을 키로 쓰기 위한 구조체.
        /// 기존 string 기반 키("{id}:{gen}:{type}") 대신 사용해서
        /// 매 프레임 string 할당을 제거하고 Dictionary 조회 비용을 줄인다.
        /// </summary>
        readonly struct EntityTypeKey : IEquatable<EntityTypeKey>
        {
            public readonly int Id;
            public readonly int Gen;
            public readonly Type Type;

            public EntityTypeKey(Entity entity, Type type)
            {
                Id = entity.Id;
                Gen = entity.Gen;
                Type = type;
            }

            public bool Equals(EntityTypeKey other)
                => Id == other.Id && Gen == other.Gen && Type == other.Type;

            public override bool Equals(object? obj)
                => obj is EntityTypeKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Id;
                    hashCode = (hashCode * 397) ^ Gen;
                    hashCode = (hashCode * 397) ^ (Type != null ? Type.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }

        /// <summary>
        /// ViewModel for the right entity/singleton panel.
        /// </summary>
        [Serializable]
        sealed class ExplorerEntityPanelState
        {
            // 스크롤
            public Vector2 Scroll;

            public Dictionary<Entity, ZenEntityForm.EntityFoldoutInfo> EntityFoldoutInfos = new();
            
            // Foldouts per entity / context
            public readonly Dictionary<Entity, bool> EntityFold = new();
            public readonly Dictionary<EntityTypeKey, bool> BinderFold = new();
            public readonly Dictionary<EntityTypeKey, bool> ComponentFold = new();
            public readonly Dictionary<EntityTypeKey, bool> ContextFold = new();

            // Watched foldouts (key: system name or other key)
            public readonly Dictionary<string, bool> WatchedFold = new();

            // Singleton selection
            public bool HasSelectedSingleton;
            public Entity SelectedSingletonEntity;
            public Type? SelectedSingletonType;

            /// <summary>
            /// Clear all entity-related foldouts.
            /// </summary>
            public void ClearEntityView()
            {
                EntityFoldoutInfos.Clear();
                EntityFold.Clear();
                BinderFold.Clear();
                ComponentFold.Clear();
                ContextFold.Clear();
                WatchedFold.Clear();
            }

            /// <summary>
            /// Clear singleton selection.
            /// </summary>
            public void ClearSingletonSelection()
            {
                HasSelectedSingleton = false;
                SelectedSingletonEntity = default;
                SelectedSingletonType = null;
            }
            
            /// <summary>
            /// Clear selection for the entity panel (entities + singleton).
            /// </summary>
            public void ClearSelection()
            {
                ClearSingletonSelection();
                ClearEntityView();
            }
        }

        readonly ExplorerEntityPanelState _entityPanel = new();
        
        private void DrawRightEntityPanel()
        {
            if (_world == null) return;
            
            using var sv = new EditorGUILayout.ScrollViewScope(_entityPanel.Scroll);
            _entityPanel.Scroll = sv.scrollPosition;

            EditorGUILayout.Space(4);

            var systems = _world.GetAllSystems();
            bool hasSystem = systems.Count > 0 &&
                             _systemTree.SelectedSystemIndex >= 0 &&
                             _systemTree.SelectedSystemIndex < (systems?.Count ?? 0);

            bool hasSingleton = false;
            if (_entityPanel.HasSelectedSingleton)
            {
                hasSingleton = _world.IsAlive(_entityPanel.SelectedSingletonEntity);
                if (!hasSingleton)
                {
                    _entityPanel.ClearSingletonSelection();
                }
            }
            
            if (!hasSystem && !hasSingleton)
            {
                // 시스템/싱글톤 선택 없음 → 안내 메시지만
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    EditorGUILayout.HelpBox(
                        "Select a system or singleton from the left panel.\n" +
                        "The System Meta or Singleton Entity will be shown here.",
                        MessageType.Info);

                    GUILayout.FlexibleSpace();
                }

                GUILayout.FlexibleSpace();
            }
            else if (hasSingleton && !hasSystem)
            {
                // ===== 싱글톤 선택 모드 =====
                //DrawSingletonDetail(_world, _entityPanel.SelectedSingletonType, _entityPanel.SelectedSingletonEntity);
                //ZenEntityForm.DrawEntity(_world, _entityPanel.SelectedSingletonEntity);
                var e = _entityPanel.SelectedSingletonEntity;
                if (_world.IsAlive(e))
                {
                    if (!_entityPanel.EntityFoldoutInfos.TryGetValue(e, out var foldoutInfo))
                    {
                        foldoutInfo = new ZenEntityForm.EntityFoldoutInfo();
                        foldoutInfo.ExpandAll();
                        _entityPanel.EntityFoldoutInfos.Add(e, foldoutInfo);
                    }

                    ZenEntityForm.DrawEntity(_world, e, _coreState.EditMode, ref foldoutInfo);
                }
            }
            else
            {
                // =========================
                // 정상 리스트 모드 (시스템 선택 있음)
                // =========================

                var sys = systems![_systemTree.SelectedSystemIndex];

                // 🔹 1) System Meta 박스
                DrawSystemMeta(sys, _world);

                EditorGUILayout.Space(6);

                // 🔹 2) Entities 헤더 + 리스트
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (_systemTree.SelectedSystemEntityCount > 0)
                        EditorGUILayout.LabelField($"Entities ({_systemTree.SelectedSystemEntityCount})", EditorStyles.boldLabel);
                    else
                        EditorGUILayout.LabelField("Entities", EditorStyles.boldLabel);

                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.Space(4);

                var tmp = new List<Entity>();
                if (!TryCollectEntitiesBySystemWatched(_world, sys, tmp))
                {
                    EditorGUILayout.HelpBox(
                        "No inspector. Implement IInspectableSystem or add [Watch].",
                        MessageType.Info);
                }

                _systemTree.SelectedSystemEntityCount = tmp.Count;

                foreach (var e in tmp.Distinct())
                    if (_world != null)
                    {
                        if (_world.IsAlive(e))
                        {
                            if (!_entityPanel.EntityFoldoutInfos.TryGetValue(e, out var foldoutInfo))
                            {
                                foldoutInfo = new ZenEntityForm.EntityFoldoutInfo();
                                _entityPanel.EntityFoldoutInfos.Add(e, foldoutInfo);
                            }

                            ZenEntityForm.DrawEntity(_world, e, _coreState.EditMode, ref foldoutInfo);
                        }
                    }
            }
        }
        
        void DrawSystemMeta(ISystem? sys, IWorld? world)
        {
            if (sys == null) return;

            var t = sys.GetType();

            // 그룹 & Phase (Fixed/Variable/Presentation)
            ResolveGroupAndPhase(t, out var group, out var phase);

            string groupLabel = group switch
            {
                SystemGroup.Unknown => "Unknown",
                SystemGroup.FrameInput => "Frame Input",
                SystemGroup.FrameSync => "Frame Sync",
                SystemGroup.FrameView => "Frame View",
                SystemGroup.FrameUI => "Frame UI",
                SystemGroup.FixedInput => "Fixed Input",
                SystemGroup.FixedDecision => "Fixed Decision",
                SystemGroup.FixedSimulation => "Fixed Simulation",
                SystemGroup.FixedPost => "Fixed Post",
                _ => group.ToString()
            };

            // Execution Group + 대표 인터페이스
            string execLabel = "Unknown";

            if (group == SystemGroup.FrameInput ||
                group == SystemGroup.FrameSync ||
                group == SystemGroup.FrameView ||
                group == SystemGroup.FrameUI)
            {
                execLabel = "Non-deterministic";
            }
            else if (group == SystemGroup.FixedInput ||
                     group == SystemGroup.FixedDecision ||
                     group == SystemGroup.FixedSimulation ||
                     group == SystemGroup.FixedPost)
            {
                execLabel = "Deterministic";
            }

            // Order Before/After (Attribute 기반)
            var beforeList = new List<string>();
            var afterList = new List<string>();

            try
            {
                var beforeAttrs = t.GetCustomAttributes(typeof(OrderBeforeAttribute), true)
                    .Cast<OrderBeforeAttribute>();
                foreach (var a in beforeAttrs)
                {
                    var target = a.Target;
                    if (target != null)
                        beforeList.Add(target.Name);
                }

                var afterAttrs = t.GetCustomAttributes(typeof(OrderAfterAttribute), true)
                    .Cast<OrderAfterAttribute>();
                foreach (var a in afterAttrs)
                {
                    var target = a.Target;
                    if (target != null)
                        afterList.Add(target.Name);
                }
            }
            catch
            {
                // 구버전에서 타입이 다를 수 있으니 조용히 무시
            }

            string beforeText = beforeList.Count > 0
                ? string.Join(", ", beforeList.Distinct())
                : "—";

            string afterText = afterList.Count > 0
                ? string.Join(", ", afterList.Distinct())
                : "—";

            // ZenSystemWatchAttribute.AllOf 기반으로 Watched Components 추출
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

            string watchedText = watchedDistinct.Count > 0
                ? string.Join(", ", watchedDistinct.Select(x => x.Name))
                : "—";

            // // 선택된 시스템에 대해 현재 Watched 엔티티 수 (있으면 메타에 추가)
            // int watchedCount = 0;
            // if (world != null)
            // {
            //     try
            //     {
            //         var tmp = new List<Entity>();
            //         if (ZenECS.Adapter.Unity.Infrastructure.WatchQueryRunner.TryCollectByWatch(sys, world, tmp))
            //         {
            //             watchedCount = tmp.Count;
            //         }
            //     }
            //     catch
            //     {
            //         // 필요하면 로그, 지금은 조용히 무시
            //     }
            //}

            // 네임스페이스용 회색 스타일
            var nsStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal =
                {
                    textColor = EditorGUIUtility.isProSkin
                        ? new Color(0.5f, 0.5f, 0.5f)
                        : new Color(0.4f, 0.4f, 0.4f)
                },
                fontSize = 10,
            };

            using (new EditorGUILayout.VerticalScope("box"))
            {
                // 상단: System 이름 + Namespace + Ping 아이콘
                string ns = string.IsNullOrEmpty(t.Namespace) ? "(global)" : t.Namespace;

                using (new EditorGUILayout.HorizontalScope())
                {
                    // 이름 + 네임스페이스를 한 덩어리로 왼쪽에 붙여서 표시
                    using (new EditorGUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField(t.Name, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
                        EditorGUILayout.LabelField($"[{ns}]", nsStyle);
                    }

                    //GUILayout.FlexibleSpace();
                    

                    var line = EditorGUIUtility.singleLineHeight;
                    var r = GUILayoutUtility.GetRect(10, line, GUILayout.ExpandWidth(true));
                    var marginRight = new Rect(r.xMax - 20, r.y, 20, r.height);
                    
                    var searchContent = ZenGUIContents.IconPing();
                    if (GUI.Button(marginRight, searchContent, EditorStyles.iconButton))
                    {
                        PingSystemType(t);
                    }
                }

                EditorGUILayout.Space(2);

                // 조금 눈에 잘 들어오도록 라벨 스타일 준비
                var leftLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontStyle = FontStyle.Bold,
                    normal =
                    {
                        textColor = systemMetaTextColor
                    },
                };
                var valueStyle = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    fontSize = 9,
                    padding = new RectOffset(0, 0, 4, 0),
                };

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Group", leftLabelStyle, GUILayout.Width(70));
                EditorGUILayout.LabelField(groupLabel, valueStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Execution", leftLabelStyle, GUILayout.Width(70));
                EditorGUILayout.LabelField(execLabel, valueStyle);
                EditorGUILayout.EndHorizontal();

                // Order (Before/After)
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Order Before", leftLabelStyle, GUILayout.Width(70));
                EditorGUILayout.LabelField(beforeText, valueStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Order After", leftLabelStyle, GUILayout.Width(70));
                EditorGUILayout.LabelField(afterText, valueStyle);
                EditorGUILayout.EndHorizontal();

                // ==== Watched Components Foldout ====

                var leftFoldoutStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 10,
                };

                leftFoldoutStyle.focused.textColor = systemMetaTextColor;
                leftFoldoutStyle.onFocused.textColor = systemMetaTextColor;
                leftFoldoutStyle.hover.textColor = systemMetaTextColor;
                leftFoldoutStyle.onHover.textColor = systemMetaTextColor;
                leftFoldoutStyle.active.textColor = systemMetaTextColor;
                leftFoldoutStyle.onActive.textColor = systemMetaTextColor;
                leftFoldoutStyle.normal.textColor = systemMetaTextColor;
                leftFoldoutStyle.onNormal.textColor = systemMetaTextColor;

                // Watched 대상이 하나도 없으면 간단히 표시만
                if (watchedDistinct.Count == 0)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Watched", leftLabelStyle, GUILayout.Width(70));
                        EditorGUILayout.LabelField("—", valueStyle);
                        EditorGUILayout.EndHorizontal();
                        // if (watchedCount > 0)
                        // {
                        //     GUILayout.FlexibleSpace();
                        //     EditorGUILayout.LabelField($"Watched Entities: {watchedCount}", EditorStyles.miniLabel,
                        //         GUILayout.MaxWidth(160));
                        // }
                    }
                }
                else
                {
                    // 시스템 타입별로 Foldout 상태를 저장
                    var key = t.FullName ?? t.Name;
                    if (!_entityPanel.WatchedFold.TryGetValue(key, out var open))
                        open = false;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // Foldout: "Watched (N)"
                        open = EditorGUILayout.Foldout(
                            open,
                            $"Watched ({watchedDistinct.Count})",
                            true,
                            leftFoldoutStyle
                        );
                        _entityPanel.WatchedFold[key] = open;

                        GUILayout.FlexibleSpace();

                        // if (watchedCount > 0)
                        // {
                        //     EditorGUILayout.LabelField($"Watched Entities: {watchedCount}",
                        //         EditorStyles.miniLabel, GUILayout.MaxWidth(160));
                        // }
                    }

                    if (open)
                    {
                        EditorGUI.indentLevel++;

                        var componentStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            wordWrap = true,
                            fontSize = 10,
                            padding = new RectOffset(0, 0, 4, 0),
                            richText = true,
                            normal =
                            {
                                textColor = systemMetaTextColor
                            }
                        };

                        foreach (var compType in watchedDistinct)
                        {
                            if (compType == null) continue;

                            string cns = string.IsNullOrEmpty(compType.Namespace) ? "(global)" : compType.Namespace;

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                // 컴포넌트명
                                EditorGUILayout.LabelField($"{compType.Name} <color=#707070>[{cns}]</color>",
                                    componentStyle);

                                // // 네임스페이스 [Namespace] (회색)
                                // EditorGUILayout.LabelField($"[{cns}]", nsStyle);

                                // 돋보기 아이콘 (우측 끝)
                                var icon = ZenGUIContents.IconPing();
                                if (GUILayout.Button(icon, EditorStyles.iconButton, GUILayout.Width(18),
                                        GUILayout.Height(16)))
                                {
                                    // 선택은 유지하고 Ping만
                                    PingComponentType(compType);
                                }
                            }
                        }

                        EditorGUI.indentLevel--;
                    }
                }
            }
        }
    }
}