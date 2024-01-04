using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Cooke.Gnissel.Queries;
using Cooke.Gnissel.Services;
using Cooke.Gnissel.Utils;

namespace Cooke.Gnissel.PlusPlus;

public interface ITable
{
    string Name { get; }
}

public class Table<T> : IToAsyncEnumerable<T>, ITable
{
    private readonly DbOptions _options;
    private readonly IDbConnector _dbConnector;
    private readonly string _insertCommandText;
    private readonly WhereQuery<T> _whereQuery;
    private readonly IDbAdapter _dbAdapter;
    private readonly IIdentifierMapper _identifierMapper;

    public Table(DbOptions options)
    {
        _options = options;
        _identifierMapper = options.IdentifierMapper;
        _dbAdapter = options.DbAdapter;
        _dbConnector = options.DbConnector;
        Columns = CreateColumns(options, this);
        _whereQuery = new WhereQuery<T>(
            options,
            Name,
            [],
            null,
            Columns.Cast<IColumn>().ToImmutableArray()
        );
        _insertCommandText = _dbAdapter.RenderSql(CreateInsertSql(_dbAdapter)).CommandText;
    }

    public ImmutableArray<Column<T>> Columns { get; set; }

    public Table(Table<T> source, DbOptions options)
    {
        _whereQuery = source._whereQuery;
        _dbConnector = options.DbConnector;
        _insertCommandText = source._insertCommandText;
        _options = options;
        _identifierMapper = options.IdentifierMapper;
        _dbAdapter = options.DbAdapter;
        Columns = source.Columns;
    }

    public string Name { get; } = typeof(T).Name.ToLower() + "s";

    private static ImmutableArray<Column<T>> CreateColumns(DbOptions dbOptions, Table<T> table)
    {
        var objectParameter = Expression.Parameter(typeof(T));
        return typeof(T)
            .GetProperties()
            .SelectMany(
                p =>
                    CreateColumns(
                        dbOptions,
                        p,
                        objectParameter,
                        Expression.Property(objectParameter, p),
                        table
                    )
            )
            .ToImmutableArray();
    }

    private static IEnumerable<Column<T>> CreateColumns(DbOptions dbOptions,
        PropertyInfo p,
        ParameterExpression rootExpression,
        Expression memberExpression, Table<T> table)
    {
        if (p.GetDbType() != null)
            yield return CreateColumn(dbOptions, p, rootExpression, memberExpression, table);
        else if (p.PropertyType == typeof(string) || p.PropertyType.IsPrimitive)
            yield return CreateColumn(dbOptions, p, rootExpression, memberExpression, table);
        else if (p.PropertyType.IsClass)
            foreach (
                var column in p.PropertyType
                    .GetProperties()
                    .SelectMany<PropertyInfo, Column<T>>(
                        innerProperty =>
                            CreateColumns(
                                dbOptions,
                                innerProperty,
                                rootExpression,
                                Expression.Property(memberExpression, innerProperty), table
                            )
                    )
            )
                yield return column;
        else
            yield return CreateColumn(dbOptions, p, rootExpression, memberExpression, table);
    }

    private static Column<T> CreateColumn(DbOptions dbOptions,
        PropertyInfo p,
        ParameterExpression objectExpression,
        Expression memberExpression, Table<T> table)
    {
        return new Column<T>(
            table,
            p.GetDbName() ?? dbOptions.DbAdapter.DefaultIdentifierMapper.ToColumnName(p),
            p.GetCustomAttribute<DatabaseGeneratedAttribute>()
                ?.Let(x => x.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity) ?? false,
            p,
            CreateParameterFactory(
                dbOptions.DbAdapter,
                memberExpression,
                p.GetDbType(),
                objectExpression
            )
        );
    }

    private static Func<T, DbParameter> CreateParameterFactory(
        IDbAdapter dbAdapter,
        Expression valueExpression,
        string? dbType,
        ParameterExpression tableItemParameter
    )
    {
        return Expression
            .Lambda<Func<T, DbParameter>>(
                Expression.Call(
                    Expression.Constant(dbAdapter),
                    nameof(dbAdapter.CreateParameter),
                    new[] { valueExpression.Type },
                    valueExpression,
                    Expression.Constant(dbType, typeof(string))
                ),
                tableItemParameter
            )
            .Compile();
    }

    [Pure]
    public ExecuteQuery Insert(T item)
    {
        var insertColumns = Columns.Where(x => !x.IsIdentity);

        var sql = new RenderedSql(
            _insertCommandText,
            insertColumns.Select(col => col.CreateParameter(item)).ToArray()
        );
        return new ExecuteQuery(_dbConnector, sql, CancellationToken.None);
    }

    [Pure]
    public ExecuteQuery Insert(params T[] items)
    {
        return Insert((IEnumerable<T>)items);
    }

    [Pure]
    public ExecuteQuery Insert(IEnumerable<T> items)
    {
        var insertColumns = Columns.Where(x => !x.IsIdentity).ToArray();
        var itemsArray = items as ICollection<T> ?? items.ToArray();
        var sb = new StringBuilder(
            _insertCommandText.Length + insertColumns.Length * 6 * itemsArray.Count
        );
        sb.Append(_insertCommandText);
        for (var i = 1; i < itemsArray.Count; i++)
        {
            sb.Append(", (");
            for (var j = 0; j < insertColumns.Length; j++)
            {
                if (j > 0)
                    sb.Append(", ");

                sb.Append('$');
                sb.Append(i * insertColumns.Length + j + 1);
            }

            sb.Append(") ");
        }

        var sql = new RenderedSql(
            sb.ToString(),
            itemsArray
                .SelectMany(item => insertColumns.Select(col => col.CreateParameter(item)))
                .ToArray()
        );
        return new ExecuteQuery(_dbConnector, sql, CancellationToken.None);
    }

    private Sql CreateInsertSql(IDbAdapter dbAdapter)
    {
        var sql = new Sql(20 + Columns.Length * 4);
        sql.AppendLiteral("INSERT INTO ");
        sql.AppendIdentifier(Name);
        sql.AppendLiteral(" (");
        var firstColumn = true;
        foreach (var column in Columns.Where(x => !x.IsIdentity))
        {
            if (!firstColumn)
                sql.AppendLiteral(", ");
            sql.AppendIdentifier(column.Name);
            firstColumn = false;
        }

        sql.AppendLiteral(") VALUES(");
        var firstParam = true;
        foreach (var dbParameter in Columns.Where(x => !x.IsIdentity))
        {
            if (!firstParam)
                sql.AppendLiteral(", ");
            sql.AppendFormatted(dbParameter);
            firstParam = false;
        }

        sql.AppendLiteral(")");
        return sql;
    }

    [Pure]
    public Query<TOut> Select<TOut>(Expression<Func<T, TOut>> selector) =>
        _whereQuery.Select(selector);

    [Pure]
    public WhereQuery<T> Where(Expression<Predicate<T>> predicate) => _whereQuery.Where(predicate);

    [Pure]
    public ValueTask<T?> FirstOrDefaultAsync(
        Expression<Predicate<T>> predicate,
        CancellationToken cancellationToken = default
    ) => _whereQuery.FirstOrDefaultAsync(predicate, cancellationToken);

    [Pure]
    public ValueTask<T> FirstAsync(
        Expression<Predicate<T>> predicate,
        CancellationToken cancellationToken = default
    ) => _whereQuery.FirstAsync(predicate, cancellationToken);

    public IAsyncEnumerable<T> ToAsyncEnumerable() => _whereQuery.ToAsyncEnumerable();

    public ExecuteQuery Delete(Expression<Predicate<T>> predicate)
    {
        var sql = new Sql(100, 2);
        sql.AppendLiteral($"DELETE FROM ");
        sql.AppendIdentifier(Name);
        sql.AppendLiteral(" WHERE ");

        ExpressionRenderer.RenderExpression(
            _identifierMapper,
            predicate,
            _whereQuery.Columns,
            sql
        );

        return new ExecuteQuery(_dbConnector, _dbAdapter.RenderSql(sql), CancellationToken.None);
    }

    public ExecuteQuery Update(
        Expression<Predicate<T>> predicate,
        Func<ISetCalls<T>, ISetCalls<T>> setCaller
    )
    {
        var sql = new Sql(100, 2);
        sql.AppendLiteral("UPDATE ");
        sql.AppendIdentifier(Name);

        var calls = new SetCalls<T>();
        setCaller(calls);

        sql.AppendLiteral(" SET ");
        for (var index = 0; index < calls.Calls.Count; index++)
        {
            var call = calls.Calls[index];

            if (index > 0)
            {
                sql.AppendLiteral(", ");
            }

            ExpressionRenderer.RenderExpression(
                _identifierMapper,
                call.property,
                _whereQuery.Columns,
                sql
            );
            sql.AppendLiteral(" = ");

            ExpressionRenderer.RenderExpression(
                _identifierMapper,
                call.value,
                _whereQuery.Columns,
                sql,
                constantsAsParameters: true
            );
        }

        sql.AppendLiteral(" WHERE ");
        ExpressionRenderer.RenderExpression(
            _identifierMapper,
            predicate,
            _whereQuery.Columns,
            sql
        );
        return new ExecuteQuery(_dbConnector, _dbAdapter.RenderSql(sql), CancellationToken.None);
    }

    public WhereQuery<T, TJoin> Join<TJoin>(
        Table<TJoin> outer,
        Expression<Func<T, TJoin, bool>> predicate
    )
    {
        return new WhereQuery<T, TJoin>(_options, Name, [new Join(outer.Name, predicate)], null,
            Columns.As<IColumn>().AddRange(outer.Columns));
    }
}
