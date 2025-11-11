#nullable enable
using UnityEngine;
using ZenECS.Core;
using ZenECS.Adapter.Unity.Linking;

namespace ZenECS.Adapter.Unity.Authoring
{
    public enum AutoLinkMode { AttachToExisting, AutoCreateEntity }
    public enum AutoLinkWhen { Awake, Start, Manual }

    [DisallowMultipleComponent]
    public sealed class EntityAutoLink : MonoBehaviour
    {
        [SerializeField] private AutoLinkMode _mode = AutoLinkMode.AutoCreateEntity;
        [SerializeField] private AutoLinkWhen _when = AutoLinkWhen.Awake;
        [SerializeField] private bool _asMain = true;
        [SerializeField] private int _subIndex = 0;

        void Awake() { if (_when == AutoLinkWhen.Awake) Run(); }
        void Start() { if (_when == AutoLinkWhen.Start) Run(); }

        public void Run()
        {
            var world = KernelLocator.CurrentWorld;
            if (world == null) { Debug.LogWarning("[EntityAutoLink] World not found"); return; }

            var link = GetComponent<EntityLink>() ?? gameObject.AddComponent<EntityLink>();

            if (_mode == AutoLinkMode.AttachToExisting && link.World != null && link.Entity.Id >= 0)
                link.Attach(link.World, link.Entity, _asMain ? ViewKey.Main() : ViewKey.Sub(_subIndex));
            else
            {
                var e = world.SpawnEntity();
                if (_asMain) gameObject.EnsureMain(world, e);
                else gameObject.EnsureSub(world, e, _subIndex);
            }
        }
    }
}