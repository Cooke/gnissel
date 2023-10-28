﻿#region

using System.Data.Common;
using System.Text.Json;
using Cooke.Gnissel.Services.Implementations;

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
        // if (dbType == "jsonb")
        // {
        //     // TODO: why can't we use the stream overload here?
        //     return JsonSerializer.Deserialize<TOut>(
        //         reader.GetFieldValue<byte[]>(ordinal),
        //         _jsonSerializerOptions
        //     );
        // }

        return _inner.Read<TOut>(reader, ordinal, dbType);
    }
}
