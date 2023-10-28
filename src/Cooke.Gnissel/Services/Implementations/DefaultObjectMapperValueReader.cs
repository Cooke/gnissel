#region

using System.Data.Common;

#endregion

namespace Cooke.Gnissel.Services.Implementations;

public class DefaultObjectMapperValueReader : IObjectMapperValueReader
{
    public TOut Read<TOut>(DbDataReader reader, int ordinal, string? dbType)
    {
        return reader.GetFieldValue<TOut>(ordinal);
    }
}
