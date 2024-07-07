#region

using System.Data.Common;
using System.Runtime.CompilerServices;
using Cooke.Gnissel.Services;

#endregion

namespace Cooke.Gnissel;

[InterpolatedStringHandler]
public class Sql
{
    private readonly List<IFragment> _fragments;

    public Sql()
    {
        _fragments = new List<IFragment>();
    }

    public Sql(int fragmentCapacity)
    {
        _fragments = new List<IFragment>(fragmentCapacity);
    }

    public Sql(int literalLength, int formattedCount)
    {
        _fragments = new List<IFragment>(literalLength + formattedCount);
    }

    public IReadOnlyList<IFragment> Fragments => _fragments;

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

    public static Raw Inject(string raw) => new(raw);

    public static Identifier Id(string identifier) => new(identifier);

    public readonly record struct Raw(String Value);

    public interface IFragment;

    public record Literal(string Value) : IFragment;

    public record Identifier(string Value) : IFragment;

    public interface IParameter : IFragment
    {
        DbParameter ToParameter(DbOptions options);
    }

    private record Parameter(object Value, string? DbType) : IParameter
    {
        public DbParameter ToParameter(DbOptions options) => options.CreateParameter(Value, DbType);
    }

    private record Parameter<T>(T Value, string? DbType) : IParameter
    {
        public DbParameter ToParameter(DbOptions options) => options.CreateParameter(Value, DbType);
    }

    private record ExistingParameter(DbParameter Parameter) : IParameter
    {
        public DbParameter ToParameter(DbOptions options) => Parameter;
    }
}
