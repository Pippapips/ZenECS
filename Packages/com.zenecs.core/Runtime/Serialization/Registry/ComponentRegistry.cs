// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Core — Serialization
// File: ComponentRegistry.cs
// Purpose: Maintain global mappings between StableId ↔ Type and Type ↔ Formatter.
// Key concepts:
//   • Snapshot resolution: used during save/load to map ids to types/formatters.
//   • Strictness: optional StableId validation for component ↔ formatter pairs.
//   • Editor aid: capture declared StableId from a formatter attribute (when available). 
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;

namespace ZenECS.Core.Serialization
{
    /// <summary>
    /// Global registry for component types and their formatters.
    /// Provides StableId ↔ <see cref="Type"/> and <see cref="Type"/> ↔ <see cref="IComponentFormatter"/> lookups.
    /// </summary>
    /// <remarks>
    /// Populate this registry during app boot (single-threaded), then treat it as read-only.
    /// </remarks>
    public static class ComponentRegistry
    {
        private static readonly Dictionary<string, Type> id2Type = new();
        private static readonly Dictionary<Type, string> type2Id = new();
        private static readonly Dictionary<Type, IComponentFormatter> formatters = new();

        // FormatterType → declared StableId (captured via attribute or explicit overload)
        private static readonly Dictionary<Type, string> declaredSidByFormatterType = new();

        /// <summary>Try resolve a component <see cref="Type"/> from its StableId.</summary>
        public static bool TryGetType(string id, out Type? t) => id2Type.TryGetValue(id, out t);

        /// <summary>Try resolve a StableId string for the given component <see cref="Type"/>.</summary>
        public static bool TryGetId(Type t, out string? id) => type2Id.TryGetValue(t, out id);

        /// <summary>
        /// Register a StableId → component mapping for <typeparamref name="T"/>.
        /// </summary>
        public static void Register<T>(string stableId) where T : struct
            => Register(stableId, typeof(T));

        /// <summary>
        /// Register a StableId → component mapping for a specific <see cref="Type"/>.
        /// Existing entries are overwritten.
        /// </summary>
        public static void Register(string stableId, Type type)
        {
            id2Type[stableId] = type;
            type2Id[type] = stableId;
        }

        /// <summary>
        /// Register a formatter instance and (in Editor) capture its declared StableId via attribute.
        /// </summary>
        /// <param name="f">Formatter instance.</param>
        public static void RegisterFormatter(IComponentFormatter f)
        {
            formatters[f.ComponentType] = f;
            TryCaptureFormatterStableIdFromAttribute(f.GetType(), out var sid);
            if (!string.IsNullOrEmpty(sid))
                declaredSidByFormatterType[f.GetType()] = sid!;
        }

        /// <summary>
        /// Register a formatter instance and explicitly provide its declared StableId.
        /// </summary>
        /// <param name="f">Formatter instance.</param>
        /// <param name="declaredStableId">StableId the formatter claims to serialize.</param>
        public static void RegisterFormatter(IComponentFormatter f, string declaredStableId)
        {
            RegisterFormatter(f);
            if (!string.IsNullOrEmpty(declaredStableId))
                declaredSidByFormatterType[f.GetType()] = declaredStableId;
        }

        /// <summary>
        /// Validate strict StableId consistency between components and registered formatters.
        /// </summary>
        /// <param name="throwOnError">Throw on first inconsistency; otherwise return issue count and log via <paramref name="log"/>.</param>
        /// <param name="log">Optional logger when <paramref name="throwOnError"/> is false.</param>
        /// <returns>Number of issues detected.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="throwOnError"/> is true and a mismatch/missing mapping is found.
        /// </exception>
        public static int ValidateStrictStableIdMatch(bool throwOnError = true, Action<string>? log = null)
        {
            log ??= msg => System.Diagnostics.Debug.WriteLine(msg);
            int issues = 0;

            foreach (var (compType, fmt) in formatters)
            {
                if (!type2Id.TryGetValue(compType, out var compSid) || string.IsNullOrEmpty(compSid))
                {
                    issues++;
                    var msg = $"Component '{compType.FullName}' has no registered StableId, but formatter '{fmt.GetType().FullName}' is registered.";
                    if (throwOnError) throw new InvalidOperationException(msg); else log(msg);
                    continue;
                }

                if (!declaredSidByFormatterType.TryGetValue(fmt.GetType(), out var fmtSid) || string.IsNullOrEmpty(fmtSid))
                {
                    issues++;
                    var msg = $"Formatter '{fmt.GetType().FullName}' has no declared StableId; cannot verify against component sid='{compSid}'.";
                    if (throwOnError) throw new InvalidOperationException(msg); else log(msg);
                    continue;
                }

                if (!string.Equals(compSid, fmtSid, StringComparison.Ordinal))
                {
                    issues++;
                    var msg = $"StableId mismatch: Component='{compType.FullName}' sid='{compSid}' vs Formatter='{fmt.GetType().FullName}' sid='{fmtSid}'.";
                    if (throwOnError) throw new InvalidOperationException(msg); else log(msg);
                }
            }

            return issues;
        }

        /// <summary>
        /// Try capture a formatter’s declared StableId from an editor-only attribute.
        /// </summary>
        /// <remarks>Effective only inside Unity Editor. In player builds this returns <see langword="false"/>.</remarks>
        private static bool TryCaptureFormatterStableIdFromAttribute(Type formatterType, out string? sid)
        {
            sid = null;
#if UNITY_EDITOR
            var attrs = formatterType.GetCustomAttributes(inherit: false);
            foreach (var a in attrs)
            {
                var at = a.GetType();
                if (at.Name == "ZenFormatterForAttribute" || at.FullName?.EndsWith(".ZenFormatterForAttribute") == true)
                {
                    var p = at.GetProperty("StableId", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (p != null && p.PropertyType == typeof(string))
                    {
                        sid = p.GetValue(a) as string;
                        if (!string.IsNullOrEmpty(sid)) return true;
                    }
                }
            }
#endif
            return false;
        }

        /// <summary>
        /// Get the registered formatter for component type <paramref name="t"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">No formatter registered for the type.</exception>
        public static IComponentFormatter GetFormatter(Type t)
        {
            if (!formatters.TryGetValue(t, out var f))
                throw new InvalidOperationException($"Formatter not registered for {t.FullName}");
            return f;
        }
    }
}
