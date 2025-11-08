#nullable enable
using System;

namespace ZenECS.Core.Internal
{
    internal sealed partial class World : IWorldHookApi
    {
        public void AddWritePermission(Func<Entity, Type, bool> hook) => _permissionHook.AddReadPermission(hook);
        public bool RemoveWritePermission(Func<Entity, Type, bool> hook) => _permissionHook.RemoveWritePermission(hook);
        public void ClearWritePermissions() => _permissionHook.ClearReadPermissions();
        public void AddReadPermission(Func<Entity, Type, bool> hook) => _permissionHook.AddReadPermission(hook);
        public bool RemoveReadPermission(Func<Entity, Type, bool> hook) => _permissionHook.RemoveReadPermission(hook);
        public void ClearReadPermissions() => _permissionHook.ClearReadPermissions();
        public void AddValidator(Func<object, bool> hook) => _permissionHook.AddValidator(hook);
        public bool RemoveValidator(Func<object, bool> hook) => _permissionHook.RemoveValidator(hook);
        public void ClearValidators() => _permissionHook.ClearValidators();
        public void AddValidator<T>(Func<T, bool> predicate) where T : struct => _permissionHook.AddValidator(predicate);
        public bool RemoveValidator<T>(Func<T, bool> predicate) where T : struct => _permissionHook.RemoveValidator(predicate);
        public void ClearTypedValidators() => _permissionHook.ClearTypedValidators();
    }
}