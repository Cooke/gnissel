using System.Data.Common;

namespace Cooke.Gnissel;

public delegate TOut ObjectReaderFunc<out TOut>(DbDataReader dataReader, int ordinalOffset);