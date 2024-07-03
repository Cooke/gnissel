using System.Data.Common;
using Cooke.Gnissel.Services;

namespace Cooke.Gnissel;

public abstract class DbValue
{
    public abstract object? Value { get; }

    public abstract DbParameter CreateParameter(IDbAdapter dbAdapter);
}

public class DbValue<T>(T value, string? dbType) : DbValue
{
    public T TypedValue => value;

    public override object? Value => value;

    public override DbParameter CreateParameter(IDbAdapter dbAdapter) =>
        dbAdapter.CreateParameter(TypedValue, dbType);
}
