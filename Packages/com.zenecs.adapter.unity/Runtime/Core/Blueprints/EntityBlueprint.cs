#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Binding;

// SO 기반 컨텍스트/바인더
using ZenECS.Adapter.Unity.Binding.Contexts.Assets;

namespace ZenECS.Adapter.Unity.Blueprints
{
    [CreateAssetMenu(menuName = "ZenECS/Entity Blueprint", fileName = "EntityBlueprint")]
    public sealed class EntityBlueprint : ScriptableObject
    {
        [Header("Components (snapshot)")]
        [SerializeField] private EntityBlueprintData _data = new();
        public EntityBlueprintData Data => _data;

        [Header("Contexts (ScriptableObject assets)")]
        [SerializeField] private List<ContextAsset> _contextAssets = new();

        [Header("Binders (managed reference)")]
        [SerializeReference] private List<IBinder> _binders = new();

        /// <summary>
        /// Spawn an entity and apply only component snapshot.
        /// Binder/Context SO는 별도 BindingInstaller 등에서 적용하는 것을 권장.
        /// </summary>
        public void Spawn(IWorld world, ISharedContextResolver? sharedContextResolver, Action<Entity>? onCreated = null)
        {
            world.ExternalCommandEnqueue(ExternalCommand.CreateEntity((e, cmd) =>
            {
                _data?.ApplyTo(world, e, cmd);
                
                // 1) Contexts first (so binders see them in OnAttach).
                for (int i = 0; i < _contextAssets.Count; i++)
                {
                    var asset = _contextAssets[i];
                    switch (asset)
                    {
                        case SharedContextAsset markerAsset:
                        {
                            if (sharedContextResolver != null)
                            {
                                var ctx = sharedContextResolver.Resolve(markerAsset);
                                world.RegisterContext(e, ctx);
                            }
                            break;
                        }
                        case PerEntityContextAsset perEntityAsset:
                        {
                            var ctx = perEntityAsset.Create();
                            world.RegisterContext(e, ctx);
                            break;
                        }
                    }
                }

                foreach (var b in _binders)
                {
                    if (b == null) continue;
                    var inst = (IBinder)ShallowCopy(b, b.GetType());
                    inst.SetApplyOrderAndAttachOrder(inst.ApplyOrder, b.AttachOrder);
                    world.AttachBinder(e, inst);
                }
                
                onCreated?.Invoke(e);
            }));
        }

        private static object ShallowCopy(object? source, Type t)
        {
            if (source == null) return null!;
            if (t.IsValueType) return source;
            if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return source;

            object target;
            try { target = Activator.CreateInstance(t)!; }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Type '{t.FullName}' requires a public parameterless ctor.", ex);
            }

            const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var f in t.GetFields(BF))
            {
                if (f.IsStatic) continue;
                var val = f.GetValue(source);
                f.SetValue(target, val);
            }
            return target;
        }
    }
}
