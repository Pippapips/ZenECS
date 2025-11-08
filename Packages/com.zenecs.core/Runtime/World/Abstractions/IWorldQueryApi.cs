namespace ZenECS.Core
{
    /// <summary>
    /// Read-only entity query surface. Returns allocation-free value-type enumerables
    /// that can be used directly in <c>foreach</c>, e.g.:
    /// <code>foreach (var e in world.Query&lt;Position, Rotation, Scale&gt;()) { ... }</code>
    /// The actual enumerables (QueryEnumerable&lt;...&gt;) are readonly structs defined in Runtime.
    /// </summary>
    public interface IWorldQueryApi
    {
        /// <summary>Query entities having <typeparamref name="T1"/>.</summary>
        QueryEnumerable<T1> Query<T1>(Filter f = default)
            where T1 : struct;

        /// <summary>Query entities having <typeparamref name="T1"/>, <typeparamref name="T2"/>.</summary>
        QueryEnumerable<T1, T2> Query<T1, T2>(Filter f = default)
            where T1 : struct
            where T2 : struct;
        
        /// <summary>Query entities having <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>.</summary>
        QueryEnumerable<T1, T2, T3> Query<T1, T2, T3>(Filter f = default)
            where T1 : struct
            where T2 : struct
            where T3 : struct;
        
        /// <summary>Query entities having <typeparamref name="T1"/> .. <typeparamref name="T4"/>.</summary>
        QueryEnumerable<T1, T2, T3, T4> Query<T1, T2, T3, T4>(Filter f = default)
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct;
        
        /// <summary>Query entities having <typeparamref name="T1"/> .. <typeparamref name="T5"/>.</summary>
        QueryEnumerable<T1, T2, T3, T4, T5> Query<T1, T2, T3, T4, T5>(Filter f = default)
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            where T5 : struct;
        
        /// <summary>Query entities having <typeparamref name="T1"/> .. <typeparamref name="T6"/>.</summary>
        QueryEnumerable<T1, T2, T3, T4, T5, T6> Query<T1, T2, T3, T4, T5, T6>(Filter f = default)
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            where T5 : struct
            where T6 : struct;
        
        /// <summary>Query entities having <typeparamref name="T1"/> .. <typeparamref name="T7"/>.</summary>
        QueryEnumerable<T1, T2, T3, T4, T5, T6, T7> Query<T1, T2, T3, T4, T5, T6, T7>(Filter f = default)
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            where T5 : struct
            where T6 : struct
            where T7 : struct;
        
        /// <summary>Query entities having <typeparamref name="T1"/> .. <typeparamref name="T8"/>.</summary>
        QueryEnumerable<T1, T2, T3, T4, T5, T6, T7, T8> Query<T1, T2, T3, T4, T5, T6, T7, T8>(Filter f = default)
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            where T5 : struct
            where T6 : struct
            where T7 : struct
            where T8 : struct;
    }

    // NOTE:
    // - QueryEnumerable<...> and Filter are part of the public runtime API (readonly structs)
    //   defined alongside World.Query.*. Keep them allocation-free and compatible with foreach.
    // - 'Filter' typically carries Without/Any/All masks or flags (e.g., Changed<T>),
    //   but passing default means "no extra filter".
}
