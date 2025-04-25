using System.Data.Common;
using System.Runtime.CompilerServices;

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

    public void AppendSql(Sql sql) => _fragments.AddRange(sql.Fragments);

    public void AppendLiteral(string s) => _fragments.Add(new Literal(s));

    public void AppendLiteralValue(object? value) => _fragments.Add(new LiteralValue(value));

    public static Identifier Id(string identifier) => new(identifier);

    public void AppendIdentifier(string identifier) => _fragments.Add(new Identifier(identifier));

    public readonly record struct Raw(String Value);

    public static Raw Inject(string raw) => new(raw);

    public void AppendFormatted(Raw raw) => _fragments.Add(new Literal(raw.Value));

    public void AppendFormatted<T>(T t) => AppendParameter(t);

    public void AppendParameter<T>(T t) => _fragments.Add(new Parameter<T>(t, null));

    public void AppendParameter(Type type, object? value) =>
        _fragments.Add(new RuntimeTypedParameter(type, value, null));

    public void AppendParameter(Parameter parameter) => _fragments.Add(parameter);

    public void AppendDbParameter(DbParameter dbParameter) =>
        _fragments.Add(new DbParameterContainer(dbParameter));

    public void AppendFormatted<T>(T t, string? format) =>
        _fragments.Add(new Parameter<T>(t, format));

    public abstract record Fragment;

    public record Literal(string Value) : Fragment;

    public record Identifier(string Value) : Fragment;

    public abstract record Parameter : Fragment
    {
        public abstract void WriteParameter(ISqlParameterWriter writer);
    }

    public record Parameter<T>(T Value, string? Format) : Parameter
    {
        public override void WriteParameter(ISqlParameterWriter writer) =>
            writer.Write(Value, Format);
    }

    public record RuntimeTypedParameter(Type Type, object? Value, string? Format) : Parameter
    {
        public override void WriteParameter(ISqlParameterWriter writer) =>
            writer.Write(Type, Value, Format);
    }

    public record DbParameterContainer(DbParameter Value) : Fragment;

    public record LiteralValue(object? Value) : Fragment;
}

public interface ISqlParameterWriter
{
    void Write<T>(T value, string? dbType = null);

    void Write(Type type, object? value, string? dbType = null);
}
