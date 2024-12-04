using System.Collections.Immutable;

namespace Cooke.Gnissel;

public class ObjectReader<TOut>(ObjectReaderFunc<TOut> read, ImmutableArray<string> columnNames)
{
    public ObjectReaderFunc<TOut> Read { get; } = read;
    public ImmutableArray<string> ColumnNames { get; } = columnNames;
}
