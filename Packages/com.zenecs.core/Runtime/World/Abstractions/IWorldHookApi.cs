using System;

namespace ZenECS.Core
{
    public interface IWorldHookApi
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
    }
}
