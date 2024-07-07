#region

using System.Data.Common;
using System.Runtime.CompilerServices;

#endregion

namespace Cooke.Gnissel;

[InterpolatedStringHandler]
public class Sql
{
    private readonly List<Fragment> _fragments;

    public Sql()
    {
        _fragments = new List<Fragment>();
    }

    public Sql(int fragmentCapacity)
    {
        _fragments = new List<Fragment>(fragmentCapacity);
    }

    public Sql(int literalLength, int formattedCount)
    {
        _fragments = new List<Fragment>(literalLength + formattedCount);
    }

    public IReadOnlyList<Fragment> Fragments => _fragments;

    public void AppendSql(Sql sql)
    {
        _fragments.AddRange(sql.Fragments);
    }

    public void AppendLiteral(string s)
    {
        _fragments.Add(new Literal(s));
    }

    public void AppendIdentifier(string identifier)
    {
        _fragments.Add(new Identifier(identifier));
    }

    public void AppendFormatted(Raw raw)
    {
        _fragments.Add(new Literal(raw.Value));
    }

    public void AppendFormatted<T>(T t)
    {
        AppendParameter(t);
    }

    public void AppendParameter<T>(T t)
    {
        _fragments.Add(new Parameter<T>(t, null));
    }

    public void AppendFormatted(DbParameter parameter)
    {
        _fragments.Add(new ExistingParameter(parameter));
    }

    public void AppendFormatted<T>(T t, string? format)
    {
        _fragments.Add(new Parameter<T>(t, format));
    }

    public readonly record struct Raw(String Value);

    public static Raw Inject(string raw) => new(raw);

    public static Identifier Id(string identifier) => new(identifier);

    public abstract record Fragment;

    public record Literal(string Value) : Fragment;

    public record Identifier(string Value) : Fragment;

    public abstract record Parameter : Fragment
    {
        public abstract DbParameter ToParameter(DbOptions options);
    }

    private record Parameter<T>(T Value, string? DbType) : Parameter
    {
        public override DbParameter ToParameter(DbOptions options) =>
            options.CreateParameter(Value, DbType);
    }

    private record ExistingParameter(DbParameter Parameter) : Parameter
    {
        public override DbParameter ToParameter(DbOptions options) => Parameter;
    }
}
