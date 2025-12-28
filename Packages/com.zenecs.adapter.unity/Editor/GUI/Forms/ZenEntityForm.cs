// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenEntityForm.cs
// Purpose: Partial class that provides entity editing UI forms for the ZenECS
//          Explorer window, including components, contexts, and binders sections.
// Key concepts:
//   • Entity editing: add/remove components, contexts, and binders.
//   • Section-based UI: Components, Contexts, Binders tabs.
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
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Editor.GUIs
{
    public static partial class ZenEntityForm
    {
        /// <summary>
        /// Represents the type of section to display in the entity form.
        /// </summary>
        public enum EEntitySection
        {
            /// <summary>
            /// Components section.
            /// </summary>
            Components = 0,
            
            /// <summary>
            /// Contexts section.
            /// </summary>
            Contexts,
            
            /// <summary>
            /// Binders section.
            /// </summary>
            Binders,
        }

        /// <summary>
        /// Class that manages the foldout state of the entity form.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Tracks the open/close state of each section (components, contexts, binders) of the entity
        /// and the foldout state of each item.
        /// </para>
        /// </remarks>
        public class EntityFoldoutInfo
        {
            /// <summary>
            /// Gets or sets the open/close state of the entire foldout.
            /// </summary>
            public bool Open { get; set; }
            private readonly Dictionary<EEntitySection, bool> _sectionFoldouts = new();
            private readonly Dictionary<Type, bool> _componentFoldouts = new();
            private readonly Dictionary<Type, bool> _contextFoldouts = new();
            private readonly Dictionary<Type, bool> _binderFoldouts = new();

            public EntityFoldoutInfo(IWorld w, Entity e, bool expandAll = false)
            {
                Open = expandAll;
                
                if (!TryGetSectionFoldout(EEntitySection.Components, out var componentSectionFoldout))
                {
                    SetSectionFoldout(EEntitySection.Components, Open);
                }
                foreach (var tuple in w.GetAllComponents(e))
                {
                    if (!TryGetFoldout(EEntitySection.Components, tuple.type, out var foldout))
                    {
                        SetFoldout(EEntitySection.Components, tuple.type, Open);
                    }
                }
            
                if (!TryGetSectionFoldout(EEntitySection.Contexts, out var contextSectionFoldout))
                {
                    SetSectionFoldout(EEntitySection.Contexts, Open);
                }
                foreach (var tuple in w.GetAllContexts(e))
                {
                    if (!TryGetFoldout(EEntitySection.Contexts, tuple.type, out var foldout))
                    {
                        SetFoldout(EEntitySection.Contexts, tuple.type, Open);
                    }
                }
            
                if (!TryGetSectionFoldout(EEntitySection.Binders, out var binderSectionFoldout))
                {
                    SetSectionFoldout(EEntitySection.Binders, Open);
                }
                foreach (var tuple in w.GetAllBinders(e))
                {
                    if (!TryGetFoldout(EEntitySection.Binders, tuple.type, out var foldout))
                    {
                        SetFoldout(EEntitySection.Binders, tuple.type, Open);
                    }
                }
            }
            
            #region expand / collapse
            
            /// <summary>
            /// Expands all sections and items.
            /// </summary>
            /// <remarks>
            /// <para>
            /// Expands the entire foldout and all sections (Components, Contexts, Binders) and all items.
            /// </para>
            /// </remarks>
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

            /// <summary>
            /// Collapses all sections and items.
            /// </summary>
            /// <remarks>
            /// <para>
            /// Collapses the entire foldout and all sections (Components, Contexts, Binders) and all items.
            /// </para>
            /// </remarks>
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

            /// <summary>
            /// Expands all items in the specified section.
            /// </summary>
            /// <param name="sectionType">The type of section to expand.</param>
            /// <remarks>
            /// <para>
            /// Expands the foldout state of the specified section and all items within that section.
            /// </para>
            /// </remarks>
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

            /// <summary>
            /// Collapses all items in the specified section.
            /// </summary>
            /// <param name="sectionType">The type of section to collapse.</param>
            /// <remarks>
            /// <para>
            /// Collapses the foldout state of the specified section and all items within that section.
            /// </para>
            /// </remarks>
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
            
            /// <summary>
            /// Gets the foldout state of the specified section.
            /// </summary>
            /// <param name="section">The type of section to check.</param>
            /// <returns>
            /// Returns <c>true</c> if the section is open; otherwise, <c>false</c>.
            /// Returns <c>false</c> if the section is not registered.
            /// </returns>
            public bool GetSectionFoldout(EEntitySection section) =>
                _sectionFoldouts.GetValueOrDefault(section);
                
            /// <summary>
            /// Gets the foldout state of the specified section.
            /// </summary>
            /// <param name="section">The type of section to check.</param>
            /// <param name="defaultFoldout">The default value to use if the section is not registered.</param>
            /// <returns>
            /// Returns <c>true</c> if the section is open; otherwise, <c>false</c>.
            /// Returns <paramref name="defaultFoldout"/> if the section is not registered.
            /// </returns>
            public bool GetSectionFoldout(EEntitySection section, bool defaultFoldout) =>
                _sectionFoldouts.GetValueOrDefault(section, defaultFoldout);

            /// <summary>
            /// Sets the foldout state of the specified section.
            /// </summary>
            /// <param name="section">The type of section to set.</param>
            /// <param name="value">The new foldout state.</param>
            public void SetSectionFoldout(EEntitySection section, bool value) => _sectionFoldouts[section] = value;

            /// <summary>
            /// Attempts to get the foldout state of the specified section.
            /// </summary>
            /// <param name="sectionType">The type of section to check.</param>
            /// <param name="value">
            /// When this method returns, contains the foldout state if the section is registered;
            /// otherwise, contains the default value.
            /// </param>
            /// <returns>
            /// Returns <c>true</c> if the section is registered; otherwise, <c>false</c>.
            /// </returns>
            public bool TryGetSectionFoldout(EEntitySection sectionType, out bool value)
            {
                return _sectionFoldouts.TryGetValue(sectionType, out value);
            }
            
            /// <summary>
            /// Attempts to get the foldout state of a specific type item within the specified section.
            /// </summary>
            /// <param name="sectionType">The type of section to check.</param>
            /// <param name="t">The type of item to check.</param>
            /// <param name="value">
            /// When this method returns, contains the foldout state if the item is registered;
            /// otherwise, contains the default value.
            /// </param>
            /// <returns>
            /// Returns <c>true</c> if the item is registered; otherwise, <c>false</c>.
            /// </returns>
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

            /// <summary>
            /// Gets the count of foldout items registered in the specified section.
            /// </summary>
            /// <param name="sectionType">The type of section to check the count for.</param>
            /// <returns>The count of foldout items registered in the section.</returns>
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
            
            /// <summary>
            /// Gets the foldout state of a specific type item within the specified section.
            /// </summary>
            /// <param name="sectionType">The type of section to check.</param>
            /// <param name="t">The type of item to check.</param>
            /// <returns>
            /// Returns <c>true</c> if the item is open; otherwise, <c>false</c>.
            /// Returns <c>false</c> if the item is not registered.
            /// </returns>
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

            /// <summary>
            /// Gets the foldout state of a specific type item within the specified section.
            /// </summary>
            /// <param name="sectionType">The type of section to check.</param>
            /// <param name="t">The type of item to check.</param>
            /// <param name="defaultFoldout">The default value to use if the item is not registered.</param>
            /// <returns>
            /// Returns <c>true</c> if the item is open; otherwise, <c>false</c>.
            /// Returns <paramref name="defaultFoldout"/> if the item is not registered.
            /// </returns>
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

            /// <summary>
            /// Sets the foldout state of a specific type item within the specified section.
            /// </summary>
            /// <param name="sectionType">The type of section to set.</param>
            /// <param name="t">The type of item to set.</param>
            /// <param name="value">The new foldout state.</param>
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
            
            /// <summary>
            /// Removes the foldout state of a specific type item within the specified section.
            /// </summary>
            /// <param name="sectionType">The type of section to remove from.</param>
            /// <param name="t">The type of item to remove.</param>
            /// <remarks>
            /// <para>
            /// No exception is thrown if the item is not registered.
            /// </para>
            /// </remarks>
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

        public static void DrawEntity(IWorld? w, Entity e, bool canEdit, ref EntityFoldoutInfo foldoutInfo, Action? onRemoved = null)
        {
            if (w == null) return;
            if (!w.IsAlive(e)) return;
            
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