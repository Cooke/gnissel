#region

using System.Data.Common;

#endregion

namespace Cooke.Gnissel.Services;

public delegate TOut ObjectReaderFunc<out TOut>(DbDataReader dataReader, int ordinalOffset);

public readonly record struct ObjectReader<TOut>(ObjectReaderFunc<TOut> Read, int Width);

public interface IObjectReaderProvider
{
    ObjectReader<TOut> GetReader<TOut>();
}
