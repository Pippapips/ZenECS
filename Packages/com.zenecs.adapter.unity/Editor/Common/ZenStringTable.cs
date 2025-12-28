// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: ZenStringTable.cs
// Purpose: Centralized string constants and formatting helpers for ZenECS
//          editor UI, ensuring consistent labels and messages across tools.
// Key concepts:
//   • String constants: UI labels, button text, tooltips, error messages.
//   • Formatting helpers: entity titles, component counts, system names.
//   • Localization-ready: all user-facing strings in one place.
//   • Editor-only: compiled out in player builds via #if UNITY_EDITOR.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
#nullable enable
using System;
using UnityEditor;
using UnityEngine;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Editor.Common
{
    public static class ZenStringTable
    {
        public const string LabelEntityId = "Entity ID:GEN ";
        public const string BtnFind = "Find";
        public const string BtnClear = "Clear";
        public const string BtnClose = "Close";
        public const string TipFind = "Show only the entity with this ID (no system switching).";
        public const string TipClear = "Exit single-entity view and show all entities.";
        public const string BtnEdit = "Edit";
        public const string EntityNotFound = "Entity Not Found";
        public const string NoWatchedSystem = "No Watched System";
        public const string SINGLETON = " <color=#999900><size=10>SINGLETON</size></color>";
        public const string RemoveEntity = "Remove Entity";
        public const string Yes = "Yes";
        public const string No = "No";
        public const string Component = "Component";
        public const string ZenECSKernelNotActiveYet = "ZenECS Kernel is not active yet.";
        public const string ZenECSKernelNotActiveYetDesc = "Enter Play Mode to initialize the EcsDriver and Kernel.\n\n" +
                                                           "When the Kernel becomes active, you can inspect Systems and Entities\n" +
                                                           "through the ZenECS Explorer.";
        public const string ZenECSNoCurrentWorld = "ZenECS No current world set in kernel.";

        public static string GetSinceRunning(float elapsed)
        {
            return $"Since running in {elapsed:0} seconds";
        }

        public static string GetFoundEntityTitle(Entity e)
        {
            return $"Found Entity #{e.Id}:{e.Gen}";
        }

        public static string GetEntityTitle(Entity e)
        {
            return $"Entity #{e.Id}:{e.Gen}";
        }

        public static string GetWatchedSystems(int count)
        {
            return $"Watched Systems ({count})";
        }

        public static string GetRemoveThisEntity(Entity e)
        {
            return $"Remove this entity?\n\nEntity #{e.Id}:{e.Gen}";
        }

        public static string GetRemoveThisSingletonEntity(Entity e)
        {
            return $"Remove this singleton?\n\nSingleton Entity #{e.Id}:{e.Gen}";
        }

        public static string GetComponents(int conut)
        {
            return $"Components: {conut}";
        }

        public static string GetAddComponent(Entity e)
        {
            return $"Entity #{e.Id}:{e.Gen} Add Component";
        }
    }
}
#endif