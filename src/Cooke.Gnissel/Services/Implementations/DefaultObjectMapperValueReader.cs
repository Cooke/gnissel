#region

using System.Data.Common;

#endregion

namespace Cooke.Gnissel;

public class DefaultObjectMapperValueReader : IObjectMapperValueReader
{
    public TOut Read<TOut>(DbDataReader reader, int ordinal, string? dbType)
    {
        return reader.GetFieldValue<TOut>(ordinal);
    }
}
