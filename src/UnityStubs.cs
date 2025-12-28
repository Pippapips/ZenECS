// Unity Stubs for DocFX Build
// This file provides stub types for Unity APIs when building outside Unity environment
// These stubs are only used for documentation generation and do not affect runtime behavior
// In actual Unity builds, these stubs are ignored because Unity provides the real types

#nullable enable

namespace UnityEngine
{
    // Unity type hierarchy:
    // Object (base)
    //   - GameObject
    //   - Component
    //     - MonoBehaviour
    
    public class Object
    {
        public string name { get; set; } = "";
        
        public static T? FindObjectOfType<T>(bool includeInactive = false) where T : class => default(T);
        
        public static T? FindFirstObjectByType<T>(FindObjectsInactive findObjectsInactive) where T : class => default(T);
        
        public static void Destroy(Object? obj) { }
        public static void Destroy(GameObject? obj) { Destroy((Object?)obj); }
        public static void DestroyImmediate(Object? obj) { }
        public static void DestroyImmediate(GameObject? obj) { DestroyImmediate((Object?)obj); }
        
        public static void DontDestroyOnLoad(Object target) { }
        public static void DontDestroyOnLoad(GameObject target) { DontDestroyOnLoad((Object)target); }
        
        // Unity's null check operator
        public static bool operator !(Object? obj) => obj == null;
        public static bool operator true(Object? obj) => obj != null;
        public static bool operator false(Object? obj) => obj == null;
    }

    public class GameObject : Object
    {
        public GameObject(string name) { this.name = name; }
        public T AddComponent<T>() where T : class => default(T)!;
    }

    public class Component : Object
    {
        public GameObject? gameObject { get; set; }
    }

    public class MonoBehaviour : Component
    {
        protected MonoBehaviour() { }
    }

    public partial class ScriptableObject : Object
    {
        // ScriptableObject inherits name from Object
    }

    public class PropertyAttribute : System.Attribute
    {
    }

    public class HeaderAttribute : PropertyAttribute
    {
        public HeaderAttribute(string header) { }
    }

    public class TooltipAttribute : PropertyAttribute
    {
        public TooltipAttribute(string tooltip) { }
    }

    public class SerializeFieldAttribute : System.Attribute
    {
    }

    public class CreateAssetMenuAttribute : System.Attribute
    {
        public string? menuName { get; set; }
        public string? fileName { get; set; }
        public CreateAssetMenuAttribute() { }
    }

    public class DisallowMultipleComponentAttribute : System.Attribute
    {
    }

    public class DefaultExecutionOrderAttribute : System.Attribute
    {
        public DefaultExecutionOrderAttribute(int order) { }
    }

    public struct Vector2
    {
        public float x, y;
    }

    public struct Vector3
    {
        public float x, y, z;
    }

    public struct Vector4
    {
        public float x, y, z, w;
    }

    public struct Quaternion
    {
        public static Quaternion identity => new Quaternion();
    }

    public struct Color
    {
        public static Color white => new Color();
    }

    public enum RuntimeInitializeLoadType
    {
        BeforeSceneLoad
    }

    public class RuntimeInitializeOnLoadMethodAttribute : System.Attribute
    {
        public RuntimeInitializeLoadType loadType { get; set; }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType loadType)
        {
            this.loadType = loadType;
        }
    }

    public enum FindObjectsInactive
    {
        Include,
        Exclude
    }

    public static class Debug
    {
        public static void Log(object? message) { }
        public static void LogWarning(object? message) { }
        public static void LogWarning(object? message, Object? context) { }
        public static void LogError(object? message) { }
        public static void LogError(object? message, Object? context) { }
    }

    public static class Time
    {
        public static float deltaTime => 0f;
        public static float fixedDeltaTime => 0f;
    }

    public static class Application
    {
        public static bool isPlaying => false;
    }

    public static class JsonUtility
    {
        public static string ToJson(object obj, bool prettyPrint = false) => "{}";
        public static T? FromJson<T>(string json) where T : class => default(T);
        public static object? FromJson(string json, System.Type type) => null;
    }
    
    // Unity Editor-only methods referenced in XML comments
    public partial class ScriptableObject
    {
        /// <summary>
        /// Editor-only validation method called when values change in the inspector.
        /// </summary>
        protected virtual void OnValidate() { }
    }
}

// Zenject stubs for XML documentation (only referenced in comments, not compiled)
namespace Zenject
{
    using UnityEngine;
    
    /// <summary>
    /// Zenject dependency injection container (stub for documentation only).
    /// </summary>
    public interface DiContainer
    {
    }
    
    /// <summary>
    /// Base class for Zenject installers (stub for documentation only).
    /// </summary>
    public abstract class MonoInstaller : MonoBehaviour
    {
    }
}

namespace Unity.Collections
{
    public struct FixedString64Bytes
    {
        public FixedString64Bytes(string value) { }
        public override string ToString() => "";
    }
}

namespace Unity.Mathematics
{
    public struct float2 { public float x, y; }
    public struct float3 { public float x, y, z; }
    public struct float4 { public float x, y, z, w; }
    public struct int2 { public int x, y; }
    public struct int3 { public int x, y, z; }
    public struct int4 { public int x, y, z, w; }
    public struct uint2 { public uint x, y; }
    public struct uint3 { public uint x, y, z; }
    public struct uint4 { public uint x, y, z, w; }
    public struct bool2 { public bool x, y; }
    public struct bool3 { public bool x, y, z; }
    public struct bool4 { public bool x, y, z, w; }
    public struct quaternion { public float4 value; }
}
