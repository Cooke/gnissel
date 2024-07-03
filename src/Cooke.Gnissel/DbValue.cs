namespace Cooke.Gnissel;

public abstract class DbValue
{
    public abstract object? Value { get; }
}

public class UntypedDbValue(object value) : DbValue
{
    public override object? Value => value;
}

public class TypedDbValue<T>(T value) : DbValue
{
    public T TypedValue => value;

    public override object? Value => value;
}
