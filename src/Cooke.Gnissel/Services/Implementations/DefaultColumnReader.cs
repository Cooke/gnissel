#region

using System.Data.Common;

#endregion

namespace Cooke.Gnissel.Services.Implementations;

public class DefaultColumnReader : IColumnReader
{
    public TOut Read<TOut>(DbDataReader reader, int column, string? dbType)
    {
        return reader.GetFieldValue<TOut>(column);
    }
}
