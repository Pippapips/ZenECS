// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Editor
// File: EntityBlueprintAssetPostprocessor.cs
// Purpose: Asset import 시 EntityBlueprint의 깨진 참조를 자동으로 복구합니다.
// Key concepts:
//   • AssetPostprocessor: Unity가 asset을 import할 때 호출됨
//   • 참조 복구: 파일 복사 후 GUID가 변경되어 깨진 참조를 복구
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
    /// EntityBlueprint asset import 시 깨진 참조를 자동으로 복구합니다.
    /// </summary>
    /// <remarks>
    /// 파일을 복사할 때 .meta 파일의 GUID가 변경되면 참조가 깨질 수 있습니다.
    /// 이 postprocessor는 같은 디렉토리에서 참조된 asset을 찾아 복구합니다.
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
                // EntityBlueprint asset만 처리
                if (!importedPath.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                // 파일명으로 EntityBlueprint인지 확인
                var fileName = Path.GetFileNameWithoutExtension(importedPath);
                if (!fileName.Contains("EntityBlueprint") && !fileName.Contains("Blueprint"))
                    continue;

                // 먼저 YAML 파일을 직접 읽어서 깨진 GUID 참조를 수정 시도
                if (TryRepairYAMLReferences(importedPath))
                {
                    // YAML 수정 후 Unity가 다시 import하도록 강제
                    AssetDatabase.ImportAsset(importedPath, ImportAssetOptions.ForceUpdate);
                    continue;
                }

                // YAML 수정이 실패했거나 불필요한 경우, asset을 로드해서 복구 시도
                var asset = AssetDatabase.LoadAssetAtPath<EntityBlueprint>(importedPath);
                if (asset == null)
                {
                    // asset을 로드할 수 없는 경우 (YAML 파싱 실패)
                    Debug.LogWarning(
                        $"[EntityBlueprintAssetPostprocessor] " +
                        $"asset을 로드할 수 없습니다: {importedPath}. " +
                        $"YAML 파싱 오류가 있을 수 있습니다. Inspector에서 수동으로 복구해주세요."
                    );
                    continue;
                }

                // 깨진 참조 복구 시도
                RepairBrokenReferences(asset, importedPath);
            }
        }

        /// <summary>
        /// YAML 파일을 직접 읽어서 깨진 GUID 참조를 수정합니다.
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

                // 같은 디렉토리의 모든 ContextAsset 찾기
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

                // _contextAssets의 깨진 GUID 참조 찾아서 수정
                // YAML 형식: - {fileID: 11400000, guid: OLD_GUID, type: 2}
                var guidPattern = @"guid:\s*([a-f0-9]{32})";
                var matches = System.Text.RegularExpressions.Regex.Matches(yaml, guidPattern);
                bool modified = false;

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var oldGuid = match.Groups[1].Value;
                    // 이 GUID가 유효한지 확인
                    var testPath = AssetDatabase.GUIDToAssetPath(oldGuid);
                    if (string.IsNullOrEmpty(testPath))
                    {
                        // 깨진 GUID - 같은 디렉토리의 후보로 교체
                        if (candidates.Count > 0)
                        {
                            var newGuid = candidates.Keys.First();
                            yaml = yaml.Replace($"guid: {oldGuid}", $"guid: {newGuid}");
                            modified = true;
                            Debug.LogWarning(
                                $"[EntityBlueprintAssetPostprocessor] " +
                                $"깨진 GUID를 복구했습니다: {oldGuid} -> {newGuid} in {assetPath}"
                            );
                        }
                    }
                }

                // _binders의 managed reference 재직렬화
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
                    $"YAML 수정 중 오류 발생: {assetPath}\n{ex}"
                );
            }

            return false;
        }

        /// <summary>
        /// _binders 섹션의 managed reference들을 재직렬화합니다.
        /// 타입 정보를 추출하여 새 인스턴스를 생성하고 재직렬화합니다.
        /// </summary>
        static bool ReserializeBindersSection(string yaml, string assetPath, out string repairedYaml)
        {
            repairedYaml = yaml;
            
            try
            {
                // _binders 섹션이 있는지 확인
                var bindersStart = yaml.IndexOf("  _binders:");
                if (bindersStart < 0)
                    return false;

                // 줄 단위로 파싱하여 정확한 위치 찾기
                var lines = yaml.Split('\n');
                int bindersLineIndex = -1;
                int bindersEndLineIndex = lines.Length;

                // _binders 라인 찾기
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

                // _binders 섹션 전체 추출
                var bindersSection = string.Join("\n", lines, bindersLineIndex, bindersEndLineIndex - bindersLineIndex);

                // 타입 정보 추출 및 재직렬화
                var reserializedSection = ReserializeManagedReferences(bindersSection, assetPath);
                if (reserializedSection != null && reserializedSection != bindersSection)
                {
                    // 재직렬화된 섹션으로 교체
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
                        $"_binders의 managed reference를 재직렬화했습니다: {assetPath}"
                    );
                    return true;
                }

                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"[EntityBlueprintAssetPostprocessor] " +
                    $"_binders 재직렬화 중 오류 발생: {assetPath}\n{ex}"
                );
                return false;
            }
        }

        /// <summary>
        /// Managed reference들을 재직렬화합니다.
        /// </summary>
        static string? ReserializeManagedReferences(string bindersSection, string assetPath)
        {
            try
            {
                // RefIds 섹션에서 타입 정보 추출
                var refIdsStart = bindersSection.IndexOf("RefIds:");
                if (refIdsStart < 0)
                    return null;

                var refIdsSection = bindersSection.Substring(refIdsStart);
                
                // 타입 정보 패턴: type: {class: ClassName, ns: Namespace, asm: AssemblyName}
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

                // 타입 정보 유효성 검증
                // 실제 재직렬화는 Unity가 asset을 로드한 후 SerializedObject를 사용하여 수행
                bool allTypesValid = true;
                foreach (var (className, namespaceName, assemblyName) in binderTypes)
                {
                    // Assembly-qualified name 구성
                    var typeName = string.IsNullOrEmpty(namespaceName)
                        ? $"{className}, {assemblyName}"
                        : $"{namespaceName}.{className}, {assemblyName}";

                    var type = System.Type.GetType(typeName, false);
                    if (type == null)
                    {
                        // 모든 어셈블리에서 찾기
                        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                        {
                            type = asm.GetType(typeName, false);
                            if (type != null) break;
                            
                            // namespace.class 형식으로도 시도
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
                            $"타입을 찾을 수 없습니다: {typeName} in {assetPath}"
                        );
                    }
                }

                // 타입이 모두 유효하면 Unity가 로드한 후 재직렬화하도록 원본 유지
                // 타입이 유효하지 않으면 빈 배열로 교체
                if (!allTypesValid)
                {
                    return "_binders: []";
                }

                // 타입이 유효하므로 Unity가 재직렬화할 수 있도록 원본 유지
                return null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"[EntityBlueprintAssetPostprocessor] " +
                    $"Managed reference 재직렬화 중 오류: {assetPath}\n{ex}"
                );
                return null;
            }
        }

        /// <summary>
        /// _binders 섹션의 깨진 managed reference를 복구합니다 (fallback).
        /// </summary>
        static bool RepairBindersSection(string yaml, string assetPath, out string repairedYaml)
        {
            repairedYaml = yaml;
            
            try
            {
                // _binders 섹션이 있는지 확인
                var bindersStart = yaml.IndexOf("  _binders:");
                if (bindersStart < 0)
                    return false;

                // 줄 단위로 파싱하여 정확한 위치 찾기
                var lines = yaml.Split('\n');
                int bindersLineIndex = -1;
                int bindersEndLineIndex = lines.Length;

                // _binders 라인 찾기
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].TrimStart().StartsWith("_binders:"))
                    {
                        bindersLineIndex = i;
                        
                        // _binders 섹션의 끝 찾기
                        // 다음 필드(같은 들여쓰기 레벨)를 찾거나 파일 끝까지
                        int indentLevel = GetIndentLevel(lines[i]);
                        for (int j = i + 1; j < lines.Length; j++)
                        {
                            var line = lines[j];
                            
                            // 빈 줄이나 주석은 건너뛰기
                            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                                continue;
                            
                            var lineIndent = GetIndentLevel(line);
                            // 같은 또는 더 작은 들여쓰기 레벨의 필드가 나오면 끝
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

                // _binders 섹션 전체 추출
                var bindersSection = string.Join("\n", lines, bindersLineIndex, bindersEndLineIndex - bindersLineIndex);

                // 타입 정보 추출 및 재직렬화 시도
                var reserializedSection = ReserializeManagedReferences(bindersSection, assetPath);
                if (reserializedSection != null && reserializedSection != bindersSection)
                {
                    // 재직렬화된 섹션으로 교체
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
                        $"_binders의 managed reference를 재직렬화했습니다: {assetPath}"
                    );
                    return true;
                }

                // 재직렬화할 수 없으면 타입 정보가 유효한지만 확인
                // Unity가 asset을 로드한 후 재직렬화하도록 함
                return false;
            }
            catch (System.Exception ex)
            {
                // 에러가 발생하면 안전하게 빈 배열로 교체
                Debug.LogError(
                    $"[EntityBlueprintAssetPostprocessor] " +
                    $"_binders 복구 중 오류 발생, 빈 배열로 교체: {assetPath}\n{ex}"
                );
                
                // 에러가 발생해도 빈 배열로 교체하여 성공 처리
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
                            // 다음 필드 찾기
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
                    // 최후의 수단: 전체 _binders 섹션을 찾아서 교체
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
        /// 들여쓰기 레벨을 반환합니다 (공백 개수).
        /// </summary>
        static int GetIndentLevel(string line)
        {
            int indent = 0;
            foreach (char c in line)
            {
                if (c == ' ')
                    indent++;
                else if (c == '\t')
                    indent += 4; // 탭을 4칸으로 간주
                else
                    break;
            }
            return indent;
        }

        /// <summary>
        /// _binders 섹션을 빈 배열로 교체합니다.
        /// </summary>
        static string ReplaceBindersSection(string[] lines, int bindersLineIndex, int bindersEndLineIndex)
        {
            var beforeBinders = bindersLineIndex > 0 
                ? string.Join("\n", lines, 0, bindersLineIndex) 
                : "";
            var afterBinders = bindersEndLineIndex < lines.Length 
                ? string.Join("\n", lines, bindersEndLineIndex, lines.Length - bindersEndLineIndex)
                : "";
            
            // 들여쓰기 레벨 확인
            var indent = lines[bindersLineIndex].Substring(0, GetIndentLevel(lines[bindersLineIndex]));
            
            if (string.IsNullOrEmpty(beforeBinders))
                return indent + "_binders: []" + (string.IsNullOrEmpty(afterBinders) ? "" : "\n" + afterBinders);
            else
                return beforeBinders + "\n" + indent + "_binders: []" + (string.IsNullOrEmpty(afterBinders) ? "" : "\n" + afterBinders);
        }

        /// <summary>
        /// Unity의 managedReferenceFullTypename을 표준 assembly-qualified name 형식으로 변환합니다.
        /// Unity 형식: "AssemblyName Namespace.ClassName"
        /// 표준 형식: "Namespace.ClassName, AssemblyName"
        /// </summary>
        static string NormalizeManagedReferenceTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return typeName;

            // 이미 표준 형식인지 확인 (쉼표가 있으면 표준 형식)
            if (typeName.Contains(","))
                return typeName;

            // Unity 형식 파싱: "AssemblyName Namespace.ClassName"
            var parts = typeName.Split(new[] { ' ' }, 2, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var assemblyName = parts[0];
                var fullClassName = parts[1];
                return $"{fullClassName}, {assemblyName}";
            }

            // 파싱 실패 시 원본 반환
            return typeName;
        }

        /// <summary>
        /// 타입 이름에서 클래스명만 추출합니다.
        /// </summary>
        static string ExtractClassName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return typeName;

            // "Namespace.ClassName" 또는 "ClassName" 형식에서 클래스명 추출
            var lastDot = typeName.LastIndexOf('.');
            if (lastDot >= 0 && lastDot < typeName.Length - 1)
            {
                return typeName.Substring(lastDot + 1);
            }

            // 공백으로 분리된 경우 (Unity 형식)
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
        /// 객체의 필드 값을 복사합니다 (shallow copy).
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
                        // 필드 복사 실패는 무시
                    }
                }
            }
            catch
            {
                // 복사 실패는 무시
            }
        }

        /// <summary>
        /// EntityBlueprint의 깨진 참조를 복구합니다.
        /// </summary>
        static void RepairBrokenReferences(EntityBlueprint blueprint, string blueprintPath)
        {
            // Unity가 asset을 로드할 수 없는 경우 (YAML 파싱 실패 등)는 건너뜀
            if (blueprint == null)
                return;

            bool needsRepair = false;
            var blueprintDir = Path.GetDirectoryName(blueprintPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(blueprintDir))
                return;

            // SerializedObject를 사용하여 참조 수정
            var so = new SerializedObject(blueprint);
            so.Update();

            // _contextAssets 복구
            var contextsProp = so.FindProperty("_contextAssets");
            if (contextsProp != null && contextsProp.isArray)
            {
                // 같은 디렉토리의 모든 ContextAsset 찾기
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

                // 깨진 참조 복구
                for (int i = 0; i < contextsProp.arraySize; i++)
                {
                    var elem = contextsProp.GetArrayElementAtIndex(i);
                    var currentAsset = elem.objectReferenceValue as ContextAsset;

                    if (currentAsset == null)
                    {
                        // null 참조는 제거하거나, 후보가 있으면 복구
                        if (candidates.Count > i)
                        {
                            // 인덱스에 맞는 후보 사용
                            elem.objectReferenceValue = candidates[i];
                            needsRepair = true;
                        }
                        else if (candidates.Count > 0)
                        {
                            // 후보가 있으면 첫 번째 사용
                            elem.objectReferenceValue = candidates[0];
                            needsRepair = true;
                        }
                        else
                        {
                            // 후보가 없으면 null로 유지 (나중에 수동으로 할당)
                        }
                    }
                }
            }

            // _binders의 모든 managed reference 재직렬화
            var bindersProp = so.FindProperty("_binders");
            if (bindersProp != null && bindersProp.isArray)
            {
                // 모든 managed reference를 재직렬화 (null이 아니어도)
                for (int i = 0; i < bindersProp.arraySize; i++)
                {
                    var elem = bindersProp.GetArrayElementAtIndex(i);
                    var currentBinder = elem.managedReferenceValue;
                    var typeName = elem.managedReferenceFullTypename;
                    
                    // 타입 정보가 있으면 재직렬화
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        // Unity의 managedReferenceFullTypename 형식 변환
                        // 형식: "AssemblyName Namespace.ClassName" -> "Namespace.ClassName, AssemblyName"
                        var normalizedTypeName = NormalizeManagedReferenceTypeName(typeName);
                        
                        var type = System.Type.GetType(normalizedTypeName, false);
                        if (type == null)
                        {
                            // 모든 어셈블리에서 찾기
                            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                            {
                                type = asm.GetType(normalizedTypeName, false);
                                if (type != null) break;
                                
                                // 원본 타입 이름으로도 시도
                                type = asm.GetType(typeName, false);
                                if (type != null) break;
                                
                                // 네임스페이스 없이 클래스명만으로도 시도
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
                                // 새 인스턴스 생성하여 재직렬화
                                var newInstance = ZenECS.Core.ZenDefaults.CreateWithDefaults(type);
                                if (newInstance != null)
                                {
                                    // 현재 값이 있으면 필드 값 복사 (shallow copy)
                                    if (currentBinder != null)
                                    {
                                        CopyFields(currentBinder, newInstance, type);
                                    }
                                    
                                    elem.managedReferenceValue = newInstance;
                                    needsRepair = true;
                                    Debug.LogWarning(
                                        $"[EntityBlueprintAssetPostprocessor] " +
                                        $"{blueprintPath}의 binder[{i}]를 재직렬화했습니다: {typeName}"
                                    );
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogWarning(
                                    $"[EntityBlueprintAssetPostprocessor] " +
                                    $"{blueprintPath}의 binder[{i}] 재직렬화 실패: {typeName}, {ex.Message}"
                                );
                            }
                        }
                        else
                        {
                            // 타입을 찾을 수 없으면 제거
                            bindersProp.DeleteArrayElementAtIndex(i);
                            needsRepair = true;
                            Debug.LogWarning(
                                $"[EntityBlueprintAssetPostprocessor] " +
                                $"{blueprintPath}의 binder[{i}] 타입을 찾을 수 없어 제거했습니다: {typeName}"
                            );
                            i--; // 인덱스 조정
                        }
                    }
                    else if (currentBinder == null)
                    {
                        // 타입 정보도 없고 값도 null이면 제거
                        bindersProp.DeleteArrayElementAtIndex(i);
                        needsRepair = true;
                        Debug.LogWarning(
                            $"[EntityBlueprintAssetPostprocessor] " +
                            $"{blueprintPath}의 binder[{i}]가 null이고 타입 정보도 없어 제거했습니다."
                        );
                        i--; // 인덱스 조정
                    }
                }
            }

            if (needsRepair)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(blueprint);
                // SaveAssets는 호출하지 않음 (Unity가 자동으로 처리)
                Debug.LogWarning(
                    $"[EntityBlueprintAssetPostprocessor] 깨진 참조를 복구했습니다: {blueprintPath}"
                );
            }
        }
    }
}
#endif
