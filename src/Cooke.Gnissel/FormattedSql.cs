#region

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

#endregion

namespace Cooke.Gnissel;

[InterpolatedStringHandler]
public class FormattedSql
{
    private readonly StringBuilder _builder;

    // TODO avoid boxing: https://github.com/dotnet/runtime/issues/28882
    private readonly List<(object? value, string? format)> _parameters = new();

    public FormattedSql(int literalLength, int formattedCount)
    {
        _builder = new StringBuilder(literalLength + formattedCount * 3);
    }

    public void AppendLiteral(string s)
    {
        _builder.Append(s);
    }

    public void AppendFormatted<T>(T t)
    {
        _builder.Append('$');
        _builder.Append(_parameters.Count + 1);
        _parameters.Add((t, null));
    }

    public void AppendFormatted<T>(T t, string? format)
    {
        _builder.Append('$');
        _builder.Append(_parameters.Count + 1);
        _parameters.Add((t, format));
    }

    public string Sql => _builder.ToString();

    public ImmutableArray<(object? value, string? dbType)> Parameters =>
        _parameters.ToImmutableArray();
}
