using System.Data.Common;

namespace Cooke.Gnissel;

public readonly struct Row
{
    private readonly DbDataReader _dataRecord;

    public Row(DbDataReader dataRecord)
    {
        _dataRecord = dataRecord;
    }

    public T Get<T>(string column) => _dataRecord.GetFieldValue<T>(_dataRecord.GetOrdinal(column));

    public T Get<T>(int ordinal) => _dataRecord.GetFieldValue<T>(ordinal);
}
