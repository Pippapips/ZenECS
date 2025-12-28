// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: EntityBlueprintAssetPostprocessor.cs
// Purpose: Automatically repairs broken references in EntityBlueprint when assets are imported.
// Key concepts:
//   • AssetPostprocessor: Called when Unity imports assets
//   • Reference repair: Repairs broken references after file copy when GUID changes
//   • Editor-only: compiled out in player builds via #if UNITY_EDITOR.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
#nullable enable
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ZenECS.Adapter.Unity.Blueprints;
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;

namespace ZenECS.Adapter.Unity.Editor.Inspectors
{
    /// <summary>
    /// Automatically repairs broken references in EntityBlueprint when assets are imported.
    /// </summary>
    /// <remarks>
    /// When files are copied, the GUID in the .meta file may change, breaking references.
    /// This postprocessor finds and repairs referenced assets in the same directory.
    /// </remarks>
    public sealed class EntityBlueprintAssetPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var importedPath in importedAssets)
            {
                // Only process EntityBlueprint assets
                if (!importedPath.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                // Check if file is EntityBlueprint by filename
                var fileName = Path.GetFileNameWithoutExtension(importedPath);
                if (!fileName.Contains("EntityBlueprint") && !fileName.Contains("Blueprint"))
                    continue;

                // First, try to repair broken GUID references by reading YAML file directly
                if (TryRepairYAMLReferences(importedPath))
                {
                    // Force Unity to reimport after YAML modification
                    AssetDatabase.ImportAsset(importedPath, ImportAssetOptions.ForceUpdate);
                    continue;
                }

                // If YAML repair failed or is unnecessary, try to repair by loading the asset
                var asset = AssetDatabase.LoadAssetAtPath<EntityBlueprint>(importedPath);
                if (asset == null)
                {
                    // Cannot load asset (YAML parsing failure)
                    Debug.LogWarning(
                        $"[EntityBlueprintAssetPostprocessor] " +
                        $"Cannot load asset: {importedPath}. " +
                        $"YAML parsing error may exist. Please repair manually in Inspector."
                    );
                    continue;
                }

                // Try to repair broken references
                RepairBrokenReferences(asset, importedPath);
            }
        }

        /// <summary>
        /// Reads YAML file directly to repair broken GUID references.
        /// </summary>
        static bool TryRepairYAMLReferences(string assetPath)
        {
            try
            {
                var fsPath = Path.GetFullPath(assetPath);
                if (!File.Exists(fsPath))
                    return false;

                var yaml = File.ReadAllText(fsPath);
                var originalYaml = yaml;
                var blueprintDir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(blueprintDir))
                    return false;

                // Find all ContextAssets in the same directory
                var allContextAssets = ZenECS.Adapter.Unity.Editor.Common.ZenAssetDatabase
                    .FindAndLoadAllAssets<ContextAsset>();

                var candidates = new System.Collections.Generic.Dictionary<string, string>();
                foreach (var candidate in allContextAssets)
                {
                    var candidatePath = AssetDatabase.GetAssetPath(candidate);
                    var candidateDir = Path.GetDirectoryName(candidatePath)?.Replace('\\', '/');
                    if (candidateDir == blueprintDir)
                    {
                        var guid = AssetDatabase.AssetPathToGUID(candidatePath);
                        if (!string.IsNullOrEmpty(guid))
                        {
                            candidates[guid] = candidatePath;
                        }
                    }
                }

                // Find and repair broken GUID references in _contextAssets
                // YAML format: - {fileID: 11400000, guid: OLD_GUID, type: 2}
                var guidPattern = @"guid:\s*([a-f0-9]{32})";
                var matches = System.Text.RegularExpressions.Regex.Matches(yaml, guidPattern);
                bool modified = false;

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var oldGuid = match.Groups[1].Value;
                    // Check if this GUID is valid
                    var testPath = AssetDatabase.GUIDToAssetPath(oldGuid);
                    if (string.IsNullOrEmpty(testPath))
                    {
                        // Broken GUID - replace with candidate from same directory
                        if (candidates.Count > 0)
                        {
                            var newGuid = candidates.Keys.First();
                            yaml = yaml.Replace($"guid: {oldGuid}", $"guid: {newGuid}");
                            modified = true;
                            Debug.LogWarning(
                                $"[EntityBlueprintAssetPostprocessor] " +
                                $"Repaired broken GUID: {oldGuid} -> {newGuid} in {assetPath}"
                            );
                        }
                    }
                }

                // Reserialize managed references in _binders
                bool bindersModified = ReserializeBindersSection(yaml, assetPath, out yaml);
                if (bindersModified)
                {
                    modified = true;
                }

                if (modified && yaml != originalYaml)
                {
                    File.WriteAllText(fsPath, yaml, System.Text.Encoding.UTF8);
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"[EntityBlueprintAssetPostprocessor] " +
                    $"Error occurred while modifying YAML: {assetPath}\n{ex}"
                );
            }

            return false;
        }

        /// <summary>
        /// Reserializes managed references in the _binders section.
        /// Extracts type information to create new instances and reserialize.
        /// </summary>
        static bool ReserializeBindersSection(string yaml, string assetPath, out string repairedYaml)
        {
            repairedYaml = yaml;
            
            try
            {
                // Check if _binders section exists
                var bindersStart = yaml.IndexOf("  _binders:");
                if (bindersStart < 0)
                    return false;

                // Parse line by line to find exact location
                var lines = yaml.Split('\n');
                int bindersLineIndex = -1;
                int bindersEndLineIndex = lines.Length;

                // Find _binders line
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].TrimStart().StartsWith("_binders:"))
                    {
                        bindersLineIndex = i;
                        int indentLevel = GetIndentLevel(lines[i]);
                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            var line = lines[j];
                            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                                continue;
                            var lineIndent = GetIndentLevel(line);
                            if (lineIndent <= indentLevel && line.Contains(":"))
                            {
                                bindersEndLineIndex = j;
                                break;
                            }
                        }
                        break;
                    }
                }

                if (bindersLineIndex < 0)
                    return false;

                // Extract entire _binders section
                var bindersSection = string.Join("\n", lines, bindersLineIndex, bindersEndLineIndex - bindersLineIndex);

                // Extract type information and reserialize
                var reserializedSection = ReserializeManagedReferences(bindersSection, assetPath);
                if (reserializedSection != null && reserializedSection != bindersSection)
                {
                    // Replace with reserialized section
                    var beforeBinders = bindersLineIndex > 0 
                        ? string.Join("\n", lines, 0, bindersLineIndex) 
                        : "";
                    var afterBinders = bindersEndLineIndex < lines.Length 
                        ? string.Join("\n", lines, bindersEndLineIndex, lines.Length - bindersEndLineIndex)
                        : "";
                    
                    var indent = lines[bindersLineIndex].Substring(0, GetIndentLevel(lines[bindersLineIndex]));
                    repairedYaml = string.IsNullOrEmpty(beforeBinders)
                        ? indent + reserializedSection + (string.IsNullOrEmpty(afterBinders) ? "" : "\n" + afterBinders)
                        : beforeBinders + "\n" + indent + reserializedSection + (string.IsNullOrEmpty(afterBinders) ? "" : "\n" + afterBinders);
                    
                    Debug.LogWarning(
                        $"[EntityBlueprintAssetPostprocessor] " +
                        $"Reserialized managed references in _binders: {assetPath}"
                    );
                    return true;
                }

                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"[EntityBlueprintAssetPostprocessor] " +
                    $"Error occurred while reserializing _binders: {assetPath}\n{ex}"
                );
                return false;
            }
        }

        /// <summary>
        /// Reserializes managed references.
        /// </summary>
        static string? ReserializeManagedReferences(string bindersSection, string assetPath)
        {
            try
            {
                // Extract type information from RefIds section
                var refIdsStart = bindersSection.IndexOf("RefIds:");
                if (refIdsStart < 0)
                    return null;

                var refIdsSection = bindersSection.Substring(refIdsStart);
                
                // Type information pattern: type: {class: ClassName, ns: Namespace, asm: AssemblyName}
                var typePattern = @"type:\s*\{class:\s*([^,]+),\s*ns:\s*([^,]+),\s*asm:\s*([^}]+)\}";
                var typeMatches = System.Text.RegularExpressions.Regex.Matches(refIdsSection, typePattern);
                
                if (typeMatches.Count == 0)
                    return null;

                var binderTypes = new System.Collections.Generic.List<System.Tuple<string, string, string>>();
                foreach (System.Text.RegularExpressions.Match match in typeMatches)
                {
                    var className = match.Groups[1].Value.Trim();
                    var namespaceName = match.Groups[2].Value.Trim();
                    var assemblyName = match.Groups[3].Value.Trim();
                    binderTypes.Add(System.Tuple.Create(className, namespaceName, assemblyName));
                }

                // Validate type information
                // Actual reserialization is performed using SerializedObject after Unity loads the asset
                bool allTypesValid = true;
                foreach (var (className, namespaceName, assemblyName) in binderTypes)
                {
                    // Construct assembly-qualified name
                    var typeName = string.IsNullOrEmpty(namespaceName)
                        ? $"{className}, {assemblyName}"
                        : $"{namespaceName}.{className}, {assemblyName}";

                    var type = System.Type.GetType(typeName, false);
                    if (type == null)
                    {
                        // Search in all assemblies
                        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                        {
                            type = asm.GetType(typeName, false);
                            if (type != null) break;
                            
                            // Also try namespace.class format
                            if (!string.IsNullOrEmpty(namespaceName))
                            {
                                type = asm.GetType($"{namespaceName}.{className}", false);
                                if (type != null) break;
                            }
                        }
                    }

                    if (type == null)
                    {
                        allTypesValid = false;
                        Debug.LogWarning(
                            $"[EntityBlueprintAssetPostprocessor] " +
                            $"Cannot find type: {typeName} in {assetPath}"
                        );
                    }
                }

                // If all types are valid, keep original for Unity to reserialize after loading
                // If types are invalid, replace with empty array
                if (!allTypesValid)
                {
                    return "_binders: []";
                }

                // Types are valid, so keep original for Unity to reserialize
                return null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"[EntityBlueprintAssetPostprocessor] " +
                    $"Error while reserializing managed references: {assetPath}\n{ex}"
                );
                return null;
            }
        }

        /// <summary>
        /// Repairs broken managed references in _binders section (fallback).
        /// </summary>
        static bool RepairBindersSection(string yaml, string assetPath, out string repairedYaml)
        {
            repairedYaml = yaml;
            
            try
            {
                // Check if _binders section exists
                var bindersStart = yaml.IndexOf("  _binders:");
                if (bindersStart < 0)
                    return false;

                // Parse line by line to find exact location
                var lines = yaml.Split('\n');
                int bindersLineIndex = -1;
                int bindersEndLineIndex = lines.Length;

                // Find _binders line
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].TrimStart().StartsWith("_binders:"))
                    {
                        bindersLineIndex = i;
                        
                        // Find end of _binders section
                        // Find next field (same indent level) or end of file
                        int indentLevel = GetIndentLevel(lines[i]);
                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            var line = lines[j];
                            
                            // Skip empty lines or comments
                            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                                continue;
                            
                            var lineIndent = GetIndentLevel(line);
                            // End when field with same or smaller indent level appears
                            if (lineIndent <= indentLevel && line.Contains(":"))
                            {
                                bindersEndLineIndex = j;
                                break;
                            }
                        }
                        break;
                    }
                }

                if (bindersLineIndex < 0)
                    return false;

                // Extract entire _binders section
                var bindersSection = string.Join("\n", lines, bindersLineIndex, bindersEndLineIndex - bindersLineIndex);

                // Extract type information and attempt reserialization
                var reserializedSection = ReserializeManagedReferences(bindersSection, assetPath);
                if (reserializedSection != null && reserializedSection != bindersSection)
                {
                    // Replace with reserialized section
                    var beforeBinders = bindersLineIndex > 0 
                        ? string.Join("\n", lines, 0, bindersLineIndex) 
                        : "";
                    var afterBinders = bindersEndLineIndex < lines.Length 
                        ? string.Join("\n", lines, bindersEndLineIndex, lines.Length - bindersEndLineIndex)
                        : "";
                    
                    var indent = lines[bindersLineIndex].Substring(0, GetIndentLevel(lines[bindersLineIndex]));
                    repairedYaml = string.IsNullOrEmpty(beforeBinders)
                        ? indent + reserializedSection + (string.IsNullOrEmpty(afterBinders) ? "" : "\n" + afterBinders)
                        : beforeBinders + "\n" + indent + reserializedSection + (string.IsNullOrEmpty(afterBinders) ? "" : "\n" + afterBinders);
                    
                    Debug.LogWarning(
                        $"[EntityBlueprintAssetPostprocessor] " +
                        $"Reserialized managed references in _binders: {assetPath}"
                    );
                    return true;
                }

                // If cannot reserialize, just verify type information is valid
                // Let Unity reserialize after loading asset
                return false;
            }
            catch (System.Exception ex)
            {
                // If error occurs, safely replace with empty array
                Debug.LogError(
                    $"[EntityBlueprintAssetPostprocessor] " +
                    $"Error occurred while repairing _binders, replacing with empty array: {assetPath}\n{ex}"
                );
                
                // Even if error occurs, replace with empty array and treat as success
                try
                {
                    var bindersStart = yaml.IndexOf("  _binders:");
                    if (bindersStart >= 0)
                    {
                        var lines = yaml.Split('\n');
                        int bindersLineIndex = -1;
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (lines[i].TrimStart().StartsWith("_binders:"))
                            {
                                bindersLineIndex = i;
                                break;
                            }
                        }
                        
                        if (bindersLineIndex >= 0)
                        {
                            // Find next field
                            int bindersEndLineIndex = lines.Length;
                            int indentLevel = GetIndentLevel(lines[bindersLineIndex]);
                            for (int j = bindersLineIndex + 1; j < lines.Length; j++)
                            {
                                var line = lines[j];
                                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                                    continue;
                                var lineIndent = GetIndentLevel(line);
                                if (lineIndent <= indentLevel && line.Contains(":"))
                                {
                                    bindersEndLineIndex = j;
                                    break;
                                }
                            }
                            
                            repairedYaml = ReplaceBindersSection(lines, bindersLineIndex, bindersEndLineIndex);
                            return true;
                        }
                    }
                }
                catch
                {
                    // Last resort: find entire _binders section and replace
                    var simplePattern = @"  _binders:.*?(?=\n  [a-zA-Z_]|\Z)";
                    repairedYaml = System.Text.RegularExpressions.Regex.Replace(
                        yaml, 
                        simplePattern, 
                        "  _binders: []",
                        System.Text.RegularExpressions.RegexOptions.Singleline
                    );
                    return repairedYaml != yaml;
                }
                
                return false;
            }
        }

        /// <summary>
        /// Returns indent level (number of spaces).
        /// </summary>
        static int GetIndentLevel(string line)
        {
            int indent = 0;
            foreach (char c in line)
            {
                if (c == ' ')
                    indent++;
                else if (c == '\t')
                    indent += 4; // Treat tab as 4 spaces
                else
                    break;
            }
            return indent;
        }

        /// <summary>
        /// Replaces _binders section with empty array.
        /// </summary>
        static string ReplaceBindersSection(string[] lines, int bindersLineIndex, int bindersEndLineIndex)
        {
            var beforeBinders = bindersLineIndex > 0 
                ? string.Join("\n", lines, 0, bindersLineIndex) 
                : "";
            var afterBinders = bindersEndLineIndex < lines.Length 
                ? string.Join("\n", lines, bindersEndLineIndex, lines.Length - bindersEndLineIndex)
                : "";
            
            // Check indent level
            var indent = lines[bindersLineIndex].Substring(0, GetIndentLevel(lines[bindersLineIndex]));
            
            if (string.IsNullOrEmpty(beforeBinders))
                return indent + "_binders: []" + (string.IsNullOrEmpty(afterBinders) ? "" : "\n" + afterBinders);
            else
                return beforeBinders + "\n" + indent + "_binders: []" + (string.IsNullOrEmpty(afterBinders) ? "" : "\n" + afterBinders);
        }

        /// <summary>
        /// Converts Unity's managedReferenceFullTypename to standard assembly-qualified name format.
        /// Unity format: "AssemblyName Namespace.ClassName"
        /// Standard format: "Namespace.ClassName, AssemblyName"
        /// </summary>
        static string NormalizeManagedReferenceTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return typeName;

            // Check if already in standard format (comma indicates standard format)
            if (typeName.Contains(","))
                return typeName;

            // Parse Unity format: "AssemblyName Namespace.ClassName"
            var parts = typeName.Split(new[] { ' ' }, 2, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var assemblyName = parts[0];
                var fullClassName = parts[1];
                return $"{fullClassName}, {assemblyName}";
            }

            // Return original if parsing fails
            return typeName;
        }

        /// <summary>
        /// Extracts only class name from type name.
        /// </summary>
        static string ExtractClassName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return typeName;

            // Extract class name from "Namespace.ClassName" or "ClassName" format
            var lastDot = typeName.LastIndexOf('.');
            if (lastDot >= 0 && lastDot < typeName.Length - 1)
            {
                return typeName.Substring(lastDot + 1);
            }

            // If separated by space (Unity format)
            var parts = typeName.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                var fullClassName = parts[1];
                lastDot = fullClassName.LastIndexOf('.');
                if (lastDot >= 0 && lastDot < fullClassName.Length - 1)
                {
                    return fullClassName.Substring(lastDot + 1);
                }
                return fullClassName;
            }

            return typeName;
        }

        /// <summary>
        /// Copies field values of object (shallow copy).
        /// </summary>
        static void CopyFields(object source, object target, System.Type type)
        {
            try
            {
                var fields = type.GetFields(
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance
                );
                
                foreach (var field in fields)
                {
                    if (field.IsStatic) continue;
                    try
                    {
                        var value = field.GetValue(source);
                        field.SetValue(target, value);
                    }
                    catch
                    {
                        // Ignore field copy failures
                    }
                }
            }
            catch
            {
                // Ignore copy failures
            }
        }

        /// <summary>
        /// Repairs broken references in EntityBlueprint.
        /// </summary>
        static void RepairBrokenReferences(EntityBlueprint blueprint, string blueprintPath)
        {
            // Skip if Unity cannot load asset (YAML parsing failure, etc.)
            if (blueprint == null)
                return;

            bool needsRepair = false;
            var blueprintDir = Path.GetDirectoryName(blueprintPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(blueprintDir))
                return;

            // Use SerializedObject to modify references
            var so = new SerializedObject(blueprint);
            so.Update();

            // Repair _contextAssets
            var contextsProp = so.FindProperty("_contextAssets");
            if (contextsProp != null && contextsProp.isArray)
            {
                // Find all ContextAssets in the same directory
                var allContextAssets = ZenECS.Adapter.Unity.Editor.Common.ZenAssetDatabase
                    .FindAndLoadAllAssets<ContextAsset>();

                var candidates = new System.Collections.Generic.List<ContextAsset>();
                foreach (var candidate in allContextAssets)
                {
                    var candidatePath = AssetDatabase.GetAssetPath(candidate);
                    var candidateDir = Path.GetDirectoryName(candidatePath)?.Replace('\\', '/');
                    if (candidateDir == blueprintDir)
                    {
                        candidates.Add(candidate);
                    }
                }

                // Repair broken references
                for (int i = 0; i < contextsProp.arraySize; i++)
                {
                    var elem = contextsProp.GetArrayElementAtIndex(i);
                    var currentAsset = elem.objectReferenceValue as ContextAsset;

                    if (currentAsset == null)
                    {
                        // Remove null references or repair if candidates exist
                        if (candidates.Count > i)
                        {
                            // Use candidate matching index
                            elem.objectReferenceValue = candidates[i];
                            needsRepair = true;
                        }
                        else if (candidates.Count > 0)
                        {
                            // Use first candidate if available
                            elem.objectReferenceValue = candidates[0];
                            needsRepair = true;
                        }
                        else
                        {
                            // Keep null if no candidates (assign manually later)
                        }
                    }
                }
            }

            // Reserialize all managed references in _binders
            var bindersProp = so.FindProperty("_binders");
            if (bindersProp != null && bindersProp.isArray)
            {
                // Reserialize all managed references (even if not null)
                for (int i = 0; i < bindersProp.arraySize; i++)
                {
                    var elem = bindersProp.GetArrayElementAtIndex(i);
                    var currentBinder = elem.managedReferenceValue;
                    var typeName = elem.managedReferenceFullTypename;
                    
                    // Reserialize if type information exists
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        // Convert Unity's managedReferenceFullTypename format
                        // Format: "AssemblyName Namespace.ClassName" -> "Namespace.ClassName, AssemblyName"
                        var normalizedTypeName = NormalizeManagedReferenceTypeName(typeName);
                        
                        var type = System.Type.GetType(normalizedTypeName, false);
                        if (type == null)
                        {
                            // Search in all assemblies
                            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                            {
                                type = asm.GetType(normalizedTypeName, false);
                                if (type != null) break;
                                
                                // Also try original type name
                                type = asm.GetType(typeName, false);
                                if (type != null) break;
                                
                                // Also try class name only without namespace
                                var className = ExtractClassName(typeName);
                                if (!string.IsNullOrEmpty(className))
                                {
                                    type = asm.GetType(className, false);
                                    if (type != null) break;
                                }
                            }
                        }

                        if (type != null)
                        {
                            try
                            {
                                // Create new instance and reserialize
                                var newInstance = ZenECS.Core.ZenDefaults.CreateWithDefaults(type);
                                if (newInstance != null)
                                {
                                    // Copy field values if current value exists (shallow copy)
                                    if (currentBinder != null)
                                    {
                                        CopyFields(currentBinder, newInstance, type);
                                    }
                                    
                                    elem.managedReferenceValue = newInstance;
                                    needsRepair = true;
                                    Debug.LogWarning(
                                        $"[EntityBlueprintAssetPostprocessor] " +
                                        $"Reserialized binder[{i}] in {blueprintPath}: {typeName}"
                                    );
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogWarning(
                                    $"[EntityBlueprintAssetPostprocessor] " +
                                    $"Failed to reserialize binder[{i}] in {blueprintPath}: {typeName}, {ex.Message}"
                                );
                            }
                        }
                        else
                        {
                            // Remove if type cannot be found
                            bindersProp.DeleteArrayElementAtIndex(i);
                            needsRepair = true;
                            Debug.LogWarning(
                                $"[EntityBlueprintAssetPostprocessor] " +
                                $"Removed binder[{i}] in {blueprintPath} because type cannot be found: {typeName}"
                            );
                            i--; // Adjust index
                        }
                    }
                    else if (currentBinder == null)
                    {
                        // Remove if both type information and value are null
                        bindersProp.DeleteArrayElementAtIndex(i);
                        needsRepair = true;
                            Debug.LogWarning(
                                $"[EntityBlueprintAssetPostprocessor] " +
                                $"Removed binder[{i}] in {blueprintPath} because it is null and has no type information."
                            );
                            i--; // Adjust index
                    }
                }
            }

            if (needsRepair)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(blueprint);
                // Do not call SaveAssets (Unity handles it automatically)
                Debug.LogWarning(
                    $"[EntityBlueprintAssetPostprocessor] Repaired broken references: {blueprintPath}"
                );
            }
        }
    }
}
#endif


