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
        public enum EEntitySection
        {
            Components = 0,
            Contexts,
            Binders,
        }

        public class EntityFoldoutInfo
        {
            public bool Open { get; set; }
            private readonly Dictionary<EEntitySection, bool> _sectionFoldouts = new();
            private readonly Dictionary<Type, bool> _componentFoldouts = new();
            private readonly Dictionary<Type, bool> _contextFoldouts = new();
            private readonly Dictionary<Type, bool> _binderFoldouts = new();

            #region expand / collapse
            public void ExpandAll()
            {
                Open = true;
                
                foreach (var key in _sectionFoldouts.Keys.ToList())
                {
                    _sectionFoldouts[key] = true;
                }
                
                ExpandAll(EEntitySection.Components);
                ExpandAll(EEntitySection.Contexts);
                ExpandAll(EEntitySection.Binders);
            }

            public void CollapseAll()
            {
                Open = false;
                
                foreach (var key in _sectionFoldouts.Keys.ToList())
                {
                    _sectionFoldouts[key] = false;
                }

                CollapseAll(EEntitySection.Components);
                CollapseAll(EEntitySection.Contexts);
                CollapseAll(EEntitySection.Binders);
            }

            public void ExpandAll(EEntitySection sectionType)
            {
                switch (sectionType)
                {
                    case EEntitySection.Components:
                    {
                        _sectionFoldouts[EEntitySection.Components] = true;
                        foreach (var key in _componentFoldouts.Keys.ToList())
                        {
                            _componentFoldouts[key] = true;
                        }
                        break;
                    }
                    case EEntitySection.Contexts:
                    {
                        _sectionFoldouts[EEntitySection.Contexts] = true;
                        foreach (var key in _contextFoldouts.Keys.ToList())
                        {
                            _contextFoldouts[key] = true;
                        }
                        break;
                    }
                    case EEntitySection.Binders:
                    {
                        _sectionFoldouts[EEntitySection.Binders] = true;
                        foreach (var key in _binderFoldouts.Keys.ToList())
                        {
                            _binderFoldouts[key] = true;
                        }
                        break;
                    }
                }
            }

            public void CollapseAll(EEntitySection sectionType)
            {
                switch (sectionType)
                {
                    case EEntitySection.Components:
                    {
                        foreach (var key in _componentFoldouts.Keys.ToList())
                        {
                            _componentFoldouts[key] = false;
                        }
                        break;
                    }
                    case EEntitySection.Contexts:
                    {
                        foreach (var key in _contextFoldouts.Keys.ToList())
                        {
                            _contextFoldouts[key] = false;
                        }
                        break;
                    }
                    case EEntitySection.Binders:
                    {
                        foreach (var key in _binderFoldouts.Keys.ToList())
                        {
                            _binderFoldouts[key] = false;
                        }
                        break;
                    }
                }
            }
            #endregion

            #region get/set
            public bool GetSectionFoldout(EEntitySection section) =>
                _sectionFoldouts.GetValueOrDefault(section);
            public bool GetSectionFoldout(EEntitySection section, bool defaultFoldout) =>
                _sectionFoldouts.GetValueOrDefault(section, defaultFoldout);

            public void SetSectionFoldout(EEntitySection section, bool value) => _sectionFoldouts[section] = value;


            public bool TryGetSectionFoldout(EEntitySection sectionType, out bool value)
            {
                return _sectionFoldouts.TryGetValue(sectionType, out value);
            }
            
            public bool TryGetFoldout(EEntitySection sectionType, Type t, out bool value)
            {
                return sectionType switch
                {
                    EEntitySection.Components => _componentFoldouts.TryGetValue(t, out value),
                    EEntitySection.Contexts => _contextFoldouts.TryGetValue(t, out value),
                    EEntitySection.Binders => _binderFoldouts.TryGetValue(t, out value),
                    _ => throw new ArgumentOutOfRangeException(nameof(sectionType), sectionType, null)
                };
            }

            public int GetFoldoutCount(EEntitySection sectionType)
            {
                return sectionType switch
                {
                    EEntitySection.Components => _componentFoldouts.Count,
                    EEntitySection.Contexts => _contextFoldouts.Count,
                    EEntitySection.Binders => _binderFoldouts.Count,
                    _ => 0
                };
            }
            
            public bool GetFoldout(EEntitySection sectionType, Type t)
            {
                return sectionType switch
                {
                    EEntitySection.Components => _componentFoldouts.GetValueOrDefault(t),
                    EEntitySection.Contexts => _contextFoldouts.GetValueOrDefault(t),
                    EEntitySection.Binders => _binderFoldouts.GetValueOrDefault(t),
                    _ => false
                };
            }

            public bool GetFoldout(EEntitySection sectionType, Type t, bool defaultFoldout)
            {
                return sectionType switch
                {
                    EEntitySection.Components => _componentFoldouts.GetValueOrDefault(t, defaultFoldout),
                    EEntitySection.Contexts => _contextFoldouts.GetValueOrDefault(t, defaultFoldout),
                    EEntitySection.Binders => _binderFoldouts.GetValueOrDefault(t, defaultFoldout),
                    _ => false
                };
            }

            public void SetFoldout(EEntitySection sectionType, Type t, bool value)
            {
                switch (sectionType)
                {
                    case EEntitySection.Components:
                        _componentFoldouts[t] = value;
                        break;
                    case EEntitySection.Contexts:
                        _contextFoldouts[t] = value;
                        break;
                    case EEntitySection.Binders:
                        _binderFoldouts[t] = value;
                        break;
                }
            }
            
            public void RemoveFoldout(EEntitySection sectionType, Type t)
            {
                switch (sectionType)
                {
                    case EEntitySection.Components:
                        _componentFoldouts.Remove(t);
                        break;
                    case EEntitySection.Contexts:
                        _contextFoldouts.Remove(t);
                        break;
                    case EEntitySection.Binders:
                        _binderFoldouts.Remove(t);
                        break;
                }
            }
            #endregion
        }

        public static void DrawEntity(IWorld w, Entity e, bool canEdit)
        {
            var foldoutInfo = new EntityFoldoutInfo();
            DrawEntity(w, e, canEdit, ref foldoutInfo);
        }

        private static void initFoldouts(IWorld w, Entity e, ref EntityFoldoutInfo foldoutInfo)
        {
            if (!foldoutInfo.TryGetSectionFoldout(EEntitySection.Components, out var componentSectionFoldout))
            {
                foldoutInfo.SetSectionFoldout(EEntitySection.Components, foldoutInfo.Open);
            }
            foreach (var tuple in w.GetAllComponents(e))
            {
                if (!foldoutInfo.TryGetFoldout(EEntitySection.Components, tuple.type, out var foldout))
                {
                    foldoutInfo.SetFoldout(EEntitySection.Components, tuple.type, foldoutInfo.Open);
                }
            }
            
            if (!foldoutInfo.TryGetSectionFoldout(EEntitySection.Contexts, out var contextSectionFoldout))
            {
                foldoutInfo.SetSectionFoldout(EEntitySection.Contexts, foldoutInfo.Open);
            }
            foreach (var tuple in w.GetAllContexts(e))
            {
                if (!foldoutInfo.TryGetFoldout(EEntitySection.Contexts, tuple.type, out var foldout))
                {
                    foldoutInfo.SetFoldout(EEntitySection.Contexts, tuple.type, foldoutInfo.Open);
                }
            }
            
            if (!foldoutInfo.TryGetSectionFoldout(EEntitySection.Binders, out var binderSectionFoldout))
            {
                foldoutInfo.SetSectionFoldout(EEntitySection.Binders, foldoutInfo.Open);
            }
            foreach (var tuple in w.GetAllBinders(e))
            {
                if (!foldoutInfo.TryGetFoldout(EEntitySection.Binders, tuple.type, out var foldout))
                {
                    foldoutInfo.SetFoldout(EEntitySection.Binders, tuple.type, foldoutInfo.Open);
                }
            }
        }
        
        public static void DrawEntity(IWorld w, Entity e, bool canEdit, ref EntityFoldoutInfo foldoutInfo, Action? onRemoved = null)
        {
            initFoldouts(w, e, ref foldoutInfo);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUI.indentLevel = 0;

                bool isSingleton = w.HasSingleton(e);
                
                var open = foldoutInfo.Open;
                open = EditorGUILayout.Foldout(open, $"Entity #{e.Id}:{e.Gen}" + (isSingleton ? " <color=yellow>SINGLETONE</color>" : ""), true, ZenGUIStyles.SystemFoldout);
                foldoutInfo.Open = open;

                int prevIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel++;

                var rects = new Rect[3];
                ZenGUIStyles.GetLeftIndentedSingleLineRects(20, 1, ref rects);
                using (new EditorGUI.DisabledScope(!canEdit))
                {
                    if (GUI.Button(rects[0], "X", ZenGUIStyles.ButtonMCNormal10))
                    {
                        string msg = isSingleton
                            ? ZenStringTable.GetRemoveThisSingletonEntity(e)
                            : ZenStringTable.GetRemoveThisEntity(e);
                        if (EditorUtility.DisplayDialog(
                                ZenStringTable.RemoveEntity,
                                msg,
                                ZenStringTable.Yes,
                                ZenStringTable.No))
                        {
                            w.ExternalCommandEnqueue(ExternalCommand.DestroyEntity(e));
                            onRemoved?.Invoke();
                        }
                    }
                }

                if (GUI.Button(rects[1], "▼", ZenGUIStyles.ButtonMCNormal10))
                {
                    foldoutInfo.ExpandAll();
                }

                if (GUI.Button(rects[2], "▲", ZenGUIStyles.ButtonMCNormal10))
                {
                    foldoutInfo.CollapseAll();
                }

                if (foldoutInfo.Open)
                {
                    drawSections(w, e, canEdit, ref foldoutInfo);
                }
                
                EditorGUI.indentLevel = prevIndent;
            }
        }

        private static void drawSections(IWorld w, Entity e, bool canEdit, ref EntityFoldoutInfo foldoutInfo)
        {
            drawSection(w, EEntitySection.Components, e, canEdit, ref foldoutInfo);
            drawSection(w, EEntitySection.Contexts, e, canEdit, ref foldoutInfo);
            drawSection(w, EEntitySection.Binders, e, canEdit, ref foldoutInfo);
        }

        private static void drawSection(IWorld w, EEntitySection section, Entity e, bool canEdit, ref EntityFoldoutInfo foldoutInfo)
        {
            var open = foldoutInfo.GetSectionFoldout(section, foldoutInfo.Open);
            open = EditorGUILayout.Foldout(open,  section.ToString() + $": {foldoutInfo.GetFoldoutCount(section)}",true, ZenGUIStyles.SystemFoldout);
            foldoutInfo.SetSectionFoldout(section, open);

            drawContents(section, w, e, canEdit, ref foldoutInfo);
        }
    }
}
#endif