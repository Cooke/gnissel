#region

using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using Cooke.Gnissel.PlusPlus.Utils;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Utils;

#endregion

namespace Cooke.Gnissel.PlusPlus;

public class WhereQuery<T1, T2, T3>(
    DbOptions options,
    TableSource source,
    ImmutableArray<Join> joins,
    Expression<Func<T1, T2, T3, bool>>? predicate)
    : IToAsyncEnumerable<ValueTuple<T1, T2, T3>>
{
    private readonly WhereQuery<ValueTuple<T1, T2, T3>> _inner = new WhereQuery<ValueTuple<T1, T2, T3>>(
        options,
        source,
        joins,
        predicate?.Let(_ => CreateTuplePredicate(predicate))
    );

    [Pure]
    public ValueTask<(T1, T2, T3)> FirstAsync(CancellationToken cancellationToken = default) =>
        _inner.FirstAsync(cancellationToken);

    [Pure]
    public ValueTask<(T1, T2, T3)> FirstAsync(
        Expression<Func<T1, T2, T3, bool>> predicate,
        CancellationToken cancellationToken = default
    ) => _inner.FirstAsync(CreateTuplePredicate(predicate), cancellationToken);

    [Pure]
    public WhereQuery<(T1, T2, T3)> Where(Expression<Func<T1, T2, T3, bool>> predicate) =>
        _inner.Where(CreateTuplePredicate(predicate));

    public IAsyncEnumerable<(T1, T2, T3)> ToAsyncEnumerable() => _inner.ToAsyncEnumerable();

    private static Expression<Func<(T1, T2, T3), bool>> CreateTuplePredicate(
        Expression<Func<T1, T2, T3, bool>> predicate
    ) => PredicateTransformer.CreateTuplePredicate<(T1, T2, T3)>(predicate);
}

public class WhereQuery<T1, T2>(
    DbOptions options,
    TableSource source,
    ImmutableArray<Join> joins,
    Expression<Func<T1, T2, bool>>? predicate)
    : IToAsyncEnumerable<ValueTuple<T1, T2>>
{
    private readonly WhereQuery<ValueTuple<T1, T2>> _inner = new(
        options,
        source,
        joins,
        predicate?.Let(_ => CreateTuplePredicate(predicate))
    );

    [Pure]
    public ValueTask<(T1, T2)> FirstAsync(CancellationToken cancellationToken = default) =>
        _inner.FirstAsync(cancellationToken);

    [Pure]
    public ValueTask<(T1, T2)> FirstAsync(
        Expression<Func<T1, T2, bool>> predicate,
        CancellationToken cancellationToken = default
    ) => _inner.FirstAsync(CreateTuplePredicate(predicate), cancellationToken);

    [Pure]
    public WhereQuery<T1, T2, TJoin> Join<TJoin>(
        Table<TJoin> outer,
        Expression<Func<T1, T2, TJoin, bool>> predicate
    )
    {
        return new WhereQuery<T1, T2, TJoin>(
            options,
            source,
            joins.Add(new Join(outer, predicate.Parameters[2], predicate)),
            null
        );
    }

    public IAsyncEnumerable<ValueTuple<T1, T2>> ToAsyncEnumerable() => _inner.ToAsyncEnumerable();

    private static Expression<Func<(T1, T2), bool>> CreateTuplePredicate(
        Expression<Func<T1, T2, bool>> predicate
    ) => PredicateTransformer.CreateTuplePredicate<(T1, T2)>(predicate);
}

public class WhereQuery<T>(
    DbOptions options,
    TableSource tableSource,
    ImmutableArray<Join> joins,
    Expression? condition)
    : IToAsyncEnumerable<T>
{
    private readonly IDbAdapter _dbAdapter = options.DbAdapter;
    private readonly IDbConnector _dbConnector = options.DbConnector;
    private readonly IObjectReaderProvider _objectReaderProvider = options.ObjectReaderProvider;

    [Pure]
    public WhereQuery<T> Where(Expression<Func<T, bool>> predicate) =>
        new(options, tableSource, joins, predicate.Body);

    public async ValueTask<T?> FirstOrDefaultAsync(CancellationToken cancellationToken = default)
    {
        var query = new Query<T>(
            _dbAdapter.RenderSql(CreateSql<T>(limit: 1)),
            _objectReaderProvider.GetReaderFunc<T>(),
            _dbConnector
        );

        await foreach (var value in query.WithCancellation(cancellationToken))
        {
            return value;
        }

        return default;
    }

    public async ValueTask<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        var query = new Query<T>(
            _dbAdapter.RenderSql(Where(predicate).CreateSql<T>(limit: 1)),
            _objectReaderProvider.GetReaderFunc<T>(),
            _dbConnector
        );

        await foreach (var value in query.WithCancellation(cancellationToken))
        {
            return value;
        }

        return default;
    }

    public async ValueTask<T> FirstAsync(CancellationToken cancellationToken = default) =>
        await FirstOrDefaultAsync(cancellationToken)
        ?? throw new InvalidOperationException("Sequence contains no elements");

    public async ValueTask<T> FirstAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default
    ) =>
        await FirstOrDefaultAsync(predicate, cancellationToken)
        ?? throw new InvalidOperationException("Sequence contains no elements");

    [Pure]
    public Query<TOut> Select<TOut>(Expression<Func<T, TOut>> selector) => CreateQuery(selector);

    [Pure]
    private Query<TOut> CreateQuery<TOut>(Expression<Func<T, TOut>> selector) =>
        new(
            _dbAdapter.RenderSql(CreateSql(selector)),
            _objectReaderProvider.GetReaderFunc<TOut>(),
            _dbConnector
        );

    [Pure]
    public IAsyncEnumerable<T> CreateQuery() => CreateQuery<T>(selector: null!);

    private Sql CreateSql<TOut>(Expression<Func<T, TOut>>? selector = null, int? limit = null)
    {
        var sql = new Sql(100, 2);
        var tableIndices = CreateTableIndicesMap();
        var tableAlias = tableIndices.ContainsKey(tableSource.Table.Name) ? $"{tableSource.Table.Name}{tableIndices[tableSource.Table.Name]++}" : null;
        var joinAliases = joins.Select(x => tableIndices.ContainsKey(x.Table.Name) ? $"{x.Table.Name}{tableIndices[x.Table.Name]++}" : null);
        var tableSourcesWithAliases = ((IReadOnlyCollection<TableSource>)[tableSource, ..joins]).Zip([tableAlias, ..joinAliases]).ToArray();
        
        sql.AppendLiteral("SELECT ");
        if (selector == null)
        {
            sql.AppendLiteral("*");
        }
        else
        {
            ExpressionRenderer.RenderExpression(
                options.IdentifierMapper,
                selector.Body,
                tableSourcesWithAliases,
                sql
            );
        }

        sql.AppendLiteral(" FROM ");
        AppendTableSource(tableSource, tableAlias);

        if (joins.Any())
        {
            foreach (var (join, alias) in joins.Zip(joinAliases))
            {
                sql.AppendLiteral($" JOIN ");
                AppendTableSource(join, alias);

                sql.AppendLiteral($" ON ");
                ExpressionRenderer.RenderExpression(
                    options.IdentifierMapper,
                    join.Condition,
                    tableSourcesWithAliases,
                    sql
                );
            }
        }

        if (condition != null)
        {
            sql.AppendLiteral(" WHERE ");
            ExpressionRenderer.RenderExpression(
                options.IdentifierMapper,
                condition,
                tableSourcesWithAliases,
                sql
            );
        }

        if (limit != null)
        {
            sql.AppendLiteral(" LIMIT ");
            sql.AppendLiteral(limit.Value.ToString());
        }

        return sql;

        void AppendTableSource(TableSource source, string? alias)
        {
            var table = source.Table.Name;
            sql.AppendIdentifier(table);
            if (alias != null)
            {
                sql.AppendLiteral($" AS ");
                sql.AppendIdentifier(alias);
            }
        }
        
        Dictionary<string, int> CreateTableIndicesMap()
        {
            var tableCount = new Dictionary<string, int> { { tableSource.Table.Name, 1 } };
            foreach (var join in joins)
            {
                tableCount.TryAdd(join.Table.Name, 0);
                tableCount[join.Table.Name]++;
            }
        
            return tableCount.Where(x => x.Value > 1).ToDictionary(x => x.Key, _ => 0);
        }
    }

    public IAsyncEnumerable<T> ToAsyncEnumerable() => CreateQuery();
}

public record Join(ITable Table, Expression Expression, Expression Condition) : TableSource(Table, Expression);

