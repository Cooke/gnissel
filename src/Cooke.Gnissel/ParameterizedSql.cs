using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cooke.Gnissel;

[InterpolatedStringHandler]
public class ParameterizedSql
{
    private readonly StringBuilder _builder;

    // TODO avoid boxing: https://github.com/dotnet/runtime/issues/28882
    private readonly List<object?> parameters = new();

    public ParameterizedSql(int literalLength, int formattedCount)
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
        _builder.Append(parameters.Count + 1);
        parameters.Add(t);
    }

    public string Sql => _builder.ToString();

    public ImmutableArray<object?> Parameters => parameters.ToImmutableArray();
}
