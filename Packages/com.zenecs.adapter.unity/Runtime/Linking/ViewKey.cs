using System;

namespace ZenECS.Adapter.Unity.Linking
{
    public enum ViewKind { Main, Sub }

    public readonly struct ViewKey : IEquatable<ViewKey>
    {
        public readonly ViewKind Kind;
        public readonly int Index; // Main=0 고정, Sub는 우선순위

        public ViewKey(ViewKind kind, int index = 0) { Kind = kind; Index = index; }
        public static ViewKey Main() => new(ViewKind.Main, 0);
        public static ViewKey Sub(int index) => new(ViewKind.Sub, index);
        public bool Equals(ViewKey other) => Kind == other.Kind && Index == other.Index;
        public override int GetHashCode() => ((int)Kind * 397) ^ Index;
        public override string ToString() => Kind == ViewKind.Main ? "Main" : $"Sub({Index})";
    }
}