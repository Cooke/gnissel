//HintName: GeneratedObjectReaderProvider.Int32.cs
namespace Gnissel.SourceGeneration;

using System.Data.Common;

public partial class GeneratedObjectReaderProvider
{
    public int ReadInt32(DbDataReader reader, IReadOnlyList<int> columnOrdinals)
    {
        return reader.GetInt32(columnOrdinals[0]);
    }
}
