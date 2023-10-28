#region

using System.Data.Common;

#endregion

namespace Cooke.Gnissel.Services.Implementations;

public interface IColumnReader
{
    TOut? Read<TOut>(DbDataReader reader, int column, string? dbType);
}
