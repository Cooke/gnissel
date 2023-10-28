#region

using System.Data.Common;

#endregion

namespace Cooke.Gnissel;

public interface IObjectMapper
{
    TOut Map<TOut>(DbDataReader rowReader);
}