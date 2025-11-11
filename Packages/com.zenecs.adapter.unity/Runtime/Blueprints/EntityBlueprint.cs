#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ZenECS.Core;
using ZenECS.Core.Binding;

namespace ZenECS.Adapter.Unity.Blueprints
{
    [CreateAssetMenu(menuName = "ZenECS/Entity Blueprint", fileName = "EntityBlueprint")]
    public sealed class EntityBlueprint : ScriptableObject
    {
        [Header("Components (snapshot)")]
        [SerializeField] private BlueprintData _data = new();
        public BlueprintData Data => _data;

        [Header("Contexts (managed reference)")]
        [SerializeReference] private List<IContext> _contexts = new();
        [Header("Binders (managed reference)")]
        [SerializeReference] private List<IBinder> _binders = new();

        public Entity Spawn(IWorld world, bool clonePerEntity = true)
        {
            var e = world.SpawnEntity();
            _data?.ApplyTo(world, e);

            foreach (var ctx in _contexts)
            {
                if (ctx == null) continue;
                var inst = clonePerEntity ? (IContext)ShallowCopy(ctx, ctx.GetType()) : ctx;

                world.RegisterContext(e, inst);
            }

            foreach (var b in _binders)
            {
                if (b == null) continue;
                var inst = clonePerEntity ? (IBinder)ShallowCopy(b, b.GetType()) : b;

                world.AttachBinder(e, inst);
            }

            return e;
        }

        private static object ShallowCopy(object? source, Type t)
        {
            if (source == null) return null!;
            if (t.IsValueType) return source;
            if (typeof(UnityEngine.Object).IsAssignableFrom(t)) return source;

            object target;
            try { target = Activator.CreateInstance(t)!; }
            catch (Exception ex) { throw new InvalidOperationException($"Type '{t.FullName}' requires a public parameterless ctor.", ex); }

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
