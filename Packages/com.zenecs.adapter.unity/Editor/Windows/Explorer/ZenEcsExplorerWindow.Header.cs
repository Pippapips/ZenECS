// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenEcsExplorerWindow.Header.cs
// Purpose: Header section implementation for ZenECS Explorer window, providing
//          toolbar with world selection, edit mode toggle, and navigation buttons.
// Key concepts:
//   • Toolbar UI: world picker, edit mode toggle, find/clear buttons.
//   • Partial class: part of ZenEcsExplorerWindow split across multiple files.
//   • Editor-only: compiled out in player builds.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Blueprints;
using ZenECS.Adapter.Unity.Editor.Common;
using ZenECS.Adapter.Unity.Editor.GUIs;
using ZenECS.Core;
using ZenECS.Core.Systems;

namespace ZenECS.Adapter.Unity.Editor.Windows
{
    public sealed partial class ZenEcsExplorerWindow
    {
        void DrawHeader()
        {
            if (_kernel == null || _world == null) return;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var worlds = _kernel.GetAllWorld().ToList();

                // ─────────────────────────────────────
                // World Select dropdown
                // ─────────────────────────────────────
                if (worlds.Count == 0)
                {
                    // When no worlds exist
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.Popup(0, new[] { "No World (create in your bootstrap)" },
                            GUILayout.MaxWidth(240));
                    }
                }
                else
                {
                    string countString = $"({worlds.Count})";
                    GUILayout.Label(countString, ZenGUIStyles.LabelLCNormal10);

                    GUILayout.Space(2);

                    // Current world index
                    int currentIndex = worlds.FindIndex(w => ReferenceEquals(w, _world));
                    if (currentIndex < 0) currentIndex = 0;

                    // Dropdown options: World name (or Id string if none)
                    string[] options = worlds
                        .Select(w =>
                        {
                            var name = string.IsNullOrEmpty(w.Name) ? "(unnamed)" : w.Name;
                            return name;
                        })
                        .ToArray();

                    int newIndex = EditorGUILayout.Popup(currentIndex, options, GUILayout.MaxWidth(220));
                    if (newIndex != currentIndex)
                    {
                        ClearState();

                        var selected = worlds[newIndex];
                        if (selected != null)
                        {
                            _kernel.SetCurrentWorld(selected);
                            _world = selected;
                        }
                    }

                    GUILayout.Space(2);

                    string idText = _world.Id.ToString();
                    string nameText = string.IsNullOrEmpty(_world.Name) ? "(unnamed)" : _world.Name;
                    string tagsText = (_world.Tags.Count > 0)
                        ? string.Join(", ", _world.Tags)
                        : "none";

                    string meta = $"Current World: {nameText} (Tags: {tagsText}) <color=#707070>[GUID: {idText}]</color>";
                    GUILayout.Label(meta, ZenGUIStyles.LabelLCNormal10, GUILayout.MaxWidth(600));
                }

                GUILayout.FlexibleSpace();

                // ─────────────────────────────────────
                // Right end: + button (maintains existing functionality)
                // ─────────────────────────────────────
                var plusContent = ZenGUIContents.IconPlus();

                var rPlus = GUILayoutUtility.GetRect(
                    plusContent,
                    EditorStyles.iconButton,
                    GUILayout.Width(24));

                rPlus.y += 2;

                if (GUI.Button(rPlus, plusContent, EditorStyles.iconButton))
                {
                    ShowPlusContextMenu(rPlus, _world);
                }
            }
            
            EditorGUILayout.Space(4);
        }
        
        void ShowPlusContextMenu(Rect activatorRectGui, IWorld? world)
        {
            if (world == null) return;

            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Add Entity from Blueprint..."), false,
                () => { ShowEntityBlueprintPicker(activatorRectGui); });

            // --- Add Singleton ---
            menu.AddItem(new GUIContent("Add Singleton..."), false, () =>
            {
                // Collect all singleton struct types
                var allSingletons = ZenUtil.SingletonTypeFinder.All();

                // Disable singleton types that already exist in world
                var disabled = new HashSet<Type>();
                try
                {
                    foreach (var (t, _) in world.GetAllSingletons())
                    {
                        if (t != null) disabled.Add(t);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                ZenSingletonPickerWindow.Show(
                    allSingletonTypes: allSingletons,
                    disabled: disabled,
                    onPick: pickedType =>
                    {
                        try
                        {
                            var inst = ZenDefaults.CreateWithDefaults(pickedType);
                            world.ExternalCommandEnqueue(ExternalCommand.SetSingleton(pickedType, inst));
                            Repaint();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                    },
                    activatorRectGui: activatorRectGui,
                    title: "Add Singleton"
                );
            });

            menu.AddItem(
                new GUIContent("Add System..."),
                false,
                () =>
                {
                    // Full System type list
                    var allSystemTypes = ZenUtil.SystemTypeFinder.All().ToList();

                    // Disable System types that are already registered
                    var disabled = new HashSet<Type>();
                    var existing = world.GetAllSystems();
                    if (existing.Count > 0)
                    {
                        foreach (var s in existing)
                        {
                            if (s == null) continue;
                            disabled.Add(s.GetType());
                        }
                    }

                    ZenSystemPickerWindow.Show(
                        allSystemTypes: allSystemTypes,
                        disabled: disabled,
                        onPick: t =>
                        {
                            try
                            {
                                var inst = Activator.CreateInstance(t) as ISystem;
                                if (inst == null)
                                {
                                    Debug.LogError(
                                        $"ZenECS Explorer: Cannot create system of type {t.FullName}. " +
                                        "System must have a public parameterless constructor.");
                                    return;
                                }

                                // Actual registration
                                world.AddSystem(inst);

                                Repaint();
                            }
                            catch (Exception ex)
                            {
                                Debug.LogException(ex);
                            }
                        },
                        activatorRectGui: activatorRectGui,
                        title: "Add System",
                        onCancel: null
                    );
                });

            // ───────────── Add System Preset… ─────────────
            if (ZenEcsUnityBridge.SystemPresetResolver == null)
            {
                menu.AddDisabledItem(new GUIContent("Add System Preset (no SystemPresetResolver)"));
            }
            else
            {
                menu.AddItem(
                    new GUIContent("Add System Preset..."),
                    false,
                    () => ShowSystemPresetPicker(activatorRectGui, world)
                );
            }

            // Context dropdown based on button
            menu.DropDown(activatorRectGui);
        }
        
        void ShowEntityBlueprintPicker(Rect activatorRectGui)
        {
            ZenBlueprintPickerWindow.Show(
                activatorRectGui,
                onPick: CreateEntityFromBlueprint,
                title: "Create Entity from Blueprint"
            );
        }

        void ShowSystemPresetPicker(Rect activatorRectGui, IWorld? world)
        {
            if (world == null) return;
            
            ZenSystemPresetPickerWindow.Show(
                activatorRectGui,
                onPick: preset =>
                {
                    var resolver = ZenEcsUnityBridge.SystemPresetResolver;
                    if (resolver == null)
                    {
                        EditorUtility.DisplayDialog(
                            "SystemPresetResolver missing",
                            "ZenEcsUnityBridge.SystemPresetResolver is null.\nPlease configure a SystemPresetResolver.",
                            "OK");
                        return;
                    }

                    try
                    {
                        // Call SystemsPreset.GetValidTypes() via reflection
                        var mi = preset.GetType().GetMethod(
                            "GetValidTypes",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                            null,
                            Type.EmptyTypes,
                            null);

                        if (mi == null)
                        {
                            EditorUtility.DisplayDialog(
                                "Invalid SystemsPreset",
                                $"SystemsPreset '{preset.name}' must define GetValidTypes() returning IEnumerable<Type>.",
                                "OK");
                            return;
                        }

                        var ret = mi.Invoke(preset, null);
                        if (ret is not IEnumerable<Type> validTypes)
                        {
                            EditorUtility.DisplayDialog(
                                "Invalid SystemsPreset",
                                $"GetValidTypes() of '{preset.name}' must return IEnumerable<Type>.",
                                "OK");
                            return;
                        }

                        // ZenEcsUnityBridge.SystemPresetResolver?.InstantiateSystems(...)
                        var systems = resolver.InstantiateSystems(validTypes.ToList());
                        if (systems == null)
                        {
                            EditorUtility.DisplayDialog(
                                "InstantiateSystems returned null",
                                "SystemPresetResolver.InstantiateSystems returned null.",
                                "OK");
                            return;
                        }

                        // Batch register via world.AddSystems(systems)
                        world.AddSystems(systems);

                        // Clear cache & refresh UI
                        ClearState();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        EditorUtility.DisplayDialog(
                            "Add System Preset failed",
                            $"Failed to apply SystemsPreset '{preset.name}'.\nSee Console for details.",
                            "OK");
                    }
                },
                title: "Add System Preset"
            );
        }
        
        void CreateEntityFromBlueprint(EntityBlueprint blueprint)
        {
            if (blueprint == null) return;

            var kernel = ZenEcsUnityBridge.Kernel;
            if (kernel == null)
            {
                EditorUtility.DisplayDialog(
                    "ZenECS Kernel not ready",
                    "Kernel is not initialized. Please make sure ZenEcsUnityBridge has a running Kernel.",
                    "OK");
                return;
            }

            var world = kernel.CurrentWorld;
            if (world == null)
            {
                EditorUtility.DisplayDialog(
                    "No current World",
                    "Kernel.CurrentWorld is null. Please ensure a World is created and set as current.",
                    "OK");
                return;
            }

            var resolver = ZenEcsUnityBridge.SharedContextResolver;
            if (resolver == null)
            {
                EditorUtility.DisplayDialog(
                    "SharedContextResolver missing",
                    "ZenEcsUnityBridge.SharedContextResolver is null. Please configure a SharedContextResolver.",
                    "OK");
                return;
            }

            try
            {
                // Call Spawn to match EntityBlueprint API
                // (Based on Spawn signature already used in EntityBlueprintInspector)
                blueprint.Spawn(world, resolver);

                // Refresh Explorer if needed
                //RefreshEntityListForCurrentSystem();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog(
                    "Spawn failed",
                    $"Failed to spawn Entity from '{blueprint.name}'.\nSee console for details.",
                    "OK");
            }
        }
    }
}