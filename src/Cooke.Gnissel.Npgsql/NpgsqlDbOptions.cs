﻿using Cooke.Gnissel.Typed;
using Npgsql;

namespace Cooke.Gnissel.Npgsql;

public static class NpgsqlDbOptionsFactory
{
    public static DbOptionsTyped Create(NpgsqlDataSource dataSource) =>
        new(
            new NpgsqlDbAdapter(dataSource),
            new NpgsqlSqlGenerator(new DefaultPostgresIdentifierMapper())
        );
}