using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using ZenECS.Adapter.Unity;

namespace ZenECS.Adapter.Unity.Editor.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(ZenReadOnlyInInspectorAttribute), useForChildren: true)]
    public sealed class ZenReadOnlyInInspector : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var field = new PropertyField(property);
            field.SetEnabled(false);
            return field;
        }
    }
}
