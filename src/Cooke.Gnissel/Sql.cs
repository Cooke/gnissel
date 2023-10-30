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

    public void AddFragment(IFragment fragment)
    {
        _fragments.Add(fragment);
    }

    public void AddFragments(IEnumerable<IFragment> fragments)
    {
        _fragments.AddRange(fragments);
    }

    public void AppendLiteral(string s)
    {
        _fragments.Add(new Literal(s));
    }

    public void AppendFormatted(Raw raw)
    {
        _fragments.Add(new Literal(raw.Value));
    }

    public void AppendFormatted<T>(T t)
    {
        _fragments.Add(new Parameter<T>(t, null));
    }

    public void AppendFormatted<T>(T t, string? format)
    {
        _fragments.Add(new Parameter<T>(t, format));
    }

    public static Raw Inject(string raw) => new(raw);

    public readonly record struct Raw(String Value);

    public interface IFragment { }

    public record Literal(string Value) : IFragment;

    public interface IParameter : IFragment
    {
        DbParameter ToParameter(IDbAdapter adapter);
    }

    private record Parameter<T>(T Value, string? DbType) : IParameter
    {
        public DbParameter ToParameter(IDbAdapter adapter) =>
            adapter.CreateParameter(Value, DbType);
    }
}
