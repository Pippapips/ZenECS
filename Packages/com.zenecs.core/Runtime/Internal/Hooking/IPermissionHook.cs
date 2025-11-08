using System;

namespace ZenECS.Core.Internal.Hooking
{
    internal interface IPermissionHook
    {
        void AddWritePermission(Func<Entity, Type, bool> hook);
        bool RemoveWritePermission(Func<Entity, Type, bool> hook);
        void ClearWritePermissions();
        void AddReadPermission(Func<Entity, Type, bool> hook);
        bool RemoveReadPermission(Func<Entity, Type, bool> hook);
        void ClearReadPermissions();
        void AddValidator(Func<object, bool> hook);
        bool RemoveValidator(Func<object, bool> hook);
        void ClearValidators();

        void AddValidator<T>(Func<T, bool> predicate) where T : struct;
        bool RemoveValidator<T>(Func<T, bool> predicate) where T : struct;
        void ClearTypedValidators();

        bool EvaluateWritePermission(Entity e, Type t);
        bool EvaluateReadPermission(Entity e, Type t);
        bool ValidateObject(object value);
        bool ValidateTyped<T>(in T value) where T : struct;

        void ClearAllHookQueues();
    }
}