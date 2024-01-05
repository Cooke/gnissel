namespace Cooke.Gnissel.Utils;

public class FuncEqualityComparer
{
    public static IEqualityComparer<T> Create<T, TKey>(Func<T?, TKey?> selector) =>
        EqualityComparer<T>.Create(
            (l, r) => Equals(selector(l), selector(r)),
            x => selector(x)?.GetHashCode() ?? 0
        );

    public static IEqualityComparer<(T1, T2)> Create<T1, T2, TKey>(
        Func<T1?, T2?, TKey?> selector
    ) =>
        EqualityComparer<(T1, T2)>.Create(
            (l, r) => Equals(selector(l.Item1, l.Item2), selector(r.Item1, r.Item2)),
            x => selector(x.Item1, x.Item2)?.GetHashCode() ?? 0
        );
}
