#region

using System.Data.Common;

#endregion

namespace Cooke.Gnissel.Services;

public interface IRowReader
{
    TOut Read<TOut>(DbDataReader rowReader);
}
