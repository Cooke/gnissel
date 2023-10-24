using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cooke.Gnissel;

[InterpolatedStringHandler]
public class FormattedSql
{
    private readonly StringBuilder _builder;

    // TODO avoid boxing: https://github.com/dotnet/runtime/issues/28882
    private readonly List<object?> _parameters = new();

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
        _parameters.Add(t);
    }

    public string Sql => _builder.ToString();

    public ImmutableArray<object?> Parameters => _parameters.ToImmutableArray();
}
