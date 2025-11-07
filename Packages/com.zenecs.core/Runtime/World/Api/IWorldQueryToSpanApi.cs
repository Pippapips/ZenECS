using System;

namespace ZenECS.Core
{
    /// <summary>
    /// Delegate for processing a component reference directly.
    /// </summary>
    public delegate void RefAction<T>(ref T value) where T : struct;
    
    /// <summary>
    /// Read-only entity query surface. Returns allocation-free value-type enumerables
    /// that can be used directly in <c>foreach</c>, e.g.:
    /// <code>foreach (var e in world.Query&lt;Position, Rotation, Scale&gt;()) { ... }</code>
    /// The actual enumerables (QueryEnumerable&lt;...&gt;) are readonly structs defined in Runtime.
    /// </summary>
    public interface IWorldQueryToSpanApi
    {
        int QueryToSpan<T1>(Span<Entity> dst, Filter f = default) where T1 : struct;
        int QueryToSpan<T1, T2>(Span<Entity> dst, Filter f = default) where T1 : struct where T2 : struct;

        int QueryToSpan<T1, T2, T3>(Span<Entity> dst, Filter f = default)
            where T1 : struct where T2 : struct where T3 : struct;

        int QueryToSpan<T1, T2, T3, T4>(Span<Entity> dst, Filter f = default) where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct;

        int QueryToSpan<T1, T2, T3, T4, T5>(Span<Entity> dst, Filter f = default) where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            where T5 : struct;

        int QueryToSpan<T1, T2, T3, T4, T5, T6>(Span<Entity> dst, Filter f = default) where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            where T5 : struct
            where T6 : struct;

        int QueryToSpan<T1, T2, T3, T4, T5, T6, T7>(Span<Entity> dst, Filter f = default) where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            where T5 : struct
            where T6 : struct
            where T7 : struct;

        int QueryToSpan<T1, T2, T3, T4, T5, T6, T7, T8>(Span<Entity> dst, Filter f = default) where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            where T5 : struct
            where T6 : struct
            where T7 : struct
            where T8 : struct;
        
        void Process<T>(ReadOnlySpan<Entity> ents, RefAction<T> action) where T : struct;
    }
}