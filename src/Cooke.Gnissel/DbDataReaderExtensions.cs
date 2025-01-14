using System.Data.Common;

namespace Cooke.Gnissel;

public static class DbDataReaderExtensions
{
    public static T? GetValueOrNull<T>(this DbDataReader dbDataReader, int ordinal) =>
        dbDataReader.IsDBNull(ordinal) ? default : dbDataReader.GetFieldValue<T>(ordinal);

    public static string? GetStringOrNull(this DbDataReader dbDataReader, int ordinal) =>
        dbDataReader.IsDBNull(ordinal) ? null : dbDataReader.GetString(ordinal);

    public static Int32? GetInt32OrNull(this DbDataReader dbDataReader, int ordinal) =>
        dbDataReader.IsDBNull(ordinal) ? null : dbDataReader.GetInt32(ordinal);
}
