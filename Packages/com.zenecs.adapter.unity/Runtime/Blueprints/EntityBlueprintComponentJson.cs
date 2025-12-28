// ──────────────────────────────────────────────────────────────────────────────
// ZenECS Adapter.Unity — Blueprints
// File: EntityBlueprintComponentJson.cs
// Purpose: JSON (JsonUtility) serializer/deserializer for component snapshots
//          used by EntityBlueprintData entries.
// Key concepts:
//   • Reflection-based snapshot of public instance fields.
//   • Supports UnityEngine types, Unity.Mathematics types, FixedString64Bytes.
//   • Robust deserialization with safe default construction via ZenDefaults.
//   • Used to store component data as JSON in blueprint entries.
// Copyright (c) 2026 Pippapips Limited
// License: MIT (https://opensource.org/licenses/MIT)
// SPDX-License-Identifier: MIT
// ──────────────────────────────────────────────────────────────────────────────
#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using ZenECS.Core;

namespace ZenECS.Adapter.Unity.Blueprints
{
    /// <summary>
    /// Utility for serializing and deserializing component snapshots as JSON.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="EntityBlueprintComponentJson"/> converts component instances
    /// into a lightweight JSON snapshot format that is compatible with
    /// <see cref="JsonUtility"/>. The snapshot stores:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The component's assembly-qualified type name.</description></item>
    /// <item><description>A list of public instance fields and their values.</description></item>
    /// </list>
    /// <para>
    /// The format supports a subset of commonly used field types, including:
    /// primitive numeric and boolean types, strings, <see cref="FixedString64Bytes"/>,
    /// Unity vector/quaternion/color types, <see cref="GameObject"/>, and
    /// several <c>Unity.Mathematics</c> structs.
    /// </para>
    /// </remarks>
    public static class EntityBlueprintComponentJson
    {
        #region Snapshot DTOs

        /// <summary>
        /// Snapshot DTO that represents a single component instance.
        /// </summary>
        [Serializable]
        private class Snapshot
        {
            /// <summary>
            /// Assembly-qualified type name of the component being serialized.
            /// </summary>
            public string? typeName;

            /// <summary>
            /// Serialized field records for the component.
            /// </summary>
            public List<Field> fields = new();
        }

        /// <summary>
        /// Snapshot DTO that represents a single field value.
        /// </summary>
        [Serializable]
        private class Field
        {
            /// <summary>
            /// Name of the field on the component type.
            /// </summary>
            public string? name;

            /// <summary>
            /// Discriminator string indicating which value slot is used
            /// (for example, <c>"int"</c>, <c>"vec3"</c>, <c>"fs64"</c>).
            /// </summary>
            public string? kind;

            // Primitive and string
            public int i;
            public float f;
            public bool b;
            public string? s;

            // UnityEngine types
            public Vector2 v2;
            public Vector3 v3;
            public Vector4 v4;
            public Quaternion q;
            public Color color;

            // Unity.Mathematics mapped types
            public Float2 f2m;
            public Float3 f3m;
            public Float4 f4m;
            public Int2 i2m;
            public Int3 i3m;
            public Int4 i4m;
            public UInt2 u2m;
            public UInt3 u3m;
            public UInt4 u4m;
            public Bool2 b2m;
            public Bool3 b3m;
            public Bool4 b4m;
            public Float4 q4m; // quaternion as float4

            // UnityEngine.Object reference
            public GameObject? go;
        }

        /// <summary>
        /// Serializable representation of a <c>float2</c>-like value.
        /// </summary>
        [Serializable] public struct Float2 { public float x, y; }

        /// <summary>
        /// Serializable representation of a <c>float3</c>-like value.
        /// </summary>
        [Serializable] public struct Float3 { public float x, y, z; }

        /// <summary>
        /// Serializable representation of a <c>float4</c>- or quaternion-like value.
        /// </summary>
        [Serializable] public struct Float4 { public float x, y, z, w; }

        /// <summary>
        /// Serializable representation of an <c>int2</c>-like value.
        /// </summary>
        [Serializable] public struct Int2 { public int x, y; }

        /// <summary>
        /// Serializable representation of an <c>int3</c>-like value.
        /// </summary>
        [Serializable] public struct Int3 { public int x, y, z; }

        /// <summary>
        /// Serializable representation of an <c>int4</c>-like value.
        /// </summary>
        [Serializable] public struct Int4 { public int x, y, z, w; }

        /// <summary>
        /// Serializable representation of a <c>uint2</c>-like value.
        /// </summary>
        [Serializable] public struct UInt2 { public uint x, y; }

        /// <summary>
        /// Serializable representation of a <c>uint3</c>-like value.
        /// </summary>
        [Serializable] public struct UInt3 { public uint x, y, z; }

        /// <summary>
        /// Serializable representation of a <c>uint4</c>-like value.
        /// </summary>
        [Serializable] public struct UInt4 { public uint x, y, z, w; }

        /// <summary>
        /// Serializable representation of a <c>bool2</c>-like value.
        /// </summary>
        [Serializable] public struct Bool2 { public bool x, y; }

        /// <summary>
        /// Serializable representation of a <c>bool3</c>-like value.
        /// </summary>
        [Serializable] public struct Bool3 { public bool x, y, z; }

        /// <summary>
        /// Serializable representation of a <c>bool4</c>-like value.
        /// </summary>
        [Serializable] public struct Bool4 { public bool x, y, z, w; }

        #endregion

        /// <summary>
        /// Serializes a component instance into a JSON snapshot string.
        /// </summary>
        /// <param name="instance">
        /// Component instance to serialize. Expected to be a struct or class
        /// containing public instance fields.
        /// </param>
        /// <param name="t">
        /// Runtime type of the component instance.
        /// </param>
        /// <returns>
            /// A JSON string that encodes a <see cref="Snapshot"/> object,
        /// suitable for storage in <see cref="EntityBlueprintData"/>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Unsupported field types are silently skipped. The method only
        /// inspects public instance fields (no properties or non-public fields).
        /// </para>
        /// </remarks>
        public static string Serialize(object instance, Type t)
        {
            var snap = new Snapshot { typeName = t.AssemblyQualifiedName };
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var ft = f.FieldType;
                var val = f.GetValue(instance);
                var rec = new Field { name = f.Name };

                if (ft == typeof(int)) { rec.kind = "int"; rec.i = val is int i ? i : 0; }
                else if (ft == typeof(float)) { rec.kind = "float"; rec.f = val is float fl ? fl : 0f; }
                else if (ft == typeof(bool)) { rec.kind = "bool"; rec.b = val is bool bo && bo; }

                // byte: reuse integer slot
                else if (ft == typeof(byte)) { rec.kind = "byte"; rec.i = val is byte by ? by : 0; }

                else if (ft == typeof(string)) { rec.kind = "string"; rec.s = val as string ?? ""; }

                // FixedString64Bytes: stored as string
                else if (ft == typeof(FixedString64Bytes))
                {
                    rec.kind = "fs64";
                    rec.s = val is FixedString64Bytes fs ? fs.ToString() : string.Empty;
                }

                else if (ft == typeof(Vector2)) { rec.kind = "vec2"; rec.v2 = val is Vector2 v2 ? v2 : default; }
                else if (ft == typeof(Vector3)) { rec.kind = "vec3"; rec.v3 = val is Vector3 v3 ? v3 : default; }
                else if (ft == typeof(Vector4)) { rec.kind = "vec4"; rec.v4 = val is Vector4 v4 ? v4 : default; }
                else if (ft == typeof(Quaternion)) { rec.kind = "quat"; rec.q = val is Quaternion q ? q : Quaternion.identity; }
                else if (ft == typeof(Color)) { rec.kind = "color"; rec.color = val is Color c ? c : Color.white; }
                else if (ft == typeof(GameObject)) { rec.kind = "go"; rec.go = val is GameObject go ? go : null; }
                else if (IsMath(ft, "Unity.Mathematics.float2")) { rec.kind = "f2m"; rec.f2m = ToF2(val, ft); }
                else if (IsMath(ft, "Unity.Mathematics.float3")) { rec.kind = "f3m"; rec.f3m = ToF3(val, ft); }
                else if (IsMath(ft, "Unity.Mathematics.float4")) { rec.kind = "f4m"; rec.f4m = ToF4(val, ft); }
                else if (IsMath(ft, "Unity.Mathematics.int2")) { rec.kind = "i2m"; rec.i2m = ToI2(val, ft); }
                else if (IsMath(ft, "Unity.Mathematics.int3")) { rec.kind = "i3m"; rec.i3m = ToI3(val, ft); }
                else if (IsMath(ft, "Unity.Mathematics.int4")) { rec.kind = "i4m"; rec.i4m = ToI4(val, ft); }
                else if (IsMath(ft, "Unity.Mathematics.uint2")) { rec.kind = "u2m"; rec.u2m = ToU2(val, ft); }
                else if (IsMath(ft, "Unity.Mathematics.uint3")) { rec.kind = "u3m"; rec.u3m = ToU3(val, ft); }
                else if (IsMath(ft, "Unity.Mathematics.uint4")) { rec.kind = "u4m"; rec.u4m = ToU4(val, ft); }
                else if (IsMath(ft, "Unity.Mathematics.bool2")) { rec.kind = "b2m"; rec.b2m = ToB2(val, ft); }
                else if (IsMath(ft, "Unity.Mathematics.bool3")) { rec.kind = "b3m"; rec.b3m = ToB3(val, ft); }
                else if (IsMath(ft, "Unity.Mathematics.bool4")) { rec.kind = "b4m"; rec.b4m = ToB4(val, ft); }
                else if (IsMath(ft, "Unity.Mathematics.quaternion")) { rec.kind = "q4m"; rec.q4m = ToQuat4(val, ft); }
                else continue; // unsupported types are skipped

                snap.fields.Add(rec);
            }
            return JsonUtility.ToJson(snap, true);
        }

        /// <summary>
        /// Deserializes a JSON snapshot string back into a component instance.
        /// </summary>
        /// <param name="json">
        /// JSON text produced by <see cref="Serialize"/>, or <c>null</c> to
        /// return a default instance.
        /// </param>
        /// <param name="t">
        /// Target component type to construct.
        /// </param>
        /// <returns>
        /// A new object of type <paramref name="t"/> with supported public
        /// fields populated from <paramref name="json"/>, or a default
        /// instance when <paramref name="json"/> is <c>null</c> or invalid.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The method uses <see cref="ZenDefaults.CreateWithDefaults"/> to
        /// construct the instance, then attempts to restore each field. Any
        /// individual field failures are caught and ignored so that a partially
        /// valid snapshot still yields a usable component.
        /// </para>
        /// </remarks>
        public static object? Deserialize(string? json, Type t)
        {
            if (json == null) return null;

            // Safe creation path (avoids direct Activator-based defaulting).
            var inst = ZenDefaults.CreateWithDefaults(t);
            if (string.IsNullOrWhiteSpace(json)) return inst;

            Snapshot? snap = null;
            try { snap = JsonUtility.FromJson<Snapshot>(json); }
            catch
            {
                // If parsing fails, return a default-constructed instance.
            }
            if (snap?.fields == null) return inst;

            foreach (var rec in snap.fields)
            {
                if (rec.name == null) continue;

                var f = t.GetField(rec.name, BindingFlags.Public | BindingFlags.Instance);
                if (f == null) continue;
                var ft = f.FieldType;

                try
                {
                    switch (rec.kind)
                    {
                        case "int":   if (ft == typeof(int))   f.SetValue(inst, rec.i); break;
                        case "float": if (ft == typeof(float)) f.SetValue(inst, rec.f); break;
                        case "bool":  if (ft == typeof(bool))  f.SetValue(inst, rec.b); break;

                        // byte: stored as int, restored by cast
                        case "byte":  if (ft == typeof(byte))  f.SetValue(inst, (byte)rec.i); break;

                        case "string":
                            if (ft == typeof(string)) f.SetValue(inst, rec.s ?? "");
                            break;

                        // FixedString64Bytes restoration
                        case "fs64":
                            if (ft == typeof(FixedString64Bytes))
                                f.SetValue(inst, new FixedString64Bytes(rec.s ?? string.Empty));
                            break;

                        case "vec2": if (ft == typeof(Vector2)) f.SetValue(inst, rec.v2); break;
                        case "vec3": if (ft == typeof(Vector3)) f.SetValue(inst, rec.v3); break;
                        case "vec4": if (ft == typeof(Vector4)) f.SetValue(inst, rec.v4); break;
                        case "quat": if (ft == typeof(Quaternion)) f.SetValue(inst, rec.q); break;
                        case "color": if (ft == typeof(Color)) f.SetValue(inst, rec.color); break;
                        case "go":   if (ft == typeof(GameObject)) f.SetValue(inst, rec.go); break;

                        case "f2m": if (IsMath(ft, "Unity.Mathematics.float2")) f.SetValue(inst, FromF2(ft, rec.f2m)); break;
                        case "f3m": if (IsMath(ft, "Unity.Mathematics.float3")) f.SetValue(inst, FromF3(ft, rec.f3m)); break;
                        case "f4m": if (IsMath(ft, "Unity.Mathematics.float4")) f.SetValue(inst, FromF4(ft, rec.f4m)); break;
                        case "i2m": if (IsMath(ft, "Unity.Mathematics.int2")) f.SetValue(inst, FromI2(ft, rec.i2m)); break;
                        case "i3m": if (IsMath(ft, "Unity.Mathematics.int3")) f.SetValue(inst, FromI3(ft, rec.i3m)); break;
                        case "i4m": if (IsMath(ft, "Unity.Mathematics.int4")) f.SetValue(inst, FromI4(ft, rec.i4m)); break;
                        case "u2m": if (IsMath(ft, "Unity.Mathematics.uint2")) f.SetValue(inst, FromU2(ft, rec.u2m)); break;
                        case "u3m": if (IsMath(ft, "Unity.Mathematics.uint3")) f.SetValue(inst, FromU3(ft, rec.u3m)); break;
                        case "u4m": if (IsMath(ft, "Unity.Mathematics.uint4")) f.SetValue(inst, FromU4(ft, rec.u4m)); break;
                        case "b2m": if (IsMath(ft, "Unity.Mathematics.bool2")) f.SetValue(inst, FromB2(ft, rec.b2m)); break;
                        case "b3m": if (IsMath(ft, "Unity.Mathematics.bool3")) f.SetValue(inst, FromB3(ft, rec.b3m)); break;
                        case "b4m": if (IsMath(ft, "Unity.Mathematics.bool4")) f.SetValue(inst, FromB4(ft, rec.b4m)); break;
                        case "q4m": if (IsMath(ft, "Unity.Mathematics.quaternion")) f.SetValue(inst, FromQuat4(ft, rec.q4m)); break;
                    }
                }
                catch
                {
                    // Ignore field-level failures and continue with the rest.
                }
            }

            return inst;
        }

        /// <summary>
        /// Checks whether a type matches a specific fully qualified type name.
        /// </summary>
        private static bool IsMath(Type? t, string fullName) => t != null && t.FullName == fullName;

        // Conversion helpers (Unity.Mathematics -> DTO)

        private static Float2 ToF2(object? v, Type ft) => v == null
            ? default
            : new Float2 {
                x = (float)(ft.GetField("x").GetValue(v) ?? 0f),
                y = (float)(ft.GetField("y").GetValue(v) ?? 0f),
            };

        private static Float3 ToF3(object? v, Type ft) => v == null
            ? default
            : new Float3 {
                x = (float)(ft.GetField("x").GetValue(v) ?? 0f),
                y = (float)(ft.GetField("y").GetValue(v) ?? 0f),
                z = (float)(ft.GetField("z").GetValue(v) ?? 0f),
            };

        private static Float4 ToF4(object? v, Type ft) => v == null
            ? default
            : new Float4 {
                x = (float)(ft.GetField("x").GetValue(v) ?? 0f),
                y = (float)(ft.GetField("y").GetValue(v) ?? 0f),
                z = (float)(ft.GetField("z").GetValue(v) ?? 0f),
                w = (float)(ft.GetField("w").GetValue(v) ?? 0f),
            };

        private static Int2 ToI2(object? v, Type ft) => v == null
            ? default
            : new Int2 {
                x = (int)(ft.GetField("x").GetValue(v) ?? 0),
                y = (int)(ft.GetField("y").GetValue(v) ?? 0),
            };

        private static Int3 ToI3(object? v, Type ft) => v == null
            ? default
            : new Int3 {
                x = (int)(ft.GetField("x").GetValue(v) ?? 0),
                y = (int)(ft.GetField("y").GetValue(v) ?? 0),
                z = (int)(ft.GetField("z").GetValue(v) ?? 0),
            };

        private static Int4 ToI4(object? v, Type ft) => v == null
            ? default
            : new Int4 {
                x = (int)(ft.GetField("x").GetValue(v) ?? 0),
                y = (int)(ft.GetField("y").GetValue(v) ?? 0),
                z = (int)(ft.GetField("z").GetValue(v) ?? 0),
                w = (int)(ft.GetField("w").GetValue(v) ?? 0),
            };

        private static UInt2 ToU2(object? v, Type ft) => v == null
            ? default
            : new UInt2 {
                x = (uint)(ft.GetField("x").GetValue(v) ?? 0u),
                y = (uint)(ft.GetField("y").GetValue(v) ?? 0u),
            };

        private static UInt3 ToU3(object? v, Type ft) => v == null
            ? default
            : new UInt3 {
                x = (uint)(ft.GetField("x").GetValue(v) ?? 0u),
                y = (uint)(ft.GetField("y").GetValue(v) ?? 0u),
                z = (uint)(ft.GetField("z").GetValue(v) ?? 0u),
            };

        private static UInt4 ToU4(object? v, Type ft) => v == null
            ? default
            : new UInt4 {
                x = (uint)(ft.GetField("x").GetValue(v) ?? 0u),
                y = (uint)(ft.GetField("y").GetValue(v) ?? 0u),
                z = (uint)(ft.GetField("z").GetValue(v) ?? 0u),
                w = (uint)(ft.GetField("w").GetValue(v) ?? 0u),
            };

        private static Bool2 ToB2(object? v, Type ft) => v == null
            ? default
            : new Bool2 {
                x = (bool)(ft.GetField("x").GetValue(v) ?? false),
                y = (bool)(ft.GetField("y").GetValue(v) ?? false),
            };

        private static Bool3 ToB3(object? v, Type ft) => v == null
            ? default
            : new Bool3 {
                x = (bool)(ft.GetField("x").GetValue(v) ?? false),
                y = (bool)(ft.GetField("y").GetValue(v) ?? false),
                z = (bool)(ft.GetField("z").GetValue(v) ?? false),
            };

        private static Bool4 ToB4(object? v, Type ft) => v == null
            ? default
            : new Bool4 {
                x = (bool)(ft.GetField("x").GetValue(v) ?? false),
                y = (bool)(ft.GetField("y").GetValue(v) ?? false),
                z = (bool)(ft.GetField("z").GetValue(v) ?? false),
                w = (bool)(ft.GetField("w").GetValue(v) ?? false),
            };

        private static Float4 ToQuat4(object? v, Type ft)
        {
            if (v == null) return new Float4 { x = 0, y = 0, z = 0, w = 1 };

            var fValue = ft.GetField("value", BindingFlags.Public | BindingFlags.Instance);
            if (fValue != null)
            {
                var val = fValue.GetValue(v);
                var t4 = val?.GetType();
                return new Float4 {
                    x = (float)(t4?.GetField("x")?.GetValue(val) ?? 0f),
                    y = (float)(t4?.GetField("y")?.GetValue(val) ?? 0f),
                    z = (float)(t4?.GetField("z")?.GetValue(val) ?? 0f),
                    w = (float)(t4?.GetField("w")?.GetValue(val) ?? 1f),
                };
            }

            return new Float4 {
                x = (float)(ft.GetField("x")?.GetValue(v) ?? 0f),
                y = (float)(ft.GetField("y")?.GetValue(v) ?? 0f),
                z = (float)(ft.GetField("z")?.GetValue(v) ?? 0f),
                w = (float)(ft.GetField("w")?.GetValue(v) ?? 1f),
            };
        }

        // Conversion helpers (DTO -> Unity.Mathematics-like instances)

        private static object FromF2(Type ft, Float2 v)
        {
            var o = Activator.CreateInstance(ft);
            ft.GetField("x")?.SetValue(o, v.x);
            ft.GetField("y")?.SetValue(o, v.y);
            return o;
        }

        private static object FromF3(Type ft, Float3 v)
        {
            var o = Activator.CreateInstance(ft);
            ft.GetField("x")?.SetValue(o, v.x);
            ft.GetField("y")?.SetValue(o, v.y);
            ft.GetField("z")?.SetValue(o, v.z);
            return o;
        }

        private static object FromF4(Type ft, Float4 v)
        {
            var o = Activator.CreateInstance(ft);
            ft.GetField("x")?.SetValue(o, v.x);
            ft.GetField("y")?.SetValue(o, v.y);
            ft.GetField("z")?.SetValue(o, v.z);
            ft.GetField("w")?.SetValue(o, v.w);
            return o;
        }

        private static object FromI2(Type ft, Int2 v)
        {
            var o = Activator.CreateInstance(ft);
            ft.GetField("x")?.SetValue(o, v.x);
            ft.GetField("y")?.SetValue(o, v.y);
            return o;
        }

        private static object FromI3(Type ft, Int3 v)
        {
            var o = Activator.CreateInstance(ft);
            ft.GetField("x")?.SetValue(o, v.x);
            ft.GetField("y")?.SetValue(o, v.y);
            ft.GetField("z")?.SetValue(o, v.z);
            return o;
        }

        private static object FromI4(Type ft, Int4 v)
        {
            var o = Activator.CreateInstance(ft);
            ft.GetField("x")?.SetValue(o, v.x);
            ft.GetField("y")?.SetValue(o, v.y);
            ft.GetField("z")?.SetValue(o, v.z);
            ft.GetField("w")?.SetValue(o, v.w);
            return o;
        }

        private static object FromU2(Type ft, UInt2 v)
        {
            var o = Activator.CreateInstance(ft);
            ft.GetField("x")?.SetValue(o, v.x);
            ft.GetField("y")?.SetValue(o, v.y);
            return o;
        }

        private static object FromU3(Type ft, UInt3 v)
        {
            var o = Activator.CreateInstance(ft);
            ft.GetField("x")?.SetValue(o, v.x);
            ft.GetField("y")?.SetValue(o, v.y);
            ft.GetField("z")?.SetValue(o, v.z);
            return o;
        }

        private static object FromU4(Type ft, UInt4 v)
        {
            var o = Activator.CreateInstance(ft);
            ft.GetField("x")?.SetValue(o, v.x);
            ft.GetField("y")?.SetValue(o, v.y);
            ft.GetField("z")?.SetValue(o, v.z);
            ft.GetField("w")?.SetValue(o, v.w);
            return o;
        }

        private static object FromB2(Type ft, Bool2 v)
        {
            var o = Activator.CreateInstance(ft);
            ft.GetField("x")?.SetValue(o, v.x);
            ft.GetField("y")?.SetValue(o, v.y);
            return o;
        }

        private static object FromB3(Type ft, Bool3 v)
        {
            var o = Activator.CreateInstance(ft);
            ft.GetField("x")?.SetValue(o, v.x);
            ft.GetField("y")?.SetValue(o, v.y);
            ft.GetField("z")?.SetValue(o, v.z);
            return o;
        }

        private static object FromB4(Type ft, Bool4 v)
        {
            var o = Activator.CreateInstance(ft);
            ft.GetField("x")?.SetValue(o, v.x);
            ft.GetField("y")?.SetValue(o, v.y);
            ft.GetField("z")?.SetValue(o, v.z);
            ft.GetField("w")?.SetValue(o, v.w);
            return o;
        }

        private static object FromQuat4(Type ft, Float4 v)
        {
            var o = Activator.CreateInstance(ft);
            var fValue = ft.GetField("value", BindingFlags.Public | BindingFlags.Instance);
            if (fValue != null)
            {
                var val = fValue.GetValue(o);
                var t4 = val?.GetType();
                t4?.GetField("x")?.SetValue(val, v.x);
                t4?.GetField("y")?.SetValue(val, v.y);
                t4?.GetField("z")?.SetValue(val, v.z);
                t4?.GetField("w")?.SetValue(val, v.w);
                fValue.SetValue(o, val);
            }
            else
            {
                ft.GetField("x")?.SetValue(o, v.x);
                ft.GetField("y")?.SetValue(o, v.y);
                ft.GetField("z")?.SetValue(o, v.z);
                ft.GetField("w")?.SetValue(o, v.w);
            }
            return o;
        }
    }
}
