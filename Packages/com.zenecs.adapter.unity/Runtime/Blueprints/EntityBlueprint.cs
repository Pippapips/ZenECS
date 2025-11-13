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
        [SerializeField] private BlueprintData _data = new();
        public BlueprintData Data => _data;

        // 🔁 기존: [SerializeReference] List<IContext> _contexts
        //     → SO 기반 컨텍스트 에셋 레퍼런스
        [Header("Contexts (assets)")]
        [SerializeField] private List<ContextAsset> _contextAssets = new();
        public IReadOnlyList<ContextAsset> ContextAssets => _contextAssets;

        [Header("Binders (managed reference)")]
        [SerializeReference] private List<IBinder> _binders = new();

        /// <summary>
        /// Spawn an entity and apply only component snapshot.
        /// Binder/Context SO는 별도 BindingInstaller 등에서 적용하는 것을 권장.
        /// </summary>
        public Entity Spawn(IWorld world, bool clonePerEntity = true)
        {
            var e = world.SpawnEntity();
            _data?.ApplyTo(world, e);

            // 1) Contexts first (so binders see them in OnAttach).
            for (int i = 0; i < _contextAssets.Count; i++)
            {
                var asset = _contextAssets[i];
                switch (asset)
                {
                    case SharedContextMarkerAsset markerAsset:
                    {
                        // var ctx = sharedResolver.Resolve(markerAsset);
                        // world.RegisterContext(e, ctx);
                        break;
                    }
                    case PerEntityContextAsset perEntityAsset:
                    {
                        var ctx = perEntityAsset.CreateContextForEntity(world, e);
                        world.RegisterContext(e, ctx);
                        break;
                    }
                }
            }

            foreach (var b in _binders)
            {
                if (b == null) continue;
                var inst = clonePerEntity ? (IBinder)ShallowCopy(b, b.GetType()) : b;
                world.AttachBinder(e, inst);
            }
            
            return e;
        }

        // 필요하다면 나중에 이런 형태의 헬퍼를 추가해서
        // SO → 런타임 컨텍스트/바인더를 바로 적용할 수 있음:
        //
        // public Entity Spawn(IWorld world, IWorldContextApi ctxApi, IBinderAttachmentApi binderApi)
        // {
        //     var e = world.SpawnEntity();
        //     _data?.ApplyTo(world, e);
        //
        //     var installer = new BindingInstaller(world, ctxApi, binderApi);
        //     installer.Apply(e, _binderAssets, _contextAssets);
        //
        //     return e;
        // }

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
