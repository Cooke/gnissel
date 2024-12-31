using System.Data.Common;

namespace Cooke.Gnissel;

public static class DbDataReaderExtensions
{
    public static string? GetStringOrNull(this DbDataReader dbDataReader, int ordinal) =>
        dbDataReader.IsDBNull(ordinal) ? null : dbDataReader.GetString(ordinal);

    public static DateTime? GetDateTimeOrNull(this DbDataReader dbDataReader, int ordinal) =>
        dbDataReader.IsDBNull(ordinal) ? null : dbDataReader.GetDateTime(ordinal);

    public static TimeSpan? GetTimeSpanOrNull(this DbDataReader dbDataReader, int ordinal) =>
        dbDataReader.IsDBNull(ordinal) ? null : dbDataReader.GetFieldValue<TimeSpan>(ordinal);

    public static Int32? GetInt32OrNull(this DbDataReader dbDataReader, int ordinal) =>
        dbDataReader.IsDBNull(ordinal) ? null : dbDataReader.GetInt32(ordinal);
}
