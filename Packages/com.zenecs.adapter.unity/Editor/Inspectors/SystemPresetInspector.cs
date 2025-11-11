#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ZenECS.Adapter.Unity.Install.SystemsPreset))]
public class SystemsPresetInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Clean Invalid & Duplicates"))
        {
            var so = (ZenECS.Adapter.Unity.Install.SystemsPreset)target;
            // OnValidate를 재사용하거나, 동일 로직을 메서드로 빼서 호출
            var serialized = new SerializedObject(so);
            serialized.Update();
            so.OnValidate();
            serialized.ApplyModifiedProperties();
        }
    }
}
#endif