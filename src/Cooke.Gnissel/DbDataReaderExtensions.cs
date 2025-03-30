using System.Data.Common;

namespace Cooke.Gnissel;

public static class DbDataReaderExtensions
{
    public static T? GetNullableValue<T>(this DbDataReader dbDataReader, int ordinal)
        where T : struct =>
        dbDataReader.IsDBNull(ordinal) ? null : dbDataReader.GetFieldValue<T>(ordinal);

    public static T? GetValueOrNull<T>(this DbDataReader dbDataReader, int ordinal)
        where T : class =>
        dbDataReader.IsDBNull(ordinal) ? null : dbDataReader.GetFieldValue<T>(ordinal);

    public static T? GetValueOrDefault<T>(this DbDataReader dbDataReader, int ordinal) =>
        dbDataReader.IsDBNull(ordinal) ? default : dbDataReader.GetFieldValue<T>(ordinal);
}
