namespace Cooke.Gnissel.Internals;

internal static class ObjectExtensions
{
    public static TOut Let<T, TOut>(this T obj, Func<T, TOut> func) => func(obj);
}
