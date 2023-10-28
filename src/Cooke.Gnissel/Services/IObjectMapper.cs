#region

using System.Data.Common;

#endregion

namespace Cooke.Gnissel.Services;

public interface IObjectMapper
{
    TOut Map<TOut>(DbDataReader rowReader);
}