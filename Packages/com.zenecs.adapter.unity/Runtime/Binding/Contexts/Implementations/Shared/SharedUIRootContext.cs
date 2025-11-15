using TMPro;
using UnityEngine;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Binding.Contexts
{
    public sealed class SharedUIRootContext : MonoBehaviour, IContext
    {
        [SerializeField] private GameObject _canvasObject;
        [SerializeField] private Transform _canvasRoot;
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private TextMeshProUGUI _text2;
        
        public GameObject CanvasObject => _canvasObject;
        public Transform CanvasRoot => _canvasRoot;
        public TextMeshProUGUI Text => _text;
        public TextMeshProUGUI Text2 => _text2;
    }
}