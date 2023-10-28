#region

using System.Data.Common;

#endregion

namespace Cooke.Gnissel.Services.Implementations;

public interface IObjectMapperValueReader
{
    TOut? Read<TOut>(DbDataReader reader, int ordinal, string? dbType);
}
