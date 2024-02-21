namespace Cooke.Gnissel.Typed;

public static class Db
{
    public static int Count() =>
        throw new InvalidOperationException("This method is only for use in queries");

    public static int Count<T>(T value) =>
        throw new InvalidOperationException("This method is only for use in queries");

    public static T Sum<T>(T value) =>
        throw new InvalidOperationException("This method is only for use in queries");

    public static T Avg<T>(T value) =>
        throw new InvalidOperationException("This method is only for use in queries");

    public static T Min<T>(T value) =>
        throw new InvalidOperationException("This method is only for use in queries");

    public static T Max<T>(T value) =>
        throw new InvalidOperationException("This method is only for use in queries");
}
