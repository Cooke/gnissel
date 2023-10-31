#region

using System.Data.Common;

#endregion

namespace Cooke.Gnissel.Services;

public interface IRowReader
{
    IAsyncEnumerable<TOut> Read<TOut>(DbDataReader reader, CancellationToken cancellationToken);
}
