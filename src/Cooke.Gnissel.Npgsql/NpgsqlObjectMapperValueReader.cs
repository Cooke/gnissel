#region

using System.Data.Common;
using System.Text.Json;

#endregion

namespace Cooke.Gnissel.Npgsql;

public class NpgsqlObjectMapperValueReader : IObjectMapperValueReader
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly DefaultObjectMapperValueReader _inner;

    public NpgsqlObjectMapperValueReader(JsonSerializerOptions jsonSerializerOptions)
    {
        _jsonSerializerOptions = jsonSerializerOptions;
        _inner = new DefaultObjectMapperValueReader();
    }

    public TOut? Read<TOut>(DbDataReader reader, int ordinal, string? dbType)
    {
        if (dbType == "jsonb")
        {
            return JsonSerializer.Deserialize<TOut>(
                reader.GetStream(ordinal),
                _jsonSerializerOptions
            );
        }

        return _inner.Read<TOut>(reader, ordinal, dbType);
    }
}
