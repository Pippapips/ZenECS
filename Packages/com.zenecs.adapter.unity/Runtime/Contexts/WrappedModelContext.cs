using UnityEngine;
using ZenECS.Core;
using ZenECS.Adapter.Unity.Linking;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Contexts
{
    public interface IGameObjectBackedContext : IContext { void Bind(Transform root); }

    public sealed class WrappedModelContext : IGameObjectBackedContext
    {
        public Transform Root { get; private set; }
        public void Bind(Transform root) => Root = root;

        public void Initialize(IWorld w, Entity e)
        {
            if (!Root) return;
            var link = Root.GetComponent<EntityLink>() ?? Root.gameObject.AddComponent<EntityLink>();
            if (!link.IsAlive) link.Attach(w, e, ViewKey.Main());
        }

        public void Deinitialize(bool destroy)
        {
            if (!Root) return;
            var go = Root.gameObject;
            if (destroy) Object.Destroy(go);
            else
            {
                var link = go.GetComponent<EntityLink>(); if (link) link.Detach();
                go.SetActive(false);
            }
            Root = null;
        }
    }
}